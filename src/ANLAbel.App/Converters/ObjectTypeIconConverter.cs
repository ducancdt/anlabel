using System.Globalization;
using System.Windows.Data;
using ANLAbel.App.Controls;
using ANLAbel.Core.Enums;

namespace ANLAbel.App.Converters;

/// <summary>Shows an object type by its vector icon (<see cref="RibbonIconSet.ForObjectType"/>).</summary>
public sealed class ObjectTypeIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is ObjectType type ? RibbonIconSet.ForObjectType(type) : null;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => Binding.DoNothing;
}
