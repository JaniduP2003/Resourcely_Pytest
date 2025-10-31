using Backend_Resourcely.Controllers;
using Backend_Resourcely.Data;
using Backend_Resourcely.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Test
{
    public class AdminControllerUnitTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly AdminController _controller;
        private readonly DbContextOptions<AppDbContext> _options;

        public AdminControllerUnitTests()
        {
            _options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(_options);
            _controller = new AdminController(_context);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private (Building building, Floor floor, Block block, Resource resource) SeedBasicHierarchy(bool resourceActive = true)
        {
            var building = new Building { Name = "B1", Description = "Desc", CreatedAt = DateTime.UtcNow };
            _context.Buildings.Add(building);
            _context.SaveChanges();

            var floor = new Floor { Name = "F1", Description = "", BuildingId = building.Id, FloorNumber = 0, IsActive = true, CreatedAt = DateTime.UtcNow };
            _context.Floors.Add(floor);
            _context.SaveChanges();

            var block = new Block { Name = "BL1", Description = "", FloorId = floor.Id, IsActive = true, CreatedAt = DateTime.UtcNow };
            _context.Blocks.Add(block);
            _context.SaveChanges();

            var resource = new Resource { Name = "R1", Type = "Room", Description = "", Capacity = 10, BlockId = block.Id, IsActive = resourceActive, CreatedAt = DateTime.UtcNow };
            _context.Resources.Add(resource);
            _context.SaveChanges();

            return (building, floor, block, resource);
        }

        [Fact]
        public async Task CreateBuilding_WithValidData_ReturnsOkAndPersists()
        {
            var req = new AdminController.CreateBuildingRequest { Name = "HQ", Description = "Head Office" };

            var result = await _controller.CreateBuilding(req);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.NotNull(ok.Value);
            Assert.Equal(1, _context.Buildings.Count());
            Assert.Equal("HQ", _context.Buildings.Single().Name);
        }

        [Fact]
        public async Task CreateBuilding_MissingName_ReturnsBadRequest()
        {
            var req = new AdminController.CreateBuildingRequest { Name = "  ", Description = "x" };

            var result = await _controller.CreateBuilding(req);

            var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(bad.Value);
            Assert.Empty(_context.Buildings);
        }

        [Fact]
        public async Task CreateFloor_BuildingNotExists_ReturnsBadRequest()
        {
            var req = new AdminController.CreateFloorRequest { Name = "F1", BuildingId = 999 };

            var result = await _controller.CreateFloor(req);

            var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(bad.Value);
        }

        [Fact]
        public async Task CreateFloor_WithValidData_AssignsIncrementalFloorNumber()
        {
            var (building, _, _, _) = SeedBasicHierarchy();

            // Add an existing floor with number 0 under same building
            var existing = new Floor { Name = "Existing", BuildingId = building.Id, FloorNumber = 0, IsActive = true, CreatedAt = DateTime.UtcNow };
            _context.Floors.Add(existing);
            _context.SaveChanges();

            var req = new AdminController.CreateFloorRequest { Name = "NewFloor", BuildingId = building.Id };

            var result = await _controller.CreateFloor(req);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.NotNull(ok.Value);

            var created = _context.Floors.OrderByDescending(f => f.Id).First();
            Assert.Equal(1, created.FloorNumber);
            Assert.Equal("NewFloor", created.Name);
        }

        [Fact]
        public async Task ToggleResource_NotFound_ReturnsNotFound()
        {
            var result = await _controller.ToggleResource(12345);

            var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.NotNull(notFound.Value);
        }

        [Fact]
        public async Task ToggleResource_TogglesIsActive()
        {
            var (_, _, _, resource) = SeedBasicHierarchy(resourceActive: true);

            var result1 = await _controller.ToggleResource(resource.Id);
            var ok1 = Assert.IsType<OkObjectResult>(result1.Result);
            Assert.NotNull(ok1.Value);
            Assert.False(_context.Resources.Find(resource.Id)!.IsActive);

            var result2 = await _controller.ToggleResource(resource.Id);
            var ok2 = Assert.IsType<OkObjectResult>(result2.Result);
            Assert.NotNull(ok2.Value);
            Assert.True(_context.Resources.Find(resource.Id)!.IsActive);
        }

        [Fact]
        public async Task ApproveBooking_NotFound_ReturnsNotFound()
        {
            var result = await _controller.ApproveBooking(999);

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.NotNull(notFound.Value);
        }

        [Fact]
        public async Task ApproveBooking_NonPending_ReturnsBadRequest()
        {
            var (_, _, _, resource) = SeedBasicHierarchy();
            var booking = new Booking
            {
                ResourceId = resource.Id,
                UserId = 1,
                BookingAt = DateTime.UtcNow.AddHours(1),
                EndAt = DateTime.UtcNow.AddHours(2),
                Reason = "r",
                Capacity = 1,
                Contact = "c",
                Status = "Approved",
                CreatedAt = DateTime.UtcNow
            };
            _context.Bookings.Add(booking);
            _context.SaveChanges();

            var result = await _controller.ApproveBooking(booking.Id);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(bad.Value);
        }

        [Fact]
        public async Task ApproveBooking_Pending_SetsApproved()
        {
            var (_, _, _, resource) = SeedBasicHierarchy();
            var booking = new Booking
            {
                ResourceId = resource.Id,
                UserId = 1,
                BookingAt = DateTime.UtcNow.AddHours(1),
                EndAt = DateTime.UtcNow.AddHours(2),
                Reason = "r",
                Capacity = 1,
                Contact = "c",
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };
            _context.Bookings.Add(booking);
            _context.SaveChanges();

            var result = await _controller.ApproveBooking(booking.Id);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
            var updated = _context.Bookings.Find(booking.Id)!;
            Assert.Equal("Approved", updated.Status);
            Assert.NotNull(updated.ApprovedAt);
            Assert.Equal("Admin", updated.ApprovedBy);
        }

        [Fact]
        public async Task RejectBooking_RequiresReason_ReturnsBadRequest()
        {
            var (_, _, _, resource) = SeedBasicHierarchy();
            var booking = new Booking
            {
                ResourceId = resource.Id,
                UserId = 1,
                BookingAt = DateTime.UtcNow.AddHours(1),
                EndAt = DateTime.UtcNow.AddHours(2),
                Reason = "r",
                Capacity = 1,
                Contact = "c",
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };
            _context.Bookings.Add(booking);
            _context.SaveChanges();

            var req = new AdminController.RejectBookingRequest { Reason = "  " };
            var result = await _controller.RejectBooking(booking.Id, req);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(bad.Value);
        }

        [Fact]
        public async Task RejectBooking_Pending_SetsRejectedAndReason()
        {
            var (_, _, _, resource) = SeedBasicHierarchy();
            var booking = new Booking
            {
                ResourceId = resource.Id,
                UserId = 1,
                BookingAt = DateTime.UtcNow.AddHours(1),
                EndAt = DateTime.UtcNow.AddHours(2),
                Reason = "r",
                Capacity = 1,
                Contact = "c",
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };
            _context.Bookings.Add(booking);
            _context.SaveChanges();

            var req = new AdminController.RejectBookingRequest { Reason = "No availability" };
            var result = await _controller.RejectBooking(booking.Id, req);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
            var updated = _context.Bookings.Find(booking.Id)!;
            Assert.Equal("Rejected", updated.Status);
            Assert.Equal("No availability", updated.RejectionReason);
        }

        [Fact]
        public async Task CreateApprovedBooking_InvalidResource_ReturnsBadRequest()
        {
            // No resource seeded
            // Seed a valid user
            var user = new User { Email = "u@example.com", Username = "u", PasswordHash = "h", PasswordSalt = "s", Role = "User", CreatedAt = DateTime.UtcNow };
            _context.Users.Add(user);
            _context.SaveChanges();

            var req = new AdminController.CreateApprovedBookingRequest
            {
                ResourceId = 999,
                UserId = user.Id,
                BookingAt = DateTime.UtcNow.AddHours(1),
                EndAt = DateTime.UtcNow.AddHours(2),
                Reason = "r",
                Capacity = 1,
                Contact = "c"
            };

            var result = await _controller.CreateApprovedBooking(req);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(bad.Value);
        }

        [Fact]
        public async Task CreateApprovedBooking_InvalidUser_ReturnsBadRequest()
        {
            var (_, _, _, resource) = SeedBasicHierarchy();

            var req = new AdminController.CreateApprovedBookingRequest
            {
                ResourceId = resource.Id,
                UserId = 999,
                BookingAt = DateTime.UtcNow.AddHours(1),
                EndAt = DateTime.UtcNow.AddHours(2),
                Reason = "r",
                Capacity = 1,
                Contact = "c"
            };

            var result = await _controller.CreateApprovedBooking(req);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(bad.Value);
        }

        [Fact]
        public async Task CreateApprovedBooking_Valid_CreatesApprovedBooking()
        {
            var (_, _, _, resource) = SeedBasicHierarchy();
            var user = new User { Email = "u@example.com", Username = "u", PasswordHash = "h", PasswordSalt = "s", Role = "User", CreatedAt = DateTime.UtcNow };
            _context.Users.Add(user);
            _context.SaveChanges();

            var req = new AdminController.CreateApprovedBookingRequest
            {
                ResourceId = resource.Id,
                UserId = user.Id,
                BookingAt = DateTime.UtcNow.AddHours(1),
                EndAt = DateTime.UtcNow.AddHours(2),
                Reason = "Meeting",
                Capacity = 2,
                Contact = "c"
            };

            var result = await _controller.CreateApprovedBooking(req);

            var created = Assert.IsType<CreatedResult>(result);
            Assert.NotNull(created.Value);
            var saved = _context.Bookings.Single();
            Assert.Equal("Approved", saved.Status);
            Assert.Equal(user.Id, saved.UserId);
            Assert.Equal(resource.Id, saved.ResourceId);
        }
    }
}


