using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FileForge.App;

/// <summary>
/// Converts a boolean to Visibility. true → Visible, false → Collapsed.
/// </summary>
public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is Visibility.Visible;
}
