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
    public class ReceiptItemDraft : ViewModelBase
    {
        private int _materialId;
        private string _lotNumber = string.Empty;
        private decimal _quantity = 100;
        private decimal _price = 1.0m;
        private DateTime _expiryDate = DateTime.Today.AddYears(2);
        private string? _storageLocation;

        public int MaterialId
        {
            get => _materialId;
            set => SetProperty(ref _materialId, value);
        }

        public string LotNumber
        {
            get => _lotNumber;
            set => SetProperty(ref _lotNumber, value);
        }

        public decimal Quantity
        {
            get => _quantity;
            set => SetProperty(ref _quantity, value);
        }

        public decimal Price
        {
            get => _price;
            set => SetProperty(ref _price, value);
        }

        public DateTime ExpiryDate
        {
            get => _expiryDate;
            set => SetProperty(ref _expiryDate, value);
        }

        public string? StorageLocation
        {
            get => _storageLocation;
            set => SetProperty(ref _storageLocation, value);
        }

        public decimal Total => Quantity * Price;
    }

    public class ReceiptsViewModel : ViewModelBase
    {
        private ObservableCollection<Receipt> _receipts = new();
        private Receipt? _selectedReceipt;
        private bool _isCreateOpen = false;

        // New receipt draft
        private string _receiptNumber = string.Empty;
        private DateTime _receiptDate = DateTime.Today;
        private int? _selectedSupplierId;
        private string? _notes;
        private ObservableCollection<ReceiptItemDraft> _draftItems = new();

        public ObservableCollection<Receipt> Receipts
        {
            get => _receipts;
            set => SetProperty(ref _receipts, value);
        }

        public Receipt? SelectedReceipt
        {
            get => _selectedReceipt;
            set => SetProperty(ref _selectedReceipt, value);
        }

        public bool IsCreateOpen
        {
            get => _isCreateOpen;
            set => SetProperty(ref _isCreateOpen, value);
        }

        public string ReceiptNumber
        {
            get => _receiptNumber;
            set => SetProperty(ref _receiptNumber, value);
        }

        public DateTime ReceiptDate
        {
            get => _receiptDate;
            set => SetProperty(ref _receiptDate, value);
        }

        public int? SelectedSupplierId
        {
            get => _selectedSupplierId;
            set => SetProperty(ref _selectedSupplierId, value);
        }

        public string? Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }

        public ObservableCollection<ReceiptItemDraft> DraftItems
        {
            get => _draftItems;
            set => SetProperty(ref _draftItems, value);
        }

        public ObservableCollection<Supplier> Suppliers { get; } = new();
        public ObservableCollection<Material> Materials { get; } = new();

        public ICommand RefreshCommand { get; }
        public ICommand OpenCreateCommand { get; }
        public ICommand AddDraftItemCommand { get; }
        public ICommand RemoveDraftItemCommand { get; }
        public ICommand SaveReceiptCommand { get; }
        public ICommand CancelCreateCommand { get; }

        public ReceiptsViewModel()
        {
            RefreshCommand = new RelayCommand(LoadReceipts);
            OpenCreateCommand = new RelayCommand(OpenCreate);
            AddDraftItemCommand = new RelayCommand(AddDraftItem);
            RemoveDraftItemCommand = new RelayCommand(item =>
            {
                if (item is ReceiptItemDraft d) DraftItems.Remove(d);
            });
            SaveReceiptCommand = new RelayCommand(SaveReceipt);
            CancelCreateCommand = new RelayCommand(() => IsCreateOpen = false);

            LoadLookups();
            LoadReceipts();
        }

        public void LoadLookups()
        {
            using var db = new AppDbContext();
            Suppliers.Clear();
            foreach (var s in db.Suppliers.OrderBy(s => s.Name)) Suppliers.Add(s);

            Materials.Clear();
            foreach (var m in db.Materials.Where(m => m.IsActive).OrderBy(m => m.Name)) Materials.Add(m);
        }

        public void LoadReceipts()
        {
            using var db = new AppDbContext();
            var list = db.Receipts
                .Include(r => r.Supplier)
                .Include(r => r.User)
                .Include(r => r.Items)
                .ThenInclude(i => i.Material)
                .ThenInclude(m => m!.Unit)
                .OrderByDescending(r => r.Date)
                .ToList();

            Receipts = new ObservableCollection<Receipt>(list);
            SelectedReceipt = Receipts.FirstOrDefault();
        }

        private void OpenCreate()
        {
            LoadLookups();
            ReceiptNumber = $"ПН-{DateTime.Now:yyMMdd-HHmm}";
            ReceiptDate = DateTime.Today;
            SelectedSupplierId = Suppliers.FirstOrDefault()?.Id;
            Notes = string.Empty;
            DraftItems.Clear();

            AddDraftItem();
            IsCreateOpen = true;
        }

        private void AddDraftItem()
        {
            var firstMat = Materials.FirstOrDefault();
            DraftItems.Add(new ReceiptItemDraft
            {
                MaterialId = firstMat?.Id ?? 1,
                LotNumber = $"П-{DateTime.Now:yyMMddHHmm}",
                Quantity = 100,
                Price = 1.0m,
                ExpiryDate = DateTime.Today.AddYears(2),
                StorageLocation = firstMat?.StorageLocation ?? "Склад: Стеллаж 1"
            });
        }

        private void SaveReceipt()
        {
            if (string.IsNullOrWhiteSpace(ReceiptNumber) || !DraftItems.Any()) return;

            var receipt = new Receipt
            {
                ReceiptNumber = ReceiptNumber.Trim(),
                Date = ReceiptDate,
                SupplierId = SelectedSupplierId,
                Notes = Notes?.Trim()
            };

            var items = DraftItems.Select(d => new ReceiptItem
            {
                MaterialId = d.MaterialId,
                LotNumber = string.IsNullOrWhiteSpace(d.LotNumber) ? $"П-{DateTime.Now:yyMMddHHmm}" : d.LotNumber.Trim(),
                Quantity = d.Quantity,
                Price = d.Price,
                ExpiryDate = d.ExpiryDate,
                StorageLocation = d.StorageLocation
            }).ToList();

            InventoryService.Instance.RegisterReceipt(receipt, items);
            IsCreateOpen = false;
            LoadReceipts();
        }
    }
}
