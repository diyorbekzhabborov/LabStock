using System;
using System.Collections.Generic;
using System.Linq;
using LabStock.Data;
using LabStock.Models;
using Microsoft.EntityFrameworkCore;

namespace LabStock.Services
{
    public class NotificationService
    {
        private static NotificationService? _instance;
        public static NotificationService Instance => _instance ??= new NotificationService();

        public event Action? OnNotificationsUpdated;

        public int GetUnreadCount()
        {
            using var db = new AppDbContext();
            return db.Notifications.Count(n => !n.IsRead);
        }

        public List<Notification> GetActiveNotifications()
        {
            using var db = new AppDbContext();
            return db.Notifications
                .Include(n => n.Material)
                .Include(n => n.Lot)
                .OrderByDescending(n => n.CreatedAt)
                .ToList();
        }

        public void MarkAllAsRead()
        {
            using var db = new AppDbContext();
            var unread = db.Notifications.Where(n => !n.IsRead).ToList();
            foreach (var item in unread)
            {
                item.IsRead = true;
            }
            db.SaveChanges();
            OnNotificationsUpdated?.Invoke();
        }

        public void MarkAsRead(int notificationId)
        {
            using var db = new AppDbContext();
            var notif = db.Notifications.Find(notificationId);
            if (notif != null && !notif.IsRead)
            {
                notif.IsRead = true;
                db.SaveChanges();
                OnNotificationsUpdated?.Invoke();
            }
        }

        public void RefreshNotifications()
        {
            using var db = new AppDbContext();

            int expiryDays = 30;
            var setting = db.Settings.FirstOrDefault(s => s.Key == "ExpiryAlertDays");
            if (setting != null && int.TryParse(setting.Value, out var days))
            {
                expiryDays = days;
            }

            var now = DateTime.Today;
            var materials = db.Materials
                .Include(m => m.Lots)
                .Include(m => m.Unit)
                .Where(m => m.IsActive)
                .ToList();

            var existingRecent = db.Notifications
                .Where(n => n.CreatedAt >= DateTime.Now.AddHours(-12) && !n.IsRead)
                .ToList();

            var newNotifications = new List<Notification>();

            foreach (var mat in materials)
            {
                var activeLots = mat.Lots.Where(l => !l.IsArchived).ToList();
                decimal totalStock = activeLots.Sum(l => l.CurrentQuantity);

                // 1. Out of stock
                if (totalStock <= 0)
                {
                    bool exists = existingRecent.Any(n => n.MaterialId == mat.Id && n.Type == NotificationType.OutOfStock);
                    if (!exists)
                    {
                        newNotifications.Add(new Notification
                        {
                            Title = "Отсутствует на складе!",
                            Message = $"Расходный материал «{mat.Name}» полностью закончился (остаток 0 {mat.Unit?.ShortName}).",
                            Type = NotificationType.OutOfStock,
                            Severity = "Danger",
                            MaterialId = mat.Id,
                            CreatedAt = DateTime.Now
                        });
                    }
                }
                // 2. Critical stock (<= half of min)
                else if (totalStock <= (mat.MinQuantity / 2))
                {
                    bool exists = existingRecent.Any(n => n.MaterialId == mat.Id && n.Type == NotificationType.CriticalStock);
                    if (!exists)
                    {
                        newNotifications.Add(new Notification
                        {
                            Title = "Критический остаток!",
                            Message = $"Критический запас «{mat.Name}»: всего {totalStock:0.##} {mat.Unit?.ShortName} при минимуме {mat.MinQuantity:0.##}.",
                            Type = NotificationType.CriticalStock,
                            Severity = "Danger",
                            MaterialId = mat.Id,
                            CreatedAt = DateTime.Now
                        });
                    }
                }
                // 3. Low stock (<= min)
                else if (totalStock <= mat.MinQuantity)
                {
                    bool exists = existingRecent.Any(n => n.MaterialId == mat.Id && (n.Type == NotificationType.LowStock || n.Type == NotificationType.CriticalStock));
                    if (!exists)
                    {
                        newNotifications.Add(new Notification
                        {
                            Title = "Низкий остаток",
                            Message = $"Запас «{mat.Name}» ниже нормы: {totalStock:0.##} {mat.Unit?.ShortName} (порог {mat.MinQuantity:0.##}).",
                            Type = NotificationType.LowStock,
                            Severity = "Warning",
                            MaterialId = mat.Id,
                            CreatedAt = DateTime.Now
                        });
                    }
                }

                // Check lots for expiry
                foreach (var lot in activeLots.Where(l => l.CurrentQuantity > 0))
                {
                    if (lot.ExpiryDate.Date < now)
                    {
                        bool exists = existingRecent.Any(n => n.LotId == lot.Id && n.Type == NotificationType.Expired);
                        if (!exists)
                        {
                            newNotifications.Add(new Notification
                            {
                                Title = "Материал просрочен!",
                                Message = $"Партия {lot.LotNumber} материала «{mat.Name}» просрочена ({lot.ExpiryDate:dd.MM.yyyy}). Требуется списание!",
                                Type = NotificationType.Expired,
                                Severity = "Danger",
                                MaterialId = mat.Id,
                                LotId = lot.Id,
                                CreatedAt = DateTime.Now
                            });
                        }
                    }
                    else if ((lot.ExpiryDate.Date - now).TotalDays <= expiryDays)
                    {
                        int daysLeft = (int)(lot.ExpiryDate.Date - now).TotalDays;
                        bool exists = existingRecent.Any(n => n.LotId == lot.Id && n.Type == NotificationType.ExpiringSoon);
                        if (!exists)
                        {
                            newNotifications.Add(new Notification
                            {
                                Title = "Истекает срок годности",
                                Message = $"Партия {lot.LotNumber} материала «{mat.Name}» истекает через {daysLeft} дн. ({lot.ExpiryDate:dd.MM.yyyy}).",
                                Type = NotificationType.ExpiringSoon,
                                Severity = "Warning",
                                MaterialId = mat.Id,
                                LotId = lot.Id,
                                CreatedAt = DateTime.Now
                            });
                        }
                    }
                }
            }

            if (newNotifications.Any())
            {
                db.Notifications.AddRange(newNotifications);
                db.SaveChanges();
            }

            OnNotificationsUpdated?.Invoke();
        }
    }
}
