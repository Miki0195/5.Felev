using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Beadando.Models;

namespace Beadando.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;

        public AccountController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View(); // This will look for Views/Account/Register.cshtml
        }
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new IdentityUser { UserName = model.Email, Email = model.Email };
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    Console.WriteLine($"User {user.Email} registered successfully.");

                    // Assign the "User" role by default
                    var roleResult = await _userManager.AddToRoleAsync(user, "User");

                    if (!roleResult.Succeeded)
                    {
                        foreach (var error in roleResult.Errors)
                        {
                            Console.WriteLine($"Role assignment error: {error.Description}");
                        }
                    }

                    // Automatically sign in the user
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Home");
                }

                foreach (var error in result.Errors)
                {
                    Console.WriteLine($"Registration error: {error.Description}");
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            Console.WriteLine("ModelState is invalid.");
            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View(); // This will look for Views/Account/Register.cshtml
        }
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, false);

                if (result.Succeeded)
                {
                    return RedirectToAction("Index", "Home");
                }

                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}
