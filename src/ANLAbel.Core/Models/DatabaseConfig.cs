using System.Collections.ObjectModel;
using System.Globalization;
using ANLAbel.Core.Printing;

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
    /// Optional Excel/CSV column whose value sets how many labels to print for each row
    /// in Print Preview. Empty means every row defaults to 1 copy (operator may edit).
    /// When configured, blank/non-integer/negative/overflow values fail closed (DB08) —
    /// they do not silently become 1 and are not silently capped.
    /// </summary>
    public string CopiesField { get; set; } = string.Empty;
    public ObservableCollection<DatabaseField> AvailableFields { get; set; } = new();
    public ObservableCollection<DatabaseField> LabelFields { get; set; } = new();

    /// <summary>
    /// Resolves copies for one row. When <paramref name="copiesField"/> is unset, returns 1.
    /// When the field is configured and the cell is invalid, returns 0 (keeps the record out
    /// of the prepared sequence) — callers that need a diagnostic must use
    /// <see cref="TryResolveCopiesForRow"/>.
    /// </summary>
    public static int ResolveCopiesForRow(string? copiesField, IReadOnlyDictionary<string, string>? row)
    {
        return TryResolveCopiesForRow(copiesField, row, out var copies, out _) ? copies : 0;
    }

    /// <summary>
    /// Fail-closed copies resolution (CORE DB08). Unconfigured field → 1 copy.
    /// Configured field requires a whole number in 0..<see cref="QuantityPlan.MaximumCopiesPerRecord"/>.
    /// Blank, missing, non-integer, negative, and overflow values set <paramref name="error"/> and return false.
    /// </summary>
    public static bool TryResolveCopiesForRow(
        string? copiesField,
        IReadOnlyDictionary<string, string>? row,
        out int copies,
        out string? error)
    {
        copies = 1;
        error = null;

        if (string.IsNullOrWhiteSpace(copiesField))
        {
            return true;
        }

        var fieldName = copiesField.Trim();
        if (row is null)
        {
            copies = 0;
            error = $"Copies field '{fieldName}' cannot be resolved because the row is missing.";
            return false;
        }

        if (!row.TryGetValue(fieldName, out var rawValue))
        {
            copies = 0;
            error = $"Copies field '{fieldName}' is missing from the row.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            copies = 0;
            error = $"Copies field '{fieldName}' is blank.";
            return false;
        }

        if (!int.TryParse(rawValue.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            copies = 0;
            error = $"Copies field '{fieldName}' value '{rawValue}' is not a whole number.";
            return false;
        }

        if (parsed < 0)
        {
            copies = 0;
            error = $"Copies field '{fieldName}' value {parsed} is negative.";
            return false;
        }

        if (parsed > QuantityPlan.MaximumCopiesPerRecord)
        {
            copies = 0;
            error = $"Copies field '{fieldName}' value {parsed} exceeds the limit of {QuantityPlan.MaximumCopiesPerRecord}.";
            return false;
        }

        copies = parsed;
        return true;
    }
}
