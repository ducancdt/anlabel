using ANLAbel.Data.Excel;
using ClosedXML.Excel;
using Xunit;

namespace ANLAbel.UnitTests;

/// <summary>
/// Rows an operator filtered away in Excel are read (nothing is dropped) and
/// reported as hidden, so printing can start without them and say so.
/// </summary>
public sealed class ExcelHiddenRowTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"anl-hidden-{Guid.NewGuid():N}");

    public ExcelHiddenRowTests() => Directory.CreateDirectory(_directory);

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    [Fact]
    public async Task RowsLeftOutByAnAutoFilter_AreReadAndReportedHidden()
    {
        var path = Path.Combine(_directory, "orders.xlsx");
        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.AddWorksheet("Orders");
            sheet.Cell(1, 1).Value = "Ma";
            sheet.Cell(1, 2).Value = "Ngay";
            for (var index = 0; index < 5; index++)
            {
                sheet.Cell(index + 2, 1).Value = "SP-" + (index + 1);
                sheet.Cell(index + 2, 2).Value = index % 2 == 0 ? "31/10" : "30/10";
            }

            sheet.Range(1, 1, 6, 2).SetAutoFilter().Column(2).EqualTo("31/10");
            workbook.SaveAs(path);
        }

        var table = await new ExcelDataService().LoadSheetAsync(path, "Orders");

        Assert.Equal(5, table.Rows.Count);
        Assert.Equal(new[] { 1, 3 }, ExcelDataService.HiddenRowIndexes(table));
    }

    [Fact]
    public async Task ASheetWithoutHiddenRows_ReportsNone()
    {
        var path = Path.Combine(_directory, "plain.xlsx");
        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.AddWorksheet("Data");
            sheet.Cell(1, 1).Value = "Ma";
            sheet.Cell(2, 1).Value = "SP-1";
            workbook.SaveAs(path);
        }

        var table = await new ExcelDataService().LoadSheetAsync(path, "Data");

        Assert.Empty(ExcelDataService.HiddenRowIndexes(table));
        Assert.Empty(ExcelDataService.HiddenRowIndexes(null));
    }
}
