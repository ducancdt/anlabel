using System.Collections.ObjectModel;

namespace ANLAbel.Core.Models;

public sealed class DatabaseConfig
{
    public string DataSourceId { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public string SheetName { get; set; } = string.Empty;
    public int HeaderRowIndex { get; set; } = 1;
    public string KeyField { get; set; } = string.Empty;
    public string KeyValue { get; set; } = string.Empty;
    public int LastSelectedRow { get; set; }

    /// <summary>
    /// Optional Excel column name whose value sets how many labels to print for each row
    /// in Print Preview (database-manager-module-plan.md M4 — NiceLabel calls this "label
    /// copies per record"). Empty means every row defaults to 1 copy, editable by hand as
    /// before. Missing/non-numeric/negative values for a given row fall back to 1 rather
    /// than blocking the preview.
    /// </summary>
    public string CopiesField { get; set; } = string.Empty;
    public ObservableCollection<DatabaseField> AvailableFields { get; set; } = new();
    public ObservableCollection<DatabaseField> LabelFields { get; set; } = new();

    /// <summary>
    /// Resolves how many copies to print for one Excel row given a (possibly empty)
    /// <see cref="CopiesField"/> column name. Pure/static so it is unit-testable without a
    /// WPF window — the actual call site is <c>PrintPreviewWindow.RefreshPreview</c>. Always
    /// falls back to 1 (never throws, never returns a negative count) so a blank or malformed
    /// cell can never block opening Print Preview.
    /// </summary>
    public static int ResolveCopiesForRow(string? copiesField, IReadOnlyDictionary<string, string>? row)
    {
        if (string.IsNullOrWhiteSpace(copiesField) || row is null || !row.TryGetValue(copiesField, out var rawValue))
        {
            return 1;
        }

        return int.TryParse(rawValue, out var copies) && copies >= 0 ? Math.Min(copies, 999) : 1;
    }
}
