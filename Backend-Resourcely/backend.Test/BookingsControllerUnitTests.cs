using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend_Resourcely.Controllers;
using Backend_Resourcely.Data;
using Backend_Resourcely.Models;

namespace backend.Test
{
    public class BookingsControllerUnitTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly BookingsController _controller;
        private readonly DbContextOptions<AppDbContext> _options;

        public BookingsControllerUnitTests()
        {
            _options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(_options);
            _controller = new BookingsController(_context);

            // Seed test data
            SeedTestData();
        }

        private void SeedTestData()
        {
            // Add test building hierarchy
            var building = new Building
            {
                Id = 1,
                Name = "Main Building",
                Description = "Test building for unit tests"
            };

            var floor = new Floor
            {
                Id = 1,
                Name = "Ground Floor",
                FloorNumber = 0,
                BuildingId = 1,
                Building = building
            };

            var block = new Block
            {
                Id = 1,
                Name = "Block A",
                FloorId = 1,
                Floor = floor
            };

            var resource = new Resource
            {
                Id = 1,
                Name = "Conference Room A",
                Type = "Meeting Room",
                Description = "Large conference room",
                Capacity = 20,
                BlockId = 1,
                Block = block,
                IsActive = true
            };

            _context.Buildings.Add(building);
            _context.Floors.Add(floor);
            _context.Blocks.Add(block);
            _context.Resources.Add(resource);
            _context.SaveChanges();
        }

        [Fact]
        public async Task Create_WithValidData_ReturnsCreatedResult()
        {
            // Arrange
            var dto = new BookingsController.BookingCreateDto
            {
                ResourceId = 1,
                Date = "2024-12-25",
                Time = "14:30",
                EndTime = "15:30",
                Reason = "Team meeting",
                Capacity = 10,
                Contact = "john.doe@example.com",
                UserId = 1
            };

            // Act
            var result = await _controller.Create(dto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.NotNull(createdResult.Value);
            Assert.Equal(nameof(_controller.GetById), createdResult.ActionName);
        }

        [Fact]
        public async Task Create_WithMissingResourceId_ReturnsBadRequest()
        {
            // Arrange
            var dto = new BookingsController.BookingCreateDto
            {
                ResourceId = 0, // Invalid resource ID
                Date = "2024-12-25",
                Time = "14:30",
                EndTime = "15:30",
                Reason = "Team meeting",
                Capacity = 10,
                Contact = "john.doe@example.com",
                UserId = 1
            };

            // Act
            var result = await _controller.Create(dto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public async Task Create_WithEmptyDate_ReturnsBadRequest()
        {
            // Arrange
            var dto = new BookingsController.BookingCreateDto
            {
                ResourceId = 1,
                Date = "", // Empty date
                Time = "14:30",
                EndTime = "15:30",
                Reason = "Team meeting",
                Capacity = 10,
                Contact = "john.doe@example.com",
                UserId = 1
            };

            // Act
            var result = await _controller.Create(dto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public async Task Create_WithInvalidCapacity_ReturnsBadRequest()
        {
            // Arrange
            var dto = new BookingsController.BookingCreateDto
            {
                ResourceId = 1,
                Date = "2024-12-25",
                Time = "14:30",
                EndTime = "15:30",
                Reason = "Team meeting",
                Capacity = -1, // Invalid capacity
                Contact = "john.doe@example.com",
                UserId = 1
            };

            // Act
            var result = await _controller.Create(dto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(badRequestResult.Value);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}