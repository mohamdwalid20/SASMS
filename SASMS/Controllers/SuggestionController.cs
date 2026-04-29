using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SASMS.Data;
using SASMS.Models;
using SASMS.ViewModels;
using Microsoft.Extensions.Localization;
using SASMS.Services;

namespace SASMS.Controllers
{
    [Authorize(Policy = "SupervisorOnly")]
    public class SuggestionController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SuggestionController> _logger;
        private readonly IHubContext<SASMS.Hubs.SASMSHub> _hubContext;
        private readonly IStringLocalizer<SharedResources> _localizer;
        private readonly INotificationService _notificationService;
        private readonly IActivityLogService _activityLogService;

        public SuggestionController(ApplicationDbContext context, ILogger<SuggestionController> logger, IHubContext<SASMS.Hubs.SASMSHub> hubContext, IStringLocalizer<SharedResources> localizer, INotificationService notificationService, IActivityLogService activityLogService)
        {
            _context = context;
            _logger = logger;
            _hubContext = hubContext;
            _localizer = localizer;
            _notificationService = notificationService;
            _activityLogService = activityLogService;
        }

        public IActionResult Index() => RedirectToAction("List");

        // GET: /Suggestion/List
        public async Task<IActionResult> List(string status = "", string category = "")
        {
            var suggestionsQuery = _context.Suggestions
                .Include(s => s.Student)
                    .ThenInclude(st => st.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                suggestionsQuery = suggestionsQuery.Where(s => s.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                suggestionsQuery = suggestionsQuery.Where(s => s.Category == category);
            }

            var suggestions = await suggestionsQuery
                .Select(s => new SuggestionViewModel
                {
                    Id = s.Id,
                    Title = s.Title,
                    Description = s.Description,
                    StudentName = s.IsAnonymous ? _localizer["AnonymousSuggestion"] : s.Student.User.Name,
                    StudentProfilePicture = s.IsAnonymous ? null : (s.Student.ProfilePicturePath ?? s.Student.User.ProfilePicturePath),
                    Category = s.Category,
                    Status = s.Status,
                    SuggestionDate = s.SuggestionDate,
                    Upvotes = s.Upvotes,
                    IsAnonymous = s.IsAnonymous,
                    ReviewNotes = s.ReviewNotes,
                    ReviewDate = s.ReviewDate
                })
                .OrderByDescending(s => s.SuggestionDate)
                .ToListAsync();

            ViewBag.StatusFilter = status;
            ViewBag.CategoryFilter = category;
            ViewBag.PendingCount = await _context.Suggestions.CountAsync(s => s.Status == "Pending");
            ViewBag.UnderReviewCount = await _context.Suggestions.CountAsync(s => s.Status == "UnderReview");

            return View(suggestions);
        }

        // POST: /Suggestion/UpdateStatus
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, string status, string notes = "")
        {
            var suggestion = await _context.Suggestions
                .Include(s => s.Student)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (suggestion == null)
            {
                return Json(new { success = false, message = "Suggestion not found" });
            }

            suggestion.Status = status;
            suggestion.ReviewNotes = notes;
            suggestion.ReviewDate = DateTime.UtcNow;
            suggestion.UpdatedAt = DateTime.UtcNow;

            // Send notification to the student if not anonymous (and if enabled)
            if (!suggestion.IsAnonymous && suggestion.Student != null && suggestion.Student.User.NotifyOnSuggestions && suggestion.Student.User.IsActive)
            {
                await _notificationService.NotifyUserAsync(
                    suggestion.Student.UserId,
                    _localizer["SuggestionUpdate"],
                    string.Format(_localizer["SuggestionStatusChanged"], suggestion.Title, _localizer[status]),
                    "Suggestion",
                    "/StudentPortal/Suggestions"
                );
            }

            await _activityLogService.LogActivityAsync("Suggestion Status Updated", "Suggestion", suggestion.Id.ToString(), $"Suggestion '{suggestion.Title}' status changed to {status}. Notes: {notes}");
            await _context.SaveChangesAsync();

            // Broadcast real-time update
            await _hubContext.Clients.All.SendAsync("ReceiveDataUpdate", "Suggestion", status);

            return Json(new { success = true, message = $"Suggestion {status} successfully" });
        }

        // POST: /Suggestion/Delete/5
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var suggestion = await _context.Suggestions.FindAsync(id);
            if (suggestion == null)
            {
                return Json(new { success = false, message = "Suggestion not found" });
            }

            _context.Suggestions.Remove(suggestion);
            await _activityLogService.LogActivityAsync("Suggestion Deleted", "Suggestion", id.ToString(), $"Suggestion '{suggestion.Title}' was deleted.");
            await _context.SaveChangesAsync();

            // Broadcast real-time update
            await _hubContext.Clients.All.SendAsync("ReceiveDataUpdate", "Suggestion", "Deleted");

            return Json(new { success = true, message = "Suggestion deleted successfully" });
        }
    }
}
