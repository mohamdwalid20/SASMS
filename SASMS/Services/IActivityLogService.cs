using System.Threading.Tasks;

namespace SASMS.Services
{
    public interface IActivityLogService
    {
        Task LogActivityAsync(int? userId, string action, string entityName, string entityId, string details);
        Task LogActivityAsync(string action, string entityName, string entityId, string details);
    }
}
