using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using LabStock.Models;
using LabStock.Services;

namespace LabStock.ViewModels
{
    public class NotificationsViewModel : ViewModelBase
    {
        private ObservableCollection<Notification> _notifications = new();
        private bool _onlyUnread = false;

        public ObservableCollection<Notification> Notifications
        {
            get => _notifications;
            set => SetProperty(ref _notifications, value);
        }

        public bool OnlyUnread
        {
            get => _onlyUnread;
            set
            {
                if (SetProperty(ref _onlyUnread, value))
                {
                    LoadNotifications();
                }
            }
        }

        public ICommand RefreshCommand { get; }
        public ICommand MarkAllReadCommand { get; }
        public ICommand MarkAsReadCommand { get; }

        public NotificationsViewModel()
        {
            RefreshCommand = new RelayCommand(() =>
            {
                NotificationService.Instance.RefreshNotifications();
                LoadNotifications();
            });
            MarkAllReadCommand = new RelayCommand(() =>
            {
                NotificationService.Instance.MarkAllAsRead();
                LoadNotifications();
            });
            MarkAsReadCommand = new RelayCommand(item =>
            {
                if (item is Notification n)
                {
                    NotificationService.Instance.MarkAsRead(n.Id);
                    LoadNotifications();
                }
            });

            LoadNotifications();
        }

        public void LoadNotifications()
        {
            var list = NotificationService.Instance.GetActiveNotifications();
            if (OnlyUnread)
            {
                list = list.Where(n => !n.IsRead).ToList();
            }
            Notifications = new ObservableCollection<Notification>(list);
        }
    }
}
