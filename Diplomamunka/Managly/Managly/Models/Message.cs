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
        public string MessageContent { get; set; }

        public DateTime SentDate { get; set; } = DateTime.Now;

        public string SenderId { get; set; } 
        public User Sender { get; set; }

        public string ReceiverId { get; set; } 
        public User Receiver { get; set; }
    }
}

