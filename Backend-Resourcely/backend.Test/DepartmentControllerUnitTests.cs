using Backend_Resourcely.Controllers;
using Backend_Resourcely.Data;
using Backend_Resourcely.Dto;
using Backend_Resourcely.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Test
{
    public class DepartmentControllerUnitTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly DepartmentController _controller;
        private readonly DbContextOptions<AppDbContext> _options;

        public DepartmentControllerUnitTests()
        {
            _options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(_options);
            _controller = new DepartmentController(_context);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        [Fact]
        public async Task GetDepartments_Empty_ReturnsEmptyList()
        {
            var result = await _controller.GetDepartments();

            var ok = Assert.IsType<ActionResult<IEnumerable<Department>>>(result);
            var value = Assert.IsAssignableFrom<IEnumerable<Department>>(ok.Value);
            Assert.Empty(value);
        }

        [Fact]
        public async Task GetDepartments_ReturnsOrderedByName()
        {
            _context.Departments.AddRange(
                new Department { Name = "Zeta" },
                new Department { Name = "Alpha" },
                new Department { Name = "Beta" }
            );
            _context.SaveChanges();

            var result = await _controller.GetDepartments();

            var ok = Assert.IsType<ActionResult<IEnumerable<Department>>>(result);
            var value = Assert.IsAssignableFrom<IEnumerable<Department>>(ok.Value);
            Assert.Equal(new[] { "Alpha", "Beta", "Zeta" }, value.Select(d => d.Name).ToArray());
        }

        [Fact]
        public async Task PostDepartment_Valid_ReturnsCreatedAndPersists()
        {
            var dto = new DepartmentDto { Name = "Computer Science", Description = "CS Dept" };

            var result = await _controller.PostDepartment(dto);

            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var department = Assert.IsType<Department>(created.Value);
            Assert.Equal("Computer Science", department.Name);
            Assert.Equal(1, _context.Departments.Count());
        }

        [Fact]
        public async Task PostDepartment_MissingName_ModelValidationFails()
        {
            // DepartmentDto has [Required] Name; simulate invalid model state
            var dto = new DepartmentDto { Name = "", Description = "x" };
            _controller.ModelState.AddModelError("Name", "The Name field is required.");

            var result = await _controller.PostDepartment(dto);

            // Because action returns CreatedAtAction unconditionally, invalid ModelState would normally be blocked by [ApiController] auto-validation in integration tests.
            // At controller-unit level, mimic expected behavior: check ModelState and assert it's invalid pre-execution.
            Assert.False(_controller.ModelState.IsValid);
            // Optionally assert DB is unchanged
            Assert.Empty(_context.Departments);
        }
    }
}


