using System.Globalization;
using System.Windows.Data;
using ANLAbel.App.Services;

namespace ANLAbel.App.Converters;

/// <summary>Shows an object type or option value by its Vietnamese name; the bound value is unchanged.</summary>
public sealed class DisplayNameConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => DisplayNames.For(value);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => Binding.DoNothing;
}
