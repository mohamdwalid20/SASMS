using System;
using System.Collections.Generic;

namespace SASMS.Models
{
    public class GovernmentFee
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? Description { get; set; }
        public int AcademicYearId { get; set; }
        public virtual AcademicYear? AcademicYear { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsPublished { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<FeeSubmission>? Submissions { get; set; }
    }
}
