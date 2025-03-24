using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Managly.Controllers;
using Managly.Data;
using Managly.Models;
using Managly.Models.DTOs.ClockIn;
using Managlytest.Helpers;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;

namespace Managlytest.Controllers
{
    public class ClockInControllerTests
    {
        private readonly Mock<UserManager<User>> _mockUserManager;
        private readonly ApplicationDbContext _context;
        private readonly ClockInController _controller;
        private readonly string _testUserId = "test-user-id";
        private readonly User _testUser;

        public ClockInControllerTests()
        {
            // Setup database context
            _context = TestDbContextFactory.CreateDbContext();
            
            // Setup user manager
            _mockUserManager = MockUserManagerFactory.Create();
            
            // Create test user with all required properties
            _testUser = new User 
            { 
                Id = _testUserId, 
                UserName = "test@example.com",
                Email = "test@example.com",
                Name = "Test",
                LastName = "User",
                CompanyId = 1,
                Address = "Test Address",  // Add required properties
                City = "Test City",        // Add required properties
                Country = "Test Country"   // Add required properties
            };
            
            // Setup user manager to return our test user
            _mockUserManager.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
                .Returns(_testUserId);
            
            // Create controller
            _controller = new ClockInController(_context, _mockUserManager.Object);
            
            // Setup controller context
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, _testUserId),
                    }))
                }
            };
        }

        #region ClockIn Tests

        [Fact]
        public async Task ClockIn_WhenUserNotFound_ReturnsUnauthorized()
        {
            // Arrange
            _mockUserManager.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
                .Returns((string)null);

            // Act
            var result = await _controller.ClockIn();

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            var response = Assert.IsType<ApiResponseDto>(unauthorizedResult.Value);
            Assert.False(response.Success);
            Assert.Equal("User not found", response.Error);
        }

        [Fact]
        public async Task ClockIn_WhenAlreadyClockedIn_ReturnsBadRequest()
        {
            // Arrange
            _context.Attendances.Add(new Attendance
            {
                UserId = _testUserId,
                CheckInTime = DateTime.Now.AddHours(-2),
                CheckOutTime = null
            });
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.ClockIn();

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponseDto>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.Equal("You are already clocked in", response.Error);
        }

        [Fact]
        public async Task ClockIn_WhenNotClockedIn_ReturnsOkWithCheckInTime()
        {
            // Act
            var result = await _controller.ClockIn();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ClockInResponseDto>(okResult.Value);
            Assert.True(response.Success);
            Assert.True(DateTime.Now.AddMinutes(-1) <= response.CheckInTime && response.CheckInTime <= DateTime.Now.AddMinutes(1));
            
            // Verify attendance record was created
            var attendance = await _context.Attendances.FirstOrDefaultAsync(a => a.UserId == _testUserId);
            Assert.NotNull(attendance);
            Assert.Null(attendance.CheckOutTime);
        }

        #endregion

        #region ClockOut Tests

        [Fact]
        public async Task ClockOut_WhenUserNotFound_ReturnsUnauthorized()
        {
            // Arrange
            _mockUserManager.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
                .Returns((string)null);

            // Act
            var result = await _controller.ClockOut();

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            var response = Assert.IsType<ApiResponseDto>(unauthorizedResult.Value);
            Assert.False(response.Success);
            Assert.Equal("User not found", response.Error);
        }

        [Fact]
        public async Task ClockOut_WhenNoActiveSession_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.ClockOut();

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponseDto>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.Equal("No active session found", response.Error);
        }

        [Fact]
        public async Task ClockOut_WithActiveSession_ReturnsOkWithCheckOutTime()
        {
            // Arrange
            var checkInTime = DateTime.Now.AddHours(-2);
            _context.Attendances.Add(new Attendance
            {
                UserId = _testUserId,
                CheckInTime = checkInTime,
                CheckOutTime = null
            });
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.ClockOut();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ClockOutResponseDto>(okResult.Value);
            Assert.True(response.Success);
            Assert.NotNull(response.CheckOutTime);
            Assert.True(response.Duration > 1.9 && response.Duration < 2.1);
            
            // Verify attendance record was updated
            var attendance = await _context.Attendances.FirstOrDefaultAsync(a => a.UserId == _testUserId);
            Assert.NotNull(attendance);
            Assert.NotNull(attendance.CheckOutTime);
        }

        #endregion

        #region GetCurrentSession Tests

        [Fact]
        public async Task GetCurrentSession_WhenUserNotFound_ReturnsUnauthorized()
        {
            // Arrange
            _mockUserManager.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
                .Returns((string)null);

            // Act
            var result = await _controller.GetCurrentSession();

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            var response = Assert.IsType<ApiResponseDto>(unauthorizedResult.Value);
            Assert.False(response.Success);
            Assert.Equal("User not found", response.Error);
        }

        [Fact]
        public async Task GetCurrentSession_WithNoActiveSession_ReturnsInactiveStatus()
        {
            // Act
            var result = await _controller.GetCurrentSession();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<SessionStatusDto>(okResult.Value);
            Assert.False(response.Active);
            Assert.Null(response.CheckInTime);
        }

        [Fact]
        public async Task GetCurrentSession_WithActiveSession_ReturnsActiveStatus()
        {
            // Arrange
            var checkInTime = DateTime.Now.AddMinutes(-30);
            _context.Attendances.Add(new Attendance
            {
                UserId = _testUserId,
                CheckInTime = checkInTime,
                CheckOutTime = null
            });
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetCurrentSession();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<SessionStatusDto>(okResult.Value);
            Assert.True(response.Active);
            Assert.Equal(checkInTime, response.CheckInTime);
            Assert.True(response.ElapsedTime > 29 * 60 && response.ElapsedTime < 31 * 60);
        }

        #endregion

        #region GetWorkHistory Tests

        [Fact]
        public async Task GetWorkHistory_WhenUserNotFound_ReturnsUnauthorized()
        {
            // Arrange
            _mockUserManager.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
                .Returns((string)null);

            // Act
            var result = await _controller.GetWorkHistory();

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            var response = Assert.IsType<ApiResponseDto>(unauthorizedResult.Value);
            Assert.False(response.Success);
            Assert.Equal("User not found", response.Error);
        }

        [Fact]
        public async Task GetWorkHistory_WithNoHistory_ReturnsNoRecordsMessage()
        {
            // Act
            var result = await _controller.GetWorkHistory();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            
            // Use the dictionary approach
            var dict = ConvertAnonymousObjectToDictionary(okResult.Value);
            Assert.True(dict.ContainsKey("message"));
            Assert.Equal("No records found.", dict["message"]);
        }

        [Fact]
        public async Task GetWorkHistory_WithHistory_ReturnsHistoryEntries()
        {
            // Arrange
            var now = DateTime.Now;
            
            // Add completed attendance record
            _context.Attendances.Add(new Attendance
            {
                UserId = _testUserId,
                CheckInTime = now.AddHours(-8),
                CheckOutTime = now.AddHours(-0)
            });
            
            // Add ongoing attendance record
            _context.Attendances.Add(new Attendance
            {
                UserId = _testUserId,
                CheckInTime = now.AddDays(-1),
                CheckOutTime = now.AddDays(-1).AddHours(7)
            });
            
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetWorkHistory();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var historyEntries = Assert.IsAssignableFrom<IEnumerable<WorkHistoryEntryDto>>(okResult.Value);
            Assert.Equal(2, historyEntries.Count());
            
            // Verify most recent entry is first
            var firstEntry = historyEntries.First();
            Assert.Equal(now.AddHours(-8).ToString("g"), firstEntry.CheckInTime.ToString("g"));
            Assert.Equal(now.AddHours(-0).ToString("g"), firstEntry.CheckOutTime?.ToString("g"));
            Assert.Equal("08:00:00", firstEntry.Duration);
        }

        #endregion

        #region GetWeeklyHours Tests

        [Fact]
        public async Task GetWeeklyHours_WhenUserNotFound_ReturnsUnauthorized()
        {
            // Arrange
            _mockUserManager.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
                .Returns((string)null);

            // Act
            var result = await _controller.GetWeeklyHours();

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            var response = Assert.IsType<ApiResponseDto>(unauthorizedResult.Value);
            Assert.False(response.Success);
            Assert.Equal("User not found", response.Error);
        }

        [Fact]
        public async Task GetWeeklyHours_WithAttendanceRecords_ReturnsCorrectHours()
        {
            // Arrange
            var startOfWeek = DateTime.Now.Date.AddDays(-(int)DateTime.Now.DayOfWeek);
            
            // Add attendance records within current week
            _context.Attendances.Add(new Attendance
            {
                UserId = _testUserId,
                CheckInTime = startOfWeek.AddHours(9),
                CheckOutTime = startOfWeek.AddHours(17)
            });
            
            _context.Attendances.Add(new Attendance
            {
                UserId = _testUserId,
                CheckInTime = startOfWeek.AddDays(1).AddHours(9),
                CheckOutTime = startOfWeek.AddDays(1).AddHours(17)
            });
            
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetWeeklyHours();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var weeklyHours = Assert.IsType<WeeklyHoursDto>(okResult.Value);
            Assert.Equal(16, weeklyHours.TotalHours);
            Assert.Equal(0, weeklyHours.TotalMinutes);
        }

        #endregion

        #region Admin Tests

        // Define a class that matches the structure of the IsAdmin response
        private class IsAdminResponse 
        {
            public bool isAdmin { get; set; }
        }

        [Fact]
        public async Task IsAdmin_WhenUserNotFound_ReturnsUnauthorized()
        {
            // Arrange
            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync((User)null);

            // Act
            var result = await _controller.IsAdmin();

            // Assert
            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task IsAdmin_WhenUserIsAdmin_ReturnsTrue()
        {
            // Arrange
            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(_testUser);
            
            _mockUserManager.Setup(um => um.IsInRoleAsync(_testUser, "Admin"))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.IsAdmin();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var dict = ConvertAnonymousObjectToDictionary(okResult.Value);
            Assert.True(dict.ContainsKey("isAdmin"));
            Assert.True((bool)dict["isAdmin"]);
        }

        [Fact]
        public async Task IsAdmin_WhenUserIsManager_ReturnsTrue()
        {
            // Arrange
            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(_testUser);
            
            _mockUserManager.Setup(um => um.IsInRoleAsync(_testUser, "Admin"))
                .ReturnsAsync(false);
                
            _mockUserManager.Setup(um => um.IsInRoleAsync(_testUser, "Manager"))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.IsAdmin();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var dict = ConvertAnonymousObjectToDictionary(okResult.Value);
            Assert.True(dict.ContainsKey("isAdmin"));
            Assert.True((bool)dict["isAdmin"]);
        }

        [Fact]
        public async Task IsAdmin_WhenUserIsNotAdminOrManager_ReturnsFalse()
        {
            // Arrange
            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(_testUser);
            
            _mockUserManager.Setup(um => um.IsInRoleAsync(_testUser, It.IsAny<string>()))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.IsAdmin();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var dict = ConvertAnonymousObjectToDictionary(okResult.Value);
            Assert.True(dict.ContainsKey("isAdmin"));
            Assert.False((bool)dict["isAdmin"]);
        }

        // Helper method to convert anonymous objects to dictionaries
        private Dictionary<string, object> ConvertAnonymousObjectToDictionary(object obj)
        {
            return obj.GetType()
                .GetProperties()
                .ToDictionary(
                    prop => prop.Name,
                    prop => prop.GetValue(obj)
                );
        }

        #endregion

        #region Team Overview Tests

        [Fact]
        public async Task GetTeamOverview_WhenUserNotFound_ReturnsUnauthorized()
        {
            // Arrange
            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync((User)null);

            // Act
            var result = await _controller.GetTeamOverview();

            // Assert
            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task GetTeamOverview_WhenCompanyIdNull_ReturnsBadRequest()
        {
            // Arrange
            var testUser = new User { Id = _testUserId, CompanyId = null };
            
            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(testUser);

            // Act
            var result = await _controller.GetTeamOverview();

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        #endregion

        #region Update Time Tests

        [Fact]
        public async Task UpdateAttendanceTime_WithInvalidRequest_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.UpdateAttendanceTime(null);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UpdateAttendanceTime_WithValidRequest_ReturnsSuccess()
        {
            // Arrange
            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(_testUser);
                
            _context.Users.Add(_testUser);
            await _context.SaveChangesAsync();
                
            var updateRequest = new UpdateTimeRequestDto
            {
                UserId = _testUserId,
                CheckInTime = DateTime.Now.AddHours(-3).ToString("o"),
                CheckOutTime = DateTime.Now.ToString("o"),
                AdminNotes = "Updated for testing"
            };

            // Act
            var result = await _controller.UpdateAttendanceTime(updateRequest);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            
            // Use the dictionary approach
            var dict = ConvertAnonymousObjectToDictionary(okResult.Value);
            Assert.True(dict.ContainsKey("success"));
            Assert.True((bool)dict["success"]);
            
            // Verify new record was created
            var attendanceRecord = await _context.Attendances
                .FirstOrDefaultAsync(a => a.UserId == _testUserId);
            Assert.NotNull(attendanceRecord);
            
            // Verify audit log was created
            var auditLog = await _context.AttendanceAuditLogs
                .FirstOrDefaultAsync(l => l.AttendanceId == attendanceRecord.Id);
            Assert.NotNull(auditLog);
            Assert.Equal("Updated for testing", auditLog.Notes);
        }

        [Fact]
        public async Task UpdateAttendanceTime_WithInvalidDateFormat_ReturnsBadRequest()
        {
            // Arrange
            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(_testUser);
            
            // Add the user to the database first
            _context.Users.Add(_testUser);
            await _context.SaveChangesAsync();
            
            var updateRequest = new UpdateTimeRequestDto
            {
                UserId = _testUserId,
                CheckInTime = "not-a-date",
                CheckOutTime = DateTime.Now.ToString("o"),
                AdminNotes = "Updated for testing"
            };

            // Act
            var result = await _controller.UpdateAttendanceTime(updateRequest);

            // Assert - updated to match the actual controller behavior
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UpdateAttendanceTime_WithDifferentCompanyUser_ReturnsNotFound()
        {
            // Arrange
            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(_testUser);

            var otherCompanyUser = new User
            {
                Id = "different-company-user",
                UserName = "other@example.com", // Add required properties
                CompanyId = 2,
                Name = "Other",
                LastName = "User",
                Email = "other@example.com",
                Address = "Other Address",     // Add required properties
                City = "Other City",           // Add required properties
                Country = "Other Country"      // Add required properties
            };

            _context.Users.Add(_testUser);
            _context.Users.Add(otherCompanyUser);
            await _context.SaveChangesAsync();

            var updateRequest = new UpdateTimeRequestDto
            {
                UserId = "different-company-user",
                CheckInTime = DateTime.Now.AddHours(-3).ToString("o"),
                CheckOutTime = DateTime.Now.ToString("o")
            };

            // Act
            var result = await _controller.UpdateAttendanceTime(updateRequest);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        #endregion
    }
} 