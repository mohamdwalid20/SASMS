namespace SASMS.Models
{
    public class Receipt
    {
        public int Id { get; set; }
        public int PaymentId { get; set; }
        public Payment Payment { get; set; }
        public string ReceiptNumber { get; set; } // Unique receipt number
        public DateTime IssueDate { get; set; }
        public string FilePath { get; set; } // PDF or image path
        public bool IsIssued { get; set; } = false;
        public int? IssuedById { get; set; }
        public User? IssuedBy { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
