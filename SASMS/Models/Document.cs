namespace SASMS.Models
{
    public class Document
    {
        public int Id { get; set; }
        public int ApplicationId { get; set; }
        public Application Application { get; set; }
        public string DocumentType { get; set; } // BirthCertificate, NationalId, Transcript, etc.
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public long FileSize { get; set; } // in bytes
        public string ContentType { get; set; }
        public bool IsVerified { get; set; } = false;
        public DateTime? VerificationDate { get; set; }
        public int? VerifiedById { get; set; }
        public User? VerifiedBy { get; set; }
        public string? VerificationNotes { get; set; }
        public DateTime UploadedAt { get; set; }
    }
}
