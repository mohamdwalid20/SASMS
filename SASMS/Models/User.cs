namespace SASMS.Models
{
    /// <summary>
    /// User authentication model - contains only login credentials and basic info
    /// Role-specific data is stored in Admin, Student, StudentAffairs, or Applicant tables
    /// </summary>
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string NationalId { get; set; }
        public string PhoneNumber { get; set; }
        public string? ProfilePicturePath { get; set; } // Profile picture path for all users
        public UserRole Role { get; set; } // Admin, StudentAffairs, Applicant, Student
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsDarkMode { get; set; } = false; // Dark Mode preference
        public string Language { get; set; } = "en"; // Language preference (en, ar)
        public DateTime? LastSeen { get; set; } // Last activity timestamp

        // Notification Preferences
        public bool NotifyOnComplaints { get; set; } = true;
        public bool NotifyOnSuggestions { get; set; } = true;
        public bool NotifyOnApplications { get; set; } = true;
        public bool NotifyOnMessages { get; set; } = true;
        public bool NotifyOnAttendance { get; set; } = true;
        public bool NotifyOnFees { get; set; } = true;
        public bool NotifyOnActivityRegistration { get; set; } = true;
        public bool NotifyOnNewActivity { get; set; } = true;
        public bool NotifyOnSystemUpdates { get; set; } = true;
        
        // Navigation properties for role-specific data (one-to-one relationships)
        public Admin? Admin { get; set; }
        public Student? Student { get; set; }
        public StudentAffairs? StudentAffairs { get; set; }
        public Applicant? Applicant { get; set; }
    }
}
