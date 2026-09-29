using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LabStock.Models
{
    public class User
    {
        public int Id { get; set; }
        [Required] public string Username { get; set; } = string.Empty;
        [Required] public string PasswordHash { get; set; } = string.Empty;
        [Required] public string FullName { get; set; } = string.Empty;
        public UserRole Role { get; set; } = UserRole.LabAssistant;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class Category
    {
        public int Id { get; set; }
        [Required] public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public ICollection<Material> Materials { get; set; } = new List<Material>();
    }

    public class Unit
    {
        public int Id { get; set; }
        [Required] public string Name { get; set; } = string.Empty;
        [Required] public string ShortName { get; set; } = string.Empty; // шт., мл, л, г, кг, упаковка, набор, пара
    }

    public class Supplier
    {
        public int Id { get; set; }
        [Required] public string Name { get; set; } = string.Empty;
        public string? ContactPerson { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? Notes { get; set; }
        public ICollection<Material> Materials { get; set; } = new List<Material>();
    }

    public class Location
    {
        public int Id { get; set; }
        [Required] public string Name { get; set; } = string.Empty; // Склад, стеллаж, полка
        public string? Description { get; set; }
    }

    public class Material
    {
        public int Id { get; set; }
        [Required] public string Name { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        public int UnitId { get; set; }
        public Unit? Unit { get; set; }

        public string? Article { get; set; } // Внутренний / заводской код
        public string? Barcode { get; set; } // Штрихкод / QR

        public decimal MinQuantity { get; set; } = 10; // Порог предупреждения
        public decimal MaxQuantity { get; set; } = 100; // Рекомендуемый верхний уровень

        public int? SupplierId { get; set; }
        public Supplier? Supplier { get; set; }

        public string? StorageLocation { get; set; } // Склад / стеллаж / полка
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;

        public ICollection<Lot> Lots { get; set; } = new List<Lot>();
        public ICollection<AnalysisMaterial> AnalysisMaterials { get; set; } = new List<AnalysisMaterial>();
        public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();

        [NotMapped]
        public decimal TotalStock { get; set; }

        [NotMapped]
        public StockStatus Status
        {
            get
            {
                if (TotalStock <= 0) return StockStatus.OutOfStock;
                if (TotalStock <= MinQuantity) return StockStatus.Low;
                if (TotalStock <= MinQuantity * 1.25m) return StockStatus.Attention;
                return StockStatus.Normal;
            }
        }
    }

    public class Lot
    {
        public int Id { get; set; }
        public int MaterialId { get; set; }
        public Material? Material { get; set; }

        [Required] public string LotNumber { get; set; } = string.Empty; // Например ED260927
        public decimal InitialQuantity { get; set; }
        public decimal CurrentQuantity { get; set; }
        public DateTime ReceivedDate { get; set; } = DateTime.Now;
        public DateTime ExpiryDate { get; set; } = DateTime.Now.AddYears(1);
        public decimal PurchasePrice { get; set; } // Закупочная цена (в сомони)

        public int? SupplierId { get; set; }
        public Supplier? Supplier { get; set; }

        public string? StorageLocation { get; set; }
        public bool IsArchived { get; set; } = false;

        [NotMapped]
        public bool IsExpired => ExpiryDate.Date < DateTime.Today;

        [NotMapped]
        public int DaysToExpiry => (ExpiryDate.Date - DateTime.Today).Days;
    }

    public class Analysis
    {
        public int Id { get; set; }
        [Required] public string Code { get; set; } = string.Empty; // Например ОАК-01
        [Required] public string Name { get; set; } = string.Empty; // Например: Общий анализ крови (ОАК)
        public string? Category { get; set; } // Гематология, Биохимия, Иммунология, и т.д.
        public string? Description { get; set; }
        public decimal Price { get; set; } // Стоимость анализа
        public bool IsActive { get; set; } = true;

        public ICollection<AnalysisMaterial> Materials { get; set; } = new List<AnalysisMaterial>();
        public ICollection<AnalysisRun> Runs { get; set; } = new List<AnalysisRun>();
    }

    public class AnalysisMaterial
    {
        public int Id { get; set; }
        public int AnalysisId { get; set; }
        public Analysis? Analysis { get; set; }

        public int MaterialId { get; set; }
        public Material? Material { get; set; }

        public decimal RequiredQuantity { get; set; } // Норма расхода на 1 анализ
    }

    public class AnalysisRun
    {
        public int Id { get; set; }
        public string RunNumber { get; set; } = string.Empty; // Номер проводки / выполнения
        public int AnalysisId { get; set; }
        public Analysis? Analysis { get; set; }

        public DateTime RunDate { get; set; } = DateTime.Now;
        public int AnalysisCount { get; set; } // Количество выполненных анализов
        public int UserId { get; set; }
        public User? User { get; set; }

        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
    }

    public class Receipt
    {
        public int Id { get; set; }
        [Required] public string ReceiptNumber { get; set; } = string.Empty; // Номер накладной
        public DateTime Date { get; set; } = DateTime.Now;
        public int? SupplierId { get; set; }
        public Supplier? Supplier { get; set; }
        public decimal TotalAmount { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        public string? Notes { get; set; }

        public ICollection<ReceiptItem> Items { get; set; } = new List<ReceiptItem>();
    }

    public class ReceiptItem
    {
        public int Id { get; set; }
        public int ReceiptId { get; set; }
        public Receipt? Receipt { get; set; }

        public int MaterialId { get; set; }
        public Material? Material { get; set; }

        public int? LotId { get; set; }
        public Lot? Lot { get; set; }

        [Required] public string LotNumber { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string? StorageLocation { get; set; }
    }

    public class Writeoff
    {
        public int Id { get; set; }
        [Required] public string WriteoffNumber { get; set; } = string.Empty;
        public DateTime Date { get; set; } = DateTime.Now;
        public int MaterialId { get; set; }
        public Material? Material { get; set; }
        public int? LotId { get; set; }
        public Lot? Lot { get; set; }
        public decimal Quantity { get; set; }
        [Required] public string Reason { get; set; } = string.Empty; // Повреждение, брак, истечение срока, потеря, контроль качества, внутреннее использование, другое
        public int UserId { get; set; }
        public User? User { get; set; }
        public string? Notes { get; set; }
    }

    public class StockMovement
    {
        public int Id { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;

        public int MaterialId { get; set; }
        public Material? Material { get; set; }

        public int? LotId { get; set; }
        public Lot? Lot { get; set; }

        public MovementType OperationType { get; set; }
        public decimal Quantity { get; set; } // + для поступлений, - для списаний
        public decimal BalanceAfter { get; set; } // Остаток после операции

        public int UserId { get; set; }
        public User? User { get; set; }

        public int? AnalysisRunId { get; set; }
        public AnalysisRun? AnalysisRun { get; set; }

        public string? ReferenceNumber { get; set; } // Накладная, Акт, Анализ
        public string? Notes { get; set; }
    }

    public class InventoryCheck
    {
        public int Id { get; set; }
        [Required] public string CheckNumber { get; set; } = string.Empty;
        public DateTime Date { get; set; } = DateTime.Now;
        public int UserId { get; set; }
        public User? User { get; set; }
        public string Status { get; set; } = "Завершено"; // Черновик, Завершено
        public string? Notes { get; set; }

        public ICollection<InventoryCheckItem> Items { get; set; } = new List<InventoryCheckItem>();
    }

    public class InventoryCheckItem
    {
        public int Id { get; set; }
        public int InventoryCheckId { get; set; }
        public InventoryCheck? InventoryCheck { get; set; }

        public int MaterialId { get; set; }
        public Material? Material { get; set; }

        public decimal SystemQuantity { get; set; }
        public decimal ActualQuantity { get; set; }
        public decimal Difference { get; set; } // Actual - System
        public string? Reason { get; set; } // Обязательная причина при расхождении
    }

    public class Notification
    {
        public int Id { get; set; }
        [Required] public string Title { get; set; } = string.Empty;
        [Required] public string Message { get; set; } = string.Empty;
        public NotificationType Type { get; set; }
        public string Severity { get; set; } = "Warning"; // Info, Warning, Danger
        public int? MaterialId { get; set; }
        public Material? Material { get; set; }
        public int? LotId { get; set; }
        public Lot? Lot { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsRead { get; set; } = false;
    }

    public class AuditLog
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public int? UserId { get; set; }
        public User? User { get; set; }
        public string Action { get; set; } = string.Empty; // Вход, Выход, Поступление, Списание, Выполнение анализа, Корректировка, Изменение справочника, Backup, Restore
        public string EntityName { get; set; } = string.Empty;
        public string? EntityId { get; set; }
        public string Details { get; set; } = string.Empty;
    }

    public class Setting
    {
        public int Id { get; set; }
        [Required] public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Category { get; set; } = "General";
    }
}
