namespace SASMS.Models
{
    /// <summary>
    /// Student Score model - tracks entrance exam scores for applicants
    /// </summary>
    public class StudentScore
    {
        public int Id { get; set; }
        public int ApplicantId { get; set; }
        public Applicant Applicant { get; set; }
        public string ExamType { get; set; } // English, Math, Science, Arabic, Interview
        public decimal Score { get; set; }
        public decimal MaxScore { get; set; }
        public DateTime ExamDate { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
