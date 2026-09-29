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
    public class WriteoffsViewModel : ViewModelBase
    {
        private ObservableCollection<Writeoff> _writeoffs = new();
        private bool _isCreateOpen = false;

        // Draft
        private int _selectedMaterialId;
        private int? _selectedLotId;
        private decimal _quantity = 1;
        private string _reason = "Повреждение";
        private string? _notes;
        private string? _errorMessage;

        public ObservableCollection<Writeoff> Writeoffs
        {
            get => _writeoffs;
            set => SetProperty(ref _writeoffs, value);
        }

        public bool IsCreateOpen
        {
            get => _isCreateOpen;
            set => SetProperty(ref _isCreateOpen, value);
        }

        public int SelectedMaterialId
        {
            get => _selectedMaterialId;
            set
            {
                if (SetProperty(ref _selectedMaterialId, value))
                {
                    LoadLotsForSelectedMaterial();
                }
            }
        }

        public int? SelectedLotId
        {
            get => _selectedLotId;
            set => SetProperty(ref _selectedLotId, value);
        }

        public decimal Quantity
        {
            get => _quantity;
            set => SetProperty(ref _quantity, value);
        }

        public string Reason
        {
            get => _reason;
            set => SetProperty(ref _reason, value);
        }

        public string? Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }

        public string? ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public ObservableCollection<Material> Materials { get; } = new();
        public ObservableCollection<Lot> AvailableLots { get; } = new();
        public ObservableCollection<string> Reasons { get; } = new()
        {
            "Повреждение",
            "Брак упаковки/заводской",
            "Истечение срока годности",
            "Потеря / недостача",
            "Контроль качества (QC)",
            "Внутреннее использование",
            "Калибровка прибора",
            "Другое"
        };

        public ICommand RefreshCommand { get; }
        public ICommand OpenCreateCommand { get; }
        public ICommand SaveWriteoffCommand { get; }
        public ICommand CancelCreateCommand { get; }

        public WriteoffsViewModel()
        {
            RefreshCommand = new RelayCommand(LoadWriteoffs);
            OpenCreateCommand = new RelayCommand(OpenCreate);
            SaveWriteoffCommand = new RelayCommand(SaveWriteoff);
            CancelCreateCommand = new RelayCommand(() => IsCreateOpen = false);

            LoadWriteoffs();
        }

        public void LoadWriteoffs()
        {
            using var db = new AppDbContext();
            var list = db.Writeoffs
                .Include(w => w.Material)
                .ThenInclude(m => m!.Unit)
                .Include(w => w.Lot)
                .Include(w => w.User)
                .OrderByDescending(w => w.Date)
                .ToList();

            Writeoffs = new ObservableCollection<Writeoff>(list);
        }

        private void LoadLookups()
        {
            using var db = new AppDbContext();
            Materials.Clear();
            var mats = db.Materials
                .Include(m => m.Lots)
                .Where(m => m.IsActive)
                .OrderBy(m => m.Name)
                .ToList();

            foreach (var m in mats)
            {
                m.TotalStock = m.Lots.Where(l => !l.IsArchived).Sum(l => l.CurrentQuantity);
                Materials.Add(m);
            }
        }

        private void LoadLotsForSelectedMaterial()
        {
            AvailableLots.Clear();
            if (SelectedMaterialId <= 0) return;

            using var db = new AppDbContext();
            var lots = db.Lots
                .Where(l => l.MaterialId == SelectedMaterialId && !l.IsArchived && l.CurrentQuantity > 0)
                .OrderBy(l => l.ExpiryDate)
                .ToList();

            foreach (var l in lots) AvailableLots.Add(l);
            SelectedLotId = AvailableLots.FirstOrDefault()?.Id;
        }

        private void OpenCreate()
        {
            ErrorMessage = null;
            LoadLookups();
            SelectedMaterialId = Materials.FirstOrDefault()?.Id ?? 0;
            Quantity = 1;
            Reason = Reasons.FirstOrDefault() ?? "Повреждение";
            Notes = string.Empty;
            IsCreateOpen = true;
        }

        private void SaveWriteoff()
        {
            ErrorMessage = null;
            if (SelectedMaterialId <= 0 || Quantity <= 0)
            {
                ErrorMessage = "Выберите материал и укажите количество больше 0.";
                return;
            }

            try
            {
                var writeoff = new Writeoff
                {
                    MaterialId = SelectedMaterialId,
                    Quantity = Quantity,
                    Reason = Reason,
                    Notes = Notes?.Trim(),
                    Date = DateTime.Now
                };

                InventoryService.Instance.RegisterWriteoff(writeoff, SelectedLotId);
                IsCreateOpen = false;
                LoadWriteoffs();
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }
    }
}
