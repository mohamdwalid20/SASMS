namespace SASMS.Models
{
    public class Departure
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public Student Student { get; set; }
        public DateTime DepartureDate { get; set; }
        public TimeOnly DepartureTime { get; set; }
        public TimeOnly? ReturnTime { get; set; }
        public string Reason { get; set; }
        public string Destination { get; set; }
        public string Status { get; set; } // Pending, Approved, Rejected, Completed
        public string? ApprovalNotes { get; set; }
        public int? ApprovedById { get; set; }
        public User? ApprovedBy { get; set; } // StudentAffairs or Admin
        public DateTime? ApprovalDate { get; set; }
        public string? EmergencyContact { get; set; }
        public string? EmergencyPhone { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
