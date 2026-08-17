using System.Collections.Immutable;
using System.Text;
using ANLAbel.Core.Data;

namespace ANLAbel.Data.Automation;

public sealed record AutomationSourceParseResult(IReadOnlyList<DataRecord> Records, IReadOnlyList<string> Diagnostics);

/// <summary>
/// Explicit UTF-8 header CSV parser for a future automation binding. Parsing is
/// isolated from claim, template selection, manifest, queue and dispatch.
/// </summary>
public static class CsvAutomationSourceParser
{
    public static AutomationSourceParseResult Parse(Stream stream)
    {
        using var reader = new StreamReader(stream, new UTF8Encoding(false), detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var headerLine = reader.ReadLine();
        if (headerLine is null) return new([], ["CSV source is empty."]);
        var headers = ParseLine(headerLine);
        if (headers.Count == 0 || headers.Any(string.IsNullOrWhiteSpace)) return new([], ["CSV header contains an empty field name."]);
        if (headers.Distinct(StringComparer.OrdinalIgnoreCase).Count() != headers.Count) return new([], ["CSV header contains duplicate field names."]);
        var records = new List<DataRecord>();
        var diagnostics = new List<string>();
        var lineNumber = 1;
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            lineNumber++;
            if (line.Length == 0) continue;
            try
            {
                var values = ParseLine(line);
                if (values.Count != headers.Count) { diagnostics.Add($"CSV row {lineNumber} has {values.Count} field(s); expected {headers.Count}."); continue; }
                var row = headers.Select((header, index) => new KeyValuePair<string, string?>(header, values[index])).ToImmutableDictionary(StringComparer.OrdinalIgnoreCase);
                records.Add(new DataRecord(row));
            }
            catch (FormatException ex) { diagnostics.Add($"CSV row {lineNumber}: {ex.Message}"); }
        }
        return new(records, diagnostics);
    }

    private static List<string> ParseLine(string line)
    {
        var values = new List<string>(); var value = new StringBuilder(); var quoted = false;
        for (var index = 0; index < line.Length; index++)
        {
            var current = line[index];
            if (quoted)
            {
                if (current == '"' && index + 1 < line.Length && line[index + 1] == '"') { value.Append('"'); index++; }
                else if (current == '"') quoted = false;
                else value.Append(current);
            }
            else if (current == ',') { values.Add(value.ToString()); value.Clear(); }
            else if (current == '"' && value.Length == 0) quoted = true;
            else value.Append(current);
        }
        if (quoted) throw new FormatException("unterminated quoted value.");
        values.Add(value.ToString());
        return values;
    }
}
