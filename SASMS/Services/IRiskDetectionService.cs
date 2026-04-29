using System.Collections.Generic;
using System.Threading.Tasks;
using SASMS.Models;

namespace SASMS.Services
{
    public interface IRiskDetectionService
    {
        /// <summary>
        /// Runs the rule-based detection logic to identify system risks
        /// </summary>
        Task<int> DetectRisksAsync();

        /// <summary>
        /// Retrieves unresolved risk alerts with optional filtering
        /// </summary>
        Task<IEnumerable<RiskAlert>> GetUnresolvedAlertsAsync(string? type = null, string? searchTerm = null);

        /// <summary>
        /// Marks a specific alert as resolved
        /// </summary>
        Task<bool> ResolveAlertAsync(int alertId, int resolvedById);

        /// <summary>
        /// Checks if a specific student has any active risk alerts
        /// </summary>
        Task<bool> HasActiveAlertAsync(int studentId);
    }
}
