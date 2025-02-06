using System;
namespace Managly.Models
{
    public class ScheduleUpdateDTO
    {
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public DateTime ShiftDate { get; set; }
        public string Comment { get; set; }
    }
}

