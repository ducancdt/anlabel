using System.Data;
using System.Text.RegularExpressions;
using ANLAbel.Core.Data;
using ANLAbel.Core.Expressions;
using ANLAbel.Core.Expressions.Formulas;
using ANLAbel.Core.Models;
using ANLAbel.Data.Excel;
using ANLAbel.Printing.PrinterProfiles;

namespace ANLAbel.App.Services;

/// <summary>
/// DB03: a field whose column has a blank or repeated header is known only by
/// the column's position. Adding, removing or swapping columns in the file can
/// then make the label print another column without any visible change, and a
/// swap of two columns with the same header cannot be detected at all. Such a
/// binding blocks printing until the header is unique. Messages stay English for
/// logs and tests; <see cref="PreflightIssueText"/> words them for the operator.
/// </summary>
public static partial class DataHeaderCheck
{
    /// <summary>Template-level blocking issues for every binding that reads a position-only column.</summary>
    public static IReadOnlyList<PrintPreflightIssue> FindPositionalBindings(LabelTemplate template, DataTable? table)
    {
        ArgumentNullException.ThrowIfNull(template);
        var origins = DataColumnOrigin.Describe(table);
        if (!origins.Any(origin => origin.DependsOnPosition))
        {
            return Array.Empty<PrintPreflightIssue>();
        }

        var issues = new List<PrintPreflightIssue>();
        foreach (var item in template.Objects.Where(item => item.IsVisible && !string.IsNullOrWhiteSpace(item.BindingExpression)))
        {
            foreach (var origin in ColumnsRead(item.BindingExpression, origins, template.DataTransforms).Where(origin => origin.DependsOnPosition))
            {
                issues.Add(new PrintPreflightIssue(0, item.Name, item.Type.ToString(), Message(origin, origins)));
            }
        }

        // The copies column decides how many labels print, so it must be as unambiguous.
        var copiesField = template.DatabaseConfig.CopiesField;
        if (!string.IsNullOrWhiteSpace(copiesField)
            && origins.FirstOrDefault(origin => string.Equals(origin.FieldName, copiesField.Trim(), StringComparison.OrdinalIgnoreCase)) is { DependsOnPosition: true } copies)
        {
            issues.Add(new PrintPreflightIssue(0, copies.FieldName, "Copies", Message(copies, origins)));
        }

        return issues;
    }

    /// <summary>Puts <see cref="FindPositionalBindings"/> in front of a preflight result.</summary>
    public static PrintPreflightResult Append(PrintPreflightResult result, LabelTemplate template, DataTable? table)
    {
        ArgumentNullException.ThrowIfNull(result);
        var positional = FindPositionalBindings(template, table);
        return positional.Count == 0 ? result : new PrintPreflightResult(positional.Concat(result.Issues).ToArray());
    }

    /// <summary>
    /// Columns an expression reads, directly or through the template's data
    /// transforms, matched with the same name rules as printing.
    /// </summary>
    public static IReadOnlyList<DataColumnOrigin> ColumnsRead(
        string expression,
        IReadOnlyList<DataColumnOrigin> origins,
        IEnumerable<DataTransformDefinition>? transforms)
    {
        var transformByName = new Dictionary<string, DataTransformDefinition>(StringComparer.OrdinalIgnoreCase);
        foreach (var transform in transforms ?? Array.Empty<DataTransformDefinition>())
        {
            if (!string.IsNullOrWhiteSpace(transform.Name))
            {
                transformByName.TryAdd(transform.Name.Trim(), transform);
            }
        }

        var read = new List<DataColumnOrigin>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        void Walk(IEnumerable<string> fields)
        {
            foreach (var field in fields)
            {
                if (Resolve(field, origins) is { } origin)
                {
                    if (!read.Contains(origin))
                    {
                        read.Add(origin);
                    }
                }
                else if (transformByName.TryGetValue(field.Trim(), out var transform) && visited.Add(transform.Name.Trim()))
                {
                    Walk(FormulaFields(transform.Formula));
                }
            }
        }

        Walk(BindingFields(expression));
        return read;
    }

    /// <summary>Field names an expression reads: <c>{Field}</c> placeholders or formula <c>FIELD("Field")</c>.</summary>
    public static IReadOnlyList<string> BindingFields(string? expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return Array.Empty<string>();
        }

        return FormulaBindingEvaluator.LooksLikeFormula(expression)
            ? FormulaFields(expression)
            : BindingExpressionEvaluator.GetFields(expression);
    }

    /// <summary>The column a field name reads, or null when the data has no such column.</summary>
    public static DataColumnOrigin? Resolve(string field, IReadOnlyList<DataColumnOrigin> origins)
        => FieldNameResolver.TryResolveFieldName(field, origins.Select(origin => origin.FieldName), out var resolved)
            ? origins.FirstOrDefault(origin => string.Equals(origin.FieldName, resolved, StringComparison.OrdinalIgnoreCase))
            : null;

    private static IReadOnlyList<string> FormulaFields(string? formula)
        => FormulaField().Matches(formula ?? string.Empty)
            .Select(match => Regex.Unescape(match.Groups[1].Value).Trim())
            .Where(field => field.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static string Message(DataColumnOrigin origin, IReadOnlyList<DataColumnOrigin> origins)
    {
        if (origin.Kind == DataHeaderKind.Blank)
        {
            return $"Field '{origin.FieldName}' reads column {origin.ColumnLetter}, whose header cell is empty, so only its position identifies it. Give the column a header in the data file, then refresh the data.";
        }

        var columns = origins
            .Where(other => other.Kind == DataHeaderKind.Repeated
                            && string.Equals(other.SourceHeader, origin.SourceHeader, StringComparison.OrdinalIgnoreCase))
            .Select(other => other.ColumnLetter);
        return $"Field '{origin.FieldName}' reads column {origin.ColumnLetter}, whose header '{origin.SourceHeader}' is repeated in columns {string.Join(", ", columns)}, so only its position identifies it. Give each column a unique header in the data file, then refresh the data.";
    }

    [GeneratedRegex("FIELD\\(\\s*\"((?:\\\\.|[^\"])*)\"\\s*\\)", RegexOptions.IgnoreCase)]
    private static partial Regex FormulaField();
}
