using System.Data;
using ClosedXML.Excel;

namespace ANLAbel.Data.Excel;

public sealed class ExcelDataService
{
    public IReadOnlyList<string> GetSheetNames(string filePath)
    {
        using var workbook = new XLWorkbook(filePath);
        return workbook.Worksheets.Select(sheet => sheet.Name).ToArray();
    }

    public Task<DataTable> LoadSheetAsync(string filePath, string sheetName, int headerRowIndex = 1, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => LoadSheet(filePath, sheetName, headerRowIndex), cancellationToken);
    }

    private static DataTable LoadSheet(string filePath, string sheetName, int headerRowIndex)
    {
        using var workbook = new XLWorkbook(filePath);
        var worksheet = workbook.Worksheet(sheetName);
        var usedRange = worksheet.RangeUsed();
        var table = new DataTable(sheetName);

        if (usedRange is null)
        {
            return table;
        }

        var firstColumn = usedRange.RangeAddress.FirstAddress.ColumnNumber;
        var lastColumn = usedRange.RangeAddress.LastAddress.ColumnNumber;
        var lastRow = usedRange.RangeAddress.LastAddress.RowNumber;
        var headers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var column = firstColumn; column <= lastColumn; column++)
        {
            var rawHeader = CleanCellText(worksheet.Cell(headerRowIndex, column).GetFormattedString());
            var header = string.IsNullOrWhiteSpace(rawHeader) ? $"Column{column - firstColumn + 1}" : rawHeader;
            header = MakeUniqueHeader(header, headers);
            table.Columns.Add(header, typeof(string));
        }

        for (var row = headerRowIndex + 1; row <= lastRow; row++)
        {
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
