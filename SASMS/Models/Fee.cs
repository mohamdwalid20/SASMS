namespace SASMS.Models
{
    public class Fee
    {
        public int Id { get; set; }
        public string Name { get; set; } // e.g., "Tuition Fee", "Registration Fee"
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public int? DepartmentId { get; set; } // If null, applies to all departments
        public Department? Department { get; set; }
        public string FeeType { get; set; } // Tuition, Registration, Activity, Library, etc.
        public string Frequency { get; set; } // OneTime, Monthly, Semester, Annual
        public DateTime DueDate { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        public ICollection<Payment> Payments { get; set; }
    }
}
