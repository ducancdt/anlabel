using ANLAbel.Core.Enums;
using ANLAbel.Core.Models;
using ANLAbel.Data.Excel;
using ANLAbel.Data.PrintLogs;
using ANLAbel.Project.SaveLoad;
using ClosedXML.Excel;
using System.Printing;
using Xunit;

namespace ANLAbel.Tests;

public sealed class DataTests
{
    [Fact]
    public async Task TemplateSaveLoad_RoundTrip_PreservesAllFields()
    {
        Assert.Equal("Arial", new ObjectStyle().FontFamily);

        var service = new ProjectFileService();
        var directory = Path.Combine(Environment.CurrentDirectory, "TestOutput");
        Directory.CreateDirectory(directory);
        var filePath = Path.Combine(directory, $"template-{Guid.NewGuid():N}.anlabel");

        var template = new LabelTemplate
        {
            Name = "Unicode tiếng Việt",
            WidthMm = 80,
            HeightMm = 30,
            Dpi = 300,
            Orientation = LabelOrientation.Landscape
        };
        template.PrinterProfile.PaperName = "Roll100x50";
        template.PrinterProfile.SettingsSource = PrinterSettingsSource.Driver;
        template.PrinterProfile.PaperSizeSource = PaperSizeSource.Manual;
        template.PrinterProfile.MediaType = LabelMediaType.BlackMark;
        template.PrinterProfile.FeedDirection = FeedDirection.RightToLeft;
        template.PrinterProfile.GapMm = 3.5;
        template.PrinterProfile.Rotated180 = true;
        template.MarginMm = 2.25;
        template.DatabaseConfig.FilePath = @"C:\labels\data\production.xlsx";
        template.DatabaseConfig.SheetName = "SheetA";
        template.DatabaseConfig.LastSelectedRow = 7;
        template.Objects.Add(new LabelObject
        {
            Type = ObjectType.Text,
            BindingExpression = "{PartNo}",
            Text = "Mã hàng {PartNo}",
            XMm = 2.5,
            YMm = 3.5,
            WidthMm = 40,
            HeightMm = 8
        });
        template.Objects[0].HasBindingIssue = true;
        template.Objects[0].BindingStateDisplayText = "Missing: PartNo";

        await service.SaveAsync(template, filePath);
        var loaded = await service.LoadAsync(filePath);

        Assert.Equal("Unicode tiếng Việt", loaded.Name);
        Assert.Equal(80d, loaded.WidthMm);
        Assert.Equal(LabelOrientation.Landscape, loaded.Orientation);
        Assert.Equal("Roll100x50", loaded.PrinterProfile.PaperName);
        Assert.Equal(PrinterSettingsSource.Driver, loaded.PrinterProfile.SettingsSource);
        Assert.Equal(PaperSizeSource.Manual, loaded.PrinterProfile.PaperSizeSource);
        Assert.Equal(LabelMediaType.BlackMark, loaded.PrinterProfile.MediaType);
        Assert.Equal(FeedDirection.RightToLeft, loaded.PrinterProfile.FeedDirection);
        Assert.Equal(3.5d, loaded.PrinterProfile.GapMm);
        Assert.True(loaded.PrinterProfile.Rotated180);
        Assert.Equal(2.25d, loaded.MarginMm);
        Assert.Equal(@"C:\labels\data\production.xlsx", loaded.DatabaseConfig.FilePath);
        Assert.Equal("SheetA", loaded.DatabaseConfig.SheetName);
        Assert.Equal(7, loaded.DatabaseConfig.LastSelectedRow);
        Assert.Single(loaded.Objects);
        Assert.Equal("{PartNo}", loaded.Objects[0].BindingExpression);
        Assert.False(loaded.Objects[0].HasBindingIssue);
        Assert.Equal(string.Empty, loaded.Objects[0].BindingStateDisplayText);
        Assert.Equal("Mã hàng {PartNo}", loaded.Objects[0].Text);
    }

    [Fact]
    public async Task XlsxImport_ReadsSheetNamesAndRows()
    {
        var directory = Path.Combine(Environment.CurrentDirectory, "TestOutput");
        Directory.CreateDirectory(directory);
        var filePath = Path.Combine(directory, $"sample-{Guid.NewGuid():N}.xlsx");

        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.AddWorksheet("Data");
            sheet.Cell(1, 1).Value = "PartNo";
            sheet.Cell(1, 2).Value = "Qty";
            sheet.Cell(1, 3).Value = "Lot";
            sheet.Cell(2, 1).Value = "PN-001";
            sheet.Cell(2, 2).Value = "20";
            sheet.Cell(2, 3).Value = "Lô tiếng Việt";
            workbook.SaveAs(filePath);
        }

        var service = new ExcelDataService();
        var sheets = service.GetSheetNames(filePath);
        Assert.Equal("Data", sheets[0]);

        var table = await service.LoadSheetAsync(filePath, "Data");
        Assert.Equal(1, table.Rows.Count);
        Assert.Equal("PN-001", table.Rows[0]["PartNo"]);
        Assert.Equal("Lô tiếng Việt", table.Rows[0]["Lot"]);
    }

    [Fact]
    public async Task PrintLog_Append_CreatesExcelFile()
    {
        var directory = Path.Combine(Environment.CurrentDirectory, "TestOutput");
        Directory.CreateDirectory(directory);
        var service = new PrintLogService(Path.Combine(directory, $"print-history-{Guid.NewGuid():N}.xlsx"));
        await service.AppendAsync(new PrintLogEntry
        {
            TemplateName = "Test Template",
            TemplateFilePath = "test.anlabel",
            PrinterName = "Test Printer",
            LabelWidthMm = 100,
            LabelHeightMm = 50,
            Dpi = 300,
            PrintMode = "Unit test",
            RowCount = 1,
            LabelCount = 1,
            ExcelFilePath = "data.xlsx",
            ExcelSheetName = "Sheet1"
        });

        Assert.True(File.Exists(service.LogFilePath));
    }
}
