﻿using System;
namespace Managly.Models.DTOs.Schedule
{
    public class AvailabilityCheckDTO
    {
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public List<string> SelectedDays { get; set; }
    }
}

