using System.Data;
using ANLAbel.Core.Expressions;
using ANLAbel.Core.Models;
using ANLAbel.Data.Excel;

namespace ANLAbel.App.Services;

/// <summary>
/// DB03 status after an import or refresh, in Vietnamese: which columns the
/// label uses are not in the header row (renamed or deleted, with the column
/// they had last time), which used columns only their position identifies, and
/// otherwise where the header row is blank or repeats a name. Positions are the
/// column letters the operator sees in Excel.
/// </summary>
public static class DataHeaderText
{
    public static string DescribeImport(LabelTemplate template, DataTable table, IReadOnlyList<string> previousFields, int headerRow)
    {
        ArgumentNullException.ThrowIfNull(template);
        var origins = DataColumnOrigin.Describe(table);
        if (origins.Count == 0)
        {
            return string.Empty;
        }

        var transformNames = template.DataTransforms
            .Select(transform => transform.Name?.Trim() ?? string.Empty)
            .Where(name => name.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var bound = template.Objects
            .Where(item => item.IsVisible && !string.IsNullOrWhiteSpace(item.BindingExpression))
            .ToArray();
        var parts = new List<string>();

        var missing = bound
            .SelectMany(item => DataHeaderCheck.BindingFields(item.BindingExpression).Select(field => (Item: item, Field: field.Trim())))
            .Where(use => use.Field.Length > 0 && !transformNames.Contains(use.Field) && DataHeaderCheck.Resolve(use.Field, origins) is null)
            .GroupBy(use => use.Field, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (missing.Length > 0)
        {
            var names = string.Join(", ", missing.Take(3).Select(group => $"“{group.Key}”{PreviousColumn(group.Key, previousFields)}"));
            var more = missing.Length > 3 ? $" và {missing.Length - 3} cột khác" : string.Empty;
            var objectCount = missing.SelectMany(group => group.Select(use => use.Item)).Distinct().Count();
            parts.Add(missing.Length == 1
                ? $"dòng tiêu đề {headerRow} không có cột {names}; {objectCount} đối tượng dùng cột này chưa in được"
                : $"dòng tiêu đề {headerRow} không có các cột {names}{more}; {objectCount} đối tượng dùng các cột này chưa in được");
        }

        var positional = bound
            .SelectMany(item => DataHeaderCheck.ColumnsRead(item.BindingExpression, origins, template.DataTransforms)
                .Where(origin => origin.DependsOnPosition)
                .Select(origin => (Item: item, Origin: origin)))
            .ToArray();
        if (positional.Length > 0)
        {
            // One phrase per repeated header and one for all blank headers in use.
            var groups = positional
                .Select(use => use.Origin)
                .Distinct()
                .GroupBy(origin => origin.Kind == DataHeaderKind.Blank ? null : origin.SourceHeader.ToUpperInvariant())
                .ToArray();
            var phrases = groups.Take(3).Select(group => group.Key is null
                ? $"{(group.Count() == 1 ? "cột" : "các cột")} {Join(group.Select(origin => origin.ColumnLetter))} không có tiêu đề (ANLAbel tạm gọi là {Names(group)})"
                : Describe(group.First(), origins));
            var more = groups.Length > 3 ? $"; và {groups.Length - 3} nhóm cột khác" : string.Empty;
            var columnCount = positional.Select(use => use.Origin).Distinct().Count();
            parts.Add($"{string.Join("; ", phrases)}{more}; {positional.Select(use => use.Item).Distinct().Count()} đối tượng dùng {(columnCount == 1 ? "cột này" : "các cột này")} chưa in được cho tới khi mỗi cột có tiêu đề riêng");
        }

        if (parts.Count == 0)
        {
            var blanks = origins.Where(origin => origin.Kind == DataHeaderKind.Blank).Select(origin => origin.ColumnLetter).ToArray();
            if (blanks.Length > 0)
            {
                parts.Add($"dòng tiêu đề {headerRow} để trống ở cột {Join(blanks)}");
            }

            foreach (var repeat in origins.Where(origin => origin.Kind == DataHeaderKind.Repeated)
                         .GroupBy(origin => origin.SourceHeader, StringComparer.OrdinalIgnoreCase)
                         .Take(2))
            {
                parts.Add($"tiêu đề “{repeat.First().SourceHeader}” lặp ở cột {Join(repeat.Select(origin => origin.ColumnLetter))}");
            }
        }

        if (parts.Count == 0)
        {
            return string.Empty;
        }

        var text = string.Join("; ", parts);
        return char.ToUpperInvariant(text[0]) + text[1..] + ".";
    }

    /// <summary>One position-only column for the Properties panel, e.g. “Cột D có tiêu đề “Part No” trùng với cột A …”.</summary>
    public static string DescribeColumn(DataColumnOrigin origin, IReadOnlyList<DataColumnOrigin> origins)
    {
        ArgumentNullException.ThrowIfNull(origin);
        if (origin.Kind == DataHeaderKind.Blank)
        {
            return $"Cột {origin.ColumnLetter} không có tiêu đề (ANLAbel tạm gọi là “{origin.FieldName}”).";
        }

        var others = origins
            .Where(other => other.Kind == DataHeaderKind.Repeated
                            && other.SourceColumn != origin.SourceColumn
                            && string.Equals(other.SourceHeader, origin.SourceHeader, StringComparison.OrdinalIgnoreCase))
            .Select(other => other.ColumnLetter);
        return $"Cột {origin.ColumnLetter} có tiêu đề “{origin.SourceHeader}” trùng với cột {Join(others)} (ANLAbel tạm gọi là “{origin.FieldName}”).";
    }

    private static string Describe(DataColumnOrigin repeated, IReadOnlyList<DataColumnOrigin> origins)
    {
        var group = origins
            .Where(other => other.Kind == DataHeaderKind.Repeated
                            && string.Equals(other.SourceHeader, repeated.SourceHeader, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        return $"tiêu đề “{repeated.SourceHeader}” lặp ở cột {Join(group.Select(origin => origin.ColumnLetter))} (ANLAbel tạm gọi là {Names(group)})";
    }

    private static string Names(IEnumerable<DataColumnOrigin> origins)
        => string.Join(", ", origins.Select(origin => $"“{origin.FieldName}”"));

    private static string PreviousColumn(string field, IReadOnlyList<string> previousFields)
    {
        if (!FieldNameResolver.TryResolveFieldName(field, previousFields, out var previous))
        {
            return string.Empty;
        }

        var index = previousFields
            .Select((name, position) => (name, position))
            .First(entry => string.Equals(entry.name, previous, StringComparison.OrdinalIgnoreCase))
            .position;
        return $" (lần nhập trước ở cột {DataColumnOrigin.ToColumnLetter(index + 1)})";
    }

    private static string Join(IEnumerable<string> letters) => string.Join(", ", letters);
}
