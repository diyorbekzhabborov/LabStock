using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using LabStock.Models;
using Microsoft.EntityFrameworkCore;

namespace LabStock.Data
{
    public static class DbInitializer
    {
        private static readonly object _lock = new object();

        public static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        public static void Initialize(AppDbContext context)
        {
            lock (_lock)
            {
                try
                {
                    context.Database.EnsureCreated();
                }
                catch { }

                if (context.Users.Any())
                {
                    return; // Database already seeded
                }

            // 1. Settings
            context.Settings.AddRange(
                new Setting { Key = "LabName", Value = "Медицинская лаборатория «LabStock»", Description = "Название лаборатории", Category = "General" },
                new Setting { Key = "Currency", Value = "сомони", Description = "Валюта системы", Category = "General" },
                new Setting { Key = "ExpiryAlertDays", Value = "30", Description = "Порог оповещения о сроке годности (в днях)", Category = "Inventory" },
                new Setting { Key = "AllowNegativeStock", Value = "false", Description = "Разрешать отрицательный остаток", Category = "Inventory" },
                new Setting { Key = "FefoEnabled", Value = "true", Description = "Использовать алгоритм FEFO при списании", Category = "Inventory" }
            );

            // 2. Users
            var admin = new User
            {
                Username = "admin",
                FullName = "Алимов Рустам Собирович",
                Role = UserRole.Admin,
                PasswordHash = HashPassword("admin123"),
                CreatedAt = DateTime.Now.AddMonths(-3)
            };

            var head = new User
            {
                Username = "head",
                FullName = "Каримова Нигора Далеровна",
                Role = UserRole.HeadOfLab,
                PasswordHash = HashPassword("head123"),
                CreatedAt = DateTime.Now.AddMonths(-3)
            };

            var labAssistant = new User
            {
                Username = "lab",
                FullName = "Саидов Фарход Бахтиёрович",
                Role = UserRole.LabAssistant,
                PasswordHash = HashPassword("lab123"),
                CreatedAt = DateTime.Now.AddMonths(-2)
            };

            var viewer = new User
            {
                Username = "viewer",
                FullName = "Назаров Далер Икромович",
                Role = UserRole.Viewer,
                PasswordHash = HashPassword("viewer123"),
                CreatedAt = DateTime.Now.AddMonths(-1)
            };

            context.Users.AddRange(admin, head, labAssistant, viewer);
            context.SaveChanges();

            // 3. Units
            var unitPcs = new Unit { Name = "Штука", ShortName = "шт." };
            var unitMl = new Unit { Name = "Миллилитр", ShortName = "мл" };
            var unitL = new Unit { Name = "Литр", ShortName = "л" };
            var unitG = new Unit { Name = "Грамм", ShortName = "г" };
            var unitKg = new Unit { Name = "Килограмм", ShortName = "кг" };
            var unitPack = new Unit { Name = "Упаковка", ShortName = "упак." };
            var unitKit = new Unit { Name = "Набор", ShortName = "набор" };
            var unitPair = new Unit { Name = "Пара", ShortName = "пара" };

            context.Units.AddRange(unitPcs, unitMl, unitL, unitG, unitKg, unitPack, unitKit, unitPair);
            context.SaveChanges();

            // 4. Categories
            var catTubes = new Category { Name = "Пробирки вакуумные", Description = "Пробирки для забора венозной и капиллярной крови" };
            var catReagentsBio = new Category { Name = "Реагенты биохимические", Description = "Наборы реагентов для биохимических анализаторов" };
            var catReagentsHem = new Category { Name = "Реагенты гематологические", Description = "Дилюенты, лизирующие растворы, промывки" };
            var catConsumables = new Category { Name = "Перчатки и расходники", Description = "Средства защиты, спиртовые салфетки, жгуты" };
            var catTips = new Category { Name = "Наконечники дозаторов", Description = "Наконечники для микропипеток различных объемов" };
            var catRapidTests = new Category { Name = "Экспресс тест-системы", Description = "Тест-полоски и кассетные тесты" };

            context.Categories.AddRange(catTubes, catReagentsBio, catReagentsHem, catConsumables, catTips, catRapidTests);
            context.SaveChanges();

            // 5. Suppliers
            var sup1 = new Supplier
            {
                Name = "ОсОО «МедСнаб Трейд»",
                ContactPerson = "Исмоилов Комрон",
                Phone = "+992 900 11 2233",
                Email = "sales@medsnab.tj",
                Address = "г. Душанбе, пр. Рудаки 45",
                Notes = "Основной поставщик вакуумных пробирок и дозаторов"
            };
            var sup2 = new Supplier
            {
                Name = "ООО «Диагностика Плюс»",
                ContactPerson = "Шарипова Мадина",
                Phone = "+992 918 33 4455",
                Email = "info@diagplus.tj",
                Address = "г. Душанбе, ул. Айни 12",
                Notes = "Поставщик реагентов для биохимии и гематологии"
            };
            var sup3 = new Supplier
            {
                Name = "ЗАО «ФармРеагент»",
                ContactPerson = "Юсупов Азиз",
                Phone = "+992 935 55 6677",
                Email = "orders@pharmreagent.tj",
                Address = "г. Худжанд, ул. Ленина 80",
                Notes = "Экспресс-тесты и защитные средства"
            };

            context.Suppliers.AddRange(sup1, sup2, sup3);
            context.SaveChanges();

            // 6. Locations
            context.Locations.AddRange(
                new Location { Name = "Склад: Стеллаж 1, Полка А", Description = "Пробирки и емкости" },
                new Location { Name = "Склад: Стеллаж 2, Полка Б", Description = "Расходные материалы и перчатки" },
                new Location { Name = "Холодильник биохимии (+2...+8°C)", Description = "Температурно-зависимые реагенты" },
                new Location { Name = "Морозильная камера (-20°C)", Description = "Контрольные сыворотки и калибраторы" },
                new Location { Name = "Лабораторная комната №1", Description = "Оперативный запас у анализаторов" }
            );
            context.SaveChanges();

            // 7. Materials & Lots
            var mat1 = new Material
            {
                Name = "Пробирка EDTA K3 4 мл (фиолетовая крышка)",
                Category = catTubes,
                Unit = unitPcs,
                Article = "EDTA-K3-4ML",
                Barcode = "4820011223344",
                MinQuantity = 200,
                MaxQuantity = 2000,
                Supplier = sup1,
                StorageLocation = "Склад: Стеллаж 1, Полка А",
                Description = "Для общего анализа крови (гематология)"
            };

            var mat2 = new Material
            {
                Name = "Пробирка с активатором свертывания 5 мл (красная)",
                Category = catTubes,
                Unit = unitPcs,
                Article = "CLOT-ACT-5ML",
                Barcode = "4820011223351",
                MinQuantity = 150,
                MaxQuantity = 1500,
                Supplier = sup1,
                StorageLocation = "Склад: Стеллаж 1, Полка А",
                Description = "Для биохимических и серологических исследований"
            };

            var mat3 = new Material
            {
                Name = "Наконечники для дозаторов 10-100 мкл (желтые)",
                Category = catTips,
                Unit = unitPcs,
                Article = "TIP-100-YEL",
                Barcode = "4820011223368",
                MinQuantity = 500,
                MaxQuantity = 5000,
                Supplier = sup1,
                StorageLocation = "Склад: Стеллаж 1, Полка Б",
                Description = "Универсальные полипропиленовые наконечники"
            };

            var mat4 = new Material
            {
                Name = "Наконечники для дозаторов 100-1000 мкл (синие)",
                Category = catTips,
                Unit = unitPcs,
                Article = "TIP-1000-BLU",
                Barcode = "4820011223375",
                MinQuantity = 300,
                MaxQuantity = 3000,
                Supplier = sup1,
                StorageLocation = "Склад: Стеллаж 1, Полка Б",
                Description = "Наконечники большого объема"
            };

            var mat5 = new Material
            {
                Name = "Перчатки нитриловые неопудренные (р-р M)",
                Category = catConsumables,
                Unit = unitPair,
                Article = "GLV-NITR-M",
                Barcode = "4820011223382",
                MinQuantity = 100,
                MaxQuantity = 1000,
                Supplier = sup3,
                StorageLocation = "Склад: Стеллаж 2, Полка Б",
                Description = "Медицинские смотровые перчатки"
            };

            var mat6 = new Material
            {
                Name = "Перчатки нитриловые неопудренные (р-р L)",
                Category = catConsumables,
                Unit = unitPair,
                Article = "GLV-NITR-L",
                Barcode = "4820011223399",
                MinQuantity = 100,
                MaxQuantity = 1000,
                Supplier = sup3,
                StorageLocation = "Склад: Стеллаж 2, Полка Б",
                Description = "Медицинские смотровые перчатки размер L (Низкий запас!)"
            };

            var mat7 = new Material
            {
                Name = "Набор реагентов Глюкоза-ФОТО (5x100 мл)",
                Category = catReagentsBio,
                Unit = unitKit,
                Article = "REAG-GLU-500",
                Barcode = "4820011223405",
                MinQuantity = 5,
                MaxQuantity = 30,
                Supplier = sup2,
                StorageLocation = "Холодильник биохимии (+2...+8°C)",
                Description = "Ферментативный колориметрический метод (GOD-PAP)"
            };

            var mat8 = new Material
            {
                Name = "Набор реагентов Холестерин общий (4x50 мл)",
                Category = catReagentsBio,
                Unit = unitKit,
                Article = "REAG-CHOL-200",
                Barcode = "4820011223412",
                MinQuantity = 5,
                MaxQuantity = 25,
                Supplier = sup2,
                StorageLocation = "Холодильник биохимии (+2...+8°C)",
                Description = "Определение холестерина в сыворотке (Критический остаток!)"
            };

            var mat9 = new Material
            {
                Name = "Дилюент гематологический (20 л)",
                Category = catReagentsHem,
                Unit = unitPcs,
                Article = "DIL-HEM-20L",
                Barcode = "4820011223429",
                MinQuantity = 2,
                MaxQuantity = 10,
                Supplier = sup2,
                StorageLocation = "Склад: Стеллаж 1, Полка А",
                Description = "Изотонический раствор для гемоанализатора"
            };

            var mat10 = new Material
            {
                Name = "Экспресс-тест на Тропонин I (кассеты)",
                Category = catRapidTests,
                Unit = unitPcs,
                Article = "TEST-TROP-I",
                Barcode = "4820011223436",
                MinQuantity = 25,
                MaxQuantity = 100,
                Supplier = sup3,
                StorageLocation = "Склад: Стеллаж 2, Полка Б",
                Description = "Срочная диагностика острого инфаркта миокарда (Истекает срок!)"
            };

            context.Materials.AddRange(mat1, mat2, mat3, mat4, mat5, mat6, mat7, mat8, mat9, mat10);
            context.SaveChanges();

            // 8. Lots for materials
            var now = DateTime.Now;
            var lot1 = new Lot
            {
                Material = mat1,
                LotNumber = "ED260927",
                InitialQuantity = 1000,
                CurrentQuantity = 920,
                ReceivedDate = now.AddDays(-20),
                ExpiryDate = now.AddYears(2),
                PurchasePrice = 0.85m,
                Supplier = sup1,
                StorageLocation = mat1.StorageLocation
            };

            var lot2 = new Lot
            {
                Material = mat2,
                LotNumber = "CL260910",
                InitialQuantity = 600,
                CurrentQuantity = 540,
                ReceivedDate = now.AddDays(-15),
                ExpiryDate = now.AddMonths(18),
                PurchasePrice = 0.95m,
                Supplier = sup1,
                StorageLocation = mat2.StorageLocation
            };

            var lot3 = new Lot
            {
                Material = mat3,
                LotNumber = "TP100-26A",
                InitialQuantity = 3000,
                CurrentQuantity = 2450,
                ReceivedDate = now.AddDays(-25),
                ExpiryDate = now.AddYears(3),
                PurchasePrice = 0.12m,
                Supplier = sup1,
                StorageLocation = mat3.StorageLocation
            };

            var lot4 = new Lot
            {
                Material = mat4,
                LotNumber = "TP1000-26B",
                InitialQuantity = 1500,
                CurrentQuantity = 1200,
                ReceivedDate = now.AddDays(-25),
                ExpiryDate = now.AddYears(3),
                PurchasePrice = 0.18m,
                Supplier = sup1,
                StorageLocation = mat4.StorageLocation
            };

            var lot5 = new Lot
            {
                Material = mat5,
                LotNumber = "GLV-M-884",
                InitialQuantity = 500,
                CurrentQuantity = 380,
                ReceivedDate = now.AddDays(-18),
                ExpiryDate = now.AddYears(2),
                PurchasePrice = 1.40m,
                Supplier = sup3,
                StorageLocation = mat5.StorageLocation
            };

            var lot6 = new Lot
            {
                Material = mat6, // Low stock: 65 pcs, min 100
                LotNumber = "GLV-L-885",
                InitialQuantity = 200,
                CurrentQuantity = 65,
                ReceivedDate = now.AddDays(-35),
                ExpiryDate = now.AddYears(2),
                PurchasePrice = 1.40m,
                Supplier = sup3,
                StorageLocation = mat6.StorageLocation
            };

            var lot7 = new Lot
            {
                Material = mat7,
                LotNumber = "GLU-2601",
                InitialQuantity = 12,
                CurrentQuantity = 9,
                ReceivedDate = now.AddDays(-10),
                ExpiryDate = now.AddMonths(10),
                PurchasePrice = 85.00m,
                Supplier = sup2,
                StorageLocation = mat7.StorageLocation
            };

            var lot8 = new Lot
            {
                Material = mat8, // Critical stock: 2 kits, min 5
                LotNumber = "CHOL-2509",
                InitialQuantity = 8,
                CurrentQuantity = 2,
                ReceivedDate = now.AddDays(-40),
                ExpiryDate = now.AddMonths(6),
                PurchasePrice = 110.00m,
                Supplier = sup2,
                StorageLocation = mat8.StorageLocation
            };

            var lot9 = new Lot
            {
                Material = mat9,
                LotNumber = "DIL-2604",
                InitialQuantity = 6,
                CurrentQuantity = 4,
                ReceivedDate = now.AddDays(-30),
                ExpiryDate = now.AddMonths(14),
                PurchasePrice = 145.00m,
                Supplier = sup2,
                StorageLocation = mat9.StorageLocation
            };

            var lot10 = new Lot
            {
                Material = mat10, // Expiring soon: in 14 days!
                LotNumber = "TRP-25-EXP",
                InitialQuantity = 30,
                CurrentQuantity = 18,
                ReceivedDate = now.AddMonths(-11),
                ExpiryDate = now.AddDays(14),
                PurchasePrice = 45.00m,
                Supplier = sup3,
                StorageLocation = mat10.StorageLocation
            };

            context.Lots.AddRange(lot1, lot2, lot3, lot4, lot5, lot6, lot7, lot8, lot9, lot10);
            context.SaveChanges();

            // 9. Initial Movements
            var mvt1 = new StockMovement
            {
                Date = now.AddDays(-20),
                Material = mat1,
                Lot = lot1,
                OperationType = MovementType.Receipt,
                Quantity = 1000,
                BalanceAfter = 1000,
                User = admin,
                ReferenceNumber = "ПН-00124",
                Notes = "Первичное поступление партии"
            };
            var mvt1_out = new StockMovement
            {
                Date = now.AddDays(-5),
                Material = mat1,
                Lot = lot1,
                OperationType = MovementType.AnalysisRun,
                Quantity = -80,
                BalanceAfter = 920,
                User = labAssistant,
                ReferenceNumber = "АНАЛИЗ #101",
                Notes = "Списание по анализам ОАК (80 шт.)"
            };

            context.StockMovements.AddRange(mvt1, mvt1_out);
            context.SaveChanges();

            // 10. Analyses with consumption rates (Нормы расхода)
            var analysisOak = new Analysis
            {
                Code = "ОАК-01",
                Name = "Общий анализ крови развернутый (ОАК)",
                Category = "Гематология",
                Description = "Исследование форменных элементов крови на автоматическом анализаторе",
                Price = 35.00m,
                IsActive = true
            };
            var analysisGlu = new Analysis
            {
                Code = "БХ-ГЛЮК",
                Name = "Глюкоза в сыворотке крови",
                Category = "Биохимия",
                Description = "Количественное определение глюкозы ферментативным методом",
                Price = 20.00m,
                IsActive = true
            };
            var analysisChol = new Analysis
            {
                Code = "БХ-ХОЛ",
                Name = "Холестерин общий в сыворотке крови",
                Category = "Биохимия",
                Description = "Определение уровня общего холестерина",
                Price = 25.00m,
                IsActive = true
            };
            var analysisTrop = new Analysis
            {
                Code = "ЭКС-ТРОП",
                Name = "Экспресс-тест: Тропонин I (кардиомаркер)",
                Category = "Экспресс-диагностика",
                Description = "Качественное экспресс-определение кардиального тропонина I",
                Price = 75.00m,
                IsActive = true
            };

            context.Analyses.AddRange(analysisOak, analysisGlu, analysisChol, analysisTrop);
            context.SaveChanges();

            // Recipes / Norms
            // ОАК: 1 пробирка EDTA, 1 пара перчаток M, 2 наконечника 10-100 мкл
            context.AnalysisMaterials.AddRange(
                new AnalysisMaterial { Analysis = analysisOak, Material = mat1, RequiredQuantity = 1 },
                new AnalysisMaterial { Analysis = analysisOak, Material = mat5, RequiredQuantity = 1 },
                new AnalysisMaterial { Analysis = analysisOak, Material = mat3, RequiredQuantity = 2 },

                // Глюкоза: 1 пробирка с активатором, 1 пара перчаток M, 1 наконечник 100-1000
                new AnalysisMaterial { Analysis = analysisGlu, Material = mat2, RequiredQuantity = 1 },
                new AnalysisMaterial { Analysis = analysisGlu, Material = mat5, RequiredQuantity = 1 },
                new AnalysisMaterial { Analysis = analysisGlu, Material = mat4, RequiredQuantity = 1 },

                // Холестерин: 1 пробирка с активатором, 1 пара перчаток M, 1 наконечник 100-1000
                new AnalysisMaterial { Analysis = analysisChol, Material = mat2, RequiredQuantity = 1 },
                new AnalysisMaterial { Analysis = analysisChol, Material = mat5, RequiredQuantity = 1 },
                new AnalysisMaterial { Analysis = analysisChol, Material = mat4, RequiredQuantity = 1 },

                // Тропонин: 1 пробирка EDTA, 1 экспресс-тест, 1 пара перчаток M
                new AnalysisMaterial { Analysis = analysisTrop, Material = mat10, RequiredQuantity = 1 },
                new AnalysisMaterial { Analysis = analysisTrop, Material = mat1, RequiredQuantity = 1 },
                new AnalysisMaterial { Analysis = analysisTrop, Material = mat5, RequiredQuantity = 1 }
            );
            context.SaveChanges();

            // 11. Initial Notifications
            context.Notifications.AddRange(
                new Notification
                {
                    Title = "Низкий остаток",
                    Message = $"Материал «{mat6.Name}» достиг остатка 65 пар (порог {mat6.MinQuantity}). Рекомендуется оформить заявку поставщику.",
                    Type = NotificationType.LowStock,
                    Severity = "Warning",
                    Material = mat6,
                    CreatedAt = now.AddHours(-3),
                    IsRead = false
                },
                new Notification
                {
                    Title = "Критический остаток!",
                    Message = $"Материал «{mat8.Name}» имеет критический остаток: всего 2 набора (порог {mat8.MinQuantity}).",
                    Type = NotificationType.CriticalStock,
                    Severity = "Danger",
                    Material = mat8,
                    CreatedAt = now.AddHours(-2),
                    IsRead = false
                },
                new Notification
                {
                    Title = "Истекает срок годности!",
                    Message = $"Партия {lot10.LotNumber} материала «{mat10.Name}» истекает через 14 дней ({lot10.ExpiryDate:dd.MM.yyyy}).",
                    Type = NotificationType.ExpiringSoon,
                    Severity = "Warning",
                    Material = mat10,
                    Lot = lot10,
                    CreatedAt = now.AddHours(-1),
                    IsRead = false
                }
            );

            // 12. Audit log
            context.AuditLogs.Add(new AuditLog
            {
                Timestamp = now,
                User = admin,
                Action = "Инициализация",
                EntityName = "Система",
                Details = "База данных успешно инициализирована с начальным набором лабораторных данных."
            });

            context.SaveChanges();
            }
        }
    }
}
