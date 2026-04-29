namespace SASMS.Models
{
    /// <summary>
    /// Applicant role model - contains applicant-specific data
    /// Linked to User table via UserId for authentication
    /// </summary>
    public class Applicant
    {
        public int Id { get; set; }
        
        // Foreign key to User table
        public int UserId { get; set; }
        public User User { get; set; }
        
        // Applicant-specific properties
        public DateOnly DateOfBirth { get; set; }
        public string Gender { get; set; }
        public string Grade { get; set; }
        public string Address { get; set; }
        public double GradeOfPrimarySchool { get; set; }
        public double GradeOfEnglishExam { get; set; }
        public double GradeOfMathExam { get; set; }
        public double GradeOfScienceExam { get; set; }
        public int PreferredDepartmentId { get; set; }
        public Department PreferredDepartment { get; set; }
        public string ParentName { get; set; }
        public string ParentPhone { get; set; }
        public string Kinship { get; set; }
        public string ParentMajor { get; set; }
        
        // Navigation properties
        public ICollection<Application> Applications { get; set; }
        public ICollection<StudentScore> Scores { get; set; }
    }
}
