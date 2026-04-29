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
    [Authorize(Policy = "AdminOnly")]
    public class AttendanceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AttendanceController> _logger;
        private readonly INotificationService _notificationService;
        private readonly Microsoft.AspNetCore.SignalR.IHubContext<SASMS.Hubs.SASMSHub> _hubContext;

        public AttendanceController(ApplicationDbContext context, ILogger<AttendanceController> logger, Microsoft.AspNetCore.SignalR.IHubContext<SASMS.Hubs.SASMSHub> hubContext, INotificationService notificationService)
        {
            _context = context;
            _logger = logger;
            _hubContext = hubContext;
            _notificationService = notificationService;
        }

        public IActionResult Index()
        {
            return View();
        }

        // GET: /Attendance/List
        public async Task<IActionResult> List(DateTime? selectedDate, string className = "")
        {
            var date = selectedDate ?? DateTime.Today;

            // Get students based on class filter
            var studentsQuery = _context.Students
                .Include(s => s.User)
                .AsQueryable();

            if (!string.IsNullOrEmpty(className))
            {
                studentsQuery = studentsQuery.Where(s => s.ClassName == className);
            }

            var students = await studentsQuery.ToListAsync();

            // Get attendance records for the selected date
            var attendanceRecordsToday = await _context.Attendances
                .Where(a => a.AttendanceDate.Date == date.Date)
                .ToListAsync();

            var attendanceRecords = students.Select(s => {
                var attendance = attendanceRecordsToday.FirstOrDefault(a => a.StudentId == s.Id);
                return new AttendanceRecordViewModel
                {
                    Id = attendance?.Id ?? 0,
                    StudentId = s.StudentId,
                    DatabaseStudentId = s.Id, // Need this for manual update
                    StudentName = s.User.Name,
                    ClassName = s.ClassName ?? "N/A",
                    ProfilePicturePath = s.ProfilePicturePath ?? s.User.ProfilePicturePath,
                    AttendanceDate = date,
                    CheckInTime = attendance?.CheckInTime.ToString(@"hh\:mm") ?? "N/A",
                    CheckOutTime = attendance?.CheckOutTime.HasValue == true ? attendance.CheckOutTime.Value.ToString(@"hh\:mm") : "N/A",
                    Status = attendance?.Status ?? "Absent", // Default to Absent if no record? Or leave empty?
                    Notes = attendance?.Notes
                };
            }).ToList();

            // Calculate statistics from the filtered record set
            var viewModel = new AttendanceViewModel
            {
                Records = attendanceRecords,
                SelectedDate = date,
                PresentToday = attendanceRecords.Count(r => r.Status == "Present"),
                AbsentToday = attendanceRecords.Count(r => r.Status == "Absent"),
                LateToday = attendanceRecords.Count(r => r.Status == "Late")
            };

            // Calculate attendance rate
            var totalStudents = viewModel.PresentToday + viewModel.AbsentToday + viewModel.LateToday;
            viewModel.AttendanceRate = totalStudents > 0 
                ? (decimal)(viewModel.PresentToday + viewModel.LateToday) / totalStudents * 100 
                : 0;

            // Use hardcoded classes matching StudentController
            ViewBag.Classes = new List<string> 
            { 
                "J1", "J2", "J3", "J4", 
                "W1", "W2", "W3", 
                "S1", "S2", "S3", "S4", "S5", "S6" 
            };
            
            ViewBag.SelectedClass = className;

            return View(viewModel);
        }
        [HttpPost]
        public async Task<IActionResult> UpdateAttendance(int studentId, DateTime date, string status, string? notes)
        {
            var attendance = await _context.Attendances
                .FirstOrDefaultAsync(a => a.StudentId == studentId && a.AttendanceDate.Date == date.Date);

            if (attendance != null)
            {
                attendance.Status = status;
                attendance.Notes = notes;
                attendance.UpdatedAt = DateTime.UtcNow;
                _context.Attendances.Update(attendance);
            }
            else
            {
                attendance = new Attendance
                {
                    StudentId = studentId,
                    AttendanceDate = date,
                    Status = status,
                    Notes = notes,
                    CheckInTime = TimeOnly.FromDateTime(DateTime.Now), // Default check-in time for manual entry
                    CreatedAt = DateTime.UtcNow
                };
                _context.Attendances.Add(attendance);
            }

            await _context.SaveChangesAsync();

            // Notify Student (if enabled)
            var studentObj = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.Id == studentId);
            if (studentObj != null && studentObj.User.NotifyOnAttendance && studentObj.User.IsActive)
            {
                await _notificationService.NotifyUserAsync(
                    studentObj.UserId,
                    "Attendance Status Updated",
                    $"Your attendance for {date:yyyy-MM-dd} has been marked as {status}.",
                    "Attendance",
                    "/StudentPortal/Attendance"
                );
            }

            // Broadcast real-time update
            await _hubContext.Clients.All.SendAsync("ReceiveDataUpdate", "Attendance", status);


            return Json(new { success = true });
        }
    }
}
