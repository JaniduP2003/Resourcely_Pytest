using Backend_Resourcely.Data;
using Backend_Resourcely.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend_Resourcely.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FloorsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public FloorsController(AppDbContext db)
        {
            _db = db;
        }

        // GET: api/floors/by-building/{buildingId}
        [HttpGet("by-building/{buildingId:int}")]
        public async Task<ActionResult<IEnumerable<object>>> GetFloorsByBuilding(int buildingId)
        {
            var floors = await _db.Floors
                .AsNoTracking()
                .Where(f => f.BuildingId == buildingId)
                .Select(f => new
                {
                    f.Id,
                    f.Name,
                    f.Description,
                    f.BuildingId,
                    BuildingName = f.Building.Name
                })
                .OrderBy(f => f.Name)
                .ToListAsync();

            return Ok(floors);
        }

        // GET: api/floors/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<object>> GetFloor(int id)
        {
            var floor = await _db.Floors
                .AsNoTracking()
                .Where(f => f.Id == id)
                .Select(f => new
                {
                    f.Id,
                    f.Name,
                    f.Description,
                    f.BuildingId,
                    BuildingName = f.Building.Name,
                    Blocks = f.Blocks.Select(b => new
                    {
                        b.Id,
                        b.Name,
                        b.Description
                    }).OrderBy(b => b.Name)
                })
                .FirstOrDefaultAsync();

            if (floor == null)
            {
                return NotFound(new { message = "Floor not found." });
            }

            return Ok(floor);
        }

        // POST: api/floors (Admin only)
        [HttpPost]
        public async Task<ActionResult<object>> CreateFloor(CreateFloorDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name) || dto.BuildingId <= 0)
            {
                return BadRequest(new { message = "Floor name and building ID are required." });
            }

            // Check if building exists
            var buildingExists = await _db.Buildings.AnyAsync(b => b.Id == dto.BuildingId);
            if (!buildingExists)
            {
                return BadRequest(new { message = "Invalid building ID." });
            }

            var floor = new Floor
            {
                Name = dto.Name.Trim(),
                Description = dto.Description?.Trim() ?? "",
                BuildingId = dto.BuildingId,
                CreatedAt = DateTime.UtcNow
            };

            _db.Floors.Add(floor);
            await _db.SaveChangesAsync();

            // Load building name for response
            await _db.Entry(floor)
                .Reference(f => f.Building)
                .LoadAsync();

            return CreatedAtAction(nameof(GetFloor), new { id = floor.Id }, new
            {
                floor.Id,
                floor.Name,
                floor.Description,
                floor.BuildingId,
                BuildingName = floor.Building.Name
            });
        }

        public class CreateFloorDto
        {
            public string Name { get; set; } = string.Empty;
            public string? Description { get; set; }
            public int BuildingId { get; set; }
        }
    }
}