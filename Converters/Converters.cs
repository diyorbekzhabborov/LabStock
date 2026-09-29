using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using LabStock.Models;

namespace LabStock.Converters
{
    public class StatusToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is StockStatus status)
            {
                return status switch
                {
                    StockStatus.Normal => "Норма",
                    StockStatus.Attention => "Внимание",
                    StockStatus.Low => "Низкий остаток",
                    StockStatus.OutOfStock => "Нет на складе",
                    _ => status.ToString()
                };
            }
            return value?.ToString() ?? "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class StatusToForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is StockStatus status)
            {
                return status switch
                {
                    StockStatus.Normal => new SolidColorBrush(Color.FromRgb(21, 128, 61)),    // #15803D (Green 700)
                    StockStatus.Attention => new SolidColorBrush(Color.FromRgb(180, 83, 9)),   // #B45309 (Amber 700)
                    StockStatus.Low => new SolidColorBrush(Color.FromRgb(185, 28, 28)),       // #B91C1C (Red 700)
                    StockStatus.OutOfStock => new SolidColorBrush(Color.FromRgb(153, 27, 27)),// #991B1B (Red 800)
                    _ => Brushes.Black
                };
            }
            return Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class StatusToBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is StockStatus status)
            {
                return status switch
                {
                    StockStatus.Normal => new SolidColorBrush(Color.FromRgb(220, 252, 231)),    // #DCFCE7 (Green 100)
                    StockStatus.Attention => new SolidColorBrush(Color.FromRgb(254, 243, 199)), // #FEF3C7 (Amber 100)
                    StockStatus.Low => new SolidColorBrush(Color.FromRgb(254, 226, 226)),       // #FEE2E2 (Red 100)
                    StockStatus.OutOfStock => new SolidColorBrush(Color.FromRgb(254, 202, 202)),// #FECACA (Red 200)
                    _ => Brushes.Transparent
                };
            }
            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class QuantityDeltaColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal d)
            {
                if (d > 0) return new SolidColorBrush(Color.FromRgb(21, 128, 61)); // Green
                if (d < 0) return new SolidColorBrush(Color.FromRgb(220, 38, 38)); // Red
            }
            return Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class QuantityDeltaTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal d)
            {
                if (d > 0) return $"+{d:0.##}";
                return d.ToString("0.##");
            }
            return value?.ToString() ?? "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
            {
                return b ? Visibility.Collapsed : Visibility.Visible;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class ZeroToCollapsedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int i && i == 0) return Visibility.Collapsed;
            if (value is decimal d && d == 0) return Visibility.Collapsed;
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return Visibility.Collapsed;
            if (value is string s && string.IsNullOrWhiteSpace(s)) return Visibility.Collapsed;
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class MovementTypeToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is MovementType type)
            {
                return type.ToFriendlyName();
            }
            return value?.ToString() ?? "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class MultiplierConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int intVal)
            {
                double factor = 6.0;
                if (parameter != null && double.TryParse(parameter.ToString(), out double p)) factor = p;
                return Math.Max(4, Math.Min(180, intVal * factor));
            }
            return 20.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class BooleanToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b && b)
            {
                return new SolidColorBrush(Color.FromRgb(220, 38, 38)); // Red if expired
            }
            return new SolidColorBrush(Color.FromRgb(15, 23, 42)); // Default
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
