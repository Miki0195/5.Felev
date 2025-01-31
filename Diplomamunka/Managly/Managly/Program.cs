﻿using Managly.Data;
using Managly.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Managly.Data;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Managly;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Adatbázis kapcsolat hozzáadása
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseMySql(
                builder.Configuration.GetConnectionString("DefaultConnection"),
                new MySqlServerVersion(new Version(9, 2, 0)) // Add meg az aktuális MySQL verziódat
            ));

        // Add services to the container.
        builder.Services.AddControllersWithViews();

        builder.Services.AddDistributedMemoryCache();
        builder.Services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromMinutes(30); // Set session timeout
            options.Cookie.HttpOnly = true; // Secure cookie
            options.Cookie.IsEssential = true; // Ensure session works even without user consent
        });

        builder.Services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.HttpOnly = true; // Prevent JavaScript access
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Ensure cookies are only sent over HTTPS
            options.ExpireTimeSpan = TimeSpan.FromDays(30); // ✅ Persistent login for 30 days
            options.SlidingExpiration = true; // Reset expiration time on activity
        });

        builder.Services.AddIdentity<User, IdentityRole>()
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("RequirePreGeneratedPassword", policy =>
                policy.Requirements.Add(new PreGeneratedPasswordRequirement()));
        });

        builder.Services.AddScoped<IAuthorizationHandler, PreGeneratedPasswordHandler>();

        builder.Services.AddSingleton<EmailService>();

        builder.Services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/Home/Login";
            options.AccessDeniedPath = "/Account/AccessDenied"; // Redirect unauthorized users
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        using (var scope = app.Services.CreateScope())
        {
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Ensure roles exist
            string[] roles = { "Admin", "Employee", "Manager" }; // Add any roles you need
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }

        app.UseSession();

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();


        app.UseAuthentication();
        app.UseAuthorization();

        app.Use(async (context, next) =>
        {
            var userManager = context.RequestServices.GetRequiredService<UserManager<User>>();
            var signInManager = context.RequestServices.GetRequiredService<SignInManager<User>>();

            if (context.User.Identity.IsAuthenticated)
            {
                var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
                var user = await userManager.FindByIdAsync(userId);

                // ✅ Only log out the user if they are deleted, but NOT the admin who deleted them.
                if (user == null)
                {
                    await signInManager.SignOutAsync();
                    context.Response.Redirect("/Account/AccountDeleted");
                    return;
                }
            }

            await next();
        });

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Login}/{id?}");

        app.Run();
    }
}

