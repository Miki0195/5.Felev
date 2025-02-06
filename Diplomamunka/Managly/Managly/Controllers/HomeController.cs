using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Managly.Models;
using Managly.Data;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;

namespace Managly.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;


    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, UserManager<User> userManager, SignInManager<User> signInManager)
    {
        _logger = logger;
        _context = context;
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [Authorize]
    public IActionResult Index()
    {
        return View();
    }

    [Authorize]
    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    // -------------------------- LOGIN -------------------------- // 
    public IActionResult Login()
    {
        var model = new Login
        {
            Email = Request.Cookies["RememberMe_Email"] ?? "",
            Password = Request.Cookies["RememberMe_Password"] ?? "",
            RememberMe = Request.Cookies["RememberMe_Ticked"] == "true"
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Login(Login model)
    {
        if (model == null || string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Password))
        {
            return View(new Login()); 
        }

        var user = await _userManager.FindByEmailAsync(model.Email);

        if (user != null)
        {
            var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, false);

            if (result.Succeeded)
            {
                if (user.IsUsingPreGeneratedPassword) 
                {
                    return RedirectToAction("ChangePassword", "Account");
                }

                if (model.RememberMe)
                {
                    Response.Cookies.Append("RememberMe_Email", model.Email, new CookieOptions
                    {
                        Expires = DateTime.UtcNow.AddDays(30), 
                        HttpOnly = true, 
                        Secure = true, 
                        IsEssential = true
                    });

                    Response.Cookies.Append("RememberMe_Password", model.Password, new CookieOptions
                    {
                        Expires = DateTime.UtcNow.AddDays(30),
                        HttpOnly = true,
                        Secure = true,
                        IsEssential = true
                    });

                    Response.Cookies.Append("RememberMe_Ticked", "true", new CookieOptions
                    {
                        Expires = DateTime.UtcNow.AddDays(30),
                        HttpOnly = true,
                        Secure = true,
                        IsEssential = true
                    });
                }
                else
                {
                    Response.Cookies.Delete("RememberMe_Email");
                    Response.Cookies.Delete("RememberMe_Password");
                    Response.Cookies.Delete("RememberMe_Ticked");
                }

                return RedirectToAction("Index", "Home");
            }
        }
        ViewBag.ErrorMessage = "Email or password is incorrect.";
        return View(new Login());
    }

    // -------------------------- REGISTER -------------------------- //
    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(Register model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (_context.Companies.Any(c => c.Name == model.CompanyName))
        {
            ModelState.AddModelError("CompanyName", "This company name is already taken.");
            return View(model);
        }

        if (_context.Users.Any(u => u.Email == model.AdminEmail))
        {
            ModelState.AddModelError("AdminEmail", "This email address is already registered.");
            return View(model);
        }

        var licenseKey = _context.LicenseKeys.FirstOrDefault(k => k.Key == model.LicenseKey && !k.IsActive);
        if (licenseKey == null)
        {
            ModelState.AddModelError("LicenseKey", "Invalid or inactive license key.");
            return View(model);
        }

        if (!Regex.IsMatch(model.Password, @"^(?=.*[A-Z])(?=.*[0-9])(?=.*[!@#$%^&*.]).{8,}$"))
        {
            ModelState.AddModelError("Password", "Password must be at least 8 characters long and contain at least one uppercase letter, one number, and one special character.");
            return View(model);
        }

        if (model.Password != model.ConfirmPassword)
        {
            ModelState.AddModelError("ConfirmPassword", "Passwords do not match.");
            return View(model);
        }

        var company = new Company
        {
            Name = model.CompanyName,
            LicenseKey = model.LicenseKey,
            CreatedDate = DateTime.Now
        };
        _context.Companies.Add(company);
        await _context.SaveChangesAsync();

        licenseKey.IsActive = true;
        licenseKey.AssignedToCompanyId = company.Id;
        await _context.SaveChangesAsync();

        var roleManager = HttpContext.RequestServices.GetRequiredService<RoleManager<IdentityRole>>();
        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new IdentityRole("Admin"));
        }

        var adminUser = new User
        {
            Name = model.AdminEmail.Split('@')[0],
            Email = model.AdminEmail,
            UserName = model.AdminEmail, // Required by Identity
            CompanyId = company.Id,
            LastName = "",
            Country = "",
            City = "",
            Address = "",
            DateOfBirth = new DateTime(2000, 1, 1),
            Gender = "Other",
            ProfilePicturePath = "/images/default/default-profile.png"
        };

        var result = await _userManager.CreateAsync(adminUser, model.Password);

        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(adminUser, "Admin");
            await _signInManager.SignInAsync(adminUser, isPersistent: false);
            return RedirectToAction("Index", "Home");
        }
        else
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        return View(model);
    }

    // -------------------------- LOGOUT -------------------------- //
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        HttpContext.Session.Clear();

        if (Request.Cookies["RememberMe_Ticked"] != "true")
        {
            Response.Cookies.Delete("RememberMe_Email");
            Response.Cookies.Delete("RememberMe_Password");
            Response.Cookies.Delete("RememberMe_Ticked");
        }

        return RedirectToAction("Login", "Home");
    }

}

