namespace SASMS.Models
{
    public class Application
    {
        public int Id { get; set; }
        public int ApplicantId { get; set; }
        public Applicant Applicant { get; set; }
        public int PreferredDepartmentId { get; set; }
        public Department PreferredDepartment { get; set; }
        public string ApplicationNumber { get; set; } // Unique application number
        public DateTime ApplicationDate { get; set; }
        public string Status { get; set; } // Pending, UnderReview, Approved, Rejected, Waitlisted
        public string? RejectionReason { get; set; }
        public DateTime? ReviewDate { get; set; }
        public int? ReviewedById { get; set; }
        public User? ReviewedBy { get; set; } // Admin or StudentAffairs
        public string? Notes { get; set; }
        public decimal? TotalScore { get; set; } // Calculated total entrance exam score
        public bool IsEligible { get; set; } = false; // Eligibility based on scores and requirements
        public int? AcademicYearId { get; set; }
        public AcademicYear? AcademicYear { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        public ICollection<Document> Documents { get; set; }
        public ICollection<DynamicFieldValue> DynamicFieldValues { get; set; }
    }
}
