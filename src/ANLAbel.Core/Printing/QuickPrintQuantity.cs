using ANLAbel.Core.Models;

namespace ANLAbel.Core.Printing;

/// <summary>
/// Expands quick-print rows using the same copies rules as preview prepare.
/// A configured copies column that is blank, non-integer, negative, or over
/// the limit blocks the batch. A manual copy count is never silently capped.
/// </summary>
public static class QuickPrintQuantity
{
    public static bool TryExpand(
        DatabaseConfig? database,
        int fallbackCopies,
        IReadOnlyList<(int SourceRowNumber, IReadOnlyDictionary<string, string>? Row)> sourceRows,
        out IReadOnlyList<IReadOnlyDictionary<string, string>?> expanded,
        out string error)
        => TryExpand(database, fallbackCopies, sourceRows, out expanded, out _, out error);

    public static bool TryExpand(
        DatabaseConfig? database,
        int fallbackCopies,
        IReadOnlyList<(int SourceRowNumber, IReadOnlyDictionary<string, string>? Row)> sourceRows,
        out IReadOnlyList<IReadOnlyDictionary<string, string>?> expanded,
        out IReadOnlyList<PrintDispatchCopy> sequence,
        out string error)
    {
        ArgumentNullException.ThrowIfNull(sourceRows);
        var normalizedField = NormalizeField(database?.CopiesField);
        if (!string.IsNullOrEmpty(normalizedField))
        {
            if (!CopiesFieldContract.TryAuthorizePrepare(normalizedField, sourceRows, out var issues))
            {
                expanded = Array.Empty<IReadOnlyDictionary<string, string>?>();
                sequence = Array.Empty<PrintDispatchCopy>();
                error = CopiesFieldContract.ToUserMessage(issues);
                return false;
            }

            var labels = new List<IReadOnlyDictionary<string, string>?>();
            var identities = new List<PrintDispatchCopy>();
            foreach (var (sourceRowNumber, row) in sourceRows)
            {
                if (!DatabaseConfig.TryResolveCopiesForRow(normalizedField, row, out var copies, out var resolveError))
                {
                    expanded = Array.Empty<IReadOnlyDictionary<string, string>?>();
                    sequence = Array.Empty<PrintDispatchCopy>();
                    error = resolveError ?? $"Copies field '{normalizedField}' is invalid on row {sourceRowNumber}.";
                    return false;
                }

                AppendCopies(labels, identities, sourceRowNumber, copies, row);
            }

            if (labels.Count == 0)
            {
                expanded = Array.Empty<IReadOnlyDictionary<string, string>?>();
                sequence = Array.Empty<PrintDispatchCopy>();
                error = "No labels to print. Selected rows have zero copies.";
                return false;
            }

            if (labels.Count > QuantityPlan.MaximumPreparedLabelCount)
            {
                expanded = Array.Empty<IReadOnlyDictionary<string, string>?>();
                sequence = Array.Empty<PrintDispatchCopy>();
                error = $"A print job cannot exceed {QuantityPlan.MaximumPreparedLabelCount} labels; the selected rows need {labels.Count}.";
                return false;
            }

            expanded = labels;
            sequence = identities;
            error = string.Empty;
            return true;
        }

        if (fallbackCopies < 1 || fallbackCopies > QuantityPlan.MaximumCopiesPerRecord)
        {
            expanded = Array.Empty<IReadOnlyDictionary<string, string>?>();
            sequence = Array.Empty<PrintDispatchCopy>();
            error = $"Copies must be a whole number from 1 to {QuantityPlan.MaximumCopiesPerRecord}. The current value is {fallbackCopies}.";
            return false;
        }

        if ((long)sourceRows.Count * fallbackCopies > QuantityPlan.MaximumPreparedLabelCount)
        {
            expanded = Array.Empty<IReadOnlyDictionary<string, string>?>();
            sequence = Array.Empty<PrintDispatchCopy>();
            error = $"A print job cannot exceed {QuantityPlan.MaximumPreparedLabelCount} labels; the selected rows need {(long)sourceRows.Count * fallbackCopies}.";
            return false;
        }

        var repeated = new List<IReadOnlyDictionary<string, string>?>(sourceRows.Count * fallbackCopies);
        var repeatedIdentities = new List<PrintDispatchCopy>(sourceRows.Count * fallbackCopies);
        foreach (var (sourceRowNumber, row) in sourceRows)
        {
            AppendCopies(repeated, repeatedIdentities, sourceRowNumber, fallbackCopies, row);
        }

        if (repeated.Count == 0)
        {
            expanded = Array.Empty<IReadOnlyDictionary<string, string>?>();
            sequence = Array.Empty<PrintDispatchCopy>();
            error = "No labels to print.";
            return false;
        }

        expanded = repeated;
        sequence = repeatedIdentities;
        error = string.Empty;
        return true;
    }

    private static void AppendCopies(
        List<IReadOnlyDictionary<string, string>?> labels,
        List<PrintDispatchCopy> identities,
        int sourceRowNumber,
        int copies,
        IReadOnlyDictionary<string, string>? row)
    {
        for (var copy = 1; copy <= copies; copy++)
        {
            labels.Add(row);
            identities.Add(new PrintDispatchCopy(sourceRowNumber, copy, labels.Count));
        }
    }

    public static string NormalizeField(string? copiesField)
    {
        if (string.IsNullOrWhiteSpace(copiesField))
        {
            return string.Empty;
        }

        var field = copiesField.Trim();
        if (field.StartsWith('{') && field.EndsWith('}') && field.Length > 2)
        {
            field = field[1..^1].Trim();
        }

        return field;
    }
}
