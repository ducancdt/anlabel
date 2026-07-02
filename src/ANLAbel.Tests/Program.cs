using ANLAbel.Barcode.Options;
using ANLAbel.Barcode.Renderers;
using ANLAbel.Core.Enums;
using ANLAbel.Core.Expressions;
using ANLAbel.Core.Expressions.Formulas;
using ANLAbel.Core.Geometry;
using ANLAbel.Core.Models;
using ANLAbel.Data.Excel;
using ANLAbel.Data.PrintLogs;
using ANLAbel.App.ViewModels;
using ANLAbel.Project.SaveLoad;
using ANLAbel.Printing.PrinterProfiles;
using ANLAbel.Printing.RenderPipeline;
using ClosedXML.Excel;
using System.Printing;
using System.Windows.Media;
using System.Windows.Media.Imaging;

var tests = new (string Name, Func<Task> Run)[]
{
    ("mm to WPF DIP conversion", TestMmToDip),
    ("mm to printer dots conversion", TestMmToPrinterDots),
    ("label orientation follows design dimensions", TestLabelOrientationFollowsDesignDimensions),
    ("binding expression evaluation", TestBindingExpression),
    ("formula engine FIELD and CONCAT", TestFormulaEngine),
    ("template save/load", TestTemplateSaveLoad),
    ("xlsx import", TestXlsxImport),
    ("excel object binding preview", TestExcelObjectBindingPreview),
    ("barcode render validation", TestBarcodeRender),
    ("print visual render", TestPrintVisualRender),
    ("print preview follows design label size", TestPrintPreviewUsesDesignLabelSize),
    ("print renderer keeps edge content", TestPrintRendererKeepsEdgeContent),
    ("print barcode uses object dpi", TestPrintBarcodeUsesObjectDpi),
    ("print preflight blocks object outside label", TestPrintPreflightBlocksObjectOutsideLabel),
    ("print preflight blocks text outside label", TestPrintPreflightBlocksTextOutsideLabel),
    ("print preflight validation", TestPrintPreflightValidation),
    ("print log excel append", TestPrintLogAppend),
    ("template library standalone (no sample-data link)", TestTemplateLibraryStandalone)
};

var failed = 0;
foreach (var test in tests)
{
    try
    {
        await test.Run();
        Console.WriteLine($"PASS {test.Name}");
    }
    catch (Exception ex)
    {
        failed++;
        Console.WriteLine($"FAIL {test.Name}: {ex.Message}");
    }
}

return failed == 0 ? 0 : 1;

static Task TestMmToDip()
{
    AssertNear(96, MmConverter.MmToDip(25.4), 0.001, "25.4 mm must equal 96 WPF DIP");
    AssertNear(25.4, MmConverter.DipToMm(96), 0.001, "96 WPF DIP must equal 25.4 mm");
    return Task.CompletedTask;
}

static Task TestMmToPrinterDots()
{
    AssertEqual(203, MmConverter.MmToPrinterDots(25.4, 203), "25.4 mm at 203 DPI must equal 203 dots");
    AssertEqual(300, MmConverter.MmToPrinterDots(25.4, 300), "25.4 mm at 300 DPI must equal 300 dots");
    return Task.CompletedTask;
}

static Task TestLabelOrientationFollowsDesignDimensions()
{
    var landscapeSize = LabelGeometry.OrientSize(50, 100, LabelOrientation.Landscape);
    var portraitSize = LabelGeometry.OrientSize(100, 50, LabelOrientation.Portrait);
    AssertEqual(100d, landscapeSize.WidthMm, "Landscape paper size must put the long side horizontally");
    AssertEqual(50d, landscapeSize.HeightMm, "Landscape paper size must put the short side vertically");
    AssertEqual(50d, portraitSize.WidthMm, "Portrait paper size must put the short side horizontally");
    AssertEqual(100d, portraitSize.HeightMm, "Portrait paper size must put the long side vertically");

    return Task.CompletedTask;
}

static Task TestBindingExpression()
{
    var row = new Dictionary<string, string>
    {
        ["PartNo"] = "PN-001",
        ["Qty"] = "20",
        ["Lot"] = "L01",
        ["Part No"] = "PN-SPACE"
    };

    var value = BindingExpressionEvaluator.Evaluate("P{PartNo} Q{Qty} 1T{Lot}", row);
    AssertEqual("PPN-001 Q20 1TL01", value, "Expression should replace fields");
    AssertEqual(3, BindingExpressionEvaluator.GetFields("P{PartNo} Q{Qty} 1T{Lot}").Count, "Expression should expose fields");
    var normalizedValue = BindingExpressionEvaluator.Evaluate("Normalized {Part_No}", row);
    AssertEqual("Normalized PN-SPACE", normalizedValue, "Expression should resolve normalized field names with spaces and separators");
    return Task.CompletedTask;
}

static Task TestFormulaEngine()
{
    var engine = new FormulaEngine();
    var row = new Dictionary<string, string?>
    {
        ["Name"] = " Product A ",
        ["Code"] = "ABC123",
        ["Empty"] = null,
        ["Lot No"] = "LOT-77"
    };

    var result = engine.Evaluate("CONCAT(\"Tên: \", FIELD(\"Name\"), \", Mã: \", FIELD(\"code\"))", row);
    AssertEqual("Tên: Product A, Mã: ABC123", result.Value, "CONCAT should combine literals and case-insensitive FIELD values");
    AssertEqual(0, result.Errors.Count, "Valid formula should not report errors");
    AssertEqual(2, result.UsedFields.Count, "Formula should report used fields");
    AssertEqual("Name", result.UsedFields[0], "Formula should preserve requested field names");
    AssertEqual("code", result.UsedFields[1], "Formula should preserve requested field names case");

    var nested = engine.Evaluate("CONCAT(CONCAT(\"Mã hàng: \", FIELD(\"Code\")), \" - \", FIELD(\"Empty\"))", row);
    AssertEqual("Mã hàng: ABC123 - ", nested.Value, "Nested CONCAT and null FIELD values should evaluate correctly");
    AssertEqual(0, nested.Errors.Count, "Null field values should not be errors");

    var normalizedField = engine.Evaluate("CONCAT(\"Lot: \", FIELD(\"LotNo\"))", row);
    AssertEqual("Lot: LOT-77", normalizedField.Value, "FIELD should resolve normalized field names");
    AssertEqual(0, normalizedField.Errors.Count, "Normalized field match should not be an error");

    var missing = engine.Evaluate("CONCAT(\"Missing: \", FIELD(\"Unknown\"))", row);
    AssertEqual("Missing: ", missing.Value, "Missing fields should evaluate to empty string");
    AssertEqual(1, missing.Errors.Count, "Missing fields should report one error");
    AssertEqual("Unknown", missing.UsedFields[0], "Missing fields should still be included in used fields");

    var parseError = engine.Evaluate("CONCAT(\"A\", FIELD(\"Code\")", row);
    AssertEqual(string.Empty, parseError.Value, "Parse errors should return an empty value");
    AssertEqual(true, parseError.Errors.Count > 0, "Parse errors should be reported");
    return Task.CompletedTask;
}

static async Task TestTemplateSaveLoad()
{
    AssertEqual("Arial", new ObjectStyle().FontFamily, "New objects should default to Arial font");

    var service = new ProjectFileService();
    var directory = Path.Combine(Environment.CurrentDirectory, "TestOutput");
    Directory.CreateDirectory(directory);
    var filePath = Path.Combine(directory, "template.anlabel");

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

    AssertEqual("Unicode tiếng Việt", loaded.Name, "Template name must survive JSON round trip");
    AssertEqual(80d, loaded.WidthMm, "Width must survive JSON round trip");
    AssertEqual(LabelOrientation.Landscape, loaded.Orientation, "Orientation must survive JSON round trip");
    AssertEqual("Roll100x50", loaded.PrinterProfile.PaperName, "Printer paper name must survive JSON round trip");
    AssertEqual(PrinterSettingsSource.Driver, loaded.PrinterProfile.SettingsSource, "Printer settings source must survive JSON round trip");
    AssertEqual(PaperSizeSource.Manual, loaded.PrinterProfile.PaperSizeSource, "Paper size source must survive JSON round trip");
    AssertEqual(LabelMediaType.BlackMark, loaded.PrinterProfile.MediaType, "Media type must survive JSON round trip");
    AssertEqual(FeedDirection.RightToLeft, loaded.PrinterProfile.FeedDirection, "Feed direction must survive JSON round trip");
    AssertEqual(3.5d, loaded.PrinterProfile.GapMm, "Gap must survive JSON round trip");
    AssertEqual(true, loaded.PrinterProfile.Rotated180, "Rotate 180 must survive JSON round trip");
    AssertEqual(2.25d, loaded.MarginMm, "Printable margin must survive JSON round trip");
    AssertEqual(@"C:\labels\data\production.xlsx", loaded.DatabaseConfig.FilePath, "Linked Excel file path must survive JSON round trip");
    AssertEqual("SheetA", loaded.DatabaseConfig.SheetName, "Linked Excel sheet name must survive JSON round trip");
    AssertEqual(7, loaded.DatabaseConfig.LastSelectedRow, "Last selected Excel row must survive JSON round trip");
    AssertEqual(1, loaded.Objects.Count, "Objects must survive JSON round trip");
    AssertEqual("{PartNo}", loaded.Objects[0].BindingExpression, "Binding expression must survive JSON round trip");
    AssertEqual(false, loaded.Objects[0].HasBindingIssue, "Designer-only binding issue state must not be persisted");
    AssertEqual(string.Empty, loaded.Objects[0].BindingStateDisplayText, "Designer-only binding status text must not be persisted");
    AssertEqual("Mã hàng {PartNo}", loaded.Objects[0].Text, "Unicode object text must survive JSON round trip");
}

static async Task TestXlsxImport()
{
    var directory = Path.Combine(Environment.CurrentDirectory, "TestOutput");
    Directory.CreateDirectory(directory);
    var filePath = Path.Combine(directory, "sample.xlsx");

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
    AssertEqual("Data", sheets[0], "Excel service should list sheet names");

    var table = await service.LoadSheetAsync(filePath, "Data");
    AssertEqual(1, table.Rows.Count, "Excel service should read one data row");
    AssertEqual("PN-001", table.Rows[0]["PartNo"], "Excel service should read PartNo");
    AssertEqual("Lô tiếng Việt", table.Rows[0]["Lot"], "Excel service should keep Unicode text");
}

static async Task TestExcelObjectBindingPreview()
{
    var directory = Path.Combine(Environment.CurrentDirectory, "TestOutput");
    Directory.CreateDirectory(directory);
    var filePath = Path.Combine(directory, "binding-preview.xlsx");

    using (var workbook = new XLWorkbook())
    {
        var sheet = workbook.AddWorksheet("Data");
        sheet.Cell(1, 1).Value = "PartNo";
        sheet.Cell(1, 2).Value = "Qty";
        sheet.Cell(2, 1).Value = "PN-001";
        sheet.Cell(2, 2).Value = "20";
        sheet.Cell(3, 1).Value = "PN-002";
        sheet.Cell(3, 2).Value = "40";
        workbook.SaveAs(filePath);
    }

    var viewModel = new MainViewModel();
    await viewModel.ImportExcelAsync(filePath, "Data");
    AssertEqual(true, viewModel.PreviewRow is not null, "Import should select the first Excel row for live preview");
    AssertEqual(true, viewModel.LabelDatabaseFields.Any(field => field.Name == "PartNo"), "First import should expose Excel fields for object binding");

    var item = new LabelObject
    {
        Type = ObjectType.Text,
        Name = "Part",
        Text = "Part",
        XMm = 1,
        YMm = 1,
        WidthMm = 20,
        HeightMm = 8
    };
    viewModel.Template.Objects.Add(item);
    viewModel.SelectedObject = item;
    viewModel.BindSelectedAsExcelFieldCommand.Execute("PartNo");

    AssertEqual("{PartNo}", item.BindingExpression, "Binding command should write the selected Excel field to the object");
    AssertEqual("PN-001", viewModel.SelectedBindingPreviewValue, "Bound object preview should show data from the selected Excel row");
}

static Task TestBarcodeRender()
{
    var renderer = new ZxingBarcodeRenderer();
    AssertEqual(true, renderer.ValidateData("ABC123", BarcodeType.Code128), "Code 128 should accept ASCII data");
    AssertEqual(false, renderer.ValidateData(string.Empty, BarcodeType.QRCode), "QR should reject empty data");

    var code128 = renderer.RenderBarcode("ABC123", BarcodeType.Code128, 40, 12, 300);
    var qr = renderer.RenderBarcode("Tiếng Việt", BarcodeType.QRCode, 20, 20, 300);
    var dm = renderer.RenderBarcode("PN-001", BarcodeType.DataMatrix, 18, 18, 300);
    var ean13 = renderer.RenderBarcode("893850597419", BarcodeType.Ean13, 35, 12, 300);
    var pdf417 = renderer.RenderBarcode("PN-001 LOT-01", BarcodeType.Pdf417, 42, 16, 300);

    AssertEqual(true, code128.WidthPixels > 100, "Code 128 should render enough pixels at 300 DPI");
    AssertEqual(true, qr.WidthPixels == qr.HeightPixels, "QR should render square image");
    AssertEqual(true, dm.WidthPixels == dm.HeightPixels, "Data Matrix should render square image");
    AssertEqual(true, ean13.WidthPixels > 100, "EAN-13 should render enough pixels at 300 DPI");
    AssertEqual(true, pdf417.WidthPixels > 100, "PDF417 should render enough pixels at 300 DPI");
    return Task.CompletedTask;
}

static Task TestPrintVisualRender()
{
    var template = new LabelTemplate
    {
        Name = "Print Test",
        WidthMm = 60,
        HeightMm = 30,
        Dpi = 300
    };
    template.Objects.Add(new LabelObject
    {
        Type = ObjectType.Text,
        Text = "PN {PartNo}",
        BindingExpression = "PN {PartNo}",
        XMm = 2,
        YMm = 2,
        WidthMm = 30,
        HeightMm = 8
    });
    template.Objects.Add(new LabelObject
    {
        Type = ObjectType.QRCode,
        BindingExpression = "{PartNo}",
        XMm = 35,
        YMm = 2,
        WidthMm = 18,
        HeightMm = 18
    });

    var renderer = new LabelVisualRenderer();
    var row = new Dictionary<string, string> { ["PartNo"] = "PN-001" };
    var plan = new PrintRenderPlan { Dpi = 300, LabelWidthMm = 60, LabelHeightMm = 30 };
    var visual = renderer.Render(template, row, plan);
    var calibration = renderer.RenderCalibration(plan);

    AssertEqual(true, visual is not null, "Print renderer should create label visual");
    AssertEqual(true, calibration is not null, "Print renderer should create calibration visual");
    return Task.CompletedTask;
}

static Task TestPrintPreviewUsesDesignLabelSize()
{
    var template = new LabelTemplate
    {
        Name = "Design Size Test",
        WidthMm = 60,
        HeightMm = 30,
        Dpi = 300,
        PrinterProfile =
        {
            LabelWidthMm = 100,
            LabelHeightMm = 50
        }
    };

    var printService = new PrintService();
    var page = printService.CreatePreviewPages(template, new IReadOnlyDictionary<string, string>?[] { null }).Single();

    AssertNear(MmConverter.MmToDip(60), page.WidthDip, 0.001, "Print preview width must follow template design width, not stale printer profile width");
    AssertNear(MmConverter.MmToDip(30), page.HeightDip, 0.001, "Print preview height must follow template design height, not stale printer profile height");
    return Task.CompletedTask;
}

static Task TestPrintRendererKeepsEdgeContent()
{
    var template = new LabelTemplate
    {
        Name = "Edge Content Test",
        WidthMm = 30,
        HeightMm = 20,
        MarginMm = 5,
        Dpi = 300
    };
    template.Objects.Add(new LabelObject
    {
        Type = ObjectType.Rectangle,
        XMm = 0,
        YMm = 0,
        WidthMm = 5,
        HeightMm = 5,
        Style =
        {
            FillStyle = FillStyle.Solid,
            FillColor = "#000000",
            OutlineStyle = OutlineStyle.None
        }
    });

    var renderer = new LabelVisualRenderer();
    var plan = new PrintRenderPlan { Dpi = 300, LabelWidthMm = 30, LabelHeightMm = 20, MarginMm = 5 };
    var visual = renderer.Render(template, null, plan);
    var bitmap = RenderTestBitmap(visual, MmConverter.MmToDip(30), MmConverter.MmToDip(20));
    var pixels = new byte[bitmap.PixelWidth * bitmap.PixelHeight * 4];
    bitmap.CopyPixels(pixels, bitmap.PixelWidth * 4, 0);

    var hasBlackNearOrigin = false;
    var scanWidth = Math.Min(bitmap.PixelWidth, 20);
    var scanHeight = Math.Min(bitmap.PixelHeight, 20);
    for (var y = 0; y < scanHeight && !hasBlackNearOrigin; y++)
    {
        for (var x = 0; x < scanWidth; x++)
        {
            var index = (y * bitmap.PixelWidth + x) * 4;
            if (pixels[index] < 20 && pixels[index + 1] < 20 && pixels[index + 2] < 20 && pixels[index + 3] > 200)
            {
                hasBlackNearOrigin = true;
                break;
            }
        }
    }

    AssertEqual(true, hasBlackNearOrigin, "Print renderer must not clip design content at label edge because of printable margin");
    return Task.CompletedTask;
}

static Task TestPrintBarcodeUsesObjectDpi()
{
    var template = new LabelTemplate
    {
        Name = "Barcode DPI Test",
        WidthMm = 40,
        HeightMm = 25,
        Dpi = 203
    };
    template.Objects.Add(new LabelObject
    {
        Type = ObjectType.QRCode,
        Text = "DPI-CHECK",
        XMm = 2,
        YMm = 2,
        WidthMm = 12,
        HeightMm = 12,
        QrDpi = 300
    });

    var fakeRenderer = new CapturingBarcodeRenderer();
    var renderer = new LabelVisualRenderer(fakeRenderer);
    var plan = new PrintRenderPlan { Dpi = 203, LabelWidthMm = 40, LabelHeightMm = 25 };
    renderer.Render(template, null, plan);

    AssertEqual(300, fakeRenderer.LastDpi, "Print barcode renderer must use object QR/barcode DPI so preview and print share the same barcode sizing rule");
    return Task.CompletedTask;
}

static Task TestPrintPreflightBlocksObjectOutsideLabel()
{
    var template = new LabelTemplate
    {
        Name = "Outside Object Test",
        WidthMm = 30,
        HeightMm = 20,
        Dpi = 300
    };
    template.Objects.Add(new LabelObject
    {
        Type = ObjectType.Rectangle,
        Name = "Frame Outside",
        XMm = 28,
        YMm = 2,
        WidthMm = 5,
        HeightMm = 5,
        Style =
        {
            OutlineStyle = OutlineStyle.Solid,
            BorderThicknessMm = 0.2
        }
    });

    var preflight = new PrintService().ValidateRows(template, new IReadOnlyDictionary<string, string>?[] { null });
    AssertEqual(false, preflight.IsSuccess, "Preflight must block visible objects that extend outside the design label because print will clip them");
    AssertEqual(true, preflight.ToUserMessage().Contains("outside the design label", StringComparison.OrdinalIgnoreCase), "Preflight should explain object is outside label bounds");
    return Task.CompletedTask;
}

static Task TestPrintPreflightBlocksTextOutsideLabel()
{
    var template = new LabelTemplate
    {
        Name = "Outside Text Test",
        WidthMm = 30,
        HeightMm = 20,
        Dpi = 300
    };
    template.Objects.Add(new LabelObject
    {
        Type = ObjectType.Text,
        Name = "Text Outside",
        BindingExpression = "{Name}",
        XMm = 20,
        YMm = 2,
        WidthMm = 8,
        HeightMm = 5,
        Style = { FontSizePt = 12 }
    });

    var rows = new IReadOnlyDictionary<string, string>?[]
    {
        new Dictionary<string, string> { ["Name"] = "LONG-LONG-LONG" }
    };
    var preflight = new PrintService().ValidateRows(template, rows);
    AssertEqual(false, preflight.IsSuccess, "Preflight must block resolved text that would be visible beyond the label on screen but clipped in print");
    AssertEqual(true, preflight.ToUserMessage().Contains("Text extends outside", StringComparison.OrdinalIgnoreCase), "Preflight should explain text exceeds label bounds");
    return Task.CompletedTask;
}

static RenderTargetBitmap RenderTestBitmap(Visual visual, double widthDip, double heightDip)
{
    var pixelWidth = Math.Max(1, (int)Math.Ceiling(widthDip));
    var pixelHeight = Math.Max(1, (int)Math.Ceiling(heightDip));
    var bitmap = new RenderTargetBitmap(pixelWidth, pixelHeight, 96, 96, PixelFormats.Pbgra32);
    bitmap.Render(visual);
    return bitmap;
}

static Task TestPrintPreflightValidation()
{
    var template = new LabelTemplate
    {
        Name = "Preflight Test",
        WidthMm = 50,
        HeightMm = 25,
        Dpi = 300
    };
    template.Objects.Add(new LabelObject
    {
        Type = ObjectType.QRCode,
        Name = "QR Main",
        BindingExpression = "{Code}",
        XMm = 2,
        YMm = 2,
        WidthMm = 12,
        HeightMm = 12
    });
    template.Objects.Add(new LabelObject
    {
        Type = ObjectType.TextBox,
        Name = "Box Main",
        BindingExpression = "{Description}",
        XMm = 16,
        YMm = 2,
        WidthMm = 10,
        HeightMm = 4,
        Style = { FontSizePt = 12 }
    });

    var printService = new PrintService();
    var rows = new IReadOnlyDictionary<string, string>?[]
    {
        new Dictionary<string, string>
        {
            ["Code"] = string.Empty,
            ["Description"] = "This text is too long for the box"
        }
    };

    var preflight = printService.ValidateRows(template, rows);
    AssertEqual(false, preflight.IsSuccess, "Preflight should fail for invalid barcode or text overflow");
    AssertEqual(true, preflight.Issues.Count >= 2, "Preflight should report multiple issues when multiple objects are invalid");
    AssertEqual(true, preflight.ToUserMessage().Contains("Print blocked", StringComparison.OrdinalIgnoreCase), "Preflight message should explain why print is blocked");
    return Task.CompletedTask;
}

static async Task TestPrintLogAppend()
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

    AssertEqual(true, File.Exists(service.LogFilePath), "Print log Excel file should exist");
}

static Task TestTemplateLibraryStandalone()
{
    // Walk up from bin output to the source TemplateLibrary directory
    var baseDir = AppContext.BaseDirectory;
    var libraryDir = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "ANLAbel.App", "TemplateLibrary"));
    if (!Directory.Exists(libraryDir))
    {
        // Fallback: try relative to solution root
        libraryDir = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "..", "src", "ANLAbel.App", "TemplateLibrary"));
    }

    if (!Directory.Exists(libraryDir))
    {
        throw new InvalidOperationException($"TemplateLibrary directory not found. Searched: {libraryDir}");
    }

    var anlabelFiles = Directory.GetFiles(libraryDir, "*.anlabel");
    AssertEqual(true, anlabelFiles.Length > 0, "TemplateLibrary must contain at least one .anlabel file");

    var options = new System.Text.Json.JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    var failures = new List<string>();
    foreach (var file in anlabelFiles)
    {
        var json = File.ReadAllText(file);
        var template = System.Text.Json.JsonSerializer.Deserialize<LabelTemplate>(json, options);
        if (template is null)
        {
            failures.Add($"{Path.GetFileName(file)}: could not deserialize");
            continue;
        }

        var filePath = template.DatabaseConfig.FilePath;
        if (!string.IsNullOrWhiteSpace(filePath))
        {
            failures.Add($"{Path.GetFileName(file)}: DatabaseConfig.FilePath should be empty but is '{filePath}'");
        }
    }

    if (failures.Count > 0)
    {
        throw new InvalidOperationException(
            $"Template Library standalone failures (templates must not ship with hardcoded Excel links):\n  - {string.Join("\n  - ", failures)}");
    }

    return Task.CompletedTask;
}

static void AssertEqual<T>(T expected, T actual, string message)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"{message}. Expected '{expected}', actual '{actual}'.");
    }
}

static void AssertNear(double expected, double actual, double tolerance, string message)
{
    if (Math.Abs(expected - actual) > tolerance)
    {
        throw new InvalidOperationException($"{message}. Expected '{expected}', actual '{actual}'.");
    }
}

sealed class CapturingBarcodeRenderer : IBarcodeRenderer
{
    public int LastDpi { get; private set; }

    public BarcodePixelImage RenderBarcode(string data, BarcodeType type, double widthMm, double heightMm, int dpi, BarcodeRenderOptions? options = null)
    {
        LastDpi = dpi;
        return new BarcodePixelImage(1, 1, new byte[] { 0, 0, 0, 255 });
    }

    public bool ValidateData(string data, BarcodeType type) => true;

    public string GetBarcodeInfo(string data, BarcodeType type) => string.Empty;

    public BarcodeVectorData? RenderBarcodeVector(string data, BarcodeType type, double widthMm, double heightMm, int dpi, BarcodeRenderOptions? options = null) => null;
}
