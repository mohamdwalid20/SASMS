using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SASMS.Data;
using SASMS.Models;
using SASMS.ViewModels;
using SASMS.Services;
using System.Security.Claims;

namespace SASMS.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class ApplicationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ApplicationController> _logger;
        private readonly INotificationService _notificationService;
        private readonly Microsoft.AspNetCore.SignalR.IHubContext<SASMS.Hubs.SASMSHub> _hubContext;
        private readonly IEmailService _emailService;

        public ApplicationController(ApplicationDbContext context, ILogger<ApplicationController> logger, Microsoft.AspNetCore.SignalR.IHubContext<SASMS.Hubs.SASMSHub> hubContext, INotificationService notificationService, IEmailService emailService)
        {
            _context = context;
            _logger = logger;
            _hubContext = hubContext;
            _notificationService = notificationService;
            _emailService = emailService;
        }

        public IActionResult Index() => RedirectToAction("List");

        // GET: /Application/List
        public async Task<IActionResult> List(string status = "")
        {
            var applicationsQuery = _context.Applications
                .Include(a => a.Applicant)
                    .ThenInclude(ap => ap.User)
                .Include(a => a.PreferredDepartment)
                .Include(a => a.ReviewedBy)
                .AsQueryable();

            // Apply status filter
            if (!string.IsNullOrWhiteSpace(status))
            {
                applicationsQuery = applicationsQuery.Where(a => a.Status == status);
            }

            var applications = await applicationsQuery
                .Select(a => new ApplicationViewModel
                {
                    Id = a.Id,
                    ApplicationNumber = a.ApplicationNumber,
                    ApplicantName = a.Applicant.User.Name,
                    ApplicantEmail = a.Applicant.User.Email,
                    Grade = a.Applicant.Grade,
                    PreferredDepartment = a.PreferredDepartment.Name,
                    ParentName = a.Applicant.ParentName,
                    ParentPhone = a.Applicant.ParentPhone,
                    ApplicationDate = a.ApplicationDate,
                    Status = a.Status,
                    TotalScore = a.TotalScore,
                    IsEligible = a.IsEligible,
                    ReviewDate = a.UpdatedAt,
                    ReviewedByName = a.ReviewedBy != null ? a.ReviewedBy.Name : null
                })
                .OrderByDescending(a => a.ApplicationDate)
                .ToListAsync();

            ViewBag.StatusFilter = status;
            ViewBag.PendingCount = await _context.Applications.CountAsync(a => a.Status == "Pending");
            ViewBag.ApprovedCount = await _context.Applications.CountAsync(a => a.Status == "Approved");
            ViewBag.RejectedCount = await _context.Applications.CountAsync(a => a.Status == "Rejected");

            return View(applications);
        }

        // GET: /Application/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var application = await _context.Applications
                .Include(a => a.Applicant)
                    .ThenInclude(ap => ap.User)
                .Include(a => a.PreferredDepartment)
                .Include(a => a.ReviewedBy)
                .Include(a => a.DynamicFieldValues)
                    .ThenInclude(dfv => dfv.Field)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (application == null)
            {
                return NotFound();
            }

            return View(application);
        }


        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, string status, string notes)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var application = await _context.Applications
                    .Include(a => a.Applicant)
                        .ThenInclude(ap => ap.User)
                    .FirstOrDefaultAsync(a => a.Id == id);
                
                if (application == null)
                {
                    return Json(new { success = false, message = "Application not found" });
                }

                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                
                application.Status = status;
                application.Notes = notes;
                application.ReviewedById = userId;
                application.UpdatedAt = DateTime.UtcNow;

                Notification notification = null;

                switch (status)
                {
                    case "AcceptedForExam": // Stage 1 Passed -> Invite to Internal Exam
                        notification = new Notification
                        {
                            UserId = application.Applicant.UserId,
                            Title = "Application Update: Initial Screening Passed",
                            Message = "Congratulations! You have passed the initial screening. You are invited to take the Internal School Exam (Math & English). Please check your email for the schedule.",
                            Type = "Info",
                            Category = "Admission",
                            Priority = "Normal",
                            ActionUrl = "/Applicant/Status",
                            CreatedAt = DateTime.UtcNow
                        };
                        break;

                    case "PassedInternalExam": // Stage 2 Passed -> Invite to Ministry Exam
                        notification = new Notification
                        {
                            UserId = application.Applicant.UserId,
                            Title = "Internal Exam Passed",
                            Message = "Great job! You have passed the Internal Exam. You are now eligible for the Ministry Exam. Please prepare accordingly.",
                            Type = "Success",
                            Category = "Admission",
                            Priority = "High",
                            ActionUrl = "/Applicant/Status",
                            CreatedAt = DateTime.UtcNow
                        };
                        break;

                    case "PassedMinistryExam": // Stage 3 Passed -> Invite to Interview
                        notification = new Notification
                        {
                            UserId = application.Applicant.UserId,
                            Title = "Ministry Exam Passed - Interview Invitation",
                            Message = "Congratulations on passing the Ministry Exam! We would like to invite you for a personal Interview. Our team will contact you shortly.",
                            Type = "Success",
                            Category = "Admission",
                            Priority = "High",
                            ActionUrl = "/Applicant/Status",
                            CreatedAt = DateTime.UtcNow
                        };
                        break;

                    case "Approved": // Final Stage -> Become Student
                        var existingStudent = await _context.Students.FirstOrDefaultAsync(s => s.UserId == application.Applicant.UserId);
                        if (existingStudent == null)
                        {
                            // 1. Update User Role
                            var applicantUser = await _context.Users.FindAsync(application.Applicant.UserId);
                            if (applicantUser != null) 
                            {
                                applicantUser.Role = UserRole.Student;
                            }

                            // 2. Generate Student ID (ST-YYYY-XXXX)
                            var yearStr = DateTime.UtcNow.Year.ToString();
                            var existingIds = await _context.Students
                                .Where(s => s.StudentId.StartsWith($"ST-{yearStr}-"))
                                .Select(s => s.StudentId)
                                .ToListAsync();
                            
                            int nextNumber = 1;
                            if (existingIds.Any())
                            {
                                nextNumber = existingIds
                                    .Select(sid => {
                                        var parts = sid.Split('-');
                                        return parts.Length == 3 && int.TryParse(parts[2], out int n) ? n : 0;
                                    })
                                    .Max() + 1;
                            }
                            var studentId = $"ST-{yearStr}-{nextNumber:D4}";

                            // 3. Determine Class (Automated balancing)
                            string assignedClass = null;
                            if (application.PreferredDepartmentId == 1) // Maintenance
                            {
                                var j1Count = await _context.Students.CountAsync(s => s.ClassName == "J1");
                                var j2Count = await _context.Students.CountAsync(s => s.ClassName == "J2");
                                assignedClass = j1Count <= j2Count ? "J1" : "J2";
                            }
                            else if (application.PreferredDepartmentId == 2) // Software
                            {
                                var j3Count = await _context.Students.CountAsync(s => s.ClassName == "J3");
                                var j4Count = await _context.Students.CountAsync(s => s.ClassName == "J4");
                                assignedClass = j3Count <= j4Count ? "J3" : "J4";
                            }

                            // 4. Create Student
                            var student = new Student
                            {
                                UserId = application.Applicant.UserId,
                                StudentId = studentId,
                                DateOfBirth = application.Applicant.DateOfBirth,
                                Gender = application.Applicant.Gender,
                                Address = application.Applicant.Address,
                                DepartmentId = application.PreferredDepartmentId,
                                ClassName = assignedClass,
                                EnrollmentDate = DateTime.UtcNow,
                                Status = "Active",
                                ParentName = application.Applicant.ParentName,
                                ParentPhone = application.Applicant.ParentPhone,
                                Kinship = application.Applicant.Kinship,
                                EmergencyContact = application.Applicant.ParentName,
                                EmergencyPhone = application.Applicant.ParentPhone,
                                ProfilePicturePath = applicantUser?.ProfilePicturePath
                            };
                            _context.Students.Add(student);

                            notification = new Notification
                            {
                                UserId = application.Applicant.UserId,
                                Title = "Congratulations! Admission Accepted",
                                Message = $"Congratulations! You have been officially accepted as a student at SASMS. Your Student ID is {studentId}.",
                                Type = "Success",
                                Category = "Admission",
                                Priority = "High",
                                ActionUrl = "/Student/Dashboard",
                                CreatedAt = DateTime.UtcNow
                            };

                            // Send Acceptance Email
                            await _emailService.SendAdmissionStatusEmailAsync(
                                application.Applicant.User.Email, 
                                application.Applicant.User.Name, 
                                true, 
                                studentId, 
                                assignedClass);
                        }
                        break;

                    case "Rejected":
                        notification = new Notification
                        {
                            UserId = application.Applicant.UserId,
                            Title = "Application Status Update",
                            Message = "We regret to inform you that we cannot proceed with your application at this stage.",
                            Type = "Warning",
                            Category = "Admission",
                            Priority = "Normal",
                            ActionUrl = "/Applicant/Status",
                            CreatedAt = DateTime.UtcNow
                        };

                        // Send Rejection Email
                        await _emailService.SendAdmissionStatusEmailAsync(
                            application.Applicant.User.Email, 
                            application.Applicant.User.Name, 
                            false);
                        break;
                }

                if (notification != null)
                {
                    await _notificationService.CreateNotificationAsync(notification);
                }
                else
                {
                    await _context.SaveChangesAsync();
                }
                
                // Broadcast real-time update
                await _hubContext.Clients.All.SendAsync("ReceiveDataUpdate", "Application", status);
                
                await transaction.CommitAsync();

                return Json(new { success = true, message = $"Application status updated to {status} successfully" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error updating application status");
                string errorMsg = ex.InnerException?.Message ?? ex.Message;
                return Json(new { success = false, message = $"Error: {errorMsg}" });
            }
        }

        // POST: /Application/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var application = await _context.Applications.FindAsync(id);
            if (application != null)
            {
                _context.Applications.Remove(application);
                await _context.SaveChangesAsync();

                // Broadcast real-time update
                await _hubContext.Clients.All.SendAsync("ReceiveDataUpdate", "Application", "Deleted");

                TempData["Success"] = "Application deleted successfully.";
            }
            else
            {
                TempData["Error"] = "Application not found.";
            }

            return RedirectToAction(nameof(List));
        }

        // POST: /Application/BulkDelete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkDelete(List<int> ids)
        {
            if (ids == null || !ids.Any())
            {
                TempData["Error"] = "No applications selected.";
                return RedirectToAction(nameof(List));
            }

            var applications = await _context.Applications
                .Where(a => ids.Contains(a.Id))
                .ToListAsync();

            if (applications.Any())
            {
                _context.Applications.RemoveRange(applications);
                await _context.SaveChangesAsync();

                // Broadcast real-time update
                await _hubContext.Clients.All.SendAsync("ReceiveDataUpdate", "Application", "Bulk Deleted");

                TempData["Success"] = $"{applications.Count} applications deleted successfully.";
            }

            return RedirectToAction(nameof(List));
        }
    }
}
