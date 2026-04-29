namespace SASMS.Models
{
    /// <summary>
    /// System Settings model - stores configurable system settings
    /// </summary>
    public class SystemSettings
    {
        public int Id { get; set; }
        public string Key { get; set; } // Unique setting key
        public string Value { get; set; } // Setting value
        public string? Description { get; set; }
        public string Category { get; set; } // General, Admission, Fees, etc.
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
