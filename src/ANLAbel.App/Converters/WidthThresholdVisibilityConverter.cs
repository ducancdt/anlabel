using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ANLAbel.App.Converters;

/// <summary>
/// Ribbon collapse (NiceLabel 10 / Office ribbon): the parameter is
/// "threshold" for the wide layout (visible at or above it) or "!threshold"
/// for the compact one (visible below it).
/// </summary>
public sealed class WidthThresholdVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var text = parameter?.ToString() ?? "0";
        var compact = text.StartsWith('!');
        var threshold = double.TryParse(text.TrimStart('!'), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0;
        var width = value is double actual && actual > 0 ? actual : double.PositiveInfinity;
        var wide = width >= threshold;
        return wide != compact ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}
