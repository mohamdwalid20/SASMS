using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SASMS.Data;
using SASMS.Models;

namespace SASMS.Services
{
    public class RiskDetectionService : IRiskDetectionService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RiskDetectionService> _logger;

        public RiskDetectionService(ApplicationDbContext context, ILogger<RiskDetectionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<int> DetectRisksAsync()
        {
            int newAlertsCount = 0;
            _logger.LogInformation("Starting risk detection scan at {Time}", DateTime.UtcNow);

            try
            {
                // 1. Detect High Absence (> 10 Absences)
                newAlertsCount += await DetectAttendanceRisksAsync();

                // 2. Detect Overdue Fees
                newAlertsCount += await DetectFinancialRisksAsync();

                // 3. Detect Incomplete Applications (Pending for > 3 days or missing docs)
                newAlertsCount += await DetectApplicationRisksAsync(); 

                // 4. Detect Long-standing Complaints (Open for > 7 days)
                newAlertsCount += await DetectComplaintRisksAsync();

                await _context.SaveChangesAsync();
                _logger.LogInformation("Risk detection completed. {Count} new alerts generated.", newAlertsCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during risk detection scan");
            }

            return newAlertsCount;
        }

        private async Task<int> DetectAttendanceRisksAsync()
        {
            int count = 0;
            var studentsWithHighAbsence = await _context.Students
                .Where(s => s.Status == "Active")
                .Select(s => new { 
                    StudentId = s.Id, 
                    Name = s.User.Name,
                    AbsenceCount = s.Attendances.Count(a => a.Status == "Absent") 
                })
                .Where(x => x.AbsenceCount > 10)
                .ToListAsync();

            foreach (var item in studentsWithHighAbsence)
            {
                bool exists = await _context.RiskAlerts.AnyAsync(r => r.StudentId == item.StudentId && r.Type == "Attendance" && !r.IsResolved);
                if (!exists)
                {
                    _context.RiskAlerts.Add(new RiskAlert
                    {
                        Type = "Attendance",
                        StudentId = item.StudentId,
                        Message = $"Student {item.Name} has very high absence count: {item.AbsenceCount} days.",
                        CreatedAt = DateTime.UtcNow
                    });
                    count++;
                }
            }
            return count;
        }

        private async Task<int> DetectFinancialRisksAsync()
        {
            int count = 0;
            var today = DateTime.UtcNow.Date;
            
            // Look for fees that are past due and not fully paid
            var overdueFees = await _context.Fees
                .Where(f => f.IsActive && f.DueDate < today)
                .ToListAsync();

            foreach (var fee in overdueFees)
            {
                // Find students who haven't paid this fee
                // Note: Simplified logic - assumes if no completed payment exists for this fee, it's a risk
                var studentsWhoDidntPay = await _context.Students
                    .Where(s => s.Status == "Active")
                    .Where(s => !s.Payments.Any(p => p.FeeId == fee.Id && p.Status == "Completed"))
                    .Select(s => new { s.Id, s.User.Name })
                    .ToListAsync();

                foreach (var student in studentsWhoDidntPay)
                {
                    bool exists = await _context.RiskAlerts.AnyAsync(r => r.StudentId == student.Id && r.Type == "Financial" && r.Message.Contains(fee.Name) && !r.IsResolved);
                    if (!exists)
                    {
                        _context.RiskAlerts.Add(new RiskAlert
                        {
                            Type = "Financial",
                            StudentId = student.Id,
                            Message = $"Student {student.Name} has overdue fee: {fee.Name} (Due: {fee.DueDate:yyyy-MM-dd}).",
                            CreatedAt = DateTime.UtcNow
                        });
                        count++;
                    }
                }
            }
            return count;
        }

        private async Task<int> DetectApplicationRisksAsync()
        {
            int count = 0;
            var problematicApps = await _context.Applications
                .Include(a => a.Applicant)
                    .ThenInclude(ap => ap.User)
                .Where(a => a.Status == "Pending" || a.Status == "UnderReview")
                .Where(a => a.ApplicationDate < DateTime.UtcNow.AddDays(-7))
                .ToListAsync();

            foreach (var app in problematicApps)
            {
                string reason = "Application stuck in review for > 7 days";
                bool exists = await _context.RiskAlerts.AnyAsync(r => r.Type == "Application" && r.Message.Contains(app.ApplicationNumber) && !r.IsResolved);
                
                if (!exists)
                {
                    _context.RiskAlerts.Add(new RiskAlert
                    {
                        Type = "Application",
                        Message = $"Application {app.ApplicationNumber} ({app.Applicant.User.Name}) requires attention: {reason}.",
                        CreatedAt = DateTime.UtcNow
                    });
                    count++;
                }
            }
            return count;
        }

        private async Task<int> DetectComplaintRisksAsync()
        {
            int count = 0;
            var thresholdDate = DateTime.UtcNow.AddDays(-7);
            
            var stagnantComplaints = await _context.Complaints
                .Include(c => c.Student)
                .ThenInclude(s => s.User)
                .Where(c => (c.Status == "Pending" || c.Status == "InProgress") && c.CreatedAt < thresholdDate)
                .ToListAsync();

            foreach (var complaint in stagnantComplaints)
            {
                bool exists = await _context.RiskAlerts.AnyAsync(r => r.Type == "Complaint" && r.Message.Contains(complaint.Title) && !r.IsResolved);
                if (!exists)
                {
                    _context.RiskAlerts.Add(new RiskAlert
                    {
                        Type = "Complaint",
                        StudentId = complaint.StudentId,
                        Message = $"Complaint '{complaint.Title}' from {complaint.Student.User.Name} has been open for more than 7 days.",
                        CreatedAt = DateTime.UtcNow
                    });
                    count++;
                }
            }
            return count;
        }

        public async Task<IEnumerable<RiskAlert>> GetUnresolvedAlertsAsync(string? type = null, string? searchTerm = null)
        {
            var query = _context.RiskAlerts
                .Include(r => r.Student)
                .ThenInclude(s => s.User)
                .Where(r => !r.IsResolved);

            if (!string.IsNullOrEmpty(type))
            {
                query = query.Where(r => r.Type == type);
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(r => r.Message.Contains(searchTerm) || 
                                       (r.Student != null && r.Student.User.Name.Contains(searchTerm)));
            }

            return await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
        }

        public async Task<bool> ResolveAlertAsync(int alertId, int resolvedById)
        {
            var alert = await _context.RiskAlerts.FindAsync(alertId);
            if (alert == null) return false;

            alert.IsResolved = true;
            alert.ResolvedAt = DateTime.UtcNow;
            alert.ResolvedById = resolvedById;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> HasActiveAlertAsync(int studentId)
        {
            return await _context.RiskAlerts.AnyAsync(r => r.StudentId == studentId && !r.IsResolved);
        }
    }
}
