using System.Globalization;
using System.Windows.Data;
using ANLAbel.App.Services;
using ANLAbel.Core.Printing;

namespace ANLAbel.App.Converters;

/// <summary>
/// Shows a Print Center column (state, action, queue, reason) in Vietnamese;
/// the job records and the selection keep their stored values.
/// </summary>
public sealed class PrintCenterTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        PrintJobLifecycleState state => PrintCenterText.State(state),
        PrintJobOperatorAction action => PrintCenterText.Action(action),
        string text when string.Equals(parameter as string, "Queue", StringComparison.Ordinal) => PrintCenterText.Queue(text),
        string text when string.Equals(parameter as string, "Reason", StringComparison.Ordinal) => PrintCenterText.Reason(text),
        null => string.Empty,
        _ => value.ToString() ?? string.Empty,
    };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => Binding.DoNothing;
}
