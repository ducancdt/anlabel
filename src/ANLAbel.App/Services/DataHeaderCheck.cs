using System.Data;
using ANLAbel.Core.Data;
using ANLAbel.Core.Expressions;
using ANLAbel.Core.Expressions.Formulas;
using ANLAbel.Core.Models;
using ANLAbel.Core.Printing;
using ANLAbel.Data.Excel;
using ANLAbel.Printing.PrinterProfiles;

namespace ANLAbel.App.Services;

/// <summary>
/// A column a binding reads (<paramref name="RequestedName"/> is the name in the
/// binding) and the lookalike columns whose names tie with it.
/// </summary>
public sealed record DataColumnRead(string RequestedName, DataColumnOrigin Column, IReadOnlyList<DataColumnOrigin> Rivals)
{
    /// <summary>True when only the column's position decides that it is the one read.</summary>
    public bool DependsOnPosition => Column.DependsOnPosition || Rivals.Count > 0;
}

/// <summary>
/// DB03: a field whose column has a blank or repeated header, or whose name
/// matches several lookalike headers equally, is known only by the column's
/// position. Adding, removing or swapping columns in the file can then make the
/// label print another column without any visible change, and a swap of two
/// columns with the same header cannot be detected at all. Such a binding blocks
/// printing until the header is unique. Fields are found and matched the way
/// printing reads them: formulas are evaluated, and names resolve over the
/// columns and the data transform outputs. Messages stay English for logs and
/// tests; <see cref="PreflightIssueText"/> words them for the operator.
/// </summary>
public static class DataHeaderCheck
{
    private static readonly IReadOnlyDictionary<string, string> EmptyRow = new Dictionary<string, string>();

    /// <summary>Template-level blocking issues for every binding that reads a position-only column.</summary>
    public static IReadOnlyList<PrintPreflightIssue> FindPositionalBindings(LabelTemplate template, DataTable? table)
    {
        ArgumentNullException.ThrowIfNull(template);
        var origins = DataColumnOrigin.Describe(table);
        if (origins.Count == 0)
        {
            return Array.Empty<PrintPreflightIssue>();
        }

        var issues = new List<PrintPreflightIssue>();
        foreach (var item in template.Objects.Where(item => item.IsVisible && !string.IsNullOrWhiteSpace(item.BindingExpression)))
        {
            foreach (var read in ColumnsRead(item.BindingExpression, origins, template.DataTransforms).Where(read => read.DependsOnPosition))
            {
                issues.Add(new PrintPreflightIssue(0, item.Name, item.Type.ToString(), Message(read, origins)));
            }
        }

        // The copies column decides how many labels print, so it must be as unambiguous.
        if (CopiesColumn(template, origins) is { DependsOnPosition: true } copies)
        {
            issues.Add(new PrintPreflightIssue(0, copies.Column.FieldName, "Copies", Message(copies, origins)));
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

    /// <summary>The copies column, named the way quick print reads it (braces allowed).</summary>
    public static DataColumnRead? CopiesColumn(LabelTemplate template, IReadOnlyList<DataColumnOrigin> origins)
    {
        ArgumentNullException.ThrowIfNull(template);
        var field = QuickPrintQuantity.NormalizeField(template.DatabaseConfig.CopiesField);
        var column = field.Length == 0
            ? null
            : origins.FirstOrDefault(origin => string.Equals(origin.FieldName, field, StringComparison.OrdinalIgnoreCase));
        return column is null ? null : new DataColumnRead(field, column, Array.Empty<DataColumnOrigin>());
    }

    /// <summary>
    /// Columns an expression reads, directly or through the template's data
    /// transforms, resolved over the columns and the transform outputs with the
    /// same name rules as printing.
    /// </summary>
    public static IReadOnlyList<DataColumnRead> ColumnsRead(
        string? expression,
        IReadOnlyList<DataColumnOrigin> origins,
        IEnumerable<DataTransformDefinition>? transforms)
    {
        var read = new List<DataColumnRead>();
        Walk(expression, origins.Select(origin => origin.FieldName).ToArray(), transforms, (field, ties) =>
        {
            var column = origins.First(origin => string.Equals(origin.FieldName, ties[0], StringComparison.OrdinalIgnoreCase));
            if (read.Any(existing => existing.Column == column))
            {
                return;
            }

            var rivals = ties.Skip(1)
                .Select(name => origins.FirstOrDefault(origin => string.Equals(origin.FieldName, name, StringComparison.OrdinalIgnoreCase)))
                .OfType<DataColumnOrigin>()
                .ToArray();
            read.Add(new DataColumnRead(field, column, rivals));
        }, missing: null);
        return read;
    }

    /// <summary>
    /// Fields an expression reads that are neither one of <paramref name="columnNames"/>
    /// nor a data transform output, including the missing inputs of the transforms it reads.
    /// </summary>
    public static IReadOnlyList<string> MissingFields(
        string? expression,
        IReadOnlyList<string> columnNames,
        IEnumerable<DataTransformDefinition>? transforms)
    {
        var missing = new List<string>();
        Walk(expression, columnNames, transforms, (_, _) => { }, field =>
        {
            if (!missing.Contains(field, StringComparer.OrdinalIgnoreCase))
            {
                missing.Add(field);
            }
        });
        return missing;
    }

    /// <summary>Field names an expression reads: <c>{Field}</c> placeholders or the FIELD calls of a formula.</summary>
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

    private static void Walk(
        string? expression,
        IReadOnlyList<string> columnNames,
        IEnumerable<DataTransformDefinition>? transforms,
        Action<string, IReadOnlyList<string>> onColumn,
        Action<string>? missing)
    {
        var transformByName = new Dictionary<string, DataTransformDefinition>(StringComparer.OrdinalIgnoreCase);
        foreach (var transform in transforms ?? Array.Empty<DataTransformDefinition>())
        {
            if (!string.IsNullOrWhiteSpace(transform.Name))
            {
                transformByName.TryAdd(transform.Name.Trim(), transform);
            }
        }

        // Printing resolves a name over the row: the columns, then the transform outputs.
        var names = columnNames.Concat(transformByName.Keys).ToArray();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        void Visit(IEnumerable<string> fields)
        {
            foreach (var field in fields)
            {
                var ties = FieldNameResolver.FindTies(field, names);
                if (ties.Count == 0)
                {
                    missing?.Invoke(field);
                }
                else if (transformByName.TryGetValue(ties[0], out var transform))
                {
                    if (visited.Add(ties[0]))
                    {
                        Visit(FormulaFields(transform.Formula));
                    }
                }
                else
                {
                    onColumn(field, ties);
                }
            }
        }

        Visit(BindingFields(expression));
    }

    /// <summary>
    /// Every field a formula reads, found by evaluating it: the language has no
    /// branches (FIELD and CONCAT), so each FIELD runs and records its name,
    /// decoded and computed exactly as printing does.
    /// </summary>
    private static IReadOnlyList<string> FormulaFields(string? formula)
    {
        if (string.IsNullOrWhiteSpace(formula))
        {
            return Array.Empty<string>();
        }

        return FormulaBindingEvaluator.Evaluate(formula, EmptyRow).UsedFields
            .Select(field => field.Trim())
            .Where(field => field.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string Message(DataColumnRead read, IReadOnlyList<DataColumnOrigin> origins)
    {
        var origin = read.Column;
        if (origin.Kind == DataHeaderKind.Blank)
        {
            return $"Field '{origin.FieldName}' reads column {origin.ColumnLetter}, whose header cell is empty, so only its position identifies it. Give the column a header in the data file, then refresh the data.";
        }

        if (origin.Kind == DataHeaderKind.Repeated)
        {
            var columns = origins
                .Where(other => other.Kind == DataHeaderKind.Repeated
                                && string.Equals(other.SourceHeader, origin.SourceHeader, StringComparison.OrdinalIgnoreCase))
                .Select(other => other.ColumnLetter);
            return $"Field '{origin.FieldName}' reads column {origin.ColumnLetter}, whose header '{origin.SourceHeader}' is repeated in columns {string.Join(", ", columns)}, so only its position identifies it. Give each column a unique header in the data file, then refresh the data.";
        }

        var tied = new[] { origin }.Concat(read.Rivals)
            .OrderBy(other => other.SourceColumn)
            .Select(other => $"{other.ColumnLetter} ({other.SourceHeader})");
        return $"Field '{read.RequestedName}' matches columns {string.Join(", ", tied)} equally, so only their order decides which one prints. Rename one of them in the data file, or bind the exact header, then refresh the data.";
    }
}
