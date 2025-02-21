// Update the Tasks model
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace Managly.Models
{
    public class Tasks
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProjectId { get; set; }  // Add this to link task to project

        [Required]
        public string TaskTitle { get; set; }

        public string Description { get; set; }

        public DateTime AssignedDate { get; set; } = DateTime.Now;

        public DateTime? DueDate { get; set; }

        public string Status { get; set; } = "Pending"; // Pending, Completed, Overdue

        public string Priority { get; set; } = "Medium"; // Add priority field

        public float TimeSpent { get; set; } = 0;

        [Required]
        public string CreatedById { get; set; }  // Add this to track who created the task
        public User CreatedBy { get; set; }

        // Navigation property for Project
        public Project Project { get; set; }

        public virtual ICollection<TaskAssignment> Assignments { get; set; } = new List<TaskAssignment>();
    }
}