using System;
using System.ComponentModel.DataAnnotations;

namespace Managly.Models
{
    public class Company
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string LicenseKey { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public ICollection<User> Users { get; set; }
    }
}

