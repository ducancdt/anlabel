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

/// <summary>Result of a link check: column and row counts, or why the sheet could not be read.</summary>
public sealed record ExcelConnectionCheck(int ColumnCount, int RowCount, Exception? Failure)
{
    public bool Ok => Failure is null && ColumnCount > 0;
}

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
    /// condition is, but it guarantees the UI always gets back control with a clear, actionable
    /// error instead of an unrecoverable freeze. Since 2026-10-15 the read is also told to stop
    /// (see <see cref="ReadAsync{T}"/>); only a call stuck where it cannot see that keeps running.
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

    private readonly TimeSpan _localTimeout;

    public ExcelDataService()
        : this(DefaultLocalTimeout)
    {
    }

    /// <summary>A shorter local timeout lets tests reach the timeout path.</summary>
    internal ExcelDataService(TimeSpan localTimeout)
    {
        _localTimeout = localTimeout;
    }

    private TimeSpan TimeoutFor(string filePath) => IsNetworkPath(filePath) ? DefaultNetworkTimeout : _localTimeout;

    private static readonly TimeSpan StopGrace = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Runs a read on a worker thread. When the safety timeout fires, the read is
    /// told to stop too: it used to carry on in the background, holding the file
    /// open (Excel could not save over it) and doubling the work when the operator
    /// tried again. A call stuck where it cannot see the request still runs on,
    /// but the caller gets its answer either way. Internal so tests can give it
    /// a read that only ends when it is told to stop.
    /// </summary>
    internal async Task<T> ReadAsync<T>(string filePath, Func<CancellationToken, T> read, CancellationToken cancellationToken)
    {
        var stop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = stop.Token;
        // The task carries the stop token, so a read that stops on request ends
        // as cancelled and is not logged as a background failure, while a real
        // failure of a read nobody waits for still reaches the error log.
        var readTask = Task.Run(() => read(token), token);
        try
        {
            return await readTask.WaitAsync(TimeoutFor(filePath), cancellationToken);
        }
        catch (TimeoutException)
        {
            stop.Cancel();
            // A moment for the read to close the file before the timeout is
            // reported; Cancel during that moment is still a cancel.
            await Task.WhenAny(readTask, Task.Delay(StopGrace, cancellationToken));
            cancellationToken.ThrowIfCancellationRequested();
            throw;
        }
        finally
        {
            _ = readTask.ContinueWith(
                _ => stop.Dispose(),
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }
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
            return await ReadAsync(filePath, token =>
            {
                token.ThrowIfCancellationRequested();
                using var stream = OpenFileStream(filePath, token);
                return (IReadOnlyList<string>)new[] { CsvSheetName };
            }, cancellationToken);
        }

        return await ReadAsync(filePath, token =>
        {
            token.ThrowIfCancellationRequested();
            using var stream = OpenFileStream(filePath, token);
            using var reader = OpenReader(stream, filePath);
            var names = new List<string>();
            do
            {
                token.ThrowIfCancellationRequested();
                names.Add(reader.Name);
            } while (reader.NextResult());

            return (IReadOnlyList<string>)names;
        }, cancellationToken);
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
        var check = await CheckConnectionAsync(filePath, sheetName, headerRowIndex, cancellationToken);
        return check.Failure is not null
            ? (false, check.Failure.Message)
            : check.ColumnCount > 0
                ? (true, $"OK — {check.ColumnCount} column(s), {check.RowCount} row(s).")
                : (false, "Sheet has no columns at the given header row.");
    }

    /// <summary>
    /// The same check as <see cref="TestConnectionAsync"/>, returning the counts
    /// and the failure itself so the UI can explain it in the operator's language.
    /// Never throws except for cancellation.
    /// </summary>
    public async Task<ExcelConnectionCheck> CheckConnectionAsync(string filePath, string sheetName, int headerRowIndex = 1, CancellationToken cancellationToken = default)
    {
        try
        {
            var table = await LoadSheetAsync(filePath, sheetName, headerRowIndex, cancellationToken);
            return new ExcelConnectionCheck(table.Columns.Count, table.Rows.Count, null);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new ExcelConnectionCheck(0, 0, ex);
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
            return await ReadAsync(
                filePath,
                token =>
                {
                    try
                    {
                        return (IReadOnlyList<ExcelPreviewRow>)ReadCsvRows(filePath, maxRows, token)
                            .Select((cells, index) => new ExcelPreviewRow(index + 1, cells))
                            .ToArray();
                    }
                    catch (DecoderFallbackException ex)
                    {
                        throw CreateInvalidCsvEncodingException(filePath, sheetName, ex);
                    }
                },
                cancellationToken);
        }

        return await ReadAsync(filePath, token =>
        {
            token.ThrowIfCancellationRequested();
            using var stream = OpenFileStream(filePath, token);
            using var reader = OpenReader(stream, filePath);
            if (!SeekToSheet(reader, sheetName, token, out var availableSheets))
            {
                throw new ExcelDataReadException(
                    ExcelDataReadError.MissingSheet,
                    $"Excel sheet '{sheetName}' was not found in {Path.GetFileName(filePath)}. Available sheets: {availableSheets}.",
                    filePath,
                    sheetName);
            }

            return (IReadOnlyList<ExcelPreviewRow>)ReadPreviewRows(reader, maxRows, token);
        }, cancellationToken);
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

        return await ReadAsync(filePath, token =>
        {
            token.ThrowIfCancellationRequested();
            using var stream = OpenFileStream(filePath, token);
            using var reader = OpenReader(stream, filePath);
            var result = new List<ExcelSheetPreview>();
            do
            {
                token.ThrowIfCancellationRequested();
                var sheetName = reader.Name;
                result.Add(new ExcelSheetPreview(sheetName, ReadPreviewRows(reader, maxRows, token)));
            } while (reader.NextResult());

            return (IReadOnlyList<ExcelSheetPreview>)result;
        }, cancellationToken);
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

        return await ReadAsync(
            filePath,
            token =>
            {
                try
                {
                    return IsCsvFile(filePath)
                        ? LoadCsv(filePath, sheetName, headerRowIndex, token)
                        : LoadSheet(filePath, sheetName, headerRowIndex, token);
                }
                catch (DecoderFallbackException ex)
                {
                    throw CreateInvalidCsvEncodingException(filePath, sheetName, ex);
                }
            },
            cancellationToken);
    }

    private static DataTable LoadSheet(string filePath, string sheetName, int headerRowIndex, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var stream = OpenFileStream(filePath, cancellationToken);
        using var reader = OpenReader(stream, filePath);
        if (!SeekToSheet(reader, sheetName, cancellationToken, out var availableSheets))
        {
            throw new ExcelDataReadException(
                ExcelDataReadError.MissingSheet,
                $"Excel sheet '{sheetName}' was not found in {Path.GetFileName(filePath)}. Available sheets: {availableSheets}.",
                filePath,
                sheetName);
        }

        var table = new DataTable(sheetName);
        string[]? headerCells = null;
        HeaderColumns? header = null;
        // Source position -> table column. A column whose header cell is empty
        // becomes a field only once a row puts data in it: one stray cell far to
        // the right (column XFD) used to turn 16,378 empty columns into fields,
        // 1.4 GB and half a minute for 1,000 rows before the data grid even drew.
        var columns = Array.Empty<DataColumn?>();
        string[]? reusable = null;
        var rowNumber = 0;
        while (reader.Read())
        {
            cancellationToken.ThrowIfCancellationRequested();
            rowNumber++;
            var cells = ReadRowCells(reader, failClosedOnCellError: true, sheetName, rowNumber, filePath, rowNumber == headerRowIndex ? null : reusable);
            if (rowNumber == headerRowIndex)
            {
                headerCells = cells;
                header = new HeaderColumns(headerCells.Select(CleanCellText).ToArray());
                columns = new DataColumn?[headerCells.Length];
                for (var index = 0; index < columns.Length; index++)
                {
                    if (!string.IsNullOrWhiteSpace(header.Raw[index]))
                    {
                        columns[index] = header.AddColumn(table, columns, index);
                    }
                }

                continue;
            }

            // Rows other than the header are copied out right away, so one array
            // serves them all; a wide sheet allocated a large one per row.
            reusable = cells;
            if (rowNumber < headerRowIndex || header is null)
            {
                continue;
            }

            // Materialize each record as it is read. Keeping every string[]
            // until the end doubles peak memory for large workbooks, despite
            // using a forward-only reader.
            var hasValue = false;
            for (var index = 0; index < columns.Length && index < cells.Length; index++)
            {
                if (string.IsNullOrEmpty(cells[index]))
                {
                    continue;
                }

                hasValue = true;
                columns[index] ??= header.AddColumn(table, columns, index);
            }

            if (!hasValue)
            {
                continue;
            }

            var dataRow = table.NewRow();
            for (var index = 0; index < columns.Length; index++)
            {
                if (columns[index] is { } column)
                {
                    dataRow[column] = index < cells.Length ? cells[index] : string.Empty;
                }
            }

            table.Rows.Add(dataRow);
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

        // DB04: a formula saved without its calculated value reads as an empty
        // cell. Printing it would put a blank where the workbook expects data.
        if (IsOpenXmlWorkbook(filePath)
            && XlsxFormulaValueInspector.FindFormulaWithoutValue(filePath, sheetName, headerRowIndex, cancellationToken) is { } formula)
        {
            throw new ExcelDataReadException(
                ExcelDataReadError.InvalidData,
                $"Excel sheet '{sheetName}', row {formula.RowNumber}, column {formula.ColumnNumber} ({formula.Reference}) has a formula without a calculated value. Open the workbook in Excel, let it calculate, save it and import again.",
                filePath,
                sheetName)
            {
                RowNumber = formula.RowNumber,
                ColumnNumber = formula.ColumnNumber
            };
        }

        return table;
    }

    private static bool IsOpenXmlWorkbook(string filePath)
    {
        var extension = Path.GetExtension(filePath);
        return string.Equals(extension, ".xlsx", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".xlsm", StringComparison.OrdinalIgnoreCase);
    }

    private static DataTable LoadCsv(string filePath, string sheetName, int headerRowIndex, CancellationToken cancellationToken)
    {
        EnsureCsvSheet(sheetName, filePath);
        var table = new DataTable(CsvSheetName);

        string? headerRecord = null;
        var sampleRecords = new List<string>();
        using (var headerStream = OpenFileStream(filePath, cancellationToken))
        using (var headerReader = OpenStrictCsvReader(headerStream))
        {
            for (var recordNumber = 1; recordNumber <= headerRowIndex; recordNumber++)
            {
                headerRecord = ReadCsvHeaderRecord(headerReader, filePath, cancellationToken);
                if (headerRecord is null)
                {
                    if (recordNumber == 1) return table;
                    throw new ExcelDataReadException(ExcelDataReadError.InvalidHeaderRow,
                        $"Header row {headerRowIndex} is outside the CSV data range (last row: {recordNumber - 1}).",
                        filePath, sheetName);
                }
            }

            if (headerRecord is not null)
            {
                sampleRecords.Add(headerRecord);
                for (var i = 0; i < 5; i++)
                {
                    var next = ReadCsvHeaderRecord(headerReader, filePath, cancellationToken);
                    if (next is null) break;
                    sampleRecords.Add(next);
                }
            }
        }

        if (headerRecord is null)
        {
            return table;
        }

        // In LoadCsv, sampleRecords[0] is the real header row (any preamble lines above
        // headerRowIndex were already skipped), so the header is authoritative: a
        // delimiter-free header means a single-column file and a delimiter that only
        // appears inside data values must not be allowed to split it.
        var delimiter = DetectCsvDelimiter(sampleRecords, headerIsAuthoritative: true);
        var rowNumber = headerRowIndex - 1;
        var foundHeader = false;
        HeaderColumns? header = null;
        var columns = Array.Empty<DataColumn?>();
        using var stream = OpenFileStream(filePath, cancellationToken);
        using var reader = OpenStrictCsvReader(stream);
        // Preamble records do not define the table schema or its delimiter.
        for (var recordNumber = 1; recordNumber < headerRowIndex; recordNumber++)
        {
            ReadCsvHeaderRecord(reader, filePath, cancellationToken);
        }
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var record = ReadCsvRecord(reader, delimiter, filePath, cancellationToken, out var reachedEnd);
            if (record is not null)
            {
                rowNumber++;
                var cells = record;
                if (rowNumber == headerRowIndex)
                {
                    foundHeader = true;
                    // As in a workbook: a column without a header is a field only
                    // once a record puts data in it. Saving a sheet with one stray
                    // far cell as CSV gives every line thousands of empty fields.
                    header = new HeaderColumns(cells.Select(CleanCellText).ToArray());
                    columns = new DataColumn?[cells.Length];
                    for (var index = 0; index < columns.Length; index++)
                    {
                        if (!string.IsNullOrWhiteSpace(header.Raw[index]))
                        {
                            columns[index] = header.AddColumn(table, columns, index);
                        }
                    }
                }
                else if (rowNumber > headerRowIndex && header is not null)
                {
                    // ADR-024: padding a short record would shift every later value
                    // into the wrong column; only a completely empty line is skipped.
                    if (cells.Length != columns.Length && cells.Any(cell => cell.Length > 0))
                    {
                        throw CreateInvalidCsvException(filePath,
                            $"Record {rowNumber} has {cells.Length} fields; the header defines {columns.Length}.",
                            rowNumber);
                    }

                    var hasValue = false;
                    for (var index = 0; index < columns.Length && index < cells.Length; index++)
                    {
                        if (string.IsNullOrEmpty(cells[index]))
                        {
                            continue;
                        }

                        hasValue = true;
                        columns[index] ??= header.AddColumn(table, columns, index);
                    }

                    if (hasValue)
                    {
                        var dataRow = table.NewRow();
                        for (var index = 0; index < columns.Length; index++)
                        {
                            if (columns[index] is { } column)
                            {
                                dataRow[column] = index < cells.Length ? cells[index] : string.Empty;
                            }
                        }

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
        var sampleRecords = new List<string>();
        using (var headerStream = OpenFileStream(filePath, cancellationToken))
        using (var headerReader = OpenStrictCsvReader(headerStream))
        {
            for (var i = 0; i < 10; i++)
            {
                var sample = ReadCsvHeaderRecord(headerReader, filePath, cancellationToken);
                if (sample is null) break;
                sampleRecords.Add(sample);
            }
        }

        if (sampleRecords.Count == 0)
        {
            return records;
        }

        // The preview path reads from the top of the file with no knowledge of which row
        // the user will pick as the header, so sampleRecords[0] may be a preamble/title
        // line. The header is therefore NOT authoritative and data-row structural detection
        // is still consulted so a delimited real header under a preamble is discovered.
        var delimiter = DetectCsvDelimiter(sampleRecords, headerIsAuthoritative: false);
        using var stream = OpenFileStream(filePath, cancellationToken);
        using var reader = OpenStrictCsvReader(stream);
        while (maxRows is null || records.Count < maxRows.Value)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var record = ReadCsvRecord(reader, delimiter, filePath, cancellationToken, out var reachedEnd);
            if (record is not null)
            {
                records.Add(record);
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
        var closedQuote = false;
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
                if (closedQuote || (!quoted && cell.Length != 0))
                {
                    throw CreateInvalidCsvException(filePath, $"Unexpected quote in field {cells.Count + 1}.");
                }

                if (quoted && reader.Peek() == '"')
                {
                    reader.Read();
                    cell.Append('"');
                }
                else
                {
                    closedQuote = quoted;
                    quoted = !quoted;
                }

                continue;
            }

            if (!quoted && current == delimiter)
            {
                cells.Add(cell.ToString());
                cell.Clear();
                closedQuote = false;
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

            if (closedQuote)
            {
                throw CreateInvalidCsvException(filePath,
                    $"Unexpected character after the closing quote in field {cells.Count + 1}.");
            }

            cell.Append(current);
        }
    }

    private static ExcelDataReadException CreateInvalidCsvException(string filePath, string detail, int? rowNumber = null)
        => new(
            ExcelDataReadError.InvalidData,
            $"CSV file '{Path.GetFileName(filePath)}' is invalid: {detail}",
            filePath)
        {
            RowNumber = rowNumber
        };

    private static readonly char[] CandidateDelimiters = { ',', ';', '\t' };

    /// <param name="headerIsAuthoritative">
    /// True when <c>records[0]</c> is guaranteed to be the real header row (LoadCsv, where any
    /// preamble above the selected header row has already been skipped). In that case a
    /// delimiter-free header is definitive proof of a single-column file, so data-row structural
    /// detection is skipped entirely — a comma (or other candidate) appearing only inside data
    /// values must never split a delimiter-free header. False when <c>records[0]</c> may be a
    /// preamble/title line (the preview path, which does not yet know which row is the header);
    /// there, data-row structural detection is still consulted so a delimited real header sitting
    /// under a preamble is discovered.
    /// </param>
    private static char DetectCsvDelimiter(IReadOnlyList<string> records, bool headerIsAuthoritative)
    {
        var samples = records.Where(record => !string.IsNullOrEmpty(record)).ToList();
        if (samples.Count == 0)
        {
            return ',';
        }

        // The header row defines the schema, so its delimiter structures the file.
        // A delimited header wins outright; this preserves multi-column behavior.
        var (headerComma, headerSemicolon, headerTab) = CountDelimiters(samples[0]);
        if (headerComma > 0 || headerSemicolon > 0 || headerTab > 0)
        {
            var counts = new[]
            {
                (Delimiter: ',', Count: headerComma),
                (Delimiter: ';', Count: headerSemicolon),
                (Delimiter: '\t', Count: headerTab)
            };
            return counts.OrderByDescending(item => item.Count).First().Delimiter;
        }

        // A delimiter-free header is either a single-column file or (preview only) a
        // preamble/title line preceding the real header. When the header is authoritative
        // we do NOT consult data rows: a delimiter that appears only in data values (e.g.
        // "Smith, John" under a "Name" header) is structurally indistinguishable from a
        // genuine two-column file, so the only safe, header-authoritative choice is to treat
        // the file as single-column. For the preview path the header may be a preamble, so a
        // stable multi-column layout across several data records is still adopted.
        if (!headerIsAuthoritative)
        {
            foreach (var candidate in CandidateDelimiters)
            {
                if (IsStructuralDelimiter(candidate, samples))
                {
                    return candidate;
                }
            }
        }

        // Single column: pick a delimiter absent from every record so each row is
        // parsed as one field, preserving values that legitimately contain commas.
        foreach (var candidate in CandidateDelimiters)
        {
            if (samples.All(record => CountDelimiter(candidate, record) == 0))
            {
                return candidate;
            }
        }

        // Finding 2: no candidate is absent from every record, so every choice will split
        // some value. The header is delimiter-free here, so none splits the header itself;
        // choose the least-damaging candidate — the one with the fewest total occurrences
        // across all samples — to preserve as much of the data verbatim as possible.
        // Tie-break by CandidateDelimiters order (comma, then semicolon, then tab).
        var leastDamaging = ',';
        var fewest = int.MaxValue;
        foreach (var candidate in CandidateDelimiters)
        {
            var total = samples.Sum(record => CountDelimiter(candidate, record));
            if (total < fewest)
            {
                fewest = total;
                leastDamaging = candidate;
            }
        }

        return leastDamaging;
    }

    private static bool IsStructuralDelimiter(char delimiter, IReadOnlyList<string> records)
    {
        var fieldCount = -1;
        var matches = 0;
        foreach (var record in records)
        {
            var fields = CountDelimiter(delimiter, record) + 1;
            if (fields <= 1)
            {
                continue;
            }

            if (fieldCount < 0)
            {
                fieldCount = fields;
            }
            else if (fields != fieldCount)
            {
                return false;
            }

            matches++;
        }

        return matches >= 2;
    }

    private static int CountDelimiter(char delimiter, string text)
    {
        var (comma, semicolon, tab) = CountDelimiters(text);
        return delimiter switch
        {
            ',' => comma,
            ';' => semicolon,
            '\t' => tab,
            _ => 0
        };
    }

    private static (int Comma, int Semicolon, int Tab) CountDelimiters(string text)
    {
        var comma = 0;
        var semicolon = 0;
        var tab = 0;
        var quoted = false;
        for (var index = 0; index < text.Length; index++)
        {
            var character = text[index];
            if (character == '"')
            {
                if (quoted && index + 1 < text.Length && text[index + 1] == '"')
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

        return (comma, semicolon, tab);
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
            rows.Add(new ExcelPreviewRow(rowNumber, ReadRowCells(reader, failClosedOnCellError: false)));
        }

        return rows;
    }

    /// <summary>
    /// Reads one row of cells from the streaming reader according to the target API contract:
    /// - For <see cref="LoadSheet"/>: <paramref name="failClosedOnCellError"/> is true, halting the entire
    ///   import and throwing <see cref="ExcelDataReadException"/> with exact row/column coordinates whenever
    ///   a cell error is encountered (DB04 fail-closed policy).
    /// - For <see cref="ReadPreviewRows"/>: <paramref name="failClosedOnCellError"/> is false, preserving
    ///   the standard Excel error token (e.g. #DIV/0!, #N/A) so the operator sees corrupted cells in the preview
    ///   grid truthfully rather than having them masked as blank or failing before review (DB04 preview truthfulness).
    /// </summary>
    private static string[] ReadRowCells(
        IExcelDataReader reader,
        bool failClosedOnCellError,
        string? sheetName = null,
        int rowNumber = 0,
        string? filePath = null,
        string[]? reusable = null)
    {
        var fieldCount = reader.FieldCount;
        // Every cell is written below, so a reused array keeps nothing of its last row.
        var cells = reusable?.Length == fieldCount ? reusable : new string[fieldCount];
        for (var column = 0; column < fieldCount; column++)
        {
            if (reader.GetCellError(column) is { } cellError)
            {
                if (failClosedOnCellError)
                {
                    throw new ExcelDataReadException(
                        ExcelDataReadError.InvalidData,
                        $"Excel sheet '{sheetName}', row {rowNumber}, column {column + 1} contains an Excel error ({FormatCellError(cellError)}). Correct the source cell before importing.",
                        filePath ?? string.Empty,
                        sheetName)
                    {
                        RowNumber = rowNumber,
                        ColumnNumber = column + 1
                    };
                }

                cells[column] = FormatCellError(cellError);
                continue;
            }

            cells[column] = FormatCellValue(reader, column, failClosedOnCellError, sheetName, rowNumber, filePath);
        }

        return cells;
    }

    /// <summary>
    /// Finds <paramref name="sheetName"/> by advancing through the workbook's result sets
    /// (case-insensitive, matching Excel's own sheet-name uniqueness rule). Streaming means
    /// sheets before the match are never read row-by-row — only their names are touched.
    /// </summary>
    private static bool SeekToSheet(IExcelDataReader reader, string sheetName, CancellationToken cancellationToken, out string availableSheets)
    {
        var names = new List<string>();
        do
        {
            // Opening each sheet reads it through once, so a stop request is checked between sheets.
            cancellationToken.ThrowIfCancellationRequested();
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

        if (cancellationToken.IsCancellationRequested)
        {
            // Close it now: a handle left to the finalizer keeps Excel from saving the file.
            stream.Dispose();
            cancellationToken.ThrowIfCancellationRequested();
        }

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
    /// Preserves text verbatim and formats authored numeric display patterns with
    /// ExcelNumberFormat, without loading a workbook DOM. Date and boolean values
    /// retain the existing invariant serialization policy.
    /// </summary>
    private static string FormatCellValue(
        IExcelDataReader reader,
        int column,
        bool failClosedOnCellError = true,
        string? sheetName = null,
        int rowNumber = 0,
        string? filePath = null)
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
                return FormatNumber(Convert.ToDouble(value, CultureInfo.InvariantCulture), reader, column, failClosedOnCellError, sheetName, rowNumber, filePath);
            default:
                return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        }
    }

    private static string FormatCellError(CellError error)
    {
        return error switch
        {
            CellError.DIV0 => "#DIV/0!",
            CellError.NA => "#N/A",
            CellError.REF => "#REF!",
            CellError.VALUE => "#VALUE!",
            CellError.NAME => "#NAME?",
            CellError.NUM => "#NUM!",
            CellError.NULL => "#NULL!",
            CellError.GETTING_DATA => "#GETTING_DATA",
            _ => $"#{error}"
        };
    }

    private static string FormatNumber(
        double value,
        IExcelDataReader reader,
        int column,
        bool failClosedOnCellError = true,
        string? sheetName = null,
        int rowNumber = 0,
        string? filePath = null)
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

        if (!string.IsNullOrEmpty(formatCode)
            && !string.Equals(formatCode, "General", StringComparison.OrdinalIgnoreCase))
        {
            var numberFormat = new ExcelNumberFormat.NumberFormat(formatCode);
            if (!numberFormat.IsValid)
            {
                if (failClosedOnCellError)
                {
                    // LoadSheet (DB04 fail-closed): halt the import with exact coordinates
                    // rather than silently rendering a value under an unsupported format.
                    throw new ExcelDataReadException(
                        ExcelDataReadError.InvalidData,
                        $"Excel sheet '{sheetName}', row {rowNumber}, column {column + 1} uses an unsupported number format ('{formatCode}'). Correct the source cell's format before importing.",
                        filePath ?? string.Empty,
                        sheetName)
                    {
                        RowNumber = rowNumber,
                        ColumnNumber = column + 1
                    };
                }

                // Preview (DB04 truthfulness): sheetName/filePath are null and rowNumber 0
                // on this path, so a thrown message would be useless and would break preview.
                // Fall back to a best-effort round-trip of the raw numeric value, matching how
                // other non-fatal preview cells are surfaced, so the preview stays truthful.
                return value.ToString("R", CultureInfo.InvariantCulture);
            }

            return numberFormat.Format(value, CultureInfo.InvariantCulture);
        }

        // General has no authored decimal precision. Preserve the reader's value
        // rather than rounding small numbers to zero or discarding fractional digits.
        return value.ToString("R", CultureInfo.InvariantCulture);
    }

    private static string CleanCellText(string value)
    {
        // Normalize schema names only. Data values may contain significant whitespace
        // or line breaks used by bindings and barcode payloads.
        return (value ?? string.Empty)
            .Replace("\r", " ")
            .Replace("\n", " ")
            .Trim();
    }

    /// <summary>
    /// Names and header kinds for a header row, worked out once for every source
    /// position, so a column added later gets the same name and origin it would
    /// have had if it had been added with the others.
    /// </summary>
    private sealed class HeaderColumns
    {
        private readonly string[] _assigned;
        private readonly HashSet<string> _repeated;

        public HeaderColumns(IReadOnlyList<string> rawHeaders)
        {
            Raw = rawHeaders;
            _assigned = AssignColumnHeaders(rawHeaders);
            _repeated = rawHeaders
                .Where(raw => !string.IsNullOrWhiteSpace(raw))
                .GroupBy(raw => raw, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        public IReadOnlyList<string> Raw { get; }

        /// <summary>
        /// Adds the column at source position <paramref name="index"/> in front of
        /// every column further right, so the table keeps the file's column order.
        /// Rows already read get an empty value in it.
        /// </summary>
        public DataColumn AddColumn(DataTable table, IReadOnlyList<DataColumn?> columns, int index)
        {
            var raw = Raw[index] ?? string.Empty;
            var kind = string.IsNullOrWhiteSpace(raw)
                ? DataHeaderKind.Blank
                : _repeated.Contains(raw) ? DataHeaderKind.Repeated : DataHeaderKind.Unique;
            var column = new DataColumn(_assigned[index], typeof(string)) { DefaultValue = string.Empty };
            table.Columns.Add(column);
            var ordinal = 0;
            for (var left = 0; left < index; left++)
            {
                if (columns[left] is not null)
                {
                    ordinal++;
                }
            }

            column.SetOrdinal(ordinal);
            DataColumnOrigin.Attach(column, new DataColumnOrigin(_assigned[index], index + 1, raw, kind));
            return column;
        }
    }

    /// <summary>
    /// Every distinct raw header is reserved before any suffix is chosen.
    /// The first occurrence keeps that name. A later duplicate or a blank
    /// must not be renamed onto another column, even when that name is itself repeated.
    /// </summary>
    private static string[] AssignColumnHeaders(IReadOnlyList<string> rawHeaders)
    {
        var comparer = StringComparer.OrdinalIgnoreCase;
        var reserved = new HashSet<string>(comparer);
        foreach (var raw in rawHeaders)
        {
            if (!string.IsNullOrWhiteSpace(raw))
            {
                reserved.Add(raw);
            }
        }

        var used = new HashSet<string>(comparer);
        var assigned = new string[rawHeaders.Count];
        for (var column = 0; column < rawHeaders.Count; column++)
        {
            var raw = rawHeaders[column] ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(raw) && used.Add(raw))
            {
                assigned[column] = raw;
                continue;
            }

            var stem = string.IsNullOrWhiteSpace(raw) ? $"Column{column + 1}" : raw;
            assigned[column] = ClaimNextHeader(stem, used, reserved);
        }

        return assigned;
    }

    private static string ClaimNextHeader(string stem, HashSet<string> used, HashSet<string> reserved)
    {
        var suffix = 2;
        var candidate = stem;
        while (true)
        {
            if (!reserved.Contains(candidate) && used.Add(candidate))
            {
                return candidate;
            }

            candidate = $"{stem}_{suffix}";
            suffix++;
        }
    }

    /// <summary>
    /// StreamReader BOM detection switches to a decoder that replaces invalid bytes
    /// with U+FFFD. Detect the mark here and keep a decoder that throws instead.
    /// </summary>
    private static StreamReader OpenStrictCsvReader(Stream stream)
    {
        var (encoding, prefixed) = OpenCsvPayload(stream);
        return new StreamReader(prefixed, encoding, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true);
    }

    private static (Encoding Encoding, Stream Stream) OpenCsvPayload(Stream stream)
    {
        var head = new byte[4];
        var read = 0;
        while (read < head.Length)
        {
            var count = stream.Read(head, read, head.Length - read);
            if (count == 0)
            {
                break;
            }

            read += count;
        }

        Encoding encoding;
        var bomLength = 0;
        if (read >= 4 && head[0] == 0xFF && head[1] == 0xFE && head[2] == 0x00 && head[3] == 0x00)
        {
            encoding = new UTF32Encoding(bigEndian: false, byteOrderMark: false, throwOnInvalidCharacters: true);
            bomLength = 4;
        }
        else if (read >= 4 && head[0] == 0x00 && head[1] == 0x00 && head[2] == 0xFE && head[3] == 0xFF)
        {
            encoding = new UTF32Encoding(bigEndian: true, byteOrderMark: false, throwOnInvalidCharacters: true);
            bomLength = 4;
        }
        else if (read >= 2 && head[0] == 0xFF && head[1] == 0xFE)
        {
            encoding = new UnicodeEncoding(bigEndian: false, byteOrderMark: false, throwOnInvalidBytes: true);
            bomLength = 2;
        }
        else if (read >= 2 && head[0] == 0xFE && head[1] == 0xFF)
        {
            encoding = new UnicodeEncoding(bigEndian: true, byteOrderMark: false, throwOnInvalidBytes: true);
            bomLength = 2;
        }
        else if (read >= 3 && head[0] == 0xEF && head[1] == 0xBB && head[2] == 0xBF)
        {
            encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
            bomLength = 3;
        }
        else
        {
            encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
        }

        var leftover = new byte[read - bomLength];
        Array.Copy(head, bomLength, leftover, 0, leftover.Length);
        return (encoding, new CsvPrefixedStream(leftover, stream));
    }

    private static ExcelDataReadException CreateInvalidCsvEncodingException(string filePath, string? sheetName, DecoderFallbackException ex)
        => new(
            ExcelDataReadError.InvalidData,
            $"CSV file '{Path.GetFileName(filePath)}' contains byte sequences that are not valid for its encoding. The file was rejected so field values are not replaced with a substitute character.",
            filePath,
            sheetName,
            ex);

    private sealed class CsvPrefixedStream : Stream
    {
        private readonly byte[] _prefix;
        private readonly Stream _inner;
        private int _prefixPos;

        public CsvPrefixedStream(byte[] prefix, Stream inner)
        {
            _prefix = prefix;
            _inner = inner;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_prefixPos < _prefix.Length)
            {
                var take = Math.Min(count, _prefix.Length - _prefixPos);
                Array.Copy(_prefix, _prefixPos, buffer, offset, take);
                _prefixPos += take;
                return take;
            }

            return _inner.Read(buffer, offset, count);
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
