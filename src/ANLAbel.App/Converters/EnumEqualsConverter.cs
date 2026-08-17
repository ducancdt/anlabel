using System.Globalization;
using System.Windows.Data;
using ANLAbel.Core.Text;

namespace ANLAbel.App.Converters;

/// <summary>
/// Two-way radio binding for alignment icons. ConvertBack only writes when
/// the icon is turned on, through <see cref="TextStyleAlignmentContract"/>.
/// </summary>
public sealed class EnumEqualsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not Enum current || parameter is not Enum icon)
        {
            return false;
        }

        return TextStyleAlignmentContract.IsOn(current, icon);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (parameter is not Enum icon)
        {
            return Binding.DoNothing;
        }

        if (value is not true)
        {
            return Binding.DoNothing;
        }

        return icon;
    }
}
