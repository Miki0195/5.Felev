﻿using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Managly.Models;
using Managly.Models.Enums;
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
    private readonly RoleManager<IdentityRole> _roleManager;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, UserManager<User> userManager, SignInManager<User> signInManager, RoleManager<IdentityRole> roleManager)
    {
        _logger = logger;
        _context = context;
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
    }

    [Authorize]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction("Login");
        }

        if (await _userManager.IsInRoleAsync(user, "Owner"))
        {
            return RedirectToAction("Dashboard", "Owner");
        }

        var fullName = !string.IsNullOrEmpty(user.Name) && !string.IsNullOrEmpty(user.LastName)
            ? $"{user.Name} {user.LastName}"
            : user.Email?.Split('@')[0] ?? "User";

        ViewData["UserFullName"] = fullName;
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

    public IActionResult Login()
    {
        if (User.Identity.IsAuthenticated)
        {
            return RedirectToAction("Index", "Home");
        }

        var model = new Login();
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

                if (await _userManager.IsInRoleAsync(user, "Owner"))
                {
                    return RedirectToAction("Dashboard", "Owner");
                }

                return RedirectToAction("Index", "Home");
            }
        }
        ViewBag.ErrorMessage = "Email or password is incorrect.";
        return View(new Login());
    }

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

        var licenseKey = _context.LicenseKeys.FirstOrDefault(k => k.Key == model.LicenseKey && k.Status == LicensekeyStatus.Available);
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

        licenseKey.Status = LicensekeyStatus.Active;
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
            UserName = model.AdminEmail, 
            CompanyId = company.Id,
            LastName = "",
            Country = "",
            City = "",
            Address = "",
            DateOfBirth = new DateTime(2000, 1, 1),
            Gender = "Other",
            ProfilePicturePath = "/images/default/default-profile.png",
            CreatedDate = DateTime.Now
        };

        var result = await _userManager.CreateAsync(adminUser, model.Password);

        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(adminUser, "Admin");
            await _signInManager.SignInAsync(adminUser, isPersistent: false);

            _context.OwnerActivityLogs.Add(new OwnerActivityLog
            {
                ActivityType = "CompanyRegistered",
                Description = $"New company '{company.Name}' registered with license key '{model.LicenseKey}'",
                CompanyName = company.Name,
                CompanyId = company.Id
            });
            await _context.SaveChangesAsync();

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

    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        HttpContext.Session.Clear();
        
        return RedirectToAction("Login", "Home");
    }

}

