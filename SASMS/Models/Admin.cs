namespace SASMS.Models
{
    /// <summary>
    /// Admin role model - contains admin-specific data
    /// Linked to User table via UserId for authentication
    /// </summary>
    public class Admin
    {
        public int Id { get; set; }
        
        // Foreign key to User table
        public int UserId { get; set; }
        public User User { get; set; }
        
        // Admin-specific properties
        public string Position { get; set; }
        public DateTime HireDate { get; set; }
        
        // Navigation properties
        public ICollection<Application> ProcessedApplications { get; set; }
        public ICollection<Complaint> HandledComplaints { get; set; }
        public ICollection<Suggestion> ReviewedSuggestions { get; set; }
        public ICollection<Message> SentMessages { get; set; }
        public ICollection<Notification> CreatedNotifications { get; set; }
    }
}
