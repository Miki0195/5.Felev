using System;
using System.Collections.Generic;
namespace Managly.Models
{
    public class UserManagement
    {
        public string UserId { get; set; }
        public string Name { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Roles { get; set; }
        public List<ProjectInfo> AssignedProjects { get; set; } = new List<ProjectInfo>();
        
        // Additional profile information
        public string PhoneNumber { get; set; }
        public string Country { get; set; }
        public string City { get; set; }
        public string Address { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string Gender { get; set; }
        public string ProfilePicturePath { get; set; }
        
        // Vacation days tracking
        public int TotalVacationDays { get; set; }
        public int UsedVacationDays { get; set; }
        public int RemainingVacationDays { get; set; }
        public int VacationYear { get; set; }
    }

    public class ProjectInfo
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string Role { get; set; }
    }
}

