using System;
namespace Managly.Models.DTOs.Schedule
{
    public class LeaveRequestDTO
    {
        public string UserId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Type { get; set; } 
        public string Reason { get; set; }
    }
}

