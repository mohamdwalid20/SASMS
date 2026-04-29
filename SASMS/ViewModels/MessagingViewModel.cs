using SASMS.Models;

namespace SASMS.ViewModels
{
    public class MessagingViewModel
    {
        public List<UserViewModel> Users { get; set; }
        public List<Message> CurrentConversation { get; set; }
    }

    public class UserViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ProfilePicturePath { get; set; }
        public string Role { get; set; }
        public string LastMessage { get; set; }
        public DateTime? LastMessageDate { get; set; }
        public int UnreadCount { get; set; }
    }
}
