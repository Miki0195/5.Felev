using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Managly.Controllers;
using Managly.Data;
using Managly.Helpers;
using Managly.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using System.IO;

namespace Managly.Tests.Controllers
{
    public class AccountControllerTests
    {
        // Use real DbContext with in-memory database
        private readonly ApplicationDbContext _context;
        private readonly Mock<UserManager<User>> _mockUserManager;
        private readonly ClaimsPrincipal _testUserClaimsPrincipal;

        private readonly User _testUser;
        private readonly User _adminUser;
        private readonly List<ProjectMember> _testProjectMembers;
        private readonly List<Project> _testProjects;

        public AccountControllerTests()
        {
            // Initialize test data
            _testUser = new User
            {
                Id = "user-id-1",
                UserName = "testuser@example.com",
                Email = "testuser@example.com",
                Name = "Test",
                LastName = "User",
                Country = "Test Country",
                City = "Test City",
                Address = "Test Address",
                DateOfBirth = new DateTime(1990, 1, 1),
                Gender = "Male",
                ProfilePicturePath = "/images/default/default-profile.png",
                CompanyId = 1,
                IsUsingPreGeneratedPassword = true
            };

            _adminUser = new User
            {
                Id = "admin-id-1",
                UserName = "admin@example.com",
                Email = "admin@example.com",
                Name = "Admin",
                LastName = "User",
                CompanyId = 1,
                IsUsingPreGeneratedPassword = false,
                Country = "Test Country",
                City = "Test City",
                Address = "Test Address"
            };

            _testProjects = new List<Project>
            {
                new Project
                {
                    Id = 1,
                    Name = "Test Project 1",
                    Status = "In Progress",
                    Priority = "High",
                    CompanyId = 1,
                    CompletedTasks = 5,
                    TotalTasks = 10,
                    Description = "Test Project 1 Description",
                    CreatedById = "admin-id-1",
                    StartDate = DateTime.Now
                },
                new Project
                {
                    Id = 2,
                    Name = "Test Project 2",
                    Status = "In Progress",
                    Priority = "Medium",
                    CompanyId = 1,
                    CompletedTasks = 2,
                    TotalTasks = 8,
                    Description = "Test Project 2 Description",
                    CreatedById = "admin-id-1",
                    StartDate = DateTime.Now
                }
            };

            _testProjectMembers = new List<ProjectMember>
            {
                new ProjectMember
                {
                    Id = 1,
                    UserId = "user-id-1",
                    ProjectId = 1,
                    Role = "Developer"
                },
                new ProjectMember
                {
                    Id = 2,
                    UserId = "user-id-1",
                    ProjectId = 2,
                    Role = "Tester"
                }
            };

            // Set up in-memory database with validation turned off for tests
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .EnableSensitiveDataLogging()
                .Options;

            _context = new ApplicationDbContext(options);

            // Add test data to in-memory database
            _context.Users.AddRange(_testUser, _adminUser);
            _context.Projects.AddRange(_testProjects);
            _context.ProjectMembers.AddRange(_testProjectMembers);
            _context.SaveChanges();

            // Setup UserManager mock
            var userStoreMock = new Mock<IUserStore<User>>();
            _mockUserManager = new Mock<UserManager<User>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null);

            _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(_testUser);

            _mockUserManager.Setup(m => m.FindByIdAsync("user-id-1"))
                .ReturnsAsync(_testUser);

            _mockUserManager.Setup(m => m.FindByIdAsync("admin-id-1"))
                .ReturnsAsync(_adminUser);

            _mockUserManager.Setup(m => m.RemovePasswordAsync(It.IsAny<User>()))
                .ReturnsAsync(IdentityResult.Success);

            _mockUserManager.Setup(m => m.AddPasswordAsync(It.IsAny<User>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);

            _mockUserManager.Setup(m => m.UpdateAsync(It.IsAny<User>()))
                .ReturnsAsync(IdentityResult.Success);

            _mockUserManager.Setup(m => m.GetPhoneNumberAsync(It.IsAny<User>()))
                .ReturnsAsync("+36123456789");

            _mockUserManager.Setup(m => m.SetPhoneNumberAsync(It.IsAny<User>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);

            _mockUserManager.Setup(m => m.IsInRoleAsync(_testUser, "Employee"))
                .ReturnsAsync(true);

            _mockUserManager.Setup(m => m.IsInRoleAsync(_testUser, "Admin"))
                .ReturnsAsync(false);

            _mockUserManager.Setup(m => m.IsInRoleAsync(_testUser, "Manager"))
                .ReturnsAsync(false);

            _mockUserManager.Setup(m => m.IsInRoleAsync(_adminUser, "Admin"))
                .ReturnsAsync(true);

            // Create claims principal for test user
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "user-id-1"),
                new Claim(ClaimTypes.Name, "testuser@example.com")
            };
            var identity = new ClaimsIdentity(claims, "Test");
            _testUserClaimsPrincipal = new ClaimsPrincipal(identity);
        }

        [Fact]
        public async Task ChangePassword_Get_ReturnsView()
        {
            // Arrange
            var controller = CreateController();

            // Act
            var result = controller.ChangePassword();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.ViewName); // Default view
        }

        [Fact]
        public async Task ChangePassword_Post_ValidModel_ChangesPasswordAndRedirects()
        {
            // Arrange
            var controller = CreateController();
            var model = new ChangePassword
            {
                NewPassword = "NewPassword123!",
                ConfirmPassword = "NewPassword123!"
            };

            // Act
            var result = await controller.ChangePassword(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Home", redirectResult.ControllerName);

            // Verify that the password was changed
            Assert.False(_testUser.IsUsingPreGeneratedPassword);
            _mockUserManager.Verify(m => m.RemovePasswordAsync(_testUser), Times.Once);
            _mockUserManager.Verify(m => m.AddPasswordAsync(_testUser, "NewPassword123!"), Times.Once);
        }

        [Fact]
        public async Task ChangePassword_Post_InvalidModel_ReturnsViewWithModel()
        {
            // Arrange
            var controller = CreateController();
            var model = new ChangePassword
            {
                NewPassword = "weak",
                ConfirmPassword = "weak"
            };
            controller.ModelState.AddModelError("NewPassword", "Password does not meet requirements");

            // Act
            var result = await controller.ChangePassword(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var returnedModel = Assert.IsType<ChangePassword>(viewResult.Model);
            Assert.Equal(model.NewPassword, returnedModel.NewPassword);
        }

        [Fact]
        public async Task ChangePassword_Post_PasswordDoesNotMeetComplexity_ReturnsViewWithError()
        {
            // Arrange
            var controller = CreateController();
            var model = new ChangePassword
            {
                NewPassword = "weak",
                ConfirmPassword = "weak"
            };

            // Act
            var result = await controller.ChangePassword(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.True(controller.ModelState.ContainsKey("NewPassword"));
            Assert.Contains("complexity requirements", controller.ModelState["NewPassword"].Errors.First().ErrorMessage);
        }

        [Fact]
        public async Task Profile_Get_ReturnsViewWithUserData()
        {
            // Arrange
            var controller = CreateController();

            // Act
            var result = await controller.Profile();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<UserProfile>(viewResult.Model);

            // Verify model properties match user data
            Assert.Equal(_testUser.Name, model.Name);
            Assert.Equal(_testUser.LastName, model.LastName);
            Assert.Equal(_testUser.Email, model.Email);
            Assert.Equal("+36", model.SelectedCountryCode);
            Assert.Equal("123456789", model.PhoneNumber);
            Assert.Equal(_testUser.Country, model.Country);
            Assert.Equal(_testUser.City, model.City);
            Assert.Equal(_testUser.Address, model.Address);
            Assert.Equal(_testUser.DateOfBirth, model.DateOfBirth);
            Assert.Equal(_testUser.Gender, model.Gender);
            Assert.Equal(_testUser.ProfilePicturePath, model.ProfilePicturePath);
            Assert.Equal("Employee", model.Rank);

            // Verify that projects are loaded
            Assert.Equal(2, model.EnrolledProjects.Count);
        }

        [Fact]
        public async Task Profile_Post_ValidModel_UpdatesUserAndReturnsView()
        {
            // Arrange
            var controller = CreateController();
            var model = new UserProfile
            {
                Name = "Updated Name",
                LastName = "Updated LastName",
                Email = "testuser@example.com",
                SelectedCountryCode = "+36",
                PhoneNumber = "987654321",
                Country = "Updated Country",
                City = "Updated City",
                Address = "Updated Address",
                DateOfBirth = new DateTime(1991, 2, 2),
                Gender = "Female"
            };

            // Act
            var result = await controller.Profile(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var returnedModel = Assert.IsType<UserProfile>(viewResult.Model);

            _mockUserManager.Verify(m => m.SetPhoneNumberAsync(_testUser, "+36987654321"), Times.Once);
            _mockUserManager.Verify(m => m.UpdateAsync(_testUser), Times.Once);

            // Verify user properties were updated
            Assert.Equal("Updated Name", _testUser.Name);
            Assert.Equal("Updated LastName", _testUser.LastName);
            Assert.Equal("Updated Country", _testUser.Country);
            Assert.Equal("Updated City", _testUser.City);
            Assert.Equal("Updated Address", _testUser.Address);
            Assert.Equal(new DateTime(1991, 2, 2), _testUser.DateOfBirth);
            Assert.Equal("Female", _testUser.Gender);
        }

        [Fact]
        public async Task Profile_Post_InvalidProfilePictureType_ReturnsViewWithError()
        {
            // Arrange
            var controller = CreateController();

            // Create test file with invalid content type
            var fileName = "test-file.txt";
            var content = "fake text content";
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(content);
            writer.Flush();
            stream.Position = 0;

            var file = new FormFile(stream, 0, stream.Length, "ProfilePicture", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "text/plain"
            };

            var model = new UserProfile
            {
                Name = _testUser.Name,
                LastName = _testUser.LastName,
                Email = _testUser.Email,
                SelectedCountryCode = "+36",
                PhoneNumber = "123456789",
                Country = _testUser.Country,
                City = _testUser.City,
                Address = _testUser.Address,
                DateOfBirth = _testUser.DateOfBirth,
                Gender = _testUser.Gender,
                ProfilePicture = file
            };

            // Act
            var result = await controller.Profile(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.True(controller.ModelState.ContainsKey("ProfilePicture"));
            Assert.Contains("image files", controller.ModelState["ProfilePicture"].Errors.First().ErrorMessage);
        }

        [Fact]
        public async Task Profile_Post_TooBigProfilePicture_ReturnsViewWithError()
        {
            // Arrange
            var controller = CreateController();

            // Mock a large file
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(6 * 1024 * 1024); // 6MB
            mockFile.Setup(f => f.ContentType).Returns("image/jpeg");

            var model = new UserProfile
            {
                Name = _testUser.Name,
                LastName = _testUser.LastName,
                Email = _testUser.Email,
                SelectedCountryCode = "+36",
                PhoneNumber = "123456789",
                Country = _testUser.Country,
                City = _testUser.City,
                Address = _testUser.Address,
                DateOfBirth = _testUser.DateOfBirth,
                Gender = _testUser.Gender,
                ProfilePicture = mockFile.Object
            };

            // Act
            var result = await controller.Profile(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.True(controller.ModelState.ContainsKey("ProfilePicture"));
            Assert.Contains("less than 5MB", controller.ModelState["ProfilePicture"].Errors.First().ErrorMessage);
        }

        [Fact]
        public async Task Profile_Post_SetPhoneNumberFailed_ReturnsViewWithError()
        {
            // Arrange
            var controller = CreateController();

            // Setup failed phone number update
            _mockUserManager.Setup(m => m.SetPhoneNumberAsync(It.IsAny<User>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Failed to update phone number" }));

            var model = new UserProfile
            {
                Name = _testUser.Name,
                LastName = _testUser.LastName,
                Email = _testUser.Email,
                SelectedCountryCode = "+36",
                PhoneNumber = "987654321",
                Country = _testUser.Country,
                City = _testUser.City,
                Address = _testUser.Address,
                DateOfBirth = _testUser.DateOfBirth,
                Gender = _testUser.Gender
            };

            // Act
            var result = await controller.Profile(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.True(controller.ModelState.ContainsKey("PhoneNumber"));
            Assert.Contains("Failed to update phone number", controller.ModelState["PhoneNumber"].Errors.First().ErrorMessage);
        }

        [Fact]
        public void AccessDenied_ReturnsView()
        {
            // Arrange
            var controller = CreateController();

            // Act
            var result = controller.AccessDenied();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.ViewName); // Default view
        }

        [Fact]
        public void AccountDeleted_ReturnsView()
        {
            // Arrange
            var controller = CreateController();

            // Act
            var result = controller.AccountDeleted();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.ViewName); // Default view
        }

        private AccountController CreateController()
        {
            // Create controller with mocked dependencies
            var controller = new AccountController(
                _mockUserManager.Object,
                _context
            );

            // Setup controller context
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = _testUserClaimsPrincipal }
            };

            return controller;
        }
    }
}