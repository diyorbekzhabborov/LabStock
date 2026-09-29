using System;
using System.Windows.Input;
using LabStock.Models;
using LabStock.Services;

namespace LabStock.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private object? _currentView;
        private string _currentViewName = "Dashboard";
        private int _unreadNotificationsCount;
        private string _quickBarcode = string.Empty;

        // ViewModels
        public LoginViewModel LoginVM { get; }
        public DashboardViewModel DashboardVM { get; }
        public WarehouseViewModel WarehouseVM { get; }
        public MaterialsViewModel MaterialsVM { get; }
        public ReceiptsViewModel ReceiptsVM { get; }
        public WriteoffsViewModel WriteoffsVM { get; }
        public AnalysesViewModel AnalysesVM { get; }
        public RunAnalysisViewModel RunAnalysisVM { get; }
        public MovementsViewModel MovementsVM { get; }
        public NotificationsViewModel NotificationsVM { get; }
        public ReportsViewModel ReportsVM { get; }
        public SuppliersViewModel SuppliersVM { get; }
        public InventoryCheckViewModel InventoryCheckVM { get; }
        public AuditLogViewModel AuditLogVM { get; }
        public UsersViewModel UsersVM { get; }
        public SettingsViewModel SettingsVM { get; }

        public object? CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
        }

        public string CurrentViewName
        {
            get => _currentViewName;
            set => SetProperty(ref _currentViewName, value);
        }

        public int UnreadNotificationsCount
        {
            get => _unreadNotificationsCount;
            set => SetProperty(ref _unreadNotificationsCount, value);
        }

        public string QuickBarcode
        {
            get => _quickBarcode;
            set => SetProperty(ref _quickBarcode, value);
        }

        public User? CurrentUser => AuthService.Instance.CurrentUser;
        public bool IsAuthenticated => AuthService.Instance.IsAuthenticated;
        public string CurrentUserFullName => CurrentUser?.FullName ?? "Гость";
        public string CurrentUserRole => CurrentUser?.Role.ToFriendlyName() ?? "";

        // Role permissions
        public bool CanManageUsers => AuthService.Instance.CanManageUsers();
        public bool CanManageSettings => AuthService.Instance.CanManageSettings();
        public bool CanManageMaterials => AuthService.Instance.CanManageMaterials();
        public bool CanRegisterReceipt => AuthService.Instance.CanRegisterReceipt();
        public bool CanPerformInventory => AuthService.Instance.CanPerformInventory();
        public bool CanPerformAnalysis => AuthService.Instance.CanPerformAnalysis();
        public bool CanPerformWriteoff => AuthService.Instance.CanPerformWriteoff();

        public ICommand NavigateCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand BarcodeLookupCommand { get; }

        public MainViewModel()
        {
            LoginVM = new LoginViewModel();
            DashboardVM = new DashboardViewModel(Navigate);
            WarehouseVM = new WarehouseViewModel();
            MaterialsVM = new MaterialsViewModel();
            ReceiptsVM = new ReceiptsViewModel();
            WriteoffsVM = new WriteoffsViewModel();
            AnalysesVM = new AnalysesViewModel();
            RunAnalysisVM = new RunAnalysisViewModel();
            MovementsVM = new MovementsViewModel();
            NotificationsVM = new NotificationsViewModel();
            ReportsVM = new ReportsViewModel();
            SuppliersVM = new SuppliersViewModel();
            InventoryCheckVM = new InventoryCheckViewModel();
            AuditLogVM = new AuditLogViewModel();
            UsersVM = new UsersViewModel();
            SettingsVM = new SettingsViewModel();

            NavigateCommand = new RelayCommand(param =>
            {
                if (param is string viewName) Navigate(viewName);
            });

            LogoutCommand = new RelayCommand(() =>
            {
                AuthService.Instance.Logout();
                Navigate("Login");
            });

            BarcodeLookupCommand = new RelayCommand(ExecuteBarcodeLookup);

            AuthService.Instance.OnAuthStateChanged += OnAuthStateChanged;
            NotificationService.Instance.OnNotificationsUpdated += UpdateNotificationBadge;
            LoginVM.OnLoginSuccess += () => Navigate("Dashboard");

            // Auto-login admin for immediate convenience, or start at Dashboard
            AuthService.Instance.Login("admin", "admin123");
            UpdateNotificationBadge();
            Navigate("Dashboard");
        }

        private void OnAuthStateChanged()
        {
            OnPropertyChanged(nameof(CurrentUser));
            OnPropertyChanged(nameof(IsAuthenticated));
            OnPropertyChanged(nameof(CurrentUserFullName));
            OnPropertyChanged(nameof(CurrentUserRole));
            OnPropertyChanged(nameof(CanManageUsers));
            OnPropertyChanged(nameof(CanManageSettings));
            OnPropertyChanged(nameof(CanManageMaterials));
            OnPropertyChanged(nameof(CanRegisterReceipt));
            OnPropertyChanged(nameof(CanPerformInventory));
            OnPropertyChanged(nameof(CanPerformAnalysis));
            OnPropertyChanged(nameof(CanPerformWriteoff));
        }

        public void UpdateNotificationBadge()
        {
            UnreadNotificationsCount = NotificationService.Instance.GetUnreadCount();
        }

        public void Navigate(string viewName)
        {
            CurrentViewName = viewName;
            CurrentView = viewName switch
            {
                "Dashboard" => DashboardVM,
                "Warehouse" => WarehouseVM,
                "Materials" => MaterialsVM,
                "Receipts" => ReceiptsVM,
                "Writeoffs" => WriteoffsVM,
                "Analyses" => AnalysesVM,
                "RunAnalysis" => RunAnalysisVM,
                "Movements" => MovementsVM,
                "Notifications" => NotificationsVM,
                "Reports" => ReportsVM,
                "Suppliers" => SuppliersVM,
                "InventoryCheck" => InventoryCheckVM,
                "AuditLog" => AuditLogVM,
                "Users" => UsersVM,
                "Settings" => SettingsVM,
                "Login" => LoginVM,
                _ => DashboardVM
            };

            // Trigger data refresh on navigation
            switch (viewName)
            {
                case "Dashboard": DashboardVM.LoadData(); break;
                case "Warehouse": WarehouseVM.LoadData(); break;
                case "Materials": MaterialsVM.LoadMaterials(); break;
                case "Receipts": ReceiptsVM.LoadReceipts(); break;
                case "Writeoffs": WriteoffsVM.LoadWriteoffs(); break;
                case "Analyses": AnalysesVM.LoadAnalyses(); break;
                case "RunAnalysis": RunAnalysisVM.LoadAnalyses(); break;
                case "Movements": MovementsVM.LoadMovements(); break;
                case "Notifications": NotificationsVM.LoadNotifications(); break;
                case "Reports": ReportsVM.LoadDailyReport(); break;
                case "Suppliers": SuppliersVM.LoadSuppliers(); break;
                case "InventoryCheck": InventoryCheckVM.LoadData(); break;
                case "AuditLog": AuditLogVM.LoadLogs(); break;
                case "Users": UsersVM.LoadUsers(); break;
            }

            UpdateNotificationBadge();
        }

        private void ExecuteBarcodeLookup()
        {
            if (string.IsNullOrWhiteSpace(QuickBarcode)) return;
            WarehouseVM.SearchText = QuickBarcode.Trim();
            Navigate("Warehouse");
            QuickBarcode = string.Empty;
        }
    }
}
