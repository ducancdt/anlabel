using System.Globalization;
using System.Windows.Data;

namespace ANLAbel.App.Converters;

/// <summary>
/// The binding a Data panel entry gives when dropped, for its tooltip: {?Name} for a prompt
/// (parameter "prompt"), {Name} for a column or a function. StringFormat does not apply to a
/// ToolTip, which is not a string property.
/// </summary>
public sealed class DataTokenConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is string name && name.Length > 0
            ? string.Equals(parameter as string, "prompt", StringComparison.Ordinal) ? "{?" + name + "}" : "{" + name + "}"
            : string.Empty;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}
