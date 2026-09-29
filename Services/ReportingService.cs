using System;
using System.Collections.Generic;
using System.Linq;
using LabStock.Data;
using LabStock.Models;
using Microsoft.EntityFrameworkCore;

namespace LabStock.Services
{
    public class DailyReportItemDto
    {
        public string MaterialName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal InitialStock { get; set; }
        public decimal Receipts { get; set; }
        public decimal AnalysisConsumption { get; set; }
        public decimal ManualWriteoffs { get; set; }
        public decimal FinalStock { get; set; }
    }

    public class DailyReportDto
    {
        public DateTime Date { get; set; }
        public int TotalAnalysesPerformed { get; set; }
        public Dictionary<string, int> AnalysesBreakdown { get; set; } = new();
        public List<DailyReportItemDto> Items { get; set; } = new();
    }

    public class MonthlyReportDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public int TotalAnalyses { get; set; }
        public decimal TotalReceiptsCost { get; set; }
        public decimal TotalWarehouseValuation { get; set; }
        public Dictionary<string, decimal> ConsumptionByCategory { get; set; } = new();
        public Dictionary<string, int> AnalysesCount { get; set; } = new();
        public List<TopMaterialUsageDto> TopMaterials { get; set; } = new();
        public List<ExpiredMaterialDto> ExpiredMaterials { get; set; } = new();
        public List<DailySummaryDto> DailyTimeline { get; set; } = new();
    }

    public class TopMaterialUsageDto
    {
        public string MaterialName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal QuantityUsed { get; set; }
        public string Unit { get; set; } = string.Empty;
    }

    public class ExpiredMaterialDto
    {
        public string MaterialName { get; set; } = string.Empty;
        public string LotNumber { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public DateTime ExpiryDate { get; set; }
    }

    public class DailySummaryDto
    {
        public DateTime Date { get; set; }
        public int AnalysesCount { get; set; }
        public decimal MaterialsUsed { get; set; }
    }

    public class AnalyticsPredictionDto
    {
        public string MaterialName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal CurrentStock { get; set; }
        public string Unit { get; set; } = string.Empty;
        public decimal DailyAvgConsumption { get; set; }
        public decimal WeeklyAvgConsumption { get; set; }
        public decimal MonthlyAvgConsumption { get; set; }
        public int DaysUntilDepletion { get; set; } // Runway
        public decimal RecommendedOrderQuantity { get; set; }
        public string Status { get; set; } = "Норма";
    }

    public class ReportingService
    {
        private static ReportingService? _instance;
        public static ReportingService Instance => _instance ??= new ReportingService();

        public DailyReportDto GetDailyReport(DateTime date)
        {
            using var db = new AppDbContext();
            var dayStart = date.Date;
            var dayEnd = dayStart.AddDays(1);

            // Runs for the day
            var runs = db.AnalysisRuns
                .Include(r => r.Analysis)
                .Where(r => r.RunDate >= dayStart && r.RunDate < dayEnd)
                .ToList();

            var analysesBreakdown = runs
                .GroupBy(r => r.Analysis?.Name ?? "Неизвестно")
                .ToDictionary(g => g.Key, g => g.Sum(r => r.AnalysisCount));

            // Movements for the day
            var movementsToday = db.StockMovements
                .Include(m => m.Material)
                .ThenInclude(mat => mat!.Unit)
                .Where(m => m.Date >= dayStart && m.Date < dayEnd)
                .ToList();

            // All materials
            var materials = db.Materials
                .Include(m => m.Unit)
                .Include(m => m.Lots)
                .Where(m => m.IsActive)
                .OrderBy(m => m.Name)
                .ToList();

            var reportItems = new List<DailyReportItemDto>();

            foreach (var mat in materials)
            {
                var matMovementsToday = movementsToday.Where(m => m.MaterialId == mat.Id).ToList();

                decimal receipts = matMovementsToday
                    .Where(m => m.Quantity > 0 && m.OperationType == MovementType.Receipt)
                    .Sum(m => m.Quantity);

                decimal analysisOut = Math.Abs(matMovementsToday
                    .Where(m => m.Quantity < 0 && m.OperationType == MovementType.AnalysisRun)
                    .Sum(m => m.Quantity));

                decimal manualOut = Math.Abs(matMovementsToday
                    .Where(m => m.Quantity < 0 && m.OperationType == MovementType.ManualWriteoff)
                    .Sum(m => m.Quantity));

                decimal currentStock = mat.Lots.Where(l => !l.IsArchived).Sum(l => l.CurrentQuantity);

                // If date is today, currentStock is the end of day. If past date, derive back from movements
                var futureMovements = db.StockMovements
                    .Where(m => m.MaterialId == mat.Id && m.Date >= dayEnd)
                    .Sum(m => (decimal?)m.Quantity) ?? 0;

                decimal finalStock = currentStock - futureMovements;
                decimal initialStock = finalStock - receipts + analysisOut + manualOut;

                if (initialStock != 0 || receipts != 0 || analysisOut != 0 || manualOut != 0 || finalStock != 0)
                {
                    reportItems.Add(new DailyReportItemDto
                    {
                        MaterialName = mat.Name,
                        Unit = mat.Unit?.ShortName ?? "шт.",
                        InitialStock = initialStock,
                        Receipts = receipts,
                        AnalysisConsumption = analysisOut,
                        ManualWriteoffs = manualOut,
                        FinalStock = finalStock
                    });
                }
            }

            return new DailyReportDto
            {
                Date = dayStart,
                TotalAnalysesPerformed = runs.Sum(r => r.AnalysisCount),
                AnalysesBreakdown = analysesBreakdown,
                Items = reportItems
            };
        }

        public MonthlyReportDto GetMonthlyReport(int year, int month)
        {
            using var db = new AppDbContext();
            var monthStart = new DateTime(year, month, 1);
            var monthEnd = monthStart.AddMonths(1);
            var monthName = monthStart.ToString("MMMM yyyy");

            var runs = db.AnalysisRuns
                .Include(r => r.Analysis)
                .Where(r => r.RunDate >= monthStart && r.RunDate < monthEnd)
                .ToList();

            var movements = db.StockMovements
                .Include(m => m.Material)
                .ThenInclude(mat => mat!.Category)
                .Include(m => m.Material)
                .ThenInclude(mat => mat!.Unit)
                .Where(m => m.Date >= monthStart && m.Date < monthEnd)
                .ToList();

            var consumptionByCategory = movements
                .Where(m => m.Quantity < 0)
                .GroupBy(m => m.Material?.Category?.Name ?? "Прочие")
                .ToDictionary(g => g.Key, g => Math.Abs(g.Sum(m => m.Quantity)));

            var analysesCount = runs
                .GroupBy(r => r.Analysis?.Name ?? "Неизвестно")
                .ToDictionary(g => g.Key, g => g.Sum(r => r.AnalysisCount));

            var topMaterials = movements
                .Where(m => m.Quantity < 0 && m.Material != null)
                .GroupBy(m => m.Material!)
                .Select(g => new TopMaterialUsageDto
                {
                    MaterialName = g.Key.Name,
                    Category = g.Key.Category?.Name ?? "Общее",
                    QuantityUsed = Math.Abs(g.Sum(m => m.Quantity)),
                    Unit = g.Key.Unit?.ShortName ?? "шт."
                })
                .OrderByDescending(t => t.QuantityUsed)
                .Take(10)
                .ToList();

            var expiredLots = db.Lots
                .Include(l => l.Material)
                .ThenInclude(m => m!.Unit)
                .Where(l => !l.IsArchived && l.CurrentQuantity > 0 && l.ExpiryDate < DateTime.Today)
                .ToList()
                .Select(l => new ExpiredMaterialDto
                {
                    MaterialName = l.Material?.Name ?? "",
                    LotNumber = l.LotNumber,
                    Quantity = l.CurrentQuantity,
                    Unit = l.Material?.Unit?.ShortName ?? "шт.",
                    ExpiryDate = l.ExpiryDate
                })
                .ToList();

            var receiptsCost = db.Receipts
                .Where(r => r.Date >= monthStart && r.Date < monthEnd)
                .Sum(r => (decimal?)r.TotalAmount) ?? 0;

            var warehouseValuation = db.Lots
                .Where(l => !l.IsArchived && l.CurrentQuantity > 0)
                .Sum(l => (decimal?)(l.CurrentQuantity * l.PurchasePrice)) ?? 0;

            // Daily timeline
            var timeline = new List<DailySummaryDto>();
            int daysInMonth = DateTime.DaysInMonth(year, month);
            for (int day = 1; day <= daysInMonth; day++)
            {
                var cur = new DateTime(year, month, day);
                var next = cur.AddDays(1);

                int aCount = runs.Where(r => r.RunDate >= cur && r.RunDate < next).Sum(r => r.AnalysisCount);
                decimal mUsed = Math.Abs(movements.Where(m => m.Date >= cur && m.Date < next && m.Quantity < 0).Sum(m => m.Quantity));

                timeline.Add(new DailySummaryDto
                {
                    Date = cur,
                    AnalysesCount = aCount,
                    MaterialsUsed = mUsed
                });
            }

            return new MonthlyReportDto
            {
                Year = year,
                Month = month,
                MonthName = monthName,
                TotalAnalyses = runs.Sum(r => r.AnalysisCount),
                TotalReceiptsCost = receiptsCost,
                TotalWarehouseValuation = warehouseValuation,
                ConsumptionByCategory = consumptionByCategory,
                AnalysesCount = analysesCount,
                TopMaterials = topMaterials,
                ExpiredMaterials = expiredLots,
                DailyTimeline = timeline
            };
        }

        public List<AnalyticsPredictionDto> GetAnalyticsPredictions()
        {
            using var db = new AppDbContext();
            var materials = db.Materials
                .Include(m => m.Category)
                .Include(m => m.Unit)
                .Include(m => m.Lots)
                .Where(m => m.IsActive)
                .ToList();

            var thirtyDaysAgo = DateTime.Today.AddDays(-30);
            var movementsLast30Days = db.StockMovements
                .Where(m => m.Date >= thirtyDaysAgo && m.Quantity < 0)
                .ToList();

            var result = new List<AnalyticsPredictionDto>();

            foreach (var mat in materials)
            {
                decimal currentStock = mat.Lots.Where(l => !l.IsArchived).Sum(l => l.CurrentQuantity);
                decimal consumed30Days = Math.Abs(movementsLast30Days
                    .Where(m => m.MaterialId == mat.Id)
                    .Sum(m => m.Quantity));

                decimal dailyAvg = consumed30Days / 30.0m;
                decimal weeklyAvg = dailyAvg * 7;
                decimal monthlyAvg = dailyAvg * 30;

                int daysUntilDepletion = dailyAvg > 0 ? (int)(currentStock / dailyAvg) : 999;

                // Recommended order quantity = MaxQuantity - CurrentStock, or at least 30 days consumption
                decimal recommendOrder = 0;
                if (currentStock <= mat.MinQuantity || daysUntilDepletion <= 14)
                {
                    decimal targetOrder = Math.Max(mat.MaxQuantity - currentStock, monthlyAvg);
                    recommendOrder = Math.Max(targetOrder, mat.MinQuantity);
                }

                string status = "Норма";
                if (currentStock <= 0) status = "Нет на складе";
                else if (daysUntilDepletion <= 7 || currentStock <= mat.MinQuantity / 2) status = "Критично";
                else if (daysUntilDepletion <= 15 || currentStock <= mat.MinQuantity) status = "Внимание";

                result.Add(new AnalyticsPredictionDto
                {
                    MaterialName = mat.Name,
                    Category = mat.Category?.Name ?? "",
                    CurrentStock = currentStock,
                    Unit = mat.Unit?.ShortName ?? "шт.",
                    DailyAvgConsumption = dailyAvg,
                    WeeklyAvgConsumption = weeklyAvg,
                    MonthlyAvgConsumption = monthlyAvg,
                    DaysUntilDepletion = daysUntilDepletion,
                    RecommendedOrderQuantity = recommendOrder,
                    Status = status
                });
            }

            return result.OrderBy(r => r.DaysUntilDepletion).ToList();
        }
    }
}
