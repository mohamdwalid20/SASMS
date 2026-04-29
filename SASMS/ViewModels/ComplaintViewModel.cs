namespace SASMS.ViewModels
{
    public class ComplaintViewModel
    {
        public int Id { get; set; }
        public string ComplaintId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string StudentName { get; set; }
        public string StudentProfilePicture { get; set; }
        public string SubmittedBy { get; set; }
        public string Role { get; set; } // Student, Parent, Staff
        public string Category { get; set; }
        public string Priority { get; set; } // Low, Medium, High, Urgent
        public string Status { get; set; } // Pending, InProgress, Resolved, Closed
        public DateTime ComplaintDate { get; set; }
        public DateTime? ResolutionDate { get; set; }
        public string AssignedToName { get; set; }
        public bool IsAnonymous { get; set; }
    }
}
