using System.Globalization;
using System.Windows.Data;

namespace ANLAbel.App.Converters;

/// <summary>Two-way negation, e.g. a "Không in" box bound to IsPrintable.</summary>
public sealed class InverseBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is not true;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is not true;
}
