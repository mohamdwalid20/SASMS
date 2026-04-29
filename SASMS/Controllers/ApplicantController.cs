using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SASMS.Data;
using SASMS.Models;
using SASMS.Services;
using SASMS.ViewModels;


namespace SASMS.Controllers
{
    /// <summary>
    /// Public-facing controller for applicant registration
    /// No authentication required for accessing application form
    /// </summary>
    public class ApplicantController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ILogger<ApplicantController> _logger;
        private readonly INotificationService _notificationService;
        private readonly Microsoft.AspNetCore.SignalR.IHubContext<SASMS.Hubs.SASMSHub> _hubContext;

        public ApplicantController(ApplicationDbContext context, IPasswordHasher passwordHasher, ILogger<ApplicantController> logger, Microsoft.AspNetCore.SignalR.IHubContext<SASMS.Hubs.SASMSHub> hubContext, INotificationService notificationService)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _logger = logger;
            _hubContext = hubContext;
            _notificationService = notificationService;
        }

        // GET: /Applicant/Apply
        public async Task<IActionResult> Apply()
        {
            ViewBag.Departments = await _context.Departments
                .Where(d => d.IsActive)
                .ToListAsync();

            ViewBag.DynamicFields = await _context.DynamicFields
                .Where(f => f.IsActive)
                .OrderBy(f => f.Section)
                .ThenBy(f => f.DisplayOrder)
                .ToListAsync();

            return View(new ApplicantApplyViewModel { DateOfBirth = new DateTime(2008, 1, 1) });
        }

        // POST: /Applicant/Apply
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(ApplicantApplyViewModel model)
        {
            // Sync dynamic values from Request.Form if they exist
            var dynamicFields = await _context.DynamicFields.Where(f => f.IsActive).ToListAsync();
            foreach (var field in dynamicFields)
            {
                if (field.Type != FieldType.File)
                {
                    string value = Request.Form[$"dyn_{field.FieldName}"].ToString()?.Trim();
                    if (!string.IsNullOrEmpty(value))
                    {
                        model.DynamicValues[field.FieldName] = value;
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Departments = await _context.Departments.Where(d => d.IsActive).ToListAsync();
                ViewBag.DynamicFields = await _context.DynamicFields.Where(f => f.IsActive).OrderBy(f => f.Section).ThenBy(f => f.DisplayOrder).ToListAsync();
                return View(model);
            }

            var dynamicValuesToSave = new List<DynamicFieldValue>();

            foreach (var field in dynamicFields)
            {
                string? value = null;
                if (field.Type == FieldType.File)
                {
                    var file = Request.Form.Files.GetFile($"dyn_{field.FieldName}");
                    if (file != null && file.Length > 0)
                    {
                        string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/applications");
                        if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(fileStream);
                        }
                        value = "/uploads/applications/" + uniqueFileName;
                    }
                }
                else
                {
                    if (model.DynamicValues.TryGetValue(field.FieldName, out var val))
                    {
                        value = val;
                    }
                }

                // Validation
                if (field.IsRequired && string.IsNullOrEmpty(value))
                {
                    ModelState.AddModelError($"dyn_{field.FieldName}", $"{field.Label} is required.");
                }

                
                if (!string.IsNullOrEmpty(value))
                {
                    dynamicValuesToSave.Add(new DynamicFieldValue { FieldId = field.Id, Value = value });
                }
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Departments = await _context.Departments.Where(d => d.IsActive).ToListAsync();
                ViewBag.DynamicFields = await _context.DynamicFields.Where(f => f.IsActive).OrderBy(f => f.Section).ThenBy(f => f.DisplayOrder).ToListAsync();
                return View(model);
            }

            // Check if email or national ID already exists
            model.Email = model.Email?.Trim();
            model.NationalId = model.NationalId?.Trim();

            if (await _context.Users.AnyAsync(u => u.Email == model.Email))
            {
                ModelState.AddModelError("Email", "Email already exists.");
            }

            if (await _context.Users.AnyAsync(u => u.NationalId == model.NationalId))
            {
                ModelState.AddModelError("NationalId", "National ID already exists.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Departments = await _context.Departments.Where(d => d.IsActive).ToListAsync();
                ViewBag.DynamicFields = await _context.DynamicFields.Where(f => f.IsActive).OrderBy(f => f.Section).ThenBy(f => f.DisplayOrder).ToListAsync();
                return View(model);
            }

            Application newApp = null;
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Create User account
                var user = new User
                {
                    Name = model.Name,
                    Email = model.Email,
                    NationalId = model.NationalId,
                    PhoneNumber = model.Phone,
                    Password = _passwordHasher.HashPassword(model.Password),
                    Role = UserRole.Applicant,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Create Applicant profile
                var applicant = new Applicant
                {
                    UserId = user.Id,
                    DateOfBirth = DateOnly.FromDateTime(model.DateOfBirth),
                    Gender = model.Gender,
                    Address = model.Address,
                    Grade = model.Grade,
                    GradeOfPrimarySchool = model.GradeOfPrimarySchool,
                    GradeOfEnglishExam = model.GradeOfEnglishExam,
                    GradeOfMathExam = model.GradeOfMathExam,
                    GradeOfScienceExam = model.GradeOfScienceExam,
                    PreferredDepartmentId = model.PreferredDepartmentId,
                    ParentName = model.ParentName,
                    ParentPhone = model.ParentPhone,
                    Kinship = model.Kinship,
                    ParentMajor = model.ParentMajor ?? "" // Ensure it's not null for non-nullable DB column
                };

                _context.Applicants.Add(applicant);
                await _context.SaveChangesAsync();

                // Create Application record
                // Generate a more unique application number to avoid clashes
                string uniquePart = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
                newApp = new Application
                {
                    ApplicantId = applicant.Id,
                    PreferredDepartmentId = model.PreferredDepartmentId,
                    ApplicationNumber = $"APP-{DateTime.UtcNow.Year}-{uniquePart}",
                    ApplicationDate = DateTime.UtcNow,
                    Status = "Pending",
                    TotalScore = (decimal)(model.GradeOfEnglishExam + model.GradeOfMathExam + model.GradeOfScienceExam),
                    IsEligible = model.GradeOfPrimarySchool >= 220 && model.GradeOfEnglishExam >= 48 && model.GradeOfMathExam >= 48,
                    AcademicYearId = (await _context.AcademicYears.FirstOrDefaultAsync(ay => ay.IsActive))?.Id,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Applications.Add(newApp);
                await _context.SaveChangesAsync();

                // Save dynamic field values
                foreach (var dynVal in dynamicValuesToSave)
                {
                    dynVal.ApplicationId = newApp.Id;
                    _context.DynamicFieldValues.Add(dynVal);
                }
                await _context.SaveChangesAsync();

                // Notify Admins
                var admins = await _context.Users
                    .Where(u => (u.Role == UserRole.Admin || u.Role == UserRole.StudentAffairs) && u.IsActive && u.NotifyOnApplications)
                    .ToListAsync();

                var notifications = admins.Select(admin => new Notification
                {
                    UserId = admin.Id,
                    Title = "New Admission Application",
                    Message = $"New application received from {model.Name}. Application #: {newApp.ApplicationNumber}",
                    Type = "Info",
                    Category = "Application",
                    Priority = "Normal",
                    ActionUrl = "/Application/List",
                    CreatedAt = DateTime.UtcNow
                });
                
                foreach (var notification in notifications)
                {
                    await _notificationService.CreateNotificationAsync(notification);
                }

                await transaction.CommitAsync();

                // Broadcast real-time update
                await _hubContext.Clients.All.SendAsync("ReceiveDataUpdate", "Application", "Submitted");

                TempData["Success"] = $"Application submitted successfully! Application Number: {newApp.ApplicationNumber}";
                TempData["ApplicationNumber"] = newApp.ApplicationNumber;
                
                return RedirectToAction("ApplicationSuccess");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating application");
                
                // Expose specific error message for debugging purposes
                string errorMessage = "An error occurred while submitting the application. ";
                if (ex.InnerException != null) errorMessage += ex.InnerException.Message;
                else errorMessage += ex.Message;
                
                ModelState.AddModelError("", errorMessage);
                
                ViewBag.Departments = await _context.Departments.Where(d => d.IsActive).ToListAsync();
                ViewBag.DynamicFields = await _context.DynamicFields.Where(f => f.IsActive).OrderBy(f => f.Section).ThenBy(f => f.DisplayOrder).ToListAsync();
                return View(model);
            }
        }

        // GET: /Applicant/ApplicationSuccess
        public IActionResult ApplicationSuccess()
        {
            if (TempData["ApplicationNumber"] == null)
            {
                return RedirectToAction("Apply");
            }
            
            // Clear the generic "Success" message so it doesn't persist to other pages (like Admin Dashboard)
            if (TempData.ContainsKey("Success"))
            {
                TempData.Remove("Success");
            }
            
            return View();
        }

        // GET: /Applicant/Status
        [Authorize(Roles = "Applicant,Student")]
        public async Task<IActionResult> Status()
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var applicant = await _context.Applicants
                .Include(a => a.User)
                .Include(a => a.Applications)
                    .ThenInclude(app => app.PreferredDepartment)
                .FirstOrDefaultAsync(a => a.UserId == userId);

            if (applicant == null)
            {
                return RedirectToAction("Apply"); 
            }

            var application = applicant.Applications.OrderByDescending(a => a.ApplicationDate).FirstOrDefault();

            if (application == null)
            {
                return RedirectToAction("Apply");
            }
            
            // Get Notifications
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            ViewBag.Notifications = notifications;

            return View(application);
        }
    }
}
