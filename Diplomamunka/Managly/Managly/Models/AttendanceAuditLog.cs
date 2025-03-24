using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Managly.Models
{
    public class AttendanceAuditLog
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int AttendanceId { get; set; }
        
        [ForeignKey("AttendanceId")]
        public Attendance Attendance { get; set; }
        
        [Required]
        public string ModifiedByUserId { get; set; }
        
        [ForeignKey("ModifiedByUserId")]
        public User ModifiedBy { get; set; }
        
        [Required]
        public DateTime ModificationTime { get; set; }
        
        public string Notes { get; set; }
    }
}

