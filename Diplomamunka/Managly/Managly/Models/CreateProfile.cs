using System.ComponentModel.DataAnnotations;

namespace Managly.Models
{
    public class CreateProfile
    {
        [Required]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Role { get; set; } // Employee or Manager
    }
}
