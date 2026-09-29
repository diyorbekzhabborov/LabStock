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
    public class RecipeItemDraft : ViewModelBase
    {
        private int _materialId;
        private string _materialName = string.Empty;
        private string _unitName = string.Empty;
        private decimal _requiredQuantity = 1;

        public int MaterialId
        {
            get => _materialId;
            set => SetProperty(ref _materialId, value);
        }

        public string MaterialName
        {
            get => _materialName;
            set => SetProperty(ref _materialName, value);
        }

        public string UnitName
        {
            get => _unitName;
            set => SetProperty(ref _unitName, value);
        }

        public decimal RequiredQuantity
        {
            get => _requiredQuantity;
            set => SetProperty(ref _requiredQuantity, value);
        }
    }

    public class AnalysesViewModel : ViewModelBase
    {
        private ObservableCollection<Analysis> _analyses = new();
        private Analysis? _selectedAnalysis;
        private bool _isEditorOpen = false;
        private bool _isNew = true;

        // Edit analysis fields
        private int _editId;
        private string _editCode = string.Empty;
        private string _editName = string.Empty;
        private string? _editCategory;
        private decimal _editPrice = 20;
        private string? _editDescription;
        private ObservableCollection<RecipeItemDraft> _editRecipe = new();

        // New recipe item selector
        private int _selectedMaterialIdToAdd;

        public ObservableCollection<Analysis> Analyses
        {
            get => _analyses;
            set => SetProperty(ref _analyses, value);
        }

        public Analysis? SelectedAnalysis
        {
            get => _selectedAnalysis;
            set => SetProperty(ref _selectedAnalysis, value);
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

        public string EditCode
        {
            get => _editCode;
            set => SetProperty(ref _editCode, value);
        }

        public string EditName
        {
            get => _editName;
            set => SetProperty(ref _editName, value);
        }

        public string? EditCategory
        {
            get => _editCategory;
            set => SetProperty(ref _editCategory, value);
        }

        public decimal EditPrice
        {
            get => _editPrice;
            set => SetProperty(ref _editPrice, value);
        }

        public string? EditDescription
        {
            get => _editDescription;
            set => SetProperty(ref _editDescription, value);
        }

        public ObservableCollection<RecipeItemDraft> EditRecipe
        {
            get => _editRecipe;
            set => SetProperty(ref _editRecipe, value);
        }

        public int SelectedMaterialIdToAdd
        {
            get => _selectedMaterialIdToAdd;
            set => SetProperty(ref _selectedMaterialIdToAdd, value);
        }

        public ObservableCollection<Material> AllMaterials { get; } = new();

        public ICommand RefreshCommand { get; }
        public ICommand OpenCreateCommand { get; }
        public ICommand OpenEditCommand { get; }
        public ICommand AddMaterialToRecipeCommand { get; }
        public ICommand RemoveMaterialFromRecipeCommand { get; }
        public ICommand SaveAnalysisCommand { get; }
        public ICommand CancelEditCommand { get; }

        public AnalysesViewModel()
        {
            RefreshCommand = new RelayCommand(LoadAnalyses);
            OpenCreateCommand = new RelayCommand(OpenCreate);
            OpenEditCommand = new RelayCommand(OpenEdit, () => SelectedAnalysis != null);
            AddMaterialToRecipeCommand = new RelayCommand(AddMaterialToRecipe);
            RemoveMaterialFromRecipeCommand = new RelayCommand(item =>
            {
                if (item is RecipeItemDraft r) EditRecipe.Remove(r);
            });
            SaveAnalysisCommand = new RelayCommand(SaveAnalysis);
            CancelEditCommand = new RelayCommand(() => IsEditorOpen = false);

            LoadLookups();
            LoadAnalyses();
        }

        public void LoadLookups()
        {
            using var db = new AppDbContext();
            AllMaterials.Clear();
            foreach (var m in db.Materials.Include(m => m.Unit).Where(m => m.IsActive).OrderBy(m => m.Name))
            {
                AllMaterials.Add(m);
            }
            SelectedMaterialIdToAdd = AllMaterials.FirstOrDefault()?.Id ?? 0;
        }

        public void LoadAnalyses()
        {
            using var db = new AppDbContext();
            var list = db.Analyses
                .Include(a => a.Materials)
                .ThenInclude(am => am.Material)
                .ThenInclude(m => m!.Unit)
                .Where(a => a.IsActive)
                .OrderBy(a => a.Name)
                .ToList();

            Analyses = new ObservableCollection<Analysis>(list);
            SelectedAnalysis = Analyses.FirstOrDefault();
        }

        private void OpenCreate()
        {
            LoadLookups();
            IsNew = true;
            _editId = 0;
            EditCode = $"АН-{DateTime.Now:yyMMdd}";
            EditName = string.Empty;
            EditCategory = "Общая лаборатория";
            EditPrice = 25.0m;
            EditDescription = string.Empty;
            EditRecipe.Clear();

            IsEditorOpen = true;
        }

        private void OpenEdit()
        {
            if (SelectedAnalysis == null) return;
            LoadLookups();
            IsNew = false;
            _editId = SelectedAnalysis.Id;
            EditCode = SelectedAnalysis.Code;
            EditName = SelectedAnalysis.Name;
            EditCategory = SelectedAnalysis.Category;
            EditPrice = SelectedAnalysis.Price;
            EditDescription = SelectedAnalysis.Description;

            EditRecipe.Clear();
            foreach (var m in SelectedAnalysis.Materials)
            {
                if (m.Material != null)
                {
                    EditRecipe.Add(new RecipeItemDraft
                    {
                        MaterialId = m.MaterialId,
                        MaterialName = m.Material.Name,
                        UnitName = m.Material.Unit?.ShortName ?? "шт.",
                        RequiredQuantity = m.RequiredQuantity
                    });
                }
            }

            IsEditorOpen = true;
        }

        private void AddMaterialToRecipe()
        {
            if (SelectedMaterialIdToAdd <= 0) return;
            if (EditRecipe.Any(r => r.MaterialId == SelectedMaterialIdToAdd)) return;

            var mat = AllMaterials.FirstOrDefault(m => m.Id == SelectedMaterialIdToAdd);
            if (mat != null)
            {
                EditRecipe.Add(new RecipeItemDraft
                {
                    MaterialId = mat.Id,
                    MaterialName = mat.Name,
                    UnitName = mat.Unit?.ShortName ?? "шт.",
                    RequiredQuantity = 1
                });
            }
        }

        private void SaveAnalysis()
        {
            if (string.IsNullOrWhiteSpace(EditName) || string.IsNullOrWhiteSpace(EditCode)) return;

            using var db = new AppDbContext();
            Analysis analysis;

            if (IsNew)
            {
                analysis = new Analysis
                {
                    Code = EditCode.Trim(),
                    Name = EditName.Trim(),
                    Category = EditCategory?.Trim(),
                    Price = EditPrice,
                    Description = EditDescription?.Trim(),
                    IsActive = true
                };
                db.Analyses.Add(analysis);
                db.SaveChanges();
            }
            else
            {
                analysis = db.Analyses.Include(a => a.Materials).FirstOrDefault(a => a.Id == _editId)!;
                if (analysis == null) return;
                analysis.Code = EditCode.Trim();
                analysis.Name = EditName.Trim();
                analysis.Category = EditCategory?.Trim();
                analysis.Price = EditPrice;
                analysis.Description = EditDescription?.Trim();

                // Remove existing materials
                db.AnalysisMaterials.RemoveRange(analysis.Materials);
                db.SaveChanges();
            }

            // Save recipe items
            foreach (var r in EditRecipe)
            {
                db.AnalysisMaterials.Add(new AnalysisMaterial
                {
                    AnalysisId = analysis.Id,
                    MaterialId = r.MaterialId,
                    RequiredQuantity = r.RequiredQuantity
                });
            }

            db.SaveChanges();
            AuditService.Instance.Log(IsNew ? "Создание" : "Изменение", "Анализы", analysis.Id.ToString(), $"Сохранен анализ {analysis.Name} ({analysis.Code}), норм расхода: {EditRecipe.Count}");

            IsEditorOpen = false;
            LoadAnalyses();
        }
    }
}
