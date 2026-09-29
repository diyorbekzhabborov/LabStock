using System;
using LabStock.Data;
using LabStock.Models;

namespace LabStock.Services
{
    public class AuditService
    {
        private static AuditService? _instance;
        public static AuditService Instance => _instance ??= new AuditService();

        public void Log(string action, string entityName, string? entityId, string details)
        {
            try
            {
                using var db = new AppDbContext();
                var currentUserId = AuthService.Instance.CurrentUser?.Id;
                db.AuditLogs.Add(new AuditLog
                {
                    Timestamp = DateTime.Now,
                    UserId = currentUserId,
                    Action = action,
                    EntityName = entityName,
                    EntityId = entityId,
                    Details = details
                });
                db.SaveChanges();
            }
            catch { }
        }
    }
}
