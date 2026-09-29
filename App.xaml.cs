using System;
using System.Windows;
using LabStock.Data;

namespace LabStock
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                using var db = new AppDbContext();
                DbInitializer.Initialize(db);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка инициализации базы данных:\n{ex.Message}", "LabStock - Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
