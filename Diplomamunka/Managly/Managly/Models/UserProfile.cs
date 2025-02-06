using System;
using System.ComponentModel.DataAnnotations;
using Managly.Helpers;

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

        [Required]
        public string SelectedCountryCode { get; set; } = "";

        [Phone]
        [Required]
        public string PhoneNumber { get; set; } = "";

        [Required]
        public string Country { get; set; } = "";

        [Required]
        public string City { get; set; } = "";

        [Required]
        public string Address { get; set; } = "";

        [Required]
        public DateTime? DateOfBirth { get; set; }

        [Required]
        public string Gender { get; set; } = "";

        public string ProfilePicturePath { get; set; } = "/images/default/default-profile.png";

        [Display(Name = "Profile Picture")]
        [DataType(DataType.Upload)]
        public IFormFile? ProfilePicture { get; set; } 

        public List<(string Code, string Name)> CountryCodes { get; set; } = CountryCallingCodes.GetCountryCodes();

        public List<string> GenderOptions { get; set; } = new List<string> { "Male", "Female", "Other" };
    }
}

