using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SASMS.Models
{
    public class ActivityLog
    {
        [Key]
        public int Id { get; set; }

        public int? UserId { get; set; }
        
        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [MaxLength(200)]
        public string UserName { get; set; }

        [MaxLength(50)]
        public string UserRole { get; set; }

        [Required]
        [MaxLength(100)]
        public string Action { get; set; }

        [MaxLength(100)]
        public string EntityName { get; set; }

        [MaxLength(100)]
        public string EntityId { get; set; }

        public string Details { get; set; }

        public DateTime Timestamp { get; set; }

        [MaxLength(50)]
        public string IpAddress { get; set; }

        public string UserAgent { get; set; }
    }
}
