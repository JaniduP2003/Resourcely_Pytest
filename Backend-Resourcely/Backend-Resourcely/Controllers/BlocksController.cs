using Backend_Resourcely.Data;
using Backend_Resourcely.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend_Resourcely.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BlocksController : ControllerBase
    {
        private readonly AppDbContext _db;

        public BlocksController(AppDbContext db)
        {
            _db = db;
        }

        // GET: api/blocks/by-floor/{floorId}
        [HttpGet("by-floor/{floorId:int}")]
        public async Task<ActionResult<IEnumerable<object>>> GetBlocksByFloor(int floorId)
        {
            var blocks = await _db.Blocks
                .AsNoTracking()
                .Where(b => b.FloorId == floorId)
                .Select(b => new
                {
                    b.Id,
                    b.Name,
                    b.Description,
                    b.FloorId,
                    FloorName = b.Floor.Name,
                    BuildingName = b.Floor.Building.Name
                })
                .OrderBy(b => b.Name)
                .ToListAsync();

            return Ok(blocks);
        }

        // GET: api/blocks/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<object>> GetBlock(int id)
        {
            var block = await _db.Blocks
                .AsNoTracking()
                .Where(b => b.Id == id)
                .Select(b => new
                {
                    b.Id,
                    b.Name,
                    b.Description,
                    b.FloorId,
                    FloorName = b.Floor.Name,
                    BuildingName = b.Floor.Building.Name,
                    Resources = b.Resources.Where(r => r.IsActive).Select(r => new
                    {
                        r.Id,
                        r.Name,
                        r.Type,
                        r.Capacity,
                        r.Description
                    }).OrderBy(r => r.Name)
                })
                .FirstOrDefaultAsync();

            if (block == null)
            {
                return NotFound(new { message = "Block not found." });
            }

            return Ok(block);
        }

        // POST: api/blocks (Admin only)
        [HttpPost]
        public async Task<ActionResult<object>> CreateBlock(CreateBlockDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name) || dto.FloorId <= 0)
            {
                return BadRequest(new { message = "Block name and floor ID are required." });
            }

            // Check if floor exists
            var floorExists = await _db.Floors.AnyAsync(f => f.Id == dto.FloorId);
            if (!floorExists)
            {
                return BadRequest(new { message = "Invalid floor ID." });
            }

            var block = new Block
            {
                Name = dto.Name.Trim(),
                Description = dto.Description?.Trim() ?? "",
                FloorId = dto.FloorId,
                CreatedAt = DateTime.UtcNow
            };

            _db.Blocks.Add(block);
            await _db.SaveChangesAsync();

            // Load floor and building names for response
            await _db.Entry(block)
                .Reference(b => b.Floor)
                .Query()
                .Include(f => f.Building)
                .LoadAsync();

            return CreatedAtAction(nameof(GetBlock), new { id = block.Id }, new
            {
                block.Id,
                block.Name,
                block.Description,
                block.FloorId,
                FloorName = block.Floor.Name,
                BuildingName = block.Floor.Building.Name
            });
        }

        public class CreateBlockDto
        {
            public string Name { get; set; } = string.Empty;
            public string? Description { get; set; }
            public int FloorId { get; set; }
        }
    }
}