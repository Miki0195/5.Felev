using Managly.Data;
using Managly.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Managly.Hubs;
using System.Security.Cryptography.X509Certificates;
using System.Net;

namespace Managly;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var urls = builder.Configuration["Urls"];
        if (string.IsNullOrEmpty(urls))
        {
            // Get ports from environment variables or use defaults
            var httpPort = Environment.GetEnvironmentVariable("HTTP_PORT") ?? "5050";
            var httpsPort = Environment.GetEnvironmentVariable("HTTPS_PORT") ?? "5051";

            // Set URLs configuration
            builder.Configuration["Urls"] = $"http://*:{httpPort};https://*:{httpsPort}";
        }

        // Use the configured URLs
        builder.WebHost.UseUrls(builder.Configuration["Urls"].Split(';'));

        // Configure Kestrel with your certificate
        builder.WebHost.ConfigureKestrel(serverOptions =>
        {
            // Load certificate
            var cert = X509Certificate2.CreateFromPemFile("localhost.crt", "localhost.key");

            // Apply HTTPS configuration
            serverOptions.ConfigureHttpsDefaults(httpsOptions =>
            {
                httpsOptions.ServerCertificate = cert;
            });
        });

        // Adatbázis kapcsolat hozzáadása
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseMySql(
                builder.Configuration.GetConnectionString("DefaultConnection"),
                new MySqlServerVersion(new Version(8, 0, 0)) // Changed to 8.0.0 for better compatibility
            ));

        // Add services to the container.
        builder.Services.AddControllersWithViews();

        builder.Services.AddSignalR();

        builder.Services.AddDistributedMemoryCache();
        builder.Services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromMinutes(30);
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
        });

        builder.Services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.ExpireTimeSpan = TimeSpan.FromDays(30);
            options.SlidingExpiration = true;
            options.LoginPath = "/Home/Login";
            options.AccessDeniedPath = "/Account/AccessDenied";
        });

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("CorsPolicy",
                builder => builder
                    .SetIsOriginAllowed(_ => true) // In development, allow any origin
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials());
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
        builder.Services.AddScoped<ChatHub>();
        builder.Services.AddSingleton<EmailService>();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            // Development-specific configuration
        }
        else
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        // Initialize Seed Data for the Owner role and user
        await SeedData.Initialize(app.Services);

        app.UseSession();

        // Handle HTTPS redirection properly for both development and production
        app.Use(async (context, next) =>
        {
            if (context.Request.IsHttps)
            {
                await next();
            }
            else
            {
                // Parse URLs to get HTTPS port
                var urls = builder.Configuration["Urls"].Split(';');
                string httpsUrl = urls.FirstOrDefault(u => u.StartsWith("https://")) ?? "https://*:5051";

                // Extract port
                var httpsPort = int.Parse(httpsUrl.Split(':').Last());

                var host = context.Request.Host;
                var newHost = new HostString(host.Host, httpsPort);

                var url = string.Concat(
                    "https://",
                    newHost.ToUriComponent(),
                    context.Request.PathBase.ToUriComponent(),
                    context.Request.Path.ToUriComponent(),
                    context.Request.QueryString.ToUriComponent());

                context.Response.Redirect(url);
            }
        });

        app.UseStaticFiles();
        app.UseRouting();
        app.UseCors("CorsPolicy");
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

                if (user == null)
                {
                    await signInManager.SignOutAsync();
                    context.Response.Redirect("/Account/AccountDeleted");
                    return;
                }
            }

            await next();
        });

        app.MapControllers();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapHub<VideoCallHub>("/videocallhub").RequireCors("CorsPolicy");
            endpoints.MapHub<ChatHub>("/chathub");
            endpoints.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Login}/{id?}");
        });

        app.Run();
    }
}