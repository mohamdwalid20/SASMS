using System.ComponentModel.DataAnnotations;

namespace SASMS.ViewModels
{
    public class ActivityViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        [SASMS.Attributes.DateGreaterThan("StartDate", ErrorMessage = "End Date must be after or equal to Start Date.")]
        public DateTime EndDate { get; set; }
        public string Location { get; set; }
        public int? Capacity { get; set; }
        public int CurrentParticipants { get; set; }
        public string Status { get; set; } // Upcoming, Ongoing, Completed, Cancelled
        public bool RequiresRegistration { get; set; }
        public DateTime? RegistrationDeadline { get; set; }
        public string ManagedByName { get; set; }
        public string? ManagedByProfilePicture { get; set; }
        public decimal? Fee { get; set; }
        public string? ImagePath { get; set; }
        public string GradientColors { get; set; } // For card styling
        public int? CreatedById { get; set; }
    }
}
