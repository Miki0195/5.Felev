using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Managly.Models
{
    public class User : IdentityUser
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public string LastName { get; set; }

        public string Country { get; set; }

        public string City { get; set; }

        public string Address { get; set; }

        public int? CompanyId { get; set; }

        [ForeignKey("CompanyId")]
        public Company Company { get; set; }

        public bool IsUsingPreGeneratedPassword { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public string Gender { get; set; } = "Other";

        public string ProfilePicturePath { get; set; } = "/images/default/default-profile.png";

        // Vacation days tracking
        public int TotalVacationDays { get; set; } = 20; // Default to 20 days per year
        public int UsedVacationDays { get; set; } = 0;
        public int RemainingVacationDays { get => TotalVacationDays - UsedVacationDays; }
        public int VacationYear { get; set; } = DateTime.Now.Year; // Track the current year for resets

        // Creation date
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public virtual ICollection<TaskAssignment> TaskAssignments { get; set; } = new List<TaskAssignment>();
        public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
    }
}

