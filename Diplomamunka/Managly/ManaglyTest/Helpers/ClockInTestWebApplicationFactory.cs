using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Managly.Data;
using System;
using System.Linq;

namespace Managlytest.Helpers
{
    public class ClockInTestWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram>
        where TProgram : class
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing"); // Mark environment as Testing

            builder.ConfigureServices(services =>
            {
                // Remove existing DbContext registrations
                var descriptors = services
                    .Where(d => d.ServiceType.IsAssignableFrom(typeof(DbContextOptions)) ||
                                (d.ServiceType.IsGenericType && 
                                 d.ServiceType.GetGenericTypeDefinition() == typeof(DbContextOptions<>)))
                    .ToList();

                foreach (var descriptor in descriptors)
                {
                    services.Remove(descriptor);
                }

                // Add ApplicationDbContext using in-memory database
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestingDb");
                });
            });
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            // Add test-specific configuration
            builder.ConfigureServices(services =>
            {
                // Create a fresh service provider
                var serviceProvider = services.BuildServiceProvider();

                // Create a scope to obtain a reference to the database context
                using (var scope = serviceProvider.CreateScope())
                {
                    var scopedServices = scope.ServiceProvider;
                    var db = scopedServices.GetRequiredService<ApplicationDbContext>();
                    db.Database.EnsureCreated();
                    
                    // Seed the database with test data if needed
                    // SeedDatabase(db);
                }
            });

            return base.CreateHost(builder);
        }
    }
} 