using Microsoft.AspNetCore.SignalR;

namespace SASMS.Hubs
{
    public class SASMSHub : Hub
    {
        // Generic method to notify all clients about a data update
        public async Task NotifyDataUpdate(string entityType, string action)
        {
            await Clients.All.SendAsync("ReceiveDataUpdate", entityType, action);
        }

        // Method to send notifications to specific users
        public async Task SendNotification(string userId, string message)
        {
            await Clients.User(userId).SendAsync("ReceiveNotification", message);
        }

        // Method to send structured notification object to a specific user (for Windows OS notifications)
        public async Task SendNotificationToUser(string userId, object notificationData)
        {
            await Clients.User(userId).SendAsync("ReceiveNewNotification", notificationData);
        }
    }
}
