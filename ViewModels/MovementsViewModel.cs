using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using LabStock.Data;
using LabStock.Models;
using LabStock.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;

namespace LabStock.ViewModels
{
    public class MovementsViewModel : ViewModelBase
    {
        private ObservableCollection<StockMovement> _movements = new();
        private DateTime _startDate = DateTime.Today.AddDays(-30);
        private DateTime _endDate = DateTime.Today;
        private string _searchText = string.Empty;
        private MovementType? _selectedType = null;
        private string? _statusNotification;

        public ObservableCollection<StockMovement> Movements
        {
            get => _movements;
            set => SetProperty(ref _movements, value);
        }

        public DateTime StartDate
        {
            get => _startDate;
            set
            {
                if (SetProperty(ref _startDate, value))
                {
                    LoadMovements();
                }
            }
        }

        public DateTime EndDate
        {
            get => _endDate;
            set
            {
                if (SetProperty(ref _endDate, value))
                {
                    LoadMovements();
                }
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    LoadMovements();
                }
            }
        }

        public MovementType? SelectedType
        {
            get => _selectedType;
            set
            {
                if (SetProperty(ref _selectedType, value))
                {
                    LoadMovements();
                }
            }
        }

        public string? StatusNotification
        {
            get => _statusNotification;
            set => SetProperty(ref _statusNotification, value);
        }

        public ICommand RefreshCommand { get; }
        public ICommand ExportExcelCommand { get; }
        public ICommand ExportCsvCommand { get; }

        public MovementsViewModel()
        {
            RefreshCommand = new RelayCommand(LoadMovements);
            ExportExcelCommand = new RelayCommand(ExportExcel);
            ExportCsvCommand = new RelayCommand(ExportCsv);

            LoadMovements();
        }

        public void LoadMovements()
        {
            using var db = new AppDbContext();
            var dayEnd = EndDate.Date.AddDays(1);

            var query = db.StockMovements
                .Include(m => m.Material)
                .ThenInclude(mat => mat!.Unit)
                .Include(m => m.Lot)
                .Include(m => m.User)
                .Where(m => m.Date >= StartDate.Date && m.Date < dayEnd)
                .AsQueryable();

            if (SelectedType.HasValue)
            {
                query = query.Where(m => m.OperationType == SelectedType.Value);
            }

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var s = SearchText.Trim().ToLower();
                query = query.Where(m => (m.Material != null && m.Material.Name.ToLower().Contains(s))
                                      || (m.Lot != null && m.Lot.LotNumber.ToLower().Contains(s))
                                      || (m.ReferenceNumber != null && m.ReferenceNumber.ToLower().Contains(s))
                                      || (m.Notes != null && m.Notes.ToLower().Contains(s)));
            }

            var list = query.OrderByDescending(m => m.Date).ToList();
            Movements = new ObservableCollection<StockMovement>(list);
        }

        private void ExportExcel()
        {
            try
            {
                var defaultFileName = ExportService.Instance.GenerateMovementsFileName("xlsx");
                var sfd = new SaveFileDialog
                {
                    FileName = defaultFileName,
                    Filter = "Excel Files (*.xlsx)|*.xlsx"
                };

                if (sfd.ShowDialog() == true)
                {
                    ExportService.Instance.ExportStockMovementsToExcel(Movements.ToList(), sfd.FileName);
                    StatusNotification = $"Файл Excel успешно сохранен: {Path.GetFileName(sfd.FileName)}";
                }
            }
            catch (Exception ex)
            {
                StatusNotification = $"Ошибка экспорта: {ex.Message}";
            }
        }

        private void ExportCsv()
        {
            try
            {
                var defaultFileName = ExportService.Instance.GenerateMovementsFileName("csv");
                var sfd = new SaveFileDialog
                {
                    FileName = defaultFileName,
                    Filter = "CSV Files (*.csv)|*.csv"
                };

                if (sfd.ShowDialog() == true)
                {
                    ExportService.Instance.ExportStockMovementsToCsv(Movements.ToList(), sfd.FileName);
                    StatusNotification = $"Файл CSV успешно сохранен: {Path.GetFileName(sfd.FileName)}";
                }
            }
            catch (Exception ex)
            {
                StatusNotification = $"Ошибка экспорта: {ex.Message}";
            }
        }
    }
}
