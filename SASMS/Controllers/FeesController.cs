using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SASMS.Data;
using SASMS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Localization;
using SASMS.Services;

namespace SASMS.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class FeesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IStringLocalizer<SharedResources> _localizer;
        private readonly INotificationService _notificationService;
        private readonly IActivityLogService _activityLogService;
        private readonly Microsoft.AspNetCore.SignalR.IHubContext<SASMS.Hubs.SASMSHub> _hubContext;

        public FeesController(ApplicationDbContext context, IStringLocalizer<SharedResources> localizer, Microsoft.AspNetCore.SignalR.IHubContext<SASMS.Hubs.SASMSHub> hubContext, INotificationService notificationService, IActivityLogService activityLogService)
        {
            _context = context;
            _localizer = localizer;
            _hubContext = hubContext;
            _notificationService = notificationService;
            _activityLogService = activityLogService;
        }

        // --- Government Fees Management (Admin only recommended, but StudentAffairs can also see) ---

        public async Task<IActionResult> Index()
        {
            var fees = await _context.GovernmentFees
                .Include(f => f.AcademicYear)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();
            return View(fees);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.AcademicYears = await _context.AcademicYears.Where(a => a.IsActive).ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(GovernmentFee fee)
        {
            if (ModelState.IsValid)
            {
                _context.GovernmentFees.Add(fee);
                await _activityLogService.LogActivityAsync("Fee Created", "Fee", fee.Id.ToString(), $"Government fee '{fee.Name}' for amount {fee.Amount} was created.");
                await _context.SaveChangesAsync();
                
                // Automatically create pending submissions for all active students if needed
                
                // Broadcast real-time update
                await _hubContext.Clients.All.SendAsync("ReceiveDataUpdate", "Fee", "Published");

                TempData["Success"] = "Submissions generated and fee published successfully.";
                return RedirectToAction(nameof(Index));
            }
            ViewBag.AcademicYears = await _context.AcademicYears.Where(a => a.IsActive).ToListAsync();
            return View(fee);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateSubmissions(int feeId)
        {
            var fee = await _context.GovernmentFees.FindAsync(feeId);
            if (fee == null) return NotFound();

            var students = await _context.Students.Where(s => s.Status == "Active").ToListAsync();
            var existingSubmissions = await _context.FeeSubmissions
                .Where(fs => fs.GovernmentFeeId == feeId)
                .Select(fs => fs.StudentId)
                .ToListAsync();

            var newSubmissions = students
                .Where(s => !existingSubmissions.Contains(s.Id))
                .Select(s => new FeeSubmission
                {
                    StudentId = s.Id,
                    GovernmentFeeId = feeId,
                    AcademicYearId = fee.AcademicYearId,
                    Status = "PendingSubmission",
                    ReceiptNumber = "N/A", // Placeholder
                    ReceiptImagePath = "placeholder.jpg", // Placeholder
                    CreatedAt = DateTime.UtcNow
                });

            _context.FeeSubmissions.AddRange(newSubmissions);
            
            fee.IsPublished = true;
            await _activityLogService.LogActivityAsync("Fee Submissions Generated", "Fee", feeId.ToString(), $"Generated {newSubmissions.Count()} submissions for fee '{fee.Name}'. Fee is now published.");
            await _context.SaveChangesAsync();

            // Notify Students (if enabled)
            var activeStudents = await _context.Users
                .Where(u => u.Role == UserRole.Student && u.IsActive && u.NotifyOnFees)
                .ToListAsync();

            foreach (var user in activeStudents)
            {
                await _notificationService.NotifyUserAsync(
                    user.Id,
                    "New Fee Assigned",
                    $"A new fee '{fee.Name}' has been assigned to you.",
                    "Fee",
                    "/StudentPortal/Fees"
                );
            }


            // Broadcast real-time update
            await _hubContext.Clients.All.SendAsync("ReceiveDataUpdate", "Fee", "Published");

            return Json(new { success = true, message = _localizer["SubmissionsGenerated"].Value });
        }

        // --- Submissions Review ---

        public async Task<IActionResult> Submissions(int? feeId, string status = "Submitted")
        {
            var query = _context.FeeSubmissions
                .Include(fs => fs.Student)
                    .ThenInclude(s => s.User)
                .Include(fs => fs.GovernmentFee)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(fs => fs.Status == status);
            }

            if (feeId.HasValue)
            {
                query = query.Where(fs => fs.GovernmentFeeId == feeId);
                var fee = await _context.GovernmentFees.FindAsync(feeId);
                ViewBag.FeeName = fee?.Name;
            }

            var submissions = await query
                .OrderByDescending(fs => fs.SubmissionDate ?? fs.CreatedAt)
                .ToListAsync();

            ViewBag.CurrentStatus = status;
            ViewBag.FeeId = feeId;
            return View(submissions);
        }

        [HttpPost]
        public async Task<IActionResult> ProcessSubmission(int id, string action, string? reason)
        {
            var submission = await _context.FeeSubmissions.FindAsync(id);
            if (submission == null) return NotFound();

            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

            if (action == "approve")
            {
                submission.Status = "Approved";
            }
            else if (action == "reject")
            {
                submission.Status = "Rejected";
                submission.RejectionReason = reason;
            }

            submission.ProcessedById = userId;
            submission.ProcessedAt = DateTime.UtcNow;

            await _activityLogService.LogActivityAsync("Fee Submission Processed", "FeeSubmission", id.ToString(), $"Submission for student ID {submission.StudentId} was '{action}'. {(action == "reject" ? "Reason: " + reason : "")}");
            await _context.SaveChangesAsync();

            // Notify Student (if enabled)
            var sub = await _context.FeeSubmissions
                .Include(fs => fs.Student)
                .ThenInclude(s => s.User)
                .Include(fs => fs.GovernmentFee)
                .FirstOrDefaultAsync(fs => fs.Id == id);

            if (sub != null && sub.Student.User.NotifyOnFees && sub.Student.User.IsActive)
            {
                await _notificationService.NotifyUserAsync(
                    sub.Student.UserId,
                    action == "approve" ? "Fee Submission Approved" : "Fee Submission Rejected",
                    action == "approve" 
                        ? $"Your submission for '{sub.GovernmentFee.Name}' has been approved."
                        : $"Your submission for '{sub.GovernmentFee.Name}' was rejected. Reason: {reason}",
                    "Fee",
                    "/StudentPortal/Fees"
                );
            }


            // Broadcast real-time update
            await _hubContext.Clients.All.SendAsync("ReceiveDataUpdate", "FeeSubmission", action);

            return Json(new { success = true, message = _localizer["StatusUpdated"].Value });
        }
    }
}
