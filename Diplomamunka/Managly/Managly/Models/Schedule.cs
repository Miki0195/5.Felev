using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Managly.Models
{
    public class Schedule
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public DateTime ShiftDate { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        public string Comment { get; set; }

        public bool IsHolidayRequest { get; set; } = false;

        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected

        [ForeignKey("UserId")]
        public User User { get; set; }
    }
}

