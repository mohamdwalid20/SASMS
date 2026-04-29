using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SASMS.Data;
using SASMS.Models;
using SASMS.ViewModels;
using SASMS.Services;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SASMS.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<StudentController> _logger;
        private readonly IPasswordHasher _passwordHasher;
        private readonly Microsoft.AspNetCore.SignalR.IHubContext<SASMS.Hubs.SASMSHub> _hubContext;

        public StudentController(ApplicationDbContext context, ILogger<StudentController> logger, IPasswordHasher passwordHasher, Microsoft.AspNetCore.SignalR.IHubContext<SASMS.Hubs.SASMSHub> hubContext)
        {
            _context = context;
            _logger = logger;
            _passwordHasher = passwordHasher;
            _hubContext = hubContext;
        }

        public IActionResult Index()
        {
            return View();
        }

        // GET: /Student/List
        public async Task<IActionResult> List(string search = "", string status = "", int? departmentId = null)
        {
            var studentsQuery = _context.Students
                .Include(s => s.User)
                .Include(s => s.Department)
                .AsQueryable();

            // Apply department filter
            if (departmentId.HasValue)
            {
                studentsQuery = studentsQuery.Where(s => s.DepartmentId == departmentId.Value);
                ViewBag.SelectedDepartmentId = departmentId.Value;
                var dept = await _context.Departments.FindAsync(departmentId.Value);
                ViewBag.DepartmentName = dept?.Name;
            }

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                studentsQuery = studentsQuery.Where(s => 
                    s.User.Name.Contains(search) || 
                    s.StudentId.Contains(search) ||
                    s.User.Email.Contains(search) ||
                    s.ParentPhone.Contains(search));
            }

            // Apply status filter
            if (!string.IsNullOrWhiteSpace(status))
            {
                studentsQuery = studentsQuery.Where(s => s.Status == status);
            }

            var students = await studentsQuery
                .Select(s => new StudentViewModel
                {
                    Id = s.Id,
                    StudentId = s.StudentId,
                    Name = s.User.Name,
                    Email = s.User.Email,
                    PhoneNumber = s.User.PhoneNumber,
                    Gender = s.Gender,
                    ClassName = s.ClassName ?? "N/A",
                    DepartmentName = s.Department.Name,
                    Status = s.Status,
                    GuardianName = s.ParentName,
                    GuardianPhone = s.ParentPhone,
                    ProfilePicturePath = s.ProfilePicturePath ?? s.User.ProfilePicturePath,
                    EnrollmentDate = s.EnrollmentDate
                })
                .OrderByDescending(s => s.EnrollmentDate)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.StatusFilter = status;
            
            return View(students);
        }

        // GET: /Student/Create
        public async Task<IActionResult> Create()
        {
            // Load dropdown data
            ViewBag.Departments = new SelectList(await _context.Departments.ToListAsync(), "Id", "Name");
            
            // Hardcoded classes: J (Year 1), W (Year 2), S (Year 3)
            var classes = new List<SelectListItem>
            {
                new SelectListItem { Value = "J1", Text = "J1 (Year 1)" },
                new SelectListItem { Value = "J2", Text = "J2 (Year 1)" },
                new SelectListItem { Value = "J3", Text = "J3 (Year 1)" },
                new SelectListItem { Value = "J4", Text = "J4 (Year 1)" },
                new SelectListItem { Value = "W1", Text = "W1 (Year 2)" },
                new SelectListItem { Value = "W2", Text = "W2 (Year 2)" },
                new SelectListItem { Value = "W3", Text = "W3 (Year 2)" },
                new SelectListItem { Value = "S1", Text = "S1 (Year 3)" },
                new SelectListItem { Value = "S2", Text = "S2 (Year 3)" },
                new SelectListItem { Value = "S3", Text = "S3 (Year 3)" },
                new SelectListItem { Value = "S4", Text = "S4 (Year 3)" },
                new SelectListItem { Value = "S5", Text = "S5 (Year 3)" },
                new SelectListItem { Value = "S6", Text = "S6 (Year 3)" }
            };
            ViewBag.Classes = classes;
            
            ViewBag.AcademicYears = new SelectList(await _context.AcademicYears.ToListAsync(), "Id", "Year");
            
            return View();
        }

        // POST: /Student/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            string name, string email, string nationalId, string phone, string password, string confirmPassword,
            string studentId, DateTime dateOfBirth, string gender, string address,
            int departmentId, string? className, int? academicYearId, DateTime? enrollmentDate, string status,
            string parentName, string parentPhone, string kinship, string emergencyContact, string emergencyPhone,
            IFormFile? profilePicture)
        {
            // Reload dropdown data in case of validation error
            ViewBag.Departments = new SelectList(await _context.Departments.ToListAsync(), "Id", "Name");
            
            // Hardcoded classes
            var classes = new List<SelectListItem>
            {
                new SelectListItem { Value = "J1", Text = "J1 (Year 1)" },
                new SelectListItem { Value = "J2", Text = "J2 (Year 1)" },
                new SelectListItem { Value = "J3", Text = "J3 (Year 1)" },
                new SelectListItem { Value = "J4", Text = "J4 (Year 1)" },
                new SelectListItem { Value = "W1", Text = "W1 (Year 2)" },
                new SelectListItem { Value = "W2", Text = "W2 (Year 2)" },
                new SelectListItem { Value = "W3", Text = "W3 (Year 2)" },
                new SelectListItem { Value = "S1", Text = "S1 (Year 3)" },
                new SelectListItem { Value = "S2", Text = "S2 (Year 3)" },
                new SelectListItem { Value = "S3", Text = "S3 (Year 3)" },
                new SelectListItem { Value = "S4", Text = "S4 (Year 3)" },
                new SelectListItem { Value = "S5", Text = "S5 (Year 3)" },
                new SelectListItem { Value = "S6", Text = "S6 (Year 3)" }
            };
            ViewBag.Classes = classes;
            
            ViewBag.AcademicYears = new SelectList(await _context.AcademicYears.ToListAsync(), "Id", "Year");

            // Validation
            if (string.IsNullOrEmpty(className))
            {
                ModelState.AddModelError("className", "Class is required.");
            }

            if (string.IsNullOrEmpty(password) || password.Length < 6)
            {
                ModelState.AddModelError("password", "Password must be at least 6 characters.");
            }

            if (password != confirmPassword)
            {
                ModelState.AddModelError("confirmPassword", "Passwords do not match.");
            }

            // Check for duplicates
            if (await _context.Users.AnyAsync(u => u.Email == email))
            {
                ModelState.AddModelError("email", "Email already exists.");
            }

            if (await _context.Users.AnyAsync(u => u.NationalId == nationalId))
            {
                ModelState.AddModelError("nationalId", "National ID already exists.");
            }

            if (await _context.Students.AnyAsync(s => s.StudentId == studentId))
            {
                ModelState.AddModelError("studentId", "Student ID already exists.");
            }

            if (!ModelState.IsValid)
            {
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
                    Role = UserRole.Student,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                // Handle profile picture upload
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

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Create Student
                var student = new Student
                {
                    UserId = user.Id,
                    StudentId = studentId,
                    DateOfBirth = DateOnly.FromDateTime(dateOfBirth),
                    Gender = gender,
                    Address = address,
                    DepartmentId = departmentId,
                    ClassName = className,
                    AcademicYearId = academicYearId,
                    EnrollmentDate = enrollmentDate ?? DateTime.UtcNow,
                    Status = status ?? "Active",
                    ParentName = parentName,
                    ParentPhone = parentPhone,
                    Kinship = kinship,
                    EmergencyContact = emergencyContact,
                    EmergencyPhone = emergencyPhone
                };

                _context.Students.Add(student);
                await _context.SaveChangesAsync();

                // Broadcast real-time update
                await _hubContext.Clients.All.SendAsync("ReceiveDataUpdate", "Student", "Created");

                await transaction.CommitAsync();
                TempData["Success"] = "Student created successfully.";
                return RedirectToAction(nameof(List));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating student");
                ModelState.AddModelError("", "An error occurred while creating the student.");
                return View();
            }
        }

        // GET: /Student/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var student = await _context.Students
                .Include(s => s.User)
                .Include(s => s.Department)
                .Include(s => s.AcademicYear)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (student == null)
            {
                return NotFound();
            }

            return View(student);
        }

        // GET: /Student/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var student = await _context.Students
                .Include(s => s.User)
                .Include(s => s.Department)
                .Include(s => s.AcademicYear)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (student == null)
            {
                return NotFound();
            }

            ViewBag.Departments = new SelectList(await _context.Departments.ToListAsync(), "Id", "Name", student.DepartmentId);
            ViewBag.AcademicYears = new SelectList(await _context.AcademicYears.ToListAsync(), "Id", "Year", student.AcademicYearId);
            
            // Hardcoded classes
            var classes = new List<SelectListItem>
            {
                new SelectListItem { Value = "J1", Text = "J1 (Year 1)" },
                new SelectListItem { Value = "J2", Text = "J2 (Year 1)" },
                new SelectListItem { Value = "J3", Text = "J3 (Year 1)" },
                new SelectListItem { Value = "J4", Text = "J4 (Year 1)" },
                new SelectListItem { Value = "W1", Text = "W1 (Year 2)" },
                new SelectListItem { Value = "W2", Text = "W2 (Year 2)" },
                new SelectListItem { Value = "W3", Text = "W3 (Year 2)" },
                new SelectListItem { Value = "S1", Text = "S1 (Year 3)" },
                new SelectListItem { Value = "S2", Text = "S2 (Year 3)" },
                new SelectListItem { Value = "S3", Text = "S3 (Year 3)" },
                new SelectListItem { Value = "S4", Text = "S4 (Year 3)" },
                new SelectListItem { Value = "S5", Text = "S5 (Year 3)" },
                new SelectListItem { Value = "S6", Text = "S6 (Year 3)" }
            };
            ViewBag.Classes = classes;

            return View(student);
        }

        // POST: /Student/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Student student, string name, string email, string? phone, string? password, IFormFile? profilePicture)
        {
            if (id != student.Id)
            {
                return NotFound();
            }

            var existingStudent = await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (existingStudent == null)
            {
                return NotFound();
            }

            // Update User info
            existingStudent.User.Name = name;
            existingStudent.User.Email = email;
            existingStudent.User.PhoneNumber = phone;
            
            if (!string.IsNullOrEmpty(password))
            {
                if (password.Length < 6)
                {
                    ModelState.AddModelError("password", "Password must be at least 6 characters.");
                    ViewBag.Departments = new SelectList(await _context.Departments.ToListAsync(), "Id", "Name", student.DepartmentId);
                    ViewBag.AcademicYears = new SelectList(await _context.AcademicYears.ToListAsync(), "Id", "Year", student.AcademicYearId);
                    
                    var classes = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>
                    {
                        new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = "J1", Text = "J1 (Year 1)" },
                        new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = "J2", Text = "J2 (Year 1)" },
                        new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = "J3", Text = "J3 (Year 1)" },
                        new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = "J4", Text = "J4 (Year 1)" },
                        new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = "W1", Text = "W1 (Year 2)" },
                        new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = "W2", Text = "W2 (Year 2)" },
                        new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = "W3", Text = "W3 (Year 2)" },
                        new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = "S1", Text = "S1 (Year 3)" },
                        new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = "S2", Text = "S2 (Year 3)" },
                        new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = "S3", Text = "S3 (Year 3)" },
                        new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = "S4", Text = "S4 (Year 3)" },
                        new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = "S5", Text = "S5 (Year 3)" },
                        new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = "S6", Text = "S6 (Year 3)" }
                    };
                    ViewBag.Classes = classes;
                    return View(student);
                }
                existingStudent.User.Password = _passwordHasher.HashPassword(password);
            }

            // Handle profile picture
            if (profilePicture != null && profilePicture.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(profilePicture.FileName);
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads", fileName);
                
                Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads"));
                
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await profilePicture.CopyToAsync(stream);
                }
                existingStudent.User.ProfilePicturePath = "/uploads/" + fileName;
                existingStudent.ProfilePicturePath = "/uploads/" + fileName;
            }

            // Update Student info
            existingStudent.StudentId = student.StudentId;
            existingStudent.DateOfBirth = student.DateOfBirth;
            existingStudent.Gender = student.Gender;
            existingStudent.Address = student.Address;
            existingStudent.DepartmentId = student.DepartmentId;
            existingStudent.ClassName = student.ClassName;
            existingStudent.AcademicYearId = student.AcademicYearId;
            existingStudent.Status = student.Status;
            existingStudent.ParentName = student.ParentName;
            existingStudent.ParentPhone = student.ParentPhone;
            existingStudent.Kinship = student.Kinship;
            existingStudent.EmergencyContact = student.EmergencyContact;
            existingStudent.EmergencyPhone = student.EmergencyPhone;

            try
            {
                _context.Update(existingStudent);
                await _context.SaveChangesAsync();
                
                // Broadcast real-time update
                await _hubContext.Clients.All.SendAsync("ReceiveDataUpdate", "Student", "Updated");
                
                TempData["Success"] = "Student updated successfully.";
                return RedirectToAction(nameof(List));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating student");
                ModelState.AddModelError("", "An error occurred while updating.");
                
                // Reload lists
                ViewBag.Departments = new SelectList(await _context.Departments.ToListAsync(), "Id", "Name", student.DepartmentId);
                ViewBag.AcademicYears = new SelectList(await _context.AcademicYears.ToListAsync(), "Id", "Year", student.AcademicYearId);
                
                var classes = new List<SelectListItem>
                {
                    new SelectListItem { Value = "J1", Text = "J1 (Year 1)" },
                    new SelectListItem { Value = "J2", Text = "J2 (Year 1)" },
                    new SelectListItem { Value = "J3", Text = "J3 (Year 1)" },
                    new SelectListItem { Value = "J4", Text = "J4 (Year 1)" },
                    new SelectListItem { Value = "W1", Text = "W1 (Year 2)" },
                    new SelectListItem { Value = "W2", Text = "W2 (Year 2)" },
                    new SelectListItem { Value = "W3", Text = "W3 (Year 2)" },
                    new SelectListItem { Value = "S1", Text = "S1 (Year 3)" },
                    new SelectListItem { Value = "S2", Text = "S2 (Year 3)" },
                    new SelectListItem { Value = "S3", Text = "S3 (Year 3)" },
                    new SelectListItem { Value = "S4", Text = "S4 (Year 3)" },
                    new SelectListItem { Value = "S5", Text = "S5 (Year 3)" },
                    new SelectListItem { Value = "S6", Text = "S6 (Year 3)" }
                };
                ViewBag.Classes = classes;
                
                return View(student);
            }
        }

        // POST: /Student/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var student = await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (student != null)
            {
                // Delete related messages manually (Received messages have NoAction on delete)
                var receivedMessages = await _context.Messages.Where(m => m.ReceiverId == student.UserId).ToListAsync();
                if (receivedMessages.Any())
                {
                    _context.Messages.RemoveRange(receivedMessages);
                }

                // Delete User (Cascade will delete Student and Sent Messages)
                _context.Users.Remove(student.User);
                await _context.SaveChangesAsync();
                
                // Broadcast real-time update
                await _hubContext.Clients.All.SendAsync("ReceiveDataUpdate", "Student", "Deleted");
                
                TempData["Success"] = "Student deleted successfully.";
            }
            else
            {
                TempData["Error"] = "Student not found.";
            }

            return RedirectToAction(nameof(List));
        }

        // POST: /Student/BulkDelete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkDelete(List<int> ids)
        {
            if (ids == null || !ids.Any())
            {
                TempData["Error"] = "No students selected.";
                return RedirectToAction(nameof(List));
            }

            var students = await _context.Students
                .Include(s => s.User)
                .Where(s => ids.Contains(s.Id))
                .ToListAsync();

            if (students.Any())
            {
                var userIds = students.Select(s => s.UserId).ToList();

                // Delete related messages manually
                var receivedMessages = await _context.Messages.Where(m => userIds.Contains(m.ReceiverId ?? 0)).ToListAsync();
                if (receivedMessages.Any())
                {
                    _context.Messages.RemoveRange(receivedMessages);
                }

                // Delete Users (Cascade will delete Students)
                var usersToDelete = students.Select(s => s.User).ToList();
                _context.Users.RemoveRange(usersToDelete);
                await _context.SaveChangesAsync();
                
                // Broadcast real-time update
                await _hubContext.Clients.All.SendAsync("ReceiveDataUpdate", "Student", "Bulk Deleted");
                
                TempData["Success"] = $"{students.Count} students deleted successfully.";
            }

            return RedirectToAction(nameof(List));
        }
    }
}
