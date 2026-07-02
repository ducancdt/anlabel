using System.Data;
using ClosedXML.Excel;

namespace ANLAbel.Data.Excel;

public sealed class ExcelDataService
{
    private static readonly TimeSpan DefaultNetworkTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Returns sheet names from the workbook. Runs on a background thread so it
    /// is safe to call from the UI thread. Supports cancellation and opens files
    /// with <see cref="FileShare.ReadWrite"/> so that workbooks already open in
    /// Excel can still be read.
    /// </summary>
    public async Task<IReadOnlyList<string>> GetSheetNamesAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var readTask = Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var stream = OpenFileStream(filePath, cancellationToken);
            using var workbook = OpenWorkbook(stream, filePath);
            cancellationToken.ThrowIfCancellationRequested();
            return (IReadOnlyList<string>)workbook.Worksheets.Select(sheet => sheet.Name).ToArray();
        }, CancellationToken.None);

        return IsNetworkPath(filePath)
            ? await readTask.WaitAsync(DefaultNetworkTimeout, cancellationToken)
            : await readTask.WaitAsync(cancellationToken);
    }

    /// <summary>
    /// Backwards-compatible synchronous overload.
    /// </summary>
    public IReadOnlyList<string> GetSheetNames(string filePath)
    {
        using var stream = OpenFileStream(filePath);
        using var workbook = OpenWorkbook(stream, filePath);
        return workbook.Worksheets.Select(sheet => sheet.Name).ToArray();
    }

    public async Task<DataTable> LoadSheetAsync(string filePath, string sheetName, int headerRowIndex = 1, CancellationToken cancellationToken = default)
    {
        if (headerRowIndex < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(headerRowIndex), "Excel header row must be 1 or greater.");
        }

        var readTask = Task.Run(
            () => LoadSheet(filePath, sheetName, headerRowIndex, cancellationToken),
            CancellationToken.None);

        return IsNetworkPath(filePath)
            ? await readTask.WaitAsync(DefaultNetworkTimeout, cancellationToken)
            : await readTask.WaitAsync(cancellationToken);
    }

    private static DataTable LoadSheet(string filePath, string sheetName, int headerRowIndex, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var stream = OpenFileStream(filePath, cancellationToken);
        using var workbook = OpenWorkbook(stream, filePath);
        if (!workbook.TryGetWorksheet(sheetName, out var worksheet))
        {
            var availableSheets = string.Join(", ", workbook.Worksheets.Select(sheet => sheet.Name));
            throw new ExcelDataReadException(
                ExcelDataReadError.MissingSheet,
                $"Excel sheet '{sheetName}' was not found in {Path.GetFileName(filePath)}. Available sheets: {availableSheets}.",
                filePath,
                sheetName);
        }
        var usedRange = worksheet.RangeUsed();
        var table = new DataTable(sheetName);

        if (usedRange is null)
        {
            return table;
        }

        var firstColumn = usedRange.RangeAddress.FirstAddress.ColumnNumber;
        var lastColumn = usedRange.RangeAddress.LastAddress.ColumnNumber;
        var lastRow = usedRange.RangeAddress.LastAddress.RowNumber;
        if (headerRowIndex > lastRow)
        {
            throw new ExcelDataReadException(
                ExcelDataReadError.InvalidHeaderRow,
                $"Header row {headerRowIndex} is outside the used range of sheet '{sheetName}' (last row: {lastRow}).",
                filePath,
                sheetName);
        }

        var headers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var column = firstColumn; column <= lastColumn; column++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var rawHeader = CleanCellText(worksheet.Cell(headerRowIndex, column).GetFormattedString());
            var header = string.IsNullOrWhiteSpace(rawHeader) ? $"Column{column - firstColumn + 1}" : rawHeader;
            header = MakeUniqueHeader(header, headers);
            table.Columns.Add(header, typeof(string));
        }

        for (var row = headerRowIndex + 1; row <= lastRow; row++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var dataRow = table.NewRow();
            var hasValue = false;

            for (var column = firstColumn; column <= lastColumn; column++)
            {
                var value = CleanCellText(worksheet.Cell(row, column).GetFormattedString());
                if (!string.IsNullOrEmpty(value))
                {
                    hasValue = true;
                }

                dataRow[column - firstColumn] = value;
            }

            if (hasValue)
            {
                table.Rows.Add(dataRow);
            }
        }

        return table;
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

    private static XLWorkbook OpenWorkbook(Stream stream, string filePath)
    {
        try
        {
            return new XLWorkbook(stream);
        }
        catch (Exception ex) when (ex is InvalidDataException
                                   or FormatException
                                   or NotSupportedException
                                   or ArgumentException)
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
