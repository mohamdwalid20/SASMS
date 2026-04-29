using System.ComponentModel.DataAnnotations;

namespace SASMS.Models
{
    /// <summary>
    /// Academic Year model - tracks school years for organization
    /// </summary>
    public class AcademicYear
    {
        public int Id { get; set; }
        public string Year { get; set; } // e.g., "2025-2026"
        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        [SASMS.Attributes.DateGreaterThan("StartDate", ErrorMessage = "End Date must be after or equal to Start Date.")]
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        
        // Navigation properties
        public ICollection<Student> Students { get; set; }
        public ICollection<Class> Classes { get; set; }
        public ICollection<Application> Applications { get; set; }
    }
}
