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
        public DbSet<Project> Projects { get; set; }
        public DbSet<ProjectMember> ProjectMembers { get; set; }
        public DbSet<TaskAssignment> TaskAssignments { get; set; }
        public DbSet<GroupChat> GroupChats { get; set; }
        public DbSet<GroupMember> GroupMembers { get; set; }
        public DbSet<GroupMessage> GroupMessages { get; set; }
        public DbSet<GroupMessageRead> GroupMessageReads { get; set; }
        public DbSet<DashboardLayout> DashboardLayouts { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<GroupMember>()
                .HasKey(gm => new { gm.GroupId, gm.UserId });

            builder.Entity<GroupMessageRead>()
                .HasKey(gmr => new { gmr.MessageId, gmr.UserId });
        }
    }
}

