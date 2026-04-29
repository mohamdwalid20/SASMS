namespace SASMS.Models
{
    public class Suggestion
    {
        public int Id { get; set; }
        public int? StudentId { get; set; } // Nullable for anonymous suggestions
        public Student? Student { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Category { get; set; } // Academic, Administrative, Facilities, Services, Activities, etc.
        public string Status { get; set; } // Pending, UnderReview, Approved, Implemented, Rejected
        public DateTime SuggestionDate { get; set; }
        public DateTime? ReviewDate { get; set; }
        public string? ReviewNotes { get; set; }
        public int? ReviewedById { get; set; }
        public User? ReviewedBy { get; set; } // Admin or StudentAffairs
        public bool IsAnonymous { get; set; } = false;
        public int? Upvotes { get; set; } = 0; // If you want to track popularity
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
