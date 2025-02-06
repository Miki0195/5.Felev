using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Managly.Models
{
    public class Message
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string SenderId { get; set; }

        [Required]
        public string ReceiverId { get; set; }

        [Required]
        public string Content { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [ForeignKey("SenderId")]
        public virtual User Sender { get; set; }

        [ForeignKey("ReceiverId")]
        public virtual User Receiver { get; set; }

        public bool IsRead { get; set; } = false;

        public bool IsDeleted { get; set; } = false;

    }
}

