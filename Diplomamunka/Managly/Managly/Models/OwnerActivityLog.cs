using System;
using System.ComponentModel.DataAnnotations;

namespace Managly.Models
{
    public class OwnerActivityLog
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string ActivityType { get; set; } // "CompanyRegistered", "LicenseGenerated", "LicenseActivated", "CompanyDeleted", etc.
        
        [Required]
        public string Description { get; set; }
        
        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        public string? CompanyName { get; set; }
        
        public int? CompanyId { get; set; }
        
        public string? PerformedByUserId { get; set; }
    }
} 