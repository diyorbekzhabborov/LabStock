using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using LabStock.Data;
using LabStock.Models;
using LabStock.Services;
using Microsoft.EntityFrameworkCore;

namespace LabStock.ViewModels
{
    public class InventoryAuditItemDto : ViewModelBase
    {
        public int MaterialId { get; set; }
        public string MaterialName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string UnitName { get; set; } = string.Empty;
        public string StorageLocation { get; set; } = string.Empty;
        public decimal SystemQuantity { get; set; }

        private decimal _actualQuantity;
        private string? _reason;

        public decimal ActualQuantity
        {
            get => _actualQuantity;
            set
            {
                if (SetProperty(ref _actualQuantity, value))
                {
                    OnPropertyChanged(nameof(Difference));
                    OnPropertyChanged(nameof(HasDiscrepancy));
                }
            }
        }

        public decimal Difference => ActualQuantity - SystemQuantity;
        public bool HasDiscrepancy => Difference != 0;

        public string? Reason
        {
            get => _reason;
            set => SetProperty(ref _reason, value);
        }
    }

    public class InventoryCheckViewModel : ViewModelBase
    {
        private ObservableCollection<InventoryAuditItemDto> _items = new();
        private InventoryAuditItemDto? _selectedItem;
        private string? _statusMessage;
        private bool _isSuccess;

        public ObservableCollection<InventoryAuditItemDto> Items
        {
            get => _items;
            set => SetProperty(ref _items, value);
        }

        public InventoryAuditItemDto? SelectedItem
        {
            get => _selectedItem;
            set => SetProperty(ref _selectedItem, value);
        }

        public string? StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public bool IsSuccess
        {
            get => _isSuccess;
            set => SetProperty(ref _isSuccess, value);
        }

        public ICommand RefreshCommand { get; }
        public ICommand ApplyAdjustmentCommand { get; }

        public InventoryCheckViewModel()
        {
            RefreshCommand = new RelayCommand(LoadData);
            ApplyAdjustmentCommand = new RelayCommand(ApplyAdjustment, () => SelectedItem != null && SelectedItem.HasDiscrepancy);

            LoadData();
        }

        public void LoadData()
        {
            using var db = new AppDbContext();
            var mats = db.Materials
                .Include(m => m.Category)
                .Include(m => m.Unit)
                .Include(m => m.Lots)
                .Where(m => m.IsActive)
                .OrderBy(m => m.Name)
                .ToList();

            var list = mats.Select(m =>
            {
                decimal stock = m.Lots.Where(l => !l.IsArchived).Sum(l => l.CurrentQuantity);
                return new InventoryAuditItemDto
                {
                    MaterialId = m.Id,
                    MaterialName = m.Name,
                    CategoryName = m.Category?.Name ?? "",
                    UnitName = m.Unit?.ShortName ?? "шт.",
                    StorageLocation = m.StorageLocation ?? "Склад",
                    SystemQuantity = stock,
                    ActualQuantity = stock,
                    Reason = "Сверка остатков"
                };
            }).ToList();

            Items = new ObservableCollection<InventoryAuditItemDto>(list);
            SelectedItem = Items.FirstOrDefault();
        }

        private void ApplyAdjustment()
        {
            if (SelectedItem == null || !SelectedItem.HasDiscrepancy) return;

            if (string.IsNullOrWhiteSpace(SelectedItem.Reason))
            {
                StatusMessage = "Для сохранения расхождения обязательно укажите причину (например, брак упаковки, пересортица, бой и т.д.)";
                IsSuccess = false;
                return;
            }

            try
            {
                InventoryService.Instance.PerformInventoryAdjustment(
                    SelectedItem.MaterialId,
                    SelectedItem.ActualQuantity,
                    SelectedItem.Reason
                );

                StatusMessage = $"Корректировка успешно проведена для «{SelectedItem.MaterialName}». Системный остаток скорректирован на {SelectedItem.Difference:+0.##;-0.##;0}.";
                IsSuccess = true;
                LoadData();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка корректировки: {ex.Message}";
                IsSuccess = false;
            }
        }
    }
}
