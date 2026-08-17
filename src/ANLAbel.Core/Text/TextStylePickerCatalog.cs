using System.Globalization;

namespace ANLAbel.Core.Text;

/// <summary>
/// Shared Excel-like font-size catalog for the Properties typography strip.
/// Designer, typed input and tests use the same fail-closed parse.
/// </summary>
public static class TextStylePickerCatalog
{
    public const double MinimumSizePt = 4;
    public const double MaximumSizePt = 200;

    public static IReadOnlyList<double> StandardSizesPt { get; } =
    [
        4, 5, 6, 7, 8, 9, 10, 11, 12, 14, 16, 18, 20, 24, 28, 32
    ];

    /// <summary>
    /// Closed catalog of families the product may offer. Windows inbox faces
    /// are used through the OS license (not redistributed). SIL/Apache faces
    /// are included only when already installed. Unlicensed or unknown
    /// machine fonts never appear.
    /// </summary>
    public static IReadOnlyList<string> LicensedFamilies { get; } =
    [
        "Arial",
        "Calibri",
        "Cambria",
        "Consolas",
        "Courier New",
        "Georgia",
        "Lucida Console",
        "Segoe UI",
        "Tahoma",
        "Times New Roman",
        "Verdana",
        "Bahnschrift",
        "Liberation Sans",
        "Liberation Serif",
        "Liberation Mono",
        "DejaVu Sans",
        "DejaVu Sans Mono",
        "Noto Sans",
        "Noto Sans Mono",
        "Carlito",
        "Caladea"
    ];

    public static bool IsLicensedFamily(string? family)
        => !string.IsNullOrWhiteSpace(family)
            && LicensedFamilies.Contains(family.Trim(), StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<string> FilterInstalled(IEnumerable<string> installedFamilyNames)
    {
        ArgumentNullException.ThrowIfNull(installedFamilyNames);
        var installed = new HashSet<string>(installedFamilyNames, StringComparer.OrdinalIgnoreCase);
        var licensed = LicensedFamilies.Where(installed.Contains).ToArray();
        return licensed.Length == 0 ? new[] { "Segoe UI" } : licensed;
    }

    public static bool TryParseSizePt(string? text, out double sizePt)
    {
        sizePt = 0;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        if (!double.TryParse(
                text.Trim(),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var parsed)
            || !double.IsFinite(parsed)
            || parsed < MinimumSizePt
            || parsed > MaximumSizePt)
        {
            return false;
        }

        sizePt = parsed;
        return true;
    }
}
