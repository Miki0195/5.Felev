using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Beadando.Data
{
    public class SportsDbContextFactory : IDesignTimeDbContextFactory<SportsDbContext>
    {
        public SportsDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<SportsDbContext>();
            optionsBuilder.UseSqlite("Data Source=SportsDb.db");

            return new SportsDbContext(optionsBuilder.Options);
        }
    }
}

