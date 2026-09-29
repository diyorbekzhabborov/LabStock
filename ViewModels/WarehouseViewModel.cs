using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using LabStock.Data;
using LabStock.Models;
using LabStock.Services;

namespace LabStock.ViewModels
{
    public class CategoryFilterItem
    {
        public int? Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class StatusFilterItem
    {
        public StockStatus? Status { get; set; }
        public string Title { get; set; } = string.Empty;
    }

    public class WarehouseViewModel : ViewModelBase
    {
        private string _searchText = string.Empty;
        private CategoryFilterItem _selectedCategory = new() { Id = null, Name = "Все категории" };
        private StatusFilterItem _selectedStatus = new() { Status = null, Title = "Все статусы" };
        private Material? _selectedMaterial;
        private ObservableCollection<Material> _materials = new();
        private ObservableCollection<Lot> _selectedMaterialLots = new();

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    ApplyFilters();
                }
            }
        }

        public CategoryFilterItem SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (SetProperty(ref _selectedCategory, value))
                {
                    ApplyFilters();
                }
            }
        }

        public StatusFilterItem SelectedStatus
        {
            get => _selectedStatus;
            set
            {
                if (SetProperty(ref _selectedStatus, value))
                {
                    ApplyFilters();
                }
            }
        }

        public Material? SelectedMaterial
        {
            get => _selectedMaterial;
            set
            {
                if (SetProperty(ref _selectedMaterial, value))
                {
                    LoadLotsForSelectedMaterial();
                }
            }
        }

        public ObservableCollection<Material> Materials
        {
            get => _materials;
            set => SetProperty(ref _materials, value);
        }

        public ObservableCollection<Lot> SelectedMaterialLots
        {
            get => _selectedMaterialLots;
            set => SetProperty(ref _selectedMaterialLots, value);
        }

        public ObservableCollection<CategoryFilterItem> Categories { get; } = new();
        public ObservableCollection<StatusFilterItem> Statuses { get; } = new();

        public ICommand RefreshCommand { get; }
        public ICommand ClearFiltersCommand { get; }

        public WarehouseViewModel()
        {
            RefreshCommand = new RelayCommand(LoadData);
            ClearFiltersCommand = new RelayCommand(() =>
            {
                _searchText = string.Empty;
                OnPropertyChanged(nameof(SearchText));
                _selectedCategory = Categories.FirstOrDefault() ?? new CategoryFilterItem { Id = null, Name = "Все категории" };
                OnPropertyChanged(nameof(SelectedCategory));
                _selectedStatus = Statuses.FirstOrDefault() ?? new StatusFilterItem { Status = null, Title = "Все статусы" };
                OnPropertyChanged(nameof(SelectedStatus));
                ApplyFilters();
            });

            InitFilters();
            LoadData();
        }

        private void InitFilters()
        {
            Categories.Clear();
            Categories.Add(new CategoryFilterItem { Id = null, Name = "Все категории" });
            using (var db = new AppDbContext())
            {
                foreach (var c in db.Categories.OrderBy(c => c.Name))
                {
                    Categories.Add(new CategoryFilterItem { Id = c.Id, Name = c.Name });
                }
            }
            _selectedCategory = Categories[0];

            Statuses.Clear();
            Statuses.Add(new StatusFilterItem { Status = null, Title = "Все статусы" });
            Statuses.Add(new StatusFilterItem { Status = StockStatus.Normal, Title = "Норма" });
            Statuses.Add(new StatusFilterItem { Status = StockStatus.Attention, Title = "Внимание (близко к мин.)" });
            Statuses.Add(new StatusFilterItem { Status = StockStatus.Low, Title = "Низкий остаток" });
            Statuses.Add(new StatusFilterItem { Status = StockStatus.OutOfStock, Title = "Нет на складе" });
            _selectedStatus = Statuses[0];
        }

        public void LoadData()
        {
            InitFilters();
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            var list = InventoryService.Instance.GetAllMaterials(
                SearchText,
                SelectedCategory?.Id,
                SelectedStatus?.Status
            );

            Materials = new ObservableCollection<Material>(list);

            if (SelectedMaterial != null)
            {
                SelectedMaterial = Materials.FirstOrDefault(m => m.Id == SelectedMaterial.Id) ?? Materials.FirstOrDefault();
            }
            else
            {
                SelectedMaterial = Materials.FirstOrDefault();
            }
        }

        private void LoadLotsForSelectedMaterial()
        {
            if (SelectedMaterial == null)
            {
                SelectedMaterialLots.Clear();
                return;
            }

            var lots = InventoryService.Instance.GetLotsForMaterial(SelectedMaterial.Id);
            SelectedMaterialLots = new ObservableCollection<Lot>(lots);
        }
    }
}
