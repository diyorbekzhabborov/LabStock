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
    public class MaterialsViewModel : ViewModelBase
    {
        private ObservableCollection<Material> _materials = new();
        private Material? _selectedMaterial;
        private string _searchText = string.Empty;
        private bool _isEditorOpen = false;
        private bool _isNew = true;

        // Editor fields
        private int _editId;
        private string _editName = string.Empty;
        private int _editCategoryId;
        private int _editUnitId;
        private string? _editArticle;
        private string? _editBarcode;
        private decimal _editMinQuantity = 10;
        private decimal _editMaxQuantity = 100;
        private int? _editSupplierId;
        private string? _editStorageLocation;
        private string? _editDescription;

        // Initial stock (only for new)
        private decimal _initialStock = 0;
        private decimal _initialPrice = 0;
        private DateTime _initialExpiry = DateTime.Today.AddYears(1);

        public ObservableCollection<Material> Materials
        {
            get => _materials;
            set => SetProperty(ref _materials, value);
        }

        public Material? SelectedMaterial
        {
            get => _selectedMaterial;
            set => SetProperty(ref _selectedMaterial, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    LoadMaterials();
                }
            }
        }

        public bool IsEditorOpen
        {
            get => _isEditorOpen;
            set => SetProperty(ref _isEditorOpen, value);
        }

        public bool IsNew
        {
            get => _isNew;
            set => SetProperty(ref _isNew, value);
        }

        public string EditName
        {
            get => _editName;
            set => SetProperty(ref _editName, value);
        }

        public int EditCategoryId
        {
            get => _editCategoryId;
            set => SetProperty(ref _editCategoryId, value);
        }

        public int EditUnitId
        {
            get => _editUnitId;
            set => SetProperty(ref _editUnitId, value);
        }

        public string? EditArticle
        {
            get => _editArticle;
            set => SetProperty(ref _editArticle, value);
        }

        public string? EditBarcode
        {
            get => _editBarcode;
            set => SetProperty(ref _editBarcode, value);
        }

        public decimal EditMinQuantity
        {
            get => _editMinQuantity;
            set => SetProperty(ref _editMinQuantity, value);
        }

        public decimal EditMaxQuantity
        {
            get => _editMaxQuantity;
            set => SetProperty(ref _editMaxQuantity, value);
        }

        public int? EditSupplierId
        {
            get => _editSupplierId;
            set => SetProperty(ref _editSupplierId, value);
        }

        public string? EditStorageLocation
        {
            get => _editStorageLocation;
            set => SetProperty(ref _editStorageLocation, value);
        }

        public string? EditDescription
        {
            get => _editDescription;
            set => SetProperty(ref _editDescription, value);
        }

        public decimal InitialStock
        {
            get => _initialStock;
            set => SetProperty(ref _initialStock, value);
        }

        public decimal InitialPrice
        {
            get => _initialPrice;
            set => SetProperty(ref _initialPrice, value);
        }

        public DateTime InitialExpiry
        {
            get => _initialExpiry;
            set => SetProperty(ref _initialExpiry, value);
        }

        public ObservableCollection<Category> Categories { get; } = new();
        public ObservableCollection<Unit> Units { get; } = new();
        public ObservableCollection<Supplier> Suppliers { get; } = new();

        public ICommand RefreshCommand { get; }
        public ICommand OpenCreateCommand { get; }
        public ICommand OpenEditCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelEditCommand { get; }
        public ICommand DeleteCommand { get; }

        public MaterialsViewModel()
        {
            RefreshCommand = new RelayCommand(LoadMaterials);
            OpenCreateCommand = new RelayCommand(OpenCreate);
            OpenEditCommand = new RelayCommand(OpenEdit, () => SelectedMaterial != null);
            SaveCommand = new RelayCommand(Save);
            CancelEditCommand = new RelayCommand(() => IsEditorOpen = false);
            DeleteCommand = new RelayCommand(Delete, () => SelectedMaterial != null);

            LoadLookups();
            LoadMaterials();
        }

        public void LoadLookups()
        {
            using var db = new AppDbContext();
            Categories.Clear();
            foreach (var c in db.Categories.OrderBy(c => c.Name)) Categories.Add(c);

            Units.Clear();
            foreach (var u in db.Units.OrderBy(u => u.Name)) Units.Add(u);

            Suppliers.Clear();
            foreach (var s in db.Suppliers.OrderBy(s => s.Name)) Suppliers.Add(s);
        }

        public void LoadMaterials()
        {
            var list = InventoryService.Instance.GetAllMaterials(SearchText);
            Materials = new ObservableCollection<Material>(list);
            SelectedMaterial = Materials.FirstOrDefault();
        }

        private void OpenCreate()
        {
            IsNew = true;
            _editId = 0;
            EditName = string.Empty;
            EditCategoryId = Categories.FirstOrDefault()?.Id ?? 1;
            EditUnitId = Units.FirstOrDefault()?.Id ?? 1;
            EditArticle = string.Empty;
            EditBarcode = string.Empty;
            EditMinQuantity = 10;
            EditMaxQuantity = 100;
            EditSupplierId = Suppliers.FirstOrDefault()?.Id;
            EditStorageLocation = "Склад: Стеллаж 1";
            EditDescription = string.Empty;
            InitialStock = 0;
            InitialPrice = 0;
            InitialExpiry = DateTime.Today.AddYears(1);

            IsEditorOpen = true;
        }

        private void OpenEdit()
        {
            if (SelectedMaterial == null) return;
            IsNew = false;
            _editId = SelectedMaterial.Id;
            EditName = SelectedMaterial.Name;
            EditCategoryId = SelectedMaterial.CategoryId;
            EditUnitId = SelectedMaterial.UnitId;
            EditArticle = SelectedMaterial.Article;
            EditBarcode = SelectedMaterial.Barcode;
            EditMinQuantity = SelectedMaterial.MinQuantity;
            EditMaxQuantity = SelectedMaterial.MaxQuantity;
            EditSupplierId = SelectedMaterial.SupplierId;
            EditStorageLocation = SelectedMaterial.StorageLocation;
            EditDescription = SelectedMaterial.Description;

            IsEditorOpen = true;
        }

        private void Save()
        {
            if (string.IsNullOrWhiteSpace(EditName)) return;

            var mat = new Material
            {
                Id = _editId,
                Name = EditName.Trim(),
                CategoryId = EditCategoryId,
                UnitId = EditUnitId,
                Article = EditArticle?.Trim(),
                Barcode = EditBarcode?.Trim(),
                MinQuantity = EditMinQuantity,
                MaxQuantity = EditMaxQuantity,
                SupplierId = EditSupplierId,
                StorageLocation = EditStorageLocation?.Trim(),
                Description = EditDescription?.Trim()
            };

            InventoryService.Instance.SaveMaterial(mat, IsNew ? InitialStock : 0, IsNew ? InitialPrice : 0, IsNew ? InitialExpiry : null);
            IsEditorOpen = false;
            LoadMaterials();
        }

        private void Delete()
        {
            if (SelectedMaterial == null) return;
            InventoryService.Instance.DeleteMaterial(SelectedMaterial.Id);
            LoadMaterials();
        }
    }
}
