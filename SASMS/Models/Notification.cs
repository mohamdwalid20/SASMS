namespace SASMS.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public int? UserId { get; set; } // Nullable for broadcast notifications
        public User? User { get; set; } // Target user (Student, Admin, or StudentAffairs)
        public string Title { get; set; }
        public string Message { get; set; }
        public string Type { get; set; } // Info, Warning, Success, Error, Alert
        public string Category { get; set; } // Application, Payment, Attendance, Activity, Complaint, etc.
        public string Priority { get; set; } // Low, Normal, High, Urgent
        public bool IsRead { get; set; } = false;
        public DateTime? ReadAt { get; set; }
        public string? ActionUrl { get; set; } // Link to related page
        public int? CreatedById { get; set; }
        public User? CreatedBy { get; set; } // Admin or StudentAffairs
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; } // Optional expiration date
    }
}
