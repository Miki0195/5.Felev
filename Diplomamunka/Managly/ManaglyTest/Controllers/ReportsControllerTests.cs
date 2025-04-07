using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Managly.Controllers;
using Managly.Data;
using Managly.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ManaglyTest.Controllers
{
    public class ReportsControllerTests : IDisposable
    {
        private readonly Mock<UserManager<User>> _mockUserManager;
        private readonly ApplicationDbContext _context;
        private readonly ReportsController _controller;

        public ReportsControllerTests()
        {
            // Setup mock UserManager
            var store = new Mock<IUserStore<User>>();
            _mockUserManager = new Mock<UserManager<User>>(store.Object, null, null, null, null, null, null, null, null);
            
            // Setup in-memory database
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;
            _context = new ApplicationDbContext(options);

            _controller = new ReportsController(_context, _mockUserManager.Object);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task GetProjects_ReturnsProjectsForCurrentUserCompany()
        {
            // Arrange
            var currentUser = new User 
            { 
                Id = "1", 
                CompanyId = 1,
                Name = "Test",
                LastName = "User",
                Country = "Test",
                City = "Test",
                Address = "Test"
            };
            var projects = new List<Project>
            {
                new Project 
                { 
                    Id = 1, 
                    Name = "Project 1", 
                    Description = "Test Description",
                    CompanyId = 1,
                    CreatedById = "1",
                    StartDate = DateTime.UtcNow
                },
                new Project 
                { 
                    Id = 2, 
                    Name = "Project 2", 
                    Description = "Test Description",
                    CompanyId = 1,
                    CreatedById = "1",
                    StartDate = DateTime.UtcNow
                }
            };

            _mockUserManager.Setup(x => x.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync(currentUser);

            _context.Users.Add(currentUser);
            _context.Projects.AddRange(projects);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetProjects();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedProjects = Assert.IsAssignableFrom<IEnumerable<object>>(okResult.Value);
            Assert.Equal(2, returnedProjects.Count());
        }

        [Fact]
        public async Task GetProjectMetrics_WithFilters_ReturnsFilteredMetrics()
        {
            // Arrange
            var currentUser = new User 
            { 
                Id = "2", 
                CompanyId = 1,
                Name = "Test",
                LastName = "User",
                Country = "Test",
                City = "Test",
                Address = "Test"
            };
            var startDate = DateTime.UtcNow.AddDays(-7);
            var endDate = DateTime.UtcNow;
            var projectId = 1;
            var status = "In Progress";
            var priority = "High";
            var teamMemberId = "2";

            _mockUserManager.Setup(x => x.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync(currentUser);

            var project = new Project 
            { 
                Id = 1, 
                Name = "Project 1", 
                Description = "Test Description",
                CompanyId = 1,
                Status = "In Progress",
                Priority = "High",
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                CreatedById = "2",
                StartDate = DateTime.UtcNow.AddDays(-5)
            };

            var tasks = new List<Tasks>
            {
                new Tasks 
                { 
                    Id = 1, 
                    Status = "Completed", 
                    ProjectId = 1, 
                    TaskTitle = "Task 1", 
                    CreatedById = "2",
                    Description = "Test Description"
                },
                new Tasks 
                { 
                    Id = 2, 
                    Status = "In Progress", 
                    DueDate = DateTime.UtcNow.AddDays(1), 
                    ProjectId = 1, 
                    TaskTitle = "Task 2", 
                    CreatedById = "2",
                    Description = "Test Description"
                }
            };

            var projectMember = new ProjectMember { UserId = "2", ProjectId = 1 };

            _context.Users.Add(currentUser);
            _context.Projects.Add(project);
            _context.Tasks.AddRange(tasks);
            _context.ProjectMembers.Add(projectMember);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetProjectMetrics(startDate, endDate, projectId, status, priority, teamMemberId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var metrics = Assert.IsAssignableFrom<IEnumerable<object>>(okResult.Value);
            Assert.Single(metrics);
        }

        [Fact]
        public async Task GetUserProductivity_ReturnsCorrectProductivityData()
        {
            // Arrange
            var currentUser = new User 
            { 
                Id = "3", 
                CompanyId = 1,
                Name = "Test",
                LastName = "User",
                Country = "Test",
                City = "Test",
                Address = "Test"
            };
            var startDate = DateTime.UtcNow.AddDays(-7);
            var endDate = DateTime.UtcNow;
            var projectId = 1;

            _mockUserManager.Setup(x => x.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync(currentUser);

            var user = new User 
            { 
                Id = "3", 
                Name = "John", 
                LastName = "Doe", 
                CompanyId = 1,
                Country = "Test",
                City = "Test",
                Address = "Test"
            };

            var project = new Project 
            { 
                Id = 1, 
                Name = "Test Project",
                Description = "Test Description",
                CompanyId = 1,
                CreatedById = "3",
                StartDate = startDate
            };

            var task = new Tasks 
            { 
                Id = 1, 
                Status = "Completed", 
                ProjectId = 1,
                TaskTitle = "Task 1",
                CreatedById = "3",
                Description = "Test Description",
                CompletedAt = startDate.AddDays(1)
            };

            var projectMember = new ProjectMember 
            { 
                UserId = "3", 
                ProjectId = 1 
            };

            var taskAssignment = new TaskAssignment 
            { 
                UserId = "3", 
                TaskId = 1,
                AssignedAt = startDate
            };

            var attendance = new Attendance 
            { 
                UserId = "3", 
                CheckInTime = startDate.AddDays(1),
                CheckOutTime = startDate.AddDays(1).AddHours(8)
            };

            _context.Users.Add(user);
            _context.Projects.Add(project);
            _context.Tasks.Add(task);
            _context.ProjectMembers.Add(projectMember);
            _context.TaskAssignments.Add(taskAssignment);
            _context.Attendances.Add(attendance);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetUserProductivity(startDate, endDate, projectId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var productivityData = Assert.IsAssignableFrom<IEnumerable<object>>(okResult.Value);
            Assert.Single(productivityData);
        }

        [Fact]
        public async Task GetTaskDistribution_ReturnsCorrectDistribution()
        {
            // Arrange
            var currentUser = new User 
            { 
                Id = "4", 
                CompanyId = 1,
                Name = "Test",
                LastName = "User",
                Country = "Test",
                City = "Test",
                Address = "Test"
            };
            var projectId = 1;

            _mockUserManager.Setup(x => x.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync(currentUser);

            var project = new Project 
            { 
                Id = 1, 
                CompanyId = 1,
                Name = "Test Project",
                Description = "Test Description",
                CreatedById = "4",
                StartDate = DateTime.UtcNow
            };
            var tasks = new List<Tasks>
            {
                new Tasks 
                { 
                    Id = 1, 
                    Status = "Completed", 
                    ProjectId = 1, 
                    TaskTitle = "Task 1", 
                    CreatedById = "4",
                    Description = "Test Description"
                },
                new Tasks 
                { 
                    Id = 2, 
                    Status = "In Progress", 
                    ProjectId = 1, 
                    TaskTitle = "Task 2", 
                    CreatedById = "4",
                    Description = "Test Description"
                }
            };

            _context.Users.Add(currentUser);
            _context.Projects.Add(project);
            _context.Tasks.AddRange(tasks);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetTaskDistribution(projectId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var distribution = Assert.IsAssignableFrom<IEnumerable<object>>(okResult.Value);
            Assert.Equal(2, distribution.Count());
        }

        [Fact]
        public async Task GetTaskDistributionByPriority_ReturnsCorrectDistribution()
        {
            // Arrange
            var currentUser = new User 
            { 
                Id = "5", 
                CompanyId = 1,
                Name = "Test",
                LastName = "User",
                Country = "Test",
                City = "Test",
                Address = "Test"
            };
            var projectId = 1;

            _mockUserManager.Setup(x => x.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync(currentUser);

            var project = new Project 
            { 
                Id = 1, 
                CompanyId = 1,
                Name = "Test Project",
                Description = "Test Description",
                CreatedById = "5",
                StartDate = DateTime.UtcNow
            };
            var tasks = new List<Tasks>
            {
                new Tasks 
                { 
                    Id = 1, 
                    Priority = "High", 
                    ProjectId = 1, 
                    TaskTitle = "Task 1", 
                    CreatedById = "5",
                    Description = "Test Description"
                },
                new Tasks 
                { 
                    Id = 2, 
                    Priority = "Medium", 
                    ProjectId = 1, 
                    TaskTitle = "Task 2", 
                    CreatedById = "5",
                    Description = "Test Description"
                }
            };

            _context.Users.Add(currentUser);
            _context.Projects.Add(project);
            _context.Tasks.AddRange(tasks);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetTaskDistributionByPriority(projectId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var distribution = Assert.IsAssignableFrom<IEnumerable<object>>(okResult.Value);
            Assert.Equal(2, distribution.Count());
        }
    }
} 