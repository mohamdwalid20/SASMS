using System.Threading.Tasks;

namespace SASMS.Services
{
    public interface IEmailService
    {
        Task SendPasswordResetEmailAsync(string toEmail, string name, string newPassword, string loginUrl);
        Task SendAdmissionStatusEmailAsync(string toEmail, string name, bool isAccepted, string studentId = null, string className = null);
    }
}
