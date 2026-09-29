using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using LabStock.Models;
using LabStock.Services;

namespace LabStock.ViewModels
{
    public class DashboardViewModel : ViewModelBase
    {
        private DashboardStatsDto _stats = new();
        private readonly Action<string> _navigateAction;

        public DashboardStatsDto Stats
        {
            get => _stats;
            set => SetProperty(ref _stats, value);
        }

        public ICommand RefreshCommand { get; }
        public ICommand NavigateToRunAnalysisCommand { get; }
        public ICommand NavigateToReceiptCommand { get; }
        public ICommand NavigateToWriteoffCommand { get; }
        public ICommand NavigateToDailyReportCommand { get; }
        public ICommand NavigateToNotificationsCommand { get; }

        public DashboardViewModel(Action<string> navigateAction)
        {
            _navigateAction = navigateAction;

            RefreshCommand = new RelayCommand(LoadData);
            NavigateToRunAnalysisCommand = new RelayCommand(() => _navigateAction("RunAnalysis"));
            NavigateToReceiptCommand = new RelayCommand(() => _navigateAction("Receipts"));
            NavigateToWriteoffCommand = new RelayCommand(() => _navigateAction("Writeoffs"));
            NavigateToDailyReportCommand = new RelayCommand(() => _navigateAction("Reports"));
            NavigateToNotificationsCommand = new RelayCommand(() => _navigateAction("Notifications"));

            LoadData();
        }

        public void LoadData()
        {
            Stats = InventoryService.Instance.GetDashboardStats();
        }
    }
}
