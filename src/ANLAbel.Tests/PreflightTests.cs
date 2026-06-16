using ANLAbel.App.ViewModels;
using ANLAbel.Core.Enums;
using ANLAbel.Core.Models;
using ANLAbel.Data.Excel;
using ANLAbel.Printing.RenderPipeline;
using ClosedXML.Excel;
using Xunit;

namespace ANLAbel.Tests;

public sealed class PreflightTests
{
    [UIFact]
    public void Preflight_ObjectOutsideLabel_BlocksPrint()
    {
        var template = new LabelTemplate { Name = "Outside Object Test", WidthMm = 30, HeightMm = 20, Dpi = 300 };
        template.Objects.Add(new LabelObject
        {
            Type = ObjectType.Rectangle,
            Name = "Frame Outside",
            XMm = 28, YMm = 2, WidthMm = 5, HeightMm = 5,
            Style = { OutlineStyle = OutlineStyle.Solid, BorderThicknessMm = 0.2 }
        });

        var result = new PrintService().ValidateRows(template, new IReadOnlyDictionary<string, string>?[] { null });
        Assert.False(result.IsSuccess);
        Assert.Contains("outside the design label", result.ToUserMessage(), StringComparison.OrdinalIgnoreCase);
    }

    [UIFact]
    public void Preflight_TextExceedsLabelWidth_BlocksPrint()
    {
        var template = new LabelTemplate { Name = "Outside Text Test", WidthMm = 30, HeightMm = 20, Dpi = 300 };
        template.Objects.Add(new LabelObject
        {
            Type = ObjectType.Text,
            Name = "Text Outside",
            BindingExpression = "{Name}",
            XMm = 20, YMm = 2, WidthMm = 8, HeightMm = 5,
            Style = { FontSizePt = 12 }
        });

        var rows = new IReadOnlyDictionary<string, string>?[]
        {
            new Dictionary<string, string> { ["Name"] = "LONG-LONG-LONG" }
        };
        var result = new PrintService().ValidateRows(template, rows);
        Assert.False(result.IsSuccess);
        Assert.Contains("Text extends outside", result.ToUserMessage(), StringComparison.OrdinalIgnoreCase);
    }

    [UIFact]
    public void Preflight_InvalidBarcodeAndOverflowTextBox_ReportsMultipleIssues()
    {
        var template = new LabelTemplate { Name = "Preflight Test", WidthMm = 50, HeightMm = 25, Dpi = 300 };
        template.Objects.Add(new LabelObject
        {
            Type = ObjectType.QRCode,
            Name = "QR Main",
            BindingExpression = "{Code}",
            XMm = 2, YMm = 2, WidthMm = 12, HeightMm = 12
        });
        template.Objects.Add(new LabelObject
        {
            Type = ObjectType.TextBox,
            Name = "Box Main",
            BindingExpression = "{Description}",
            XMm = 16, YMm = 2, WidthMm = 10, HeightMm = 4,
            Style = { FontSizePt = 12 }
        });

        var rows = new IReadOnlyDictionary<string, string>?[]
        {
            new Dictionary<string, string>
            {
                ["Code"] = string.Empty,
                ["Description"] = "This text is too long for the box"
            }
        };

        var result = new PrintService().ValidateRows(template, rows);
        Assert.False(result.IsSuccess);
        Assert.True(result.Issues.Count >= 2);
        Assert.Contains("Print blocked", result.ToUserMessage(), StringComparison.OrdinalIgnoreCase);
    }

    [UIFact]
    public async Task ExcelObjectBinding_BindCommand_SetsExpressionAndPreviewValue()
    {
        var directory = Path.Combine(Environment.CurrentDirectory, "TestOutput");
        Directory.CreateDirectory(directory);
        var filePath = Path.Combine(directory, $"binding-preview-{Guid.NewGuid():N}.xlsx");

        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.AddWorksheet("Data");
            sheet.Cell(1, 1).Value = "PartNo";
            sheet.Cell(1, 2).Value = "Qty";
            sheet.Cell(2, 1).Value = "PN-001";
            sheet.Cell(2, 2).Value = "20";
            workbook.SaveAs(filePath);
        }

        var viewModel = new MainViewModel();
        await viewModel.ImportExcelAsync(filePath, "Data");
        Assert.NotNull(viewModel.PreviewRow);
        Assert.Contains(viewModel.LabelDatabaseFields, f => f.Name == "PartNo");

        var item = new LabelObject
        {
            Type = ObjectType.Text, Name = "Part", Text = "Part",
            XMm = 1, YMm = 1, WidthMm = 20, HeightMm = 8
        };
        viewModel.Template.Objects.Add(item);
        viewModel.SelectedObject = item;
        viewModel.BindSelectedAsExcelFieldCommand.Execute("PartNo");

        Assert.Equal("{PartNo}", item.BindingExpression);
        Assert.Equal("PN-001", viewModel.SelectedBindingPreviewValue);
    }
}
