using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Managly.Models
{
    public class User : IdentityUser
    {
        [Required]
        public string Name { get; set; }

        public string LastName { get; set; }

        public string Country { get; set; }

        public string City { get; set; }

        public string Address { get; set; }

        public int? CompanyId { get; set; }

        [ForeignKey("CompanyId")]
        public Company Company { get; set; }

        public bool IsUsingPreGeneratedPassword { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public string Gender { get; set; } = "Other";

        public string ProfilePicturePath { get; set; } = "/images/default/default-profile.png";
    }
}

