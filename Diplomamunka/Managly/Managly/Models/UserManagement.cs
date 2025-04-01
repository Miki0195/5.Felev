using System;
using System.Collections.Generic;
namespace Managly.Models
{
    public class UserManagement
    {
        public required string UserId { get; set; }
        public required string Name { get; set; }
        public required string LastName { get; set; }
        public required string Email { get; set; }
        public required string Roles { get; set; }
        public List<ProjectInfo> AssignedProjects { get; set; } = new List<ProjectInfo>();

        // Additional profile information
        public string PhoneNumber { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string ProfilePicturePath { get; set; } = string.Empty;

        // Vacation days tracking
        public int TotalVacationDays { get; set; }
        public int UsedVacationDays { get; set; }
        public int RemainingVacationDays { get; set; }
        public int VacationYear { get; set; }
    }

    public class ProjectInfo
    {
        public int ProjectId { get; set; }
        public required string ProjectName { get; set; }
        public required string Role { get; set; }
    }
}

