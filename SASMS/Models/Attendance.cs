namespace SASMS.Models
{
    public class Attendance
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public Student Student { get; set; }
        public DateTime AttendanceDate { get; set; }
        public TimeOnly CheckInTime { get; set; }
        public TimeOnly? CheckOutTime { get; set; }
        public string Status { get; set; } // Present, Absent, Late, Excused
        public string? Notes { get; set; }
        public int? RecordedById { get; set; }
        public User? RecordedBy { get; set; } // StudentAffairs or Admin
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
