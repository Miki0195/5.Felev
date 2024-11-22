using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;

namespace Beadando.Data
{
    public static class DbInitializer
    {
        public static async Task SeedRoles(RoleManager<IdentityRole> roleManager)
        {
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
            }

            if (!await roleManager.RoleExistsAsync("User"))
            {
                await roleManager.CreateAsync(new IdentityRole("User"));
            }
        }

        public static async Task SeedAdminUser(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            var adminRole = "Admin";
            var adminEmail = "admin@example.com";
            var adminPassword = "Admin123!";

            // Check if there are any users in the Admin role
            var admins = await userManager.GetUsersInRoleAsync(adminRole);
            if (admins.Count > 0)
            {
                // Admin user already exists; exit the method
                return;
            }

            // Create the admin user if none exists
            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var adminUser = new IdentityUser { UserName = adminEmail, Email = adminEmail };
                var result = await userManager.CreateAsync(adminUser, adminPassword);

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, adminRole);
                }
            }
        }
    }
}
