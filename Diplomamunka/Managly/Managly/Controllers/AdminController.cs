using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Managly.Data;
using Managly.Models;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System;

namespace Managly.Controllers
{
    /// <summary>
    /// Controller responsible for administrative functions including user management and profiles
    /// </summary>
    [Authorize]
    public class AdminController : Controller
    {
        #region Private Members

        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly EmailService _emailService;
        private readonly ApplicationDbContext _context;

        #endregion

        #region Constructor

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

        #endregion

        #region Profile Management Actions

        /// <summary>
        /// Displays the form to create a new user profile
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult CreateProfile()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateProfile(CreateProfile model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please fill in all required fields correctly.";
                return View(model);
            }

            try
            {
                var normalizedEmail = _userManager.NormalizeEmail(model.Email);
                var existingUser = await _userManager.FindByEmailAsync(normalizedEmail);

                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "This email address is already registered.");
                    TempData["ErrorMessage"] = "This email address is already registered.";
                    return View(model);
                }

                var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
                var adminUser = await _userManager.FindByIdAsync(adminUserId);

                if (adminUser == null)
                {
                    TempData["ErrorMessage"] = "Admin user not found.";
                    return RedirectToAction("Login", "Home");
                }

                var company = await _context.Companies
                    .FirstOrDefaultAsync(c => c.Id == adminUser.CompanyId);

                var senderName = company?.Name ?? "Unknown Company";
                var randomPassword = GenerateRandomPassword();

                var newUser = new User
                {
                    Name = model.Name,
                    LastName = model.LastName,
                    Email = model.Email,
                    UserName = model.Email,
                    CompanyId = adminUser.CompanyId,
                    IsUsingPreGeneratedPassword = true,
                    Country = "",
                    City = "",
                    Address = "",
                    DateOfBirth = new DateTime(2000, 1, 1),
                    Gender = "Other",
                    ProfilePicturePath = "/images/default/default-profile.png",
                    TotalVacationDays = 20,
                    UsedVacationDays = 0,
                    VacationYear = DateTime.Now.Year,
                    EmailConfirmed = true 
                };

                var result = await _userManager.CreateAsync(newUser, randomPassword);

                if (result.Succeeded)
                {
                    var roleResult = await _userManager.AddToRoleAsync(newUser, model.Role);

                    if (!roleResult.Succeeded)
                    {
                        var errorMessage = string.Join(", ", roleResult.Errors.Select(e => e.Description));

                        await _userManager.DeleteAsync(newUser);

                        TempData["ErrorMessage"] = $"User created but role assignment failed: {errorMessage}";
                        return View(model);
                    }

                    try
                    {
                        await SendEmailAsync(
                            toEmail: model.Email,
                            subject: "Your Account Has Been Created",
                            body: $"Welcome to {senderName}! Your temporary password is: {randomPassword}",
                            senderEmail: adminUser.Email ?? "",
                            senderName: senderName
                        );

                        TempData["SuccessMessage"] = $"User {model.Email} has been successfully created!";
                        ModelState.Clear();
                        return View(new CreateProfile());
                    }
                    catch (Exception emailEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Email sending failed: {emailEx.Message}");
                        TempData["SuccessMessage"] = $"User {model.Email} created successfully but email notification failed.";
                        ModelState.Clear();
                        return View(new CreateProfile());
                    }
                }
                else
                {
                    var errorMessage = string.Join(", ", result.Errors.Select(e => e.Description));

                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    TempData["ErrorMessage"] = $"Failed to create the user: {errorMessage}";
                    return View(model);
                }
            }
            catch (DbUpdateConcurrencyException concurrencyEx)
            {
                System.Diagnostics.Debug.WriteLine($"Concurrency exception: {concurrencyEx.Message}");
                TempData["ErrorMessage"] = "A database concurrency issue occurred. Please try again.";
                return View(model);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception in CreateProfile: {ex.Message}");
                TempData["ErrorMessage"] = $"An unexpected error occurred: {ex.Message}";
                return View(model);
            }
        }


        #endregion

        #region User Management Actions

        /// <summary>
        /// Displays the user management dashboard
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> UserManagement()
        {
            var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new Exception("AdminUserId was not found!");
            var adminUser = await _userManager.FindByIdAsync(adminUserId) ?? throw new Exception("AdminUser was not found!");

            if (adminUserId == null || adminUser.CompanyId == null)
            {
                return RedirectToAction("Login", "Home");
            }

            var usersInCompany = await _userManager.Users
                .Where(u => u.CompanyId == adminUser.CompanyId && u.Id != adminUserId)
                .ToListAsync();

            var userRoles = new List<UserManagement>();
            var availableRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            var userRole = await _userManager.GetRolesAsync(adminUser);
            var currentUserRole = userRole.FirstOrDefault() ?? "Employee";
            await CheckAndResetVacationDaysForNewYear(usersInCompany);

            foreach (var user in usersInCompany)
            {
                var roles = await _userManager.GetRolesAsync(user);
                string role = roles.FirstOrDefault() ?? "Employee";

                // Only get active projects
                var projects = await _context.ProjectMembers
                    .Where(pm => pm.UserId == user.Id)
                    .Include(pm => pm.Project)
                    .Where(pm => pm.Project.Status != "Completed" && pm.Project.Status != "Not started") 
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
                    Email = user.Email ?? "default@default.com",
                    Roles = role,
                    AssignedProjects = projects,
                    PhoneNumber = user.PhoneNumber ?? "301234567",
                    Country = user.Country,
                    City = user.City,
                    Address = user.Address,
                    DateOfBirth = user.DateOfBirth,
                    Gender = user.Gender,
                    ProfilePicturePath = user.ProfilePicturePath,
                    TotalVacationDays = user.TotalVacationDays,
                    UsedVacationDays = user.UsedVacationDays,
                    RemainingVacationDays = user.RemainingVacationDays,
                    VacationYear = user.VacationYear
                });
            }

            ViewData["UserRole"] = currentUserRole;
            ViewData["AvailableRoles"] = availableRoles;
            ViewData["CurrentYear"] = DateTime.Now.Year;

            return View(userRoles);
        }

        /// <summary>
        /// Retrieves user details for editing
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> GetUserDetails(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest(new { success = false, message = "User ID is required" });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { success = false, message = "User not found" });
            }

            var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new Exception("AdminUserId was not found!");
            var adminUser = await _userManager.FindByIdAsync(adminUserId);

            if (adminUser == null || adminUser.CompanyId != user.CompanyId)
            {
                return Unauthorized(new { success = false, message = "Unauthorized access" });
            }

            var roles = await _userManager.GetRolesAsync(user);

            var userDetails = new
            {
                userId = user.Id,
                name = user.Name,
                lastName = user.LastName,
                email = user.Email,
                roles = roles.FirstOrDefault() ?? "Employee",
                phoneNumber = user.PhoneNumber,
                country = user.Country,
                city = user.City,
                address = user.Address,
                dateOfBirth = user.DateOfBirth,
                gender = user.Gender,
                totalVacationDays = user.TotalVacationDays,
                usedVacationDays = user.UsedVacationDays,
                remainingVacationDays = user.RemainingVacationDays,
                success = true
            };

            return Ok(userDetails);
        }

        /// <summary>
        /// Deletes a user from the system
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest(new { success = false, message = "User ID is required" });
            }

            var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new Exception("AdminUserId was not found!");
            var adminUser = await _userManager.FindByIdAsync(adminUserId);

            if (adminUser == null || adminUser.CompanyId == null)
            {
                return Unauthorized(new { success = false, message = "Unauthorized access." });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { success = false, message = "User not found." });
            }

            if (user.CompanyId != adminUser.CompanyId)
            {
                return Unauthorized(new { success = false, message = "You cannot delete users outside your company." });
            }

            try
            {
                var projectMembers = await _context.ProjectMembers
                    .Where(pm => pm.UserId == userId)
                    .ToListAsync();
                    
                if (projectMembers.Any())
                {
                    _context.ProjectMembers.RemoveRange(projectMembers);
                    await _context.SaveChangesAsync();
                }

                var leaveRequests = await _context.Leaves
                    .Where(lr => lr.UserId == userId)
                    .ToListAsync();
                    
                if (leaveRequests.Any())
                {
                    _context.Leaves.RemoveRange(leaveRequests);
                    await _context.SaveChangesAsync();
                }

                var result = await _userManager.DeleteAsync(user);

                if (result.Succeeded)
                {
                    return Ok(new { success = true, message = $"User {user.Name} has been successfully deleted." });
                }
                else
                {
                    string errorMessage = string.Join(", ", result.Errors.Select(e => e.Description));
                    return BadRequest(new { success = false, message = "Failed to delete the user: " + errorMessage });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while deleting the user: " + ex.Message });
            }
        }

        /// <summary>
        /// Updates a user's role
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateUserRole(string userId, string role)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(role))
            {
                return BadRequest(new { success = false, message = "User ID and role are required." });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { success = false, message = "User not found." });
            }

            var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new Exception("AdminUserId was not found!");
            var adminUser = await _userManager.FindByIdAsync(adminUserId);

            if (adminUser == null || adminUser.CompanyId != user.CompanyId)
            {
                return Unauthorized(new { success = false, message = "Unauthorized access" });
            }

            if (!await _roleManager.RoleExistsAsync(role))
            {
                return BadRequest(new { success = false, message = "Invalid role specified." });
            }

            try
            {
                var currentRoles = await _userManager.GetRolesAsync(user);
                if (currentRoles.Any())
                {
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);
                }

                var result = await _userManager.AddToRoleAsync(user, role);

                if (result.Succeeded)
                {
                    await _context.SaveChangesAsync();

                    return Ok(new
                    {
                        success = true,
                        message = $"Role for {user.Name} has been updated to {role}.",
                        userId = userId,
                        newRole = role
                    });
                }
                else
                {
                    string errorMessage = string.Join(", ", result.Errors.Select(e => e.Description));
                    return BadRequest(new { success = false, message = "Failed to update role: " + errorMessage });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while updating the role: " + ex.Message });
            }
        }

        /// <summary>
        /// Updates a user's vacation days allocation
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> UpdateVacationDays(string userId, int totalVacationDays)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest(new { success = false, message = "User ID is required." });
            }

            if (totalVacationDays < 20)
            {
                return BadRequest(new { success = false, message = "Total vacation days must be at least 20." });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { success = false, message = "User not found." });
            }

            var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new Exception("AdminUserId was not found!");
            var adminUser = await _userManager.FindByIdAsync(adminUserId);

            if (adminUser == null || adminUser.CompanyId != user.CompanyId)
            {
                return Unauthorized(new { success = false, message = "Unauthorized access" });
            }

            try
            {
                user.TotalVacationDays = totalVacationDays;
                
                if (user.UsedVacationDays > totalVacationDays)
                {
                    user.UsedVacationDays = totalVacationDays;
                }

                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    return Ok(new { 
                        success = true, 
                        message = $"Vacation days for {user.Name} updated successfully.",
                        totalDays = user.TotalVacationDays,
                        usedDays = user.UsedVacationDays,
                        remainingDays = user.RemainingVacationDays
                    });
                }
                else
                {
                    string errorMessage = string.Join(", ", result.Errors.Select(e => e.Description));
                    return BadRequest(new { success = false, message = "Failed to update vacation days: " + errorMessage });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while updating vacation days: " + ex.Message });
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Checks if the vacation year needs to be reset and resets vacation days if needed
        /// </summary>
        private async Task CheckAndResetVacationDaysForNewYear(List<User> users)
        {
            int currentYear = DateTime.Now.Year;
            bool anyChanges = false;

            foreach (var user in users)
            {
                if (user.VacationYear < currentYear)
                {
                    user.VacationYear = currentYear;
                    user.UsedVacationDays = 0;
                    anyChanges = true;
                }
            }

            if (anyChanges)
            {
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Generates a secure random password
        /// </summary>
        private string GenerateRandomPassword()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 12).Select(s => s[random.Next(s.Length)]).ToArray());
        }

        /// <summary>
        /// Sends an email with SMTP
        /// </summary>
        private async Task SendEmailAsync(string toEmail, string subject, string body, string senderEmail, string senderName)
        {
            await _emailService.SendEmailWithSmtpAsync(toEmail, subject, body, senderEmail, senderName);
        }

        #endregion
    }
}

