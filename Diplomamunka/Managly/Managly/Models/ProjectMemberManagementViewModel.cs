using System.Collections.Generic;

namespace Managly.Models
{
    public class ProjectMemberManagementViewModel
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public List<MemberViewModel> Members { get; set; } = new List<MemberViewModel>();
        public bool IsCurrentUserAdmin { get; set; }
    }

    public class MemberViewModel
    {
        public string UserId { get; set; }
        public string Name { get; set; }
        public string LastName { get; set; }
        public string FullName => $"{Name} {LastName}";
        public string ProfilePicturePath { get; set; }
        public string Role { get; set; }
        public bool IsProjectLead => Role == "Project Lead";
    }
} 