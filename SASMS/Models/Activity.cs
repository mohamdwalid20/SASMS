using System.ComponentModel.DataAnnotations;

namespace SASMS.Models
{
    public class Activity
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Category { get; set; } // Sports, Cultural, Academic, Social, Volunteer, etc.
        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        [SASMS.Attributes.DateGreaterThan("StartDate", ErrorMessage = "End Date must be after or equal to Start Date.")]
        public DateTime EndDate { get; set; }

        public TimeOnly? StartTime { get; set; }

        [SASMS.Attributes.TimeGreaterThan("StartTime", ErrorMessage = "End Time must be after Start Time.")]
        public TimeOnly? EndTime { get; set; }
        public string Location { get; set; }
        public bool IsUnlimitedCapacity { get; set; } = false;
        public int? Capacity { get; set; }
        public int CurrentParticipants { get; set; } = 0;
        public string Status { get; set; } // Upcoming, Ongoing, Completed, Cancelled
        public bool RequiresRegistration { get; set; } = false;
        public DateTime? RegistrationDeadline { get; set; }
        public int? ManagedById { get; set; }
        public User? ManagedBy { get; set; } // StudentAffairs or Admin
        public int? CreatedById { get; set; }
        public User? CreatedBy { get; set; }
        public string? Requirements { get; set; }
        public decimal? Fee { get; set; }
        public string? ImagePath { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        public ICollection<ActivityParticipation> Participations { get; set; }
    }
}
