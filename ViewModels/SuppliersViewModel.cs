using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using LabStock.Data;
using LabStock.Models;
using LabStock.Services;

namespace LabStock.ViewModels
{
    public class SuppliersViewModel : ViewModelBase
    {
        private ObservableCollection<Supplier> _suppliers = new();
        private Supplier? _selectedSupplier;
        private bool _isEditorOpen = false;
        private bool _isNew = true;

        private int _editId;
        private string _editName = string.Empty;
        private string? _editContact;
        private string? _editPhone;
        private string? _editEmail;
        private string? _editAddress;
        private string? _editNotes;

        public ObservableCollection<Supplier> Suppliers
        {
            get => _suppliers;
            set => SetProperty(ref _suppliers, value);
        }

        public Supplier? SelectedSupplier
        {
            get => _selectedSupplier;
            set => SetProperty(ref _selectedSupplier, value);
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

        public string EditName
        {
            get => _editName;
            set => SetProperty(ref _editName, value);
        }

        public string? EditContact
        {
            get => _editContact;
            set => SetProperty(ref _editContact, value);
        }

        public string? EditPhone
        {
            get => _editPhone;
            set => SetProperty(ref _editPhone, value);
        }

        public string? EditEmail
        {
            get => _editEmail;
            set => SetProperty(ref _editEmail, value);
        }

        public string? EditAddress
        {
            get => _editAddress;
            set => SetProperty(ref _editAddress, value);
        }

        public string? EditNotes
        {
            get => _editNotes;
            set => SetProperty(ref _editNotes, value);
        }

        public ICommand RefreshCommand { get; }
        public ICommand OpenCreateCommand { get; }
        public ICommand OpenEditCommand { get; }
        public ICommand SaveSupplierCommand { get; }
        public ICommand CancelEditCommand { get; }
        public ICommand DeleteSupplierCommand { get; }

        public SuppliersViewModel()
        {
            RefreshCommand = new RelayCommand(LoadSuppliers);
            OpenCreateCommand = new RelayCommand(OpenCreate);
            OpenEditCommand = new RelayCommand(OpenEdit, () => SelectedSupplier != null);
            SaveSupplierCommand = new RelayCommand(SaveSupplier);
            CancelEditCommand = new RelayCommand(() => IsEditorOpen = false);
            DeleteSupplierCommand = new RelayCommand(DeleteSupplier, () => SelectedSupplier != null);

            LoadSuppliers();
        }

        public void LoadSuppliers()
        {
            using var db = new AppDbContext();
            var list = db.Suppliers.OrderBy(s => s.Name).ToList();
            Suppliers = new ObservableCollection<Supplier>(list);
            SelectedSupplier = Suppliers.FirstOrDefault();
        }

        private void OpenCreate()
        {
            IsNew = true;
            _editId = 0;
            EditName = string.Empty;
            EditContact = string.Empty;
            EditPhone = string.Empty;
            EditEmail = string.Empty;
            EditAddress = string.Empty;
            EditNotes = string.Empty;
            IsEditorOpen = true;
        }

        private void OpenEdit()
        {
            if (SelectedSupplier == null) return;
            IsNew = false;
            _editId = SelectedSupplier.Id;
            EditName = SelectedSupplier.Name;
            EditContact = SelectedSupplier.ContactPerson;
            EditPhone = SelectedSupplier.Phone;
            EditEmail = SelectedSupplier.Email;
            EditAddress = SelectedSupplier.Address;
            EditNotes = SelectedSupplier.Notes;
            IsEditorOpen = true;
        }

        private void SaveSupplier()
        {
            if (string.IsNullOrWhiteSpace(EditName)) return;

            using var db = new AppDbContext();
            if (IsNew)
            {
                var s = new Supplier
                {
                    Name = EditName.Trim(),
                    ContactPerson = EditContact?.Trim(),
                    Phone = EditPhone?.Trim(),
                    Email = EditEmail?.Trim(),
                    Address = EditAddress?.Trim(),
                    Notes = EditNotes?.Trim()
                };
                db.Suppliers.Add(s);
                db.SaveChanges();
                AuditService.Instance.Log("Создание", "Поставщики", s.Id.ToString(), $"Добавлен поставщик: {s.Name}");
            }
            else
            {
                var s = db.Suppliers.Find(_editId);
                if (s != null)
                {
                    s.Name = EditName.Trim();
                    s.ContactPerson = EditContact?.Trim();
                    s.Phone = EditPhone?.Trim();
                    s.Email = EditEmail?.Trim();
                    s.Address = EditAddress?.Trim();
                    s.Notes = EditNotes?.Trim();
                    db.SaveChanges();
                    AuditService.Instance.Log("Изменение", "Поставщики", s.Id.ToString(), $"Обновлен поставщик: {s.Name}");
                }
            }

            IsEditorOpen = false;
            LoadSuppliers();
        }

        private void DeleteSupplier()
        {
            if (SelectedSupplier == null) return;
            using var db = new AppDbContext();
            var s = db.Suppliers.Find(SelectedSupplier.Id);
            if (s != null)
            {
                db.Suppliers.Remove(s);
                db.SaveChanges();
                AuditService.Instance.Log("Удаление", "Поставщики", SelectedSupplier.Id.ToString(), $"Удален поставщик: {s.Name}");
            }
            LoadSuppliers();
        }
    }
}
