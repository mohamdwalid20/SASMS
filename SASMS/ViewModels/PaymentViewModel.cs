namespace SASMS.ViewModels
{
    public class PaymentViewModel
    {
        public int Id { get; set; }
        public string PaymentNumber { get; set; }
        public string StudentId { get; set; }
        public string StudentName { get; set; }
        public string StudentProfilePicture { get; set; }
        public string ClassName { get; set; }
        public string FeeName { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal OutstandingAmount { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string Status { get; set; } // Paid, Unpaid, Partial, Overdue
        public string PaymentMethod { get; set; }
    }
}
