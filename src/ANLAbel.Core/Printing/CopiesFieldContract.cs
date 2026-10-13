using ANLAbel.Core.Models;

namespace ANLAbel.Core.Printing;

/// <summary>
/// CORE DB08/DB10: when a CopiesField is configured, invalid cells on selected
/// rows must block the prepared batch with a row/field diagnostic — never
/// silently drop those rows while printing the rest.
/// </summary>
public sealed record CopiesFieldIssue(int SourceRowNumber, string FieldName, string Message);

public static class CopiesFieldContract
{
    public static IReadOnlyList<CopiesFieldIssue> ValidateSelectedRows(
        string? copiesField,
        IEnumerable<(int SourceRowNumber, IReadOnlyDictionary<string, string>? Row)> selectedRows)
    {
        ArgumentNullException.ThrowIfNull(selectedRows);

        if (string.IsNullOrWhiteSpace(copiesField))
        {
            return Array.Empty<CopiesFieldIssue>();
        }

        var fieldName = copiesField.Trim();
        var issues = new List<CopiesFieldIssue>();
        foreach (var (sourceRowNumber, row) in selectedRows)
        {
            if (DatabaseConfig.TryResolveCopiesForRow(fieldName, row, out _, out var error))
            {
                continue;
            }

            issues.Add(new CopiesFieldIssue(
                sourceRowNumber,
                fieldName,
                error ?? $"Copies field '{fieldName}' is invalid."));
        }

        return issues;
    }

    /// <summary>
    /// Prepare/print authorization gate: any invalid configured CopiesField on a
    /// selected row blocks the entire batch (operator must fix or unselect).
    /// </summary>
    public static bool TryAuthorizePrepare(
        string? copiesField,
        IEnumerable<(int SourceRowNumber, IReadOnlyDictionary<string, string>? Row)> selectedRows,
        out IReadOnlyList<CopiesFieldIssue> blockingIssues)
    {
        blockingIssues = ValidateSelectedRows(copiesField, selectedRows);
        return blockingIssues.Count == 0;
    }

    public static string ToUserMessage(IReadOnlyList<CopiesFieldIssue> issues)
    {
        ArgumentNullException.ThrowIfNull(issues);
        if (issues.Count == 0)
        {
            return string.Empty;
        }

        var lines = issues
            .OrderBy(issue => issue.SourceRowNumber)
            .Select(issue =>
                $"- Row {issue.SourceRowNumber}, field '{issue.FieldName}': {issue.Message}");
        return
            "Printing is blocked because one or more selected rows have an invalid copies value. " +
            "Fix the Excel/CSV cell or unselect the row, then try again." +
            Environment.NewLine +
            string.Join(Environment.NewLine, lines);
    }
}
