using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SASMS.Data;
using SASMS.Hubs;
using SASMS.Models;

namespace SASMS.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<SASMSHub> _hubContext;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(ApplicationDbContext context, IHubContext<SASMSHub> hubContext, ILogger<NotificationService> logger)
        {
            _context = context;
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task CreateNotificationAsync(Notification notification)
        {
            if (notification.CreatedAt == default)
            {
                notification.CreatedAt = DateTime.UtcNow;
            }

            if (string.IsNullOrEmpty(notification.Type))
            {
                notification.Type = "Info";
            }

            if (string.IsNullOrEmpty(notification.Priority))
            {
                notification.Priority = "Normal";
            }

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Push to Windows Desktop/Browser via SignalR
            if (notification.UserId.HasValue)
            {
                await SendDesktopNotificationAsync(notification);
            }
        }

        public async Task NotifyUserAsync(int userId, string title, string message, string category = "Info", string actionUrl = "")
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Category = category,
                ActionUrl = actionUrl,
                Type = "Info",      // Explicitly set default
                Priority = "Normal", // Explicitly set default
                CreatedAt = DateTime.UtcNow
            };
            await CreateNotificationAsync(notification);
        }

        public async Task SendActivityRegistrationNotification(int studentId, int activityId)
        {
            var student = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.Id == studentId);
            var activity = await _context.Activities.FindAsync(activityId);

            if (student == null || activity == null) return;

            var staffToNotify = await _context.Users
                .Where(u => u.IsActive && u.NotifyOnActivityRegistration && 
                            (u.Role == UserRole.Admin || u.Role == UserRole.StudentAffairs || u.Role == UserRole.Supervisor || u.Id == activity.CreatedById))
                .ToListAsync();

            foreach (var staff in staffToNotify)
            {
                await NotifyUserAsync(
                    staff.Id,
                    "New Activity Registration",
                    $"Student '{student.User.Name}' has registered for activity '{activity.Title}'.",
                    "Activity",
                    $"/Activity/Participants/{activity.Id}"
                );
            }
        }

        private async Task SendDesktopNotificationAsync(Notification notification)
        {
            try
            {
                var payload = new
                {
                    id = notification.Id,
                    title = notification.Title,
                    message = notification.Message,
                    actionUrl = notification.ActionUrl,
                    category = notification.Category,
                    createdAt = notification.CreatedAt
                };

                await _hubContext.Clients.User(notification.UserId.ToString()).SendAsync("ReceiveNewNotification", payload);
                _logger.LogInformation($"Desktop notification sent to user {notification.UserId} for notification {notification.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send desktop notification to user {notification.UserId}");
            }
        }
    }
}
