namespace SASMS.Models
{
    public class Class
    {
        public int Id { get; set; }
        public string Name { get; set; } // e.g., "Class 1A", "Class 2B"
        public string Code { get; set; }
        public int DepartmentId { get; set; }
        public Department Department { get; set; }
        public int? AcademicYearId { get; set; }
        public AcademicYear? AcademicYear { get; set; }
        public string Semester { get; set; } // Fall, Spring, Summer
        public int? Capacity { get; set; }
        public int? RoomNumber { get; set; }
        public string? Schedule { get; set; } // Class schedule information
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        
        // Navigation properties
        public ICollection<Student> Students { get; set; }
    }
}
