using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SASMS.Data;
using SASMS.Models;
using System.Security.Claims;

namespace SASMS.ViewComponents
{
    public class NotificationViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public NotificationViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userIdClaim = UserClaimsPrincipal.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) 
            {
                // Fallback: If not logged in or claim missing, show empty notifications but render the icon
                return View(new List<Notification>());
            }

            int userId = int.Parse(userIdClaim.Value);

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .Take(5)
                .ToListAsync();

            return View(notifications);
        }
    }
}
