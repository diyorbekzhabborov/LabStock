using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LabStock.Data;
using LabStock.Models;
using LabStock.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[assembly: DoNotParallelize]

namespace LabStock.Tests
{
    [TestClass]
    public class LabStockTests
    {
        [AssemblyInitialize]
        public static void AssemblyInit(TestContext context)
        {
            using var db = new AppDbContext();
            DbInitializer.Initialize(db);
        }

        [TestMethod]
        public void Test_1_DbInitialization_SeedsUsersAndMaterials()
        {
            using var db = new AppDbContext();

            // Verify Users
            var users = db.Users.ToList();
            Assert.IsTrue(users.Count >= 4, "Users count should be at least 4");
            Assert.IsTrue(users.Any(u => u.Role == UserRole.Admin), "Admin should exist");
            Assert.IsTrue(users.Any(u => u.Role == UserRole.LabAssistant), "LabAssistant should exist");

            // Verify Categories & Units
            Assert.IsTrue(db.Categories.Count() >= 5, "Categories should be seeded");
            Assert.IsTrue(db.Units.Count() >= 6, "Units should be seeded");

            // Verify Materials & Lots
            var materials = db.Materials.Include(m => m.Lots).ToList();
            Assert.IsTrue(materials.Count >= 10, "Materials count should be at least 10");
            Assert.IsTrue(materials.All(m => m.Lots.Any()), "All materials should have lots");

            // Verify Analyses and Norms
            var analyses = db.Analyses.Include(a => a.Materials).ToList();
            Assert.IsTrue(analyses.Count >= 4, "Analyses should be seeded");
            Assert.IsTrue(analyses.All(a => a.Materials.Any()), "Analyses should have recipe norms");
        }

        [TestMethod]
        public void Test_2_AuthService_LoginAndPermissions()
        {
            var auth = AuthService.Instance;

            // Successful admin login
            bool ok = auth.Login("admin", "admin123");
            Assert.IsTrue(ok, "Admin login should succeed");
            Assert.IsNotNull(auth.CurrentUser);
            Assert.AreEqual(UserRole.Admin, auth.CurrentUser.Role);
            Assert.IsTrue(auth.CanManageUsers());
            Assert.IsTrue(auth.CanManageSettings());

            // Lab assistant login
            ok = auth.Login("lab", "lab123");
            Assert.IsTrue(ok, "Lab assistant login should succeed");
            Assert.AreEqual(UserRole.LabAssistant, auth.CurrentUser.Role);
            Assert.IsFalse(auth.CanManageUsers());
            Assert.IsTrue(auth.CanPerformAnalysis());
            Assert.IsTrue(auth.CanPerformWriteoff());

            // Wrong password
            ok = auth.Login("admin", "wrong_password");
            Assert.IsFalse(ok, "Wrong password should fail");
        }

        [TestMethod]
        public void Test_3_PreviewAnalysisConsumption_CalculatesAccurately()
        {
            using var db = new AppDbContext();
            var oak = db.Analyses.Include(a => a.Materials).First(a => a.Code == "ОАК-01");

            int count = 25; // As in the PDF example: "Пример: если выполнено 25 анализов ОАК и норма пробирки 1 шт, списывается 25"
            var preview = InventoryService.Instance.PreviewAnalysisConsumption(oak.Id, count);

            Assert.IsTrue(preview.Count >= 3, "Preview should contain all recipe materials");

            var tubeItem = preview.First(p => p.MaterialName.Contains("EDTA"));
            Assert.AreEqual(1m, tubeItem.NormPerAnalysis, "EDTA tube norm should be 1");
            Assert.AreEqual(25m, tubeItem.TotalRequired, "Total required for 25 analyses should be 25");
            Assert.IsTrue(tubeItem.AvailableStock >= 25m, "Stock should be available");
            Assert.IsFalse(tubeItem.HasShortage, "Should have no shortage");
        }

        [TestMethod]
        public void Test_4_AnalysisExecution_DeductsFEFO_AndCreatesMovements()
        {
            using var db = new AppDbContext();
            var oak = db.Analyses.Include(a => a.Materials).First(a => a.Code == "ОАК-01");
            var edtaMaterial = db.Materials.Include(m => m.Lots).First(m => m.Name.Contains("EDTA"));

            decimal stockBefore = edtaMaterial.Lots.Where(l => !l.IsArchived).Sum(l => l.CurrentQuantity);

            // Execute 5 analyses
            int count = 5;
            InventoryService.Instance.ExecuteAnalysisRun(oak.Id, count, "Тестовое выполнение ОАК x5");

            // Verify stock decreased by exactly 5
            using var dbAfter = new AppDbContext();
            var edtaAfter = dbAfter.Materials.Include(m => m.Lots).First(m => m.Id == edtaMaterial.Id);
            decimal stockAfter = edtaAfter.Lots.Where(l => !l.IsArchived).Sum(l => l.CurrentQuantity);

            Assert.AreEqual(stockBefore - 5m, stockAfter, "Stock should decrease by 5");

            // Verify StockMovement recorded
            var latestMovement = dbAfter.StockMovements
                .OrderByDescending(m => m.Id)
                .FirstOrDefault(m => m.MaterialId == edtaMaterial.Id && m.OperationType == MovementType.AnalysisRun);

            Assert.IsNotNull(latestMovement);
            Assert.AreEqual(-5m, latestMovement.Quantity);
            Assert.AreEqual(stockAfter, latestMovement.BalanceAfter);
        }

        [TestMethod]
        public void Test_5_NegativeStockProtection_BlocksExecutionWhenShortage()
        {
            using var db = new AppDbContext();
            var oak = db.Analyses.First(a => a.Code == "ОАК-01");

            // Try to run 999999 analyses which exceeds stock
            Assert.ThrowsException<InvalidOperationException>(() =>
            {
                InventoryService.Instance.ExecuteAnalysisRun(oak.Id, 999999, "Слишком много анализов");
            });
        }

        [TestMethod]
        public void Test_6_ReceiptRegistration_IncrementsStockAndCreatesLot()
        {
            using var db = new AppDbContext();
            var mat = db.Materials.Include(m => m.Lots).First();
            decimal stockBefore = mat.Lots.Where(l => !l.IsArchived).Sum(l => l.CurrentQuantity);

            var receipt = new Receipt
            {
                ReceiptNumber = $"ТЕСТ-ПН-{DateTime.Now.Ticks}",
                Date = DateTime.Now,
                SupplierId = mat.SupplierId,
                Notes = "Тестовое поступление"
            };

            var items = new List<ReceiptItem>
            {
                new ReceiptItem
                {
                    MaterialId = mat.Id,
                    LotNumber = $"T-LOT-{DateTime.Now:HHmmss}",
                    Quantity = 500,
                    Price = 2.50m,
                    ExpiryDate = DateTime.Today.AddYears(2),
                    StorageLocation = mat.StorageLocation
                }
            };

            InventoryService.Instance.RegisterReceipt(receipt, items);

            using var dbAfter = new AppDbContext();
            var matAfter = dbAfter.Materials.Include(m => m.Lots).First(m => m.Id == mat.Id);
            decimal stockAfter = matAfter.Lots.Where(l => !l.IsArchived).Sum(l => l.CurrentQuantity);

            Assert.AreEqual(stockBefore + 500m, stockAfter, "Stock should increase by 500");

            var createdLot = dbAfter.Lots.FirstOrDefault(l => l.LotNumber == items[0].LotNumber);
            Assert.IsNotNull(createdLot, "Lot should be created");
            Assert.AreEqual(500m, createdLot.CurrentQuantity);

            var movement = dbAfter.StockMovements.FirstOrDefault(m => m.LotId == createdLot.Id && m.OperationType == MovementType.Receipt);
            Assert.IsNotNull(movement, "Receipt stock movement should exist");
            Assert.AreEqual(500m, movement.Quantity);
        }

        [TestMethod]
        public void Test_7_Writeoff_DeductsStockWithReason()
        {
            using var db = new AppDbContext();
            var mat = db.Materials.Include(m => m.Lots).First();
            decimal stockBefore = mat.Lots.Where(l => !l.IsArchived).Sum(l => l.CurrentQuantity);

            var writeoff = new Writeoff
            {
                MaterialId = mat.Id,
                Quantity = 3,
                Reason = "Повреждение",
                Notes = "Тестовый бой при транспортировке",
                Date = DateTime.Now
            };

            InventoryService.Instance.RegisterWriteoff(writeoff);

            using var dbAfter = new AppDbContext();
            var matAfter = dbAfter.Materials.Include(m => m.Lots).First(m => m.Id == mat.Id);
            decimal stockAfter = matAfter.Lots.Where(l => !l.IsArchived).Sum(l => l.CurrentQuantity);

            Assert.AreEqual(stockBefore - 3m, stockAfter, "Stock should decrease by 3");

            var movement = dbAfter.StockMovements
                .OrderByDescending(m => m.Id)
                .First(m => m.MaterialId == mat.Id && m.OperationType == MovementType.ManualWriteoff);

            Assert.AreEqual(-3m, movement.Quantity);
            Assert.IsTrue(movement.Notes?.Contains("Повреждение") == true);
        }

        [TestMethod]
        public void Test_8_InventoryAdjustment_CorrectsDiscrepancy()
        {
            using var db = new AppDbContext();
            var mat = db.Materials.Include(m => m.Lots).First();
            decimal stockBefore = mat.Lots.Where(l => !l.IsArchived).Sum(l => l.CurrentQuantity);

            decimal newActualQuantity = stockBefore + 15m; // found 15 surplus units
            InventoryService.Instance.PerformInventoryAdjustment(mat.Id, newActualQuantity, "Обнаружен излишек при ревизии");

            using var dbAfter = new AppDbContext();
            var matAfter = dbAfter.Materials.Include(m => m.Lots).First(m => m.Id == mat.Id);
            decimal stockAfter = matAfter.Lots.Where(l => !l.IsArchived).Sum(l => l.CurrentQuantity);

            Assert.AreEqual(newActualQuantity, stockAfter, "Stock should exactly match actual count");

            var checkItem = dbAfter.InventoryCheckItems
                .Include(i => i.InventoryCheck)
                .OrderByDescending(i => i.Id)
                .First(i => i.MaterialId == mat.Id);

            Assert.AreEqual(15m, checkItem.Difference);
            Assert.AreEqual("Обнаружен излишек при ревизии", checkItem.Reason);
        }

        [TestMethod]
        public void Test_9_DailyAndMonthlyReports_GeneratedCorrectly()
        {
            var daily = ReportingService.Instance.GetDailyReport(DateTime.Today);
            Assert.IsNotNull(daily);
            Assert.IsTrue(daily.Items.Count > 0, "Daily report should contain items");

            var monthly = ReportingService.Instance.GetMonthlyReport(DateTime.Today.Year, DateTime.Today.Month);
            Assert.IsNotNull(monthly);
            Assert.IsTrue(monthly.TotalWarehouseValuation > 0, "Warehouse valuation should be positive");
            Assert.IsTrue(monthly.TopMaterials.Count > 0, "Top materials should be populated");
        }

        [TestMethod]
        public void Test_10_Exports_Excel_Pdf_Csv()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "LabStockTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                var daily = ReportingService.Instance.GetDailyReport(DateTime.Today);
                var monthly = ReportingService.Instance.GetMonthlyReport(DateTime.Today.Year, DateTime.Today.Month);

                // 1. Export Excel
                var xlsxPath = Path.Combine(tempDir, "daily.xlsx");
                ExportService.Instance.ExportDailyReportToExcel(daily, xlsxPath);
                Assert.IsTrue(File.Exists(xlsxPath), "Excel daily report should be created");
                Assert.IsTrue(new FileInfo(xlsxPath).Length > 1000, "Excel file should not be empty");

                // 2. Export Monthly Excel
                var monthlyXlsx = Path.Combine(tempDir, "monthly.xlsx");
                ExportService.Instance.ExportMonthlyReportToExcel(monthly, monthlyXlsx);
                Assert.IsTrue(File.Exists(monthlyXlsx), "Monthly excel report should be created");

                // 3. Export PDF
                var pdfPath = Path.Combine(tempDir, "daily.pdf");
                ExportService.Instance.ExportDailyReportToPdf(daily, pdfPath);
                Assert.IsTrue(File.Exists(pdfPath), "PDF report should be created");
                Assert.IsTrue(new FileInfo(pdfPath).Length > 1000, "PDF file should not be empty");

                // 4. Export CSV
                using var db = new AppDbContext();
                var movements = db.StockMovements.Include(m => m.Material).Include(m => m.Lot).Include(m => m.User).Take(20).ToList();
                var csvPath = Path.Combine(tempDir, "movements.csv");
                ExportService.Instance.ExportStockMovementsToCsv(movements, csvPath);
                Assert.IsTrue(File.Exists(csvPath), "CSV export should be created");
                Assert.IsTrue(new FileInfo(csvPath).Length > 50, "CSV file should contain records");
            }
            finally
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }

        [TestMethod]
        public void Test_11_BackupService_CreatesAndValidatesBackup()
        {
            var backupPath = BackupService.Instance.CreateBackup();
            Assert.IsTrue(File.Exists(backupPath), "Backup file should be created");
            Assert.IsTrue(BackupService.Instance.ValidateBackupFile(backupPath), "Backup file must pass integrity check");

            // Clean up test backup file
            try { File.Delete(backupPath); } catch { }
        }
    }
}
