using ANLAbel.Data.Excel;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class ExcelDelimiterDetectionTests
{
    [Fact]
    public async Task SingleColumnHeaderKeepsCommaBearingDataAsOneField()
    {
        var path = Path.Combine(Path.GetTempPath(), $"anlabel-csv-{Guid.NewGuid():N}.csv");
        try
        {
            await File.WriteAllTextAsync(path, "Code\nModel A, rev2\nModel B\n");
            var table = await new ExcelDataService().LoadSheetAsync(path, ExcelDataService.CsvSheetName);

            Assert.Single(table.Columns);
            Assert.Equal("Code", table.Columns[0].ColumnName);
            Assert.Equal(2, table.Rows.Count);
            Assert.Equal("Model A, rev2", table.Rows[0]["Code"]);
            Assert.Equal("Model B", table.Rows[1]["Code"]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task DelimiterFreeHeaderWithCommasInMultipleDataRowsStaysSingleColumn()
    {
        // Regression F1: a genuinely single-column file whose header has no delimiter but
        // whose data values contain a comma in >=2 rows must import as ONE column with the
        // comma-bearing values preserved verbatim, not be misdetected as two columns.
        var path = Path.Combine(Path.GetTempPath(), $"anlabel-csv-{Guid.NewGuid():N}.csv");
        try
        {
            await File.WriteAllTextAsync(path, "Name\nSmith, John\nDoe, Jane\n");
            var table = await new ExcelDataService().LoadSheetAsync(path, ExcelDataService.CsvSheetName);

            Assert.Single(table.Columns);
            Assert.Equal("Name", table.Columns[0].ColumnName);
            Assert.Equal(2, table.Rows.Count);
            Assert.Equal("Smith, John", table.Rows[0]["Name"]);
            Assert.Equal("Doe, Jane", table.Rows[1]["Name"]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task MultiColumnCommaFileStillDetectsComma()
    {
        var path = Path.Combine(Path.GetTempPath(), $"anlabel-csv-{Guid.NewGuid():N}.csv");
        try
        {
            await File.WriteAllTextAsync(path, "Code,Lot\n000123,LOT-A\n000456,LOT-B\n");
            var table = await new ExcelDataService().LoadSheetAsync(path, ExcelDataService.CsvSheetName);

            Assert.Equal(2, table.Columns.Count);
            Assert.Equal(2, table.Rows.Count);
            Assert.Equal("000123", table.Rows[0]["Code"]);
            Assert.Equal("LOT-A", table.Rows[0]["Lot"]);
            Assert.Equal("000456", table.Rows[1]["Code"]);
            Assert.Equal("LOT-B", table.Rows[1]["Lot"]);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
