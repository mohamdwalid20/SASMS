using System;

namespace SASMS.Models
{
    public class FeeSubmission
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public virtual Student? Student { get; set; }
        
        public int GovernmentFeeId { get; set; }
        public virtual GovernmentFee? GovernmentFee { get; set; }
        
        public int AcademicYearId { get; set; }
        public virtual AcademicYear? AcademicYear { get; set; }

        public string? ReceiptNumber { get; set; }
        public string? ReceiptImagePath { get; set; }
        
        // Status: PendingSubmission, Submitted, Approved, Rejected
        public string Status { get; set; } = "PendingSubmission";
        
        public string? RejectionReason { get; set; }
        public DateTime? SubmissionDate { get; set; }
        
        public int? ProcessedById { get; set; }
        public User? ProcessedBy { get; set; }
        public DateTime? ProcessedAt { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
