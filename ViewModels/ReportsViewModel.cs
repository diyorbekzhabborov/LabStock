using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using LabStock.Services;
using Microsoft.Win32;

namespace LabStock.ViewModels
{
    public class ReportsViewModel : ViewModelBase
    {
        private int _selectedTabIndex = 0;

        // Daily Report
        private DateTime _dailyDate = DateTime.Today;
        private DailyReportDto? _dailyReport;

        // Monthly Report
        private int _selectedYear = DateTime.Today.Year;
        private int _selectedMonth = DateTime.Today.Month;
        private MonthlyReportDto? _monthlyReport;

        // Analytics Predictions
        private ObservableCollection<AnalyticsPredictionDto> _predictions = new();

        private string? _statusMessage;

        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                if (SetProperty(ref _selectedTabIndex, value))
                {
                    if (_selectedTabIndex == 0 && _dailyReport == null) LoadDailyReport();
                    else if (_selectedTabIndex == 1 && _monthlyReport == null) LoadMonthlyReport();
                    else if (_selectedTabIndex == 2 && !_predictions.Any()) LoadPredictions();
                }
            }
        }

        public DateTime DailyDate
        {
            get => _dailyDate;
            set
            {
                if (SetProperty(ref _dailyDate, value))
                {
                    LoadDailyReport();
                }
            }
        }

        public DailyReportDto? DailyReport
        {
            get => _dailyReport;
            set => SetProperty(ref _dailyReport, value);
        }

        public int SelectedYear
        {
            get => _selectedYear;
            set
            {
                if (SetProperty(ref _selectedYear, value))
                {
                    LoadMonthlyReport();
                }
            }
        }

        public int SelectedMonth
        {
            get => _selectedMonth;
            set
            {
                if (SetProperty(ref _selectedMonth, value))
                {
                    LoadMonthlyReport();
                }
            }
        }

        public MonthlyReportDto? MonthlyReport
        {
            get => _monthlyReport;
            set => SetProperty(ref _monthlyReport, value);
        }

        public ObservableCollection<AnalyticsPredictionDto> Predictions
        {
            get => _predictions;
            set => SetProperty(ref _predictions, value);
        }

        public string? StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public ObservableCollection<int> Years { get; } = new() { 2025, 2026, 2027, 2028 };
        public ObservableCollection<int> Months { get; } = new() { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };

        public ICommand RefreshDailyCommand { get; }
        public ICommand ExportDailyPdfCommand { get; }
        public ICommand ExportDailyExcelCommand { get; }
        public ICommand RefreshMonthlyCommand { get; }
        public ICommand ExportMonthlyExcelCommand { get; }
        public ICommand RefreshPredictionsCommand { get; }

        public ReportsViewModel()
        {
            RefreshDailyCommand = new RelayCommand(LoadDailyReport);
            ExportDailyPdfCommand = new RelayCommand(ExportDailyPdf);
            ExportDailyExcelCommand = new RelayCommand(ExportDailyExcel);
            RefreshMonthlyCommand = new RelayCommand(LoadMonthlyReport);
            ExportMonthlyExcelCommand = new RelayCommand(ExportMonthlyExcel);
            RefreshPredictionsCommand = new RelayCommand(LoadPredictions);

            LoadDailyReport();
        }

        public void LoadDailyReport()
        {
            DailyReport = ReportingService.Instance.GetDailyReport(DailyDate);
        }

        public void LoadMonthlyReport()
        {
            MonthlyReport = ReportingService.Instance.GetMonthlyReport(SelectedYear, SelectedMonth);
        }

        public void LoadPredictions()
        {
            var list = ReportingService.Instance.GetAnalyticsPredictions();
            Predictions = new ObservableCollection<AnalyticsPredictionDto>(list);
        }

        private void ExportDailyPdf()
        {
            if (DailyReport == null) return;
            try
            {
                var defaultFileName = ExportService.Instance.GenerateDailyReportFileName(DailyDate, "pdf");
                var sfd = new SaveFileDialog
                {
                    FileName = defaultFileName,
                    Filter = "PDF Files (*.pdf)|*.pdf"
                };

                if (sfd.ShowDialog() == true)
                {
                    ExportService.Instance.ExportDailyReportToPdf(DailyReport, sfd.FileName);
                    StatusMessage = $"PDF отчёт сохранен: {Path.GetFileName(sfd.FileName)}";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка экспорта PDF: {ex.Message}";
            }
        }

        private void ExportDailyExcel()
        {
            if (DailyReport == null) return;
            try
            {
                var defaultFileName = ExportService.Instance.GenerateDailyReportFileName(DailyDate, "xlsx");
                var sfd = new SaveFileDialog
                {
                    FileName = defaultFileName,
                    Filter = "Excel Files (*.xlsx)|*.xlsx"
                };

                if (sfd.ShowDialog() == true)
                {
                    ExportService.Instance.ExportDailyReportToExcel(DailyReport, sfd.FileName);
                    StatusMessage = $"Excel отчёт сохранен: {Path.GetFileName(sfd.FileName)}";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка экспорта Excel: {ex.Message}";
            }
        }

        private void ExportMonthlyExcel()
        {
            if (MonthlyReport == null) return;
            try
            {
                var defaultFileName = ExportService.Instance.GenerateMonthlyReportFileName(SelectedYear, SelectedMonth, "xlsx");
                var sfd = new SaveFileDialog
                {
                    FileName = defaultFileName,
                    Filter = "Excel Files (*.xlsx)|*.xlsx"
                };

                if (sfd.ShowDialog() == true)
                {
                    ExportService.Instance.ExportMonthlyReportToExcel(MonthlyReport, sfd.FileName);
                    StatusMessage = $"Месячный отчёт сохранен: {Path.GetFileName(sfd.FileName)}";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка экспорта Excel: {ex.Message}";
            }
        }
    }
}
