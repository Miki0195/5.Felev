using System;
using System.ComponentModel.DataAnnotations;

namespace Managly.Models.DTOs.Projects
{
    public class ProjectCreateDto
    {

        [Required(ErrorMessage = "Project name is required")]
        public string Name { get; set; }

        public string Description { get; set; }

        [Required(ErrorMessage = "Start date is required")]
        [Display(Name = "Start Date")]
        public string StartDate { get; set; }

        [Required(ErrorMessage = "Deadline is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Deadline")]
        public string Deadline { get; set; }

        [Required(ErrorMessage = "Priority is required")]
        public string Priority { get; set; } = "Medium";

        [Required(ErrorMessage = "Status is required")]
        public string Status { get; set; } = "Not Started";

        public List<string> TeamMemberIds { get; set; } = new List<string>();
    }
}

