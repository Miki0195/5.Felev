using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Managly.Models.OwnerDashboard
{
    public class CompanyViewModel
    {
        public int Id { get; set; }
        
        [Display(Name = "Company Name")]
        public string Name { get; set; }
        
        [Display(Name = "License Key")]
        public string LicenseKey { get; set; }
        
        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; }
        
        [Display(Name = "Total Users")]
        public int TotalUsers { get; set; }
        
        [Display(Name = "License Status")]
        public string LicenseStatus { get; set; }
        
        [Display(Name = "Expiration Date")]
        public DateTime? ExpirationDate { get; set; }
    }
    
    public class CompanyDetailsViewModel
    {
        public int Id { get; set; }
        
        [Display(Name = "Company Name")]
        public string Name { get; set; }
        
        [Display(Name = "License Key")]
        public string LicenseKey { get; set; }
        
        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; }
        
        [Display(Name = "License Status")]
        public string LicenseStatus { get; set; }
        
        [Display(Name = "Expiration Date")]
        public DateTime? ExpirationDate { get; set; }
        
        [Display(Name = "Users")]
        public List<UserSummaryViewModel> Users { get; set; } = new List<UserSummaryViewModel>();
        
        [Display(Name = "Admin Users")]
        public int AdminCount { get; set; }
        
        [Display(Name = "Manager Users")]
        public int ManagerCount { get; set; }
        
        [Display(Name = "Employee Users")]
        public int EmployeeCount { get; set; }
    }
    
    public class UserSummaryViewModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public DateTime CreatedDate { get; set; }
    }
} 