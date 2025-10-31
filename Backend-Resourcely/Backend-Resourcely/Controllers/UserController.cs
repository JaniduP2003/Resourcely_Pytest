using Backend_Resourcely.Data;
using Backend_Resourcely.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend_Resourcely.Dto;

namespace Backend_Resourcely.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _db;

        public UserController(AppDbContext db)
        {
            _db = db;
        }

        // GET: api/user/stats
        [HttpGet("stats")]
        public async Task<ActionResult<object>> GetUserStats()
        {
            try
            {
                var now = DateTime.UtcNow;
                var today = now.Date;

                // For now, we'll get general stats since we don't have user authentication
                // In a real app, you'd get the current user ID from authentication

                var totalBookings = await _db.Bookings.CountAsync();
                var upcomingBookings = await _db.Bookings
                    .CountAsync(b => b.BookingAt > now && b.Status == "Approved");
                var totalResources = await _db.Resources
                    .CountAsync(r => r.IsActive);
                var availableToday = await _db.Resources
                    .Where(r => r.IsActive)
                    .CountAsync(r => !_db.Bookings.Any(b =>
                        b.ResourceId == r.Id &&
                        b.Status == "Approved" &&
                        b.BookingAt.Date == today &&
                        b.BookingAt <= now &&
                        b.EndAt > now));

                var stats = new
                {
                    upcomingBookings,
                    totalBookings,
                    availableRooms = availableToday,
                    favoriteRooms = 0 // Placeholder - would need favorites feature
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to load user stats", error = ex.Message });
            }
        }

        // GET: api/user/bookings/recent
        [HttpGet("bookings/recent")]
        public async Task<ActionResult<IEnumerable<object>>> GetRecentBookings()
        {
            try
            {
                // For demo purposes, get recent bookings for all users
                // In a real app, filter by current user ID
                var recentBookings = await _db.Bookings
                    .Include(b => b.Resource)
                    .ThenInclude(r => r.Block)
                    .ThenInclude(bl => bl.Floor)
                    .ThenInclude(f => f.Building)
                    .OrderByDescending(b => b.CreatedAt)
                    .Take(5)
                    .Select(b => new
                    {
                        id = b.Id.ToString(),
                        roomName = b.Resource.Name,
                        date = b.BookingAt.ToString("yyyy-MM-dd"),
                        time = $"{b.BookingAt:HH:mm} - {b.EndAt:HH:mm}",
                        status = b.Status == "Approved" ? "confirmed" :
                                b.Status == "Rejected" ? "cancelled" : "pending",
                        location = $"{b.Resource.Block.Floor.Building.Name} > {b.Resource.Block.Floor.Name} > {b.Resource.Block.Name}",
                        b.Reason,
                        b.Capacity,
                        b.Contact
                    })
                    .ToListAsync();

                return Ok(recentBookings);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to load recent bookings", error = ex.Message });
            }
        }

        // GET: api/user/role/user
        [ HttpGet("role/user")]
        public async Task<ActionResult<IEnumerable<object>>> GetAllRoleUser()
        {
            var users = await _db.Users
                .Where(u => u.Role.ToLower() == "user")
                .Select(u => new
        {
            u.Id,
            u.Username,
            u.Email,
            u.Role,
            u.CreatedAt
        })
            .OrderBy(u => u.Username)
            .ToListAsync();

        return Ok(users);
        }


        [HttpGet("students")]
        public async Task<ActionResult<IEnumerable<object>>> GetAllStudents()
        {
            var students = await _db.Users
                .Where(u => u.Role.ToLower() == "student")
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.Email,
                    u.Role,
                    // Include batch if exists
                    Batch = _db.StudentProfiles
                        .Where(sp => sp.UserId == u.Id)
                        .Select(sp => new
                        {
                            sp.BatchId,
                            BatchName = sp.Batch.Name,
                            BatchCode = sp.Batch.Code
                        })
                        .FirstOrDefault()
                })
                .ToListAsync();

            return Ok(students);
        }


        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetAllUsers()
        {
            var users = await _db.Users
                .Where(u => u.Role.ToLower() != "admin")
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.Email,
                    u.Role,
                    u.CreatedAt
                })
                .OrderBy(u => u.Username)
                .ToListAsync();

            return Ok(users);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var user = await _db.Users.FindAsync(id);
                if (user == null)
                    return NotFound(new { message = "User not found" });

                if (user.Role.Equals("admin", StringComparison.OrdinalIgnoreCase))
                    return BadRequest(new { message = "Admin user cannot be deleted" });

                // Clean up dependent entities first if necessary
                var studentProfile = await _db.StudentProfiles.FindAsync(id);
                if (studentProfile != null)
                    _db.StudentProfiles.Remove(studentProfile);

                // If your schema has other dependents (e.g., bookings owned by user),
                // either enforce ON DELETE CASCADE in the FK or handle them here.
                // Example (uncomment if applicable):
                // var userBookings = await _db.Bookings.Where(b => b.UserId == id).ToListAsync();
                // if (userBookings.Any())
                //     _db.Bookings.RemoveRange(userBookings);

                _db.Users.Remove(user);
                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return Ok(new { message = "User deleted successfully", userId = id });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return StatusCode(500, new { message = "Failed to delete user", error = ex.Message });
            }
        }


        [HttpGet("lecturers")]
        public async Task<ActionResult<IEnumerable<object>>> GetAllLecturers()
        {
            var lecturers = await _db.Users
                .Where(u => u.Role.ToLower() == "lecturer")
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.Email,
                    u.Role,
                    // Include department if exists
                    Department = _db.LecturerProfiles
                        .Where(lp => lp.UserId == u.Id)
                        .Select(lp => new
                        {
                            lp.DepartmentId,
                            DepartmentName = lp.Department.Name
                        })
                        .FirstOrDefault()
                })
                .ToListAsync();

            return Ok(lecturers);
        }

        // ✅ Update a user's role (PUT /api/users/{id}/role)
        [HttpPut("{id}/role")]
        public async Task<IActionResult> UpdateUserRole(int id, [FromBody] UpdateUserRoleDto dto)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null)
                return NotFound(new { message = "User not found" });

            if (user.Role.ToLower() == "admin")
                return BadRequest(new { message = "Admin role cannot be modified" });

            user.Role = dto.Role;
            await _db.SaveChangesAsync();

            return Ok(new { message = "Role updated successfully", user.Id, user.Username, user.Role });
        }

        //Assign Batch for student
        [HttpPut("{id:int}/batch")]
        public async Task<IActionResult> AssignUserToBatch(int id, [FromBody] AssignUserToBatchDto dto)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound(new { message = "User not found" });

            if (user.Role.Equals("admin", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "Admin cannot be assigned to a batch" });

            if (!user.Role.Equals("student", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "Only users with role 'student' can be assigned a batch" });

            var batch = await _db.Batches.FirstOrDefaultAsync(b => b.Id == dto.BatchId && b.IsActive);
            if (batch == null) return NotFound(new { message = "Active batch not found" });

            var profile = await _db.StudentProfiles.FindAsync(id);
            if (profile == null)
            {
                profile = new StudentProfile
                {
                    UserId = id,
                    BatchId = dto.BatchId
                };
                _db.StudentProfiles.Add(profile);
            }
            else
            {
                profile.BatchId = dto.BatchId;
            }

            await _db.SaveChangesAsync();
            return Ok(new
            {
                message = "Batch assigned successfully",
                userId = id,
                batchId = dto.BatchId,
                batch.Name,
                batch.Code
            });
        }

        // ✅ Remove a student's batch assignment
        [HttpDelete("{id:int}/batch")]
        public async Task<IActionResult> RemoveUserBatch(int id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound(new { message = "User not found" });

            if (!user.Role.Equals("student", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "Only 'student' users have batch assignments" });

            var profile = await _db.StudentProfiles.FindAsync(id);
            if (profile == null) return NotFound(new { message = "Student has no batch assignment" });

            _db.StudentProfiles.Remove(profile);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Batch assignment removed", userId = id });
        }

        // Assign Department for lecturer
        [HttpPut("{id:int}/department")]
        public async Task<IActionResult> AssignLecturerToDepartment(int id, [FromBody] AssignDepartmentDto dto)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound(new { message = "User not found" });

            if (!user.Role.Equals("lecturer", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "Only users with role 'lecturer' can be assigned a department" });

            var department = await _db.Departments.FindAsync(dto.DepartmentId);
            if (department == null) return NotFound(new { message = "Department not found" });

            var profile = await _db.LecturerProfiles.FindAsync(id);
            if (profile == null)
            {
                profile = new LecturerProfile
                {
                    UserId = id,
                    DepartmentId = dto.DepartmentId
                };
                _db.LecturerProfiles.Add(profile);
            }
            else
            {
                profile.DepartmentId = dto.DepartmentId;
            }

            await _db.SaveChangesAsync();
            return Ok(new
            {
                message = "Department assigned successfully",
                userId = id,
                departmentId = dto.DepartmentId,
                department.Name
            });
        }

        // Remove a lecturer's department assignment
        [HttpDelete("{id:int}/department")]
        public async Task<IActionResult> RemoveLecturerDepartment(int id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound(new { message = "User not found" });

            if (!user.Role.Equals("lecturer", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "Only 'lecturer' users have department assignments" });

            var profile = await _db.LecturerProfiles.FindAsync(id);
            if (profile == null) return NotFound(new { message = "Lecturer has no department assignment" });

            _db.LecturerProfiles.Remove(profile);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Department assignment removed", userId = id });
        }


    }
}