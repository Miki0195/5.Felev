using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Managly.Models;
using System;
using System.Threading.Tasks;

namespace Managly.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var services = scope.ServiceProvider;

            try
            {
                var context = services.GetRequiredService<ApplicationDbContext>();
                var userManager = services.GetRequiredService<UserManager<User>>();
                var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

                await context.Database.MigrateAsync();
                await SeedRoles(roleManager);
                await SeedOwner(userManager);
            }
            catch (Exception ex)
            {
                var logger = services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "An error occurred while seeding the database.");
            }
        }

        private static async Task SeedRoles(RoleManager<IdentityRole> roleManager)
        {
            // Ensure roles exist
            string[] roleNames = { "Owner", "Admin", "Manager", "Employee" };
            
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }
        }

        private static async Task SeedOwner(UserManager<User> userManager)
        {
            // Check if the Owner user exists
            var ownerUser = await userManager.FindByEmailAsync("owner@managly.com");
            
            if (ownerUser == null)
            {
                // Create the Owner user
                ownerUser = new User
                {
                    UserName = "owner@managly.com",
                    Email = "owner@managly.com",
                    EmailConfirmed = true,
                    Name = "System",
                    LastName = "Owner",
                    ProfilePicturePath = "/images/default/default-profile.png",
                    CreatedDate = DateTime.Now,
                    Country = "System",
                    City = "System",
                    Address = "System",
                    Gender = "Other"
                };

                var result = await userManager.CreateAsync(ownerUser, "Owner123!");
                
                if (result.Succeeded)
                {
                    // Assign the Owner role
                    await userManager.AddToRoleAsync(ownerUser, "Owner");
                }
            }
            else
            {
                // Ensure the existing owner user has the Owner role
                if (!await userManager.IsInRoleAsync(ownerUser, "Owner"))
                {
                    await userManager.AddToRoleAsync(ownerUser, "Owner");
                }
                
                // Ensure all required fields have values
                bool needsUpdate = false;
                
                if (string.IsNullOrEmpty(ownerUser.Country))
                {
                    ownerUser.Country = "System";
                    needsUpdate = true;
                }
                
                if (string.IsNullOrEmpty(ownerUser.City))
                {
                    ownerUser.City = "System";
                    needsUpdate = true;
                }
                
                if (string.IsNullOrEmpty(ownerUser.Address))
                {
                    ownerUser.Address = "System";
                    needsUpdate = true;
                }
                
                if (string.IsNullOrEmpty(ownerUser.Gender))
                {
                    ownerUser.Gender = "Other";
                    needsUpdate = true;
                }
                
                if (needsUpdate)
                {
                    await userManager.UpdateAsync(ownerUser);
                }
            }
        }
    }
} 