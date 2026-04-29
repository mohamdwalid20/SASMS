using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SASMS.Models;

namespace SASMS.Controllers
{
    public class LandController : Controller
    {
        [AllowAnonymous]
        public IActionResult Index()
        {
            return View();
        }

        [Authorize]
        public IActionResult Dashboard()
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
                    _ => RedirectToAction("Index", "Land")
                };
            }
            return RedirectToAction("Index", "Land");
        }
    }
}
