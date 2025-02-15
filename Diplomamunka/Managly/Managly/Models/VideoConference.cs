using System;
using System.ComponentModel.DataAnnotations;

namespace Managly.Models
{
    public class VideoConference
    {
        [Key]
        public string CallId { get; set; } = Guid.NewGuid().ToString(); // Unique Call ID
        public string CallerId { get; set; }
        public string ReceiverId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; } // Nullable until call ends

        public TimeSpan? Duration => EndTime.HasValue ? EndTime - StartTime : null;
    }
}
