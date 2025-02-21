using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Managly.Models
{
    public class TaskAssignment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TaskId { get; set; }
        public Tasks Task { get; set; }

        [Required]
        public string UserId { get; set; }
        public User User { get; set; }

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    }
} 