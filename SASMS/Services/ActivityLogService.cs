using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SASMS.Data;
using SASMS.Models;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SASMS.Services
{
    public class ActivityLogService : IActivityLogService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ActivityLogService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogActivityAsync(int? userId, string action, string entityName, string entityId, string details)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var ipAddress = httpContext?.Connection?.RemoteIpAddress?.ToString();
            var userAgent = httpContext?.Request?.Headers["User-Agent"].ToString();

            string userName = "System";
            string userRole = "System";

            if (userId.HasValue)
            {
                var user = await _context.Users.FindAsync(userId.Value);
                if (user != null)
                {
                    userName = user.Name;
                    userRole = user.Role.ToString();
                }
            }

            var log = new ActivityLog
            {
                UserId = userId,
                UserName = userName,
                UserRole = userRole,
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                Details = details,
                Timestamp = DateTime.UtcNow,
                IpAddress = ipAddress,
                UserAgent = userAgent
            };

            _context.ActivityLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        public async Task LogActivityAsync(string action, string entityName, string entityId, string details)
        {
            var context = _httpContextAccessor.HttpContext;
            int? userId = null;

            if (context?.User?.Identity?.IsAuthenticated == true)
            {
                var userIdStr = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(userIdStr, out int id))
                {
                    userId = id;
                }
            }

            await LogActivityAsync(userId, action, entityName, entityId, details);
        }
    }
}
