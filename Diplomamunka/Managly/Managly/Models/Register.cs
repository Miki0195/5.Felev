using System;
using System.ComponentModel.DataAnnotations;

namespace Managly.Models
{
    public class Register
    {
        [Required]
        [StringLength(100, MinimumLength = 3)]
        public string CompanyName { get; set; }

        [Required]
        public string LicenseKey { get; set; }

        [Required]
        [EmailAddress]
        public string AdminEmail { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; }
    }
}

