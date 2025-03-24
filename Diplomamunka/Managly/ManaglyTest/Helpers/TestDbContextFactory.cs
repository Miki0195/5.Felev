using Microsoft.EntityFrameworkCore;
using Managly.Data;
using System;

namespace Managlytest.Helpers
{
    public static class TestDbContextFactory
    {
        public static ApplicationDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }
    }
} 