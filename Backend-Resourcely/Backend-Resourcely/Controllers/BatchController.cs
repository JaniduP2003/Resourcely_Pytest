using Backend_Resourcely.Data;
using Backend_Resourcely.Models;
using Backend_Resourcely.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend_Resourcely.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BatchesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BatchesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/batches
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Batch>>> GetBatches()
        {
            return await _context.Batches
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
        }

        // GET: api/batches/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Batch>> GetBatch(int id)
        {
            var batch = await _context.Batches.FindAsync(id);
            if (batch == null) return NotFound();
            return batch;
        }

        // POST: api/batches
        [HttpPost]
        public async Task<ActionResult<Batch>> CreateBatch([FromBody] BatchCreateDto dto)
        {
            if (await _context.Batches.AnyAsync(b => b.Name == dto.Name))
                return Conflict($"A batch named '{dto.Name}' already exists.");

            var batch = new Batch
            {
                Name = dto.Name,
                Code = dto.Code,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Batches.Add(batch);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetBatch), new { id = batch.Id }, batch);
        }

        // PUT: api/batches/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateBatch(int id, [FromBody] BatchUpdateDto dto)
        {
            var batch = await _context.Batches.FindAsync(id);
            if (batch == null) return NotFound();

            if (dto.Name != null) batch.Name = dto.Name;
            if (dto.Code != null) batch.Code = dto.Code;
            if (dto.StartDate.HasValue) batch.StartDate = dto.StartDate;
            if (dto.EndDate.HasValue) batch.EndDate = dto.EndDate;
            if (dto.IsActive.HasValue) batch.IsActive = dto.IsActive.Value;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/batches/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteBatch(int id)
        {
            var batch = await _context.Batches.FindAsync(id);
            if (batch == null) return NotFound();

            _context.Batches.Remove(batch);
            await _context.SaveChangesAsync();

            return NoContent();
        }



        // ✅ Bulk-assign student users to a batch
        // POST: /api/batches/{batchId}/students
        [HttpPost("{batchId:int}/students")]
        public async Task<IActionResult> BulkAssignStudents(int batchId, [FromBody] BulkAssignStudentsToBatchDto dto)
        {
            if (dto == null || dto.UserIds.Count == 0)
                return BadRequest(new { message = "No user IDs provided" });

            var batch = await _context.Batches.FirstOrDefaultAsync(b => b.Id == batchId && b.IsActive);
            if (batch == null) return NotFound(new { message = "Active batch not found" });

            var students = await _context.Users
                .Where(u => dto.UserIds.Contains(u.Id) && u.Role.ToLower() == "student")
                .ToListAsync();

            var existingProfiles = await _context.StudentProfiles
                .Where(sp => dto.UserIds.Contains(sp.UserId))
                .ToDictionaryAsync(sp => sp.UserId, sp => sp);

            foreach (var student in students)
            {
                if (existingProfiles.TryGetValue(student.Id, out var prof))
                {
                    prof.BatchId = batchId;
                }
                else
                {
                    _context.StudentProfiles.Add(new StudentProfile
                    {
                        UserId = student.Id,
                        BatchId = batchId
                    });
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Students assigned to batch",
                batchId,
                assignedCount = students.Count
            });
        }

        // ✅ Get all students in a batch
        // GET: /api/batches/{batchId}/students
        [HttpGet("{batchId:int}/students")]
        public async Task<ActionResult<IEnumerable<object>>> GetStudentsInBatch(int batchId)
        {
            var batch = await _context.Batches.FindAsync(batchId);
            if (batch == null) return NotFound(new { message = "Batch not found" });

            var students = await _context.StudentProfiles
                .Where(sp => sp.BatchId == batchId)
                .Include(sp => sp.User)
                .Select(sp => new
                {
                    sp.UserId,
                    sp.User.Username,
                    sp.User.Email,
                    sp.User.Role,
                    BatchId = sp.BatchId,
                    BatchName = batch.Name,
                    BatchCode = batch.Code
                })
                .OrderBy(x => x.Username)
                .ToListAsync();

            return Ok(students);
        }
    }
}
