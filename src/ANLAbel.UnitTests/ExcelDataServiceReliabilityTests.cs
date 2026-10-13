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
    public async Task LoadSheet_DuplicateHeaderDoesNotTakeALaterUniqueName()
    {
        var filePath = CreateWorkbook(workbook =>
        {
            var sheet = workbook.AddWorksheet("Data");
            sheet.Cell(1, 1).Value = "Part No";
            sheet.Cell(1, 2).Value = "Part No";
            sheet.Cell(1, 3).Value = "part no_2";
            sheet.Cell(1, 4).Value = "";
            sheet.Cell(1, 5).Value = "";
            sheet.Cell(1, 6).Value = "Column5";
            sheet.Cell(2, 1).Value = "A";
            sheet.Cell(2, 2).Value = "B";
            sheet.Cell(2, 3).Value = "C";
            sheet.Cell(2, 4).Value = "blank-4";
            sheet.Cell(2, 5).Value = "blank-5";
            sheet.Cell(2, 6).Value = "real-column5";
            sheet.Cell(3, 1).Value = "A";
            sheet.Cell(3, 2).Value = "B";
            sheet.Cell(3, 3).Value = "C";
            sheet.Cell(3, 4).Value = "blank-4";
            sheet.Cell(3, 5).Value = "blank-5";
            sheet.Cell(3, 6).Value = "real-column5";
        });

        try
        {
            var table = await new ExcelDataService().LoadSheetAsync(filePath, "Data");
            Assert.Equal(new[] { "Part No", "Part No_3", "part no_2", "Column4", "Column5_2", "Column5" },
                table.Columns.Cast<System.Data.DataColumn>().Select(column => column.ColumnName).ToArray());
            Assert.Equal(2, table.Rows.Count);
            Assert.Equal("C", table.Rows[0]["part no_2"]);
            Assert.Equal("B", table.Rows[0]["Part No_3"]);
            Assert.Equal("real-column5", table.Rows[0]["Column5"]);
            Assert.Equal("blank-5", table.Rows[0]["Column5_2"]);
            Assert.Equal("A", table.Rows[1]["Part No"]);
        }
        finally
        {
            DeleteWorkbook(filePath);
        }
    }

    [Fact]
    public async Task LoadCsv_DuplicateHeaderDoesNotTakeALaterUniqueName()
    {
        var directory = CreateTemporaryDirectory();
        var filePath = Path.Combine(directory, "headers.csv");
        await File.WriteAllTextAsync(
            filePath,
            "Part No,Part No,part no_2,,,Column5\nA,B,C,blank-4,blank-5,real-column5\nA,B,C,blank-4,blank-5,real-column5\n");

        try
        {
            var duplicate = await new ExcelDataService().LoadSheetAsync(filePath, ExcelDataService.CsvSheetName);
            Assert.Equal(new[] { "Part No", "Part No_3", "part no_2", "Column4", "Column5_2", "Column5" },
                duplicate.Columns.Cast<System.Data.DataColumn>().Select(column => column.ColumnName).ToArray());
            Assert.Equal(2, duplicate.Rows.Count);
            Assert.Equal("C", duplicate.Rows[0]["part no_2"]);
            Assert.Equal("B", duplicate.Rows[0]["Part No_3"]);
            Assert.Equal("real-column5", duplicate.Rows[0]["Column5"]);
            Assert.Equal("blank-5", duplicate.Rows[0]["Column5_2"]);
        }
        finally
        {
            DeleteWorkbook(filePath);
        }
    }

    [Fact]
    public async Task LoadSheet_RepeatedTargetNameStaysOnItsFirstColumn()
    {
        var filePath = CreateWorkbook(workbook =>
        {
            var sheet = workbook.AddWorksheet("Data");
            sheet.Cell(1, 1).Value = "Part No";
            sheet.Cell(1, 2).Value = "Part No";
            sheet.Cell(1, 3).Value = "part no_2";
            sheet.Cell(1, 4).Value = "part no_2";
            sheet.Cell(2, 1).Value = "A";
            sheet.Cell(2, 2).Value = "B";
            sheet.Cell(2, 3).Value = "C";
            sheet.Cell(2, 4).Value = "D";
        });

        try
        {
            var table = await new ExcelDataService().LoadSheetAsync(filePath, "Data");
            AssertRepeatedPartNoTarget(table);
        }
        finally
        {
            DeleteWorkbook(filePath);
        }
    }

    [Fact]
    public async Task LoadCsv_RepeatedTargetNameStaysOnItsFirstColumn()
    {
        var directory = CreateTemporaryDirectory();
        var filePath = Path.Combine(directory, "repeated-target.csv");
        await File.WriteAllTextAsync(filePath, "Part No,Part No,part no_2,part no_2\nA,B,C,D\n");

        try
        {
            var table = await new ExcelDataService().LoadSheetAsync(filePath, ExcelDataService.CsvSheetName);
            AssertRepeatedPartNoTarget(table);
        }
        finally
        {
            DeleteWorkbook(filePath);
        }
    }

    private static void AssertRepeatedPartNoTarget(System.Data.DataTable table)
    {
        Assert.Equal(new[] { "Part No", "Part No_3", "part no_2", "part no_2_2" },
            table.Columns.Cast<System.Data.DataColumn>().Select(column => column.ColumnName).ToArray());
        Assert.Equal("C", table.Rows[0]["part no_2"]);
        Assert.Equal("A", table.Rows[0]["Part No"]);
        Assert.Equal("B", table.Rows[0]["Part No_3"]);
        Assert.Equal("D", table.Rows[0]["part no_2_2"]);
        Assert.NotEqual("B", table.Rows[0]["part no_2"]);
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
