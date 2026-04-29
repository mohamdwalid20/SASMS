using System.ComponentModel.DataAnnotations;

namespace SASMS.Models
{
    public enum FieldType
    {
        Text,
        Number,
        Date,
        Select,
        File,
        TextArea
    }

    public enum FormSection
    {
        Personal,
        Academic,
        Guardian
    }

    public class DynamicField
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Label { get; set; }

        [Required]
        [MaxLength(50)]
        public string FieldName { get; set; } // Unique identifier for the field in the form

        [Required]
        public FieldType Type { get; set; }

        public bool IsRequired { get; set; }

        public string? Options { get; set; } // Comma separated for Select type

        [Required]
        public FormSection Section { get; set; }

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
