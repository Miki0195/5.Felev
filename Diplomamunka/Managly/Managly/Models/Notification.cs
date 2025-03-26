using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Managly.Models.Enums;

namespace Managly.Models
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } 

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [Required]
        public string Message { get; set; } 

        public bool IsRead { get; set; } = false; 

        public string Link { get; set; } 

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [Required]
        public NotificationType Type { get; set; }

        public int? ProjectId { get; set; }
        public int? TaskId { get; set; }
        public string RelatedUserId { get; set; }  // For user-related notifications


        public string MetaData { get; set; }
    }
}