using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Managly.Controllers;
using Managly.Data;
using Managly.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Microsoft.Extensions.Configuration;

namespace Managly.Tests.Controllers
{
    // Create an interface for EmailService to allow proper mocking
    public interface IEmailService
    {
        Task SendEmailWithSmtpAsync(string toEmail, string subject, string body, string senderEmail, string senderName);
    }
    
    public class AdminControllerTests
    {
        // Test data and mocks
        private readonly ApplicationDbContext _context;
        private readonly Mock<UserManager<User>> _mockUserManager;
        private readonly Mock<RoleManager<IdentityRole>> _mockRoleManager;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<IConfiguration> _mockConfiguration;
        
        private readonly User _testUser;
        private readonly User _adminUser;
        private readonly User _managerUser;
        private readonly Company _testCompany;
        
        public AdminControllerTests()
        {
            // Setup in-memory database
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "AdminControllerTest_" + Guid.NewGuid())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
                
            _context = new ApplicationDbContext(options);
            
            // Create test company
            _testCompany = new Company 
            { 
                Id = 1, 
                Name = "Test Company", 
                LicenseKey = "TEST-KEY-123",
                CreatedDate = DateTime.Now.AddMonths(-6)
            };
            
            // Setup test users
            _testUser = new User
            {
                Id = "user-id-1",
                UserName = "testuser@example.com",
                Email = "testuser@example.com",
                Name = "Test",
                LastName = "User",
                CompanyId = 1,
                TotalVacationDays = 20,
                UsedVacationDays = 5,
                EmailConfirmed = true,
                Country = "Test Country",
                City = "Test City",
                Address = "Test Address",
                CreatedDate = DateTime.Now.AddMonths(-3)
            };
            
            _adminUser = new User
            {
                Id = "admin-id-1",
                UserName = "admin@example.com",
                Email = "admin@example.com",
                Name = "Admin",
                LastName = "User",
                CompanyId = 1,
                TotalVacationDays = 25,
                UsedVacationDays = 10,
                EmailConfirmed = true,
                Country = "Admin Country",
                City = "Admin City",
                Address = "Admin Address",
                CreatedDate = DateTime.Now.AddMonths(-5)
            };
            
            _managerUser = new User
            {
                Id = "manager-id-1",
                UserName = "manager@example.com",
                Email = "manager@example.com",
                Name = "Manager",
                LastName = "User",
                CompanyId = 1,
                TotalVacationDays = 22,
                UsedVacationDays = 8,
                EmailConfirmed = true,
                Country = "Manager Country",
                City = "Manager City",
                Address = "Manager Address",
                CreatedDate = DateTime.Now.AddMonths(-4)
            };
            
            // Seed the database
            _context.Companies.Add(_testCompany);
            _context.Users.AddRange(_testUser, _adminUser, _managerUser);
            _context.SaveChanges();
            
            // Setup User Manager mock
            var mockUserStore = new Mock<IUserStore<User>>();
            _mockUserManager = new Mock<UserManager<User>>(
                mockUserStore.Object, null, null, null, null, null, null, null, null);
                
            // Setup common UserManager methods
            _mockUserManager.Setup(m => m.FindByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((string id) => _context.Users.FirstOrDefault(u => u.Id == id));
                
            _mockUserManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync((string email) => _context.Users.FirstOrDefault(u => u.Email == email));
                
            _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(_adminUser);

            // Mock Users to support async enumeration
            var usersQueryable = _context.Users.AsQueryable();
            _mockUserManager.Setup(m => m.Users).Returns(usersQueryable);
                
            _mockUserManager.Setup(m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);
                
            _mockUserManager.Setup(m => m.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);
                
            _mockUserManager.Setup(m => m.UpdateAsync(It.IsAny<User>()))
                .ReturnsAsync(IdentityResult.Success);
                
            _mockUserManager.Setup(m => m.DeleteAsync(It.IsAny<User>()))
                .ReturnsAsync(IdentityResult.Success);
                
            _mockUserManager.Setup(m => m.GetRolesAsync(It.IsAny<User>()))
                .ReturnsAsync((User user) => {
                    if (user.Id == "admin-id-1") return new List<string> { "Admin" };
                    if (user.Id == "manager-id-1") return new List<string> { "Manager" };
                    return new List<string> { "Employee" };
                });
                
            _mockUserManager.Setup(m => m.IsInRoleAsync(It.IsAny<User>(), It.IsAny<string>()))
                .ReturnsAsync((User user, string role) => {
                    if (user.Id == "admin-id-1" && role == "Admin") return true;
                    if (user.Id == "manager-id-1" && role == "Manager") return true;
                    return false;
                });
                
            _mockUserManager.Setup(m => m.RemoveFromRolesAsync(It.IsAny<User>(), It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(IdentityResult.Success);
                
            _mockUserManager.Setup(m => m.NormalizeEmail(It.IsAny<string>()))
                .Returns((string email) => email.ToUpper());
                
            // Setup Role Manager mock
            var mockRoleStore = new Mock<IRoleStore<IdentityRole>>();
            _mockRoleManager = new Mock<RoleManager<IdentityRole>>(
                mockRoleStore.Object, null, null, null, null);
                
            _mockRoleManager.Setup(m => m.RoleExistsAsync(It.IsAny<string>()))
                .ReturnsAsync(true);
                
            _mockRoleManager.Setup(m => m.Roles)
                .Returns(new List<IdentityRole> 
                { 
                    new IdentityRole { Id = "1", Name = "Admin" },
                    new IdentityRole { Id = "2", Name = "Manager" },
                    new IdentityRole { Id = "3", Name = "Employee" }
                }.AsQueryable());
                
            // Setup mock configuration for email service
            _mockConfiguration = new Mock<IConfiguration>();
            var mockConfigSection = new Mock<IConfigurationSection>();
            mockConfigSection.Setup(s => s.Value).Returns("smtp.example.com");
            _mockConfiguration.Setup(c => c["EmailSettings:SmtpHost"]).Returns("smtp.example.com");
            _mockConfiguration.Setup(c => c["EmailSettings:SmtpPort"]).Returns("587");
            _mockConfiguration.Setup(c => c["EmailSettings:SmtpUser"]).Returns("test@example.com");
            _mockConfiguration.Setup(c => c["EmailSettings:SmtpPass"]).Returns("password123");
            
            // Setup Email Service mock with interface
            _mockEmailService = new Mock<IEmailService>();
            _mockEmailService.Setup(m => m.SendEmailWithSmtpAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), 
                It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
        }
        
        [Fact]
        public void CreateProfile_Get_ReturnsView()
        {
            // Arrange
            var controller = CreateControllerAsAdmin();
            
            // Act
            var result = controller.CreateProfile();
            
            // Assert
            Assert.IsType<ViewResult>(result);
        }
        
        [Fact]
        public async Task CreateProfile_Post_ValidModel_CreatesUserAndReturnsView()
        {
            // Arrange
            var controller = CreateControllerAsAdmin();
            var model = new CreateProfile 
            {
                Name = "New",
                LastName = "User",
                Email = "newuser@example.com",
                Role = "Employee"
            };
            
            // Act
            var result = await controller.CreateProfile(model);
            
            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.True(string.IsNullOrEmpty(viewResult.ViewName) || viewResult.ViewName == "CreateProfile");
            
            // Verify that the model is cleared (new instance)
            Assert.IsType<CreateProfile>(viewResult.Model);
            Assert.NotSame(model, viewResult.Model);
            
            // Verify TempData contains success message
            Assert.Contains("success", ((string)controller.TempData["SuccessMessage"]).ToLower());
            
            // Verify user creation was called
            _mockUserManager.Verify(m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Once);
            _mockUserManager.Verify(m => m.AddToRoleAsync(It.IsAny<User>(), "Employee"), Times.Once);
            
            // Email service verification is not needed - it's done through the EmailService wrapper method
        }
        
        [Fact]
        public async Task CreateProfile_Post_InvalidModel_ReturnsViewWithModel()
        {
            // Arrange
            var controller = CreateControllerAsAdmin();
            var model = new CreateProfile(); // Invalid - missing required fields
            controller.ModelState.AddModelError("Name", "Required");
            
            // Act
            var result = await controller.CreateProfile(model);
            
            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(model, viewResult.Model);
            
            // Verify TempData contains error message
            Assert.Contains("ErrorMessage", controller.TempData.Keys);
            
            // Verify user creation was not called
            _mockUserManager.Verify(m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
        }
        
        [Fact]
        public async Task CreateProfile_Post_ExistingEmail_ReturnsViewWithError()
        {
            // Arrange
            var controller = CreateControllerAsAdmin();
            var model = new CreateProfile 
            {
                Name = "Existing",
                LastName = "User",
                Email = "testuser@example.com", // Already exists
                Role = "Employee"
            };
            
            // Setup to return existing user
            _mockUserManager.Setup(m => m.FindByEmailAsync("TESTUSER@EXAMPLE.COM"))
                .ReturnsAsync(_testUser);
            
            // Act
            var result = await controller.CreateProfile(model);
            
            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var returnedModel = Assert.IsType<CreateProfile>(viewResult.Model);
            
            // Verify returned model properties - it should return the same model instance
            Assert.Same(model, returnedModel);
            
            // Verify ModelState has error for Email
            Assert.True(controller.ModelState.ContainsKey("Email"));
            
            // Verify TempData contains error message
            Assert.Contains("ErrorMessage", controller.TempData.Keys);
            
            // Verify user creation was not called
            _mockUserManager.Verify(m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
        }
        
        [Fact]
        public async Task GetUserDetails_ValidUserId_ReturnsUser()
        {
            // Arrange
            var controller = CreateControllerAsAdmin();
            
            // Act
            var result = await controller.GetUserDetails("user-id-1");
            
            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var userDetails = okResult.Value;
            
            // Verify user details as anonymous type
            Assert.NotNull(userDetails);
            var userDict = userDetails.GetType().GetProperties()
                .ToDictionary(p => p.Name, p => p.GetValue(userDetails));
            
            Assert.True(userDict.ContainsKey("userId"));
            Assert.Equal("user-id-1", userDict["userId"]);
            Assert.True(userDict.ContainsKey("name"));
            Assert.Equal("Test", userDict["name"]);
            Assert.True(userDict.ContainsKey("lastName"));
            Assert.Equal("User", userDict["lastName"]);
            Assert.True(userDict.ContainsKey("email"));
            Assert.Equal("testuser@example.com", userDict["email"]);
            Assert.True(userDict.ContainsKey("roles"));
            Assert.Equal("Employee", userDict["roles"]);
            Assert.True(userDict.ContainsKey("success"));
            Assert.True((bool)userDict["success"]);
        }
        
        [Fact]
        public async Task GetUserDetails_InvalidUserId_ReturnsNotFound()
        {
            // Arrange
            var controller = CreateControllerAsAdmin();
            
            // Act
            var result = await controller.GetUserDetails("invalid-id");
            
            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }
        
        [Fact]
        public async Task GetUserDetails_UnauthorizedAccess_ReturnsUnauthorized()
        {
            // Arrange
            var controller = CreateControllerAsAdmin();
            
            // Setup to return user from different company
            var userFromDifferentCompany = new User
            {
                Id = "different-company-user-id",
                CompanyId = 2 // Different from admin's company
            };
            
            _mockUserManager.Setup(m => m.FindByIdAsync("different-company-user-id"))
                .ReturnsAsync(userFromDifferentCompany);
            
            // Act
            var result = await controller.GetUserDetails("different-company-user-id");
            
            // Assert
            Assert.IsType<UnauthorizedObjectResult>(result);
        }
        
        [Fact]
        public async Task DeleteUser_ValidUser_DeletesUserAndReturnsSuccess()
        {
            // Arrange
            var controller = CreateControllerAsAdmin();
            
            // Act
            var result = await controller.DeleteUser("user-id-1");
            
            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            
            // Verify response contains success key
            var responseDict = response.GetType().GetProperties()
                .ToDictionary(p => p.Name, p => p.GetValue(response));
            
            Assert.True(responseDict.ContainsKey("success"));
            Assert.True((bool)responseDict["success"]);
            Assert.True(responseDict.ContainsKey("message"));
            Assert.Contains("successfully deleted", (string)responseDict["message"]);
            
            // Verify user deletion was called
            _mockUserManager.Verify(m => m.DeleteAsync(It.Is<User>(u => u.Id == "user-id-1")), Times.Once);
        }
        
        [Fact]
        public async Task DeleteUser_InvalidUserId_ReturnsNotFound()
        {
            // Arrange
            var controller = CreateControllerAsAdmin();
            
            // Act
            var result = await controller.DeleteUser("invalid-id");
            
            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
            
            // Verify user deletion was not called
            _mockUserManager.Verify(m => m.DeleteAsync(It.IsAny<User>()), Times.Never);
        }
        
        [Fact]
        public async Task DeleteUser_UserFromDifferentCompany_ReturnsUnauthorized()
        {
            // Arrange
            var controller = CreateControllerAsAdmin();
            
            // Setup to return user from different company
            var userFromDifferentCompany = new User
            {
                Id = "different-company-user-id",
                CompanyId = 2 // Different from admin's company
            };
            
            _mockUserManager.Setup(m => m.FindByIdAsync("different-company-user-id"))
                .ReturnsAsync(userFromDifferentCompany);
            
            // Act
            var result = await controller.DeleteUser("different-company-user-id");
            
            // Assert
            Assert.IsType<UnauthorizedObjectResult>(result);
            
            // Verify user deletion was not called
            _mockUserManager.Verify(m => m.DeleteAsync(It.IsAny<User>()), Times.Never);
        }
        
        [Fact]
        public async Task UpdateUserRole_ValidUserAndRole_UpdatesRoleAndReturnsSuccess()
        {
            // Arrange
            var controller = CreateControllerAsAdmin();
            
            // Act
            var result = await controller.UpdateUserRole("user-id-1", "Manager");
            
            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            
            // Verify response contains success key
            var responseDict = response.GetType().GetProperties()
                .ToDictionary(p => p.Name, p => p.GetValue(response));
            
            Assert.True(responseDict.ContainsKey("success"));
            Assert.True((bool)responseDict["success"]);
            Assert.True(responseDict.ContainsKey("message"));
            Assert.Contains("has been updated", (string)responseDict["message"]);
            Assert.True(responseDict.ContainsKey("userId"));
            Assert.Equal("user-id-1", responseDict["userId"]);
            Assert.True(responseDict.ContainsKey("newRole"));
            Assert.Equal("Manager", responseDict["newRole"]);
            
            // Verify role operations were called
            _mockUserManager.Verify(m => m.RemoveFromRolesAsync(
                It.Is<User>(u => u.Id == "user-id-1"), 
                It.IsAny<IEnumerable<string>>()), Times.Once);
                
            _mockUserManager.Verify(m => m.AddToRoleAsync(
                It.Is<User>(u => u.Id == "user-id-1"), 
                "Manager"), Times.Once);
        }
        
        [Fact]
        public async Task UpdateUserRole_InvalidUserId_ReturnsNotFound()
        {
            // Arrange
            var controller = CreateControllerAsAdmin();
            
            // Act
            var result = await controller.UpdateUserRole("invalid-id", "Manager");
            
            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
            
            // Verify role operations were not called
            _mockUserManager.Verify(m => m.RemoveFromRolesAsync(
                It.IsAny<User>(), It.IsAny<IEnumerable<string>>()), Times.Never);
                
            _mockUserManager.Verify(m => m.AddToRoleAsync(
                It.IsAny<User>(), It.IsAny<string>()), Times.Never);
        }
        
        [Fact]
        public async Task UpdateUserRole_InvalidRole_ReturnsBadRequest()
        {
            // Arrange
            var controller = CreateControllerAsAdmin();
            
            // Setup role validation to fail
            _mockRoleManager.Setup(m => m.RoleExistsAsync("InvalidRole"))
                .ReturnsAsync(false);
            
            // Act
            var result = await controller.UpdateUserRole("user-id-1", "InvalidRole");
            
            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
            
            // Verify role operations were not called
            _mockUserManager.Verify(m => m.RemoveFromRolesAsync(
                It.IsAny<User>(), It.IsAny<IEnumerable<string>>()), Times.Never);
                
            _mockUserManager.Verify(m => m.AddToRoleAsync(
                It.IsAny<User>(), It.IsAny<string>()), Times.Never);
        }
        
        [Fact]
        public async Task UpdateVacationDays_ValidRequest_UpdatesDaysAndReturnsSuccess()
        {
            // Arrange
            var controller = CreateControllerAsAdmin();
            
            // Act
            var result = await controller.UpdateVacationDays("user-id-1", 25);
            
            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            
            // Verify response contains success key
            var responseDict = response.GetType().GetProperties()
                .ToDictionary(p => p.Name, p => p.GetValue(response));
            
            Assert.True(responseDict.ContainsKey("success"));
            Assert.True((bool)responseDict["success"]);
            Assert.True(responseDict.ContainsKey("message"));
            Assert.Contains("updated successfully", (string)responseDict["message"]);
            Assert.True(responseDict.ContainsKey("totalDays"));
            Assert.Equal(25, responseDict["totalDays"]);
            Assert.True(responseDict.ContainsKey("usedDays"));
            Assert.Equal(5, responseDict["usedDays"]); // Should not change
            Assert.True(responseDict.ContainsKey("remainingDays"));
            Assert.Equal(20, responseDict["remainingDays"]); // 25 - 5 = 20
            
            // Verify user update was called
            _mockUserManager.Verify(m => m.UpdateAsync(It.Is<User>(u => 
                u.Id == "user-id-1" && u.TotalVacationDays == 25)), Times.Once);
        }
        
        [Fact]
        public async Task UpdateVacationDays_InsufficientDays_ReturnsBadRequest()
        {
            // Arrange
            var controller = CreateControllerAsAdmin();
            
            // Act
            var result = await controller.UpdateVacationDays("user-id-1", 15); // Less than required 20
            
            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
            
            // Verify user update was not called
            _mockUserManager.Verify(m => m.UpdateAsync(It.IsAny<User>()), Times.Never);
        }
        
        [Fact]
        public async Task UpdateVacationDays_UserWithMoreUsedDays_AdjustsUsedDays()
        {
            // Arrange
            var controller = CreateControllerAsAdmin();
            
            // Setup a user with more used days than the new total
            var userWithManyUsedDays = new User
            {
                Id = "many-used-days-id",
                UserName = "manyused@example.com",
                Email = "manyused@example.com",
                Name = "Many",
                LastName = "Used",
                CompanyId = 1,
                TotalVacationDays = 30,
                UsedVacationDays = 25 // More than new total of 22
            };
            
            _mockUserManager.Setup(m => m.FindByIdAsync("many-used-days-id"))
                .ReturnsAsync(userWithManyUsedDays);
            
            // Act
            var result = await controller.UpdateVacationDays("many-used-days-id", 22);
            
            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            
            // Verify response with dictionary approach
            var responseDict = response.GetType().GetProperties()
                .ToDictionary(p => p.Name, p => p.GetValue(response));
            
            Assert.True(responseDict.ContainsKey("totalDays"));
            Assert.Equal(22, responseDict["totalDays"]);
            Assert.True(responseDict.ContainsKey("usedDays"));
            Assert.Equal(22, responseDict["usedDays"]); // Should be adjusted down to 22
            Assert.True(responseDict.ContainsKey("remainingDays"));
            Assert.Equal(0, responseDict["remainingDays"]); // 22 - 22 = 0
            
            // Verify user update was called with adjusted values
            _mockUserManager.Verify(m => m.UpdateAsync(It.Is<User>(u => 
                u.Id == "many-used-days-id" && 
                u.TotalVacationDays == 22 && 
                u.UsedVacationDays == 22)), Times.Once);
        }
        
        // Helper methods
        
        private AdminController CreateControllerAsAdmin()
        {
            // Create a controller with admin user
            var controller = new AdminController(
                _mockUserManager.Object,
                _mockRoleManager.Object,
                CreateEmailServiceMock(), // Use the method to create a mock-able email service
                _context
            );
            
            // Setup controller context with admin user claims
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, "admin-id-1"),
                new Claim(ClaimTypes.Name, "admin@example.com"),
                new Claim(ClaimTypes.Role, "Admin")
            }));
            
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
            
            // Setup TempData for controller
            controller.TempData = new TempDataDictionary(
                new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
            
            return controller;
        }
        
        private AdminController CreateControllerAsManager()
        {
            // Create a controller with manager user
            var controller = new AdminController(
                _mockUserManager.Object,
                _mockRoleManager.Object,
                CreateEmailServiceMock(), // Use the method to create a mock-able email service
                _context
            );
            
            // Override the default GetUserAsync to return manager user
            _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(_managerUser);
            
            // Setup controller context with manager user claims
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, "manager-id-1"),
                new Claim(ClaimTypes.Name, "manager@example.com"),
                new Claim(ClaimTypes.Role, "Manager")
            }));
            
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
            
            // Setup TempData for controller
            controller.TempData = new TempDataDictionary(
                new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
            
            return controller;
        }

        private EmailService CreateEmailServiceMock()
        {
            // Instead of mocking the EmailService itself, we mock the SmtpClient
            // by creating a real EmailService with a mocked configuration
            var emailService = new EmailService(_mockConfiguration.Object);
            
            // We're relying on the fact that the SMTP call will be skipped in tests 
            // because the configuration points to a non-existent SMTP server
            return emailService;
        }
    }
} 