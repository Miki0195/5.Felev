using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public interface IApplicationDbContext
{
    DbSet<Schedule> Schedules { get; set; }
    DbSet<Leave> Leaves { get; set; }
    // Other DbSets...
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
} 