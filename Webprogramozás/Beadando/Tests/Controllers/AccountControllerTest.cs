using System;
using Beadando.Controllers;
using Beadando.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace Tests.Controllers
{
	public class AccountControllerTest
	{
        private static Mock<UserManager<ApplicationUser>> MockUserManager()
        {
            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            return new Mock<UserManager<ApplicationUser>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null
            );
        }   

        private static Mock<SignInManager<ApplicationUser>> MockSignInManager()
        {
            var userManagerMock = MockUserManager();
            return new Mock<SignInManager<ApplicationUser>>(userManagerMock.Object,
                                                            Mock.Of<IHttpContextAccessor>(),
                                                            Mock.Of<IUserClaimsPrincipalFactory<ApplicationUser>>(),
                                                            null, null, null, null);
        }


        [Fact]
        public void Login_ReturnsViewResult()
        {
            var userManagerMock = MockUserManager();
            var signInManagerMock = MockSignInManager();
            var controller = new AccountController(userManagerMock.Object, signInManagerMock.Object);

            var result = controller.Login();

            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public void Register_ReturnsViewResult()
        {
            var userManagerMock = MockUserManager();
            var signInManagerMock = MockSignInManager();
            var controller = new AccountController(userManagerMock.Object, signInManagerMock.Object);

            var result = controller.Register();

            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task Login_Post_ReturnsRedirectToHome_WhenSuccessful()
        {
            var userManagerMock = MockUserManager();
            var signInManagerMock = MockSignInManager();
            var model = new LoginViewModel { Email = "test@test.com", Password = "Password123!", RememberMe = false };

            var user = new ApplicationUser { UserName = "testuser", Email = "test@test.com" };
            userManagerMock.Setup(um => um.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
            userManagerMock.Setup(um => um.GetRolesAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(new List<string> { "User" });
            signInManagerMock.Setup(sm => sm.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), false, false))
                             .ReturnsAsync(SignInResult.Success);

            var controller = new AccountController(userManagerMock.Object, signInManagerMock.Object);

            var result = await controller.Login(model);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Home", redirectResult.ControllerName);
        }

        [Fact]
        public async Task Logout_Post_RedirectsToHome()
        {
            var userManagerMock = MockUserManager();
            var signInManagerMock = MockSignInManager();

            signInManagerMock.Setup(sm => sm.SignOutAsync()).Returns(Task.CompletedTask);

            var controller = new AccountController(userManagerMock.Object, signInManagerMock.Object);

            var result = await controller.Logout();

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Home", redirectResult.ControllerName);
        }

    }
}

