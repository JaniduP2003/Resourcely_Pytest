    using System.Globalization;
    using Backend_Resourcely.Data;
    using Backend_Resourcely.Models;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;

    namespace Backend_Resourcely.Controllers
    {
        [ApiController]
        [Route("api/[controller]")]
        public class BookingsController : ControllerBase
        {
            private readonly AppDbContext _db;

            public BookingsController(AppDbContext db)
            {
                _db = db;
            }

            public class BookingCreateDto
            {
                public int ResourceId { get; set; }
                public string Date { get; set; } = string.Empty; // "YYYY-MM-DD"
                public string Time { get; set; } = string.Empty; // "HH:mm"
                public string EndTime { get; set; } = string.Empty; // "HH:mm"
                public string Reason { get; set; } = string.Empty;
                public int Capacity { get; set; }
                public string Contact { get; set; } = string.Empty;
                public int? UserId { get; set; }
            }

            [HttpPost]
            public async Task<ActionResult<object>> Create(BookingCreateDto dto)
            {
                if (dto.ResourceId <= 0 ||
                    string.IsNullOrWhiteSpace(dto.Date) ||
                    string.IsNullOrWhiteSpace(dto.Time) ||
                    string.IsNullOrWhiteSpace(dto.EndTime) ||
                    string.IsNullOrWhiteSpace(dto.Reason) ||
                    string.IsNullOrWhiteSpace(dto.Contact) ||
                    dto.Capacity <= 0)
                {
                    return BadRequest(new { message = "All fields are required and capacity must be positive." });
                }

                // Parse booking start and end times
                if (!DateTime.TryParseExact(
                        $"{dto.Date} {dto.Time}",
                        "yyyy-MM-dd HH:mm",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out var bookingAt))
                {
                    return BadRequest(new { message = "Invalid date/time format for start time." });
                }

                if (!DateTime.TryParseExact(
                        $"{dto.Date} {dto.EndTime}",
                        "yyyy-MM-dd HH:mm",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out var endAt))
                {
                    return BadRequest(new { message = "Invalid date/time format for end time." });
                }

                if (endAt <= bookingAt)
                {
                    return BadRequest(new { message = "End time must be after start time." });
                }

                // Check if resource exists and is active
                var resource = await _db.Resources
                    .Include(r => r.Block)
                    .ThenInclude(b => b.Floor)
                    .ThenInclude(f => f.Building)
                    .FirstOrDefaultAsync(r => r.Id == dto.ResourceId);

                if (resource == null || !resource.IsActive)
                {
                    return BadRequest(new { message = "Resource not found or not available." });
                }

                // Check if requested capacity exceeds resource capacity
                if (dto.Capacity > resource.Capacity)
                {
                    return BadRequest(new { message = $"Requested capacity ({dto.Capacity}) exceeds resource capacity ({resource.Capacity})." });
                }

                // Check for overlapping bookings (prevent double-booking with approved bookings)
                var overlappingBooking = await _db.Bookings
                    .Where(b => b.ResourceId == dto.ResourceId &&
                               b.Status == "Approved" &&
                               ((bookingAt < b.EndAt && endAt > b.BookingAt)))
                    .FirstOrDefaultAsync();

                if (overlappingBooking != null)
                {
                    return BadRequest(new { message = "Resource is already booked during the requested time slot." });
                }

                var userId = dto.UserId ?? 1;

                var booking = new Booking
                {
                    UserId = userId,
                    ResourceId = dto.ResourceId,
                    BookingAt = bookingAt,
                    EndAt = endAt,
                    Reason = dto.Reason.Trim(),
                    Capacity = dto.Capacity,
                    Contact = dto.Contact.Trim(),
                    Status = "Pending", // All bookings start as pending
                    CreatedAt = DateTime.UtcNow
                };

                _db.Bookings.Add(booking);
                await _db.SaveChangesAsync();

                return CreatedAtAction(nameof(GetById), new { id = booking.Id }, new
                {
                    booking.Id,
                    booking.UserId,
                    booking.ResourceId,
                    ResourceName = resource.Name,
                    ResourceLocation = $"{resource.Block.Floor.Building.Name} > {resource.Block.Floor.Name} > {resource.Block.Name} > {resource.Name}",
                    BookingAt = booking.BookingAt,
                    EndAt = booking.EndAt,
                    booking.Reason,
                    booking.Capacity,
                    booking.Contact,
                    booking.CreatedAt
                });
            }

            [HttpGet("{id:int}")]
            public async Task<ActionResult<object>> GetById(int id)
            {
                var booking = await _db.Bookings
                    .AsNoTracking()
                    .Where(b => b.Id == id)
                    .Include(b => b.Resource)
                    .ThenInclude(r => r.Block)
                    .ThenInclude(bl => bl.Floor)
                    .ThenInclude(f => f.Building)
                    .Select(b => new
                    {
                        b.Id,
                        b.UserId,
                        b.ResourceId,
                        ResourceName = b.Resource.Name,
                        ResourceLocation = $"{b.Resource.Block.Floor.Building.Name} > {b.Resource.Block.Floor.Name} > {b.Resource.Block.Name} > {b.Resource.Name}",
                        BookingAt = b.BookingAt,
                        EndAt = b.EndAt,
                        b.Reason,
                        b.Capacity,
                        b.Contact,
                        b.Status,
                        b.ApprovedBy,
                        b.ApprovedAt,
                        b.RejectionReason,
                        b.CreatedAt
                    })
                    .FirstOrDefaultAsync();

                return booking is null ? NotFound() : Ok(booking);
            }

            [HttpDelete("{id:int}")]
            public async Task<IActionResult> Delete(int id)
            {
                var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.Id == id);
                if (booking is null)
                {
                    return NotFound();
                }

                // Block deleting past bookings
                if (booking.BookingAt <= DateTime.Now)
                {
                    return BadRequest(new { message = "Cannot delete past bookings." });
                }

                _db.Bookings.Remove(booking);
                await _db.SaveChangesAsync();
                return NoContent();
            }

            // GET: api/bookings/by-resource/{resourceId}
            [HttpGet("by-resource/{resourceId:int}")]
            public async Task<ActionResult<IEnumerable<object>>> GetBookingsByResource(int resourceId, [FromQuery] DateTime? date)
            {
                var query = _db.Bookings
                    .AsNoTracking()
                    .Where(b => b.ResourceId == resourceId);

                if (date.HasValue)
                {
                    var startDate = date.Value.Date;
                    var endDate = startDate.AddDays(1);
                    query = query.Where(b => b.BookingAt >= startDate && b.BookingAt < endDate);
                }

                var bookings = await query
                    .Select(b => new
                    {
                        b.Id,
                        b.UserId,
                        BookingAt = b.BookingAt,
                        EndAt = b.EndAt,
                        b.Reason,
                        b.Capacity,
                        b.Contact,
                        b.Status,
                        b.CreatedAt
                    })
                    .OrderBy(b => b.BookingAt)
                    .ToListAsync();

                return Ok(bookings);
            }

            // GET: api/bookings/my-bookings
            [HttpGet("my-bookings")]
            public async Task<ActionResult<IEnumerable<object>>> GetMyBookings([FromQuery] string? userId)
            {
                // For demo purposes, if no userId provided, return recent bookings
                // In a real app, get userId from authentication context
                var baseQuery = _db.Bookings
                    .AsNoTracking()
                    .Include(b => b.Resource)
                    .ThenInclude(r => r.Block)
                    .ThenInclude(bl => bl.Floor)
                    .ThenInclude(f => f.Building);

                IQueryable<Booking> query = baseQuery;
                if (!string.IsNullOrEmpty(userId) && int.TryParse(userId, out var userIdInt))
                {
                    query = baseQuery.Where(b => b.UserId == userIdInt);
                }

                var userBookings = await query
                    .OrderByDescending(b => b.CreatedAt)
                    .Take(10)
                    .Select(b => new
                    {
                        b.Id,
                        b.UserId,
                        b.ResourceId,
                        ResourceName = b.Resource.Name,
                        ResourceLocation = $"{b.Resource.Block.Floor.Building.Name} > {b.Resource.Block.Floor.Name} > {b.Resource.Block.Name}",
                        b.BookingAt,
                        b.EndAt,
                        b.Reason,
                        b.Capacity,
                        b.Contact,
                        b.CreatedAt,
                        b.Status,
                        b.RejectionReason
                    })
                    .ToListAsync();

                return Ok(userBookings);
            }

            // GET: api/bookings/pending (Admin only - get pending bookings for approval)
            [HttpGet("pending")]
            public async Task<ActionResult<IEnumerable<object>>> GetPendingBookings()
            {
                var pendingBookings = await _db.Bookings
                    .AsNoTracking()
                    .Where(b => b.Status == "Pending")
                    .Include(b => b.Resource)
                        .ThenInclude(r => r.Block)
                        .ThenInclude(bl => bl.Floor)
                        .ThenInclude(f => f.Building)
                    .Select(b => new
                    {
                        b.Id,
                        b.UserId,
                        UserName = "User " + b.UserId, // Can be enhanced with actual user lookup
                        b.ResourceId,
                        ResourceName = b.Resource.Name,
                        ResourceType = b.Resource.Type,
                        BuildingName = b.Resource.Block.Floor.Building.Name,
                        FloorName = b.Resource.Block.Floor.Name,
                        BlockName = b.Resource.Block.Name,
                        ResourceLocation = $"{b.Resource.Block.Floor.Building.Name} > {b.Resource.Block.Floor.Name} > {b.Resource.Block.Name} > {b.Resource.Name}",
                        b.BookingAt,
                        b.EndAt,
                        b.Reason,
                        b.Capacity,
                        b.Contact,
                        b.Status,
                        b.CreatedAt
                    })
                    .OrderBy(b => b.CreatedAt)
                    .ToListAsync();

                return Ok(pendingBookings);
            }

            // POST: api/bookings/approve/{id}
            [HttpPost("approve/{id:int}")]
            public async Task<ActionResult<object>> ApproveBooking(int id, ApprovalDto dto)
            {
                var booking = await _db.Bookings
                    .Include(b => b.Resource)
                    .FirstOrDefaultAsync(b => b.Id == id);

                if (booking == null)
                {
                    return NotFound(new { message = "Booking not found." });
                }

                if (booking.Status != "Pending")
                {
                    return BadRequest(new { message = "Only pending bookings can be approved." });
                }

                booking.Status = "Approved";
                booking.ApprovedBy = dto.ApproverEmail;
                booking.ApprovedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                return Ok(new
                {
                    message = "Booking approved successfully.",
                    bookingId = booking.Id,
                    status = booking.Status,
                    approvedAt = booking.ApprovedAt
                });
            }

            // POST: api/bookings/reject/{id}
            [HttpPost("reject/{id:int}")]
            public async Task<ActionResult<object>> RejectBooking(int id, RejectDto dto)
            {
                var booking = await _db.Bookings
                    .Include(b => b.Resource)
                    .FirstOrDefaultAsync(b => b.Id == id);

                if (booking == null)
                {
                    return NotFound(new { message = "Booking not found." });
                }

                if (booking.Status != "Pending")
                {
                    return BadRequest(new { message = "Only pending bookings can be rejected." });
                }

                booking.Status = "Rejected";
                booking.ApprovedBy = dto.ApproverEmail;
                booking.ApprovedAt = DateTime.UtcNow;
                booking.RejectionReason = dto.RejectionReason?.Trim() ?? "";

                await _db.SaveChangesAsync();

                return Ok(new
                {
                    message = "Booking rejected successfully.",
                    bookingId = booking.Id,
                    status = booking.Status,
                    rejectedAt = booking.ApprovedAt,
                    rejectionReason = booking.RejectionReason
                });
            }

            // GET: api/bookings/my-bookings/{userId}
            [HttpGet("my-bookings/{userId}")]
            public async Task<ActionResult<IEnumerable<object>>> GetUserBookings(string userId)
            {
                if (!int.TryParse(userId, out var userIdInt))
                {
                    return BadRequest(new { message = "Invalid user ID format." });
                }

                var userBookings = await _db.Bookings
                    .AsNoTracking()
                    .Where(b => b.UserId == userIdInt)
                    .Include(b => b.Resource)
                        .ThenInclude(r => r.Block)
                        .ThenInclude(bl => bl.Floor)
                        .ThenInclude(f => f.Building)
                    .Select(b => new
                    {
                        b.Id,
                        b.ResourceId,
                        ResourceName = b.Resource.Name,
                        ResourceType = b.Resource.Type,
                        Location = $"{b.Resource.Block.Floor.Building.Name} > {b.Resource.Block.Floor.Name} > {b.Resource.Block.Name}",
                        StartTime = b.BookingAt,
                        EndTime = b.EndAt,
                        b.Reason,
                        b.Capacity,
                        b.Contact,
                        b.Status,
                        b.ApprovedBy,
                        b.ApprovedAt,
                        b.RejectionReason,
                        b.CreatedAt
                    })
                    .OrderByDescending(b => b.CreatedAt)
                    .ToListAsync();

                return Ok(userBookings);
            }

            // GET: api/bookings/user (For current user - simplified endpoint)
            [HttpGet("user")]
            public async Task<ActionResult<IEnumerable<object>>> GetCurrentUserBookings([FromQuery] string? userId = null)
            {
                // In a real application, this would get userId from authentication context
                // For now, we'll use a query parameter or default to a test user
                var currentUserId = userId ?? "1";

                if (!int.TryParse(currentUserId, out var userIdInt))
                {
                    return BadRequest(new { message = "Invalid user ID format." });
                }

                var userBookings = await _db.Bookings
                    .AsNoTracking()
                    .Where(b => b.UserId == userIdInt)
                    .Include(b => b.Resource)
                        .ThenInclude(r => r.Block)
                        .ThenInclude(bl => bl.Floor)
                        .ThenInclude(f => f.Building)
                    .Select(b => new
                    {
                        b.Id,
                        b.ResourceId,
                        ResourceName = b.Resource.Name,
                        ResourceType = b.Resource.Type,
                        Location = $"{b.Resource.Block.Floor.Building.Name} > {b.Resource.Block.Floor.Name} > {b.Resource.Block.Name}",
                        StartTime = b.BookingAt,
                        EndTime = b.EndAt,
                        b.Reason,
                        b.Capacity,
                        b.Contact,
                        b.Status,
                        b.ApprovedBy,
                        b.ApprovedAt,
                        b.RejectionReason,
                        b.CreatedAt
                    })
                    .OrderByDescending(b => b.CreatedAt)
                    .ToListAsync();

                return Ok(userBookings);
            }

            public class ApprovalDto
            {
                public string ApproverEmail { get; set; } = string.Empty;
            }

            public class RejectDto
            {
                public string ApproverEmail { get; set; } = string.Empty;
                public string? RejectionReason { get; set; }
            }
        }
    }