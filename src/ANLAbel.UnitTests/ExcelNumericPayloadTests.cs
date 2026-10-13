using ANLAbel.Data.Excel;
using ClosedXML.Excel;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class ExcelNumericPayloadTests
{
    [Theory]
    [InlineData(0.00000000001, "General", "1E-11")]
    [InlineData(1.12345678901234, "General", "1.12345678901234")]
    [InlineData(123, "000000", "000123")]
    [InlineData(0, "000000", "000000")]
    [InlineData(1234567, "000000", "1234567")]
    [InlineData(12.3, "0.00", "12.30")]
    [InlineData(0.125, "0.00%", "12.50%")]
    [InlineData(12, "0\"%\"", "12%")]
    [InlineData(123, "\"PN-\"000000", "PN-000123")]
    public async Task NumericFormatPreservesAuthoredDisplayPayload(double value, string format, string expected)
    {
        var path = Path.Combine(Path.GetTempPath(), $"anlabel-number-{Guid.NewGuid():N}.xlsx");
        try
        {
            using (var workbook = new XLWorkbook())
            {
                var sheet = workbook.AddWorksheet("Data");
                sheet.Cell(1, 1).Value = "Code";
                sheet.Cell(2, 1).Value = value;
                sheet.Cell(2, 1).Style.NumberFormat.Format = format;
                workbook.SaveAs(path);
            }
            var service = new ExcelDataService();
            var table = await service.LoadSheetAsync(path, "Data");
            Assert.Equal(expected, table.Rows[0]["Code"]);
            var preview = await service.PreviewRowsAsync(path, "Data");
            Assert.Equal(expected, preview[1].Cells[0]);
        }
        finally { File.Delete(path); }
    }

}
