using SASMS.Models;

namespace SASMS.Services
{
    public interface INotificationService
    {
        Task CreateNotificationAsync(Notification notification);
        Task NotifyUserAsync(int userId, string title, string message, string category = "Info", string actionUrl = "");
        Task SendActivityRegistrationNotification(int studentId, int activityId);
    }
}
