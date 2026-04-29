namespace SASMS.ViewModels
{
    public class SuggestionViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string StudentName { get; set; }
        public string StudentProfilePicture { get; set; }
        public string Category { get; set; }
        public string Status { get; set; } // Pending, UnderReview, Approved, Implemented, Rejected
        public DateTime SuggestionDate { get; set; }
        public int? Upvotes { get; set; }
        public bool IsAnonymous { get; set; }
        public string ReviewNotes { get; set; }
        public DateTime? ReviewDate { get; set; }
    }
}
