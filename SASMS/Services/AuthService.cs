using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using SASMS.Data;
using SASMS.Models;

namespace SASMS.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher _passwordHasher;

        public AuthService(ApplicationDbContext context, IPasswordHasher passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        public async Task<User?> AuthenticateAsync(string email, string password)
        {
            Console.WriteLine($"[AUTH] AuthenticateAsync called");
            Console.WriteLine($"[AUTH] Email: '{email}'");
            Console.WriteLine($"[AUTH] Password length: {password?.Length ?? 0}");
            
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                Console.WriteLine($"[AUTH] FAILED: Email or password is null/empty");
                return null;
            }

            // Load user with related role data based on their role
            var user = await _context.Users
                .Include(u => u.Admin)
                .Include(u => u.Student)
                .Include(u => u.StudentAffairs)
                .Include(u => u.Applicant)
                .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);

            if (user == null)
            {
                Console.WriteLine($"[AUTH] FAILED: User not found or not active");
                return null;
            }

            Console.WriteLine($"[AUTH] User found: {user.Name} (ID: {user.Id})");
            Console.WriteLine($"[AUTH] Stored password hash: {user.Password?.Substring(0, 20)}...");
            Console.WriteLine($"[AUTH] Verifying password...");
            
            bool passwordValid = _passwordHasher.VerifyPassword(password, user.Password);
            Console.WriteLine($"[AUTH] Password verification result: {passwordValid}");
            
            if (!passwordValid)
            {
                Console.WriteLine($"[AUTH] FAILED: Password verification failed");
                return null;
            }

            Console.WriteLine($"[AUTH] SUCCESS: User authenticated");
            return user;
        }

     
        public async Task SignInAsync(HttpContext httpContext, User user)
        {
            var claims = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, user.Id.ToString()),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, user.Name),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, user.Email),
                new System.Security.Claims.Claim("UserType", user.Role.ToString()), // Keep as "UserType" for backwards compatibility
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, user.Role.ToString()),
                new System.Security.Claims.Claim("NationalId", user.NationalId),
                new System.Security.Claims.Claim("ProfilePicture", user.ProfilePicturePath ?? ""),
                new System.Security.Claims.Claim("IsDarkMode", user.IsDarkMode.ToString()),
                new System.Security.Claims.Claim("Language", user.Language ?? "en")
            };

            // Add specific claims based on user role
            if (user.Role == UserRole.Student && user.Student != null)
            {
                claims.Add(new System.Security.Claims.Claim("StudentId", user.Student.StudentId ?? ""));
                claims.Add(new System.Security.Claims.Claim("DepartmentId", user.Student.DepartmentId.ToString()));
            }
            else if (user.Role == UserRole.Admin && user.Admin != null)
            {
                claims.Add(new System.Security.Claims.Claim("Position", user.Admin.Position ?? ""));
            }
            else if ((user.Role == UserRole.StudentAffairs || user.Role == UserRole.Supervisor || user.Role == UserRole.ActivityTeacher) && user.StudentAffairs != null)
            {
                claims.Add(new System.Security.Claims.Claim("Position", user.StudentAffairs.Position ?? ""));
            }
            else if (user.Role == UserRole.Applicant && user.Applicant != null)
            {
                claims.Add(new System.Security.Claims.Claim("PreferredDepartmentId", user.Applicant.PreferredDepartmentId.ToString()));
            }

            var claimsIdentity = new System.Security.Claims.ClaimsIdentity(claims, "CookieAuth");
            var claimsPrincipal = new System.Security.Claims.ClaimsPrincipal(claimsIdentity);

            await httpContext.SignInAsync("CookieAuth", claimsPrincipal, new Microsoft.AspNetCore.Authentication.AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
            });
        }

        public async Task SignOutAsync(HttpContext httpContext)
        {
            await httpContext.SignOutAsync("CookieAuth");
        }

        public User? GetCurrentUser(HttpContext httpContext)
        {
            var userIdClaim = httpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return null;

            // Load user with related role data
            return _context.Users
                .Include(u => u.Admin)
                .Include(u => u.Student)
                .Include(u => u.StudentAffairs)
                .Include(u => u.Applicant)
                .FirstOrDefault(u => u.Id == userId);
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return null;

            return await _context.Users
                .Include(u => u.Admin)
                .Include(u => u.Student)
                .Include(u => u.StudentAffairs)
                .Include(u => u.Applicant)
                .FirstOrDefaultAsync(u => u.Email == email);
        }

       
    }
}
