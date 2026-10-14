using System.Data;
using ANLAbel.Core.Expressions;
using ANLAbel.Core.Models;
using ANLAbel.Data.Excel;

namespace ANLAbel.App.Services;

/// <summary>
/// The import note and whether it is about columns the label uses (those block
/// printing) or only about headers nothing reads.
/// </summary>
public sealed record DataImportNote(string Text, bool AffectsLabel)
{
    public static DataImportNote None { get; } = new(string.Empty, false);
}

/// <summary>
/// DB03 status after an import or refresh, in Vietnamese: which columns the
/// label uses are not in the header row (renamed or deleted, with the column
/// they had last time), which used columns only their position identifies, and
/// otherwise where the header row is blank or repeats a name. Positions are the
/// column letters the operator sees in Excel.
/// </summary>
public static class DataHeaderText
{
    private const int MaxLettersListed = 6;

    public static DataImportNote DescribeImport(LabelTemplate template, DataTable table, IReadOnlyList<DatabaseField> previousFields, int headerRow)
    {
        ArgumentNullException.ThrowIfNull(template);
        var origins = DataColumnOrigin.Describe(table);
        if (origins.Count == 0)
        {
            return DataImportNote.None;
        }

        var columnNames = origins.Select(origin => origin.FieldName).ToArray();
        var bound = template.Objects
            .Where(item => item.IsVisible && !string.IsNullOrWhiteSpace(item.BindingExpression))
            .ToArray();
        var parts = new List<string>();

        var missing = bound
            .SelectMany(item => DataHeaderCheck.MissingFields(item.BindingExpression, columnNames, template.DataTransforms)
                .Select(field => (Item: item, Field: field)))
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
                .Where(read => read.DependsOnPosition)
                .Select(read => (Item: (object)item, Read: read)))
            .ToList();
        var copies = DataHeaderCheck.CopiesColumns(template, origins).Where(read => read.DependsOnPosition).ToArray();
        positional.AddRange(copies.Select(read => (Item: (object)template.DatabaseConfig, Read: read)));

        if (positional.Count > 0)
        {
            // One phrase per repeated header, one for the blank headers in use, and
            // one per set of lookalike headers that tie.
            var groups = positional
                .Select(use => use.Read)
                .GroupBy(read => read.Column.Kind switch
                {
                    DataHeaderKind.Blank => "blank",
                    DataHeaderKind.Repeated => "repeated:" + read.Column.SourceHeader.ToUpperInvariant(),
                    _ => "tie:" + string.Join("|", new[] { read.Column }.Concat(read.Rivals).Select(origin => origin.SourceColumn).Order())
                })
                .ToArray();
            var phrases = groups.Take(3).Select(group => Describe(group.ToArray(), origins));
            var more = groups.Length > 3 ? $"; và {groups.Length - 3} nhóm cột khác" : string.Empty;
            var columnCount = positional
                .SelectMany(use => new[] { use.Read.Column }.Concat(use.Read.Rivals))
                .Select(origin => origin.SourceColumn)
                .Distinct()
                .Count();
            var objectCount = positional.Select(use => use.Item).OfType<LabelObject>().Distinct().Count();
            var users = new List<string>();
            if (objectCount > 0)
            {
                users.Add($"{objectCount} đối tượng");
            }

            if (copies.Length > 0)
            {
                users.Add("cột số bản in");
            }

            // A blank or repeated header needs a header of its own; a lookalike name
            // that ties needs to match one column only.
            var headers = positional.Any(use => use.Read.Column.DependsOnPosition);
            var ties = positional.Any(use => !use.Read.Column.DependsOnPosition);
            var until = (headers, ties) switch
            {
                (true, true) => "mỗi cột có tiêu đề riêng và mỗi tên chỉ khớp một cột",
                (false, true) => "mỗi tên chỉ khớp một cột",
                _ => "mỗi cột có tiêu đề riêng"
            };
            parts.Add($"{string.Join("; ", phrases)}{more}; {string.Join(" và ", users)} dùng {(columnCount == 1 ? "cột này" : "các cột này")} chưa in được cho tới khi {until}");
        }

        var affectsLabel = parts.Count > 0;
        if (parts.Count == 0)
        {
            var blanks = origins.Where(origin => origin.Kind == DataHeaderKind.Blank).Select(origin => origin.ColumnLetter).ToArray();
            if (blanks.Length > 0)
            {
                parts.Add($"dòng tiêu đề {headerRow} để trống ở cột {Letters(blanks)}");
            }

            foreach (var repeat in origins.Where(origin => origin.Kind == DataHeaderKind.Repeated)
                         .GroupBy(origin => origin.SourceHeader, StringComparer.OrdinalIgnoreCase)
                         .Take(2))
            {
                parts.Add($"tiêu đề “{repeat.First().SourceHeader}” lặp ở cột {Letters(repeat.Select(origin => origin.ColumnLetter).ToArray())}");
            }
        }

        if (parts.Count == 0)
        {
            return DataImportNote.None;
        }

        var text = string.Join("; ", parts);
        return new DataImportNote(char.ToUpperInvariant(text[0]) + text[1..] + ".", affectsLabel);
    }

    /// <summary>One position-only column for the Properties panel, e.g. “Cột D có tiêu đề “Part No” trùng với cột A …”.</summary>
    public static string DescribeColumn(DataColumnRead read, IReadOnlyList<DataColumnOrigin> origins)
    {
        ArgumentNullException.ThrowIfNull(read);
        var origin = read.Column;
        if (origin.Kind == DataHeaderKind.Blank)
        {
            return $"Cột {origin.ColumnLetter} không có tiêu đề (ANLAbel tạm gọi là “{origin.FieldName}”).";
        }

        if (origin.Kind == DataHeaderKind.Repeated)
        {
            var others = origins
                .Where(other => other.Kind == DataHeaderKind.Repeated
                                && other.SourceColumn != origin.SourceColumn
                                && string.Equals(other.SourceHeader, origin.SourceHeader, StringComparison.OrdinalIgnoreCase))
                .Select(other => other.ColumnLetter);
            return $"Cột {origin.ColumnLetter} có tiêu đề “{origin.SourceHeader}” trùng với cột {string.Join(", ", others)} (ANLAbel tạm gọi là “{origin.FieldName}”).";
        }

        return $"Tên “{read.RequestedName}” khớp ngang nhau với {Tied(read)}; chỉ thứ tự cột quyết định cột nào được in.";
    }

    private static string Describe(IReadOnlyList<DataColumnRead> group, IReadOnlyList<DataColumnOrigin> origins)
    {
        var first = group[0];
        if (first.Column.Kind == DataHeaderKind.Blank)
        {
            var blanks = group.Select(read => read.Column).DistinctBy(origin => origin.SourceColumn).OrderBy(origin => origin.SourceColumn).ToArray();
            return $"{(blanks.Length == 1 ? "cột" : "các cột")} {Letters(blanks.Select(origin => origin.ColumnLetter).ToArray())} không có tiêu đề (ANLAbel tạm gọi là {Names(blanks)})";
        }

        if (first.Column.Kind == DataHeaderKind.Repeated)
        {
            var repeated = origins
                .Where(other => other.Kind == DataHeaderKind.Repeated
                                && string.Equals(other.SourceHeader, first.Column.SourceHeader, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            return $"tiêu đề “{first.Column.SourceHeader}” lặp ở cột {Letters(repeated.Select(origin => origin.ColumnLetter).ToArray())} (ANLAbel tạm gọi là {Names(repeated)})";
        }

        return $"tên “{first.RequestedName}” khớp ngang nhau với {Tied(first)}";
    }

    private static string Tied(DataColumnRead read)
        => string.Join(", ", new[] { read.Column }.Concat(read.Rivals)
            .OrderBy(origin => origin.SourceColumn)
            .Select(origin => $"cột {origin.ColumnLetter} (“{origin.SourceHeader}”)"));

    private static string PreviousColumn(string field, IReadOnlyList<DatabaseField> previousFields)
    {
        if (!FieldNameResolver.TryResolveFieldName(field, previousFields.Select(previous => previous.Name), out var name))
        {
            return string.Empty;
        }

        var index = previousFields
            .Select((previous, position) => (previous, position))
            .First(entry => string.Equals(entry.previous.Name, name, StringComparison.OrdinalIgnoreCase));
        // Fields saved by an older ANLAbel have no column; their place in the list
        // was their column then, since every column was a field.
        var column = index.previous.SourceColumn > 0 ? index.previous.SourceColumn : index.position + 1;
        return $" (lần nhập trước ở cột {DataColumnOrigin.ToColumnLetter(column)})";
    }

    private static string Names(IEnumerable<DataColumnOrigin> origins)
        => string.Join(", ", origins.Select(origin => $"“{origin.FieldName}”"));

    /// <summary>Column letters, at most six; a stray value far to the right can leave many blank headers.</summary>
    private static string Letters(IReadOnlyList<string> letters)
        => letters.Count <= MaxLettersListed
            ? string.Join(", ", letters)
            : $"{string.Join(", ", letters.Take(MaxLettersListed))} và {letters.Count - MaxLettersListed} cột khác";
}
