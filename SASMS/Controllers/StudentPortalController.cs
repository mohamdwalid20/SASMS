using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SASMS.Data;
using SASMS.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using SASMS.Services;
using SASMS.ViewModels;
using SASMS.Helpers;
using Microsoft.Extensions.Localization;

namespace SASMS.Controllers
{
    [Authorize(Policy = "StudentOnly")]
    public class StudentPortalController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IAuthService _authService;
        private readonly ILogger<StudentPortalController> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IStringLocalizer<SharedResources> _localizer;
        private readonly INotificationService _notificationService;
        private readonly Microsoft.AspNetCore.SignalR.IHubContext<SASMS.Hubs.SASMSHub> _hubContext;

        public StudentPortalController(ApplicationDbContext context, IPasswordHasher passwordHasher, IAuthService authService, ILogger<StudentPortalController> logger, IWebHostEnvironment webHostEnvironment, IStringLocalizer<SharedResources> localizer, Microsoft.AspNetCore.SignalR.IHubContext<SASMS.Hubs.SASMSHub> hubContext, INotificationService notificationService)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _authService = authService;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
            _localizer = localizer;
            _hubContext = hubContext;
            _notificationService = notificationService;
        }

        public async Task<IActionResult> Index()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            var student = await _context.Students
                .Include(s => s.User)
                .Include(s => s.Department)
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (student == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // 1. Calculate Attendance Rate
            var attendanceRecords = await _context.Attendances
                .Where(a => a.StudentId == student.Id)
                .ToListAsync();

            double attendanceRate = 0;
            if (attendanceRecords.Any())
            {
                var presentCount = attendanceRecords.Count(a => a.Status == "Present" || a.Status == "Late");
                attendanceRate = (double)presentCount / attendanceRecords.Count * 100;
            }
            ViewBag.AttendanceRate = attendanceRate;

            // 2. Calculate Outstanding Fees
            // Get all fees that apply to the student (either specifically or via department)
            var applicableFees = await _context.Fees
                .Where(f => f.IsActive && (f.DepartmentId == null || f.DepartmentId == student.DepartmentId))
                .ToListAsync();

            var totalFeeAmount = applicableFees.Sum(f => f.Amount);

            // Get completed payments
            var completedPayments = await _context.Payments
                .Where(p => p.StudentId == student.Id && p.Status == "Completed")
                .SumAsync(p => p.Amount);

            ViewBag.OutstandingFees = totalFeeAmount - completedPayments;

            // 3. Count Active Activities
            var activeActivitiesCount = await _context.ActivityParticipations
                .CountAsync(ap => ap.StudentId == student.Id && 
                                 (ap.Status == "Registered" || ap.Status == "Confirmed" || ap.Status == "Attended"));
            
            ViewBag.ActiveActivities = activeActivitiesCount;

            return View(student);
        }

        public async Task<IActionResult> Complaints()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId.ToString() == userId);
            
            if (student == null) return RedirectToAction("Login", "Account");

            var complaints = await _context.Complaints
                .Where(c => c.StudentId == student.Id)
                .OrderByDescending(c => c.ComplaintDate)
                .ToListAsync();

            return View(complaints);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitComplaint(string title, string category, string priority, string description)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var student = await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.UserId.ToString() == userId);

            if (student == null) return RedirectToAction("Login", "Account");

            var complaint = new Complaint
            {
                StudentId = student.Id,
                Title = title,
                Category = category,
                Priority = priority,
                Description = description,
                Status = "Pending",
                ComplaintDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _context.Complaints.Add(complaint);
            await _context.SaveChangesAsync();

            // Notify Admin and Supervisor (if enabled)
            var staffToNotify = await _context.Users
                .Where(u => u.IsActive && u.NotifyOnComplaints && (u.Role == UserRole.Admin || u.Role == UserRole.StudentAffairs || u.Role == UserRole.Supervisor))
                .ToListAsync();

            var studentName = User.Identity?.Name ?? "A student";
            foreach (var staff in staffToNotify)
            {
                await _notificationService.NotifyUserAsync(
                    staff.Id,
                    "New Complaint Submitted",
                    $"{studentName} has submitted a new complaint: '{complaint.Title}'.",
                    "Complaint",
                    "/Complaint/List"
                );
            }
            
            // Broadcast real-time update
            Console.WriteLine("[SignalR BROADCAST] Entity: Complaint, Action: Submitted");
            await _hubContext.Clients.All.SendAsync("ReceiveDataUpdate", "Complaint", "Submitted");

            TempData["Success"] = "Complaint submitted successfully.";
            return RedirectToAction(nameof(Complaints));
        }

        public async Task<IActionResult> Fees()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var student = await _context.Students
                .Include(s => s.Department)
                .FirstOrDefaultAsync(s => s.UserId.ToString() == userId);
            
            if (student == null) return RedirectToAction("Login", "Account");

            var submissions = await _context.FeeSubmissions
                .Include(fs => fs.GovernmentFee)
                    .ThenInclude(f => f.AcademicYear)
                .Where(fs => fs.StudentId == student.Id && fs.GovernmentFee.IsPublished)
                .OrderByDescending(fs => fs.CreatedAt)
                .ToListAsync();

            return View(submissions);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitReceipt(int submissionId, string receiptNumber, IFormFile receiptImage)
        {
            var submission = await _context.FeeSubmissions.FindAsync(submissionId);
            if (submission == null) return NotFound();

            if (receiptImage != null && receiptImage.Length > 0)
            {
                // Save image
                var uploads = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "receipts");
                if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);
                
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(receiptImage.FileName);
                var filePath = Path.Combine(uploads, fileName);
                
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await receiptImage.CopyToAsync(fileStream);
                }

                submission.ReceiptNumber = receiptNumber;
                submission.ReceiptImagePath = "/uploads/receipts/" + fileName;
                submission.Status = "Submitted";
                submission.SubmissionDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Notify Admins and Student Affairs (if enabled)
                var adminsToNotify = await _context.Users
                    .Where(u => (u.Role == UserRole.Admin || u.Role == UserRole.StudentAffairs) && u.NotifyOnFees && u.IsActive)
                    .ToListAsync();

                var studentName = User.Identity?.Name ?? "A student";
                foreach (var admin in adminsToNotify)
                {
                    await _notificationService.NotifyUserAsync(
                        admin.Id,
                        "New Fee Receipt Submitted",
                        $"{studentName} has submitted a receipt for review.",
                        "Fee",
                        $"/Fees/Submissions?feeId={submission.GovernmentFeeId}&status=Submitted"
                    );
                }

                // Broadcast real-time update
                await _hubContext.Clients.All.SendAsync("ReceiveDataUpdate", "FeeSubmission", "Submitted");


                return Json(new { success = true, message = _localizer["StatusUpdated"].Value });
            }

            return Json(new { success = false, message = _localizer["Error"].Value });
        }

        public async Task<IActionResult> Attendance()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId.ToString() == userId);
            
            if (student == null) return RedirectToAction("Login", "Account");

            var attendanceRecords = await _context.Attendances
                .Where(a => a.StudentId == student.Id)
                .OrderByDescending(a => a.AttendanceDate)
                .ToListAsync();

            ViewBag.TotalPresent = attendanceRecords.Count(a => a.Status == "Present");
            ViewBag.TotalAbsent = attendanceRecords.Count(a => a.Status == "Absent");
            ViewBag.TotalLate = attendanceRecords.Count(a => a.Status == "Late");

            // Check if already checked in today
            var today = DateTime.Today;
            ViewBag.AlreadyCheckedIn = attendanceRecords.Any(a => a.AttendanceDate.Date == today);

            return View(attendanceRecords);
        }

        [HttpPost]
        public async Task<IActionResult> CheckIn(double latitude, double longitude)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return Json(new { success = false, message = "Unauthorized access." });

            var student = await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (student == null) return Json(new { success = false, message = "Student not found." });

            var today = DateTime.Today;
            var existingAttendance = await _context.Attendances
                .FirstOrDefaultAsync(a => a.StudentId == student.Id && a.AttendanceDate.Date == today);

            if (existingAttendance != null)
            {
                return Json(new { success = false, message = "You have already checked in today." });
            }

            // Get school location from settings
            var schoolLatSetting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.Key == "SchoolLatitude");
            var schoolLonSetting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.Key == "SchoolLongitude");
            var rangeSetting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.Key == "AttendanceRangeMeters");

            if (schoolLatSetting == null || schoolLonSetting == null)
            {
                return Json(new { success = false, message = "School location not configured." });
            }

            double schoolLat = double.Parse(schoolLatSetting.Value);
            double schoolLon = double.Parse(schoolLonSetting.Value);
            double range = rangeSetting != null ? double.Parse(rangeSetting.Value) : 100.0;

            bool isWithinRange = GeoLocationHelper.IsWithinRange(latitude, longitude, schoolLat, schoolLon, range);

            if (!isWithinRange)
            {
                return Json(new { success = false, message = "You are out of the school range. Attendance rejected." });
            }

            // Determine if late (e.g., after 9:00 AM)
            var now = DateTime.Now;
            var checkInTime = TimeOnly.FromDateTime(now);
            string status = "Present";
            
            // Optional: Get school start time from settings
            var startTimeSetting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.Key == "SchoolStartTime");
            if (startTimeSetting != null && TimeOnly.TryParse(startTimeSetting.Value, out var startTime))
            {
                if (checkInTime > startTime.AddMinutes(15)) // 15 mins grace period
                {
                    status = "Late";
                }
            }

            var attendance = new Attendance
            {
                StudentId = student.Id,
                AttendanceDate = today,
                CheckInTime = checkInTime,
                Status = status,
                CreatedAt = DateTime.UtcNow,
                Notes = "Self Check-in (GPS)"
            };

            _context.Attendances.Add(attendance);
            await _context.SaveChangesAsync();

            // Notify Admins and Student Affairs (if enabled)
            var adminsToNotify = await _context.Users
                .Where(u => (u.Role == UserRole.Admin || u.Role == UserRole.StudentAffairs) && u.NotifyOnAttendance && u.IsActive)
                .ToListAsync();

            foreach (var admin in adminsToNotify)
            {
                await _notificationService.NotifyUserAsync(
                    admin.Id,
                    "Student Check-In",
                    $"Student '{student.User.Name}' has checked in as {status}.",
                    "Attendance",
                    $"/Attendance/List?selectedDate={today:yyyy-MM-dd}"
                );
            }

            // Broadcast real-time update
            await _hubContext.Clients.All.SendAsync("ReceiveDataUpdate", "Attendance", status);

            return Json(new { success = true, message = $"Attendance recorded as {status}." });
        }

        public async Task<IActionResult> Activities()
        {
            var activities = await _context.Activities
                .Include(a => a.ManagedBy)
                .Where(a => a.Status != "Completed")
                .OrderBy(a => a.StartDate)
                .ToListAsync();

            return View(activities);
        }

        [HttpPost]
        public IActionResult SelectChat(int userId)
        {
            HttpContext.Session.SetInt32("SelectedChatUserId", userId);
            return RedirectToAction("Messaging");
        }

        public async Task<IActionResult> Messaging(int? userId)
        {
            // If userId is passed in URL (legacy or direct link), save to session and redirect to clean URL
            if (userId.HasValue)
            {
                HttpContext.Session.SetInt32("SelectedChatUserId", userId.Value);
                return RedirectToAction("Messaging");
            }

            // Try to get from session
            userId = HttpContext.Session.GetInt32("SelectedChatUserId");
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            // SECURITY: Prevent chatting with self (already handled by logic but good double check)
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

            // 1. Get List of Staff to Chat With (Admins & Student Affairs)
            var users = await _context.Users
                .Where(u => u.Id != currentUserId && (u.Role == UserRole.StudentAffairs || u.Role == UserRole.Admin))
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
                    .Include(m => m.Sender)
                    .Include(m => m.Receiver)
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

                // Security Check: Verify that the student is allowed to chat with this user (Admin or StudentAffairs)
                var targetUser = await _context.Users.FindAsync(userId);
                if (targetUser == null || (targetUser.Role != UserRole.Admin && targetUser.Role != UserRole.StudentAffairs))
                {
                     HttpContext.Session.Remove("SelectedChatUserId");
                     return RedirectToAction("Messaging");
                }

                ViewBag.SelectedUser = targetUser;
            }

            var viewModel = new MessagingViewModel
            {
                Users = users,
                CurrentConversation = messages
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(int receiverId, string? content, List<IFormFile>? attachments, double? latitude, double? longitude, IFormFile? voiceMessage, int? voiceDuration)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var parsedUserId = int.Parse(userId);

            // SECURITY: Prevent sending to self
            if (receiverId == parsedUserId)
            {
                return RedirectToAction("Messaging");
            }

            if (string.IsNullOrWhiteSpace(content) && (attachments == null || !attachments.Any()) && !latitude.HasValue && voiceMessage == null) 
                return RedirectToAction("Messaging");

            string? voiceMessagePath = null;

            // Handle Voice Message
            if (voiceMessage != null && voiceMessage.Length > 0)
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "voice-messages");
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
                SenderId = parsedUserId,
                ReceiverId = receiverId,
                Subject = "Direct Message",
                Content = messageContent ?? "",
                SentAt = DateTime.UtcNow,
                IsRead = false,
                MessageType = "Direct",
                Priority = "Normal",
                Latitude = latitude,
                Longitude = longitude,
                VoiceMessagePath = voiceMessagePath,
                VoiceMessageDuration = voiceDuration
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            // Handle Attachments
            if (attachments != null && attachments.Any())
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "messages");
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
                var senderUser = await _context.Users.FindAsync(parsedUserId);
                await _notificationService.NotifyUserAsync(
                    receiverId,
                    "New Message Received",
                    $"You have a new message from {senderUser?.Name ?? "Student"}",
                    "Message",
                    "/Admin/Messaging?userId=" + parsedUserId
                );
            }

            return RedirectToAction("Messaging");
        }


        public async Task<IActionResult> Settings()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var student = await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.UserId.ToString() == userId);

            if (student == null) return RedirectToAction("Login", "Account");

            return View(student.User);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(string name, string email, string phone, IFormFile? profilePicture)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var user = await _context.Users.FindAsync(userId);

            if (user == null) return NotFound();

            user.Name = name;
            user.Email = email;
            user.PhoneNumber = phone ?? "";

            if (profilePicture != null && profilePicture.Length > 0)
            {
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

            if (user == null) return RedirectToAction("Login", "Account");

            bool isPasswordCorrect = _passwordHasher.VerifyPassword(currentPassword, user.Password);

            if (!isPasswordCorrect)
            {
                TempData["Error"] = "Incorrect current password.";
                return RedirectToAction("Settings");
            }

            user.Password = _passwordHasher.HashPassword(newPassword);
            await _context.SaveChangesAsync();

            await _authService.SignInAsync(HttpContext, user);

            TempData["Success"] = "Password updated successfully.";
            return RedirectToAction(nameof(Settings));
        }

        [HttpPost]
        public async Task<IActionResult> UpdateAppearance(bool darkMode)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var user = await _context.Users.FindAsync(userId);

            if (user == null) return NotFound();

            user.IsDarkMode = darkMode;
            await _context.SaveChangesAsync();

            await _authService.SignInAsync(HttpContext, user);

            return Json(new { success = true, isDarkMode = user.IsDarkMode });
        }

        // POST: /StudentPortal/UpdateLanguage
        [HttpPost]
        public async Task<IActionResult> UpdateLanguage(string language)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var user = await _context.Users.FindAsync(userId);

            if (user == null) return NotFound();

            user.Language = language;
            await _context.SaveChangesAsync();

            // Refresh cookie
             await _authService.SignInAsync(HttpContext, user);

            // Set the localization cookie to ensure the middleware picks it up
            Response.Cookies.Append(
                Microsoft.AspNetCore.Localization.CookieRequestCultureProvider.DefaultCookieName,
                Microsoft.AspNetCore.Localization.CookieRequestCultureProvider.MakeCookieValue(new Microsoft.AspNetCore.Localization.RequestCulture(language)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
            );

            return Json(new { success = true, language = user.Language });
        }

        // POST: /StudentPortal/UpdatePreferences
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePreferences(bool notifyComplaints, bool notifyApplications, bool notifyMessages)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var user = await _context.Users.FindAsync(userId);

            if (user == null) return NotFound();

            user.NotifyOnComplaints = notifyComplaints;
            user.NotifyOnApplications = notifyApplications; // Repurposed for School Announcements in UI
            user.NotifyOnMessages = notifyMessages;

            await _context.SaveChangesAsync();

            // Refresh cookie
            await _authService.SignInAsync(HttpContext, user);

            TempData["Success"] = "Preferences updated successfully.";
            return RedirectToAction("Settings");
        }
        // POST: /StudentPortal/DeleteComplaint/5
        [HttpPost]
        public async Task<IActionResult> DeleteComplaint(int id)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
            
            if (student == null) return Unauthorized();

            var complaint = await _context.Complaints.FirstOrDefaultAsync(c => c.Id == id && c.StudentId == student.Id);
            
            if (complaint == null)
            {
                return Json(new { success = false, message = "Complaint not found" });
            }

            // User requested students can delete RESOLVED complaints
            if (complaint.Status != "Resolved" && complaint.Status != "Pending")
            {
                return Json(new { success = false, message = "Only Pending or Resolved complaints can be deleted." });
            }

            _context.Complaints.Remove(complaint);
            await _context.SaveChangesAsync();

            // Broadcast real-time update
            Console.WriteLine($"[SignalR BROADCAST] Entity: Complaint, Action: DeletedByStudent");
            await _hubContext.Clients.All.SendAsync("ReceiveDataUpdate", "Complaint", "Deleted");

            return Json(new { success = true, message = "Complaint deleted successfully" });
        }

        // POST: /StudentPortal/EditComplaint/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditComplaint(int id, string title, string category, string priority, string description)
        {
            Console.WriteLine($"[EditComplaint] Received request for ID: {id}");
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
            
            if (student == null) {
                Console.WriteLine("[EditComplaint] Student not found for user ID: " + userId);
                return Unauthorized();
            }

            var complaint = await _context.Complaints.FirstOrDefaultAsync(c => c.Id == id && c.StudentId == student.Id);
            
            if (complaint == null)
            {
                Console.WriteLine($"[EditComplaint] Complaint {id} not found or doesn't belong to student {student.Id}");
                return Json(new { success = false, message = "Complaint not found" });
            }

            if (complaint.Status != "Pending")
            {
                Console.WriteLine($"[EditComplaint] Blocked: Complaint {id} is in status {complaint.Status}");
                return Json(new { success = false, message = "Only Pending complaints can be edited." });
            }

            complaint.Title = title;
            complaint.Category = category;
            complaint.Priority = priority;
            complaint.Description = description;
            complaint.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Broadcast real-time update
            Console.WriteLine($"[SignalR BROADCAST] Entity: Complaint, Action: UpdatedByStudent");
            await _hubContext.Clients.All.SendAsync("ReceiveDataUpdate", "Complaint", "Updated");

            return Json(new { success = true, message = "Complaint updated successfully" });
        }
        public async Task<IActionResult> Suggestions()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId.ToString() == userId);

            if (student == null) return RedirectToAction("Login", "Account");

            var suggestions = await _context.Suggestions
                .Where(s => s.StudentId == student.Id)
                .OrderByDescending(s => s.SuggestionDate)
                .ToListAsync();

            return View(suggestions);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitSuggestion(string title, string category, string description, bool isAnonymous = false)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId.ToString() == userId);

            if (student == null) return Unauthorized();

            var suggestion = new Suggestion
            {
                StudentId = student.Id,
                Title = title,
                Category = category,
                Description = description,
                IsAnonymous = isAnonymous,
                Status = "Pending",
                SuggestionDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                Upvotes = 0
            };

            _context.Suggestions.Add(suggestion);
            await _context.SaveChangesAsync();

            // Notify Admin and Supervisor (if enabled)
            var staffToNotify = await _context.Users
                .Where(u => u.IsActive && u.NotifyOnSuggestions && (u.Role == UserRole.Admin || u.Role == UserRole.StudentAffairs || u.Role == UserRole.Supervisor))
                .ToListAsync();

            var studentName = User.Identity?.Name ?? "A student";
            foreach (var staff in staffToNotify)
            {
                await _notificationService.NotifyUserAsync(
                    staff.Id,
                    "New Suggestion Submitted",
                    (suggestion.IsAnonymous ? "An anonymous student" : studentName) + $" has submitted a new suggestion: '{suggestion.Title}'.",
                    "Suggestion",
                    "/Suggestion/List"
                );
            }

            // Broadcast real-time update
            await _hubContext.Clients.All.SendAsync("ReceiveDataUpdate", "Suggestion", "Created");

            return Json(new { success = true, message = "Suggestion submitted successfully" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSuggestion(int id, string title, string category, string description, bool isAnonymous = false)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId.ToString() == userId);

            if (student == null) return Unauthorized();

            var suggestion = await _context.Suggestions.FirstOrDefaultAsync(s => s.Id == id && s.StudentId == student.Id);
            if (suggestion == null || suggestion.Status != "Pending")
            {
                return Json(new { success = false, message = "Suggestion not found or already under review" });
            }

            suggestion.Title = title;
            suggestion.Category = category;
            suggestion.Description = description;
            suggestion.IsAnonymous = isAnonymous;
            suggestion.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await _hubContext.Clients.All.SendAsync("ReceiveDataUpdate", "Suggestion", "Updated");

            return Json(new { success = true, message = "Suggestion updated successfully" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSuggestion(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId.ToString() == userId);

            if (student == null) return Unauthorized();

            var suggestion = await _context.Suggestions.FirstOrDefaultAsync(s => s.Id == id && s.StudentId == student.Id);
            if (suggestion == null || suggestion.Status != "Pending")
            {
                return Json(new { success = false, message = "Suggestion not found or already under review" });
            }

            _context.Suggestions.Remove(suggestion);
            await _context.SaveChangesAsync();
            await _hubContext.Clients.All.SendAsync("ReceiveDataUpdate", "Suggestion", "Deleted");

            return Json(new { success = true, message = "Suggestion deleted successfully" });
        }
    }
}
