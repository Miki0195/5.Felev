using System;
using System.ComponentModel.DataAnnotations;

namespace Managly.Models
{
    public class LicenseKey
    {
        public int Id { get; set; } // Automatically incremented

        [Required]
        [MaxLength(100)]
        public string Key { get; set; } 

        public bool IsActive { get; set; } = false; // False means that the License Key is currently not in use, but as soon it's assigned to a company it becomes true indicating it is being used

        public int? AssignedToCompanyId { get; set; } // Foreign Key

        public Company AssignedToCompany { get; set; } // Navigation property  

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow; 

        public DateTime? ExpirationDate { get; set; } // For future implementation
    }

}

