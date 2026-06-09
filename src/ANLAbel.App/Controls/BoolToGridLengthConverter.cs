using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ANLAbel.App.Controls;

public sealed class BoolToGridLengthConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not true)
        {
            return new GridLength(0);
        }

        var requested = parameter?.ToString();
        if (string.IsNullOrWhiteSpace(requested))
        {
            return GridLength.Auto;
        }

        if (requested == "*")
        {
            return new GridLength(1, GridUnitType.Star);
        }

        return double.TryParse(requested, NumberStyles.Float, CultureInfo.InvariantCulture, out var pixels)
            ? new GridLength(pixels)
            : GridLength.Auto;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is GridLength gridLength && gridLength.Value > 0;
    }
}
