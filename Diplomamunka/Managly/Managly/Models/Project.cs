using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

        [Required]
        public DateTime Deadline { get; set; }

        public string Status { get; set; } = "Active"; // Active, Completed, On Hold, Cancelled

        public string Priority { get; set; } = "Medium"; // Low, Medium, High

        [Required]
        public string CreatedById { get; set; }
        
        [ForeignKey("CreatedById")]
        public User CreatedBy { get; set; }

        [Required]
        public int CompanyId { get; set; }

        [ForeignKey("CompanyId")]
        public Company Company { get; set; }

        public virtual ICollection<ProjectMember> ProjectMembers { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Progress tracking
        public int TotalTasks { get; set; } = 0;
        public int CompletedTasks { get; set; } = 0;

        public virtual ICollection<Tasks> Tasks { get; set; } = new List<Tasks>();
    }

    public class ProjectMember
    {
        [Key]
        public int Id { get; set; }

        public int ProjectId { get; set; }
        
        [ForeignKey("ProjectId")]
        public Project Project { get; set; }

        public string UserId { get; set; }
        
        [ForeignKey("UserId")]
        public User User { get; set; }

        public string Role { get; set; } = "Member"; // Project Lead, Member
        
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }
} 