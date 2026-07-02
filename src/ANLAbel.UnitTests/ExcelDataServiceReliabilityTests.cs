using ANLAbel.Data.Excel;
using ClosedXML.Excel;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class ExcelDataServiceReliabilityTests
{
    [Fact]
    public async Task LoadSheet_MissingFile_ReturnsActionableError()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.xlsx");

        var error = await Assert.ThrowsAsync<ExcelDataReadException>(
            () => new ExcelDataService().LoadSheetAsync(filePath, "Data"));

        Assert.Equal(ExcelDataReadError.MissingFile, error.Error);
        Assert.Equal(filePath, error.FilePath);
        Assert.Contains("not found", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LoadSheet_RenamedSheet_ListsAvailableSheets()
    {
        var filePath = CreateWorkbook(workbook =>
        {
            var sheet = workbook.AddWorksheet("CurrentData");
            sheet.Cell(1, 1).Value = "PartNo";
            sheet.Cell(2, 1).Value = "PN-100";
        });

        try
        {
            var error = await Assert.ThrowsAsync<ExcelDataReadException>(
                () => new ExcelDataService().LoadSheetAsync(filePath, "OldData"));

            Assert.Equal(ExcelDataReadError.MissingSheet, error.Error);
            Assert.Equal("OldData", error.SheetName);
            Assert.Contains("CurrentData", error.Message, StringComparison.Ordinal);
        }
        finally
        {
            DeleteWorkbook(filePath);
        }
    }

    [Fact]
    public async Task GetSheetNames_CorruptWorkbook_ReturnsActionableError()
    {
        var directory = CreateTemporaryDirectory();
        var filePath = Path.Combine(directory, "corrupt.xlsx");
        File.WriteAllText(filePath, "this is not an Open XML workbook");

        try
        {
            var error = await Assert.ThrowsAsync<ExcelDataReadException>(
                () => new ExcelDataService().GetSheetNamesAsync(filePath));

            Assert.Equal(ExcelDataReadError.InvalidWorkbook, error.Error);
            Assert.Contains("damaged", error.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            DeleteWorkbook(filePath);
        }
    }

    [Fact]
    public async Task LoadSheet_DuplicateAndBlankHeaders_AreStableAndUnique()
    {
        var filePath = CreateWorkbook(workbook =>
        {
            var sheet = workbook.AddWorksheet("Data");
            sheet.Cell(1, 1).Value = " Part\r\nNo ";
            sheet.Cell(1, 2).Value = "part no";
            sheet.Cell(2, 1).Value = "PN-100";
            sheet.Cell(2, 2).Value = "PN-ALT";
            sheet.Cell(2, 3).Value = "blank-header-value";
        });

        try
        {
            var table = await new ExcelDataService().LoadSheetAsync(filePath, "Data");

            Assert.Equal(new[] { "Part No", "part no_2", "Column3" },
                table.Columns.Cast<System.Data.DataColumn>().Select(column => column.ColumnName).ToArray());
            Assert.Single(table.Rows);
            Assert.Equal("blank-header-value", table.Rows[0]["Column3"]);
        }
        finally
        {
            DeleteWorkbook(filePath);
        }
    }

    [Fact]
    public async Task LoadSheet_FileOpenWithReadWriteSharing_RemainsReadable()
    {
        var filePath = CreateWorkbook(workbook =>
        {
            var sheet = workbook.AddWorksheet("Data");
            sheet.Cell(1, 1).Value = "PartNo";
            sheet.Cell(2, 1).Value = "PN-OPEN";
        });

        try
        {
            using var heldByExcelLikeProcess = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite);

            var table = await new ExcelDataService().LoadSheetAsync(filePath, "Data");

            Assert.Equal("PN-OPEN", table.Rows[0]["PartNo"]);
        }
        finally
        {
            DeleteWorkbook(filePath);
        }
    }

    [Fact]
    public async Task LoadSheet_HeaderOutsideUsedRange_ReturnsActionableError()
    {
        var filePath = CreateWorkbook(workbook =>
        {
            var sheet = workbook.AddWorksheet("Data");
            sheet.Cell(1, 1).Value = "PartNo";
            sheet.Cell(2, 1).Value = "PN-100";
        });

        try
        {
            var error = await Assert.ThrowsAsync<ExcelDataReadException>(
                () => new ExcelDataService().LoadSheetAsync(filePath, "Data", headerRowIndex: 10));

            Assert.Equal(ExcelDataReadError.InvalidHeaderRow, error.Error);
            Assert.Contains("last row: 2", error.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            DeleteWorkbook(filePath);
        }
    }

    private static string CreateWorkbook(Action<XLWorkbook> configure)
    {
        var directory = CreateTemporaryDirectory();
        var filePath = Path.Combine(directory, "data.xlsx");
        using var workbook = new XLWorkbook();
        configure(workbook);
        workbook.SaveAs(filePath);
        return filePath;
    }

    private static string CreateTemporaryDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"anlabel-excel-reliability-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static void DeleteWorkbook(string filePath)
    {
        try
        {
            var directory = Path.GetDirectoryName(filePath);
            if (directory is not null)
            {
                Directory.Delete(directory, recursive: true);
            }
        }
        catch
        {
        }
    }
}
