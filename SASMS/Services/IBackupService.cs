using System.Collections.Generic;
using System.Threading.Tasks;
using SASMS.Models;

namespace SASMS.Services
{
    public interface IBackupService
    {
        Task<SystemBackup> CreateDatabaseBackupAsync(string createdBy);
        Task<SystemBackup> CreateFilesBackupAsync(string createdBy);
        Task<IEnumerable<SystemBackup>> GetBackupsAsync();
        Task<SystemBackup> GetBackupByIdAsync(int id);
        Task<bool> DeleteBackupAsync(int id);
    }
}
