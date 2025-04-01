using System;
using System.Collections.Generic;

namespace Managly.Models.OwnerDashboard
{
    public class DashboardViewModel
    {
        public int TotalCompanies { get; set; }
        public int TotalUsers { get; set; }
        public int TotalActiveLicenses { get; set; }
        public int TotalAvailableLicenses { get; set; }
        public int TotalExpiredLicenses { get; set; }
        public int TotalRevokedLicenses { get; set; }
        
        public List<MonthlyRegistrationData> MonthlyRegistrations { get; set; } = new List<MonthlyRegistrationData>();
        public List<RecentActivityLog> RecentActivities { get; set; } = new List<RecentActivityLog>();
        public List<CompanyViewModel> RecentCompanies { get; set; } = new List<CompanyViewModel>();
    }
    
    public class MonthlyRegistrationData
    {
        public string Month { get; set; }
        public int CompanyCount { get; set; }
    }
    
    public class RecentActivityLog
    {
        public int Id { get; set; }
        public string ActivityType { get; set; } // "CompanyRegistered", "LicenseGenerated", "LicenseActivated", etc.
        public string Description { get; set; }
        public DateTime Timestamp { get; set; }
        public string CompanyName { get; set; }
        public int? CompanyId { get; set; }
    }
} 