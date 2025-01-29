using System;
using System.ComponentModel.DataAnnotations;

namespace Managly.Models
{
    public class UserProfile
    {
        [Required]
        public string Name { get; set; } = ""; // Auto-filled

        [Required]
        public string LastName { get; set; } = "";

        [EmailAddress]
        public string Email { get; set; } = ""; // Auto-filled, cannot change

        [Phone]
        [Required]
        public string PhoneNumber { get; set; } = "";

        [Required]
        public string Country { get; set; } = "";

        [Required]
        public string City { get; set; } = "";

        [Required]
        public string Address { get; set; } = "";
    }
}

