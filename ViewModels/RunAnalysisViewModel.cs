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
    public class RunAnalysisViewModel : ViewModelBase
    {
        private Analysis? _selectedAnalysis;
        private int _analysisCount = 1;
        private string? _notes;
        private string? _statusMessage;
        private bool _isSuccessMessage;
        private ObservableCollection<Analysis> _analyses = new();
        private ObservableCollection<AnalysisMaterialPreviewDto> _previewItems = new();
        private bool _canConfirm = false;

        public Analysis? SelectedAnalysis
        {
            get => _selectedAnalysis;
            set
            {
                if (SetProperty(ref _selectedAnalysis, value))
                {
                    UpdatePreview();
                }
            }
        }

        public int AnalysisCount
        {
            get => _analysisCount;
            set
            {
                if (value < 1) value = 1;
                if (SetProperty(ref _analysisCount, value))
                {
                    UpdatePreview();
                }
            }
        }

        public string? Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }

        public string? StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public bool IsSuccessMessage
        {
            get => _isSuccessMessage;
            set => SetProperty(ref _isSuccessMessage, value);
        }

        public bool CanConfirm
        {
            get => _canConfirm;
            set => SetProperty(ref _canConfirm, value);
        }

        public string CurrentOperatorName => AuthService.Instance.CurrentUser?.FullName ?? "Текущий пользователь";

        public ObservableCollection<Analysis> Analyses
        {
            get => _analyses;
            set => SetProperty(ref _analyses, value);
        }

        public ObservableCollection<AnalysisMaterialPreviewDto> PreviewItems
        {
            get => _previewItems;
            set => SetProperty(ref _previewItems, value);
        }

        public ICommand ConfirmExecutionCommand { get; }
        public ICommand IncrementCountCommand { get; }
        public ICommand DecrementCountCommand { get; }
        public ICommand QuickCountCommand { get; }

        public RunAnalysisViewModel()
        {
            ConfirmExecutionCommand = new RelayCommand(ExecuteAnalysis, () => CanConfirm);
            IncrementCountCommand = new RelayCommand(() => AnalysisCount++);
            DecrementCountCommand = new RelayCommand(() => { if (AnalysisCount > 1) AnalysisCount--; });
            QuickCountCommand = new RelayCommand(param =>
            {
                if (param != null && int.TryParse(param.ToString(), out int c))
                {
                    AnalysisCount = c;
                }
            });

            LoadAnalyses();
        }

        public void LoadAnalyses()
        {
            using var db = new AppDbContext();
            var list = db.Analyses
                .Include(a => a.Materials)
                .ThenInclude(am => am.Material)
                .Where(a => a.IsActive)
                .OrderBy(a => a.Name)
                .ToList();

            Analyses = new ObservableCollection<Analysis>(list);
            if (SelectedAnalysis != null)
            {
                SelectedAnalysis = Analyses.FirstOrDefault(a => a.Id == SelectedAnalysis.Id) ?? Analyses.FirstOrDefault();
            }
            else
            {
                SelectedAnalysis = Analyses.FirstOrDefault();
            }
        }

        private void UpdatePreview()
        {
            StatusMessage = null;
            if (SelectedAnalysis == null || AnalysisCount <= 0)
            {
                PreviewItems.Clear();
                CanConfirm = false;
                return;
            }

            var preview = InventoryService.Instance.PreviewAnalysisConsumption(SelectedAnalysis.Id, AnalysisCount);
            PreviewItems = new ObservableCollection<AnalysisMaterialPreviewDto>(preview);

            bool anyShortage = preview.Any(p => p.HasShortage);
            if (anyShortage)
            {
                StatusMessage = "Внимание: На складе недостаточно некоторых расходных материалов для выполнения указанного количества анализов!";
                IsSuccessMessage = false;

                using var db = new AppDbContext();
                bool allowNeg = db.Settings.FirstOrDefault(s => s.Key == "AllowNegativeStock")?.Value?.ToLower() == "true";
                CanConfirm = allowNeg; // only allowed if setting explicitly allows it
            }
            else
            {
                StatusMessage = $"Все материалы в наличии для списания по нормам ({AnalysisCount} шт.). Метод списания: FEFO (сначала с ближайшим сроком).";
                IsSuccessMessage = true;
                CanConfirm = true;
            }
        }

        private void ExecuteAnalysis()
        {
            if (SelectedAnalysis == null) return;

            try
            {
                InventoryService.Instance.ExecuteAnalysisRun(SelectedAnalysis.Id, AnalysisCount, Notes);
                StatusMessage = $"Успешно! Списаны материалы по {AnalysisCount} анализам «{SelectedAnalysis.Name}». Остатки обновлены.";
                IsSuccessMessage = true;
                Notes = string.Empty;
                AnalysisCount = 1;
                UpdatePreview();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка выполнения: {ex.Message}";
                IsSuccessMessage = false;
            }
        }
    }
}
