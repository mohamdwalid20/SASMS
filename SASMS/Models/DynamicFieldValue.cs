using System.ComponentModel.DataAnnotations;

namespace SASMS.Models
{
    public class DynamicFieldValue
    {
        public int Id { get; set; }

        public int ApplicationId { get; set; }
        public Application Application { get; set; }

        public int FieldId { get; set; }
        public DynamicField Field { get; set; }

        public string? Value { get; set; } // Stores the text value or file path
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
