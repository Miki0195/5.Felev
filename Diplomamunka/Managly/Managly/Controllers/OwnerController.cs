using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Managly.Data;
using Managly.Models;
using Managly.Models.Enums;
using Managly.Models.OwnerDashboard;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Managly.Controllers
{
    [Authorize(Roles = "Owner")]
    public class OwnerController : Controller
    {
        #region Private Members

        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        #endregion

        #region Constructor

        public OwnerController(
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        #endregion

        #region Dashboard Actions

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var model = new DashboardViewModel
            {
                TotalCompanies = await _context.Companies.CountAsync(),
                TotalUsers = await _context.Users.CountAsync(),
                TotalActiveLicenses = await _context.LicenseKeys.CountAsync(lk => lk.Status == LicensekeyStatus.Active),
                TotalAvailableLicenses = await _context.LicenseKeys.CountAsync(lk => lk.Status == LicensekeyStatus.Available),
                TotalExpiredLicenses = await _context.LicenseKeys.CountAsync(lk => lk.Status == LicensekeyStatus.Expired),
                TotalRevokedLicenses = 0, 
            };

            var sixMonthsAgo = DateTime.Now.AddMonths(-6);
            var monthlyData = await _context.Companies
                .Where(c => c.CreatedDate >= sixMonthsAgo)
                .GroupBy(c => new { Month = c.CreatedDate.Month, Year = c.CreatedDate.Year })
                .Select(g => new
                {
                    Month = g.Key.Month,
                    Year = g.Key.Year,
                    Count = g.Count()
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToListAsync();

            foreach (var data in monthlyData)
            {
                var monthName = new DateTime(data.Year, data.Month, 1).ToString("MMM yyyy");
                model.MonthlyRegistrations.Add(new MonthlyRegistrationData
                {
                    Month = monthName,
                    CompanyCount = data.Count
                });
            }

            model.RecentActivities = await _context.OwnerActivityLogs
                .OrderByDescending(a => a.Timestamp)
                .Take(4)
                .Select(a => new RecentActivityLog
                {
                    Id = a.Id,
                    ActivityType = a.ActivityType,
                    Description = a.Description,
                    Timestamp = a.Timestamp,
                    CompanyName = a.CompanyName ?? string.Empty,
                    CompanyId = a.CompanyId
                })
                .ToListAsync();

            model.RecentCompanies = await _context.Companies
                .OrderByDescending(c => c.CreatedDate)
                .Take(5)
                .Select(c => new CompanyViewModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    LicenseKey = c.LicenseKey,
                    CreatedDate = c.CreatedDate,
                    TotalUsers = _context.Users.Count(u => u.CompanyId == c.Id),
                    LicenseStatus = "Active"
                })
                .ToListAsync();

            return View(model);
        }

        #endregion

        #region Company Management Actions

        [HttpGet]
        public async Task<IActionResult> Companies()
        {
            var companies = await _context.Companies
                .OrderByDescending(c => c.CreatedDate)
                .Select(c => new CompanyViewModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    LicenseKey = c.LicenseKey,
                    CreatedDate = c.CreatedDate,
                    TotalUsers = _context.Users.Count(u => u.CompanyId == c.Id),
                    LicenseStatus = "Active" 
                })
                .ToListAsync();

            foreach (var company in companies)
            {
                var licenseKey = await _context.LicenseKeys
                    .FirstOrDefaultAsync(lk => lk.Key == company.LicenseKey);
                
                if (licenseKey != null)
                {
                    company.ExpirationDate = licenseKey.ExpirationDate;
                    
                    if (licenseKey.ExpirationDate.HasValue && licenseKey.ExpirationDate.Value < DateTime.Now)
                    {
                        company.LicenseStatus = "Expired";
                    }
                }
            }

            return View(companies);
        }

        [HttpGet]
        public async Task<IActionResult> CompanyDetails(int id)
        {
            var company = await _context.Companies
                .FirstOrDefaultAsync(c => c.Id == id);

            if (company == null)
            {
                TempData["ErrorMessage"] = "Company not found";
                return RedirectToAction("Companies");
            }

            var licenseKey = await _context.LicenseKeys
                .FirstOrDefaultAsync(lk => lk.Key == company.LicenseKey);

            var model = new CompanyDetailsViewModel
            {
                Id = company.Id,
                Name = company.Name,
                LicenseKey = company.LicenseKey,
                CreatedDate = company.CreatedDate,
                ExpirationDate = licenseKey?.ExpirationDate,
                LicenseStatus = licenseKey?.ExpirationDate != null && licenseKey.ExpirationDate < DateTime.Now ? "Expired" : "Active"
            };

            var usersInCompany = await _context.Users
                .Where(u => u.CompanyId == company.Id)
                .ToListAsync();

            model.AdminCount = 0;
            model.ManagerCount = 0;
            model.EmployeeCount = 0;

            foreach (var user in usersInCompany)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var role = roles.FirstOrDefault() ?? "Employee";

                model.Users.Add(new UserSummaryViewModel
                {
                    Id = user.Id,
                    Name = user.Name,
                    LastName = user.LastName,
                    Email = user.Email,
                    Role = role,
                    CreatedDate = user.CreatedDate 
                });

                if (role == "Admin")
                    model.AdminCount++;
                else if (role == "Manager")
                    model.ManagerCount++;
                else
                    model.EmployeeCount++;
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCompany(int id)
        {
            var company = await _context.Companies
                .FirstOrDefaultAsync(c => c.Id == id);

            if (company == null)
            {
                TempData["ErrorMessage"] = "Company not found";
                return RedirectToAction("Companies");
            }

            var usersInCompany = await _context.Users
                .Where(u => u.CompanyId == company.Id)
                .ToListAsync();

            foreach (var user in usersInCompany)
            {
                await _userManager.DeleteAsync(user);
            }

            var licenseKey = await _context.LicenseKeys
                .FirstOrDefaultAsync(lk => lk.Key == company.LicenseKey);

            if (licenseKey != null)
            {
                licenseKey.IsActive = false;
                licenseKey.AssignedToCompanyId = null;
                _context.LicenseKeys.Update(licenseKey);
            }

            var activity = new OwnerActivityLog
            {
                ActivityType = "CompanyDeleted",
                Description = $"Company '{company.Name}' was deleted",
                CompanyName = company.Name,
                CompanyId = company.Id,
                PerformedByUserId = _userManager.GetUserId(User)
            };
            _context.OwnerActivityLogs.Add(activity);

            _context.Companies.Remove(company);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Company '{company.Name}' has been deleted and its license key is now available";
            return RedirectToAction("Companies");
        }

        #endregion

        #region License Key Management Actions

        [HttpGet]
        public async Task<IActionResult> LicenseKeys()
        {
            var licenseKeys = await _context.LicenseKeys
                .OrderByDescending(lk => lk.CreatedDate)
                .Select(lk => new LicenseKeyViewModel
                {
                    Id = lk.Id,
                    Key = lk.Key,
                    Status = lk.Status.ToString(),
                    AssignedToCompanyId = lk.AssignedToCompanyId,
                    AssignedToCompanyName = lk.AssignedToCompany != null ? lk.AssignedToCompany.Name : null,
                    CreatedDate = lk.CreatedDate,
                    ExpirationDate = lk.ExpirationDate
                })
                .ToListAsync();

            foreach (var key in licenseKeys.Where(k => k.Status != "Expired" && k.ExpirationDate.HasValue && k.ExpirationDate.Value < DateTime.Now))
            {
                var dbKey = await _context.LicenseKeys.FindAsync(key.Id);
                if (dbKey != null && dbKey.Status != LicensekeyStatus.Expired && dbKey.ExpirationDate < DateTime.Now)
                {
                    dbKey.Status = LicensekeyStatus.Expired;
                    await _context.SaveChangesAsync();
                }
                key.Status = "Expired";
            }

            return View(licenseKeys);
        }

        [HttpGet]
        public IActionResult GenerateLicenseKey()
        {
            return View(new GenerateLicenseKeyViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateLicenseKey(GenerateLicenseKeyViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var licenseKey = new LicenseKey
            {
                Key = GenerateUniqueKey(),
                Status = LicensekeyStatus.Available,
                CreatedDate = DateTime.UtcNow,
                ExpirationDate = model.ExpirationDate
            };

            _context.LicenseKeys.Add(licenseKey);

            var activity = new OwnerActivityLog
            {
                ActivityType = "LicenseKeyGenerated",
                Description = $"New license key '{licenseKey.Key}' generated",
                PerformedByUserId = _userManager.GetUserId(User)
            };
            _context.OwnerActivityLogs.Add(activity);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "New license key generated successfully";
            return RedirectToAction("LicenseKeys");
        }

        [HttpGet]
        public async Task<IActionResult> EditLicenseKey(int id)
        {
            var licenseKey = await _context.LicenseKeys.FindAsync(id);
            if (licenseKey == null)
            {
                TempData["ErrorMessage"] = "License key not found";
                return RedirectToAction("LicenseKeys");
            }

            var model = new UpdateLicenseKeyViewModel
            {
                Id = licenseKey.Id,
                Key = licenseKey.Key,
                Status = licenseKey.Status.ToString(),
                ExpirationDate = licenseKey.ExpirationDate
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLicenseKey(UpdateLicenseKeyViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var licenseKey = await _context.LicenseKeys.FindAsync(model.Id);
            if (licenseKey == null)
            {
                TempData["ErrorMessage"] = "License key not found";
                return RedirectToAction("LicenseKeys");
            }

            licenseKey.ExpirationDate = model.ExpirationDate;

            var oldStatus = licenseKey.Status.ToString();
            LicensekeyStatus newStatus;
            
            if (!Enum.TryParse(model.Status, out newStatus))
            {
                TempData["ErrorMessage"] = "Invalid license key status";
                return View(model);
            }

            if (newStatus != licenseKey.Status)
            {
                if (newStatus == LicensekeyStatus.Available && licenseKey.Status == LicensekeyStatus.Active)
                {
                    if (licenseKey.AssignedToCompanyId.HasValue)
                    {
                        var company = await _context.Companies
                            .FirstOrDefaultAsync(c => c.Id == licenseKey.AssignedToCompanyId);
                        
                        if (company != null)
                        {
                            var activity = new OwnerActivityLog
                            {
                                ActivityType = "LicenseKeyRevoked",
                                Description = $"License key '{licenseKey.Key}' was revoked from company '{company.Name}'",
                                CompanyName = company.Name,
                                CompanyId = company.Id,
                                PerformedByUserId = _userManager.GetUserId(User)
                            };
                            _context.OwnerActivityLogs.Add(activity);
                        }
                        
                        licenseKey.AssignedToCompanyId = null;
                    }
                }
                
                licenseKey.Status = newStatus;
            }

            var statusActivity = new OwnerActivityLog
            {
                ActivityType = "LicenseKeyUpdated",
                Description = $"License key '{licenseKey.Key}' updated from '{oldStatus}' to '{model.Status}'",
                PerformedByUserId = _userManager.GetUserId(User)
            };
            _context.OwnerActivityLogs.Add(statusActivity);

            _context.LicenseKeys.Update(licenseKey);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "License key updated successfully";
            return RedirectToAction("LicenseKeys");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLicenseKey(int id)
        {
            var licenseKey = await _context.LicenseKeys.FindAsync(id);
            if (licenseKey == null)
            {
                TempData["ErrorMessage"] = "License key not found";
                return RedirectToAction("LicenseKeys");
            }

            if (licenseKey.Status == LicensekeyStatus.Active && licenseKey.AssignedToCompanyId.HasValue)
            {
                TempData["ErrorMessage"] = "Cannot delete an active license key that is assigned to a company";
                return RedirectToAction("LicenseKeys");
            }

            var activity = new OwnerActivityLog
            {
                ActivityType = "LicenseKeyDeleted",
                Description = $"License key '{licenseKey.Key}' was deleted",
                PerformedByUserId = _userManager.GetUserId(User)
            };
            _context.OwnerActivityLogs.Add(activity);

            _context.LicenseKeys.Remove(licenseKey);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "License key deleted successfully";
            return RedirectToAction("LicenseKeys");
        }

        #endregion

        #region Activity Logs

        [HttpGet]
        public async Task<IActionResult> Activities()
        {
            var activityLogs = await _context.OwnerActivityLogs
                .OrderByDescending(a => a.Timestamp)
                .Select(a => new RecentActivityLog
                {
                    Id = a.Id,
                    ActivityType = a.ActivityType,
                    Description = a.Description,
                    Timestamp = a.Timestamp,
                    CompanyName = a.CompanyName ?? string.Empty,
                    CompanyId = a.CompanyId
                })
                .ToListAsync();

            return View(activityLogs);
        }

        #endregion

        #region Helper Methods

        private string GenerateUniqueKey()
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                var keyBytes = new byte[16];
                rng.GetBytes(keyBytes);
                
                var sb = new StringBuilder();
                for (int i = 0; i < keyBytes.Length; i++)
                {
                    sb.Append(keyBytes[i].ToString("X2"));
                    if (i > 0 && i % 4 == 3 && i < keyBytes.Length - 1)
                    {
                        sb.Append("-");
                    }
                }
                
                return sb.ToString();
            }
        }

        #endregion
    }
} 