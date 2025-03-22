using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Managly.Models
{
    public class Project
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public string Description { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        public DateTime? Deadline { get; set; }

        [Required]
        public int CompanyId { get; set; }

        [ForeignKey("CompanyId")]
        public Company Company { get; set; }

        public bool IsActive => !Deadline.HasValue || Deadline.Value >= DateTime.Today;

        public string Status { get; set; } = "Active"; // Active, Completed, On Hold, Cancelled

        public string Priority { get; set; } = "Medium"; // Low, Medium, High

        [Required]
        public string CreatedById { get; set; }
        
        [ForeignKey("CreatedById")]
        public User CreatedBy { get; set; }

        public virtual ICollection<ProjectMember> ProjectMembers { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? CompletedAt { get; set; } // New field to track when a project was completed

        // Progress tracking
        public int TotalTasks { get; set; } = 0;
        public int CompletedTasks { get; set; } = 0;

        public virtual ICollection<Tasks> Tasks { get; set; } = new List<Tasks>();
        
        // Activity log
        public virtual ICollection<ActivityLog> Activities { get; set; } = new List<ActivityLog>();
    }

    public class ProjectMember
    {
        [Key]
        public int Id { get; set; }

        public int ProjectId { get; set; }
        
        [ForeignKey("ProjectId")]
        [JsonIgnore]
        public Project Project { get; set; }

        public string UserId { get; set; }
        
        [ForeignKey("UserId")]
        public User User { get; set; }

        public string Role { get; set; } = "Member"; // Project Lead, Member
        
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }
    
    // New model class for activity logging
    public class ActivityLog
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int ProjectId { get; set; }
        
        [ForeignKey("ProjectId")]
        [JsonIgnore]
        public Project Project { get; set; }
        
        [Required]
        public string UserId { get; set; }
        
        [ForeignKey("UserId")]
        public User User { get; set; }
        
        [Required]
        public string Action { get; set; }
        
        public string TargetType { get; set; } // "Task", "Project", "Member"
        
        public string TargetId { get; set; }
        
        public string TargetName { get; set; }
        
        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public string AdditionalData { get; set; } // JSON string for any extra data
    }
} 