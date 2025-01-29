using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Managly.Models
{
    public class Tasks
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public string TaskTitle { get; set; }

        public string Description { get; set; }

        public DateTime AssignedDate { get; set; } = DateTime.Now;

        public DateTime? DueDate { get; set; }

        public string Status { get; set; } = "Pending"; // Pending, Completed, Overdue

        public float TimeSpent { get; set; } = 0;

        public string AssignedToId { get; set; } // Reference to IdentityUser
        public User AssignedTo { get; set; }
    }
}

