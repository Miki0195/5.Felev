using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Managly.Controllers;
using Managly.Data;
using Managly.Models;
using Managly.Models.DTOs.Schedule;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Managly.Tests.Controllers
{
    public class ScheduleControllerEdgeCaseTests
    {
        private readonly Mock<UserManager<User>> _mockUserManager;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ILogger<ScheduleController>> _mockLogger;
        private readonly ApplicationDbContext _mockContext;

        public ScheduleControllerEdgeCaseTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
                
            _mockContext = new ApplicationDbContext(options);
            
            var userStoreMock = new Mock<IUserStore<User>>();
            _mockUserManager = new Mock<UserManager<User>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null);
                
            _mockConfiguration = new Mock<IConfiguration>();
            
            _mockLogger = new Mock<ILogger<ScheduleController>>();
        }

        [Fact]
        public async Task UpdateSchedule_DbException_ReturnsServerError()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
                
            var context = new ApplicationDbContext(options);
            
            var schedule = new Schedule { 
                Id = 1, 
                UserId = "test-user-id",
                ShiftDate = DateTime.Today,
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(17, 0, 0),
                Comment = ""
            };
            context.Schedules.Add(schedule);
            await context.SaveChangesAsync();
            
            var userStoreMock = new Mock<IUserStore<User>>();
            var userManagerMock = new Mock<UserManager<User>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null);
            var configurationMock = new Mock<IConfiguration>();
            var loggerMock = new Mock<ILogger<ScheduleController>>();
            
            var mockContext = new Mock<ApplicationDbContext>(options);
            mockContext.Setup(c => c.SaveChangesAsync(default))
                .ThrowsAsync(new DbUpdateException("Test exception", new Exception()));
                
            var controller = new ScheduleController(
                mockContext.Object,
                userManagerMock.Object,
                configurationMock.Object,
                loggerMock.Object
            );
            
            var result = await controller.UpdateSchedule(1, new ScheduleUpdateDTO
            {
                ShiftDate = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd"),
                StartTime = "10:00",
                EndTime = "18:00"
            });
            
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
        }
        
        [Fact]
        public async Task GetSchedule_EmptySchedule_ReturnsEmptyArray()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
                
            var context = new ApplicationDbContext(options);
            
            var userStoreMock = new Mock<IUserStore<User>>();
            var userManagerMock = new Mock<UserManager<User>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null);
            var configurationMock = new Mock<IConfiguration>();
            var loggerMock = new Mock<ILogger<ScheduleController>>();
            
            var controller = new ScheduleController(
                context,
                userManagerMock.Object,
                configurationMock.Object,
                loggerMock.Object
            );
            
            var result = await controller.GetSchedule("non-existent-user");
            
            var okResult = Assert.IsType<OkObjectResult>(result);
            var schedules = okResult.Value as IEnumerable<object>;
            Assert.NotNull(schedules);
            Assert.Empty(schedules);
        }
        
        [Fact]
        public async Task RequestLeave_NullRequest_ReturnsBadRequest()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
                
            var context = new ApplicationDbContext(options);
            
            var controller = new ScheduleController(
                context,
                _mockUserManager.Object,
                _mockConfiguration.Object,
                _mockLogger.Object
            );
            
            var result = await controller.RequestLeave(null);
            
            Assert.IsType<BadRequestObjectResult>(result);
        }
        
        [Fact]
        public async Task RequestLeave_EmptyRequest_ReturnsBadRequest()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
                
            var context = new ApplicationDbContext(options);
            
            var controller = new ScheduleController(
                context,
                _mockUserManager.Object,
                _mockConfiguration.Object,
                _mockLogger.Object
            );
            
            var result = await controller.RequestLeave(new List<Leave>());
            
            Assert.IsType<BadRequestObjectResult>(result);
        }
    }
} 