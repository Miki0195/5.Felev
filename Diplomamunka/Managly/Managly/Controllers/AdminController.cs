using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Managly.Data;
using Managly.Models;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SendGrid.Helpers.Mail;

namespace Managly.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly EmailService _emailService;
        private readonly ApplicationDbContext _context;

        public AdminController(
            UserManager<User> userManager, 
            RoleManager<IdentityRole> roleManager,
            EmailService emailService, 
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _emailService = emailService;
            _context = context;
        }

        [HttpGet]
        public IActionResult CreateProfile()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateProfile(CreateProfile model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var normalizedEmail = _userManager.NormalizeEmail(model.Email);
            var existingUser = await _userManager.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "This email address is already registered.");
                return View(model);
            }

            var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier); 
            var adminUser = await _userManager.FindByIdAsync(adminUserId);

            if (adminUser == null)
            {
                return RedirectToAction("Login", "Home");
            }

            var senderName = _context.Companies.FirstOrDefault(c => c.Id == adminUser.CompanyId)?.Name ?? "Unknown Company";

            var randomPassword = GenerateRandomPassword();

            var newUser = new User
            {
                Name = model.Name,
                Email = model.Email,
                UserName = model.Email,
                CompanyId = adminUser.CompanyId,
                IsUsingPreGeneratedPassword = true,
                LastName = "",
                Country = "",
                City = "",
                Address = "",
                DateOfBirth = new DateTime(2000, 1, 1), 
                Gender = "Other", 
                ProfilePicturePath = "/images/default/default-profile.png"
            };

            var result = await _userManager.CreateAsync(newUser, randomPassword);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(newUser, model.Role);

                await SendEmailAsync(
                    toEmail: model.Email,
                    subject: "Your Account Has Been Created",
                    body: $"Welcome to {senderName}! Your temporary password is: {randomPassword}",
                    senderEmail: adminUser.Email, 
                    senderName: senderName        
                );

                TempData["SuccessMessage"] = $"User {model.Email} has been successfully created!";
                ModelState.Clear(); 
                return View(new CreateProfile()); 
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to create the user.";
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        private string GenerateRandomPassword()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 12).Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private async Task SendEmailAsync(string toEmail, string subject, string body, string senderEmail, string senderName)
        {
            await _emailService.SendEmailWithSmtpAsync(toEmail, subject, body, senderEmail, senderName);
        }

        [HttpGet]
        public async Task<IActionResult> UserManagement()
        {
            var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var adminUser = await _userManager.FindByIdAsync(adminUserId);

            if (adminUser == null || adminUser.CompanyId == null)
            {
                return RedirectToAction("Login", "Home");
            }

            var usersInCompany = await _userManager.Users
                .Where(u => u.CompanyId == adminUser.CompanyId && u.Id != adminUserId)
                .ToListAsync();

            var userRoles = new List<UserManagement>();
            var availableRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();

            foreach (var user in usersInCompany)
            {
                var roles = await _userManager.GetRolesAsync(user);
                string role = roles.FirstOrDefault() ?? "Employee";

                var projects = await _context.ProjectMembers
                    .Where(pm => pm.UserId == user.Id)
                    .Include(pm => pm.Project)
                    .Select(pm => new ProjectInfo
                    {
                        ProjectId = pm.ProjectId,
                        ProjectName = pm.Project.Name,
                        Role = pm.Role
                    })
                    .ToListAsync();

                userRoles.Add(new UserManagement
                {
                    UserId = user.Id,
                    Name = user.Name,
                    LastName = user.LastName,
                    Email = user.Email,
                    Roles = role,
                    AssignedProjects = projects,
                    PhoneNumber = user.PhoneNumber,
                    Country = user.Country,
                    City = user.City,
                    Address = user.Address,
                    DateOfBirth = user.DateOfBirth,
                    Gender = user.Gender,
                    ProfilePicturePath = user.ProfilePicturePath
                });
            }

            ViewData["AvailableRoles"] = availableRoles;
            return View(userRoles);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var adminUser = await _userManager.FindByIdAsync(adminUserId);

            if (adminUser == null || adminUser.CompanyId == null)
            {
                return Json(new { success = false, message = "Unauthorized access." });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || user.CompanyId != adminUser.CompanyId)
            {
                return Json(new { success = false, message = "You cannot delete users outside your company." });
            }

            var result = await _userManager.DeleteAsync(user);

            if (result.Succeeded)
            {
                return Json(new { success = true, message = $"User {user.Name} has been successfully deleted." });
            }
            else
            {
                return Json(new { success = false, message = "Failed to delete the user." });
            }
        }

        /// <summary>
        /// Updates a user's role dynamically
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> UpdateUserRole(string userId, string role)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(role))
            {
                return Json(new { success = false, message = "Invalid request data." });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found." });
            }

            // Check if role exists
            if (!await _roleManager.RoleExistsAsync(role))
            {
                return Json(new { success = false, message = "Invalid role specified." });
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            var availableRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            var rolesToRemove = currentRoles.Intersect(availableRoles).ToList();

            if (rolesToRemove.Any())
            {
                await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
            }

            var result = await _userManager.AddToRoleAsync(user, role);

            if (result.Succeeded)
            {
                return Json(new { success = true, message = $"Role for {user.Name} has been updated to {role}." });
            }
            else
            {
                return Json(new { success = false, message = "Failed to update role." });
            }
        }
    }
}

