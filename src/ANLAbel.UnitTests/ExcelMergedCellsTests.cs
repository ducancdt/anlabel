using System.Data;
using ANLAbel.Data.Excel;
using ClosedXML.Excel;
using Xunit;

namespace ANLAbel.UnitTests;

/// <summary>
/// Merged cells keep their value in the first cell only. The reader reports
/// each merged range over data cells with the rows that read as empty, so the
/// operator is told before labels print without the value.
/// </summary>
public sealed class ExcelMergedCellsTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"anl-merged-{Guid.NewGuid():N}");

    public ExcelMergedCellsTests() => Directory.CreateDirectory(_directory);

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    [Fact]
    public async Task MergedRanges_AreReportedWithTheRowsThatReadEmpty()
    {
        var path = Path.Combine(_directory, "lots.xlsx");
        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.AddWorksheet("Lots");
            sheet.Cell(1, 1).Value = "Ma";
            sheet.Cell(1, 2).Value = "Lot";
            sheet.Cell(1, 3).Value = "Ca";
            sheet.Cell(1, 4).Value = "Ghi chu";
            for (var index = 0; index < 5; index++)
            {
                sheet.Cell(index + 2, 1).Value = "SP-" + (index + 1);
            }

            // A lot merged over three rows, and a shift merged into the note column.
            sheet.Cell(2, 2).Value = "L2610";
            sheet.Range(2, 2, 4, 2).Merge();
            sheet.Cell(5, 2).Value = "L2611";
            sheet.Cell(6, 2).Value = "L2612";
            sheet.Cell(6, 3).Value = "Ca 1";
            sheet.Range(6, 3, 6, 4).Merge();
            workbook.SaveAs(path);
        }

        var table = await new ExcelDataService().LoadSheetAsync(path, "Lots");

        Assert.Equal(new[] { "L2610", "", "", "L2611", "L2612" }, table.Rows.Cast<DataRow>().Select(row => (string)row["Lot"]));
        Assert.Collection(
            ExcelDataService.MergedCells(table),
            lot =>
            {
                Assert.Equal("B2:B4", lot.Reference);
                Assert.Equal(new[] { "Lot" }, lot.Fields);
                Assert.Equal(new[] { 1, 2 }, lot.EmptyRowIndexes);
            },
            shift =>
            {
                Assert.Equal("C6:D6", shift.Reference);
                Assert.Equal(new[] { "Ca", "Ghi chu" }, shift.Fields);
                Assert.Equal(new[] { 4 }, shift.EmptyRowIndexes);
            });
    }

    [Fact]
    public async Task ATitleMergedAboveTheHeader_IsNotReported()
    {
        var path = Path.Combine(_directory, "title.xlsx");
        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.AddWorksheet("Data");
            sheet.Cell(1, 1).Value = "Danh sách hàng";
            sheet.Range(1, 1, 1, 3).Merge();
            sheet.Cell(2, 1).Value = "Ma";
            sheet.Cell(2, 2).Value = "Lot";
            sheet.Cell(3, 1).Value = "SP-1";
            sheet.Cell(3, 2).Value = "L1";
            workbook.SaveAs(path);
        }

        var table = await new ExcelDataService().LoadSheetAsync(path, "Data", headerRowIndex: 2);

        Assert.Single(table.Rows.Cast<DataRow>());
        Assert.Empty(ExcelDataService.MergedCells(table));
        Assert.Empty(ExcelDataService.MergedCells(null));
    }
}
