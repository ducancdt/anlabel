using ANLAbel.Data.Excel;
using ClosedXML.Excel;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class DataPayloadPreservationTests
{
    [Theory]
    [InlineData(".csv", " 000123 ")]
    [InlineData(".csv", "Lô A\r\nLô B")]
    [InlineData(".csv", "   ")]
    [InlineData(".xlsx", " 000123 ")]
    [InlineData(".xlsx", "Lô A\nLô B")]
    [InlineData(".xlsx", "   ")]
    public async Task ImportAndPreviewPreserveTextWhileNormalizingOnlyHeader(string extension, string payload)
    {
        var path = Path.Combine(Path.GetTempPath(), $"anlabel-payload-{Guid.NewGuid():N}{extension}");
        var sheetName = extension == ".csv" ? ExcelDataService.CsvSheetName : "Data";
        try
        {
            if (extension == ".csv")
            {
                await File.WriteAllTextAsync(path, " Code \n\"" + payload.Replace("\"", "\"\"") + "\"\n");
            }
            else
            {
                using var workbook = new XLWorkbook();
                var sheet = workbook.AddWorksheet(sheetName);
                sheet.Cell(1, 1).Value = " Code ";
                sheet.Cell(2, 1).Value = payload;
                workbook.SaveAs(path);
            }

            var service = new ExcelDataService();
            var preview = await service.PreviewRowsAsync(path, sheetName);
            Assert.Equal(payload, preview[1].Cells[0]);
            var table = await service.LoadSheetAsync(path, sheetName);
            Assert.Equal("Code", table.Columns[0].ColumnName);
            Assert.Single(table.Rows);
            Assert.Equal(payload, table.Rows[0]["Code"]);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
