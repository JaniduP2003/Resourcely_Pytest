using Backend_Resourcely.Data;
using Backend_Resourcely.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend_Resourcely.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ResourcesController : ControllerBase
    {
        private readonly AppDbContext _db;

        public ResourcesController(AppDbContext db)
        {
            _db = db;
        }

        // GET: api/resources/all (Admin only)
        [HttpGet("all")]
        public async Task<ActionResult<IEnumerable<object>>> GetAllResources()
        {
            var resources = await _db.Resources
                .AsNoTracking()
                .Where(r => r.IsActive)
                .Select(r => new
                {
                    r.Id,
                    r.Name,
                    r.Type,
                    r.Description,
                    r.Capacity,
                    r.BlockId,
                    r.IsRestricted,
                    r.RestrictedToRoles,
                    BlockName = r.Block.Name,
                    FloorName = r.Block.Floor.Name,
                    BuildingName = r.Block.Floor.Building.Name
                })
                .OrderBy(r => r.BuildingName)
                .ThenBy(r => r.FloorName)
                .ThenBy(r => r.BlockName)
                .ThenBy(r => r.Name)
                .ToListAsync();

            return Ok(resources);
        }

        // GET: api/resources/by-block/{blockId}
        [HttpGet("by-block/{blockId:int}")]
        public async Task<ActionResult<IEnumerable<object>>> GetResourcesByBlock(int blockId, [FromQuery] string? userRole = null)
        {
            var query = _db.Resources
                .AsNoTracking()
                .Where(r => r.BlockId == blockId && r.IsActive);

            // Apply role-based filtering if userRole is provided
            if (!string.IsNullOrEmpty(userRole))
            {
                query = query.Where(r => !r.IsRestricted || 
                                   string.IsNullOrEmpty(r.RestrictedToRoles) || 
                                   r.RestrictedToRoles.Contains(userRole));
            }

            var resources = await query
                .Select(r => new
                {
                    r.Id,
                    r.Name,
                    r.Type,
                    r.Description,
                    r.Capacity,
                    r.BlockId,
                    r.IsRestricted,
                    r.RestrictedToRoles,
                    BlockName = r.Block.Name,
                    FloorName = r.Block.Floor.Name,
                    BuildingName = r.Block.Floor.Building.Name
                })
                .OrderBy(r => r.Name)
                .ToListAsync();

            return Ok(resources);
        }

        // GET: api/resources/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<object>> GetResource(int id)
        {
            var resource = await _db.Resources
                .AsNoTracking()
                .Where(r => r.Id == id)
                .Select(r => new
                {
                    r.Id,
                    r.Name,
                    r.Type,
                    r.Description,
                    r.Capacity,
                    r.IsActive,
                    r.BlockId,
                    BlockName = r.Block.Name,
                    FloorName = r.Block.Floor.Name,
                    BuildingName = r.Block.Floor.Building.Name,
                    FullPath = $"{r.Block.Floor.Building.Name} > {r.Block.Floor.Name} > {r.Block.Name} > {r.Name}"
                })
                .FirstOrDefaultAsync();

            if (resource == null)
            {
                return NotFound(new { message = "Resource not found." });
            }

            return Ok(resource);
        }

        // GET: api/resources/{id}/availability
        [HttpGet("{id:int}/availability")]
        public async Task<ActionResult<object>> GetResourceAvailability(int id, [FromQuery] DateTime date)
        {
            var resource = await _db.Resources.FindAsync(id);
            if (resource == null || !resource.IsActive)
            {
                return NotFound(new { message = "Resource not found or not active." });
            }

            // Get all bookings for this resource on the specified date
            var startDate = date.Date;
            var endDate = startDate.AddDays(1);

            var bookings = await _db.Bookings
                .AsNoTracking()
                .Where(b => b.ResourceId == id && 
                           b.BookingAt >= startDate && 
                           b.BookingAt < endDate)
                .Select(b => new
                {
                    b.Id,
                    StartTime = b.BookingAt,
                    EndTime = b.EndAt,
                    b.Reason,
                    b.Capacity
                })
                .OrderBy(b => b.StartTime)
                .ToListAsync();

            return Ok(new
            {
                ResourceId = id,
                Date = date.ToString("yyyy-MM-dd"),
                Bookings = bookings,
                IsAvailable = !bookings.Any() // Simple check - can be enhanced
            });
        }

        // POST: api/resources (Admin only)
        [HttpPost]
        public async Task<ActionResult<object>> CreateResource(CreateResourceDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name) || 
                string.IsNullOrWhiteSpace(dto.Type) || 
                dto.Capacity <= 0 || 
                dto.BlockId <= 0)
            {
                return BadRequest(new { message = "All fields are required and capacity must be positive." });
            }

            // Check if block exists
            var blockExists = await _db.Blocks.AnyAsync(b => b.Id == dto.BlockId);
            if (!blockExists)
            {
                return BadRequest(new { message = "Invalid block ID." });
            }

            var resource = new Resource
            {
                Name = dto.Name.Trim(),
                Type = dto.Type.Trim(),
                Description = dto.Description?.Trim() ?? "",
                Capacity = dto.Capacity,
                BlockId = dto.BlockId,
                IsActive = true,
                IsRestricted = dto.IsRestricted,
                RestrictedToRoles = dto.RestrictedToRoles?.Trim() ?? "",
                CreatedAt = DateTime.UtcNow
            };

            _db.Resources.Add(resource);
            await _db.SaveChangesAsync();

            // Load hierarchy for response
            await _db.Entry(resource)
                .Reference(r => r.Block)
                .Query()
                .Include(b => b.Floor)
                .ThenInclude(f => f.Building)
                .LoadAsync();

            return CreatedAtAction(nameof(GetResource), new { id = resource.Id }, new
            {
                resource.Id,
                resource.Name,
                resource.Type,
                resource.Description,
                resource.Capacity,
                resource.BlockId,
                resource.IsRestricted,
                resource.RestrictedToRoles,
                BlockName = resource.Block.Name,
                FloorName = resource.Block.Floor.Name,
                BuildingName = resource.Block.Floor.Building.Name
            });
        }

        [HttpGet("available-now")]
        public async Task<ActionResult<IEnumerable<object>>> GetAvailableNow(
            [FromQuery] string? userRole = null,
            [FromQuery] int? blockId = null,
            [FromQuery] DateTime? at = null)
        {
            // Use UTC now by default. If 'at' supplied, compare in UTC for consistency.
            var now = (at.HasValue ? DateTime.SpecifyKind(at.Value, at.Value.Kind) : DateTime.UtcNow);
            if (now.Kind == DateTimeKind.Local) now = now.ToUniversalTime();

            var resourcesQuery = _db.Resources
                .AsNoTracking()
                .Where(r => r.IsActive);

            if (blockId.HasValue)
                resourcesQuery = resourcesQuery.Where(r => r.BlockId == blockId.Value);

            if (!string.IsNullOrEmpty(userRole))
            {
                resourcesQuery = resourcesQuery.Where(r =>
                    !r.IsRestricted ||
                    string.IsNullOrEmpty(r.RestrictedToRoles) ||
                    r.RestrictedToRoles.Contains(userRole));
            }

            // Exclude any resource that has a booking overlapping 'now'
            resourcesQuery = resourcesQuery.Where(r =>
                !_db.Bookings.Any(b =>
                    b.ResourceId == r.Id &&
                    b.BookingAt <= now &&
                    b.EndAt > now));

            var resources = await resourcesQuery
                .Select(r => new
                {
                    r.Id,
                    r.Name,
                    r.Type,
                    r.Description,
                    r.Capacity,
                    r.BlockId,
                    r.IsRestricted,
                    r.RestrictedToRoles,
                    BlockName = r.Block.Name,
                    FloorName = r.Block.Floor.Name,
                    BuildingName = r.Block.Floor.Building.Name
                })
                .OrderBy(r => r.BuildingName)
                .ThenBy(r => r.FloorName)
                .ThenBy(r => r.BlockName)
                .ThenBy(r => r.Name)
                .ToListAsync();

            return Ok(new
            {
                CheckedAtUtc = now.ToUniversalTime(),
                Count = resources.Count,
                Resources = resources
            });
        }

        public class CreateResourceDto
        {
            public string Name { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
            public string? Description { get; set; }
            public int Capacity { get; set; }
            public int BlockId { get; set; }
            public bool IsRestricted { get; set; } = false;
            public string? RestrictedToRoles { get; set; }
        }
    }
}