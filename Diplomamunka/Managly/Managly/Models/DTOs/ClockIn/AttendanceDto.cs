using System;

namespace Managly.Models.DTOs.ClockIn
{
    // Base DTO for common responses
    public class ApiResponseDto
    {
        public bool Success { get; set; }
        public string Error { get; set; }
    }

    // Clock-in response
    public class ClockInResponseDto : ApiResponseDto
    {
        public DateTime CheckInTime { get; set; }
    }

    // Clock-out response
    public class ClockOutResponseDto : ApiResponseDto
    {
        public DateTime? CheckOutTime { get; set; }
        public double Duration { get; set; } // In hours
    }

    // Current session response
    public class SessionStatusDto
    {
        public bool Active { get; set; }
        public DateTime? CheckInTime { get; set; }
        public double ElapsedTime { get; set; } // In seconds
    }

    // Work history entry
    public class WorkHistoryEntryDto
    {
        public DateTime CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }
        public string Duration { get; set; } // Formatted as HH:MM:SS
    }

    // Weekly hours response
    public class WeeklyHoursDto
    {
        public double TotalHours { get; set; }
        public double TotalMinutes { get; set; }
    }

    public class UpdateTimeRequestDto
    {
        public string RecordId { get; set; }
        public string UserId { get; set; }
        public string CheckInTime { get; set; }
        public string CheckOutTime { get; set; }
        public string AdminNotes { get; set; }
    }
} 