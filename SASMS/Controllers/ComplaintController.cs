using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SASMS.Data;
using SASMS.Models;
using SASMS.ViewModels;
using SASMS.Services;

namespace SASMS.Controllers
{
    [Authorize(Policy = "SupervisorOnly")]
    public class ComplaintController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ComplaintController> _logger;
        private readonly INotificationService _notificationService;
        private readonly IActivityLogService _activityLogService;
        private readonly Microsoft.AspNetCore.SignalR.IHubContext<SASMS.Hubs.SASMSHub> _hubContext;

        public ComplaintController(ApplicationDbContext context, ILogger<ComplaintController> logger, Microsoft.AspNetCore.SignalR.IHubContext<SASMS.Hubs.SASMSHub> hubContext, INotificationService notificationService, IActivityLogService activityLogService)
        {
            _context = context;
            _logger = logger;
            _hubContext = hubContext;
            _notificationService = notificationService;
            _activityLogService = activityLogService;
        }

        public IActionResult Index() => RedirectToAction("List");

        // GET: /Complaint/List
        public async Task<IActionResult> List(string status = "", string category = "")
        {
            var complaintsQuery = _context.Complaints
                .Include(c => c.Student)
                    .ThenInclude(s => s.User)
                .Include(c => c.AssignedTo)
                .AsQueryable();

            // Apply status filter
            if (!string.IsNullOrWhiteSpace(status))
            {
                complaintsQuery = complaintsQuery.Where(c => c.Status == status);
            }

            // Apply category filter
            if (!string.IsNullOrWhiteSpace(category))
            {
                complaintsQuery = complaintsQuery.Where(c => c.Category == category);
            }

            var complaints = await complaintsQuery
                .Select(c => new ComplaintViewModel
                {
                    Id = c.Id,
                    ComplaintId = $"C-{c.Id:D4}",
                    Title = c.Title,
                    Description = c.Description,
                    StudentName = c.Student.User.Name,
                    StudentProfilePicture = c.Student.ProfilePicturePath ?? c.Student.User.ProfilePicturePath,
                    SubmittedBy = c.Student.User.Name,
                    Role = "Student",
                    Category = c.Category,
                    Priority = c.Priority,
                    Status = c.Status,
                    ComplaintDate = c.ComplaintDate,
                    ResolutionDate = c.ResolutionDate,
                    AssignedToName = c.AssignedTo != null ? c.AssignedTo.Name : "Unassigned",
                    IsAnonymous = c.IsAnonymous
                })
                .OrderByDescending(c => c.ComplaintDate)
                .ToListAsync();

            ViewBag.StatusFilter = status;
            ViewBag.CategoryFilter = category;
            ViewBag.PendingCount = await _context.Complaints.CountAsync(c => c.Status == "Pending");
            ViewBag.InProgressCount = await _context.Complaints.CountAsync(c => c.Status == "InProgress");

            return View(complaints);
        }

        // GET: /Complaint/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var complaint = await _context.Complaints
                .Include(c => c.Student)
                    .ThenInclude(s => s.User)
                .Include(c => c.AssignedTo)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (complaint == null)
            {
                return NotFound();
            }

            // Mark as In Progress if currently Pending when viewed by Admin
            if (complaint.Status == "Pending")
            {
                complaint.Status = "InProgress";
                await _activityLogService.LogActivityAsync("Complaint Viewed", "Complaint", complaint.Id.ToString(), $"Complaint '{complaint.Title}' moved to InProgress upon viewing.");
                await _context.SaveChangesAsync();
            }

            return View(complaint);
        }

        // POST: /Complaint/UpdateStatus
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var complaint = await _context.Complaints
                .Include(c => c.Student)
                    .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (complaint == null)
            {
                return Json(new { success = false, message = "Complaint not found" });
            }

            complaint.Status = status;
            if (status == "Resolved" || status == "Closed")
            {
                complaint.ResolutionDate = DateTime.UtcNow;

                // Send notification to the student (if enabled)
                if (complaint.Student.User.NotifyOnComplaints && complaint.Student.User.IsActive)
                {
                    await _notificationService.NotifyUserAsync(
                        complaint.Student.UserId,
                        "Complaint Resolved",
                        $"Your complaint '{complaint.Title}' has been resolved by the administration.",
                        "Complaint",
                        "/StudentPortal/Complaints"
                    );
                }
            }

            await _activityLogService.LogActivityAsync("Complaint Status Updated", "Complaint", complaint.Id.ToString(), $"Complaint '{complaint.Title}' status changed to {status}");
            await _context.SaveChangesAsync();

            // Broadcast real-time update
            Console.WriteLine($"[SignalR BROADCAST] Entity: Complaint, Action: {status}");
            await _hubContext.Clients.All.SendAsync("ReceiveDataUpdate", "Complaint", status);

            return Json(new { success = true, message = $"Complaint {status.ToLower()} successfully" });
        }
        // POST: /Complaint/Delete/5
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var complaint = await _context.Complaints.FindAsync(id);
            if (complaint == null)
            {
                return Json(new { success = false, message = "Complaint not found" });
            }

            _context.Complaints.Remove(complaint);
            await _activityLogService.LogActivityAsync("Complaint Deleted", "Complaint", id.ToString(), $"Complaint '{complaint.Title}' was deleted.");
            await _context.SaveChangesAsync();

            // Broadcast real-time update
            Console.WriteLine($"[SignalR BROADCAST] Entity: Complaint, Action: Deleted");
            await _hubContext.Clients.All.SendAsync("ReceiveDataUpdate", "Complaint", "Deleted");

            return Json(new { success = true, message = "Complaint deleted successfully" });
        }
    }
}
    