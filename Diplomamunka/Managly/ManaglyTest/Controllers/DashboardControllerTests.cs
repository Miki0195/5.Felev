using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Managly.Controllers;
using Managly.Data;
using Managly.Models;
using Managly.Models.Enums;
using Moq;
using Xunit;

namespace ManaglyTest.Controllers
{
    public class DashboardControllerTests
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<UserManager<User>> _mockUserManager;
        private readonly string _userId = "test-user-id";
        
        public DashboardControllerTests()
        {
            // Set up in-memory database for testing
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            _context = new ApplicationDbContext(options);
            
            // Set up test user
            var user = new User
            {
                Id = _userId,
                UserName = "testuser@example.com",
                Email = "testuser@example.com",
                Name = "Test",
                LastName = "User",
                CompanyId = 1,
                Country = "Test Country",
                City = "Test City",
                Address = "Test Address"
            };
            _context.Users.Add(user);

            // Set up test projects
            var projects = new List<Project>
            {
                new Project
                {
                    Id = 1,
                    Name = "Test Project 1",
                    Description = "Description 1",
                    StartDate = DateTime.UtcNow.AddDays(-30),
                    Deadline = DateTime.UtcNow.AddDays(30),
                    CompanyId = 1,
                    CreatedById = _userId,
                    TotalTasks = 5,
                    CompletedTasks = 2,
                    CreatedAt = DateTime.UtcNow.AddDays(-30)
                },
                new Project
                {
                    Id = 2,
                    Name = "Test Project 2",
                    Description = "Description 2",
                    StartDate = DateTime.UtcNow.AddDays(-15),
                    CompanyId = 1,
                    CreatedById = _userId,
                    TotalTasks = 3,
                    CompletedTasks = 1,
                    CreatedAt = DateTime.UtcNow.AddDays(-15)
                }
            };
            _context.Projects.AddRange(projects);

            // Set up project members
            var projectMembers = new List<ProjectMember>
            {
                new ProjectMember { ProjectId = 1, UserId = _userId, Role = "Project Lead" },
                new ProjectMember { ProjectId = 2, UserId = _userId, Role = "Member" }
            };
            _context.ProjectMembers.AddRange(projectMembers);

            // Set up tasks
            var tasks = new List<Tasks>
            {
                new Tasks
                {
                    Id = 1,
                    ProjectId = 1,
                    TaskTitle = "Task 1",
                    Description = "Task 1 Description",
                    DueDate = DateTime.UtcNow.AddDays(5),
                    Status = "Pending",
                    Priority = "High",
                    CreatedById = _userId
                },
                new Tasks
                {
                    Id = 2,
                    ProjectId = 1,
                    TaskTitle = "Task 2",
                    Description = "Task 2 Description",
                    DueDate = DateTime.UtcNow.AddDays(10),
                    Status = "Pending",
                    Priority = "Medium",
                    CreatedById = _userId
                },
                new Tasks
                {
                    Id = 3,
                    ProjectId = 2,
                    TaskTitle = "Task 3",
                    Description = "Task 3 Description",
                    DueDate = DateTime.UtcNow.AddDays(7),
                    Status = "Completed",
                    Priority = "Low",
                    CreatedById = _userId,
                    CompletedAt = DateTime.UtcNow.AddDays(-1)
                }
            };
            _context.Tasks.AddRange(tasks);

            // Set up task assignments
            var taskAssignments = new List<TaskAssignment>
            {
                new TaskAssignment { TaskId = 1, UserId = _userId },
                new TaskAssignment { TaskId = 2, UserId = _userId },
                new TaskAssignment { TaskId = 3, UserId = _userId }
            };
            _context.TaskAssignments.AddRange(taskAssignments);

            // Set up messages
            var messages = new List<Message>
            {
                new Message
                {
                    SenderId = _userId,
                    ReceiverId = "other-user-1",
                    Content = "Hello",
                    Timestamp = DateTime.UtcNow.AddHours(-2)
                },
                new Message
                {
                    SenderId = "other-user-1",
                    ReceiverId = _userId,
                    Content = "Hi there",
                    Timestamp = DateTime.UtcNow.AddHours(-1)
                }
            };
            _context.Messages.AddRange(messages);

            // Set up schedules
            var schedules = new List<Schedule>
            {
                new Schedule
                {
                    UserId = _userId,
                    ShiftDate = DateTime.Today,
                    StartTime = new TimeSpan(9, 0, 0),
                    EndTime = new TimeSpan(17, 0, 0),
                    Comment = "Regular shift",
                    Status = "Approved",
                    ProjectId = 1
                },
                new Schedule
                {
                    UserId = _userId,
                    ShiftDate = DateTime.Today.AddDays(1),
                    StartTime = new TimeSpan(10, 0, 0),
                    EndTime = new TimeSpan(18, 0, 0),
                    Comment = "Project meeting",
                    Status = "Approved",
                    ProjectId = 2
                }
            };
            _context.Schedules.AddRange(schedules);

            // Set up notifications
            var notifications = new List<Notification>
            {
                new Notification
                {
                    UserId = _userId,
                    Message = "New message",
                    Link = "/chat",
                    IsRead = false,
                    Type = NotificationType.Message,
                    Timestamp = DateTime.UtcNow.AddHours(-2),
                    RelatedUserId = "other-user-1",
                    MetaData = "{\"messageId\": 1}"
                },
                new Notification
                {
                    UserId = _userId,
                    Message = "Task assigned",
                    Link = "/projects/tasks/1",
                    IsRead = false,
                    Type = NotificationType.TaskAssigned,
                    ProjectId = 1,
                    TaskId = 1,
                    Timestamp = DateTime.UtcNow.AddHours(-5),
                    RelatedUserId = "other-user-1",
                    MetaData = "{\"taskTitle\": \"Task 1\"}"
                }
            };
            _context.Notifications.AddRange(notifications);

            // Set up attendances
            var attendances = new List<Attendance>
            {
                new Attendance
                {
                    UserId = _userId,
                    CheckInTime = DateTime.UtcNow.AddDays(-1).Date.AddHours(9),
                    CheckOutTime = DateTime.UtcNow.AddDays(-1).Date.AddHours(17)
                },
                new Attendance
                {
                    UserId = _userId,
                    CheckInTime = DateTime.UtcNow.AddHours(-2),
                    CheckOutTime = null // Active session
                }
            };
            _context.Attendances.AddRange(attendances);

            // Set up dashboard layout
            var layout = new DashboardLayout
            {
                UserId = _userId,
                LayoutData = "{\"widgets\":[{\"id\":\"tasks\",\"position\":1}]}",
                LastUpdated = DateTime.UtcNow.AddDays(-1)
            };
            _context.DashboardLayouts.Add(layout);

            _context.SaveChanges();

            // Mock UserManager
            var userStoreMock = new Mock<IUserStore<User>>();
            _mockUserManager = new Mock<UserManager<User>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null);

            _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(user);
        }

        [Fact]
        public async Task GetTasks_ReturnsTopFivePendingTasks()
        {
            // Arrange
            var controller = CreateController();

            // Act
            var result = await controller.GetTasks();

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            var tasks = Assert.IsAssignableFrom<IEnumerable<object>>(jsonResult.Value);
            
            // Verify we get tasks that aren't completed
            Assert.Equal(2, tasks.Count()); // Should be Tasks 1 and 2 (pending ones)
            
            // Verify tasks are ordered by due date
            var tasksList = tasks.ToList();
            var firstTaskTitle = tasksList[0].GetType().GetProperty("Title").GetValue(tasksList[0]);
            Assert.Equal("Task 1", firstTaskTitle); // Task 1 has earlier due date
        }

        [Fact]
        public async Task GetTimeline_ReturnsProjectsWithProgress()
        {
            // Arrange
            var controller = CreateController();

            // Act
            var result = await controller.GetTimeline();

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            var projects = Assert.IsAssignableFrom<IEnumerable<object>>(jsonResult.Value);
            
            Assert.Equal(2, projects.Count());
            
            // Verify progress calculation
            var projectsList = projects.ToList();
            var firstProjectProgress = (double)projectsList[0].GetType().GetProperty("Progress").GetValue(projectsList[0]);
            Assert.Equal(40, firstProjectProgress); // Project 1: 2/5 tasks complete = 40%
        }

        [Fact]
        public async Task GetMeetings_ReturnsUpcomingMeetings()
        {
            // Arrange
            var controller = CreateController();

            // Act
            var result = await controller.GetMeetings();

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            var meetings = Assert.IsAssignableFrom<IEnumerable<object>>(jsonResult.Value);
            
            Assert.Equal(2, meetings.Count());
            
            // Verify today's meeting comes first (ordered by date)
            var meetingsList = meetings.ToList();
            var firstMeetingDate = meetingsList[0].GetType().GetProperty("Date").GetValue(meetingsList[0]);
            Assert.Contains(DateTime.Today.ToString("MMM dd"), firstMeetingDate.ToString());
        }

        [Fact]
        public async Task GetNotifications_ReturnsUnreadNotifications()
        {
            // Arrange
            var controller = CreateController();

            // Act
            var result = await controller.GetNotifications();

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            var notifications = Assert.IsAssignableFrom<IEnumerable<object>>(jsonResult.Value);
            
            Assert.Equal(2, notifications.Count()); // All unread notifications for user
            
            // Verify ordered by timestamp (most recent first)
            var notificationsList = notifications.ToList();
            var firstMessage = notificationsList[0].GetType().GetProperty("Message").GetValue(notificationsList[0]);
            Assert.Equal("New message", firstMessage);
        }

        [Fact]
        public async Task SaveLayout_ValidLayout_SavesSuccessfully()
        {
            // Arrange
            var controller = CreateController();
            var newLayout = new DashboardLayout
            {
                LayoutData = "{\"widgets\":[{\"id\":\"calendar\",\"position\":2}]}"
            };
            
            // Act
            var result = await controller.SaveLayout(newLayout);
            
            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            dynamic resultValue = okResult.Value;
            
            // Verify the result contains success=true
            Assert.True((bool)resultValue.GetType().GetProperty("success").GetValue(resultValue));
            
            // Verify the database was updated
            var savedLayout = await _context.DashboardLayouts
                .FirstOrDefaultAsync(l => l.UserId == _userId);
                
            Assert.NotNull(savedLayout);
            Assert.Equal(newLayout.LayoutData, savedLayout.LayoutData);
        }

        [Fact]
        public async Task GetLayout_ExistingLayout_ReturnsLayout()
        {
            // Arrange
            var controller = CreateController();
            
            // Act
            var result = await controller.GetLayout();
            
            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            dynamic resultValue = okResult.Value;
            
            // Verify the result contains success=true
            Assert.True((bool)resultValue.GetType().GetProperty("success").GetValue(resultValue));
            
            // Verify the layout data matches what's in database
            var layoutData = resultValue.GetType().GetProperty("layoutData").GetValue(resultValue);
            var expectedLayout = await _context.DashboardLayouts
                .Where(l => l.UserId == _userId)
                .Select(l => l.LayoutData)
                .FirstOrDefaultAsync();
                
            Assert.Equal(expectedLayout, layoutData);
        }

        [Fact]
        public async Task GetAnalytics_ReturnsTaskStatsAndProjectProgress()
        {
            // Arrange
            var controller = CreateController();
            
            // Act
            var result = await controller.GetAnalytics();
            
            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            dynamic analytics = jsonResult.Value;
            
            // Verify that the response contains task statistics
            var taskStats = (IEnumerable<object>)analytics.GetType().GetProperty("TaskStatistics").GetValue(analytics);
            Assert.NotNull(taskStats);
            
            // Should have 2 status groups: Pending and Completed
            Assert.Equal(2, taskStats.Count());
            
            // Verify project progress data
            var projectProgress = (IEnumerable<object>)analytics.GetType().GetProperty("ProjectProgress").GetValue(analytics);
            Assert.NotNull(projectProgress);
            Assert.Equal(2, projectProgress.Count());
        }

        [Fact]
        public async Task GetClockIn_WithActiveSession_ReturnsActiveStatus()
        {
            // Arrange
            var controller = CreateController();
            
            // Act
            var result = await controller.GetClockIn();
            
            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            dynamic clockInStatus = okResult.Value;
            
            // Should report active status
            Assert.True((bool)clockInStatus.GetType().GetProperty("active").GetValue(clockInStatus));
            
            // Should include checkInTime and elapsedTime
            Assert.NotNull(clockInStatus.GetType().GetProperty("checkInTime").GetValue(clockInStatus));
            Assert.True((double)clockInStatus.GetType().GetProperty("elapsedTime").GetValue(clockInStatus) > 0);
        }

        [Fact]
        public async Task GetClockIn_NoActiveSession_ReturnsInactiveStatus()
        {
            // Arrange
            // Close the active session
            var activeSession = await _context.Attendances
                .Where(a => a.UserId == _userId && a.CheckOutTime == null)
                .FirstOrDefaultAsync();
                
            activeSession.CheckOutTime = DateTime.UtcNow;
            _context.Attendances.Update(activeSession);
            await _context.SaveChangesAsync();
            
            var controller = CreateController();
            
            // Act
            var result = await controller.GetClockIn();
            
            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            dynamic clockInStatus = okResult.Value;
            
            // Should report inactive status
            Assert.False((bool)clockInStatus.GetType().GetProperty("active").GetValue(clockInStatus));
            Assert.Equal("Not clocked in", clockInStatus.GetType().GetProperty("message").GetValue(clockInStatus));
        }

        private DashboardController CreateController()
        {
            var controller = new DashboardController(_context, _mockUserManager.Object);
            
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, _userId)
            };
            var identity = new ClaimsIdentity(claims);
            var user = new ClaimsPrincipal(identity);
            
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
            
            return controller;
        }
    }
} 