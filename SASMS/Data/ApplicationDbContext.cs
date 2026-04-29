using Microsoft.EntityFrameworkCore;
using SASMS.Models;
using SASMS.Services;

namespace SASMS.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // User Tables
        public DbSet<User> Users { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Applicant> Applicants { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<StudentAffairs> StudentAffairs { get; set; }

        // Admission System
        public DbSet<Application> Applications { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<DynamicField> DynamicFields { get; set; }
        public DbSet<DynamicFieldValue> DynamicFieldValues { get; set; }

        // Student Information
        public DbSet<Department> Departments { get; set; }
        public DbSet<Class> Classes { get; set; }

        // Attendance & Departure
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<Departure> Departures { get; set; }

        // Fees Management
        public DbSet<Fee> Fees { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Receipt> Receipts { get; set; }

        // Complaints & Suggestions
        public DbSet<Complaint> Complaints { get; set; }
        public DbSet<ComplaintAttachment> ComplaintAttachments { get; set; }
        public DbSet<Suggestion> Suggestions { get; set; }

        // Activities
        public DbSet<Activity> Activities { get; set; }
        public DbSet<ActivityParticipation> ActivityParticipations { get; set; }

        // Messaging & Notifications
        public DbSet<Message> Messages { get; set; }
        public DbSet<MessageAttachment> MessageAttachments { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        // Academic Year & Settings
        public DbSet<AcademicYear> AcademicYears { get; set; }
        public DbSet<StudentScore> StudentScores { get; set; }
        public DbSet<SystemSettings> SystemSettings { get; set; }
        
        // Government Fees Tracking
        public DbSet<GovernmentFee> GovernmentFees { get; set; }
        public DbSet<FeeSubmission> FeeSubmissions { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }
        public DbSet<SystemBackup> SystemBackups { get; set; }
        public DbSet<RiskAlert> RiskAlerts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<SystemBackup>()
                .Property(b => b.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<RiskAlert>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Student)
                    .WithMany()
                    .HasForeignKey(e => e.StudentId)
                    .OnDelete(DeleteBehavior.NoAction);
                
                entity.HasOne(e => e.ResolvedBy)
                    .WithMany()
                    .HasForeignKey(e => e.ResolvedById)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // ========================================
            // Configure User Model (No TPH - standalone authentication table)
            // ========================================
            
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);
                
                // Indexes
                entity.HasIndex(u => u.Email).IsUnique();
                entity.HasIndex(u => u.NationalId).IsUnique();
                
                // Properties
                entity.Property(u => u.Name).IsRequired().HasMaxLength(200);
                entity.Property(u => u.Email).IsRequired().HasMaxLength(256);
                entity.Property(u => u.Password).IsRequired().HasMaxLength(500);
                entity.Property(u => u.NationalId).IsRequired().HasMaxLength(50);
                entity.Property(u => u.PhoneNumber).HasMaxLength(20);
                entity.Property(u => u.Role).IsRequired();
                
                // One-to-one relationships with role tables
                entity.HasOne(u => u.Admin)
                    .WithOne(a => a.User)
                    .HasForeignKey<Admin>(a => a.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                entity.HasOne(u => u.Student)
                    .WithOne(s => s.User)
                    .HasForeignKey<Student>(s => s.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                entity.HasOne(u => u.StudentAffairs)
                    .WithOne(sa => sa.User)
                    .HasForeignKey<StudentAffairs>(sa => sa.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                entity.HasOne(u => u.Applicant)
                    .WithOne(ap => ap.User)
                    .HasForeignKey<Applicant>(ap => ap.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ========================================
            // Configure Admin Model
            // ========================================
            
            modelBuilder.Entity<Admin>(entity =>
            {
                entity.HasKey(a => a.Id);
                entity.HasIndex(a => a.UserId).IsUnique();
                entity.Property(a => a.Position).HasMaxLength(100);
            });

            // ========================================
            // Configure Student Model
            // ========================================
            
            modelBuilder.Entity<Student>(entity =>
            {
                entity.HasKey(s => s.Id);
                entity.HasIndex(s => s.UserId).IsUnique();
                entity.HasIndex(s => s.StudentId).IsUnique();
                
                entity.Property(s => s.StudentId).IsRequired().HasMaxLength(50);
                entity.Property(s => s.Gender).HasMaxLength(20);
                entity.Property(s => s.Address).HasMaxLength(500);
                entity.Property(s => s.Status).HasMaxLength(50);
                
                entity.HasOne(s => s.Department)
                    .WithMany(d => d.Students)
                    .HasForeignKey(s => s.DepartmentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ========================================
            // Configure StudentAffairs Model
            // ========================================
            
            modelBuilder.Entity<StudentAffairs>(entity =>
            {
                entity.HasKey(sa => sa.Id);
                entity.HasIndex(sa => sa.UserId).IsUnique();
                entity.Property(sa => sa.Position).HasMaxLength(100);
            });

            // ========================================
            // Configure Applicant Model
            // ========================================
            
            modelBuilder.Entity<Applicant>(entity =>
            {
                entity.HasKey(ap => ap.Id);
                entity.HasIndex(ap => ap.UserId).IsUnique();
                
                entity.Property(ap => ap.Gender).HasMaxLength(20);
                entity.Property(ap => ap.Address).HasMaxLength(500);
                
                entity.HasOne(ap => ap.PreferredDepartment)
                    .WithMany(d => d.Applicants)
                    .HasForeignKey(ap => ap.PreferredDepartmentId)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                entity.HasOne(ap => ap.User)
                    .WithOne(u => u.Applicant)
                    .HasForeignKey<Applicant>(ap => ap.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ========================================
            // Configure Application Model
            // ========================================
            
            modelBuilder.Entity<Application>(entity =>
            {
                entity.HasIndex(a => a.ApplicationNumber).IsUnique();
                
                entity.HasOne(a => a.Applicant)
                    .WithMany(ap => ap.Applications)
                    .HasForeignKey(a => a.ApplicantId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ========================================
            // Configure Class Model
            // ========================================
            
            modelBuilder.Entity<Class>(entity =>
            {
                entity.HasOne(c => c.Department)
                    .WithMany(d => d.Classes)
                    .HasForeignKey(c => c.DepartmentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ========================================
            // Configure Payment & Receipt Models
            // ========================================
            
            modelBuilder.Entity<Payment>(entity =>
            {
                entity.HasIndex(p => p.PaymentNumber).IsUnique();
            });

            modelBuilder.Entity<Receipt>(entity =>
            {
                entity.HasIndex(r => r.ReceiptNumber).IsUnique();
            });

            // ========================================
            // Configure Government Fees Tracking
            // ========================================
            
            modelBuilder.Entity<GovernmentFee>(entity =>
            {
                entity.HasKey(gf => gf.Id);
                entity.Property(gf => gf.Name).IsRequired().HasMaxLength(200);
                entity.HasOne(gf => gf.AcademicYear)
                    .WithMany()
                    .HasForeignKey(gf => gf.AcademicYearId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<FeeSubmission>(entity =>
            {
                entity.HasKey(fs => fs.Id);
                entity.Property(fs => fs.ReceiptNumber).IsRequired().HasMaxLength(100);
                entity.Property(fs => fs.Status).IsRequired().HasMaxLength(50);
                
                entity.HasOne(fs => fs.Student)
                    .WithMany()
                    .HasForeignKey(fs => fs.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                entity.HasOne(fs => fs.GovernmentFee)
                    .WithMany(gf => gf.Submissions)
                    .HasForeignKey(fs => fs.GovernmentFeeId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(fs => fs.AcademicYear)
                    .WithMany()
                    .HasForeignKey(fs => fs.AcademicYearId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(fs => fs.ProcessedBy)
                    .WithMany()
                    .HasForeignKey(fs => fs.ProcessedById)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ========================================
            // Configure Messaging & Notifications (Cascade Delete)
            // ========================================
            
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasOne(n => n.User)
                    .WithMany()
                    .HasForeignKey(n => n.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Message>(entity =>
            {
                entity.HasOne(m => m.Sender)
                    .WithMany()
                    .HasForeignKey(m => m.SenderId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(m => m.Receiver)
                    .WithMany()
                    .HasForeignKey(m => m.ReceiverId)
                    .OnDelete(DeleteBehavior.NoAction); // Avoid multiple cascade paths
            });

            modelBuilder.Entity<Activity>(entity =>
            {
                entity.HasOne(a => a.ManagedBy)
                    .WithMany()
                    .HasForeignKey(a => a.ManagedById)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(a => a.CreatedBy)
                    .WithMany()
                    .HasForeignKey(a => a.CreatedById)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ========================================
            // SEED DATA
            // ========================================
            
            // Note: Seeding is handled via migrations. 
            // If data already exists, HasData may cause PK violations.
        }
    }
}
