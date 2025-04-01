using System;
using Managly.Models;
using Managly.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Managly.Models.VideoConference
{
    public class VideoConference
    {
        [Key]
        public string CallId { get; set; } = Guid.NewGuid().ToString(); 
        public required string CallerId { get; set; }
        public required string ReceiverId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; } 

        public bool IsEnded { get; set; } = false;
        [Required]
        public CallStatus Status { get; set; } = CallStatus.Active;

        public TimeSpan? Duration => EndTime.HasValue ? EndTime - StartTime : null;
    }
}
