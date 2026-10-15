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
    public async Task LoadSheet_RecordsWhereEachColumnCameFrom()
    {
        var filePath = CreateWorkbook(workbook =>
        {
            var sheet = workbook.AddWorksheet("Data");
            sheet.Cell(1, 1).Value = "Part No";
            sheet.Cell(1, 2).Value = "Lot";
            sheet.Cell(1, 3).Value = "";
            sheet.Cell(1, 4).Value = "part no";
            sheet.Cell(1, 5).Value = "Qty";
            sheet.Cell(2, 1).Value = "A";
            sheet.Cell(2, 3).Value = "C";
        });

        try
        {
            var table = await new ExcelDataService().LoadSheetAsync(filePath, "Data");
            AssertOrigins(table);
        }
        finally
        {
            DeleteWorkbook(filePath);
        }
    }

    // One stray cell far to the right (column XFD) used to make every empty
    // column before it a field: 16,384 fields, 1.4 GB and half a minute for
    // 1,000 rows in the designer before its data grid drew them all.
    [Fact]
    public async Task LoadSheet_ColumnsWithNeitherHeaderNorDataAreNotFields()
    {
        var filePath = CreateWorkbook(workbook =>
        {
            var sheet = workbook.AddWorksheet("Data");
            sheet.Cell(1, 1).Value = "Part";
            sheet.Cell(1, 2).Value = "Lot";
            sheet.Cell(1, 4).Value = "Qty";
            sheet.Cell(1, 16384).Value = "Note";
            sheet.Cell(2, 1).Value = "P1";
            sheet.Cell(2, 2).Value = "L1";
            sheet.Cell(2, 4).Value = "1";
            sheet.Cell(3, 1).Value = "P2";
            sheet.Cell(3, 5).Value = "late";
        });

        try
        {
            var table = await new ExcelDataService().LoadSheetAsync(filePath, "Data");
            var origins = DataColumnOrigin.Describe(table);

            // C has no header and no data; E has no header but a value in the second record.
            Assert.Equal(new[] { "A", "B", "D", "E", "XFD" }, origins.Select(origin => origin.ColumnLetter));
            Assert.Equal(new[] { "Part", "Lot", "Qty", "Column5", "Note" }, origins.Select(origin => origin.FieldName));
            Assert.Equal(DataHeaderKind.Blank, origins[3].Kind);
            Assert.Equal(string.Empty, table.Rows[0]["Column5"]);
            Assert.Equal("late", table.Rows[1]["Column5"]);
            Assert.Equal(string.Empty, table.Rows[1]["Qty"]);
        }
        finally
        {
            DeleteWorkbook(filePath);
        }
    }

    // The same sheet saved as CSV: every line carries the empty tail up to the stray cell.
    [Fact]
    public async Task LoadCsv_ColumnsWithNeitherHeaderNorDataAreNotFields()
    {
        var directory = CreateTemporaryDirectory();
        var filePath = Path.Combine(directory, "wide.csv");
        var tail = new string(',', 2000);
        await File.WriteAllTextAsync(filePath, $"Part,Lot,{tail}x\nP1,L1,{tail}\nP2,,{tail}\n");

        try
        {
            var table = await new ExcelDataService().LoadSheetAsync(filePath, ExcelDataService.CsvSheetName);
            var origins = DataColumnOrigin.Describe(table);

            Assert.Equal(new[] { "Part", "Lot", "x" }, origins.Select(origin => origin.FieldName));
            Assert.Equal(new[] { 1, 2, 2003 }, origins.Select(origin => origin.SourceColumn));
            Assert.Equal(2, table.Rows.Count);
            Assert.Equal(string.Empty, table.Rows[1]["Lot"]);
        }
        finally
        {
            DeleteWorkbook(filePath);
        }
    }

    [Fact]
    public async Task LoadCsv_RecordsWhereEachColumnCameFrom()
    {
        var directory = CreateTemporaryDirectory();
        var filePath = Path.Combine(directory, "origins.csv");
        await File.WriteAllTextAsync(filePath, "Part No,Lot,,part no,Qty\nA,,C,,\n");

        try
        {
            var table = await new ExcelDataService().LoadSheetAsync(filePath, ExcelDataService.CsvSheetName);
            AssertOrigins(table);
        }
        finally
        {
            DeleteWorkbook(filePath);
        }
    }

    [Theory]
    [InlineData(1, "A")]
    [InlineData(26, "Z")]
    [InlineData(27, "AA")]
    [InlineData(52, "AZ")]
    [InlineData(53, "BA")]
    [InlineData(702, "ZZ")]
    [InlineData(703, "AAA")]
    [InlineData(0, "")]
    public void ColumnLetters_FollowTheSpreadsheet(int column, string letter)
    {
        Assert.Equal(letter, DataColumnOrigin.ToColumnLetter(column));
    }

    private static void AssertOrigins(System.Data.DataTable table)
    {
        var origins = DataColumnOrigin.Describe(table);
        Assert.Equal(
            new[]
            {
                new DataColumnOrigin("Part No", 1, "Part No", DataHeaderKind.Repeated),
                new DataColumnOrigin("Lot", 2, "Lot", DataHeaderKind.Unique),
                new DataColumnOrigin("Column3", 3, "", DataHeaderKind.Blank),
                new DataColumnOrigin("part no_2", 4, "part no", DataHeaderKind.Repeated),
                new DataColumnOrigin("Qty", 5, "Qty", DataHeaderKind.Unique)
            },
            origins);
        Assert.Equal(new[] { "A", "B", "C", "D", "E" }, origins.Select(origin => origin.ColumnLetter));
        Assert.Equal(new[] { true, false, true, true, false }, origins.Select(origin => origin.DependsOnPosition));
        Assert.Equal("C", table.Rows[0]["Column3"]);
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

    // A read that ran past the timeout used to carry on in the background,
    // holding the file open (Excel cannot save over it) and doubling the work
    // when the operator tried again.
    [Fact]
    public async Task TimedOutRead_StopsAndReleasesTheFile()
    {
        var directory = CreateTemporaryDirectory();
        var filePath = Path.Combine(directory, "large.csv");
        using (var writer = new StreamWriter(filePath))
        {
            writer.WriteLine("PartNo,Qty,Lot");
            for (var row = 1; row <= 300_000; row++)
            {
                writer.WriteLine($"P-{row:D6},{row % 50 + 1},L{row:D7}");
            }
        }

        try
        {
            var service = new ExcelDataService(TimeSpan.FromMilliseconds(1));

            await Assert.ThrowsAsync<TimeoutException>(
                () => service.LoadSheetAsync(filePath, ExcelDataService.CsvSheetName));

            using var exclusive = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            Assert.True(exclusive.Length > 0);
        }
        finally
        {
            DeleteWorkbook(filePath);
        }
    }

    // The test above passes for a read that finishes within the grace period
    // anyway; this read only ends when it is told to stop.
    [Fact]
    public async Task TimedOutRead_IsToldToStop()
    {
        var service = new ExcelDataService(TimeSpan.FromMilliseconds(1));
        var stopped = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        await Assert.ThrowsAsync<TimeoutException>(() => service.ReadAsync<int>(
            "local.xlsx",
            token =>
            {
                stopped.TrySetResult(token.WaitHandle.WaitOne(TimeSpan.FromSeconds(10)));
                token.ThrowIfCancellationRequested();
                return 0;
            },
            CancellationToken.None));

        Assert.True(await stopped.Task, "The timed-out read was not asked to stop.");
    }

    // Cancel pressed while a timed-out read is still letting go of the file is a
    // cancel, not a timeout message.
    [Fact]
    public async Task CancelWhileATimedOutReadStops_IsACancel()
    {
        var service = new ExcelDataService(TimeSpan.FromMilliseconds(1));
        using var cancel = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.ReadAsync<int>(
            "local.xlsx",
            _ =>
            {
                // A step that cannot see the stop request, like the Excel reader's opening scan.
                Thread.Sleep(3000);
                return 0;
            },
            cancel.Token));
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
