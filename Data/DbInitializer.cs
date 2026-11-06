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

                EnsureCustomerAccountsAndItems(context);

                if (context.Users.Any(u => u.Username == "admin"))
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

        public static void EnsureCustomerAccountsAndItems(AppDbContext context)
        {
            // 1. Ensure abdullojon and darmongoh exist
            if (!context.Users.Any(u => u.Username == "abdullojon"))
            {
                context.Users.Add(new User
                {
                    Username = "abdullojon",
                    FullName = "Ҷабборов Абдуллоҷон",
                    Role = UserRole.Admin,
                    PasswordHash = HashPassword("jabborov"),
                    CreatedAt = DateTime.Now
                });
            }

            if (!context.Users.Any(u => u.Username == "darmongoh"))
            {
                context.Users.Add(new User
                {
                    Username = "darmongoh",
                    FullName = "Бухгалтер (Дармонгох)",
                    Role = UserRole.HeadOfLab,
                    PasswordHash = HashPassword("qwer1234"),
                    CreatedAt = DateTime.Now
                });
            }
            context.SaveChanges();

            // 2. Units & Category for real items
            var unitMl = context.Units.FirstOrDefault(u => u.ShortName == "мл") ?? new Unit { Name = "Миллилитр", ShortName = "мл" };
            var unitPcs = context.Units.FirstOrDefault(u => u.ShortName == "шт.") ?? new Unit { Name = "Штука", ShortName = "шт." };
            var unitPack = context.Units.FirstOrDefault(u => u.ShortName == "упак.") ?? new Unit { Name = "Упаковка", ShortName = "упак." };
            var unitStrips = context.Units.FirstOrDefault(u => u.ShortName == "тест-полоски") ?? new Unit { Name = "Тест-полоски", ShortName = "тест-полоски" };

            if (unitMl.Id == 0) context.Units.Add(unitMl);
            if (unitPcs.Id == 0) context.Units.Add(unitPcs);
            if (unitPack.Id == 0) context.Units.Add(unitPack);
            if (unitStrips.Id == 0) context.Units.Add(unitStrips);
            context.SaveChanges();

            var catBio = context.Categories.FirstOrDefault(c => c.Name.Contains("биохим") || c.Name.Contains("Реагенты"))
                ?? context.Categories.FirstOrDefault()
                ?? new Category { Name = "Клинические реагенты" };
            if (catBio.Id == 0) { context.Categories.Add(catBio); context.SaveChanges(); }

            var sup = context.Suppliers.FirstOrDefault(s => s.Name.Contains("Абдулло") || s.Name.Contains("Диагностика"))
                ?? new Supplier { Name = "Абдуллочон", ContactPerson = "Ҷабборов Абдуллоҷон" };
            if (sup.Id == 0) { context.Suppliers.Add(sup); context.SaveChanges(); }

            // 3. 42 Clinical items from backup
            var itemsData = new (string Sku, string Name, string UnitStr, decimal Stock, decimal MinQty, string ExpDate, string LotNum)[]
            {
                ("12", "Мочавина кислата", "мл", 26m, 5m, "2027-10-01", "0"),
                ("43", "Гепатит ИФА", "шт.", 54m, 5m, "2027-10-01", "0"),
                ("1", "Глюкоза", "мл", 84m, 5m, "2027-10-01", "1"),
                ("39", "ЗППП-8 намуд", "шт.", 49m, 5m, "2027-10-01", ""),
                ("2", "Холистерин", "мл", 162m, 5m, "2027-10-01", "2"),
                ("14", "Калсий", "мл", 9m, 5m, "2027-10-01", "14"),
                ("3", "Креатинин", "мл", 82m, 5m, "2027-10-01", "3"),
                ("10", "Ш. Фосфатаза", "мл", 20m, 5m, "2027-10-01", "10"),
                ("41", "Гепатит Д", "упак.", 82m, 5m, "2027-10-01", "0"),
                ("20", "ЛГ", "шт.", 55m, 5m, "2027-10-01", "0"),
                ("29", "Ляблия -Аскарида", "шт.", 62m, 5m, "2027-10-01", "0"),
                ("27", "Т3", "мл", 89m, 5m, "2027-10-01", "0"),
                ("28", "Т4", "мл", 79m, 5m, "2027-10-01", "0"),
                ("4", "Мочавина", "мл", 79m, 5m, "2027-10-01", "4"),
                ("49", "палича для мазок", "шт.", 9251m, 5m, "2027-10-02", "0"),
                ("37", "ПРОБИРКА К", "шт.", 247m, 5m, "2027-10-01", "0"),
                ("38", "Пробирка коаг", "шт.", 4m, 5m, "2027-10-01", "0"),
                ("36", "Пробиркап ф", "шт.", 54m, 5m, "2027-10-01", ""),
                ("15", "Калий", "мл", 20m, 5m, "2027-10-01", "15"),
                ("32", "СА-125", "упак.", 46m, 5m, "2027-10-01", "0"),
                ("21", "Пролактин", "шт.", 49m, 5m, "2027-10-01", "0"),
                ("16", "Фосфор", "мл", 12m, 5m, "2027-10-01", "16"),
                ("22", "Эстрадиол", "шт.", 80m, 5m, "2027-10-01", "0"),
                ("30", "Пса обш", "шт.", 39m, 5m, "2027-10-01", "0"),
                ("17", "РФ СрБ АсО", "мл", 71m, 5m, "2028-11-01", "257"),
                ("18", "АБД(гурупа)", "мл", 116m, 5m, "2027-10-01", "18"),
                ("24", "Тесестрон", "мл", 56m, 5m, "2027-10-01", "0"),
                ("35", "Тест Геаптит В С ВМНО RW", "тест-полоски", 3702m, 5m, "2027-10-01", "0"),
                ("33", "Тест моча", "тест-полоски", 83m, 5m, "2027-10-01", "0"),
                ("7", "АлТ - АсТ", "мл", 106m, 5m, "2027-10-01", "7"),
                ("5", "Албумин", "мл", 45m, 5m, "2027-10-01", "5"),
                ("40", "АМГ", "шт.", 20m, 5m, "2027-10-01", "0"),
                ("9", "Тимол", "мл", 8m, 5m, "2027-10-01", "9"),
                ("26", "ТТГ", "шт.", 96m, 5m, "2027-10-01", "0"),
                ("11", "Амилаза", "мл", 67m, 5m, "2027-10-01", "11"),
                ("13", "АЧТВ МНО ФИБРИНОГЕН", "мл", 154m, 5m, "2027-10-01", "13"),
                ("8", "Белок", "мл", 121m, 5m, "2027-10-01", "8"),
                ("19", "ФСГ", "шт.", 57m, 5m, "2027-10-01", "1"),
                ("31", "ХГЧ", "упак.", 78m, 5m, "2027-10-01", "0"),
                ("6", "Билурубин", "мл", 19m, 5m, "2027-10-01", "6"),
                ("25", "Витамин  Д", "упак.", 35m, 5m, "2027-10-01", "0"),
                ("23", "Прогестерон", "шт.", 77m, 5m, "2027-10-01", "0")
            };

            foreach (var item in itemsData)
            {
                if (context.Materials.Any(m => m.Name == item.Name)) continue;

                var u = item.UnitStr switch
                {
                    "мл" => unitMl,
                    "шт." => unitPcs,
                    "упак." => unitPack,
                    "тест-полоски" => unitStrips,
                    _ => unitPcs
                };

                var mat = new Material
                {
                    Name = item.Name,
                    Article = item.Sku,
                    Category = catBio,
                    Unit = u,
                    MinQuantity = item.MinQty,
                    MaxQuantity = item.MinQty * 10,
                    Supplier = sup,
                    StorageLocation = "Основной склад",
                    Description = "Клинический материал из базы данных лаборатории"
                };

                context.Materials.Add(mat);
                context.SaveChanges();

                DateTime expDate = DateTime.TryParse(item.ExpDate, out var dt) ? dt : DateTime.Today.AddYears(1);

                var lot = new Lot
                {
                    MaterialId = mat.Id,
                    LotNumber = string.IsNullOrWhiteSpace(item.LotNum) ? "LOT-01" : item.LotNum,
                    InitialQuantity = item.Stock,
                    CurrentQuantity = item.Stock,
                    ReceivedDate = DateTime.Now.AddDays(-30),
                    ExpiryDate = expDate,
                    PurchasePrice = 10,
                    Supplier = sup,
                    StorageLocation = "Основной склад"
                };

                context.Lots.Add(lot);
                context.SaveChanges();
            }
        }
    }
}
