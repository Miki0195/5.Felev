using System;
using System.ComponentModel.DataAnnotations;

namespace Managly.Models.OwnerDashboard
{
    public class LicenseKeyViewModel
    {
        public int Id { get; set; }
        
        [Required]
        [Display(Name = "License Key")]
        public string Key { get; set; }
        
        [Display(Name = "Status")]
        public string Status { get; set; } // "Active", "Available", "Expired", "Revoked"
        
        [Display(Name = "Assigned To")]
        public string AssignedToCompanyName { get; set; }
        
        public int? AssignedToCompanyId { get; set; }
        
        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; }
        
        [Display(Name = "Expiration Date")]
        public DateTime? ExpirationDate { get; set; }
    }
    
    public class GenerateLicenseKeyViewModel
    {
        [Display(Name = "Expiration Date")]
        public DateTime? ExpirationDate { get; set; }
    }
    
    public class UpdateLicenseKeyViewModel
    {
        public int Id { get; set; }
        
        [Required]
        public string Key { get; set; }
        
        [Required]
        [Display(Name = "Status")]
        public string Status { get; set; }
        
        [Display(Name = "Expiration Date")]
        public DateTime? ExpirationDate { get; set; }
    }
} 