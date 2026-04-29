using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SASMS.Data;
using SASMS.Models;
using SASMS.ViewModels;
using SASMS.Services;
using System.Security.Claims;

namespace SASMS.Controllers
{
    [Authorize(Policy = "Authenticated")]
    public class ActivityController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ActivityController> _logger;
        private readonly IAuthService _authService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly INotificationService _notificationService;
        private readonly IActivityLogService _activityLogService;
        private readonly Microsoft.AspNetCore.SignalR.IHubContext<SASMS.Hubs.SASMSHub> _hubContext;

        public ActivityController(ApplicationDbContext context, ILogger<ActivityController> logger, IAuthService authService, IWebHostEnvironment webHostEnvironment, Microsoft.AspNetCore.SignalR.IHubContext<SASMS.Hubs.SASMSHub> hubContext, INotificationService notificationService, IActivityLogService activityLogService)
        {
            _context = context;
            _logger = logger;
            _authService = authService;
            _webHostEnvironment = webHostEnvironment;
            _hubContext = hubContext;
            _notificationService = notificationService;
            _activityLogService = activityLogService;
        }

        public IActionResult Index() => RedirectToAction("List");

        // GET: /Activity/List
        public async Task<IActionResult> List(string status = "")
        {
            var activitiesQuery = _context.Activities
                .Include(a => a.ManagedBy)
                .Include(a => a.Participations)
                .AsQueryable();

            // Filter out completed activities for students
            var userType = User.FindFirstValue("UserType");
            if (userType == "Student")
            {
                activitiesQuery = activitiesQuery.Where(a => a.Status != "Completed");
            }

            // Apply status filter
            if (!string.IsNullOrWhiteSpace(status))
            {
                activitiesQuery = activitiesQuery.Where(a => a.Status == status);
            }

            var activities = await activitiesQuery
                .Select(a => new ActivityViewModel
                {
                    Id = a.Id,
                    Title = a.Title,
                    Description = a.Description,
                    Category = a.Category,
                    StartDate = a.StartDate,
                    EndDate = a.EndDate,
                    Location = a.Location,
                    Capacity = a.Capacity,
                    CurrentParticipants = a.Participations.Count,
                    Status = a.Status,
                    RequiresRegistration = a.RequiresRegistration,
                    RegistrationDeadline = a.RegistrationDeadline,
                    ManagedByName = a.ManagedBy != null ? a.ManagedBy.Name : "N/A",
                    ManagedByProfilePicture = a.ManagedBy != null ? a.ManagedBy.ProfilePicturePath : null,
                    Fee = a.Fee,
                    ImagePath = a.ImagePath,
                    GradientColors = GetGradientForCategory(a.Category),
                    CreatedById = a.CreatedById
                })
                .OrderBy(a => a.StartDate)
                .ToListAsync();

            ViewBag.StatusFilter = status;
            ViewBag.UpcomingCount = await _context.Activities.CountAsync(a => a.Status == "Upcoming");
            ViewBag.OngoingCount = await _context.Activities.CountAsync(a => a.Status == "Ongoing");

            return View(activities);
        }

        // GET: /Activity/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var activity = await _context.Activities
                .Include(a => a.ManagedBy)
                .Include(a => a.Participations)
                .ThenInclude(p => p.Student)
                .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (activity == null)
            {
                return NotFound();
            }

            // Check if current user is a student and if they are registered or rejected
            var userType = User.FindFirstValue("UserType");
            if (userType == "Student")
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(userIdStr, out int userId))
                {
                    var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
                    if (student != null)
                    {
                        var participation = activity.Participations.FirstOrDefault(p => p.StudentId == student.Id);
                        ViewBag.IsRegistered = participation != null;
                        ViewBag.IsRejected = participation?.IsApproved == false;
                        ViewBag.ParticipationId = participation?.Id;
                    }
                }
            }

            return View(activity);
        }

        // GET: /Activity/Create
        [Authorize(Policy = "ActivityTeacherOnly")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Policy = "ActivityTeacherOnly")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Activity activity, IFormFile? image)
        {
            var currentUser = _authService.GetCurrentUser(HttpContext);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Set default values before validation
            activity.ManagedById = currentUser.Id; // Historical compatibility
            activity.CreatedById = currentUser.Id; // New ownership tracking
            activity.CreatedAt = DateTime.UtcNow;
            activity.Status = "Upcoming";
            
            // Handle Image Upload
            if (image != null && image.Length > 0)
            {
                try
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "activities");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + image.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await image.CopyToAsync(fileStream);
                    }
                    
                    activity.ImagePath = "/uploads/activities/" + uniqueFileName;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error uploading activity image");
                }
            }

            // Remove properties that should not be validated from the request form
            ModelState.Remove(nameof(activity.ManagedBy));
            ModelState.Remove(nameof(activity.Participations));
            ModelState.Remove(nameof(activity.Status));
            ModelState.Remove(nameof(activity.ManagedById));
            ModelState.Remove(nameof(image));

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Activities.Add(activity);
                    await _activityLogService.LogActivityAsync("Activity Created", "Activity", activity.Id.ToString(), $"Activity '{activity.Title}' was created.");
                    await _context.SaveChangesAsync();

                    // Notify Supervisors and Students (if enabled)
                    var usersToNotify = await _context.Users
                        .Where(u => u.IsActive && u.NotifyOnNewActivity && (u.Role == UserRole.Supervisor || u.Role == UserRole.Student))
                        .ToListAsync();

                    foreach (var user in usersToNotify)
                    {
                        await _notificationService.NotifyUserAsync(
                            user.Id,
                            "New Activity Added",
                            $"A new activity '{activity.Title}' has been added.",
                            "Activity",
                            $"/Activity/Details/{activity.Id}"
                        );
                    }


                    // Broadcast real-time update
                    await _hubContext.Clients.All.SendAsync("ReceiveDataUpdate", "Activity", "Created");

                    TempData["Success"] = "Activity created successfully!";
                    return RedirectToAction(nameof(List));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating activity");
                    ModelState.AddModelError("", "An error occurred while saving to the database.");
                }
            }

            // Log validation errors for debugging
            foreach (var modelStateKey in ModelState.Keys)
            {
                var modelStateVal = ModelState[modelStateKey];
                foreach (var error in modelStateVal.Errors)
                {
                    _logger.LogWarning("Validation Error - Key: {Key}, Error: {ErrorMessage}", modelStateKey, error.ErrorMessage);
                }
            }

            TempData["Error"] = "Please fix the errors below.";
            return View(activity);
        }

        // GET: /Activity/Edit/5
        [Authorize(Policy = "ActivityTeacherOnly")]
        public async Task<IActionResult> Edit(int id)
        {
            var activity = await _context.Activities.FindAsync(id);
            if (activity == null)
            {
                return NotFound();
            }

            var currentUser = _authService.GetCurrentUser(HttpContext);
            // Admin, StudentAffairs, and Supervisor can edit any; ActivityTeacher can only edit their own
            if (currentUser == null || (currentUser.Role != UserRole.Admin && currentUser.Role != UserRole.StudentAffairs && currentUser.Role != UserRole.Supervisor && activity.CreatedById != currentUser.Id))
            {
                return Forbid();
            }

            return View(activity);
        }

        [HttpPost]
        [Authorize(Policy = "ActivityTeacherOnly")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Activity activity, IFormFile? image)
        {
            if (id != activity.Id)
            {
                return NotFound();
            }

            var existingActivity = await _context.Activities.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
            if (existingActivity == null)
            {
                return NotFound();
            }

            // Handle Image Update
            if (image != null && image.Length > 0)
            {
                try
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "activities");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + image.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await image.CopyToAsync(fileStream);
                    }

                    activity.ImagePath = "/uploads/activities/" + uniqueFileName;

                    // Delete old image if it exists
                    if (!string.IsNullOrEmpty(existingActivity.ImagePath))
                    {
                        string oldPath = Path.Combine(_webHostEnvironment.WebRootPath, existingActivity.ImagePath.TrimStart('/'));
                        if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating activity image");
                }
            }
            else
            {
                // Keep existing image if no new one is uploaded
                activity.ImagePath = existingActivity.ImagePath;
            }

            // Set required properties that are not in the form
            activity.ManagedById = existingActivity.ManagedById;
            activity.CreatedById = existingActivity.CreatedById;
            activity.CreatedAt = existingActivity.CreatedAt;
            activity.UpdatedAt = DateTime.UtcNow;
            if (string.IsNullOrEmpty(activity.Status)) activity.Status = existingActivity.Status;

            // Remove properties from validation
            ModelState.Remove(nameof(activity.ManagedBy));
            ModelState.Remove(nameof(activity.Participations));
            ModelState.Remove(nameof(image));

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(activity);
                    await _activityLogService.LogActivityAsync("Activity Updated", "Activity", activity.Id.ToString(), $"Activity '{activity.Title}' was updated.");
                    await _context.SaveChangesAsync();

                    // Broadcast real-time update
                    await _hubContext.Clients.All.SendAsync("ReceiveDataUpdate", "Activity", "Updated");

                    TempData["Success"] = "Activity updated successfully!";
                    return RedirectToAction(nameof(List));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Activities.Any(e => e.Id == activity.Id))
                    {
                        return NotFound();
                    }
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating activity");
                    ModelState.AddModelError("", "An error occurred while saving the changes.");
                }
            }

            return View(activity);
        }

        [HttpPost]
        [Authorize(Policy = "ActivityTeacherOnly")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var activity = await _context.Activities.FindAsync(id);
            if (activity == null)
            {
                return NotFound();
            }

            var currentUser = _authService.GetCurrentUser(HttpContext);
            if (currentUser == null || (currentUser.Role != UserRole.Admin && currentUser.Role != UserRole.StudentAffairs && currentUser.Role != UserRole.Supervisor && activity.CreatedById != currentUser.Id))
            {
                return Forbid();
            }

            try
            {
                // Delete image if exists
                if (!string.IsNullOrEmpty(activity.ImagePath))
                {
                    string imagePath = Path.Combine(_webHostEnvironment.WebRootPath, activity.ImagePath.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath)) System.IO.File.Delete(imagePath);
                }

                _context.Activities.Remove(activity);
                await _activityLogService.LogActivityAsync("Activity Deleted", "Activity", id.ToString(), $"Activity '{activity.Title}' was deleted.");
                await _context.SaveChangesAsync();

                // Broadcast real-time update
                await _hubContext.Clients.All.SendAsync("ReceiveDataUpdate", "Activity", "Deleted");

                TempData["Success"] = "Activity deleted successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting activity");
                TempData["Error"] = "An error occurred while deleting the activity.";
            }

            return RedirectToAction(nameof(List));
        }

        [HttpPost]
        [Authorize(Policy = "StudentOnly")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(int id)
        {
            var activity = await _context.Activities
                .Include(a => a.Participations)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (activity == null) return NotFound();

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);

            if (student == null) return Forbid();

            // Validation logic
            if (activity.Status != "Upcoming")
            {
                TempData["Error"] = "Registration is only available for upcoming activities.";
                return RedirectToAction("Details", new { id });
            }

            if (activity.RequiresRegistration && activity.RegistrationDeadline.HasValue && activity.RegistrationDeadline < DateTime.UtcNow)
            {
                TempData["Error"] = "Registration deadline has passed.";
                return RedirectToAction("Details", new { id });
            }

            if (!activity.IsUnlimitedCapacity && activity.Capacity.HasValue && activity.CurrentParticipants >= activity.Capacity.Value)
            {
                TempData["Error"] = "Activity is already full.";
                return RedirectToAction("Details", new { id });
            }

            var existingParticipation = activity.Participations.FirstOrDefault(p => p.StudentId == student.Id);
            if (existingParticipation != null)
            {
                if (existingParticipation.IsApproved == false)
                {
                    TempData["Error"] = "Your registration for this activity has been rejected.";
                    return RedirectToAction("Details", new { id });
                }
                TempData["Error"] = "You are already registered for this activity.";
                return RedirectToAction("Details", new { id });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var participation = new ActivityParticipation
                {
                    ActivityId = activity.Id,
                    StudentId = student.Id,
                    RegistrationDate = DateTime.UtcNow,
                    Status = "Registered",
                    CreatedAt = DateTime.UtcNow
                };

                _context.ActivityParticipations.Add(participation);
                activity.CurrentParticipants++;
                
                await _activityLogService.LogActivityAsync(student.UserId, "Activity Registration", "Activity", activity.Id.ToString(), $"Student '{student.User.Name}' registered for activity '{activity.Title}'.");
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Notify Admin, Supervisor, and Activity Teacher (if enabled)
                // Replaced manual notification logic with INotificationService call
                await _notificationService.SendActivityRegistrationNotification(student.Id, activity.Id);

                // Broadcast real-time update
                await _hubContext.Clients.All.SendAsync("ReceiveDataUpdate", "Activity", "Registration");


                TempData["Success"] = "You have successfully registered for " + activity.Title;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error registering student for activity");
                TempData["Error"] = "An error occurred during registration. Please try again.";
            }

            return RedirectToAction("Details", new { id });
        }

        // GET: /Activity/Participants/5
        [Authorize(Policy = "ActivityTeacherOnly")]
        public async Task<IActionResult> Participants(int id)
        {
            var activity = await _context.Activities
                .Include(a => a.Participations)
                .ThenInclude(p => p.Student)
                .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (activity == null)
            {
                return NotFound();
            }

            return View(activity);
        }

        // POST: /Activity/RejectParticipation
        [HttpPost]
        [Authorize(Policy = "SupervisorOnly")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectParticipation(int id, int activityId)
        {
            var participation = await _context.ActivityParticipations
                .Include(p => p.Student)
                .ThenInclude(s => s.User)
                .Include(p => p.Activity)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (participation == null)
            {
                return NotFound();
            }

            try
            {
                participation.IsApproved = false;
                participation.Status = "Rejected";
                participation.UpdatedAt = DateTime.UtcNow;

                // Notify student
                await _notificationService.NotifyUserAsync(
                    participation.Student.UserId,
                    "Activity Registration Rejected",
                    $"Your registration for '{participation.Activity.Title}' has been rejected by the admin.",
                    "Activity",
                    "/StudentPortal/Activities"
                );
                
                await _activityLogService.LogActivityAsync("Participant Rejected", "Activity", activityId.ToString(), $"Participant '{participation.Student.User.Name}' was rejected for activity '{participation.Activity.Title}'.");
                await _context.SaveChangesAsync();

                // Broadcast real-time update
                await _hubContext.Clients.All.SendAsync("ReceiveDataUpdate", "Activity", "Rejected");

                TempData["Success"] = "Participant rejected successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting participation");
                TempData["Error"] = "An error occurred while rejecting the participant.";
            }

            return RedirectToAction(nameof(Participants), new { id = activityId });
        }

        // POST: /Activity/ApproveParticipation
        [HttpPost]
        [Authorize(Policy = "SupervisorOnly")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveParticipation(int id, int activityId)
        {
            var participation = await _context.ActivityParticipations
                .Include(p => p.Student) // Include Student to get UserId for notification
                .Include(p => p.Activity) // Include Activity to get Title for notification
                .FirstOrDefaultAsync(p => p.Id == id);

            if (participation == null)
            {
                return NotFound();
            }

            try
            {
                participation.IsApproved = true;
                participation.Status = "Confirmed";
                participation.UpdatedAt = DateTime.UtcNow;
 
                // Notify student
                if (participation.Student != null)
                {
                    await _notificationService.NotifyUserAsync(
                        participation.Student.UserId,
                        "Activity Registration Approved",
                        $"Your registration for '{participation.Activity.Title}' has been approved.",
                        "Activity",
                        "/StudentPortal/Activities"
                    );
                }
 
                await _activityLogService.LogActivityAsync("Participant Approved", "Activity", activityId.ToString(), $"Participant '{participation.Student.User.Name}' was approved for activity '{participation.Activity.Title}'.");
                await _context.SaveChangesAsync();

                // Broadcast real-time update
                await _hubContext.Clients.All.SendAsync("ReceiveDataUpdate", "Activity", "Approved");

                TempData["Success"] = "Participant approved successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving participation");
                TempData["Error"] = "An error occurred while approving the participant.";
            }

            return RedirectToAction(nameof(Participants), new { id = activityId });
        }

        // POST: /Activity/CancelRegistration
        [HttpPost]
        [Authorize(Policy = "StudentOnly")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelRegistration(int id, int activityId)
        {
            var participation = await _context.ActivityParticipations
                .Include(p => p.Activity)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (participation == null)
            {
                return NotFound();
            }

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);

            if (student == null || participation.StudentId != student.Id)
            {
                return Forbid();
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                participation.Status = "Cancelled";
                participation.UpdatedAt = DateTime.UtcNow;

                // Decrease participant count
                var activity = participation.Activity;
                if (activity.CurrentParticipants > 0)
                {
                    activity.CurrentParticipants--;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Broadcast real-time update
                await _hubContext.Clients.All.SendAsync("ReceiveDataUpdate", "Activity", "Registration Cancelled");

                TempData["Success"] = "Registration cancelled successfully.";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error cancelling registration");
                TempData["Error"] = "An error occurred while cancelling your registration.";
            }

            return RedirectToAction("Details", new { id = activityId });
        }

        // POST: /Activity/ApproveAll
        [HttpPost]
        [Authorize(Policy = "SupervisorOnly")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveAll(int activityId)
        {
            var participations = await _context.ActivityParticipations
                .Where(p => p.ActivityId == activityId && (p.IsApproved == null || p.IsApproved == false))
                .ToListAsync();

            if (!participations.Any())
            {
                TempData["Info"] = "No pending participants to approve.";
                return RedirectToAction(nameof(Participants), new { id = activityId });
            }

            try
            {
                foreach (var participation in participations)
                {
                    participation.IsApproved = true;
                    participation.Status = "Confirmed";
                    participation.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = $"{participations.Count} participant(s) approved successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving all participants");
                TempData["Error"] = "An error occurred while approving participants.";
            }

            return RedirectToAction(nameof(Participants), new { id = activityId });
        }

        // POST: /Activity/RejectAll
        [HttpPost]
        [Authorize(Policy = "SupervisorOnly")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectAll(int activityId)
        {
            var participations = await _context.ActivityParticipations
                .Include(p => p.Student)
                .ThenInclude(s => s.User)
                .Include(p => p.Activity)
                .Where(p => p.ActivityId == activityId && (p.IsApproved == null || p.IsApproved == true))
                .ToListAsync();

            if (!participations.Any())
            {
                TempData["Info"] = "No participants to reject.";
                return RedirectToAction(nameof(Participants), new { id = activityId });
            }

            try
            {
                foreach (var participation in participations)
                {
                    participation.IsApproved = false;
                    participation.Status = "Rejected";
                    participation.UpdatedAt = DateTime.UtcNow;

                    // Notify each student
                    await _notificationService.NotifyUserAsync(
                        participation.Student.UserId,
                        "Activity Registration Rejected",
                        $"Your registration for '{participation.Activity.Title}' has been rejected by the admin.",
                        "Activity",
                        "/StudentPortal/Activities"
                    );
                }

                await _context.SaveChangesAsync();

                // Broadcast real-time update
                await _hubContext.Clients.All.SendAsync("ReceiveDataUpdate", "Activity", "RejectedBulk");

                TempData["Success"] = $"{participations.Count} participant(s) rejected successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting all participants");
                TempData["Error"] = "An error occurred while rejecting participants.";
            }

            return RedirectToAction(nameof(Participants), new { id = activityId });
        }


        private static string GetGradientForCategory(string category)
        {
            return category switch
            {
                "Sports" => "linear-gradient(135deg, #667eea 0%, #764ba2 100%)",
                "Arts" => "linear-gradient(135deg, #f093fb 0%, #f5576c 100%)",
                "Technology" => "linear-gradient(135deg, #4facfe 0%, #00f2fe 100%)",
                "Academic" => "linear-gradient(135deg, #43e97b 0%, #38f9d7 100%)",
                _ => "linear-gradient(135deg, #fa709a 0%, #fee140 100%)"
            };
        }
    }
}
