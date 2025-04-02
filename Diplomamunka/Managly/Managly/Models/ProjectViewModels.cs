// ViewModels/ProjectViewModels.cs
using System;
using System.Collections.Generic;

namespace Managly.Models
{
    public class ProjectDetailsViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string FormattedStartDate { get; set; }
        public string FormattedDeadline { get; set; }
        public string Status { get; set; }
        public string StatusCssClass { get; set; }
        public string Priority { get; set; }
        public string PriorityCssClass { get; set; }
        public bool IsProjectLead { get; set; }
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int ProgressPercentage { get; set; }
        public string CurrentUserId { get; set; }

        public List<ProjectMemberViewModel> TeamMembers { get; set; }
        public List<TaskViewModel> Tasks { get; set; }
        public List<ActivityViewModel> Activities { get; set; }
    }

    public class ProjectMemberViewModel
    {
        public string UserId { get; set; }
        public string Name { get; set; }
        public string LastName { get; set; }
        public string FullName => $"{Name} {LastName}";
        public string Role { get; set; }
        public string ProfilePicturePath { get; set; }
    }

    public class TaskViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string FormattedDueDate { get; set; }
        public DateTime DueDate { get; set; }
        public bool IsOverdue { get; set; }
        public string Priority { get; set; }
        public string PriorityCssClass { get; set; }
        public string Status { get; set; }
        public string StatusCssClass { get; set; }
        public float TimeSpent { get; set; }
        public string FormattedTimeSpent { get; set; }
        public List<AssignedUserViewModel> AssignedUsers { get; set; }
    }

    public class AssignedUserViewModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string LastName { get; set; }
        public string ProfilePicturePath { get; set; }
    }

    public class ActivityViewModel
    {
        public string Type { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string UserAvatar { get; set; }
        public string TimeAgo { get; set; }
        public string Description { get; set; }
        public string TargetType { get; set; }
        public string TargetId { get; set; }
        public string TargetName { get; set; }
    }

    public class ArchivedProjectViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string CreatorName { get; set; }
        public string FormattedStartDate { get; set; }
        public string FormattedDeadline { get; set; }
        public string FormattedCompletionDate { get; set; }
        public string Priority { get; set; }
        public string PriorityCssClass { get; set; }
        
        public List<ProjectMemberViewModel> TeamMembers { get; set; }
        public List<TaskViewModel> Tasks { get; set; }
    }
}