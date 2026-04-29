namespace SASMS.Models
{
    /// <summary>
    /// Student role model - contains student-specific data
    /// Linked to User table via UserId for authentication
    /// </summary>
    public class Student
    {
        public int Id { get; set; }
        
        // Foreign key to User table
        public int UserId { get; set; }
        public User User { get; set; }
        
        // Student-specific properties
        public string StudentId { get; set; } // Unique student identifier
        public DateOnly DateOfBirth { get; set; }
        public string Gender { get; set; }
        public string Address { get; set; }
        public string? ProfilePicturePath { get; set; } // Path to profile picture
        public int DepartmentId { get; set; }
        public Department Department { get; set; }
        public string? ClassName { get; set; } // Class name: J1, J2, W1, W2, S1, S2, etc.
        public int? AcademicYearId { get; set; }
        public AcademicYear? AcademicYear { get; set; }
        public DateTime EnrollmentDate { get; set; }
        public string Status { get; set; } // Active, Graduated, Suspended, etc.
        public string ParentName { get; set; }
        public string ParentPhone { get; set; }
        public string Kinship { get; set; }
        public string EmergencyContact { get; set; }
        public string EmergencyPhone { get; set; }
        
        // Navigation properties
        public ICollection<Attendance> Attendances { get; set; }
        public ICollection<Departure> Departures { get; set; }
        public ICollection<Payment> Payments { get; set; }
        public ICollection<Complaint> Complaints { get; set; }
        public ICollection<Suggestion> Suggestions { get; set; }
        public ICollection<ActivityParticipation> ActivityParticipations { get; set; }
        public ICollection<Message> SentMessages { get; set; }
        public ICollection<Message> ReceivedMessages { get; set; }
        public ICollection<Notification> Notifications { get; set; }
    }
}
