using ClosedXML.Excel;

namespace ANLAbel.Data.PrintLogs;

public sealed class PrintLogService
{
    public PrintLogService()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ANLAbel",
            "print-history.xlsx"))
    {
    }

    public PrintLogService(string logFilePath)
    {
        LogFilePath = logFilePath;
    }

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

    public string LogFilePath { get; }

    public Task AppendAsync(PrintLogEntry entry, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => Append(entry), cancellationToken);
    }

    public Task AppendManyAsync(IEnumerable<PrintLogEntry> entries, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => AppendMany(entries), cancellationToken);
    }

    private void Append(PrintLogEntry entry)
    {
        AppendMany(new[] { entry });
    }

    private void AppendMany(IEnumerable<PrintLogEntry> entries)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(LogFilePath)!);

        using var workbook = File.Exists(LogFilePath) ? new XLWorkbook(LogFilePath) : new XLWorkbook();
        var worksheet = workbook.Worksheets.FirstOrDefault(sheet => sheet.Name == "PrintHistory")
            ?? workbook.AddWorksheet("PrintHistory");

        EnsureHeaders(worksheet);
        var row = Math.Max(2, worksheet.LastRowUsed()?.RowNumber() + 1 ?? 2);
        foreach (var entry in entries)
        {
            worksheet.Cell(row, 1).Value = entry.PrintedAt;
            worksheet.Cell(row, 1).Style.DateFormat.Format = "yyyy-mm-dd hh:mm:ss";
            worksheet.Cell(row, 2).Value = entry.PartNo;
            worksheet.Cell(row, 3).Value = entry.ItemName;
            worksheet.Cell(row, 4).Value = entry.Lot;
            worksheet.Cell(row, 5).Value = entry.Quantity;
            worksheet.Cell(row, 6).Value = entry.LabelContent;
            worksheet.Cell(row, 7).Value = entry.RowData;
            worksheet.Cell(row, 8).Value = entry.TemplateName;
            worksheet.Cell(row, 9).Value = entry.PrinterName;
            worksheet.Cell(row, 10).Value = entry.PrintMode;
            worksheet.Cell(row, 11).Value = entry.LabelIndex;
            worksheet.Cell(row, 12).Value = entry.ExcelFilePath;
            worksheet.Cell(row, 13).Value = entry.ExcelSheetName;
            worksheet.Cell(row, 14).Value = entry.Notes;
            row++;
        }

        worksheet.Columns().AdjustToContents();
        workbook.SaveAs(LogFilePath);
    }

    private static void EnsureHeaders(IXLWorksheet worksheet)
    {
        for (var index = 0; index < Headers.Length; index++)
        {
            var cell = worksheet.Cell(1, index + 1);
            cell.Value = Headers[index];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#DCEBFF");
        }
    }
}
