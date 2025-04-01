using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Managly.Models;

namespace Managly.Models.VideoConference
{
    public class VideoCallInvitation
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public required string SenderId { get; set; }

        [Required]
        public required string ReceiverId { get; set; }

        [Required]
        public DateTime Timestamp { get; set; } = DateTime.Now;

        public bool IsAccepted { get; set; } = false;

        [ForeignKey("SenderId")]
        public User? Sender { get; set; }

        [ForeignKey("ReceiverId")]
        public User? Receiver { get; set; }
    }
}
