using Backend_Resourcely.Data;
using Backend_Resourcely.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend_Resourcely.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BuildingsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public BuildingsController(AppDbContext db)
        {
            _db = db;
        }

        // GET: api/buildings
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetBuildings()
        {
            var buildings = await _db.Buildings
                .AsNoTracking()
                .Select(b => new
                {
                    b.Id,
                    b.Name,
                    b.Description,
                    b.CreatedAt
                })
                .OrderBy(b => b.Name)
                .ToListAsync();

            return Ok(buildings);
        }

        // GET: api/buildings/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<object>> GetBuilding(int id)
        {
            var building = await _db.Buildings
                .AsNoTracking()
                .Where(b => b.Id == id)
                .Select(b => new
                {
                    b.Id,
                    b.Name,
                    b.Description,
                    b.CreatedAt,
                    Floors = b.Floors.Select(f => new
                    {
                        f.Id,
                        f.Name,
                        f.Description
                    }).OrderBy(f => f.Name)
                })
                .FirstOrDefaultAsync();

            if (building == null)
            {
                return NotFound(new { message = "Building not found." });
            }

            return Ok(building);
        }

        // POST: api/buildings (Admin only)
        [HttpPost]
        public async Task<ActionResult<object>> CreateBuilding(CreateBuildingDto dto)
        {
            // TODO: Add admin role check here
            // For now, we'll skip authentication to keep it simple

            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                return BadRequest(new { message = "Building name is required." });
            }

            var building = new Building
            {
                Name = dto.Name.Trim(),
                Description = dto.Description?.Trim() ?? "",
                CreatedAt = DateTime.UtcNow
            };

            _db.Buildings.Add(building);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetBuilding), new { id = building.Id }, new
            {
                building.Id,
                building.Name,
                building.Description,
                building.CreatedAt
            });
        }

        public class CreateBuildingDto
        {
            public string Name { get; set; } = string.Empty;
            public string? Description { get; set; }
        }
    }
}