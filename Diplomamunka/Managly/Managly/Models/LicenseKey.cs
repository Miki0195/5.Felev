using System;
using System.ComponentModel.DataAnnotations;
using Managly.Models.Enums;

namespace Managly.Models
{
    public class LicenseKey
    {
        public int Id { get; set; } // Automatically incremented

        [Required]
        [MaxLength(100)]
        public string Key { get; set; } 

        public LicensekeyStatus Status { get; set; } = LicensekeyStatus.Available;

        public int? AssignedToCompanyId { get; set; } // Foreign Key

        public Company AssignedToCompany { get; set; } // Navigation property  

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow; 

        public DateTime? ExpirationDate { get; set; } // For future implementation

        [Obsolete("Use Status property instead. This property is only for backward compatibility.")]
        public bool IsActive 
        { 
            get => Status == LicensekeyStatus.Active; 
            set => Status = value ? LicensekeyStatus.Active : LicensekeyStatus.Available; 
        }
    }

}

