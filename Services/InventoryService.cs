using System;
using System.Collections.Generic;
using System.Linq;
using LabStock.Data;
using LabStock.Models;
using Microsoft.EntityFrameworkCore;

namespace LabStock.Services
{
    public class DashboardStatsDto
    {
        public int TotalMaterialsCount { get; set; }
        public decimal TotalStockSum { get; set; }
        public int LowStockCount { get; set; }
        public int ExpiringLotsCount { get; set; }
        public decimal UsedTodayCount { get; set; }
        public int AnalysesTodayCount { get; set; }
        public List<Material> CriticalMaterials { get; set; } = new();
        public List<StockMovement> RecentMovements { get; set; } = new();
        public List<DailyTrendDto> DailyTrends { get; set; } = new();
    }

    public class DailyTrendDto
    {
        public string DateLabel { get; set; } = string.Empty;
        public int AnalysesCount { get; set; }
        public decimal ItemsUsed { get; set; }
    }

    public class AnalysisMaterialPreviewDto
    {
        public int MaterialId { get; set; }
        public string MaterialName { get; set; } = string.Empty;
        public string UnitName { get; set; } = string.Empty;
        public decimal NormPerAnalysis { get; set; }
        public decimal TotalRequired { get; set; }
        public decimal AvailableStock { get; set; }
        public bool HasShortage => AvailableStock < TotalRequired;
        public decimal ShortageAmount => HasShortage ? (TotalRequired - AvailableStock) : 0;
        public string StatusText => HasShortage ? $"Не хватает: {ShortageAmount:0.##} {UnitName}" : "В наличии";
    }

    public class InventoryService
    {
        private static InventoryService? _instance;
        public static InventoryService Instance => _instance ??= new InventoryService();

        public DashboardStatsDto GetDashboardStats()
        {
            using var db = new AppDbContext();
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            int expiryDays = 30;
            var setting = db.Settings.FirstOrDefault(s => s.Key == "ExpiryAlertDays");
            if (setting != null && int.TryParse(setting.Value, out var days))
            {
                expiryDays = days;
            }

            var materials = db.Materials
                .Include(m => m.Lots)
                .Include(m => m.Unit)
                .Include(m => m.Category)
                .Where(m => m.IsActive)
                .ToList();

            foreach (var mat in materials)
            {
                mat.TotalStock = mat.Lots.Where(l => !l.IsArchived).Sum(l => l.CurrentQuantity);
            }

            var critical = materials
                .Where(m => m.Status is StockStatus.Low or StockStatus.OutOfStock or StockStatus.Attention)
                .OrderBy(m => m.TotalStock / (m.MinQuantity > 0 ? m.MinQuantity : 1))
                .Take(7)
                .ToList();

            var recentMovements = db.StockMovements
                .Include(sm => sm.Material)
                .Include(sm => sm.Material!.Unit)
                .Include(sm => sm.User)
                .OrderByDescending(sm => sm.Date)
                .Take(8)
                .ToList();

            // Daily trends for last 7 days
            var trends = new List<DailyTrendDto>();
            for (int i = 6; i >= 0; i--)
            {
                var day = today.AddDays(-i);
                var nextDay = day.AddDays(1);

                int runsCount = db.AnalysisRuns
                    .Where(r => r.RunDate >= day && r.RunDate < nextDay)
                    .Sum(r => (int?)r.AnalysisCount) ?? 0;

                decimal used = Math.Abs(db.StockMovements
                    .Where(m => m.Date >= day && m.Date < nextDay && m.Quantity < 0)
                    .Sum(m => (decimal?)m.Quantity) ?? 0);

                trends.Add(new DailyTrendDto
                {
                    DateLabel = day.ToString("dd.MM"),
                    AnalysesCount = runsCount,
                    ItemsUsed = used
                });
            }

            var expiringLotsCount = db.Lots
                .Where(l => !l.IsArchived && l.CurrentQuantity > 0 && l.ExpiryDate <= today.AddDays(expiryDays))
                .Count();

            var usedToday = Math.Abs(db.StockMovements
                .Where(m => m.Date >= today && m.Date < tomorrow && m.Quantity < 0)
                .Sum(m => (decimal?)m.Quantity) ?? 0);

            var analysesToday = db.AnalysisRuns
                .Where(r => r.RunDate >= today && r.RunDate < tomorrow)
                .Sum(r => (int?)r.AnalysisCount) ?? 0;

            return new DashboardStatsDto
            {
                TotalMaterialsCount = materials.Count,
                TotalStockSum = materials.Sum(m => m.TotalStock),
                LowStockCount = materials.Count(m => m.Status is StockStatus.Low or StockStatus.OutOfStock),
                ExpiringLotsCount = expiringLotsCount,
                UsedTodayCount = usedToday,
                AnalysesTodayCount = analysesToday,
                CriticalMaterials = critical,
                RecentMovements = recentMovements,
                DailyTrends = trends
            };
        }

        public List<Material> GetAllMaterials(string? search = null, int? categoryId = null, StockStatus? status = null)
        {
            using var db = new AppDbContext();
            var query = db.Materials
                .Include(m => m.Category)
                .Include(m => m.Unit)
                .Include(m => m.Supplier)
                .Include(m => m.Lots)
                .Where(m => m.IsActive)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(m => m.Name.ToLower().Contains(s)
                                      || (m.Article != null && m.Article.ToLower().Contains(s))
                                      || (m.Barcode != null && m.Barcode.ToLower().Contains(s))
                                      || (m.StorageLocation != null && m.StorageLocation.ToLower().Contains(s))
                                      || m.Lots.Any(l => l.LotNumber.ToLower().Contains(s)));
            }

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(m => m.CategoryId == categoryId.Value);
            }

            var list = query.OrderBy(m => m.Name).ToList();

            foreach (var mat in list)
            {
                mat.TotalStock = mat.Lots.Where(l => !l.IsArchived).Sum(l => l.CurrentQuantity);
            }

            if (status.HasValue)
            {
                list = list.Where(m => m.Status == status.Value).ToList();
            }

            return list;
        }

        public List<Lot> GetLotsForMaterial(int materialId)
        {
            using var db = new AppDbContext();
            return db.Lots
                .Include(l => l.Supplier)
                .Where(l => l.MaterialId == materialId && !l.IsArchived)
                .OrderBy(l => l.ExpiryDate)
                .ToList();
        }

        public void SaveMaterial(Material material, decimal initialQuantity = 0, decimal initialPrice = 0, DateTime? expiryDate = null)
        {
            using var db = new AppDbContext();
            var currentUser = AuthService.Instance.CurrentUser;

            if (material.Id == 0)
            {
                material.CreatedAt = DateTime.Now;
                material.IsActive = true;
                db.Materials.Add(material);
                db.SaveChanges();

                if (initialQuantity > 0)
                {
                    var lot = new Lot
                    {
                        MaterialId = material.Id,
                        LotNumber = $"П-{DateTime.Now:yyMMddHHmm}",
                        InitialQuantity = initialQuantity,
                        CurrentQuantity = initialQuantity,
                        ReceivedDate = DateTime.Now,
                        ExpiryDate = expiryDate ?? DateTime.Now.AddYears(1),
                        PurchasePrice = initialPrice,
                        SupplierId = material.SupplierId,
                        StorageLocation = material.StorageLocation
                    };
                    db.Lots.Add(lot);
                    db.SaveChanges();

                    db.StockMovements.Add(new StockMovement
                    {
                        Date = DateTime.Now,
                        MaterialId = material.Id,
                        LotId = lot.Id,
                        OperationType = MovementType.InitialBalance,
                        Quantity = initialQuantity,
                        BalanceAfter = initialQuantity,
                        UserId = currentUser?.Id ?? 1,
                        ReferenceNumber = "Начальный остаток",
                        Notes = "Ввод начального остатка при создании позиции"
                    });
                    db.SaveChanges();
                }

                AuditService.Instance.Log("Создание", "Материалы", material.Id.ToString(), $"Создан материал: {material.Name}, начальный остаток: {initialQuantity}");
            }
            else
            {
                var existing = db.Materials.Find(material.Id);
                if (existing != null)
                {
                    existing.Name = material.Name;
                    existing.CategoryId = material.CategoryId;
                    existing.UnitId = material.UnitId;
                    existing.Article = material.Article;
                    existing.Barcode = material.Barcode;
                    existing.MinQuantity = material.MinQuantity;
                    existing.MaxQuantity = material.MaxQuantity;
                    existing.SupplierId = material.SupplierId;
                    existing.StorageLocation = material.StorageLocation;
                    existing.Description = material.Description;
                    db.SaveChanges();

                    AuditService.Instance.Log("Изменение", "Материалы", material.Id.ToString(), $"Обновлены данные материала: {material.Name}");
                }
            }

            NotificationService.Instance.RefreshNotifications();
        }

        public void DeleteMaterial(int materialId)
        {
            using var db = new AppDbContext();
            var mat = db.Materials.Find(materialId);
            if (mat != null)
            {
                mat.IsActive = false; // Soft delete
                db.SaveChanges();
                AuditService.Instance.Log("Удаление", "Материалы", materialId.ToString(), $"Помечен удаленным материал: {mat.Name}");
                NotificationService.Instance.RefreshNotifications();
            }
        }

        public void RegisterReceipt(Receipt receipt, List<ReceiptItem> items)
        {
            using var db = new AppDbContext();
            var currentUser = AuthService.Instance.CurrentUser;

            receipt.UserId = currentUser?.Id ?? 1;
            receipt.Date = receipt.Date == default ? DateTime.Now : receipt.Date;
            receipt.TotalAmount = items.Sum(i => i.Quantity * i.Price);

            db.Receipts.Add(receipt);
            db.SaveChanges();

            foreach (var item in items)
            {
                item.ReceiptId = receipt.Id;

                // Create Lot
                var lot = new Lot
                {
                    MaterialId = item.MaterialId,
                    LotNumber = string.IsNullOrWhiteSpace(item.LotNumber) ? $"П-{DateTime.Now:yyMMddHHmm}" : item.LotNumber.Trim(),
                    InitialQuantity = item.Quantity,
                    CurrentQuantity = item.Quantity,
                    ReceivedDate = receipt.Date,
                    ExpiryDate = item.ExpiryDate == default ? receipt.Date.AddYears(1) : item.ExpiryDate,
                    PurchasePrice = item.Price,
                    SupplierId = receipt.SupplierId,
                    StorageLocation = item.StorageLocation
                };
                db.Lots.Add(lot);
                db.SaveChanges();

                item.LotId = lot.Id;
                db.ReceiptItems.Add(item);

                // Calculate current total stock
                var currentTotal = db.Lots
                    .Where(l => l.MaterialId == item.MaterialId && !l.IsArchived)
                    .Sum(l => l.CurrentQuantity);

                // Add movement
                db.StockMovements.Add(new StockMovement
                {
                    Date = receipt.Date,
                    MaterialId = item.MaterialId,
                    LotId = lot.Id,
                    OperationType = MovementType.Receipt,
                    Quantity = item.Quantity,
                    BalanceAfter = currentTotal,
                    UserId = receipt.UserId,
                    ReferenceNumber = receipt.ReceiptNumber,
                    Notes = $"Поступление по накладной {receipt.ReceiptNumber}, партия {lot.LotNumber}"
                });
            }

            db.SaveChanges();
            AuditService.Instance.Log("Поступление", "Склад", receipt.Id.ToString(), $"Оформлено поступление № {receipt.ReceiptNumber}, позиций: {items.Count}, сумма: {receipt.TotalAmount:N2}");
            NotificationService.Instance.RefreshNotifications();
        }

        public List<AnalysisMaterialPreviewDto> PreviewAnalysisConsumption(int analysisId, int count)
        {
            using var db = new AppDbContext();
            var analysis = db.Analyses
                .Include(a => a.Materials)
                .ThenInclude(am => am.Material)
                .ThenInclude(m => m!.Unit)
                .Include(a => a.Materials)
                .ThenInclude(am => am.Material)
                .ThenInclude(m => m!.Lots)
                .FirstOrDefault(a => a.Id == analysisId);

            if (analysis == null) return new List<AnalysisMaterialPreviewDto>();

            var preview = new List<AnalysisMaterialPreviewDto>();
            foreach (var am in analysis.Materials)
            {
                if (am.Material == null) continue;
                decimal available = am.Material.Lots.Where(l => !l.IsArchived).Sum(l => l.CurrentQuantity);
                decimal required = am.RequiredQuantity * count;

                preview.Add(new AnalysisMaterialPreviewDto
                {
                    MaterialId = am.MaterialId,
                    MaterialName = am.Material.Name,
                    UnitName = am.Material.Unit?.ShortName ?? "шт.",
                    NormPerAnalysis = am.RequiredQuantity,
                    TotalRequired = required,
                    AvailableStock = available
                });
            }

            return preview;
        }

        public void ExecuteAnalysisRun(int analysisId, int count, string? notes)
        {
            if (count <= 0) throw new ArgumentException("Количество анализов должно быть больше 0.");

            using var db = new AppDbContext();
            var currentUser = AuthService.Instance.CurrentUser;

            var analysis = db.Analyses
                .Include(a => a.Materials)
                .ThenInclude(am => am.Material)
                .ThenInclude(m => m!.Lots)
                .FirstOrDefault(a => a.Id == analysisId);

            if (analysis == null) throw new InvalidOperationException("Анализ не найден.");

            // 1. Check availability
            var allowNegativeSetting = db.Settings.FirstOrDefault(s => s.Key == "AllowNegativeStock")?.Value?.ToLower() == "true";
            var shortages = new List<string>();

            foreach (var am in analysis.Materials)
            {
                if (am.Material == null) continue;
                decimal available = am.Material.Lots.Where(l => !l.IsArchived).Sum(l => l.CurrentQuantity);
                decimal required = am.RequiredQuantity * count;

                if (available < required && !allowNegativeSetting)
                {
                    shortages.Add($"«{am.Material.Name}»: требуется {required:0.##}, в наличии {available:0.##}");
                }
            }

            if (shortages.Any())
            {
                throw new InvalidOperationException("Недостаточно материалов на складе:\n" + string.Join("\n", shortages));
            }

            // 2. Create AnalysisRun record
            var run = new AnalysisRun
            {
                RunNumber = $"АР-{DateTime.Now:yyMMdd-HHmmss}",
                AnalysisId = analysisId,
                AnalysisCount = count,
                RunDate = DateTime.Now,
                UserId = currentUser?.Id ?? 1,
                Notes = notes,
                CreatedAt = DateTime.Now
            };
            db.AnalysisRuns.Add(run);
            db.SaveChanges();

            // 3. FEFO Deduction (First Expired First Out)
            foreach (var am in analysis.Materials)
            {
                if (am.Material == null) continue;
                decimal needed = am.RequiredQuantity * count;

                // Sort lots by ExpiryDate ascending
                var lots = db.Lots
                    .Where(l => l.MaterialId == am.MaterialId && !l.IsArchived)
                    .OrderBy(l => l.ExpiryDate)
                    .ToList();

                decimal currentMaterialTotal = lots.Sum(l => l.CurrentQuantity);

                foreach (var lot in lots.Where(l => l.CurrentQuantity > 0))
                {
                    if (needed <= 0) break;

                    decimal deductFromThisLot = Math.Min(lot.CurrentQuantity, needed);
                    lot.CurrentQuantity -= deductFromThisLot;
                    needed -= deductFromThisLot;
                    currentMaterialTotal -= deductFromThisLot;

                    db.StockMovements.Add(new StockMovement
                    {
                        Date = DateTime.Now,
                        MaterialId = am.MaterialId,
                        LotId = lot.Id,
                        OperationType = MovementType.AnalysisRun,
                        Quantity = -deductFromThisLot,
                        BalanceAfter = currentMaterialTotal,
                        UserId = currentUser?.Id ?? 1,
                        AnalysisRunId = run.Id,
                        ReferenceNumber = $"{analysis.Code} (x{count})",
                        Notes = $"Списание по анализу «{analysis.Name}» (партия {lot.LotNumber})"
                    });
                }

                // If negative allowed and still needed remaining
                if (needed > 0 && allowNegativeSetting)
                {
                    var fallbackLot = lots.LastOrDefault();
                    if (fallbackLot != null)
                    {
                        fallbackLot.CurrentQuantity -= needed;
                    }
                    currentMaterialTotal -= needed;

                    db.StockMovements.Add(new StockMovement
                    {
                        Date = DateTime.Now,
                        MaterialId = am.MaterialId,
                        LotId = fallbackLot?.Id,
                        OperationType = MovementType.AnalysisRun,
                        Quantity = -needed,
                        BalanceAfter = currentMaterialTotal,
                        UserId = currentUser?.Id ?? 1,
                        AnalysisRunId = run.Id,
                        ReferenceNumber = $"{analysis.Code} (x{count})",
                        Notes = $"Сверхнормативный расход «{analysis.Name}»"
                    });
                }
            }

            db.SaveChanges();
            AuditService.Instance.Log("Выполнение анализа", "Анализы", run.Id.ToString(), $"Выполнен анализ «{analysis.Name}» в количестве {count} шт. Лаборант: {currentUser?.FullName ?? "Система"}");
            NotificationService.Instance.RefreshNotifications();
        }

        public void RegisterWriteoff(Writeoff writeoff, int? specificLotId = null)
        {
            using var db = new AppDbContext();
            var currentUser = AuthService.Instance.CurrentUser;

            writeoff.UserId = currentUser?.Id ?? 1;
            writeoff.Date = writeoff.Date == default ? DateTime.Now : writeoff.Date;
            if (string.IsNullOrWhiteSpace(writeoff.WriteoffNumber))
            {
                writeoff.WriteoffNumber = $"СП-{DateTime.Now:yyMMddHHmm}";
            }

            var allowNegativeSetting = db.Settings.FirstOrDefault(s => s.Key == "AllowNegativeStock")?.Value?.ToLower() == "true";
            decimal available = db.Lots.Where(l => l.MaterialId == writeoff.MaterialId && !l.IsArchived).Sum(l => l.CurrentQuantity);

            if (available < writeoff.Quantity && !allowNegativeSetting)
            {
                throw new InvalidOperationException($"Нельзя списать {writeoff.Quantity:0.##}. Доступный остаток: {available:0.##}");
            }

            db.Writeoffs.Add(writeoff);
            db.SaveChanges();

            decimal needed = writeoff.Quantity;

            decimal totalRunning = available;

            if (specificLotId.HasValue && specificLotId.Value > 0)
            {
                var lot = db.Lots.Find(specificLotId.Value);
                if (lot != null)
                {
                    lot.CurrentQuantity -= needed;
                    writeoff.LotId = lot.Id;
                    totalRunning -= needed;

                    db.StockMovements.Add(new StockMovement
                    {
                        Date = writeoff.Date,
                        MaterialId = writeoff.MaterialId,
                        LotId = lot.Id,
                        OperationType = MovementType.ManualWriteoff,
                        Quantity = -needed,
                        BalanceAfter = totalRunning,
                        UserId = writeoff.UserId,
                        ReferenceNumber = writeoff.WriteoffNumber,
                        Notes = $"Списание: {writeoff.Reason}. {writeoff.Notes}"
                    });
                }
            }
            else
            {
                // FEFO
                var lots = db.Lots
                    .Where(l => l.MaterialId == writeoff.MaterialId && !l.IsArchived && l.CurrentQuantity > 0)
                    .OrderBy(l => l.ExpiryDate)
                    .ToList();

                foreach (var lot in lots)
                {
                    if (needed <= 0) break;
                    decimal deduct = Math.Min(lot.CurrentQuantity, needed);
                    lot.CurrentQuantity -= deduct;
                    needed -= deduct;
                    totalRunning -= deduct;

                    db.StockMovements.Add(new StockMovement
                    {
                        Date = writeoff.Date,
                        MaterialId = writeoff.MaterialId,
                        LotId = lot.Id,
                        OperationType = MovementType.ManualWriteoff,
                        Quantity = -deduct,
                        BalanceAfter = totalRunning,
                        UserId = writeoff.UserId,
                        ReferenceNumber = writeoff.WriteoffNumber,
                        Notes = $"Списание: {writeoff.Reason}. {writeoff.Notes} (партия {lot.LotNumber})"
                    });
                }
            }

            db.SaveChanges();
            var mat = db.Materials.Find(writeoff.MaterialId);
            AuditService.Instance.Log("Списание", "Склад", writeoff.Id.ToString(), $"Ручное списание: {mat?.Name}, кол-во: {writeoff.Quantity}, причина: {writeoff.Reason}");
            NotificationService.Instance.RefreshNotifications();
        }

        public void PerformInventoryAdjustment(int materialId, decimal actualQuantity, string reason)
        {
            using var db = new AppDbContext();
            var currentUser = AuthService.Instance.CurrentUser;

            var lots = db.Lots.Where(l => l.MaterialId == materialId && !l.IsArchived).OrderByDescending(l => l.ReceivedDate).ToList();
            decimal systemQuantity = lots.Sum(l => l.CurrentQuantity);
            decimal diff = actualQuantity - systemQuantity;

            if (diff == 0) return; // No discrepancy

            var check = new InventoryCheck
            {
                CheckNumber = $"ИНВ-{DateTime.Now:yyMMddHHmm}",
                Date = DateTime.Now,
                UserId = currentUser?.Id ?? 1,
                Status = "Завершено",
                Notes = $"Корректировка по результатам инвентаризации: {reason}"
            };
            db.InventoryChecks.Add(check);
            db.SaveChanges();

            db.InventoryCheckItems.Add(new InventoryCheckItem
            {
                InventoryCheckId = check.Id,
                MaterialId = materialId,
                SystemQuantity = systemQuantity,
                ActualQuantity = actualQuantity,
                Difference = diff,
                Reason = reason
            });

            // Adjust lot
            var targetLot = lots.FirstOrDefault();
            if (targetLot != null)
            {
                targetLot.CurrentQuantity += diff;
            }

            db.StockMovements.Add(new StockMovement
            {
                Date = DateTime.Now,
                MaterialId = materialId,
                LotId = targetLot?.Id,
                OperationType = MovementType.InventoryAdjustment,
                Quantity = diff,
                BalanceAfter = actualQuantity,
                UserId = currentUser?.Id ?? 1,
                ReferenceNumber = check.CheckNumber,
                Notes = $"Инвентаризация: разница {diff:+0.##;-0.##;0}. Причина: {reason}"
            });

            db.SaveChanges();
            var mat = db.Materials.Find(materialId);
            AuditService.Instance.Log("Инвентаризация", "Склад", check.Id.ToString(), $"Корректировка материала {mat?.Name}: факт {actualQuantity}, расхождение {diff}. Причина: {reason}");
            NotificationService.Instance.RefreshNotifications();
        }
    }
}
