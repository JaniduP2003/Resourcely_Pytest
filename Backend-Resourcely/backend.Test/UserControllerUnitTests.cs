using Backend_Resourcely.Controllers;
using Backend_Resourcely.Data;
using Backend_Resourcely.Dto;
using Backend_Resourcely.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Test
{
    public class UserControllerUnitTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly UserController _controller;
        private readonly DbContextOptions<AppDbContext> _options;

        public UserControllerUnitTests()
        {
            _options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(_options);
            _controller = new UserController(_context);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private void SeedUsers()
        {
            _context.Users.AddRange(
                new User { Email = "a@example.com", Username = "alice", Role = "user", PasswordHash = "h", PasswordSalt = "s", CreatedAt = DateTime.UtcNow },
                new User { Email = "b@example.com", Username = "bob", Role = "student", PasswordHash = "h", PasswordSalt = "s", CreatedAt = DateTime.UtcNow },
                new User { Email = "c@example.com", Username = "charlie", Role = "lecturer", PasswordHash = "h", PasswordSalt = "s", CreatedAt = DateTime.UtcNow },
                new User { Email = "admin@example.com", Username = "admin", Role = "admin", PasswordHash = "h", PasswordSalt = "s", CreatedAt = DateTime.UtcNow }
            );
            _context.SaveChanges();
        }

        [Fact]
        public async Task GetAllUsers_ExcludesAdmins_OrdersByUsername()
        {
            SeedUsers();

            var result = await _controller.GetAllUsers();

            var ok = Assert.IsType<ActionResult<IEnumerable<object>>>(result);
            var okResult = Assert.IsType<OkObjectResult>(ok.Result);
            var list = Assert.IsAssignableFrom<IEnumerable<object>>(okResult.Value);
            var names = list.Select(x => (string)x!.GetType().GetProperty("Username")!.GetValue(x, null)!).ToArray();
            Assert.Equal(new[] { "alice", "bob", "charlie" }, names);
        }

        [Fact]
        public async Task GetAllRoleUser_ReturnsOnlyRoleUser()
        {
            SeedUsers();

            var result = await _controller.GetAllRoleUser();

            var ok = Assert.IsType<ActionResult<IEnumerable<object>>>(result);
            var okResult = Assert.IsType<OkObjectResult>(ok.Result);
            var list = Assert.IsAssignableFrom<IEnumerable<object>>(okResult.Value);
            var roles = list.Select(x => (string)x!.GetType().GetProperty("Role")!.GetValue(x, null)!).Distinct().ToArray();
            Assert.Equal(new[] { "user" }, roles);
        }

        [Fact]
        public async Task GetAllStudents_ProjectsBatch()
        {
            SeedUsers();
            var student = _context.Users.Single(u => u.Role == "student");
            var batch = new Batch { Name = "Y1S1", Code = "B-101", StartDate = DateTime.UtcNow.Date, EndDate = DateTime.UtcNow.Date.AddMonths(3), IsActive = true };
            _context.Batches.Add(batch);
            _context.SaveChanges();
            _context.StudentProfiles.Add(new StudentProfile { UserId = student.Id, BatchId = batch.Id });
            _context.SaveChanges();

            var result = await _controller.GetAllStudents();

            var ok = Assert.IsType<ActionResult<IEnumerable<object>>>(result);
            var okResult = Assert.IsType<OkObjectResult>(ok.Result);
            var list = Assert.IsAssignableFrom<IEnumerable<object>>(okResult.Value);
            var first = list.First();
            var batchProp = first.GetType().GetProperty("Batch")!.GetValue(first, null);
            Assert.NotNull(batchProp);
            var batchName = (string)batchProp!.GetType().GetProperty("BatchName")!.GetValue(batchProp, null)!;
            Assert.Equal("Y1S1", batchName);
        }

        [Fact]
        public async Task GetAllLecturers_ProjectsDepartment()
        {
            SeedUsers();
            var lecturer = _context.Users.Single(u => u.Role == "lecturer");
            var dept = new Department { Name = "CS" };
            _context.Departments.Add(dept);
            _context.SaveChanges();
            _context.LecturerProfiles.Add(new LecturerProfile { UserId = lecturer.Id, DepartmentId = dept.Id });
            _context.SaveChanges();

            var result = await _controller.GetAllLecturers();

            var ok = Assert.IsType<ActionResult<IEnumerable<object>>>(result);
            var okResult = Assert.IsType<OkObjectResult>(ok.Result);
            var list = Assert.IsAssignableFrom<IEnumerable<object>>(okResult.Value);
            var first = list.First();
            var department = first.GetType().GetProperty("Department")!.GetValue(first, null);
            Assert.NotNull(department);
            var depName = (string)department!.GetType().GetProperty("DepartmentName")!.GetValue(department, null)!;
            Assert.Equal("CS", depName);
        }

        [Fact]
        public async Task UpdateUserRole_UserNotFound_ReturnsNotFound()
        {
            var result = await _controller.UpdateUserRole(999, new UpdateUserRoleDto { Role = "student" });
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.NotNull(notFound.Value);
        }

        [Fact]
        public async Task UpdateUserRole_AdminRejected_ReturnsBadRequest()
        {
            SeedUsers();
            var admin = _context.Users.Single(u => u.Role == "admin");
            var result = await _controller.UpdateUserRole(admin.Id, new UpdateUserRoleDto { Role = "user" });
            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(bad.Value);
        }

        [Fact]
        public async Task UpdateUserRole_Valid_UpdatesRole()
        {
            SeedUsers();
            var user = _context.Users.Single(u => u.Role == "user");
            var result = await _controller.UpdateUserRole(user.Id, new UpdateUserRoleDto { Role = "student" });
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
            Assert.Equal("student", _context.Users.Find(user.Id)!.Role);
        }

        [Fact]
        public async Task AssignUserToBatch_OnlyStudentsAllowed_AndBatchMustBeActive()
        {
            SeedUsers();
            var student = _context.Users.Single(u => u.Role == "student");
            var batch = new Batch { Name = "Y1S1", Code = "B-101", StartDate = DateTime.UtcNow.Date, EndDate = DateTime.UtcNow.Date.AddMonths(3), IsActive = true };
            _context.Batches.Add(batch);
            _context.SaveChanges();

            var ok = await _controller.AssignUserToBatch(student.Id, new AssignUserToBatchDto { BatchId = batch.Id });
            var okResult = Assert.IsType<OkObjectResult>(ok);
            Assert.NotNull(okResult.Value);
            Assert.Equal(batch.Id, _context.StudentProfiles.Find(student.Id)!.BatchId);

            var user = _context.Users.Single(u => u.Role == "user");
            var bad1 = await _controller.AssignUserToBatch(user.Id, new AssignUserToBatchDto { BatchId = batch.Id });
            Assert.IsType<BadRequestObjectResult>(bad1);

            var admin = _context.Users.Single(u => u.Role == "admin");
            var bad2 = await _controller.AssignUserToBatch(admin.Id, new AssignUserToBatchDto { BatchId = batch.Id });
            Assert.IsType<BadRequestObjectResult>(bad2);

            var inactive = new Batch { Name = "Old", Code = "B-000", StartDate = DateTime.UtcNow.Date.AddYears(-1), EndDate = DateTime.UtcNow.Date.AddMonths(-9), IsActive = false };
            _context.Batches.Add(inactive);
            _context.SaveChanges();
            var nf = await _controller.AssignUserToBatch(student.Id, new AssignUserToBatchDto { BatchId = inactive.Id });
            Assert.IsType<NotFoundObjectResult>(nf);
        }

        [Fact]
        public async Task RemoveUserBatch_OnlyStudentsAllowed_AndMustExist()
        {
            SeedUsers();
            var student = _context.Users.Single(u => u.Role == "student");
            var batch = new Batch { Name = "Y1S2", Code = "B-102", StartDate = DateTime.UtcNow.Date, EndDate = DateTime.UtcNow.Date.AddMonths(3), IsActive = true };
            _context.Batches.Add(batch);
            _context.SaveChanges();
            _context.StudentProfiles.Add(new StudentProfile { UserId = student.Id, BatchId = batch.Id });
            _context.SaveChanges();

            var ok = await _controller.RemoveUserBatch(student.Id);
            Assert.IsType<OkObjectResult>(ok);
            Assert.Null(_context.StudentProfiles.Find(student.Id));

            var user = _context.Users.Single(u => u.Role == "user");
            Assert.IsType<BadRequestObjectResult>(await _controller.RemoveUserBatch(user.Id));

            var lecturer = _context.Users.Single(u => u.Role == "lecturer");
            Assert.IsType<BadRequestObjectResult>(await _controller.RemoveUserBatch(lecturer.Id));

            var nfUserId = 9999;
            Assert.IsType<NotFoundObjectResult>(await _controller.RemoveUserBatch(nfUserId));
        }

        [Fact]
        public async Task AssignLecturerToDepartment_OnlyLecturersAndExistingDepartment()
        {
            SeedUsers();
            var lecturer = _context.Users.Single(u => u.Role == "lecturer");
            var dept = new Department { Name = "Math" };
            _context.Departments.Add(dept);
            _context.SaveChanges();

            var ok = await _controller.AssignLecturerToDepartment(lecturer.Id, new AssignDepartmentDto { DepartmentId = dept.Id });
            Assert.IsType<OkObjectResult>(ok);
            Assert.Equal(dept.Id, _context.LecturerProfiles.Find(lecturer.Id)!.DepartmentId);

            var user = _context.Users.Single(u => u.Role == "user");
            Assert.IsType<BadRequestObjectResult>(await _controller.AssignLecturerToDepartment(user.Id, new AssignDepartmentDto { DepartmentId = dept.Id }));

            Assert.IsType<NotFoundObjectResult>(await _controller.AssignLecturerToDepartment(lecturer.Id, new AssignDepartmentDto { DepartmentId = 999 }));
        }

        [Fact]
        public async Task RemoveLecturerDepartment_OnlyLecturersAndMustExist()
        {
            SeedUsers();
            var lecturer = _context.Users.Single(u => u.Role == "lecturer");
            var dept = new Department { Name = "Physics" };
            _context.Departments.Add(dept);
            _context.SaveChanges();
            _context.LecturerProfiles.Add(new LecturerProfile { UserId = lecturer.Id, DepartmentId = dept.Id });
            _context.SaveChanges();

            var ok = await _controller.RemoveLecturerDepartment(lecturer.Id);
            Assert.IsType<OkObjectResult>(ok);
            Assert.Null(_context.LecturerProfiles.Find(lecturer.Id));

            var user = _context.Users.Single(u => u.Role == "user");
            Assert.IsType<BadRequestObjectResult>(await _controller.RemoveLecturerDepartment(user.Id));

            Assert.IsType<NotFoundObjectResult>(await _controller.RemoveLecturerDepartment(9999));
        }

        [Fact]
        public async Task GetUserStats_And_GetRecentBookings_WorkWithoutAuth()
        {
            // Seed resource/building structure
            var building = new Building { Name = "Main" };
            _context.Buildings.Add(building);
            _context.SaveChanges();
            var floor = new Floor { Name = "G", BuildingId = building.Id, FloorNumber = 0, IsActive = true, CreatedAt = DateTime.UtcNow };
            _context.Floors.Add(floor);
            _context.SaveChanges();
            var block = new Block { Name = "A", FloorId = floor.Id, IsActive = true, CreatedAt = DateTime.UtcNow };
            _context.Blocks.Add(block);
            _context.SaveChanges();
            var resource = new Resource { Name = "R1", Type = "Room", Capacity = 5, BlockId = block.Id, IsActive = true, CreatedAt = DateTime.UtcNow };
            _context.Resources.Add(resource);
            _context.SaveChanges();

            // Seed bookings
            _context.Bookings.AddRange(
                new Booking { ResourceId = resource.Id, UserId = 1, BookingAt = DateTime.UtcNow.AddHours(1), EndAt = DateTime.UtcNow.AddHours(2), Reason = "r", Capacity = 1, Contact = "c", Status = "Approved", CreatedAt = DateTime.UtcNow },
                new Booking { ResourceId = resource.Id, UserId = 1, BookingAt = DateTime.UtcNow.AddHours(-2), EndAt = DateTime.UtcNow.AddHours(-1), Reason = "r", Capacity = 1, Contact = "c", Status = "Approved", CreatedAt = DateTime.UtcNow }
            );
            _context.SaveChanges();

            var stats = await _controller.GetUserStats();
            var statsOk = Assert.IsType<ActionResult<object>>(stats);
            Assert.IsType<OkObjectResult>(statsOk.Result);

            var recent = await _controller.GetRecentBookings();
            var recentOk = Assert.IsType<ActionResult<IEnumerable<object>>>(recent);
            Assert.IsType<OkObjectResult>(recentOk.Result);
        }
    }
}


