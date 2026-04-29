using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SASMS.Data;
using SASMS.Models;

namespace SASMS.Services
{
    public class BackupService : IBackupService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<BackupService> _logger;
        private readonly string _backupRoot;

        public BackupService(
            ApplicationDbContext context, 
            IConfiguration configuration, 
            IWebHostEnvironment environment,
            ILogger<BackupService> logger)
        {
            _context = context;
            _configuration = configuration;
            _environment = environment;
            _logger = logger;
            _backupRoot = Path.Combine(_environment.ContentRootPath, "Backups");
            
            if (!Directory.Exists(_backupRoot))
            {
                Directory.CreateDirectory(_backupRoot);
            }
        }

        public async Task<SystemBackup> CreateDatabaseBackupAsync(string createdBy)
        {
            var dbName = "SASMS3"; // Based on appsettings.json
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var fileName = $"SASMS3_DB_{timestamp}.bak";
            
            // 1. Determine a 'Bridge Path' that BOTH SQL Server and the App can access
            // We've created C:\SASMS_Backup_Temp with 'Everyone' permissions as our primary bridge
            string primaryBridge = @"C:\SASMS_Backup_Temp";
            string sqlDefaultPath = "";
            
            try 
            {
                if (!Directory.Exists(primaryBridge))
                {
                    Directory.CreateDirectory(primaryBridge);
                }
                sqlDefaultPath = primaryBridge;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not access or create C:\\SASMS_Backup_Temp. Attempting SQL internal paths.");
                
                // Fallback: Try to get SQL Server's internal path if our bridge fails
                try 
                {
                    using (var command = _context.Database.GetDbConnection().CreateCommand())
                    {
                        command.CommandText = "SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS NVARCHAR(MAX))";
                        await _context.Database.OpenConnectionAsync();
                        sqlDefaultPath = (string)await command.ExecuteScalarAsync();
                    }
                }
                catch (Exception sqlEx)
                {
                    _logger.LogError(sqlEx, "Failed to determine any valid SQL backup path.");
                }
            }

            if (string.IsNullOrEmpty(sqlDefaultPath))
            {
                throw new Exception("Unable to determine a valid location for the SQL Backup. Please ensure C:\\SASMS_Backup_Temp exists and is accessible.");
            }

            var tempSqlFilePath = Path.Combine(sqlDefaultPath, fileName);
            var finalFilePath = Path.Combine(_backupRoot, fileName);

            try
            {
                // 2. Execute SQL Backup
                await _context.Database.ExecuteSqlRawAsync(
                    $"BACKUP DATABASE [{dbName}] TO DISK = '{tempSqlFilePath}' WITH FORMAT, MEDIANAME = 'SASMS_Backups', NAME = 'Full Backup of {dbName}';"
                );

                // 3. Move/Copy logic
                if (tempSqlFilePath != finalFilePath)
                {
                    if (File.Exists(tempSqlFilePath))
                    {
                        _logger.LogInformation("Backup successful at bridge. Moving {Src} to {Dest}", tempSqlFilePath, finalFilePath);
                        File.Copy(tempSqlFilePath, finalFilePath, true);
                        File.Delete(tempSqlFilePath);
                    }
                    else
                    {
                        throw new FileNotFoundException($"SQL Server reported success, but the file is missing from the bridge path '{tempSqlFilePath}'.", tempSqlFilePath);
                    }
                }

                if (!File.Exists(finalFilePath))
                {
                    throw new FileNotFoundException($"Verified file exists at bridge, but failed to transfer to final storage '{finalFilePath}'.", finalFilePath);
                }

                var fileInfo = new FileInfo(finalFilePath);
                var backup = new SystemBackup
                {
                    FileName = fileName,
                    Type = "Database",
                    SizeBytes = fileInfo.Length,
                    FilePath = finalFilePath,
                    CreatedBy = createdBy,
                    CreatedAt = DateTime.UtcNow,
                    IsManual = true
                };

                _context.SystemBackups.Add(backup);
                await _context.SaveChangesAsync();

                return backup;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Backup Protocol Failure. Attempted bridge path: {Path}", tempSqlFilePath);
                throw new Exception(ex.Message, ex);
            }
        }

        public async Task<SystemBackup> CreateFilesBackupAsync(string createdBy)
        {
            var uploadDir = Path.Combine(_environment.WebRootPath, "uploads");
            if (!Directory.Exists(uploadDir))
            {
                Directory.CreateDirectory(uploadDir);
            }

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var fileName = $"SASMS3_Files_{timestamp}.zip";
            var filePath = Path.Combine(_backupRoot, fileName);

            try
            {
                await Task.Run(() => ZipFile.CreateFromDirectory(uploadDir, filePath));

                var fileInfo = new FileInfo(filePath);
                var backup = new SystemBackup
                {
                    FileName = fileName,
                    Type = "Files",
                    SizeBytes = fileInfo.Length,
                    FilePath = filePath,
                    CreatedBy = createdBy,
                    CreatedAt = DateTime.UtcNow,
                    IsManual = true
                };

                _context.SystemBackups.Add(backup);
                await _context.SaveChangesAsync();

                return backup;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating files backup");
                throw;
            }
        }

        public async Task<IEnumerable<SystemBackup>> GetBackupsAsync()
        {
            return await _context.SystemBackups
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
        }

        public async Task<SystemBackup> GetBackupByIdAsync(int id)
        {
            return await _context.SystemBackups.FindAsync(id);
        }

        public async Task<bool> DeleteBackupAsync(int id)
        {
            var backup = await _context.SystemBackups.FindAsync(id);
            if (backup == null) return false;

            try
            {
                if (File.Exists(backup.FilePath))
                {
                    File.Delete(backup.FilePath);
                }

                _context.SystemBackups.Remove(backup);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting backup {BackupId}", id);
                return false;
            }
        }
    }
}
