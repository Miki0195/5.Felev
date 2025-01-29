using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Managly.Models
{
    public class Leave
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public DateTime LeaveDate { get; set; }

        public string Reason { get; set; }

        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected

        public string MedicalProof { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; }
    }
}

