using System.Globalization;
using System.Windows.Data;
using ANLAbel.App.Services;
using ANLAbel.Project.SaveLoad;

namespace ANLAbel.App.Converters;

/// <summary>Shows a revision column (kind, status, diagnostic) in Vietnamese; the entries keep their stored values.</summary>
public sealed class RevisionTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        ProjectRevisionKind kind => RevisionText.Kind(kind),
        string text when string.Equals(parameter as string, "Status", StringComparison.Ordinal) => RevisionText.Status(text),
        string text when string.Equals(parameter as string, "Diagnostic", StringComparison.Ordinal) => RevisionText.Diagnostic(text),
        null => string.Empty,
        _ => value.ToString() ?? string.Empty,
    };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => Binding.DoNothing;
}
