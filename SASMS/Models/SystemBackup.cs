using System;
using System.ComponentModel.DataAnnotations;

namespace SASMS.Models
{
    public class SystemBackup
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string FileName { get; set; }

        [Required]
        [MaxLength(50)]
        public string Type { get; set; } // "Database" or "Files"

        public long SizeBytes { get; set; }

        [Required]
        public string FilePath { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(100)]
        public string CreatedBy { get; set; }

        public bool IsManual { get; set; } = true;
    }
}
