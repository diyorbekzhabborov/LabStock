using System;
using System.IO;
using LabStock.Data;
using Microsoft.Data.Sqlite;

namespace LabStock.Services
{
    public class BackupService
    {
        private static BackupService? _instance;
        public static BackupService Instance => _instance ??= new BackupService();

        public string GetDefaultBackupFolder()
        {
            var folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            return folder;
        }

        public string CreateBackup(string? targetDirectory = null)
        {
            var sourceDbPath = AppDbContext.DatabasePath;
            if (!File.Exists(sourceDbPath))
            {
                throw new FileNotFoundException("Файл базы данных не найден.");
            }

            var folder = targetDirectory ?? GetDefaultBackupFolder();
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            var backupFileName = $"LabStock_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.db";
            var backupFilePath = Path.Combine(folder, backupFileName);

            // Using SQLite online backup API to ensure clean snapshot even if open
            using (var srcConnection = new SqliteConnection($"Data Source={sourceDbPath}"))
            using (var destConnection = new SqliteConnection($"Data Source={backupFilePath}"))
            {
                srcConnection.Open();
                destConnection.Open();
                srcConnection.BackupDatabase(destConnection);
            }

            AuditService.Instance.Log("Резервное копирование", "Система", null, $"Создан бэкап: {backupFileName}");
            return backupFilePath;
        }

        public bool ValidateBackupFile(string backupFilePath)
        {
            if (!File.Exists(backupFilePath)) return false;

            try
            {
                using var conn = new SqliteConnection($"Data Source={backupFilePath}");
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "PRAGMA integrity_check;";
                var result = cmd.ExecuteScalar()?.ToString();
                return result == "ok";
            }
            catch
            {
                return false;
            }
        }

        public void RestoreBackup(string backupFilePath)
        {
            if (!ValidateBackupFile(backupFilePath))
            {
                throw new InvalidOperationException("Файл резервной копии поврежден или не является корректной базой данных LabStock.");
            }

            var currentDbPath = AppDbContext.DatabasePath;

            // First create an emergency safety backup of current db
            try
            {
                var safetyDir = Path.Combine(GetDefaultBackupFolder(), "Emergency");
                if (!Directory.Exists(safetyDir)) Directory.CreateDirectory(safetyDir);
                var safetyFile = Path.Combine(safetyDir, $"PreRestore_Safety_{DateTime.Now:yyyyMMdd_HHmmss}.db");
                File.Copy(currentDbPath, safetyFile, true);
            }
            catch { }

            // Clear connection pools to release lock
            SqliteConnection.ClearAllPools();

            // Overwrite database file
            File.Copy(backupFilePath, currentDbPath, overwrite: true);

            AuditService.Instance.Log("Восстановление", "Система", null, $"Восстановление базы данных из файла: {Path.GetFileName(backupFilePath)}");
        }
    }
}
