using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using LabStock.Data;
using LabStock.Models;
using LabStock.Services;
using Microsoft.Win32;

namespace LabStock.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private string _labName = "Медицинская лаборатория «LabStock»";
        private string _currency = "сомони";
        private int _expiryAlertDays = 30;
        private bool _allowNegativeStock = false;
        private string? _statusMessage;
        private bool _isSuccess;
        private string _lastBackupPath = string.Empty;

        public string LabName
        {
            get => _labName;
            set => SetProperty(ref _labName, value);
        }

        public string Currency
        {
            get => _currency;
            set => SetProperty(ref _currency, value);
        }

        public int ExpiryAlertDays
        {
            get => _expiryAlertDays;
            set => SetProperty(ref _expiryAlertDays, value);
        }

        public bool AllowNegativeStock
        {
            get => _allowNegativeStock;
            set => SetProperty(ref _allowNegativeStock, value);
        }

        public string? StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public bool IsSuccess
        {
            get => _isSuccess;
            set => SetProperty(ref _isSuccess, value);
        }

        public string LastBackupPath
        {
            get => _lastBackupPath;
            set => SetProperty(ref _lastBackupPath, value);
        }

        public ObservableCollection<int> ExpiryDaysOptions { get; } = new() { 7, 15, 30, 60 };

        public ICommand SaveSettingsCommand { get; }
        public ICommand CreateBackupCommand { get; }
        public ICommand RestoreBackupCommand { get; }

        public SettingsViewModel()
        {
            SaveSettingsCommand = new RelayCommand(SaveSettings);
            CreateBackupCommand = new RelayCommand(CreateBackup);
            RestoreBackupCommand = new RelayCommand(RestoreBackup);

            LoadSettings();
        }

        public void LoadSettings()
        {
            using var db = new AppDbContext();
            var ln = db.Settings.FirstOrDefault(s => s.Key == "LabName");
            if (ln != null) LabName = ln.Value;

            var cur = db.Settings.FirstOrDefault(s => s.Key == "Currency");
            if (cur != null) Currency = cur.Value;

            var exp = db.Settings.FirstOrDefault(s => s.Key == "ExpiryAlertDays");
            if (exp != null && int.TryParse(exp.Value, out int days)) ExpiryAlertDays = days;

            var neg = db.Settings.FirstOrDefault(s => s.Key == "AllowNegativeStock");
            if (neg != null) AllowNegativeStock = neg.Value.ToLower() == "true";
        }

        private void SaveSettings()
        {
            try
            {
                using var db = new AppDbContext();

                UpdateSetting(db, "LabName", LabName);
                UpdateSetting(db, "Currency", Currency);
                UpdateSetting(db, "ExpiryAlertDays", ExpiryAlertDays.ToString());
                UpdateSetting(db, "AllowNegativeStock", AllowNegativeStock.ToString().ToLower());

                db.SaveChanges();
                NotificationService.Instance.RefreshNotifications();

                StatusMessage = "Настройки успешно сохранены!";
                IsSuccess = true;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка сохранения: {ex.Message}";
                IsSuccess = false;
            }
        }

        private void UpdateSetting(AppDbContext db, string key, string value)
        {
            var s = db.Settings.FirstOrDefault(x => x.Key == key);
            if (s != null) s.Value = value;
            else db.Settings.Add(new Setting { Key = key, Value = value });
        }

        private void CreateBackup()
        {
            try
            {
                var path = BackupService.Instance.CreateBackup();
                LastBackupPath = path;
                StatusMessage = $"Резервная копия базы данных успешно создана: {Path.GetFileName(path)}";
                IsSuccess = true;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка создания бэкапа: {ex.Message}";
                IsSuccess = false;
            }
        }

        private void RestoreBackup()
        {
            try
            {
                var ofd = new OpenFileDialog
                {
                    Title = "Выберите файл резервной копии SQLite (*.db)",
                    Filter = "SQLite Backup (*.db)|*.db|All files (*.*)|*.*",
                    InitialDirectory = BackupService.Instance.GetDefaultBackupFolder()
                };

                if (ofd.ShowDialog() == true)
                {
                    BackupService.Instance.RestoreBackup(ofd.FileName);
                    StatusMessage = $"База данных успешно восстановлена из: {Path.GetFileName(ofd.FileName)}. Рекомендуется перезапустить приложение для полного обновления состояния.";
                    IsSuccess = true;
                    LoadSettings();
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка восстановления: {ex.Message}";
                IsSuccess = false;
            }
        }
    }
}
