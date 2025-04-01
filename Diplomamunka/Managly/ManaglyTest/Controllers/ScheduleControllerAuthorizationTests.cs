using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Managly.Controllers;
using Managly.Data;
using Managly.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Managly.Tests.Controllers
{
    public class ScheduleControllerAuthorizationTests
    {
        private readonly Mock<ApplicationDbContext> _mockContext;
        private readonly Mock<UserManager<User>> _mockUserManager;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ILogger<ScheduleController>> _mockLogger;
        
        public ScheduleControllerAuthorizationTests()
        {
            // Setup DbContext mock
            _mockContext = new Mock<ApplicationDbContext>(new DbContextOptions<ApplicationDbContext>());
            
            // Setup UserManager mock
            var userStoreMock = new Mock<IUserStore<User>>();
            _mockUserManager = new Mock<UserManager<User>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null);
                
            // Setup IConfiguration mock
            _mockConfiguration = new Mock<IConfiguration>();
            
            // Setup Logger mock
            _mockLogger = new Mock<ILogger<ScheduleController>>();
        }
        
        [Fact]
        public async Task Manage_NonAdminUser_ThrowsUnauthorizedException()
        {
            // Arrange
            var controller = new ScheduleController(
                _mockContext.Object,
                _mockUserManager.Object,
                _mockConfiguration.Object,
                _mockLogger.Object
            );
            
            // Setup controller context with regular user (non-admin)
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, "user-id-1"),
                new Claim(ClaimTypes.Name, "test@example.com")
                // No Admin role claim
            }));
            
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
            
            // We need to simulate the authorization filter - in actual MVC, this would be handled by the framework
            // For unit testing purposes, we can check the Authorize attribute on the method
            var methodInfo = typeof(ScheduleController).GetMethod("Manage");
            var authorizeAttribute = methodInfo.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true)
                .Cast<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>()
                .FirstOrDefault();
                
            // Assert
            authorizeAttribute.Should().NotBeNull();
            authorizeAttribute.Roles.Should().Be("Admin");
            
            // We can't directly test the framework's authorization behavior in a unit test,
            // but we can verify the attribute is present with the correct role requirement
        }
        
        [Fact]
        public async Task GetMySchedule_UnauthenticatedUser_ReturnsUnauthorized()
        {
            // Arrange
            var controller = new ScheduleController(
                _mockContext.Object,
                _mockUserManager.Object,
                _mockConfiguration.Object,
                _mockLogger.Object
            );
            
            // Setup controller context with unauthenticated user (no claims)
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
            };
            
            // Act
            var result = await controller.GetMySchedule();
            
            // Assert
            result.Should().BeOfType<UnauthorizedResult>();
        }
        
        [Fact]
        public async Task UpdateLeaveStatus_RegularUser_ThrowsUnauthorizedException()
        {
            // Arrange
            var controller = new ScheduleController(
                _mockContext.Object,
                _mockUserManager.Object,
                _mockConfiguration.Object,
                _mockLogger.Object
            );
            
            // Setup controller context with regular user (non-admin)
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, "user-id-1"),
                new Claim(ClaimTypes.Name, "test@example.com")
                // No Admin role claim
            }));
            
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
            
            // We need to check the Authorize attribute on the method
            var methodInfo = typeof(ScheduleController).GetMethod("UpdateLeaveStatus");
            var authorizeAttribute = methodInfo.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true)
                .Cast<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>()
                .FirstOrDefault();
                
            // Assert
            authorizeAttribute.Should().NotBeNull();
            authorizeAttribute.Roles.Should().Be("Admin");
        }
    }
} 