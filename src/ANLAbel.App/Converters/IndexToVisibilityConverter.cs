using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ANLAbel.App.Converters;

/// <summary>Shows a sub-page when the tab index equals the parameter.</summary>
public sealed class IndexToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is int index && int.TryParse(parameter?.ToString(), out var page) && index == page
            ? Visibility.Visible
            : Visibility.Collapsed;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}
