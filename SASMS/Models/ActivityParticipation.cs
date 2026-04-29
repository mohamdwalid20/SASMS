namespace SASMS.Models
{
    public class ActivityParticipation
    {
        public int Id { get; set; }
        public int ActivityId { get; set; }
        public Activity Activity { get; set; }
        public int StudentId { get; set; }
        public Student Student { get; set; }
        public DateTime RegistrationDate { get; set; }
        public string Status { get; set; } // Registered, Confirmed, Attended, Absent, Cancelled
        public bool? IsApproved { get; set; } // null = pending, true = approved, false = rejected
        public bool Attended { get; set; } = false;
        public DateTime? AttendanceDate { get; set; }
        public string? Notes { get; set; }
        public decimal? FeePaid { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
