using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SASMS.Data;
using SASMS.Models;
using SASMS.Services;
using SASMS.ViewModels;
using System.Security.Claims;
using Microsoft.Extensions.Localization;

namespace SASMS.Controllers
{
    // [Authorize(Roles = "Admin")] // In .NET Core with our custom setup, we usually check claims manually or use policies
    // Using our custom attribute or Policy based on Program.cs setup
    [Authorize(Policy = "StaffOnly")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ILogger<AdminController> _logger;
        private readonly IAuthService _authService;
        private readonly IEmailService _emailService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IStringLocalizer<SharedResources> _localizer;
        private readonly INotificationService _notificationService;
        private readonly Microsoft.AspNetCore.SignalR.IHubContext<SASMS.Hubs.SASMSHub> _hubContext;
        private readonly IBackupService _backupService;
        private readonly IRiskDetectionService _riskDetectionService;

        public AdminController(ApplicationDbContext context, IPasswordHasher passwordHasher, ILogger<AdminController> logger, IAuthService authService, IEmailService emailService, IWebHostEnvironment webHostEnvironment, IStringLocalizer<SharedResources> localizer, Microsoft.AspNetCore.SignalR.IHubContext<SASMS.Hubs.SASMSHub> hubContext, INotificationService notificationService, IBackupService backupService, IRiskDetectionService riskDetectionService)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _logger = logger;
            _authService = authService;
            _emailService = emailService;
            _webHostEnvironment = webHostEnvironment;
            _localizer = localizer;
            _hubContext = hubContext;
            _notificationService = notificationService;
            _backupService = backupService;
            _riskDetectionService = riskDetectionService;
        }


        // GET: /Admin
        // GET: /Admin
        public async Task<IActionResult> Index()
        {
            var currentYear = await _context.AcademicYears.FirstOrDefaultAsync(ay => ay.IsActive);
            var today = DateTime.UtcNow.Date;
            var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);

            // 1. Fetch Monthly data for chart - Optimized to fetch all in 3 queries instead of 36
            var studentsByMonth = await _context.Students
                .Where(s => s.EnrollmentDate.Year == today.Year)
                .GroupBy(s => s.EnrollmentDate.Month)
                .Select(g => new { Month = g.Key, Count = g.Count() })
                .ToListAsync();

            var appsByMonth = await _context.Applications
                .Where(a => a.ApplicationDate.Year == today.Year)
                .GroupBy(a => a.ApplicationDate.Month)
                .Select(g => new { Month = g.Key, Count = g.Count() })
                .ToListAsync();

            var complaintsByMonth = await _context.Complaints
                .Where(c => c.ComplaintDate.Year == today.Year)
                .GroupBy(c => c.ComplaintDate.Month)
                .Select(g => new { Month = g.Key, Count = g.Count() })
                .ToListAsync();

            // 2. Gather Dashboard Statistics
            var roleClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            Enum.TryParse<UserRole>(roleClaim, out var userRole);
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

            var dashboardData = new SASMS.ViewModels.DashboardViewModel
            {
                MonthlyStudents = Enumerable.Range(1, 12).Select(month => new SASMS.ViewModels.MonthlyDataViewModel
                {
                    Month = new DateTime(today.Year, month, 1).ToString("MMM"),
                    Count = studentsByMonth.FirstOrDefault(x => x.Month == month)?.Count ?? 0
                }).ToList(),

                MonthlyApplications = Enumerable.Range(1, 12).Select(month => new SASMS.ViewModels.MonthlyDataViewModel
                {
                    Month = new DateTime(today.Year, month, 1).ToString("MMM"),
                    Count = appsByMonth.FirstOrDefault(x => x.Month == month)?.Count ?? 0
                }).ToList(),

                MonthlyComplaints = Enumerable.Range(1, 12).Select(month => new SASMS.ViewModels.MonthlyDataViewModel
                {
                    Month = new DateTime(today.Year, month, 1).ToString("MMM"),
                    Count = complaintsByMonth.FirstOrDefault(x => x.Month == month)?.Count ?? 0
                }).ToList(),

                RecentActivities = new List<SASMS.ViewModels.RecentActivityViewModel>()
            };

            bool isAdmin = userRole == UserRole.Admin || userRole == UserRole.StudentAffairs;
            bool isSupervisor = userRole == UserRole.Supervisor;
            bool isActivityTeacher = userRole == UserRole.ActivityTeacher;

            if (isAdmin)
            {
                var totalStudents = await _context.Students.CountAsync();
                var studentsByDept = await _context.Students
                    .GroupBy(s => (int?)s.DepartmentId)
                    .Select(g => new { DeptId = g.Key, Count = g.Count() })
                    .ToListAsync();

                var allActiveFees = await _context.Fees.Where(f => f.IsActive).ToListAsync();

                decimal totalAssignedFees = 0;
                foreach (var fee in allActiveFees)
                {
                    if (fee.DepartmentId == null)
                    {
                        totalAssignedFees += fee.Amount * totalStudents;
                    }
                    else
                    {
                        var studentCountInDept = studentsByDept.FirstOrDefault(d => d.DeptId == fee.DepartmentId)?.Count ?? 0;
                        totalAssignedFees += fee.Amount * studentCountInDept;
                    }
                }

                var totalFeesCollected = await _context.Payments
                    .Where(p => p.Status == "Completed")
                    .SumAsync(p => (decimal?)p.Amount) ?? 0;

                dashboardData.TotalStudents = totalStudents;
                dashboardData.ActiveStudents = await _context.Students.CountAsync(s => s.Status == "Active");
                dashboardData.PendingApplications = await _context.Applications.CountAsync(a => a.Status == "Pending");
                dashboardData.NewApplicationsThisMonth = await _context.Applications.CountAsync(a => a.ApplicationDate >= firstDayOfMonth);
                dashboardData.TotalFeesCollected = totalFeesCollected;
                dashboardData.OutstandingFees = totalAssignedFees - totalFeesCollected;
                dashboardData.PendingComplaints = await _context.Complaints.CountAsync(c => c.Status == "Pending");
                dashboardData.TotalAttendanceToday = await _context.Attendances.CountAsync(a => a.AttendanceDate.Date == today && a.Status == "Present");
                dashboardData.AbsentToday = await _context.Attendances.CountAsync(a => a.AttendanceDate.Date == today && a.Status == "Absent");
                dashboardData.LateToday = await _context.Attendances.CountAsync(a => a.AttendanceDate.Date == today && a.Status == "Late");
                dashboardData.TotalComplaintsCount = await _context.Complaints.CountAsync();
            }
            else if (userRole == UserRole.Supervisor)
            {
                dashboardData.TotalStudents = await _context.Students.CountAsync();
                dashboardData.TotalManagedStudentsCount = dashboardData.TotalStudents;
                dashboardData.PendingComplaints = await _context.Complaints.CountAsync(c => c.Status == "Pending");
                dashboardData.PendingSuggestionsCount = await _context.Suggestions.CountAsync(s => s.Status == "Pending");
                dashboardData.ActiveStudents = await _context.Students.CountAsync(s => s.Status == "Active");

                var suggestionsByMonth = await _context.Suggestions
                    .Where(s => s.CreatedAt.Year == today.Year)
                    .GroupBy(s => s.CreatedAt.Month)
                    .Select(g => new { Month = g.Key, Count = g.Count() })
                    .ToListAsync();

                dashboardData.MonthlySuggestions = Enumerable.Range(1, 12).Select(month => new SASMS.ViewModels.MonthlyDataViewModel
                {
                    Month = new DateTime(today.Year, month, 1).ToString("MMM"),
                    Count = suggestionsByMonth.FirstOrDefault(x => x.Month == month)?.Count ?? 0
                }).ToList();
            }
            else if (userRole == UserRole.ActivityTeacher)
            {
                var myManagedActivities = await _context.Activities.Where(a => a.ManagedById == userId).ToListAsync();
                var myManagedActivityIds = myManagedActivities.Select(a => a.Id).ToList();

                dashboardData.MyManagedActivitiesCount = myManagedActivities.Count;
                dashboardData.ActiveManagedActivities = myManagedActivities.Count(a => a.Status == "Ongoing" || a.Status == "Upcoming");
                dashboardData.TotalParticipantsInMyActivities = await _context.ActivityParticipations
                    .CountAsync(ap => myManagedActivityIds.Contains(ap.ActivityId));
                dashboardData.NewParticipantsThisMonth = await _context.ActivityParticipations
                    .CountAsync(ap => myManagedActivityIds.Contains(ap.ActivityId) && ap.RegistrationDate >= firstDayOfMonth);

                var myParticipantsByMonth = await _context.ActivityParticipations
                    .Where(ap => myManagedActivityIds.Contains(ap.ActivityId) && ap.RegistrationDate.Year == today.Year)
                    .GroupBy(ap => ap.RegistrationDate.Month)
                    .Select(g => new { Month = g.Key, Count = g.Count() })
                    .ToListAsync();

                dashboardData.MonthlyParticipants = Enumerable.Range(1, 12).Select(month => new SASMS.ViewModels.MonthlyDataViewModel
                {
                    Month = new DateTime(today.Year, month, 1).ToString("MMM"),
                    Count = myParticipantsByMonth.FirstOrDefault(x => x.Month == month)?.Count ?? 0
                }).ToList();
            }

            // Get current admin user ID
            var adminUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            // Fetch recent system notifications for this admin user only
            var recentNotifications = await _context.Notifications
                .Where(n => n.UserId == adminUserId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(7)
                .ToListAsync();

            foreach (var note in recentNotifications)
            {
                // Map category to meaningful activity type
                string activityType = note.Category switch
                {
                    "Complaint" => note.Type == "Alert" ? "New Complaint" : "Complaint Update",
                    "Application" => "Application",
                    "Payment" => "Payment",
                    "Attendance" => "Attendance",
                    "Activity" => "Activity",
                    "Message" => "Message",
                    _ => note.Type
                };

                string badgeColor = note.Type switch
                {
                    "Success" => "#E6F4EA",
                    "Error" or "Warning" => "#FCE8E6",
                    "Alert" => "#FFF4E5",
                    _ => "#E8F0FE"
                };

                string icon = note.Type switch
                {
                    "Success" => "fa-check-circle",
                    "Error" or "Warning" => "fa-exclamation-circle",
                    "Alert" => "fa-bell",
                    _ => "fa-info-circle"
                };

                dashboardData.RecentActivities.Add(new SASMS.ViewModels.RecentActivityViewModel
                {
                    Type = _localizer[activityType],
                    Description = SASMS.Helpers.LocalizationHelper.TranslateNotification(note.Message, _localizer),
                    Date = note.CreatedAt,
                    BadgeColor = badgeColor,
                    Icon = icon
                });
            }

            return View(dashboardData);
        }

        // GET: /Admin/ActivityLogs
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> ActivityLogs(string? searchTerm, string? actionFilter, DateTime? startDate, DateTime? endDate)
        {
            var query = _context.ActivityLogs
                .Include(l => l.User)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(l => l.UserName.Contains(searchTerm) || l.Details.Contains(searchTerm) || l.EntityId.Contains(searchTerm));
            }

            if (!string.IsNullOrEmpty(actionFilter))
            {
                query = query.Where(l => l.Action == actionFilter);
            }

            if (startDate.HasValue)
            {
                query = query.Where(l => l.Timestamp >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(l => l.Timestamp <= endDate.Value.AddDays(1));
            }

            var logs = await query
                .OrderByDescending(l => l.Timestamp)
                .Take(500) // Limit to last 500 for performance
                .ToListAsync();

            ViewBag.SearchTerm = searchTerm;
            ViewBag.ActionFilter = actionFilter;
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
            
            // Get unique actions for filter dropdown
            ViewBag.Actions = await _context.ActivityLogs
                .Select(l => l.Action)
                .Distinct()
                .ToListAsync();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_ActivityLogsPartial", logs);
            }

            return View(logs);
        }

        private string TranslateNotification(string message)
        {
            return SASMS.Helpers.LocalizationHelper.TranslateNotification(message, _localizer);
        }


        // GET: /Admin/StudentAffairsList
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> StudentAffairsList(string? searchTerm)
        {
            var query = _context.StudentAffairs
                .Include(sa => sa.User)
                .Where(sa => sa.User.IsActive); // Only show active staff

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(sa => sa.User.Name.Contains(searchTerm) || sa.User.Email.Contains(searchTerm));
                ViewData["SearchTerm"] = searchTerm;
            }

            var staff = await query.ToListAsync();

            return View(staff);
        }

        // GET: /Admin/CreateStudentAffairs
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> CreateStudentAffairs()
        {
            ViewBag.Roles = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(new List<object>
            {
                new { Value = UserRole.StudentAffairs, Text = _localizer["StudentAffairs"].Value },
                new { Value = UserRole.Supervisor, Text = _localizer["Supervisor"].Value },
                new { Value = UserRole.ActivityTeacher, Text = _localizer["ActivityTeacher"].Value }
            }, "Value", "Text");
            return View();
        }

        // POST: /Admin/CreateStudentAffairs
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> CreateStudentAffairs(string name, string email, string nationalId, string phone, string password, string position, DateTime hireDate, UserRole role)
        {
            if (ModelState.IsValid)
            {
                // Check if email or national ID exists
                if (await _context.Users.AnyAsync(u => u.Email == email))
                {
                    ModelState.AddModelError("Email", "Email already exists.");
                    return View();
                }
                if (await _context.Users.AnyAsync(u => u.NationalId == nationalId))
                {
                    ModelState.AddModelError("NationalId", "National ID already exists.");
                    return View();
                }

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Create User
                    var user = new User
                    {
                        Name = name,
                        Email = email,
                        NationalId = nationalId,
                        PhoneNumber = phone,
                        Password = _passwordHasher.HashPassword(password),
                        Role = role,
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true
                    };

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    // Create StudentAffairs Profile
                    var staff = new StudentAffairs
                    {
                        UserId = user.Id,
                        Position = position,
                        HireDate = hireDate
                    };

                    _context.StudentAffairs.Add(staff);
                    await _context.SaveChangesAsync();

                    // Broadcast real-time update
                    Console.WriteLine("[SignalR BROADCAST] Entity: Staff, Action: Created");
            await _hubContext.Clients.All.SendAsync("ReceiveDataUpdate", "Staff", "Created");

                    await transaction.CommitAsync();
                    TempData["Success"] = "Student Affairs staff member created successfully.";
                    return RedirectToAction(nameof(StudentAffairsList));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error creating Student Affairs user");
                    ModelState.AddModelError("", "An error occurred while creating the user.");
                }
            }

            ViewBag.Roles = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(new List<object>
            {
                new { Value = UserRole.StudentAffairs, Text = _localizer["StudentAffairs"].Value },
                new { Value = UserRole.Supervisor, Text = _localizer["Supervisor"].Value },
                new { Value = UserRole.ActivityTeacher, Text = _localizer["ActivityTeacher"].Value }
            }, "Value", "Text", role);
            return View();
        }

        // GET: /Admin/EditStudentAffairs/5
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> EditStudentAffairs(int id)
        {
            var staff = await _context.StudentAffairs
                .Include(sa => sa.User)
                .FirstOrDefaultAsync(sa => sa.Id == id);

            if (staff == null)
            {
                return NotFound();
            }

            ViewBag.Roles = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(new List<object>
            {
                new { Value = UserRole.StudentAffairs, Text = _localizer["StudentAffairs"].Value },
                new { Value = UserRole.Supervisor, Text = _localizer["Supervisor"].Value },
                new { Value = UserRole.ActivityTeacher, Text = _localizer["ActivityTeacher"].Value }
            }, "Value", "Text", staff.User.Role);
            return View(staff);
        }

        // POST: /Admin/EditStudentAffairs/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> EditStudentAffairs(int id, string name, string email, string phone, string position, bool isActive, UserRole role)
        {
            var staff = await _context.StudentAffairs
                .Include(sa => sa.User)
                .FirstOrDefaultAsync(sa => sa.Id == id);

            if (staff == null) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Update User Info
                    staff.User.Name = name;
                    staff.User.PhoneNumber = phone;
                    staff.User.IsActive = isActive;
                    staff.User.Role = role;
                    
                    // Only update email if changed and verify uniqueness
                    if (staff.User.Email != email)
                    {
                         if (await _context.Users.AnyAsync(u => u.Email == email))
                        {
                            ModelState.AddModelError("Email", "Email already exists.");
                            return View(staff);
                        }
                        staff.User.Email = email;
                    }

                    // Update Staff Info
                    staff.Position = position;

                    await _context.SaveChangesAsync();

                    // Broadcast real-time update
                    Console.WriteLine("[SignalR BROADCAST] Entity: Staff, Action: Updated");
            await _hubContext.Clients.All.SendAsync("ReceiveDataUpdate", "Staff", "Updated");

                    TempData["Success"] = "Staff member updated successfully.";
                    return RedirectToAction(nameof(StudentAffairsList));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating Student Affairs user");
                    ModelState.AddModelError("", "An error occurred while updating.");
                }
            }

            ViewBag.Roles = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(new List<object>
            {
                new { Value = UserRole.StudentAffairs, Text = _localizer["StudentAffairs"].Value },
                new { Value = UserRole.Supervisor, Text = _localizer["Supervisor"].Value },
                new { Value = UserRole.ActivityTeacher, Text = _localizer["ActivityTeacher"].Value }
            }, "Value", "Text", role);
            return View(staff);
        }

        // POST: /Admin/DeleteStudentAffairs/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(int id)
        {
             var staff = await _context.StudentAffairs
                .Include(sa => sa.User)
                .FirstOrDefaultAsync(sa => sa.Id == id);

            if (staff != null)
            {
                // Delete related messages manually (Received messages have NoAction)
                var receivedMessages = await _context.Messages.Where(m => m.ReceiverId == staff.UserId).ToListAsync();
                if (receivedMessages.Any())
                {
                    _context.Messages.RemoveRange(receivedMessages);
                }

                // Permanent Delete (Hard Delete)
                _context.Users.Remove(staff.User); // This will cascade delete the StudentAffairs record
                await _context.SaveChangesAsync();

                // Broadcast real-time update
                Console.WriteLine("[SignalR BROADCAST] Entity: Staff, Action: Deleted");
            await _hubContext.Clients.All.SendAsync("ReceiveDataUpdate", "Staff", "Deleted");

                TempData["Success"] = "Staff member permanently deleted.";
            }
            else 
            {
                TempData["Error"] = "Staff member not found.";
            }

            return RedirectToAction(nameof(StudentAffairsList));
        }

        // POST: /Admin/BulkDeleteStudentAffairs
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> BulkDeleteStudentAffairs(List<int> ids)
        {
            if (ids == null || !ids.Any())
            {
                TempData["Error"] = "No items selected.";
                return RedirectToAction(nameof(StudentAffairsList));
            }

            var staffMembers = await _context.StudentAffairs
                .Include(sa => sa.User)
                .Where(sa => ids.Contains(sa.Id))
                .ToListAsync();

            if (staffMembers.Any())
            {
                var userIds = staffMembers.Select(s => s.UserId).ToList();

                // Delete related messages manually (Received messages have NoAction)
                var receivedMessages = await _context.Messages.Where(m => userIds.Contains(m.ReceiverId ?? 0)).ToListAsync();
                if (receivedMessages.Any())
                {
                    _context.Messages.RemoveRange(receivedMessages);
                }

                // Delete the Users (Cascade will delete Staff profiles)
                var usersToDelete = staffMembers.Select(s => s.User).ToList();
                _context.Users.RemoveRange(usersToDelete);
                
                await _context.SaveChangesAsync();

                // Broadcast real-time update
                await _hubContext.Clients.All.SendAsync("ReceiveDataUpdate", "Staff", "Bulk Deleted");

                TempData["Success"] = $"{staffMembers.Count} staff members permanently deleted.";
            }

            return RedirectToAction(nameof(StudentAffairsList));
        }
        // GET: /Admin/Admissions - Redirect to Application Controller
        public IActionResult Admissions()
        {
            return RedirectToAction("List", "Application");
        }

        // GET: /Admin/Students - Redirect to Student Controller
        public IActionResult Students()
        {
            return RedirectToAction("List", "Student");
        }

        // GET: /Admin/Attendance - Redirect to Attendance Controller
        public IActionResult Attendance()
        {
            return RedirectToAction("List", "Attendance");
        }

        // GET: /Admin/Fees - Redirect to Fees Controller
        public IActionResult Fees()
        {
            return RedirectToAction("List", "Fees");
        }

        // GET: /Admin/Complaints - Redirect to Complaint Controller
        public IActionResult Complaints()
        {
            return RedirectToAction("List", "Complaint");
        }

        // GET: /Admin/Activities - Redirect to Activity Controller
        public IActionResult Activities()
        {
            return RedirectToAction("List", "Activity");
        }

        [HttpPost]
        public IActionResult SelectChat(int userId)
        {
            HttpContext.Session.SetInt32("SelectedChatUserId", userId);
            return RedirectToAction("Messaging");
        }

        // GET: /Admin/Messaging
        public async Task<IActionResult> Messaging(int? userId)
        {
            // If userId is passed in URL, save to session and redirect
            if (userId.HasValue)
            {
                HttpContext.Session.SetInt32("SelectedChatUserId", userId.Value);
                return RedirectToAction("Messaging");
            }

            // Try to get from session
            userId = HttpContext.Session.GetInt32("SelectedChatUserId");
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            // SECURITY: Prevent chatting with self
            if (userId.HasValue && userId.Value == currentUserId)
            {
                HttpContext.Session.Remove("SelectedChatUserId");
                return RedirectToAction("Messaging");
            }

            // Update LastSeen for the current user
            var currentUser = await _context.Users.FindAsync(currentUserId);
            if (currentUser != null)
            {
                currentUser.LastSeen = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            // 1. Get List of Users to Chat With (Staff, Admins, & Students)
            // Exclude current user
            var users = await _context.Users
                .Where(u => u.Id != currentUserId && (u.Role == UserRole.StudentAffairs || u.Role == UserRole.Admin || u.Role == UserRole.Student))
                .Select(u => new UserViewModel
                {
                    Id = u.Id,
                    Name = u.Name,
                    ProfilePicturePath = u.ProfilePicturePath,
                    Role = u.Role.ToString(),
                    // Get last message info
                    LastMessage = _context.Messages
                        .Where(m => (m.SenderId == currentUserId && m.ReceiverId == u.Id) || (m.SenderId == u.Id && m.ReceiverId == currentUserId))
                        .OrderByDescending(m => m.SentAt)
                        .Select(m => m.Content)
                        .FirstOrDefault(),
                    LastMessageDate = _context.Messages
                        .Where(m => (m.SenderId == currentUserId && m.ReceiverId == u.Id) || (m.SenderId == u.Id && m.ReceiverId == currentUserId))
                        .OrderByDescending(m => m.SentAt)
                        .Select(m => m.SentAt)
                        .FirstOrDefault(),
                     UnreadCount = _context.Messages
                        .Count(m => m.SenderId == u.Id && m.ReceiverId == currentUserId && !m.IsRead)
                })
                .OrderByDescending(u => u.LastMessageDate)
                .ToListAsync();


            if (!userId.HasValue && users.Any())
            {
                userId = users.First().Id;
            }

            ViewBag.SelectedUserId = userId;
            ViewBag.CurrentUserId = currentUserId;

            // 3. Get Messages for Selected User
            var messages = new List<Message>();
            if (userId.HasValue)
            {
                messages = await _context.Messages
                    .Include(m => m.Attachments)
                    .Where(m => (m.SenderId == currentUserId && m.ReceiverId == userId) || (m.SenderId == userId && m.ReceiverId == currentUserId))
                    .OrderBy(m => m.SentAt)
                    .ToListAsync();

                // Mark as read
                var unreadMessages = messages.Where(m => m.ReceiverId == currentUserId && !m.IsRead).ToList();
                if (unreadMessages.Any())
                {
                    foreach (var msg in unreadMessages)
                    {
                        msg.IsRead = true;
                        msg.ReadAt = DateTime.UtcNow;
                    }
                    await _context.SaveChangesAsync();
                }

                ViewBag.SelectedUser = await _context.Users.FindAsync(userId);
            }

            var viewModel = new MessagingViewModel
            {
                Users = users,
                CurrentConversation = messages
            };

            return View(viewModel);
        }

        // POST: /Admin/SendMessage
        // POST: /Admin/SendMessage
        [HttpPost]
        public async Task<IActionResult> SendMessage(int receiverId, string? content, List<IFormFile>? attachments, double? latitude, double? longitude, IFormFile? voiceMessage, int? voiceDuration)
        {
            if (string.IsNullOrWhiteSpace(content) && (attachments == null || !attachments.Any()) && !latitude.HasValue && voiceMessage == null) 
                return RedirectToAction("Messaging");

            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            // SECURITY: Prevent sending to self
            if (receiverId == currentUserId)
            {
                return RedirectToAction("Messaging");
            }

            string? voiceMessagePath = null;

            // Handle Voice Message
            if (voiceMessage != null && voiceMessage.Length > 0)
            {
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads", "voice-messages");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                string uniqueFileName = Guid.NewGuid().ToString() + "_" + voiceMessage.FileName;
                if (!uniqueFileName.EndsWith(".webm") && !uniqueFileName.EndsWith(".mp3")) uniqueFileName += ".webm";

                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await voiceMessage.CopyToAsync(fileStream);
                }

                voiceMessagePath = "/uploads/voice-messages/" + uniqueFileName;
            }

            var messageContent = content;
            if (string.IsNullOrWhiteSpace(messageContent))
            {
                if (voiceMessage != null) messageContent = "[Voice Message]";
                else if (latitude.HasValue) messageContent = "[Location Shared]";
                else if (attachments != null && attachments.Any()) messageContent = "[Attachment]";
            }

            var message = new Message
            {
                SenderId = currentUserId,
                ReceiverId = receiverId,
                Content = messageContent ?? "",
                SentAt = DateTime.UtcNow,
                IsRead = false,
                MessageType = "Direct",
                Priority = "Normal",
                Subject = "Chat",
                Latitude = latitude,
                Longitude = longitude,
                VoiceMessagePath = voiceMessagePath,
                VoiceMessageDuration = voiceDuration
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            // Broadcast real-time update
            Console.WriteLine("[SignalR BROADCAST] Entity: Message, Action: Sent");
            await _hubContext.Clients.All.SendAsync("ReceiveDataUpdate", "Message", "Sent");

            // Handle Attachments
            if (attachments != null && attachments.Any())
            {
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads", "messages");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                foreach (var file in attachments)
                {
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(fileStream);
                    }

                    var attachment = new MessageAttachment
                    {
                        MessageId = message.Id,
                        FileName = file.FileName,
                        FilePath = "/uploads/messages/" + uniqueFileName,
                        FileSize = file.Length,
                        ContentType = file.ContentType,
                        UploadedAt = DateTime.UtcNow
                    };
                    _context.MessageAttachments.Add(attachment);
                }
                await _context.SaveChangesAsync();
            }

            // Create Notification for Receiver if enabled
            var receiverUser = await _context.Users.FindAsync(receiverId);
            if (receiverUser != null && receiverUser.NotifyOnMessages)
            {
                var senderUser = await _context.Users.FindAsync(currentUserId);
                await _notificationService.NotifyUserAsync(
                    receiverId,
                    "New Message Received",
                    $"You have a new message from {senderUser?.Name ?? "Admin"}",
                    "Message",
                    senderUser?.Role == UserRole.Student ? "/StudentPortal/Messaging" : "/Admin/Messaging?userId=" + currentUserId
                );
            }

            return RedirectToAction("Messaging");
        }

        // GET: /Admin/Reports
        [Authorize(Policy = "SupervisorOnly")]
        public async Task<IActionResult> Reports(string? className)
        {
            var studentsQuery = _context.Students.AsQueryable();
            var attendanceQuery = _context.Attendances.AsQueryable();
            var applicationsQuery = _context.Applications.AsQueryable();
            var complaintsQuery = _context.Complaints.AsQueryable();
            var feeSubQuery = _context.FeeSubmissions.AsQueryable();

            if (!string.IsNullOrEmpty(className))
            {
                studentsQuery = studentsQuery.Where(s => s.ClassName == className);
                attendanceQuery = attendanceQuery.Where(a => _context.Students.Any(s => s.Id == a.StudentId && s.ClassName == className));
                // Applications don't have ClassName until approved, so we skip filtering them by class unless we want to filter by target department
                complaintsQuery = complaintsQuery.Where(c => _context.Students.Any(s => s.Id == c.StudentId && s.ClassName == className));
                feeSubQuery = feeSubQuery.Where(fs => _context.Students.Any(s => s.Id == fs.StudentId && s.ClassName == className));
            }

            // 1. Attendance Stats
            var attendanceStats = new
            {
                Present = await attendanceQuery.CountAsync(a => a.Status == "Present"),
                Absent = await attendanceQuery.CountAsync(a => a.Status == "Absent"),
                Late = await attendanceQuery.CountAsync(a => a.Status == "Late"),
                Total = await attendanceQuery.CountAsync()
            };

            // 2. Attendance Trend (Last 30 Days)
            var thirtyDaysAgo = DateTime.Today.AddDays(-30);
            var attendanceTrend = await attendanceQuery
                .Where(a => a.AttendanceDate >= thirtyDaysAgo)
                .GroupBy(a => a.AttendanceDate.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Present = g.Count(a => a.Status == "Present" || a.Status == "Late"),
                    Total = g.Count()
                })
                .OrderBy(g => g.Date)
                .ToListAsync();

            // 3. Department Distribution (Students)
            var deptDistribution = await studentsQuery
                .Include(s => s.Department)
                .GroupBy(s => s.Department.Name)
                .Select(g => new { Department = g.Key, Count = g.Count() })
                .ToListAsync();

            // 4. Admissions Data
            var admissionsStatusData = await applicationsQuery
                .GroupBy(a => a.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var admissionsDeptData = await applicationsQuery
                .Include(a => a.PreferredDepartment)
                .GroupBy(a => a.PreferredDepartment.Name)
                .Select(g => new { Department = g.Key, Count = g.Count() })
                .ToListAsync();

            var admissionsEligibilityData = await applicationsQuery
                .GroupBy(a => a.IsEligible)
                .Select(g => new { IsEligible = g.Key, Count = g.Count() })
                .ToListAsync();

            var admissionStats = new
            {
                Total = await applicationsQuery.CountAsync(),
                Eligible = await applicationsQuery.CountAsync(a => a.IsEligible),
                NotEligible = await applicationsQuery.CountAsync(a => !a.IsEligible)
            };

            // 5. Student Status Data
            var studentStatusData = await studentsQuery
                .GroupBy(s => s.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();


            // 7. Complaints Data
            var complaintsData = await complaintsQuery
                .GroupBy(c => c.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var complaintsCategoryData = await complaintsQuery
                .GroupBy(c => c.Category)
                .Select(g => new { Category = g.Key, Count = g.Count() })
                .ToListAsync();

            // 8. Activities Data
            var activitiesParticipationData = await _context.Activities
                .Where(a => a.Status != "Cancelled")
                .Select(a => new { Title = a.Title, Participants = a.CurrentParticipants, Capacity = a.Capacity ?? 0 })
                .ToListAsync();

            var activitiesCategoryData = await _context.Activities
                .GroupBy(a => a.Category)
                .Select(g => new { Category = g.Key, Count = g.Count() })
                .ToListAsync();

            // 9. Fee Statistics (Submitted vs Remaining)
            var totalTargetStudents = await studentsQuery.CountAsync(s => s.Status == "Active");
            var submittedStudents = await feeSubQuery
                .Where(fs => fs.Status == "Submitted" || fs.Status == "Approved")
                .Select(fs => fs.StudentId)
                .Distinct()
                .CountAsync();
            
            var feeSubmissionStats = new
            {
                Submitted = submittedStudents,
                Remaining = Math.Max(0, totalTargetStudents - submittedStudents),
                Total = totalTargetStudents
            };

            // 10. Fee Collection Status Details
            var feeStatusDetails = await feeSubQuery
                .GroupBy(fs => fs.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            // 11. Class Comparison (if "All Classes" is selected)
            List<object> classAttendanceList = new List<object>();
            if (string.IsNullOrEmpty(className))
            {
                var classStats = await _context.Attendances
                    .Where(a => a.Student != null && !string.IsNullOrEmpty(a.Student.ClassName))
                    .GroupBy(a => a.Student.ClassName)
                    .Select(g => new
                    {
                        ClassName = g.Key,
                        PresentCount = g.Count(a => a.Status == "Present" || a.Status == "Late"),
                        TotalCount = g.Count()
                    })
                    .ToListAsync();

                classAttendanceList = classStats
                    .Select(x => new
                    {
                        ClassName = x.ClassName,
                        PresentRate = x.TotalCount > 0 ? (double)x.PresentCount * 100.0 / x.TotalCount : 0.0
                    })
                    .OrderByDescending(x => x.PresentRate)
                    .Cast<object>()
                    .ToList();
            }

            ViewBag.AttendanceStats = attendanceStats;
            ViewBag.AttendanceTrend = attendanceTrend.Select(d => (object)d).ToList();
            ViewBag.DeptDistribution = deptDistribution.Select(d => (object)d).ToList();
            
            ViewBag.AdmissionsStatusData = admissionsStatusData.Select(d => (object)d).ToList();
            ViewBag.AdmissionsDeptData = admissionsDeptData.Select(d => (object)d).ToList();
            ViewBag.AdmissionsEligibilityData = admissionsEligibilityData.Select(d => (object)d).ToList();
            ViewBag.AdmissionStats = admissionStats;

            ViewBag.StudentStatusData = studentStatusData.Select(d => (object)d).ToList();
            ViewBag.ComplaintsData = complaintsData.Select(d => (object)d).ToList();
            ViewBag.ComplaintsCategoryData = complaintsCategoryData.Select(d => (object)d).ToList();
            ViewBag.ActivitiesData = activitiesParticipationData.Select(d => (object)d).ToList();
            ViewBag.ActivitiesCategoryData = activitiesCategoryData.Select(d => (object)d).ToList();
            ViewBag.FeeSubmissionStats = feeSubmissionStats;
            ViewBag.FeeStatusDetails = feeStatusDetails.Select(d => (object)d).ToList();
            ViewBag.ClassAttendanceComparison = classAttendanceList;
            
            ViewBag.SelectedClass = className;
            ViewBag.Classes = new List<string> { "J1", "J2", "J3", "J4", "W1", "W2", "W3", "S1", "S2", "S3", "S4", "S5", "S6" };

            return View();
        }

        // GET: /Admin/Settings
        [Authorize(Policy = "StaffOnly")]
        public async Task<IActionResult> Settings()
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var user = await _context.Users.FindAsync(userId);

            // Fetch System Settings for Attendance
            var settings = await _context.SystemSettings.Where(s => s.Category == "Attendance").ToListAsync();
            ViewBag.SchoolLatitude = settings.FirstOrDefault(s => s.Key == "SchoolLatitude")?.Value;
            ViewBag.SchoolLongitude = settings.FirstOrDefault(s => s.Key == "SchoolLongitude")?.Value;
            ViewBag.AttendanceRange = settings.FirstOrDefault(s => s.Key == "AttendanceRangeMeters")?.Value;

            return View(user);
        }

        // POST: /Admin/UpdateProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(string name, string email, string phone, IFormFile? profilePicture)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var user = await _context.Users.FindAsync(userId);

            if (user == null) return NotFound();

            user.Name = name;
            user.Email = email;
            // Ensure phone is not null if DB constraint exists. If empty string is allowed, use that.
            // If DB allows empty string but not null, use "". If it requires a value, we rely on validation or existing value?
            // Assuming DB wants a string.
            user.PhoneNumber = phone ?? ""; 

            if (profilePicture != null && profilePicture.Length > 0)
            {
                // Simple file upload logic
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(profilePicture.FileName);
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads", fileName);
                
                Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads"));
                
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await profilePicture.CopyToAsync(stream);
                }
                user.ProfilePicturePath = "/uploads/" + fileName;
            }

            try 
            {
                await _context.SaveChangesAsync();
                
                // Refresh the sign-in cookie with new claims (name, photo)
                await _authService.SignInAsync(HttpContext, user);

                TempData["Success"] = "Profile updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile");
                TempData["Error"] = "An error occurred while updating profile.";
            }

            return RedirectToAction("Settings");
        }

        // POST: /Admin/UpdatePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrEmpty(currentPassword))
            {
                TempData["Error"] = "Current password is required.";
                return RedirectToAction("Settings");
            }

            if (string.IsNullOrEmpty(newPassword))
            {
                TempData["Error"] = "New password is required.";
                return RedirectToAction("Settings");
            }

            if (newPassword.Length < 6)
            {
                TempData["Error"] = "New password must be at least 6 characters.";
                return RedirectToAction("Settings");
            }

            if (newPassword != confirmPassword)
            {
                TempData["Error"] = "Passwords do not match.";
                return RedirectToAction("Settings");
            }

            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var user = await _context.Users.FindAsync(userId);

            if (user == null) 
            {
                _logger.LogWarning($"UpdatePassword: User with ID {userId} not found.");
                return NotFound();
            }

            Console.WriteLine($"[UpdatePassword] Attempting to update password for User ID: {user.Id}");
            Console.WriteLine($"[UpdatePassword] Current Password (input, len): {currentPassword?.Length}");
            Console.WriteLine($"[UpdatePassword] Stored Hash (len): {user.Password?.Length}");
            bool isPasswordCorrect = _passwordHasher.VerifyPassword(currentPassword, user.Password);
            Console.WriteLine($"[UpdatePassword] Verification Result: {isPasswordCorrect}");

            // Verify current password
            if (!isPasswordCorrect)
            {
                TempData["Error"] = "Incorrect current password.";
                return RedirectToAction("Settings");
            }

            user.Password = _passwordHasher.HashPassword(newPassword);
            await _context.SaveChangesAsync();
            
            // Refresh sign-in to ensure security stamp/claims are up to date if needed (optional for password but good practice)
             await _authService.SignInAsync(HttpContext, user);

            TempData["Success"] = "Password updated successfully.";
            return RedirectToAction("Settings");
        }

        // POST: /Admin/UpdateAppearance
        [HttpPost]
        public async Task<IActionResult> UpdateAppearance(bool darkMode)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var user = await _context.Users.FindAsync(userId);

            if (user == null) return NotFound();

            user.IsDarkMode = darkMode;
            await _context.SaveChangesAsync();

            // Refresh cookie to apply theme immediately
            await _authService.SignInAsync(HttpContext, user);

            return Json(new { success = true, isDarkMode = user.IsDarkMode });
        }

        // POST: /Admin/UpdateLanguage
        [HttpPost]
        public async Task<IActionResult> UpdateLanguage(string language)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var user = await _context.Users.FindAsync(userId);

            if (user == null) return NotFound();

            user.Language = language;
            await _context.SaveChangesAsync();

            // Set the localization cookie to ensure the middleware picks it up
            Response.Cookies.Append(
                Microsoft.AspNetCore.Localization.CookieRequestCultureProvider.DefaultCookieName,
                Microsoft.AspNetCore.Localization.CookieRequestCultureProvider.MakeCookieValue(new Microsoft.AspNetCore.Localization.RequestCulture(language)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
            );

            // Refresh cookie to apply language immediately
            await _authService.SignInAsync(HttpContext, user);

            return Json(new { success = true, language = user.Language });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePreferences(
            bool notifyComplaints, 
            bool notifySuggestions,
            bool notifyApplications, 
            bool notifyMessages, 
            bool notifyAttendance,
            bool notifyFees,
            bool notifyActivityRegistration,
            bool notifyNewActivity)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var user = await _context.Users.FindAsync(userId);

            if (user == null) return NotFound();

            user.NotifyOnComplaints = notifyComplaints;
    user.NotifyOnSuggestions = notifySuggestions;
    user.NotifyOnApplications = notifyApplications;
    user.NotifyOnMessages = notifyMessages;
    user.NotifyOnAttendance = notifyAttendance;
    user.NotifyOnFees = notifyFees;
    user.NotifyOnActivityRegistration = notifyActivityRegistration;
    user.NotifyOnNewActivity = notifyNewActivity;

            await _context.SaveChangesAsync();

            // Refresh cookie
            await _authService.SignInAsync(HttpContext, user);

            TempData["Success"] = "Preferences updated successfully.";
            return RedirectToAction("Settings");
        }

        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSystemSettings(string lat, string lng, string range)
        {
            try
            {
                var settings = await _context.SystemSettings.ToListAsync();
                
                var latSetting = settings.FirstOrDefault(s => s.Key == "SchoolLatitude");
                if (latSetting == null)
                {
                    _context.SystemSettings.Add(new SystemSettings { Key = "SchoolLatitude", Value = lat, Category = "Attendance", CreatedAt = DateTime.UtcNow });
                }
                else
                {
                    latSetting.Value = lat;
                    latSetting.UpdatedAt = DateTime.UtcNow;
                }

                var lngSetting = settings.FirstOrDefault(s => s.Key == "SchoolLongitude");
                if (lngSetting == null)
                {
                    _context.SystemSettings.Add(new SystemSettings { Key = "SchoolLongitude", Value = lng, Category = "Attendance", CreatedAt = DateTime.UtcNow });
                }
                else
                {
                    lngSetting.Value = lng;
                    lngSetting.UpdatedAt = DateTime.UtcNow;
                }

                var rangeSetting = settings.FirstOrDefault(s => s.Key == "AttendanceRangeMeters");
                if (rangeSetting == null)
                {
                    _context.SystemSettings.Add(new SystemSettings { Key = "AttendanceRangeMeters", Value = range, Category = "Attendance", CreatedAt = DateTime.UtcNow });
                }
                else
                {
                    rangeSetting.Value = range ?? "100";
                    rangeSetting.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "System settings updated successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error updating settings: " + ex.Message });
            }
        }

        // ========================================
        // BACKUP MANAGEMENT
        // ========================================

        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> ManageBackups()
        {
            var backups = await _backupService.GetBackupsAsync();
            return PartialView("_BackupManagementPartial", backups);
        }

        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> CreateDatabaseBackup()
        {
            try
            {
                var userName = User.Identity?.Name ?? "Admin";
                await _backupService.CreateDatabaseBackupAsync(userName);
                return Json(new { success = true, message = "Database backup created successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating DB backup");
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> CreateFilesBackup()
        {
            try
            {
                var userName = User.Identity?.Name ?? "Admin";
                await _backupService.CreateFilesBackupAsync(userName);
                return Json(new { success = true, message = "Files backup created successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating files backup");
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpGet]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> DownloadBackup(int id)
        {
            var backup = await _backupService.GetBackupByIdAsync(id);
            if (backup == null || !System.IO.File.Exists(backup.FilePath))
            {
                return NotFound();
            }

            var contentType = backup.Type == "Database" ? "application/octet-stream" : "application/zip";
            return PhysicalFile(backup.FilePath, contentType, backup.FileName);
        }

        [HttpGet]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> RiskAlerts(string? type, string? searchTerm)
        {
            var alerts = await _riskDetectionService.GetUnresolvedAlertsAsync(type, searchTerm);
            ViewBag.SelectedType = type;
            ViewBag.SearchTerm = searchTerm;
            return View(alerts);
        }

        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TriggerRiskDetection()
        {
            int count = await _riskDetectionService.DetectRisksAsync();
            return Json(new { success = true, message = $"Scan completed. {count} new risks detected." });
        }

        [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResolveRiskAlert(int id)
    {
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId)) 
            return Unauthorized();

        bool success = await _riskDetectionService.ResolveAlertAsync(id, userId);
            if (success)
            {
                return Json(new { success = true, message = "Risk case marked as resolved." });
            }
            return Json(new { success = false, message = "Alert not found." });
        }

        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> DeleteBackup(int id)
        {
            var success = await _backupService.DeleteBackupAsync(id);
            if (success)
            {
                return Json(new { success = true, message = "Backup deleted successfully." });
            }
            return Json(new { success = false, message = "Error deleting backup." });
        }
    }
}
