namespace SASMS.Models
{
    /// <summary>
    /// StudentAffairs role model - contains student affairs staff-specific data
    /// Linked to User table via UserId for authentication
    /// </summary>
    public class StudentAffairs
    {
        public int Id { get; set; }
        
        // Foreign key to User table
        public int UserId { get; set; }
        public User User { get; set; }
        
        public string Position { get; set; }
        public DateTime HireDate { get; set; }
        
        // Navigation properties
        public ICollection<Application> ProcessedApplications { get; set; }
        public ICollection<Attendance> RecordedAttendances { get; set; }
        public ICollection<Departure> ProcessedDepartures { get; set; }
        public ICollection<Complaint> HandledComplaints { get; set; }
        public ICollection<Suggestion> ReviewedSuggestions { get; set; }
        public ICollection<Activity> ManagedActivities { get; set; }
        public ICollection<Message> SentMessages { get; set; }
        public ICollection<Notification> CreatedNotifications { get; set; }
    }
}
