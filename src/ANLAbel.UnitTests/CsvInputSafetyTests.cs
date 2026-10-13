using ANLAbel.Data.Excel;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class CsvInputSafetyTests
{
    [Fact]
    public async Task SelectedHeaderPastEndReportsInvalidHeader()
    {
        var path = Path.Combine(Path.GetTempPath(), $"anlabel-csv-{Guid.NewGuid():N}.csv");
        try
        {
            await File.WriteAllTextAsync(path, "Report\n");
            var error = await Assert.ThrowsAsync<ExcelDataReadException>(() =>
                new ExcelDataService().LoadSheetAsync(path, ExcelDataService.CsvSheetName, 3));
            Assert.Equal(ExcelDataReadError.InvalidHeaderRow, error.Error);
            Assert.Contains("last row: 1", error.Message);
        }
        finally { File.Delete(path); }
    }

    [Theory]
    [InlineData("Report\n", ";", 2)]
    [InlineData("Report, generated, today\n", ";", 2)]
    [InlineData("Report\n\n", "\t", 3)]
    [InlineData("\"Report\ncontinued\"\n", ";", 2)]
    public async Task DelimiterComesFromSelectedLogicalHeader(string preamble, string delimiter, int headerRow)
    {
        var path = Path.Combine(Path.GetTempPath(), $"anlabel-csv-{Guid.NewGuid():N}.csv");
        try
        {
            await File.WriteAllTextAsync(path, preamble + "Code" + delimiter + "Lot\n000123" + delimiter + "Lô A\n");
            var table = await new ExcelDataService().LoadSheetAsync(path, ExcelDataService.CsvSheetName, headerRow);
            Assert.Equal(2, table.Columns.Count);
            Assert.Single(table.Rows);
            Assert.Equal("000123", table.Rows[0]["Code"]);
            Assert.Equal("Lô A", table.Rows[0]["Lot"]);
        }
        finally { File.Delete(path); }
    }

    [Theory]
    [InlineData("Code,Lot\nA,B,C\n")]
    [InlineData("Code,Lot\nA,B,\n")]
    [InlineData("Code\nAB\"CD\"\n")]
    [InlineData("Code\n\"AB\"CD\n")]
    public async Task ImportRejectsMalformedRecordsInsteadOfChangingPayload(string csv)
    {
        var path = Path.Combine(Path.GetTempPath(), $"anlabel-csv-{Guid.NewGuid():N}.csv");
        try
        {
            await File.WriteAllTextAsync(path, csv);
            var error = await Assert.ThrowsAsync<ExcelDataReadException>(() =>
                new ExcelDataService().LoadSheetAsync(path, ExcelDataService.CsvSheetName));
            Assert.Equal(ExcelDataReadError.InvalidData, error.Error);
            Assert.Equal(path, error.FilePath);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Theory]
    [InlineData("\"A\"\"B\",C", "A\"B", "C")]
    [InlineData("\"A,B\",\"\"", "A,B", "")]
    [InlineData("A,", "A", "")]
    public async Task ImportPreservesValidQuotedAndEmptyFields(string record, string code, string lot)
    {
        var path = Path.Combine(Path.GetTempPath(), $"anlabel-csv-{Guid.NewGuid():N}.csv");
        try
        {
            await File.WriteAllTextAsync(path, "Code,Lot\n" + record);
            var table = await new ExcelDataService().LoadSheetAsync(path, ExcelDataService.CsvSheetName);
            Assert.Single(table.Rows);
            Assert.Equal(code, table.Rows[0]["Code"]);
            Assert.Equal(lot, table.Rows[0]["Lot"]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task PreviewRowsDetectsDelimiterWhenFirstRowIsUndelimitedPreamble()
    {
        var path = Path.Combine(Path.GetTempPath(), $"anlabel-csv-{Guid.NewGuid():N}.csv");
        try
        {
            await File.WriteAllTextAsync(path, "Inventory Report 2026\nPart;Qty;Batch\n000123;5;LOT-A\n");
            var rows = await new ExcelDataService().PreviewRowsAsync(path, ExcelDataService.CsvSheetName, 10);

            Assert.Equal(3, rows.Count);
            Assert.Single(rows[0].Cells);
            Assert.Equal("Inventory Report 2026", rows[0].Cells[0]);
            Assert.Equal(new[] { "Part", "Qty", "Batch" }, rows[1].Cells);
            Assert.Equal(new[] { "000123", "5", "LOT-A" }, rows[2].Cells);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task PreviewRowsDetectsTabDelimiterWhenFirstRowIsUndelimitedPreamble()
    {
        var path = Path.Combine(Path.GetTempPath(), $"anlabel-csv-{Guid.NewGuid():N}.csv");
        try
        {
            await File.WriteAllTextAsync(path, "Tab Report Title\nPart\tQty\tBatch\n000456\t10\tLOT-B\n");
            var rows = await new ExcelDataService().PreviewRowsAsync(path, ExcelDataService.CsvSheetName, 10);

            Assert.Equal(3, rows.Count);
            Assert.Single(rows[0].Cells);
            Assert.Equal("Tab Report Title", rows[0].Cells[0]);
            Assert.Equal(new[] { "Part", "Qty", "Batch" }, rows[1].Cells);
            Assert.Equal(new[] { "000456", "10", "LOT-B" }, rows[2].Cells);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
