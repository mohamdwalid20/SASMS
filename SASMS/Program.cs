using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using SASMS.Data;
using SASMS.Services;

namespace SASMS
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews(options => 
            {
                options.Filters.Add<SASMS.Filters.BreadcrumbFilter>();
            })
                .AddViewLocalization(Microsoft.AspNetCore.Mvc.Razor.LanguageViewLocationExpanderFormat.Suffix)
                .AddDataAnnotationsLocalization();

            builder.Services.AddSignalR();

            // Add Entity Framework
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
                    sqlOptions => sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null)));

          
            builder.Services.AddAuthentication("CookieAuth")
                .AddCookie("CookieAuth", options =>
                {
                    options.LoginPath = "/Account/Login";
                    options.LogoutPath = "/Account/Logout";
                    options.AccessDeniedPath = "/Account/AccessDenied";
                    options.ExpireTimeSpan = TimeSpan.FromDays(30);
                    options.SlidingExpiration = true;
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                    options.Cookie.SameSite = SameSiteMode.Lax;
                })
                .AddGoogle(options =>
                {
                    options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "";
                    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "";
                    options.CallbackPath = "/signin-google";
                    options.SaveTokens = true;
                    options.SignInScheme = "CookieAuth";
                });
            // Add Authorization
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminOnly", policy => policy.RequireClaim("UserType", "Admin", "StudentAffairs"));
                options.AddPolicy("SupervisorOnly", policy => policy.RequireClaim("UserType", "Supervisor", "Admin", "StudentAffairs"));
                options.AddPolicy("ActivityTeacherOnly", policy => policy.RequireClaim("UserType", "ActivityTeacher", "Supervisor", "Admin", "StudentAffairs"));
                options.AddPolicy("StaffOnly", policy => policy.RequireClaim("UserType", "Admin", "StudentAffairs", "Supervisor", "ActivityTeacher"));
                options.AddPolicy("StudentAffairsOnly", policy => policy.RequireClaim("UserType", "StudentAffairs", "Supervisor", "Admin"));
                options.AddPolicy("StudentOnly", policy => policy.RequireClaim("UserType", "Student"));
                options.AddPolicy("ApplicantOnly", policy => policy.RequireClaim("UserType", "Applicant"));
                options.AddPolicy("Authenticated", policy => policy.RequireAuthenticatedUser());
            });

            // Add Custom Services
            builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IEmailService, EmailService>();
            builder.Services.AddScoped<IBreadcrumbService, BreadcrumbService>();
            builder.Services.AddScoped<INotificationService, NotificationService>();
            builder.Services.AddScoped<IActivityLogService, ActivityLogService>();
            builder.Services.AddScoped<IBackupService, BackupService>();
builder.Services.AddScoped<IRiskDetectionService, RiskDetectionService>();
            builder.Services.AddHttpContextAccessor();

            // Add Localization
            builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
            builder.Services.Configure<RequestLocalizationOptions>(options =>
            {
                var supportedCultures = new[] { "en", "ar" };
                options.SetDefaultCulture("en")
                    .AddSupportedCultures(supportedCultures)
                    .AddSupportedUICultures(supportedCultures);

                // Ensure Cookie Request Culture Provider is FIRST and ONLY source of truth if set
                options.RequestCultureProviders.Clear();
                options.RequestCultureProviders.Add(new Microsoft.AspNetCore.Localization.CookieRequestCultureProvider());
                options.RequestCultureProviders.Add(new Microsoft.AspNetCore.Localization.AcceptLanguageHeaderRequestCultureProvider());
            });

            // Add Session
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            var app = builder.Build();

            // Seed Data or Fix Admin Password
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {

                    var context = services.GetRequiredService<SASMS.Data.ApplicationDbContext>();
                    var passwordHasher = services.GetRequiredService<SASMS.Services.IPasswordHasher>();
                    
                    // Ensure database is created
                    // context.Database.Migrate();

                    // Fix Admin Password
                    // Fix Admin Password or Create Admin if not exists
                    var adminUser = context.Users.FirstOrDefault(u => u.Email == "admin@sasms.edu");
                    var rawPassword = "Admin@123456";
                    var newHash = passwordHasher.HashPassword(rawPassword);

                    if (adminUser != null)
                    {
                        // Commented out to prevent resetting password on every restart
                        // adminUser.Password = newHash;
                        // context.SaveChanges();
                        // Console.WriteLine("✅ ADMIN PASSWORD FLUSHED AND UPDATED SUCCESSFULLY to 'Admin@123456'");
                    }
                    else
                    {
                        // Create Admin User
                        var newAdmin = new SASMS.Models.User
                        {
                            Name = "System Administrator",
                            Email = "admin@sasms.edu",
                            Password = newHash,
                            Role = SASMS.Models.UserRole.Admin,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow,
                            NationalId = "1000000000",
                            PhoneNumber = "01000000000",
                            Admin = new SASMS.Models.Admin 
                            { 
                                Position = "IT Manager",
                                HireDate = DateTime.UtcNow 
                            }
                        };
                        context.Users.Add(newAdmin);
                        context.SaveChanges();
                        Console.WriteLine("✅ ADMIN USER CREATED SUCCESSFULLY with password 'Admin@123456'");
                    }

                    // Seed Departments
                    if (!context.Departments.Any())
                    {
                        var softwareDept = new SASMS.Models.Department
                        {
                            Name = "Software Development",
                            Code = "SD",
                            Description = "Software Development Department",
                            HeadOfDepartment = "Eng. Ahmed Ezzat",
                            Capacity = 400,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        };

                        var maintenanceDept = new SASMS.Models.Department
                        {
                            Name = "Operational Maintenance",
                            Code = "OM",
                            Description = "System Maintenance Department",
                            HeadOfDepartment = "Eng. George Emmauel",
                            Capacity = 400,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        };

                        context.Departments.AddRange(softwareDept, maintenanceDept);
                        context.SaveChanges();
                        Console.WriteLine("✅ DEPARTMENTS SEEDED SUCCESSFULLY");
                    }

                    // Seed Academic Year
                    if (!context.AcademicYears.Any())
                    {
                        var academicYear = new SASMS.Models.AcademicYear
                        {
                            Year = "2025-2026",
                            StartDate = new DateTime(2025, 9, 1),
                            EndDate = new DateTime(2026, 6, 30),
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        };

                        context.AcademicYears.Add(academicYear);
                        context.SaveChanges();
                        Console.WriteLine("✅ ACADEMIC YEAR SEEDED SUCCESSFULLY");
                    }
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred while seeding the database.");
                }
            }

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            // Add Security Headers Middleware
            app.UseMiddleware<Middleware.SecurityHeadersMiddleware>();

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            // Custom Middleware to FORCE culture
            app.UseMiddleware<SASMS.Middleware.CultureMiddleware>();
            
            // Standard Localization (kept as backup for other services, but our middleware runs first logic)
            var localizationOptions = app.Services.GetService<Microsoft.Extensions.Options.IOptions<RequestLocalizationOptions>>().Value;
            app.UseRequestLocalization(localizationOptions);

            app.UseRouting();

            // Authentication & Authorization must be in this order
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseSession();

            app.MapHub<SASMS.Hubs.SASMSHub>("/sasmsHub");

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Land}/{action=Index}/{id?}");

            app.Run();

      
        }
    }
}
