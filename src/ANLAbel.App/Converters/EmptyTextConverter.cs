using System.Globalization;
using System.Windows.Data;

namespace ANLAbel.App.Converters;

/// <summary>
/// Shows the parameter text when the bound text is null, empty or blank
/// (TargetNullValue only covers null), e.g. "Chưa chọn máy in".
/// </summary>
public sealed class EmptyTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is string text && !string.IsNullOrWhiteSpace(text) ? text : parameter?.ToString() ?? string.Empty;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}
