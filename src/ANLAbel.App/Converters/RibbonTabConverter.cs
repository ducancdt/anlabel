using System.Globalization;
using System.Windows;
using System.Windows.Data;
using ANLAbel.App.ViewModels;

namespace ANLAbel.App.Converters;

/// <summary>
/// Binds a ribbon tab header (bool) or panel (Visibility) to
/// <see cref="MainViewModel.SelectedRibbonTab"/>; the parameter names the tab.
/// </summary>
public sealed class RibbonTabConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var selected = value is RibbonTab tab && Enum.TryParse<RibbonTab>(parameter?.ToString(), out var own) && tab == own;
        return targetType == typeof(Visibility) ? (selected ? Visibility.Visible : Visibility.Collapsed) : selected;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true && Enum.TryParse<RibbonTab>(parameter?.ToString(), out var own) ? own : Binding.DoNothing;
}
