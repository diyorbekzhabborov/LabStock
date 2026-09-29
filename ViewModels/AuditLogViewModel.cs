using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using LabStock.Data;
using LabStock.Models;
using Microsoft.EntityFrameworkCore;

namespace LabStock.ViewModels
{
    public class AuditLogViewModel : ViewModelBase
    {
        private ObservableCollection<AuditLog> _logs = new();
        private string _searchText = string.Empty;

        public ObservableCollection<AuditLog> Logs
        {
            get => _logs;
            set => SetProperty(ref _logs, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    LoadLogs();
                }
            }
        }

        public ICommand RefreshCommand { get; }

        public AuditLogViewModel()
        {
            RefreshCommand = new RelayCommand(LoadLogs);
            LoadLogs();
        }

        public void LoadLogs()
        {
            using var db = new AppDbContext();
            var query = db.AuditLogs
                .Include(l => l.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var s = SearchText.Trim().ToLower();
                query = query.Where(l => l.Action.ToLower().Contains(s)
                                      || l.EntityName.ToLower().Contains(s)
                                      || l.Details.ToLower().Contains(s)
                                      || (l.User != null && l.User.FullName.ToLower().Contains(s)));
            }

            var list = query.OrderByDescending(l => l.Timestamp).Take(200).ToList();
            Logs = new ObservableCollection<AuditLog>(list);
        }
    }
}
