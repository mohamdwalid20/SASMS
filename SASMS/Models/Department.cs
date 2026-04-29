namespace SASMS.Models
{
    public class Department
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; } // Department code
        public string Description { get; set; }
        public string HeadOfDepartment { get; set; }
        public int Capacity { get; set; } // Maximum number of students
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        
        // Navigation properties
        public ICollection<Student> Students { get; set; }
        public ICollection<Applicant> Applicants { get; set; }
        public ICollection<Application> Applications { get; set; }
        public ICollection<Class> Classes { get; set; }
        public ICollection<Fee> Fees { get; set; }
    }
}
