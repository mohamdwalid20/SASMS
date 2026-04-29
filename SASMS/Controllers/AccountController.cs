using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SASMS.Models;
using SASMS.Services;
using SASMS.Data;

namespace SASMS.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AccountController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly IActivityLogService _activityLogService;
        private readonly IEmailService _emailService;
        private readonly IPasswordHasher _passwordHasher;
        private readonly Microsoft.AspNetCore.SignalR.IHubContext<SASMS.Hubs.SASMSHub> _hubContext;

        public AccountController(IAuthService authService, ILogger<AccountController> logger, ApplicationDbContext context, Microsoft.AspNetCore.SignalR.IHubContext<SASMS.Hubs.SASMSHub> hubContext, INotificationService notificationService, IActivityLogService activityLogService, IEmailService emailService, IPasswordHasher passwordHasher)
        {
            _authService = authService;
            _logger = logger;
            _context = context;
            _hubContext = hubContext;
            _notificationService = notificationService;
            _activityLogService = activityLogService;
            _emailService = emailService;
            _passwordHasher = passwordHasher;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var roleClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
                if (!string.IsNullOrEmpty(roleClaim) && Enum.TryParse<UserRole>(roleClaim, out var role))
                {
                    return role switch
                    {
                        UserRole.Admin => RedirectToAction("Index", "Admin"),
                        UserRole.StudentAffairs => RedirectToAction("Index", "Admin"),
                        UserRole.Supervisor => RedirectToAction("Index", "Admin"),
                        UserRole.ActivityTeacher => RedirectToAction("Index", "Admin"),
                        UserRole.Student => RedirectToAction("Index", "StudentPortal"),
                        UserRole.Applicant => RedirectToAction("Status", "Applicant"),
                        _ => await SignOutAndReturnLogin()
                    };
                }
                return await SignOutAndReturnLogin();
            }

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        private async Task<IActionResult> SignOutAndReturnLogin()
        {
            await _authService.SignOutAsync(HttpContext);
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, string? returnUrl = null)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                TempData["Error"] = "Please enter your email and password";
                return View();
            }

            email = email.Trim();

            // Email validation
            var emailPattern = new System.Text.RegularExpressions.Regex(@"^[^\s@]+@[^\s@]+\.[^\s@]+$");
            if (!emailPattern.IsMatch(email))
            {
                TempData["Error"] = "Please enter a valid email address";
                return View();
            }

            Console.WriteLine($"[LOGIN ATTEMPT] Email: {email}, Password Length: {password?.Length}"); // DEBUG LOG
            var user = await _authService.AuthenticateAsync(email, password);

            if (user == null)
            {
                // Log failure for server-side debugging
                _logger.LogWarning("Failed login attempt for email: {Email}", email);
                await _activityLogService.LogActivityAsync(null, "Login Failed", "Auth", email, $"Failed login attempt for {email}");
                Console.WriteLine($"[LOGIN FAILED] Invalid credentials for {email}");

                TempData["Error"] = "Invalid email or password";
                return View();
            }

            Console.WriteLine($"[LOGIN SUCCESS] User {user.Email} logged in."); // DEBUG LOG

            if (!user.IsActive)
            {
                TempData["Error"] = "Your account is disabled. Please contact administration";
                return View();
            }

            await _authService.SignInAsync(HttpContext, user);
            _logger.LogInformation("User {UserId} logged in successfully", user.Id);
            await _activityLogService.LogActivityAsync(user.Id, "Login", "Auth", user.Id.ToString(), $"User {user.Email} logged in successfully");

            // Set Localization Cookie based on User Preference
            if (!string.IsNullOrEmpty(user.Language))
            {
                Response.Cookies.Append(
                    Microsoft.AspNetCore.Localization.CookieRequestCultureProvider.DefaultCookieName,
                    Microsoft.AspNetCore.Localization.CookieRequestCultureProvider.MakeCookieValue(new Microsoft.AspNetCore.Localization.RequestCulture(user.Language)),
                    new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
                );
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            // Redirect based on user role
            return user.Role switch
            {
                UserRole.Admin => RedirectToAction("Index", "Admin"),
                UserRole.StudentAffairs => RedirectToAction("Index", "Admin"),
                UserRole.Supervisor => RedirectToAction("Index", "Admin"),
                UserRole.ActivityTeacher => RedirectToAction("Index", "Admin"),
                UserRole.Student => RedirectToAction("Index", "StudentPortal"),
                UserRole.Applicant => RedirectToAction("Status", "Applicant"),
                _ => RedirectToAction("Index", "Land")
            };
        }


        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdStr, out int userId))
            {
                await _activityLogService.LogActivityAsync(userId, "Logout", "Auth", userId.ToString(), "User logged out");
            }

            await _authService.SignOutAsync(HttpContext);
            _logger.LogInformation("User logged out");
            return RedirectToAction("Index", "Land");
        }


        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword([FromForm] string email)
        {
            _logger.LogInformation("[ForgotPassword] Request received for email: {Email}", email);
            var isAjax = Request.Headers["X-Requested-With"].ToString().Equals("XMLHttpRequest", StringComparison.OrdinalIgnoreCase) || 
                         Request.Headers["Accept"].ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(email))
            {
                var msg = "Please enter your email address";
                if (isAjax) return Json(new { success = false, message = msg });
                TempData["Error"] = msg;
                return View();
            }

            var user = await _context.Users
                .Include(u => u.Student)
                .FirstOrDefaultAsync(u => u.Email == email.Trim());

            if (user == null)
            {
                var msg = "Email address not found in our records.";
                _logger.LogWarning("Password reset requested for non-existent email: {Email}", email);
                if (isAjax) return Json(new { success = false, message = msg });
                
                TempData["Error"] = msg;
                return View();
            }

            // Generate Random Password
            string randomPassword = Guid.NewGuid().ToString("N").Substring(0, 8);
            user.Password = _passwordHasher.HashPassword(randomPassword);
            
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            // Send Email
            var loginUrl = Url.Action("Login", "Account", null, Request.Scheme) ?? "";
            try 
            {
                await _emailService.SendPasswordResetEmailAsync(user.Email, user.Name, randomPassword, loginUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email to {Email}", user.Email);
                // We still update the password but notify the user carefully? 
                // Actually, if email fails, it's better to tell them or log it.
            }

            await _activityLogService.LogActivityAsync(user.Id, "Password Reset", "Auth", user.Id.ToString(), $"Password reset automatically for {user.Email}");

            _logger.LogInformation("[ForgotPassword] Automated reset completed for User: {UserName}", user.Name);

            if (isAjax) return Json(new { success = true });

            TempData["Success"] = "A new temporary password has been sent to your email address.";
            return RedirectToAction("Login");
        }

        [Authorize]
        public IActionResult Profile()
        {
            var user = _authService.GetCurrentUser(HttpContext);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            return View(user);
        }
    }
}
