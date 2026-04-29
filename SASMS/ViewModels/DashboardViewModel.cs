namespace SASMS.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalStudents { get; set; }
        public int ActiveStudents { get; set; }
        public int PendingApplications { get; set; }
        public int NewApplicationsThisMonth { get; set; }
        public decimal TotalFeesCollected { get; set; }
        public decimal OutstandingFees { get; set; }
        public int PendingComplaints { get; set; }
        public int ResolvedComplaintsToday { get; set; }
        public int TotalAttendanceToday { get; set; }
        public int AbsentToday { get; set; }
        public int LateToday { get; set; }
        
        // Added for admin dashboard
        public int TotalStudentsCount { get; set; }
        public int NewApplicationsCount { get; set; }
        public int TotalComplaintsCount { get; set; }
        public int PendingComplaintsCount { get; set; }
        
        // Chart data
        public List<MonthlyDataViewModel> MonthlyStudents { get; set; } = new();
        public List<MonthlyDataViewModel> MonthlyApplications { get; set; } = new();
        public List<MonthlyDataViewModel> MonthlyComplaints { get; set; } = new();
        public List<MonthlyDataViewModel> MonthlyParticipants { get; set; } = new();
        public List<MonthlyDataViewModel> MonthlySuggestions { get; set; } = new();
        
        // Staff-specific metrics
        public int MyManagedActivitiesCount { get; set; }
        public int ActiveManagedActivities { get; set; }
        public int TotalParticipantsInMyActivities { get; set; }
        public int NewParticipantsThisMonth { get; set; }
        public int TotalManagedStudentsCount { get; set; } // For Supervisors
        public int PendingSuggestionsCount { get; set; }
        
        // Recent activities
        public List<RecentActivityViewModel> RecentActivities { get; set; } = new();
    }
    
    public class MonthlyStudentData
    {
        public string Month { get; set; }
        public int Count { get; set; }
    }

    public class MonthlyDataViewModel
    {
        public string Month { get; set; }
        public int Count { get; set; }
    }
    
    public class RecentActivityViewModel
    {
        public string Type { get; set; }
        public string Description { get; set; }
        public DateTime Date { get; set; }
        public string BadgeColor { get; set; }
        public string Icon { get; set; }
    }
}
