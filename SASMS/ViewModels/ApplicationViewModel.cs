namespace SASMS.ViewModels
{
    public class ApplicationViewModel
    {
        public int Id { get; set; }
        public string ApplicationNumber { get; set; }
        public string ApplicantName { get; set; }
        public string ApplicantEmail { get; set; }
        public string Grade { get; set; }
        public string PreferredDepartment { get; set; }
        public string ParentName { get; set; }
        public string ParentPhone { get; set; }
        public DateTime ApplicationDate { get; set; }
        public string Status { get; set; } // Pending, UnderReview, Approved, Rejected
        public decimal? TotalScore { get; set; }
        public bool IsEligible { get; set; }
        public DateTime? ReviewDate { get; set; }
        public string ReviewedByName { get; set; }
    }
}
