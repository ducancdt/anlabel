using System.Diagnostics;
using System.Windows;
using ANLAbel.App;
using ANLAbel.Core.Enums;
using ANLAbel.Core.Models;
using ANLAbel.Data;
using ANLAbel.Data.Excel;
using ANLAbel.Data.PrintLogs;
using ANLAbel.Printing.PrinterProfiles;
using ClosedXML.Excel;

/// <summary>
/// Plan 13 GĐ4: import and preview cost for 1, 1,000 and 10,000 rows. Prints
/// MEASURE lines for the published limits and fails only on gross regressions.
/// </summary>
internal static class DataVolumeRegression
{
    public static async Task ImportAndPreviewCostIsMeasured()
    {
        var directory = Path.Combine(Path.GetTempPath(), "anlabel-volume-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            foreach (var rows in new[] { 1, 1_000, 10_000 })
            {
                var xlsx = Path.Combine(directory, $"rows-{rows}.xlsx");
                using (var workbook = new XLWorkbook())
                {
                    var sheet = workbook.AddWorksheet("Data");
                    string[] headers = ["Part", "Lot", "Qty", "Desc", "Ean", "Date"];
                    for (var column = 0; column < headers.Length; column++)
                    {
                        sheet.Cell(1, column + 1).Value = headers[column];
                    }

                    for (var row = 0; row < rows; row++)
                    {
                        sheet.Cell(row + 2, 1).Value = $"PN-{row:000000}";
                        sheet.Cell(row + 2, 2).Value = $"LOT-{row % 97:000}";
                        sheet.Cell(row + 2, 3).Value = 1;
                        sheet.Cell(row + 2, 4).Value = $"Mô tả sản phẩm số {row}";
                        sheet.Cell(row + 2, 5).Value = "5901234123457";
                        sheet.Cell(row + 2, 6).Value = "2026-10-13";
                    }

                    workbook.SaveAs(xlsx);
                }

                var csv = Path.Combine(directory, $"rows-{rows}.csv");
                await File.WriteAllLinesAsync(csv, new[] { "Part,Lot,Qty,Desc,Ean,Date" }
                    .Concat(Enumerable.Range(0, rows).Select(row => $"PN-{row:000000},LOT-{row % 97:000},1,Mô tả sản phẩm số {row},5901234123457,2026-10-13")));

                var (xlsxMs, xlsxMb) = await MeasureImportAsync(xlsx, "Data");
                var (csvMs, csvMb) = await MeasureImportAsync(csv, ExcelDataService.CsvSheetName);
                var previewMs = rows <= 1_000 ? MeasurePreview(rows) : (double?)null;
                Console.WriteLine(
                    $"MEASURE dữ liệu {rows:N0} dòng: nhập XLSX {xlsxMs:0} ms (+{xlsxMb:0.0} MB), nhập CSV {csvMs:0} ms (+{csvMb:0.0} MB)" +
                    (previewMs is { } ms ? $", mở xem trước {ms:0} ms" : string.Empty));
                Require(xlsxMs < 20_000 && csvMs < 20_000, $"Importing {rows} rows took {xlsxMs:0}/{csvMs:0} ms.");
            }
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static async Task<(double Milliseconds, double Megabytes)> MeasureImportAsync(string path, string sheet)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        var before = GC.GetTotalMemory(forceFullCollection: true);
        var watch = Stopwatch.StartNew();
        var table = await new ExcelDataService().LoadSheetAsync(path, sheet);
        var elapsed = watch.Elapsed.TotalMilliseconds;
        var after = GC.GetTotalMemory(forceFullCollection: true);
        GC.KeepAlive(table);
        return (elapsed, Math.Max(0, after - before) / 1048576.0);
    }

    private static double MeasurePreview(int rows)
    {
        double elapsed = 0;
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var template = new LabelTemplate { Name = "Khối lượng", WidthMm = 100, HeightMm = 50, Dpi = 203 };
                template.PrinterProfile.Dpi = 203;
                template.Objects.Add(new LabelObject { Id = "part", Type = ObjectType.Text, Name = "Mã", BindingExpression = "{Part}", XMm = 5, YMm = 5, WidthMm = 40, HeightMm = 6 });
                template.Objects.Add(new LabelObject { Id = "ean", Type = ObjectType.BarcodeCode128, Name = "EAN", BarcodeSymbology = BarcodeSymbology.Ean13, BindingExpression = "{Ean}", XMm = 5, YMm = 15, WidthMm = 50, HeightMm = 20 });
                var prepared = Enumerable.Range(0, rows)
                    .Select(row => (IReadOnlyDictionary<string, string>?)new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["Part"] = $"PN-{row:000000}",
                        ["Ean"] = "5901234123457"
                    })
                    .ToArray();
                var watch = Stopwatch.StartNew();
                var window = new PrintPreviewWindow(
                    template,
                    prepared[0],
                    null,
                    new PrintService(queueLookup: new MissingPrinterQueueLookup()),
                    new PrintLogService(Path.Combine(Path.GetTempPath(), $"anlabel-volume-{Guid.NewGuid():N}.csv")),
                    string.Empty,
                    preparedRows: prepared);
                var deadline = DateTime.UtcNow.AddMinutes(3);
                while ((window.IsPreviewBusy || window.Pages.Count == 0) && DateTime.UtcNow < deadline)
                {
                    System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);
                    Thread.Sleep(5);
                }

                elapsed = watch.Elapsed.TotalMilliseconds;
                Require(window.Pages.Count == rows, $"Preview must prepare {rows} labels, prepared {window.Pages.Count}.");
                window.Close();
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (failure is not null)
        {
            throw failure;
        }

        return elapsed;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
