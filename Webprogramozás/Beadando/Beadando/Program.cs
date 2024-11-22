using Beadando.Data;
using Microsoft.EntityFrameworkCore;

namespace Beadando;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllersWithViews();
        builder.Services.AddDistributedMemoryCache();
        builder.Services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromMinutes(30); // Set session timeout
            options.Cookie.HttpOnly = true; // Make session cookie HTTP-only
            options.Cookie.IsEssential = true; // Ensure cookie is included even if user hasn't consented to non-essential cookies
        });


        builder.Services.AddDbContext<SportsDbContext>(options =>
        options.UseSqlite(builder.Configuration.GetConnectionString("SportsDatabase")));

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthorization();

        app.UseSession();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{date?}");
            endpoints.MapControllerRoute(
                name: "search",
                pattern: "{controller=Search}/{action=Matches}/{id?}");
            endpoints.MapControllerRoute(
                name: "matchDetails",
                pattern: "Match/{action=MatchDetails}/{id?}");
        });

        app.Run();
    }
}

