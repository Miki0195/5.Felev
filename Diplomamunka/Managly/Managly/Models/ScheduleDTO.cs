using System;
namespace Managly.Models
{
    public class ScheduleDTO
    {
        public string UserId { get; set; }
        public DateTime ShiftDate { get; set; }
        public string StartTime { get; set; }  // Accept as a string from the frontend
        public string EndTime { get; set; }    // Accept as a string from the frontend
        public string Comment { get; set; }
    }
}

