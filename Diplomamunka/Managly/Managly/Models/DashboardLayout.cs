using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Managly.Models
{
    public class DashboardLayout
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public string LayoutData { get; set; }

        public DateTime LastUpdated { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }
} 