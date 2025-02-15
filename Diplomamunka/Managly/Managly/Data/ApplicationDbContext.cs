using System;
using Managly.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Managly.Data
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
        {
        }

        public DbSet<Company> Companies { get; set; }
        public DbSet<Schedule> Schedules { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Tasks> Tasks { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<Leave> Leaves { get; set; }
        public DbSet<LicenseKey> LicenseKeys { get; set; }
        public DbSet<VideoCallInvitation> VideoCallInvitations { get; set; }
        public DbSet<VideoConference> VideoConferences { get; set; }
    }
}

