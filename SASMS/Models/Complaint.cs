namespace SASMS.Models
{
    public class Complaint
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public Student Student { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Category { get; set; } // Academic, Administrative, Facilities, Services, etc.
        public string Priority { get; set; } // Low, Medium, High, Urgent
        public string Status { get; set; } // Pending, InProgress, Resolved, Closed, Rejected
        public DateTime ComplaintDate { get; set; }
        public DateTime? ResolutionDate { get; set; }
        public string? ResolutionNotes { get; set; }
        public int? AssignedToId { get; set; }
        public User? AssignedTo { get; set; } // Admin or StudentAffairs
        public int? ResolvedById { get; set; }
        public User? ResolvedBy { get; set; }
        public string? Response { get; set; }
        public bool IsAnonymous { get; set; } = false;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        public ICollection<ComplaintAttachment> Attachments { get; set; }
    }
}
