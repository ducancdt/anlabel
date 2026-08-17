using System.Data;
using System.Globalization;
using System.Text;
using ExcelDataReader;

namespace ANLAbel.Data.Excel;

/// <summary>
/// One raw physical row from <see cref="ExcelDataService.PreviewRowsAsync"/>. <see cref="RowNumber"/>
/// is the sheet's absolute 1-based row number — the same value <c>DatabaseConfig.HeaderRowIndex</c>
/// and <c>LoadSheetAsync</c>'s headerRowIndex parameter use, so it can be assigned directly once
/// the user points at the real header row in a preview grid.
/// </summary>
public sealed record ExcelPreviewRow(int RowNumber, IReadOnlyList<string> Cells);

/// <summary>One sheet's name plus its preview rows, from <see cref="ExcelDataService.GetSheetsWithPreviewAsync"/>.</summary>
public sealed record ExcelSheetPreview(string SheetName, IReadOnlyList<ExcelPreviewRow> Rows);

/// <summary>
/// Reads .xlsx/.xlsm workbooks with ExcelDataReader (bug fix 2026-07-03: switched from
/// ClosedXML). ClosedXML builds a full in-memory DOM of the whole workbook — every sheet,
/// every cell's style/formatting — regardless of how little of it a caller actually reads,
/// which made even a modest Import spike memory hard enough to trigger long, all-thread
/// Garbage Collector pauses on RAM-constrained machines (reported as the app "hanging" while
/// adding an Excel file, confirmed by moderate-not-pegged CPU alongside high RAM use at the
/// time). ExcelDataReader is a forward-only streaming reader with no such DOM — it reads raw
/// cell values sheet-by-sheet, row-by-row, and never materializes sheets or rows the caller
/// doesn't ask for.
/// </summary>
public sealed class ExcelDataService
{
    public const string CsvSheetName = "CSV";
    private static readonly TimeSpan DefaultNetworkTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Safety-net timeout applied to LOCAL file reads too (bug fix 2026-07-03, second round):
    /// a machine reported the app "hanging" during Import Excel even after the ExcelDataReader
    /// switch, with a SMALL local file, for several minutes until force-killed via Task Manager
    /// — a genuine stuck condition, not just slowness, that could not be reproduced elsewhere.
    /// Previously only UNC/network paths got a timeout; a local read had none at all, so a stuck
    /// call (inside ExcelDataReader itself, or anywhere in this class) could hang forever with no
    /// recovery except killing the process. This does not fix whatever the underlying stuck
    /// condition is — the background Task.Run may keep running after this fires — but it
    /// guarantees the UI always gets back control with a clear, actionable error instead of an
    /// unrecoverable freeze.
    /// </summary>
    private static readonly TimeSpan DefaultLocalTimeout = TimeSpan.FromSeconds(45);

    static ExcelDataService()
    {
        // ExcelReaderFactory needs the Windows-1252 code page even for .xlsx files (its
        // configuration constructor references it for legacy .xls fallback), which .NET
        // Core/5+ no longer registers by default — without this, every CreateReader call
        // throws NotSupportedException("No data is available for encoding 1252").
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
    }

    /// <summary>
    /// Returns sheet names from the workbook. Runs on a background thread so it
    /// is safe to call from the UI thread. Supports cancellation and opens files
    /// with <see cref="FileShare.ReadWrite"/> so that workbooks already open in
    /// Excel can still be read.
    /// </summary>
    public async Task<IReadOnlyList<string>> GetSheetNamesAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (IsCsvFile(filePath))
        {
            return await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var stream = OpenFileStream(filePath, cancellationToken);
                return (IReadOnlyList<string>)new[] { CsvSheetName };
            }, CancellationToken.None).WaitAsync(IsNetworkPath(filePath) ? DefaultNetworkTimeout : DefaultLocalTimeout, cancellationToken);
        }

        var readTask = Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var stream = OpenFileStream(filePath, cancellationToken);
            using var reader = OpenReader(stream, filePath);
            var names = new List<string>();
            do
            {
                cancellationToken.ThrowIfCancellationRequested();
                names.Add(reader.Name);
            } while (reader.NextResult());

            return (IReadOnlyList<string>)names;
        }, CancellationToken.None);

        return await readTask.WaitAsync(IsNetworkPath(filePath) ? DefaultNetworkTimeout : DefaultLocalTimeout, cancellationToken);
    }

    /// <summary>
    /// Lightweight connectivity check for the Database Manager (database-manager-module-plan.md
    /// M2): verifies the file opens, the sheet exists, and the header row yields at least one
    /// column, without the caller having to keep the resulting <see cref="DataTable"/> around.
    /// Never throws — failures are reported in the returned message so the UI can show them
    /// inline next to a "Test Connection" button.
    /// </summary>
    public async Task<(bool Ok, string Message)> TestConnectionAsync(string filePath, string sheetName, int headerRowIndex = 1, CancellationToken cancellationToken = default)
    {
        try
        {
            var table = await LoadSheetAsync(filePath, sheetName, headerRowIndex, cancellationToken);
            return table.Columns.Count > 0
                ? (true, $"OK — {table.Columns.Count} column(s), {table.Rows.Count} row(s).")
                : (false, "Sheet has no columns at the given header row.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// Reads up to <paramref name="maxRows"/> raw physical rows from the top of the sheet,
    /// with no assumption about which row is the header (database-plan.md Giai đoạn 3 item 8:
    /// "chọn dòng header + preview trước khi import"). Used to render a preview grid so the
    /// user can point at the actual header row before importing, instead of the app always
    /// assuming row 1. Streaming stops reading this sheet as soon as maxRows is reached — it
    /// never touches the rest of the sheet, let alone other sheets in the workbook.
    /// </summary>
    public async Task<IReadOnlyList<ExcelPreviewRow>> PreviewRowsAsync(string filePath, string sheetName, int maxRows = 15, CancellationToken cancellationToken = default)
    {
        if (IsCsvFile(filePath))
        {
            EnsureCsvSheet(sheetName, filePath);
            return await Task.Run(
                () => (IReadOnlyList<ExcelPreviewRow>)ReadCsvRows(filePath, maxRows, cancellationToken)
                    .Select((cells, index) => new ExcelPreviewRow(index + 1, cells))
                    .ToArray(),
                CancellationToken.None).WaitAsync(IsNetworkPath(filePath) ? DefaultNetworkTimeout : DefaultLocalTimeout, cancellationToken);
        }

        var readTask = Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var stream = OpenFileStream(filePath, cancellationToken);
            using var reader = OpenReader(stream, filePath);
            if (!SeekToSheet(reader, sheetName, out var availableSheets))
            {
                throw new ExcelDataReadException(
                    ExcelDataReadError.MissingSheet,
                    $"Excel sheet '{sheetName}' was not found in {Path.GetFileName(filePath)}. Available sheets: {availableSheets}.",
                    filePath,
                    sheetName);
            }

            return (IReadOnlyList<ExcelPreviewRow>)ReadPreviewRows(reader, maxRows, cancellationToken);
        }, CancellationToken.None);

        return await readTask.WaitAsync(IsNetworkPath(filePath) ? DefaultNetworkTimeout : DefaultLocalTimeout, cancellationToken);
    }

    /// <summary>
    /// Combines <see cref="GetSheetNamesAsync"/> and <see cref="PreviewRowsAsync"/> into a
    /// single file open (bug fix 2026-07-03): opening a workbook used to parse the whole file
    /// into memory regardless of how much of it you read afterward, so calling both separately
    /// — as <c>ExcelImportWindow.Browse_Click</c> originally did after the header-row-picker
    /// feature was added — silently made every Import re-parse the same file a second time
    /// before the real import parsed it a third time. This method reads every sheet's name and
    /// preview rows in one streaming pass so the import dialog only needs to open the file
    /// twice total (once here, once for the real <see cref="LoadSheetAsync"/>).
    /// </summary>
    public async Task<IReadOnlyList<ExcelSheetPreview>> GetSheetsWithPreviewAsync(string filePath, int maxRows = 15, CancellationToken cancellationToken = default)
    {
        if (IsCsvFile(filePath))
        {
            var rows = await PreviewRowsAsync(filePath, CsvSheetName, maxRows, cancellationToken);
            return new[] { new ExcelSheetPreview(CsvSheetName, rows) };
        }

        var readTask = Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var stream = OpenFileStream(filePath, cancellationToken);
            using var reader = OpenReader(stream, filePath);
            var result = new List<ExcelSheetPreview>();
            do
            {
                cancellationToken.ThrowIfCancellationRequested();
                var sheetName = reader.Name;
                result.Add(new ExcelSheetPreview(sheetName, ReadPreviewRows(reader, maxRows, cancellationToken)));
            } while (reader.NextResult());

            return (IReadOnlyList<ExcelSheetPreview>)result;
        }, CancellationToken.None);

        return await readTask.WaitAsync(IsNetworkPath(filePath) ? DefaultNetworkTimeout : DefaultLocalTimeout, cancellationToken);
    }

    /// <summary>
    /// Backwards-compatible synchronous overload.
    /// </summary>
    public IReadOnlyList<string> GetSheetNames(string filePath)
    {
        if (IsCsvFile(filePath))
        {
            using var csvStream = OpenFileStream(filePath);
            return new[] { CsvSheetName };
        }

        using var stream = OpenFileStream(filePath);
        using var reader = OpenReader(stream, filePath);
        var names = new List<string>();
        do
        {
            names.Add(reader.Name);
        } while (reader.NextResult());

        return names;
    }

    public async Task<DataTable> LoadSheetAsync(string filePath, string sheetName, int headerRowIndex = 1, CancellationToken cancellationToken = default)
    {
        if (headerRowIndex < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(headerRowIndex), "Excel header row must be 1 or greater.");
        }

        var readTask = Task.Run(
            () => IsCsvFile(filePath)
                ? LoadCsv(filePath, sheetName, headerRowIndex, cancellationToken)
                : LoadSheet(filePath, sheetName, headerRowIndex, cancellationToken),
            CancellationToken.None);

        return await readTask.WaitAsync(IsNetworkPath(filePath) ? DefaultNetworkTimeout : DefaultLocalTimeout, cancellationToken);
    }

    private static DataTable LoadSheet(string filePath, string sheetName, int headerRowIndex, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var stream = OpenFileStream(filePath, cancellationToken);
        using var reader = OpenReader(stream, filePath);
        if (!SeekToSheet(reader, sheetName, out var availableSheets))
        {
            throw new ExcelDataReadException(
                ExcelDataReadError.MissingSheet,
                $"Excel sheet '{sheetName}' was not found in {Path.GetFileName(filePath)}. Available sheets: {availableSheets}.",
                filePath,
                sheetName);
        }

        var table = new DataTable(sheetName);
        string[]? headerCells = null;
        var rowNumber = 0;
        while (reader.Read())
        {
            cancellationToken.ThrowIfCancellationRequested();
            rowNumber++;
            var cells = ReadRowCellsArray(reader);
            if (rowNumber == headerRowIndex)
            {
                headerCells = cells;
                var headers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (var column = 0; column < headerCells.Length; column++)
                {
                    var rawHeader = headerCells[column];
                    var header = string.IsNullOrWhiteSpace(rawHeader) ? $"Column{column + 1}" : rawHeader;
                    table.Columns.Add(MakeUniqueHeader(header, headers), typeof(string));
                }
            }
            else if (rowNumber > headerRowIndex)
            {
                // Materialize each record as it is read. Keeping every string[]
                // until the end doubles peak memory for large workbooks, despite
                // using a forward-only reader.
                var dataRow = table.NewRow();
                var hasValue = false;
                for (var column = 0; column < table.Columns.Count; column++)
                {
                    var value = column < cells.Length ? cells[column] : string.Empty;
                    if (!string.IsNullOrEmpty(value))
                    {
                        hasValue = true;
                    }

                    dataRow[column] = value;
                }

                if (hasValue)
                {
                    table.Rows.Add(dataRow);
                }
            }
        }

        if (rowNumber == 0)
        {
            // Sheet has no rows at all — matches the previous ClosedXML "usedRange is null"
            // behavior of returning an empty table rather than an InvalidHeaderRow error.
            return table;
        }

        if (headerCells is null)
        {
            throw new ExcelDataReadException(
                ExcelDataReadError.InvalidHeaderRow,
                $"Header row {headerRowIndex} is outside the used range of sheet '{sheetName}' (last row: {rowNumber}).",
                filePath,
                sheetName);
        }

        return table;
    }

    private static DataTable LoadCsv(string filePath, string sheetName, int headerRowIndex, CancellationToken cancellationToken)
    {
        EnsureCsvSheet(sheetName, filePath);
        var table = new DataTable(CsvSheetName);

        string? headerRecord;
        using (var headerStream = OpenFileStream(filePath, cancellationToken))
        using (var headerReader = new StreamReader(headerStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true), detectEncodingFromByteOrderMarks: true))
        {
            headerRecord = ReadCsvHeaderRecord(headerReader, filePath, cancellationToken);
        }

        if (headerRecord is null)
        {
            return table;
        }

        var delimiter = DetectCsvDelimiter(headerRecord);
        var rowNumber = 0;
        var foundHeader = false;
        using var stream = OpenFileStream(filePath, cancellationToken);
        using var reader = new StreamReader(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true), detectEncodingFromByteOrderMarks: true);
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var record = ReadCsvRecord(reader, delimiter, filePath, cancellationToken, out var reachedEnd);
            if (record is not null)
            {
                rowNumber++;
                var cells = record.Select(CleanCellText).ToArray();
                if (rowNumber == headerRowIndex)
                {
                    foundHeader = true;
                    var headers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    for (var column = 0; column < cells.Length; column++)
                    {
                        var header = string.IsNullOrWhiteSpace(cells[column]) ? $"Column{column + 1}" : cells[column];
                        table.Columns.Add(MakeUniqueHeader(header, headers), typeof(string));
                    }
                }
                else if (rowNumber > headerRowIndex)
                {
                    var dataRow = table.NewRow();
                    var hasValue = false;
                    for (var column = 0; column < table.Columns.Count; column++)
                    {
                        var value = column < cells.Length ? cells[column] : string.Empty;
                        hasValue |= !string.IsNullOrEmpty(value);
                        dataRow[column] = value;
                    }

                    if (hasValue)
                    {
                        table.Rows.Add(dataRow);
                    }
                }
            }

            if (reachedEnd)
            {
                break;
            }
        }

        if (!foundHeader)
        {
            throw new ExcelDataReadException(
                ExcelDataReadError.InvalidHeaderRow,
                $"Header row {headerRowIndex} is outside the CSV data range (last row: {rowNumber}).",
                filePath,
                sheetName);
        }

        return table;
    }

    private static List<string[]> ReadCsvRows(string filePath, int? maxRows, CancellationToken cancellationToken)
    {
        var records = new List<string[]>();
        string? headerRecord;
        using (var headerStream = OpenFileStream(filePath, cancellationToken))
        using (var headerReader = new StreamReader(headerStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true), detectEncodingFromByteOrderMarks: true))
        {
            headerRecord = ReadCsvHeaderRecord(headerReader, filePath, cancellationToken);
        }

        if (headerRecord is null)
        {
            return records;
        }

        var delimiter = DetectCsvDelimiter(headerRecord);
        using var stream = OpenFileStream(filePath, cancellationToken);
        using var reader = new StreamReader(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true), detectEncodingFromByteOrderMarks: true);
        while (maxRows is null || records.Count < maxRows.Value)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var record = ReadCsvRecord(reader, delimiter, filePath, cancellationToken, out var reachedEnd);
            if (record is not null)
            {
                records.Add(record.Select(CleanCellText).ToArray());
            }

            if (reachedEnd)
            {
                break;
            }
        }

        return records;
    }

    /// <summary>
    /// Reads the first logical CSV record for delimiter discovery. A physical
    /// <see cref="TextReader.ReadLine"/> is insufficient because CSV permits a
    /// newline inside a quoted header cell.
    /// </summary>
    private static string? ReadCsvHeaderRecord(TextReader reader, string filePath, CancellationToken cancellationToken)
    {
        var header = new StringBuilder();
        var quoted = false;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var next = reader.Read();
            if (next < 0)
            {
                if (quoted)
                {
                    throw CreateInvalidCsvException(filePath, "The first CSV record has an unterminated quoted field.");
                }

                return header.Length == 0 ? null : header.ToString();
            }

            var current = (char)next;
            if (current == '"')
            {
                header.Append(current);
                if (quoted && reader.Peek() == '"')
                {
                    header.Append((char)reader.Read());
                }
                else
                {
                    quoted = !quoted;
                }

                continue;
            }

            if (!quoted && (current == '\r' || current == '\n'))
            {
                if (current == '\r' && reader.Peek() == '\n')
                {
                    reader.Read();
                }

                return header.ToString();
            }

            header.Append(current);
        }
    }

    private static string[]? ReadCsvRecord(TextReader reader, char delimiter, string filePath, CancellationToken cancellationToken, out bool reachedEnd)
    {
        var cells = new List<string>();
        var cell = new StringBuilder();
        var quoted = false;
        var any = false;
        reachedEnd = false;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var next = reader.Read();
            if (next < 0)
            {
                reachedEnd = true;
                if (quoted)
                {
                    throw CreateInvalidCsvException(filePath, "CSV data has an unterminated quoted field.");
                }

                if (!any && cell.Length == 0 && cells.Count == 0)
                {
                    return null;
                }

                cells.Add(cell.ToString());
                return cells.ToArray();
            }

            any = true;
            var current = (char)next;
            if (current == '"')
            {
                if (quoted && reader.Peek() == '"')
                {
                    reader.Read();
                    cell.Append('"');
                }
                else
                {
                    quoted = !quoted;
                }

                continue;
            }

            if (!quoted && current == delimiter)
            {
                cells.Add(cell.ToString());
                cell.Clear();
                continue;
            }

            if (!quoted && (current == '\r' || current == '\n'))
            {
                if (current == '\r' && reader.Peek() == '\n')
                {
                    reader.Read();
                }

                cells.Add(cell.ToString());
                return cells.ToArray();
            }

            cell.Append(current);
        }
    }

    private static ExcelDataReadException CreateInvalidCsvException(string filePath, string detail)
        => new(
            ExcelDataReadError.InvalidData,
            $"CSV file '{Path.GetFileName(filePath)}' is invalid: {detail}",
            filePath);

    private static char DetectCsvDelimiter(string header)
    {
        // Locale-neutral CSV commonly uses one of these three delimiters. Delimiter
        // selection happens before decoding rows so comma/semicolon/tab files all enter
        // the same quoted-field parser below.
        var comma = 0;
        var semicolon = 0;
        var tab = 0;
        var quoted = false;
        for (var index = 0; index < header.Length; index++)
        {
            var character = header[index];
            if (character == '"')
            {
                if (quoted && index + 1 < header.Length && header[index + 1] == '"')
                {
                    index++;
                }
                else
                {
                    quoted = !quoted;
                }

                continue;
            }

            if (quoted)
            {
                continue;
            }

            switch (character)
            {
                case ',': comma++; break;
                case ';': semicolon++; break;
                case '\t': tab++; break;
            }
        }

        var counts = new[] { (Delimiter: ',', Count: comma), (Delimiter: ';', Count: semicolon), (Delimiter: '\t', Count: tab) };
        return counts.OrderByDescending(item => item.Count).First().Delimiter;
    }

    private static bool IsCsvFile(string filePath)
        => string.Equals(Path.GetExtension(filePath), ".csv", StringComparison.OrdinalIgnoreCase);

    private static void EnsureCsvSheet(string sheetName, string filePath)
    {
        if (!string.Equals(sheetName, CsvSheetName, StringComparison.OrdinalIgnoreCase))
        {
            throw new ExcelDataReadException(
                ExcelDataReadError.MissingSheet,
                $"CSV data source '{Path.GetFileName(filePath)}' exposes one sheet named '{CsvSheetName}'.",
                filePath,
                sheetName);
        }
    }

    private static List<ExcelPreviewRow> ReadPreviewRows(IExcelDataReader reader, int maxRows, CancellationToken cancellationToken)
    {
        var rows = new List<ExcelPreviewRow>();
        var rowNumber = 0;
        while (rows.Count < maxRows && reader.Read())
        {
            cancellationToken.ThrowIfCancellationRequested();
            rowNumber++;
            rows.Add(new ExcelPreviewRow(rowNumber, ReadRowCellsArray(reader)));
        }

        return rows;
    }

    private static string[] ReadRowCellsArray(IExcelDataReader reader)
    {
        var fieldCount = reader.FieldCount;
        var cells = new string[fieldCount];
        for (var column = 0; column < fieldCount; column++)
        {
            cells[column] = CleanCellText(FormatCellValue(reader, column));
        }

        return cells;
    }

    /// <summary>
    /// Finds <paramref name="sheetName"/> by advancing through the workbook's result sets
    /// (case-insensitive, matching Excel's own sheet-name uniqueness rule). Streaming means
    /// sheets before the match are never read row-by-row — only their names are touched.
    /// </summary>
    private static bool SeekToSheet(IExcelDataReader reader, string sheetName, out string availableSheets)
    {
        var names = new List<string>();
        do
        {
            names.Add(reader.Name);
            if (string.Equals(reader.Name, sheetName, StringComparison.OrdinalIgnoreCase))
            {
                availableSheets = string.Empty;
                return true;
            }
        } while (reader.NextResult());

        availableSheets = string.Join(", ", names);
        return false;
    }

    /// <summary>
    /// Opens a <see cref="FileStream"/> with <see cref="FileShare.ReadWrite"/>
    /// so that workbooks already open in Excel can still be read. For UNC/network
    /// paths, a read timeout is applied so that stalled connections do not hang
    /// the caller indefinitely.
    /// </summary>
    private static FileStream OpenFileStream(string filePath, CancellationToken cancellationToken = default)
    {
        FileStream stream;
        try
        {
            stream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite,
                bufferSize: 4096,
                useAsync: false);
        }
        catch (FileNotFoundException ex)
        {
            throw new ExcelDataReadException(
                ExcelDataReadError.MissingFile,
                $"Excel file was not found: {filePath}",
                filePath,
                innerException: ex);
        }
        catch (DirectoryNotFoundException ex)
        {
            throw new ExcelDataReadException(
                ExcelDataReadError.MissingFile,
                $"Excel folder was not found: {Path.GetDirectoryName(filePath)}",
                filePath,
                innerException: ex);
        }

        cancellationToken.ThrowIfCancellationRequested();
        return stream;
    }

    private static IExcelDataReader OpenReader(Stream stream, string filePath)
    {
        try
        {
            return ExcelReaderFactory.CreateReader(stream);
        }
        catch (Exception ex) when (ex is ExcelDataReader.Exceptions.ExcelReaderException
                                   or InvalidDataException
                                   or FormatException
                                   or NotSupportedException
                                   or ArgumentException
                                   or EndOfStreamException
                                   or IndexOutOfRangeException)
        {
            throw new ExcelDataReadException(
                ExcelDataReadError.InvalidWorkbook,
                $"Excel file is damaged or is not a valid .xlsx/.xlsm workbook: {Path.GetFileName(filePath)}",
                filePath,
                innerException: ex);
        }
    }

    private static bool IsNetworkPath(string filePath)
    {
        return filePath.StartsWith(@"\\", StringComparison.Ordinal)
               || filePath.StartsWith("//", StringComparison.Ordinal);
    }

    /// <summary>
    /// Converts a raw cell value to display text. Unlike ClosedXML's GetFormattedString(),
    /// this does not replicate Excel's exact number-format rendering (currency symbols,
    /// thousands separators, custom formats) — it is a best-effort approximation covering
    /// the cases that matter for label data: plain numbers (trimmed, no forced decimals),
    /// percentages (detected via the cell's format code), and dates. This is an accepted
    /// trade-off for the memory-usage fix switching away from ClosedXML's full-DOM parser.
    /// </summary>
    private static string FormatCellValue(IExcelDataReader reader, int column)
    {
        var value = reader.GetValue(column);
        switch (value)
        {
            case null:
                return string.Empty;
            case string text:
                return text;
            case bool flag:
                return flag ? "TRUE" : "FALSE";
            case DateTime date:
                return date.TimeOfDay == TimeSpan.Zero
                    ? date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                    : date.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            case double or float or decimal or int or long or short or byte:
                return FormatNumber(Convert.ToDouble(value, CultureInfo.InvariantCulture), reader, column);
            default:
                return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        }
    }

    private static string FormatNumber(double value, IExcelDataReader reader, int column)
    {
        string? formatCode = null;
        try
        {
            formatCode = reader.GetNumberFormatString(column);
        }
        catch (Exception ex) when (ex is IndexOutOfRangeException or NotSupportedException)
        {
            // Format lookup is best-effort — fall back to plain number formatting below.
        }

        if (formatCode is not null && formatCode.Contains('%'))
        {
            return (value * 100).ToString("0.##", CultureInfo.InvariantCulture) + "%";
        }

        return value.ToString("0.##########", CultureInfo.InvariantCulture);
    }

    private static string CleanCellText(string value)
    {
        return (value ?? string.Empty)
            .Replace("\r", " ")
            .Replace("\n", " ")
            .Trim();
    }

    private static string MakeUniqueHeader(string header, HashSet<string> headers)
    {
        var candidate = header;
        var index = 2;
        while (!headers.Add(candidate))
        {
            candidate = $"{header}_{index}";
            index++;
        }

        return candidate;
    }
}
