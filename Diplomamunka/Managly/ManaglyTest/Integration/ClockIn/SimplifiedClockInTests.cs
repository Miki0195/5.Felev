using Xunit;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Managly.Controllers;
using Managly.Data;
using Managly.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Managlytest.Helpers;
using System.Security.Claims;
using Moq;

namespace Managlytest.Integration
{
    public class SimplifiedClockInTests
    {
        [Fact]
        public async Task Unauthorized_User_Cannot_Clock_In()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("TestDb")
                .Options;

            using var context = new ApplicationDbContext(options);
            var mockUserManager = MockUserManagerFactory.Create();
            mockUserManager.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
                .Returns((string)null);

            var controller = new ClockInController(context, mockUserManager.Object);

            // Act
            var result = await controller.ClockIn();

            // Assert
            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        // Add more tests as needed
    }
} 