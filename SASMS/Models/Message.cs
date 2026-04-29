namespace SASMS.Models
{
    public class Message
    {
        public int Id { get; set; }
        public int? SenderId { get; set; } // Nullable for system messages
        public User? Sender { get; set; } // Student, Admin, or StudentAffairs
        public int? ReceiverId { get; set; } // Nullable for broadcast messages
        public User? Receiver { get; set; } // Student, Admin, or StudentAffairs
        public string Subject { get; set; }
        public string Content { get; set; }
        public string MessageType { get; set; } // Direct, Broadcast, Announcement
        public string Priority { get; set; } // Low, Normal, High, Urgent
        public bool IsRead { get; set; } = false;
        public DateTime? ReadAt { get; set; }
        public bool IsDeletedBySender { get; set; } = false;
        public bool IsDeletedByReceiver { get; set; } = false;
        public DateTime SentAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public string? VoiceMessagePath { get; set; }
        public int? VoiceMessageDuration { get; set; } // Duration in seconds
        
        // Navigation properties
        public ICollection<MessageAttachment> Attachments { get; set; }
    }
}
