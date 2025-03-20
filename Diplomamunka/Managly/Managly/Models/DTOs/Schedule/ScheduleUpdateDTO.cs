using System;
namespace Managly.Models.DTOs.Schedule
{
    public class ScheduleUpdateDTO
    {
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public string ShiftDate { get; set; }
        public string Comment { get; set; }
    }
}

