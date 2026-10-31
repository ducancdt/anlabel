using ANLAbel.Core.Expressions;
using ANLAbel.Core.Models;

namespace ANLAbel.App.Services;

/// <summary>
/// Rows that hold no record for the label, which are not printed unless the
/// operator picks them: rows where every data field the label reads (Excel
/// columns and functions, not the answers, date and serial given at print
/// time) is blank, and a run of three or more rows at the end of the sheet
/// that read the same and leave at least two thirds of those fields blank, a
/// formula or a constant filled down past the records (customer sheets: 1
/// record and 198 rows holding only a weight, 1 record and 8 rows holding
/// only the maker). Quick print leaves out the blank rows only: it shows no
/// list to check a guess against.
/// </summary>
public static class LabelDataRows
{
    private const int MinimumFilledDownRun = 3;

    /// <summary>The data fields the label prints.</summary>
    public static IReadOnlyList<string> Fields(LabelTemplate template) => template.Objects
        .Where(item => item.IsVisible && item.IsPrintable)
        .SelectMany(item => DataHeaderCheck.BindingFields(item.BindingExpression))
        .Where(field => !field.StartsWith('?') && !field.StartsWith('#'))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    /// <summary>
    /// The rows (0-based) without a record for the label. None when the label
    /// reads no data field, or when no field it reads is in the rows at all: a
    /// sheet without the label's columns is reported as missing columns instead.
    /// </summary>
    public static IReadOnlyList<int> WithoutData(LabelTemplate template, IReadOnlyList<IReadOnlyDictionary<string, string>?> rows, bool filledDown = true)
    {
        // Each field's column, resolved once in the first row that has it.
        var keys = Fields(template)
            .Select(field => rows.OfType<IReadOnlyDictionary<string, string>>()
                .Select(row => FieldNameResolver.TryGetValue(row, field, out _, out var resolved) ? resolved : null)
                .FirstOrDefault(resolved => resolved is not null))
            .OfType<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (keys.Length == 0)
        {
            return Array.Empty<int>();
        }

        var values = rows
            .Select(row => row is null
                ? null
                : keys.Select(key => row.TryGetValue(key, out var value) ? value?.Trim() ?? string.Empty : string.Empty).ToArray())
            .ToArray();
        var without = new SortedSet<int>(values
            .Select((row, index) => (Row: row, Index: index))
            .Where(entry => entry.Row is not null && entry.Row.All(string.IsNullOrEmpty))
            .Select(entry => entry.Index));

        // A run at the end that reads the same, mostly blank, below at least one other row.
        var last = values.Length - 1;
        if (filledDown && last > 0 && values[last] is { } tail && tail.Count(string.IsNullOrEmpty) * 3 >= tail.Length * 2)
        {
            var start = last;
            while (start > 0 && values[start - 1] is { } previous && previous.SequenceEqual(tail))
            {
                start--;
            }

            if (start > 0 && last - start + 1 >= MinimumFilledDownRun)
            {
                for (var index = start; index <= last; index++)
                {
                    without.Add(index);
                }
            }
        }

        return without.ToArray();
    }
}
