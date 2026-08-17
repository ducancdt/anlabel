using System.Text;
using ClosedXML.Excel;

namespace ANLAbel.Data.PrintLogs;

/// <summary>
/// Writes the human-facing print history as append-only CSV (changed 2026-07-04 from a
/// live .xlsx rewritten via ClosedXML on every print). The old design opened the ENTIRE
/// existing log into a full ClosedXML DOM, appended one row, and re-saved the whole file —
/// O(n) work per print that got slower and heavier every month of production use, using the
/// same memory-hungry parser that caused the Import Excel hang fixed earlier this session.
/// CSV appends are O(1): one <see cref="File.AppendAllText(string, string)"/> call per print,
/// no matter how large the log has grown. Use <see cref="ExportToExcelAsync"/> for an
/// occasional, on-demand formatted report — ClosedXML is fine there since it only runs once
/// per user click, not once per label.
/// </summary>
public sealed class PrintLogService
{
    private static readonly string[] Headers =
    {
        "PrintedAt",
        "PartNo",
        "ItemName",
        "Lot",
        "Quantity",
        "LabelContent",
        "RowData",
        "TemplateName",
        "PrinterName",
        "PrintMode",
        "LabelIndex",
        "ExcelFilePath",
        "ExcelSheetName",
        "Notes"
    };

    private readonly object _writeLock = new();

    public PrintLogService()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ANLAbel",
            "print-history.csv"))
    {
    }

    public PrintLogService(string logFilePath)
    {
        LogFilePath = logFilePath;
    }

    public string LogFilePath { get; }

    public Task AppendAsync(PrintLogEntry entry, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => AppendMany(new[] { entry }), cancellationToken);
    }

    public Task AppendManyAsync(IEnumerable<PrintLogEntry> entries, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => AppendMany(entries), cancellationToken);
    }

    /// <summary>
    /// Returns redacted label-history summaries for a read-only activity projection.
    /// Raw LabelContent and RowData deliberately never leave this service through this API.
    /// </summary>
    public Task<(IReadOnlyList<PrintLogSummary> Entries, IReadOnlyList<string> Diagnostics)> ReadSummariesAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            try
            {
                var rows = ReadAllRows(cancellationToken);
                var summaries = rows.Select((row, index) => new PrintLogSummary(
                    index + 1,
                    ParsePrintedAt(row.ElementAtOrDefault(0)),
                    row.ElementAtOrDefault(7) ?? string.Empty,
                    row.ElementAtOrDefault(8) ?? string.Empty,
                    row.ElementAtOrDefault(9) ?? string.Empty,
                    row.ElementAtOrDefault(4) ?? string.Empty)).ToArray();
                return ((IReadOnlyList<PrintLogSummary>)summaries, (IReadOnlyList<string>)Array.Empty<string>());
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or FormatException)
            {
                return ((IReadOnlyList<PrintLogSummary>)Array.Empty<PrintLogSummary>(), (IReadOnlyList<string>)[$"CSV label history could not be read: {ex.Message}"]);
            }
        }, cancellationToken);
    }

    private static DateTime? ParsePrintedAt(string? value) => DateTime.TryParse(value, out var parsed) ? parsed : null;

    /// <summary>
    /// Reads the whole CSV log and writes a nicely formatted .xlsx report (bold header, light
    /// blue fill, auto-fit columns — the same look the old live-Excel log had). This is a rare,
    /// user-initiated action (a button click), not something that runs on every print, so using
    /// ClosedXML's full-DOM writer here is fine — the memory cost of a one-off export is nothing
    /// like the cost of doing that same full-DOM rewrite after every single label.
    /// </summary>
    public Task ExportToExcelAsync(string destinationXlsxPath, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => ExportToExcel(destinationXlsxPath, cancellationToken), cancellationToken);
    }

    private void ExportToExcel(string destinationXlsxPath, CancellationToken cancellationToken)
    {
        var rows = ReadAllRows(cancellationToken);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.AddWorksheet("PrintHistory");
        for (var column = 0; column < Headers.Length; column++)
        {
            var cell = worksheet.Cell(1, column + 1);
            cell.Value = Headers[column];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#DCEBFF");
        }

        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var row = rows[rowIndex];
            for (var column = 0; column < Headers.Length && column < row.Count; column++)
            {
                worksheet.Cell(rowIndex + 2, column + 1).Value = row[column];
            }
        }

        worksheet.Columns().AdjustToContents();

        var directory = Path.GetDirectoryName(destinationXlsxPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        workbook.SaveAs(destinationXlsxPath);
    }

    /// <summary>
    /// Parses the CSV log back into rows (skipping the header). Fields never contain raw
    /// newlines — <see cref="AppendMany"/> strips them before writing — so a simple per-line
    /// reader is safe here without needing a full multi-line-aware CSV grammar.
    /// </summary>
    private List<List<string>> ReadAllRows(CancellationToken cancellationToken)
    {
        var rows = new List<List<string>>();
        if (!File.Exists(LogFilePath))
        {
            return rows;
        }

        using var reader = new StreamReader(LogFilePath, Encoding.UTF8);
        var isHeaderRow = true;
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (isHeaderRow)
            {
                isHeaderRow = false;
                continue;
            }

            if (line.Length == 0)
            {
                continue;
            }

            rows.Add(ParseCsvLine(line));
        }

        return rows;
    }

    private void AppendMany(IEnumerable<PrintLogEntry> entries)
    {
        var directory = Path.GetDirectoryName(LogFilePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var builder = new StringBuilder();

        // Locking + checking File.Exists must happen together so two concurrent print jobs
        // can't both decide the file is new and each write their own header row.
        lock (_writeLock)
        {
            if (!File.Exists(LogFilePath))
            {
                builder.AppendLine(string.Join(",", Headers.Select(EscapeCsvField)));
            }

            foreach (var entry in entries)
            {
                builder.AppendLine(string.Join(",", new[]
                {
                    entry.PrintedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                    entry.PartNo,
                    entry.ItemName,
                    entry.Lot,
                    entry.Quantity,
                    entry.LabelContent,
                    entry.RowData,
                    entry.TemplateName,
                    entry.PrinterName,
                    entry.PrintMode,
                    entry.LabelIndex.ToString(),
                    entry.ExcelFilePath,
                    entry.ExcelSheetName,
                    entry.Notes
                }.Select(EscapeCsvField)));
            }

            File.AppendAllText(LogFilePath, builder.ToString(), Encoding.UTF8);
        }
    }

    /// <summary>
    /// RFC4180-style escaping. Field values are stripped of \r/\n first (print log fields are
    /// short summaries, not multi-line text — see <see cref="AppendMany"/>/callers), so quoting
    /// is only needed for commas and embedded quotes.
    /// </summary>
    private static string EscapeCsvField(string? value)
    {
        var cleaned = (value ?? string.Empty).Replace("\r", " ").Replace("\n", " ");
        return cleaned.Contains(',') || cleaned.Contains('"')
            ? "\"" + cleaned.Replace("\"", "\"\"") + "\""
            : cleaned;
    }

    private static List<string> ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (inQuotes)
            {
                if (c == '"' && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else if (c == '"')
                {
                    inQuotes = false;
                }
                else
                {
                    current.Append(c);
                }
            }
            else if (c == '"' && current.Length == 0)
            {
                inQuotes = true;
            }
            else if (c == ',')
            {
                fields.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        fields.Add(current.ToString());
        return fields;
    }
}

public sealed record PrintLogSummary(int RecordNumber, DateTime? PrintedAtLocal, string TemplateName, string PrinterName, string PrintMode, string Quantity);
