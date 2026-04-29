using SASMS.Models;

namespace SASMS.Services
{
    public interface IAuthService
    {
        Task<User?> AuthenticateAsync(string email, string password);
      
        Task SignInAsync(HttpContext httpContext, User user);
        Task SignOutAsync(HttpContext httpContext);
        User? GetCurrentUser(HttpContext httpContext);
        Task<User?> GetUserByEmailAsync(string email);
    }
}
