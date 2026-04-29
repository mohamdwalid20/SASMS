namespace SASMS.Models
{
    public class Payment
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public Student Student { get; set; }
        public int FeeId { get; set; }
        public Fee Fee { get; set; }
        public string PaymentNumber { get; set; } // Unique payment reference
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string PaymentMethod { get; set; } // Cash, BankTransfer, CreditCard, etc.
        public string Status { get; set; } // Pending, Completed, Failed, Refunded
        public string? TransactionId { get; set; }
        public string? BankReference { get; set; }
        public string? Notes { get; set; }
        public int? ProcessedById { get; set; }
        public User? ProcessedBy { get; set; } // Admin or StudentAffairs
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        public ICollection<Receipt> Receipts { get; set; }
    }
}
