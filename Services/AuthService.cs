using System;
using System.Linq;
using LabStock.Data;
using LabStock.Models;
using Microsoft.EntityFrameworkCore;

namespace LabStock.Services
{
    public class AuthService
    {
        private static AuthService? _instance;
        public static AuthService Instance => _instance ??= new AuthService();

        public User? CurrentUser { get; private set; }
        public bool IsAuthenticated => CurrentUser != null;

        public event Action? OnAuthStateChanged;

        public bool Login(string username, string password)
        {
            using var db = new AppDbContext();
            var user = db.Users.FirstOrDefault(u => u.Username.ToLower() == username.Trim().ToLower() && u.IsActive);
            if (user == null) return false;

            var hashed = DbInitializer.HashPassword(password);
            if (user.PasswordHash == hashed)
            {
                CurrentUser = user;
                OnAuthStateChanged?.Invoke();

                // Audit
                db.AuditLogs.Add(new AuditLog
                {
                    Timestamp = DateTime.Now,
                    UserId = user.Id,
                    Action = "Вход",
                    EntityName = "Пользователи",
                    EntityId = user.Id.ToString(),
                    Details = $"Успешный вход в систему пользователя {user.FullName} ({user.Role.ToFriendlyName()})"
                });
                db.SaveChanges();

                return true;
            }

            return false;
        }

        public void Logout()
        {
            if (CurrentUser != null)
            {
                try
                {
                    using var db = new AppDbContext();
                    db.AuditLogs.Add(new AuditLog
                    {
                        Timestamp = DateTime.Now,
                        UserId = CurrentUser.Id,
                        Action = "Выход",
                        EntityName = "Пользователи",
                        EntityId = CurrentUser.Id.ToString(),
                        Details = $"Выход из системы пользователя {CurrentUser.FullName}"
                    });
                    db.SaveChanges();
                }
                catch { }
            }

            CurrentUser = null;
            OnAuthStateChanged?.Invoke();
        }

        public bool CanManageUsers() => CurrentUser?.Role == UserRole.Admin;
        public bool CanManageSettings() => CurrentUser?.Role == UserRole.Admin;
        public bool CanPerformBackup() => CurrentUser?.Role == UserRole.Admin;
        public bool CanManageMaterials() => CurrentUser?.Role is UserRole.Admin or UserRole.HeadOfLab;
        public bool CanManageAnalyses() => CurrentUser?.Role is UserRole.Admin or UserRole.HeadOfLab;
        public bool CanRegisterReceipt() => CurrentUser?.Role is UserRole.Admin or UserRole.HeadOfLab;
        public bool CanPerformInventory() => CurrentUser?.Role is UserRole.Admin or UserRole.HeadOfLab;
        public bool CanPerformAnalysis() => CurrentUser?.Role is UserRole.Admin or UserRole.HeadOfLab or UserRole.LabAssistant;
        public bool CanPerformWriteoff() => CurrentUser?.Role is UserRole.Admin or UserRole.HeadOfLab or UserRole.LabAssistant;
        public bool CanViewReports() => CurrentUser != null;
    }
}
