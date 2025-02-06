using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using Managly.Models;
using System.Security.Claims;
using Managly.Helpers;

namespace Managly.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly UserManager<User> _userManager;

        public AccountController(UserManager<User> userManager)
        {
            _userManager = userManager;
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
            string selectedCountryCode = "+36"; 
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

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Profile(UserProfile model)
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

            user.Name = model.Name;
            user.LastName = model.LastName;
            user.Country = model.Country;
            user.City = model.City;
            user.Address = model.Address;
            user.DateOfBirth = model.DateOfBirth;
            user.Gender = model.Gender;

            if (model.ProfilePicture != null && model.ProfilePicture.Length > 0)
            {
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
            //else
            //{
            //    model.ProfilePicturePath = user.ProfilePicturePath;
            //}

            var fullPhoneNumber = model.SelectedCountryCode + model.PhoneNumber;
            var phoneResult = await _userManager.SetPhoneNumberAsync(user, fullPhoneNumber);
            if (!phoneResult.Succeeded)
            {
                ViewBag.ErrorMessage = "Failed to update phone number.";
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
                ViewBag.ErrorMessage = "Failed to update profile.";
            }

            return View(model);
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
