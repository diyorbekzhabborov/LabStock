using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using LabStock.Data;
using LabStock.Models;
using LabStock.Services;

namespace LabStock.ViewModels
{
    public class UsersViewModel : ViewModelBase
    {
        private ObservableCollection<User> _users = new();
        private User? _selectedUser;
        private bool _isEditorOpen = false;
        private bool _isNew = true;

        private int _editId;
        private string _editUsername = string.Empty;
        private string _editFullName = string.Empty;
        private string _editPassword = string.Empty;
        private UserRole _editRole = UserRole.LabAssistant;
        private bool _editIsActive = true;
        private string? _errorMessage;

        public ObservableCollection<User> Users
        {
            get => _users;
            set => SetProperty(ref _users, value);
        }

        public User? SelectedUser
        {
            get => _selectedUser;
            set => SetProperty(ref _selectedUser, value);
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

        public string EditUsername
        {
            get => _editUsername;
            set => SetProperty(ref _editUsername, value);
        }

        public string EditFullName
        {
            get => _editFullName;
            set => SetProperty(ref _editFullName, value);
        }

        public string EditPassword
        {
            get => _editPassword;
            set => SetProperty(ref _editPassword, value);
        }

        public UserRole EditRole
        {
            get => _editRole;
            set => SetProperty(ref _editRole, value);
        }

        public bool EditIsActive
        {
            get => _editIsActive;
            set => SetProperty(ref _editIsActive, value);
        }

        public string? ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public ObservableCollection<UserRole> AvailableRoles { get; } = new()
        {
            UserRole.Admin,
            UserRole.HeadOfLab,
            UserRole.LabAssistant,
            UserRole.Viewer
        };

        public ICommand RefreshCommand { get; }
        public ICommand OpenCreateCommand { get; }
        public ICommand OpenEditCommand { get; }
        public ICommand SaveUserCommand { get; }
        public ICommand CancelEditCommand { get; }
        public ICommand ToggleStatusCommand { get; }

        public UsersViewModel()
        {
            RefreshCommand = new RelayCommand(LoadUsers);
            OpenCreateCommand = new RelayCommand(OpenCreate);
            OpenEditCommand = new RelayCommand(OpenEdit, () => SelectedUser != null);
            SaveUserCommand = new RelayCommand(SaveUser);
            CancelEditCommand = new RelayCommand(() => IsEditorOpen = false);
            ToggleStatusCommand = new RelayCommand(ToggleStatus, () => SelectedUser != null);

            LoadUsers();
        }

        public void LoadUsers()
        {
            using var db = new AppDbContext();
            var list = db.Users.OrderBy(u => u.FullName).ToList();
            Users = new ObservableCollection<User>(list);
            SelectedUser = Users.FirstOrDefault();
        }

        private void OpenCreate()
        {
            ErrorMessage = null;
            IsNew = true;
            _editId = 0;
            EditUsername = string.Empty;
            EditFullName = string.Empty;
            EditPassword = string.Empty;
            EditRole = UserRole.LabAssistant;
            EditIsActive = true;
            IsEditorOpen = true;
        }

        private void OpenEdit()
        {
            if (SelectedUser == null) return;
            ErrorMessage = null;
            IsNew = false;
            _editId = SelectedUser.Id;
            EditUsername = SelectedUser.Username;
            EditFullName = SelectedUser.FullName;
            EditPassword = string.Empty; // empty means don't change
            EditRole = SelectedUser.Role;
            EditIsActive = SelectedUser.IsActive;
            IsEditorOpen = true;
        }

        private void SaveUser()
        {
            ErrorMessage = null;
            if (string.IsNullOrWhiteSpace(EditUsername) || string.IsNullOrWhiteSpace(EditFullName))
            {
                ErrorMessage = "Заполните логин и ФИО сотрудника.";
                return;
            }

            if (IsNew && string.IsNullOrWhiteSpace(EditPassword))
            {
                ErrorMessage = "Для нового пользователя обязателен пароль.";
                return;
            }

            using var db = new AppDbContext();
            if (IsNew)
            {
                if (db.Users.Any(u => u.Username.ToLower() == EditUsername.Trim().ToLower()))
                {
                    ErrorMessage = "Пользователь с таким логином уже существует.";
                    return;
                }

                var u = new User
                {
                    Username = EditUsername.Trim(),
                    FullName = EditFullName.Trim(),
                    PasswordHash = DbInitializer.HashPassword(EditPassword),
                    Role = EditRole,
                    IsActive = EditIsActive,
                    CreatedAt = DateTime.Now
                };
                db.Users.Add(u);
                db.SaveChanges();
                AuditService.Instance.Log("Создание", "Пользователи", u.Id.ToString(), $"Создан аккаунт {u.FullName} ({u.Role})");
            }
            else
            {
                var u = db.Users.Find(_editId);
                if (u != null)
                {
                    u.FullName = EditFullName.Trim();
                    u.Role = EditRole;
                    u.IsActive = EditIsActive;
                    if (!string.IsNullOrWhiteSpace(EditPassword))
                    {
                        u.PasswordHash = DbInitializer.HashPassword(EditPassword);
                    }
                    db.SaveChanges();
                    AuditService.Instance.Log("Изменение", "Пользователи", u.Id.ToString(), $"Обновлен профиль {u.FullName}");
                }
            }

            IsEditorOpen = false;
            LoadUsers();
        }

        private void ToggleStatus()
        {
            if (SelectedUser == null) return;
            using var db = new AppDbContext();
            var u = db.Users.Find(SelectedUser.Id);
            if (u != null)
            {
                u.IsActive = !u.IsActive;
                db.SaveChanges();
                AuditService.Instance.Log("Изменение", "Пользователи", u.Id.ToString(), $"Статус активности {u.FullName} изменен на: {u.IsActive}");
            }
            LoadUsers();
        }
    }
}
