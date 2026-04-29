using System;
using System.ComponentModel.DataAnnotations;

namespace SASMS.Models
{
    public class RiskAlert
    {
        public int Id { get; set; }

        [Required]
        public string Type { get; set; } // "Attendance", "Financial", "Application", "Complaint"

        [Required]
        public string Message { get; set; }

        public int? StudentId { get; set; }
        public virtual Student? Student { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsResolved { get; set; } = false;
        
        public DateTime? ResolvedAt { get; set; }
        
        public int? ResolvedById { get; set; }
        public virtual User? ResolvedBy { get; set; }
    }
}
