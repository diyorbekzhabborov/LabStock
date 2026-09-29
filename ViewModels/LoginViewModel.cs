using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using LabStock.Services;

namespace LabStock.ViewModels
{
    public class DemoUserItem
    {
        public string Title { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string RoleBadge { get; set; } = string.Empty;
    }

    public class LoginViewModel : ViewModelBase
    {
        private string _username = "admin";
        private string _password = "admin123";
        private string? _errorMessage;
        private bool _isLoading;

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public string? ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ObservableCollection<DemoUserItem> DemoUsers { get; } = new()
        {
            new DemoUserItem { Title = "Алимов Рустам (Администратор)", Username = "admin", Password = "admin123", RoleBadge = "Полный доступ" },
            new DemoUserItem { Title = "Каримова Нигора (Заведующий)", Username = "head", Password = "head123", RoleBadge = "Склад и анализы" },
            new DemoUserItem { Title = "Саидов Фарход (Лаборант)", Username = "lab", Password = "lab123", RoleBadge = "Списание и работа" },
            new DemoUserItem { Title = "Назаров Далер (Аудитор/Просмотр)", Username = "viewer", Password = "viewer123", RoleBadge = "Только чтение" }
        };

        public ICommand LoginCommand { get; }
        public ICommand SelectDemoUserCommand { get; }

        public event Action? OnLoginSuccess;

        public LoginViewModel()
        {
            LoginCommand = new RelayCommand(ExecuteLogin);
            SelectDemoUserCommand = new RelayCommand(param =>
            {
                if (param is DemoUserItem item)
                {
                    Username = item.Username;
                    Password = item.Password;
                    ErrorMessage = null;
                }
            });
        }

        private void ExecuteLogin()
        {
            ErrorMessage = null;
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Пожалуйста, введите логин и пароль.";
                return;
            }

            IsLoading = true;
            try
            {
                bool success = AuthService.Instance.Login(Username, Password);
                if (success)
                {
                    OnLoginSuccess?.Invoke();
                }
                else
                {
                    ErrorMessage = "Неверное имя пользователя или пароль.";
                }
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
