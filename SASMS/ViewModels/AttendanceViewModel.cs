namespace SASMS.ViewModels
{
    public class AttendanceViewModel
    {
        // Statistics
        public int PresentToday { get; set; }
        public int AbsentToday { get; set; }
        public int LateToday { get; set; }
        public decimal AttendanceRate { get; set; }
        
        // Records
        public List<AttendanceRecordViewModel> Records { get; set; } = new();
        
        // Filters
        public DateTime SelectedDate { get; set; }
        public int? SelectedClassId { get; set; }
    }
    
    public class AttendanceRecordViewModel
    {
        public int Id { get; set; }
        public string StudentId { get; set; }
        public int DatabaseStudentId { get; set; }
        public string StudentName { get; set; }
        public string ClassName { get; set; }
        public string ProfilePicturePath { get; set; }
        public DateTime AttendanceDate { get; set; }
        public string CheckInTime { get; set; }
        public string CheckOutTime { get; set; }
        public string Status { get; set; } // Present, Absent, Late, Excused
        public string Notes { get; set; }
    }
}
