using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using Managly.Models;
using Managly.Data;
using System.Security.Claims;
using Managly.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Managly.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly ApplicationDbContext _context;

        public AccountController(UserManager<User> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        [HttpGet]
        [Authorize(Policy = "RequirePreGeneratedPassword")]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Policy = "RequirePreGeneratedPassword")]
        public async Task<IActionResult> ChangePassword(ChangePassword model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); 
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return RedirectToAction("Login", "Home");
            }

            if (!Regex.IsMatch(model.NewPassword, @"^(?=.*[A-Z])(?=.*[0-9])(?=.*[!@#$%^&*.]).{8,}$"))
            {
                ModelState.AddModelError("NewPassword", "Password must meet complexity requirements.");
                return View(model);
            }

            var removePasswordResult = await _userManager.RemovePasswordAsync(user);
            if (!removePasswordResult.Succeeded)
            {
                ModelState.AddModelError(string.Empty, "Failed to reset the password.");
                return View(model);
            }

            var addPasswordResult = await _userManager.AddPasswordAsync(user, model.NewPassword);
            if (addPasswordResult.Succeeded)
            {
                user.IsUsingPreGeneratedPassword = false; 
                await _userManager.UpdateAsync(user);

                return RedirectToAction("Index", "Home");
            }

            foreach (var error in addPasswordResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return RedirectToAction("Login", "Home");
            }

            string fullPhoneNumber = await _userManager.GetPhoneNumberAsync(user) ?? "";
            string selectedCountryCode = "+36"; // Default
            string phoneNumberWithoutCode = fullPhoneNumber;

            foreach (var country in CountryCallingCodes.GetCountryCodes())
            {
                if (fullPhoneNumber.StartsWith(country.Code))
                {
                    selectedCountryCode = country.Code;
                    phoneNumberWithoutCode = fullPhoneNumber.Substring(country.Code.Length);
                    break;
                }
            }

            var model = new UserProfile
            {
                Name = user.Name ?? "",
                LastName = user.LastName ?? "",
                Email = user.Email ?? "",
                SelectedCountryCode = selectedCountryCode,
                PhoneNumber = phoneNumberWithoutCode.Trim(), 
                Country = user.Country ?? "",
                City = user.City ?? "",
                Address = user.Address ?? "",
                DateOfBirth = user.DateOfBirth,
                Gender = user.Gender ?? "",
                ProfilePicturePath = user.ProfilePicturePath ?? "/images/default/default-profile.png", 
                GenderOptions = new List<string> { "Male", "Female", "Other" }
            };

            await LoadUserProjects(userId, model);

            return View(model);
        }

        private string GetPriorityClass(string priority)
        {
            return priority?.ToLower() switch
            {
                "high" => "priority-high",
                "medium" => "priority-medium",
                "low" => "priority-low",
                _ => "priority-medium"
            };
        }

        private string GetStatusClass(string status)
        {
            return status?.ToLower() switch
            {
                "completed" => "status-completed",
                "in progress" => "status-inprogress",
                "not started" => "status-notstarted",
                "on hold" => "status-onhold",
                _ => "status-notstarted"
            };
        }

        [HttpPost]
        public async Task<IActionResult> Profile(UserProfile model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return RedirectToAction("Login", "Home");
            }

            if (!ModelState.IsValid)
            {
                model.GenderOptions = new List<string> { "Male", "Female", "Other" };
                model.CountryCodes = CountryCallingCodes.GetCountryCodes();
                await LoadUserProjects(userId, model);
                return View(model);
            }

            try
            {
                user.Name = model.Name;
                user.LastName = model.LastName;
                user.Country = model.Country;
                user.City = model.City;
                user.Address = model.Address;
                user.DateOfBirth = model.DateOfBirth;
                user.Gender = model.Gender;
                
                if (model.ProfilePicture != null && model.ProfilePicture.Length > 0)
                {
                    var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif" };
                    if (!allowedTypes.Contains(model.ProfilePicture.ContentType))
                    {
                        ModelState.AddModelError("ProfilePicture", "Only image files (jpg, png, gif) are allowed");
                        model.GenderOptions = new List<string> { "Male", "Female", "Other" };
                        model.CountryCodes = CountryCallingCodes.GetCountryCodes();
                        await LoadUserProjects(userId, model);
                        return View(model);
                    }

                    if (model.ProfilePicture.Length > 5 * 1024 * 1024)
                    {
                        ModelState.AddModelError("ProfilePicture", "File size should be less than 5MB");
                        model.GenderOptions = new List<string> { "Male", "Female", "Other" };
                        model.CountryCodes = CountryCallingCodes.GetCountryCodes();
                        await LoadUserProjects(userId, model);
                        return View(model);
                    }

                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                    Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = user.Id + Path.GetExtension(model.ProfilePicture.FileName);
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.ProfilePicture.CopyToAsync(stream);
                    }

                    user.ProfilePicturePath = "/uploads/" + uniqueFileName;
                }

                var fullPhoneNumber = model.SelectedCountryCode + model.PhoneNumber;
                var phoneResult = await _userManager.SetPhoneNumberAsync(user, fullPhoneNumber);
                if (!phoneResult.Succeeded)
                {
                    ModelState.AddModelError("PhoneNumber", "Failed to update phone number");
                    model.GenderOptions = new List<string> { "Male", "Female", "Other" };
                    model.CountryCodes = CountryCallingCodes.GetCountryCodes();
                    await LoadUserProjects(userId, model);
                    return View(model);
                }

                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    model.ProfilePicturePath = user.ProfilePicturePath;
                    ViewBag.SuccessMessage = "Your profile has been updated successfully!";
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    model.GenderOptions = new List<string> { "Male", "Female", "Other" };
                    model.CountryCodes = CountryCallingCodes.GetCountryCodes();
                    await LoadUserProjects(userId, model);
                    return View(model);
                }

                await LoadUserProjects(userId, model);
                
                return View(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "An unexpected error occurred: " + ex.Message);
                model.GenderOptions = new List<string> { "Male", "Female", "Other" };
                model.CountryCodes = CountryCallingCodes.GetCountryCodes();
                await LoadUserProjects(userId, model);
                return View(model);
            }
        }

        private async Task LoadUserProjects(string userId, UserProfile model)
        {
            var user = await _userManager.FindByIdAsync(userId);
            string rank = "";
            if (await _userManager.IsInRoleAsync(user, "Employee"))
            {
                rank = "Employee";
            }
            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                rank = "Admin";
            }
            else if (await _userManager.IsInRoleAsync(user, "Manager"))
            {
                rank = "Manager";
            }
            model.Rank = rank;
            
            var userProjects = await _context.ProjectMembers
                .Where(pm => pm.UserId == userId && pm.Project.Status == "In Progress")
                .Include(pm => pm.Project)
                .Select(pm => new 
                {
                    ProjectId = pm.Project.Id,
                    ProjectName = pm.Project.Name ?? "Untitled Project",
                    ProjectStatus = pm.Project.Status ?? "Not Started",
                    ProjectPriority = pm.Project.Priority ?? "Medium",
                    MemberRole = pm.Role ?? "Member",
                    CompletedTasks = pm.Project.CompletedTasks,
                    TotalTasks = pm.Project.TotalTasks > 0 ? pm.Project.TotalTasks : 1
                })
                .ToListAsync();
            
            model.EnrolledProjects = userProjects.Select(p => 
            {
                int progressPercentage = (int)Math.Round((double)p.CompletedTasks / p.TotalTasks * 100);
                
                return new ProjectViewModel
                {
                    Id = p.ProjectId,
                    Name = p.ProjectName,
                    Status = p.ProjectStatus,
                    Priority = p.ProjectPriority,
                    Role = p.MemberRole,
                    PriorityClass = GetPriorityClass(p.ProjectPriority),
                    StatusClass = GetStatusClass(p.ProjectStatus),
                    CompletedTasks = p.CompletedTasks,
                    TotalTasks = p.TotalTasks,
                    ProgressPercentage = progressPercentage
                };
            }).ToList();
        }

        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [AllowAnonymous]
        public IActionResult AccountDeleted()
        {
            return View();
        }
    }
}
