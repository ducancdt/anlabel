using ANLAbel.Barcode.Options;
using ANLAbel.Barcode.Renderers;
using ANLAbel.Core.Barcode;
using ANLAbel.Core.Enums;
using ANLAbel.Core.Expressions;
using ANLAbel.Core.Expressions.Formulas;
using ANLAbel.Core.Geometry;
using ANLAbel.Core.Models;
using ANLAbel.Data.DataLogs;
using ANLAbel.Data.Excel;
using System.Data;
using ANLAbel.Data.PrintLogs;
using ANLAbel.App.ViewModels;
using ANLAbel.App.Controls;
using ANLAbel.Data;
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
    ("code 39 lowercase input decodes correctly in standard mode", TestCode39LowercaseDecodesCorrectly),
    ("print visual render", TestPrintVisualRender),
    ("print preview follows design label size", TestPrintPreviewUsesDesignLabelSize),
    ("print renderer keeps edge content", TestPrintRendererKeepsEdgeContent),
    ("print barcode uses plan (real print) dpi", TestPrintBarcodeUsesPlanDpi),
    ("print preflight blocks object outside label", TestPrintPreflightBlocksObjectOutsideLabel),
    ("print preflight blocks text outside label", TestPrintPreflightBlocksTextOutsideLabel),
    ("print preflight validation", TestPrintPreflightValidation),
    ("print log CSV append is fast and escapes fields correctly", TestPrintLogAppend),
    ("print log exports to a readable Excel report", TestPrintLogExportToExcel),
    ("template library standalone (no sample-data link)", TestTemplateLibraryStandalone),
    ("template excel link survives folder move", TestExcelLinkSurvivesFolderMove),
    ("data source registry CRUD", TestDataSourceRegistryCrud),
    ("designer preview row keeps object geometry", TestDesignerPreviewRowKeepsGeometry),
    ("excel read honors cancellation", TestExcelReadHonorsCancellation),
    ("excel refresh skips unchanged file", TestExcelRefreshSkipsUnchangedFile),
    ("database config full round trip", TestDatabaseConfigRoundTrip),
    ("data operation log records import success and failure", TestDataOperationLogRecordsImports),
    ("key field selection tracks row across refresh", TestKeyFieldSelectionTracksRow),
    ("linked excel file watcher flags stale data", TestLinkedExcelFileWatcherFlagsStaleData),
    ("shared data source relink fixes every referencing template", TestSharedDataSourceRelinkFixesTemplate),
    ("layer forward/backward swap adjacent ZIndex", TestLayerForwardBackward),
    ("rotation quick buttons set exact degrees", TestSetRotationCommand),
    ("barcode module size warning flags sub-2-dot modules", TestBarcodeModuleSizeWarning),
    ("print preflight blocks missing bound field", TestPrintPreflightBlocksMissingField),
    ("quick print blocks when linked excel data is stale", TestQuickPrintBlocksOnStaleData),
    ("print operation log records job-level trace", TestPrintOperationLogRecordsJob),
    ("preview and print render the same geometry, offset by the plan", TestPreviewAndPrintRenderSameGeometry),
    ("preflight warns when barcode module too small at real print dpi", TestPreflightWarnsSmallModuleAtPrintDpi),
    ("tracking row printed indicator toggles with IsPrinted", TestTrackingRowPrintedIndicator),
    ("barcode module size warning uses same dpi as real preflight", TestBarcodeModuleSizeWarningMatchesPrintDpi),
    ("add current as data source is idempotent", TestAddCurrentAsDataSourceIsIdempotent),
    ("unlink excel clears database config but keeps bindings", TestUnlinkExcelKeepsBindings),
    ("unlink excel works when link is broken", TestUnlinkExcelWhenLinkBroken),
    ("test connection reports ok, missing sheet, and missing file", TestExcelTestConnectionReportsStatus),
    ("data source records recent template usage", TestDataSourceRecordsRecentUsage),
    ("registry with unknown extra fields still loads", TestRegistryForwardCompatibility),
    ("import keeps a non-default header row instead of resetting to 1", TestImportKeepsCustomHeaderRow),
    ("excel preview rows use absolute row numbers and respect maxRows", TestExcelPreviewRows),
    ("copies-per-record resolves from Excel column, defaults to 1", TestResolveCopiesForRow),
    ("get sheets with preview reads every sheet in a single file open", TestGetSheetsWithPreview),
    ("excel cell value formatting after switching to ExcelDataReader", TestExcelCellValueFormatting)
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

static async Task TestExcelCellValueFormatting()
{
    // Bug fix (2026-07-03): switched ExcelDataService from ClosedXML to ExcelDataReader to
    // fix memory-pressure GC-pause hangs on weak machines. ExcelDataReader gives raw typed
    // values instead of ClosedXML's exact display-formatted string, so number/date/bool
    // formatting here is a deliberate best-effort approximation (documented trade-off) —
    // this test locks in the agreed behavior so a future change doesn't silently drift it.
    var dir = Path.Combine(Environment.CurrentDirectory, "TestOutput");
    Directory.CreateDirectory(dir);
    var filePath = Path.Combine(dir, $"cell-formatting-{Guid.NewGuid():N}.xlsx");

    using (var workbook = new XLWorkbook())
    {
        var sheet = workbook.AddWorksheet("Data");
        sheet.Cell(1, 1).Value = "WholeNumber";
        sheet.Cell(1, 2).Value = "Fraction";
        sheet.Cell(1, 3).Value = "Percent";
        sheet.Cell(1, 4).Value = "DateOnly";
        sheet.Cell(1, 5).Value = "Flag";

        sheet.Cell(2, 1).Value = 42;
        sheet.Cell(2, 2).Value = 3.5;
        sheet.Cell(2, 3).Value = 0.5;
        sheet.Cell(2, 3).Style.NumberFormat.Format = "0%";
        sheet.Cell(2, 4).Value = new DateTime(2026, 7, 3);
        sheet.Cell(2, 4).Style.DateFormat.Format = "yyyy-mm-dd";
        sheet.Cell(2, 5).Value = true;

        workbook.SaveAs(filePath);
    }

    try
    {
        var table = await new ExcelDataService().LoadSheetAsync(filePath, "Data");
        var row = table.Rows[0];

        AssertEqual("42", row["WholeNumber"], "A whole number must not show a forced decimal point");
        AssertEqual("3.5", row["Fraction"], "A fractional number must show its decimal digits");
        AssertEqual("50%", row["Percent"], "A percentage-formatted cell must be detected and shown with a % sign");
        AssertEqual("2026-07-03", row["DateOnly"], "A date-only cell must format as yyyy-MM-dd, not include a time");
        AssertEqual("TRUE", row["Flag"], "A boolean cell must format as Excel-style TRUE/FALSE");
    }
    finally
    {
        try { File.Delete(filePath); } catch { }
    }
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

static Task TestCode39LowercaseDecodesCorrectly()
{
    // Regression test: Code 39's writer alphabet is uppercase-only and case-sensitive.
    // Lowercase input used to slip through ValidateData's case-insensitive check and
    // reach ZXing's encoder unmodified, which silently switched to Full ASCII/Extended
    // mode — producing a barcode that a standard-mode scanner reads back as garbage
    // (e.g. "abc123" -> "+A+B+C123") instead of failing loudly or printing "ABC123".
    var renderer = new ZxingBarcodeRenderer();
    var image = renderer.RenderBarcode("abc-01", BarcodeType.Code39, 60, 20, 300);

    var source = new ZXing.RGBLuminanceSource(image.BgraPixels, image.WidthPixels, image.HeightPixels, ZXing.RGBLuminanceSource.BitmapFormat.BGRA32);
    var bitmap = new ZXing.BinaryBitmap(new ZXing.Common.HybridBinarizer(source));
    var reader = new ZXing.OneD.Code39Reader(usingCheckDigit: false, extendedMode: false);
    var result = reader.decode(bitmap);

    AssertEqual("ABC-01", result?.Text, "A standard-mode Code 39 scanner must read back the uppercased data, not an extended-mode shift sequence");
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

static Task TestPrintBarcodeUsesPlanDpi()
{
    // print-preview-reliability-plan R5/item 8 (2026-07-03, confirmed with project owner):
    // Preview/Print must render barcode modules at the printer's real DPI (the plan),
    // not the object's own QrDpi, so module dots are physically sized correctly on the
    // label that actually comes out of the printer. This intentionally supersedes the
    // older "match the Designer's QrDpi" rule (see agent.md rule 12) — the Designer
    // canvas still renders barcodes at the object's own QrDpi independently.
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

    AssertEqual(203, fakeRenderer.LastDpi, "Print/preview barcode renderer must use the plan's real print DPI, not the object's own QrDpi, so module dots match the physical printer");
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

static Task TestPreviewAndPrintRenderSameGeometry()
{
    // print-preview-reliability-plan Đợt 2 item 4: the designer canvas already renders
    // object geometry directly from XMm/YMm/WidthMm/HeightMm with no mutation (locked in
    // by "designer preview row keeps object geometry"), so the model itself IS the
    // canvas's geometry. What actually needs locking down is that LabelVisualRenderer —
    // shared by both the Print Preview thumbnail and the real print job — places that
    // same object at the exact same pixel position for a "preview" plan (no printer
    // offset) as the design, and shifts by *exactly* the configured offset for a "print"
    // plan — proving preview and print never silently diverge in geometry.
    var template = new LabelTemplate { Name = "WYSIWYG Test", WidthMm = 40, HeightMm = 25, Dpi = 300 };
    template.Objects.Add(new LabelObject
    {
        Type = ObjectType.Rectangle,
        XMm = 10,
        YMm = 8,
        WidthMm = 6,
        HeightMm = 4,
        Style = { FillStyle = FillStyle.Solid, FillColor = "#000000", OutlineStyle = OutlineStyle.None }
    });

    var renderer = new LabelVisualRenderer();

    // "Preview" plan: no printer offset/rotation — must match the design position
    // (within ~1 rasterized pixel — anti-aliasing on the fill edge means the first
    // fully-opaque pixel row/column lands slightly after the mathematical edge).
    const double rasterToleranceMm = 0.3;
    var previewPlan = new PrintRenderPlan { Dpi = 300, LabelWidthMm = 40, LabelHeightMm = 25 };
    var previewBounds = RenderAndFindDarkBoundsMm(renderer, template, previewPlan);
    AssertNear(10, previewBounds.Left, rasterToleranceMm, "Preview render left edge must match the design XMm");
    AssertNear(8, previewBounds.Top, rasterToleranceMm, "Preview render top edge must match the design YMm");

    // "Print" plan: same template/row, but with a real printer calibration offset — the
    // object must shift by exactly that offset. Compare the *delta* between the two
    // renders rather than each render's absolute position against the nominal mm value.
    // Sub-pixel anti-aliasing still varies with fractional pixel alignment (offsetting an
    // object lands it at a different fractional pixel than the un-offset version), so this
    // pixel-threshold measurement technique carries its own ~1px resolution — real geometry
    // bugs here show up as multi-mm or wrong-direction drift, well outside this tolerance.
    const double offsetXMm = 3.0;
    const double offsetYMm = 1.5;
    var printPlan = new PrintRenderPlan { Dpi = 300, LabelWidthMm = 40, LabelHeightMm = 25, OffsetXMm = offsetXMm, OffsetYMm = offsetYMm };
    var printBounds = RenderAndFindDarkBoundsMm(renderer, template, printPlan);
    AssertNear(offsetXMm, printBounds.Left - previewBounds.Left, rasterToleranceMm, "Print render must shift left edge by exactly the plan's OffsetXMm relative to preview, not a different amount");
    AssertNear(offsetYMm, printBounds.Top - previewBounds.Top, rasterToleranceMm, "Print render must shift top edge by exactly the plan's OffsetYMm relative to preview, not a different amount");

    return Task.CompletedTask;
}

static (double Left, double Top) RenderAndFindDarkBoundsMm(LabelVisualRenderer renderer, LabelTemplate template, PrintRenderPlan plan)
{
    var visual = renderer.Render(template, null, plan);
    var widthDip = MmConverter.MmToDip(plan.LabelWidthMm);
    var heightDip = MmConverter.MmToDip(plan.LabelHeightMm);
    var bitmap = RenderTestBitmap(visual, widthDip, heightDip);
    var pixels = new byte[bitmap.PixelWidth * bitmap.PixelHeight * 4];
    bitmap.CopyPixels(pixels, bitmap.PixelWidth * 4, 0);

    var minX = int.MaxValue;
    var minY = int.MaxValue;
    for (var y = 0; y < bitmap.PixelHeight; y++)
    {
        for (var x = 0; x < bitmap.PixelWidth; x++)
        {
            var index = (y * bitmap.PixelWidth + x) * 4;
            var isDark = pixels[index] < 20 && pixels[index + 1] < 20 && pixels[index + 2] < 20 && pixels[index + 3] > 200;
            if (!isDark)
            {
                continue;
            }

            if (x < minX) minX = x;
            if (y < minY) minY = y;
        }
    }

    if (minX == int.MaxValue)
    {
        throw new InvalidOperationException("No dark pixels found in rendered bitmap — the test object did not render.");
    }

    return (MmConverter.DipToMm(minX), MmConverter.DipToMm(minY));
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

static Task TestTrackingRowPrintedIndicator()
{
    // print-preview-reliability-plan Đợt 4 item 11 ("chống trùng tem"): the Print Preview
    // tracking row must expose a bindable printed flag + display glyph so the UI can warn
    // before re-sending an already-printed row.
    var row = new TrackingRowViewModel { SourceRowNumber = 1 };
    AssertEqual(false, row.IsPrinted, "A fresh tracking row must not be marked printed");
    AssertEqual(string.Empty, row.PrintedIndicatorText, "Unprinted row must show no printed indicator");

    var raisedPrintedChanged = false;
    row.PropertyChanged += (_, e) =>
    {
        if (e.PropertyName == nameof(TrackingRowViewModel.PrintedIndicatorText))
        {
            raisedPrintedChanged = true;
        }
    };

    row.IsPrinted = true;
    AssertEqual(true, row.IsPrinted, "Marking a row printed must stick");
    AssertEqual(false, string.IsNullOrEmpty(row.PrintedIndicatorText), "Printed row must show a non-empty indicator");
    AssertEqual(true, raisedPrintedChanged, "Setting IsPrinted must notify PrintedIndicatorText so the UI glyph updates");

    return Task.CompletedTask;
}

static Task TestPreflightWarnsSmallModuleAtPrintDpi()
{
    // print-preview-reliability-plan R5/item 8: since Preview/Print now render matrix
    // barcodes at the printer's real DPI instead of the object's own QrDpi, preflight
    // must catch the case where that switch would shrink the module below ~2 physical
    // dots — this is exactly the scenario the DPI-priority change (see agent.md rule 12)
    // could otherwise silently create.
    var template = new LabelTemplate { Name = "Module DPI Test", WidthMm = 40, HeightMm = 25, Dpi = 203 };
    template.Objects.Add(new LabelObject
    {
        Type = ObjectType.QRCode,
        Text = "DPI-CHECK",
        XMm = 2,
        YMm = 2,
        WidthMm = 12,
        HeightMm = 12,
        QrDpi = 300,
        QrSizingMode = QrSizingMode.FixedVersionAndModuleSize,
        QrModuleSizePx = 2
    });

    var printService = new PrintService();
    var rows = new IReadOnlyDictionary<string, string>?[] { null };
    var preflight = printService.ValidateRows(template, rows);

    // 2 px module designed at 300 DPI, printed at 203 DPI (PrinterProfile.Dpi default)
    // => 2 * 203 / 300 ≈ 1.35 dots.
    AssertEqual(false, preflight.IsSuccess, "Preflight must flag a matrix barcode module that would print under 2 physical dots");
    AssertEqual(true, preflight.Issues.Any(issue => issue.Message.Contains("dot(s)", StringComparison.OrdinalIgnoreCase)), "Preflight issue must explain the module-size-in-dots problem");

    return Task.CompletedTask;
}

static Task TestPrintPreflightBlocksMissingField()
{
    // print-preview-reliability-plan R3: the plain "{Field}" syntax silently resolves a
    // missing Excel column to an empty string, so without this check a text object bound
    // to a renamed/removed column would print blank with zero warning. Preflight must
    // catch this before the label reaches the printer.
    var template = new LabelTemplate { Name = "Missing Field Test", WidthMm = 60, HeightMm = 30, Dpi = 300 };
    template.Objects.Add(new LabelObject
    {
        Type = ObjectType.Text,
        Name = "PartNo Label",
        BindingExpression = "{PartNo}",
        XMm = 2,
        YMm = 2,
        WidthMm = 30,
        HeightMm = 6
    });

    var printService = new PrintService();

    // Row has the field: preflight must pass (isolating this test from unrelated overflow rules).
    var okRows = new IReadOnlyDictionary<string, string>?[] { new Dictionary<string, string> { ["PartNo"] = "PN-100" } };
    var okPreflight = printService.ValidateRows(template, okRows);
    AssertEqual(true, okPreflight.IsSuccess, "Preflight must pass when the bound field is present in the row");

    // Row is missing the field entirely (e.g. the Excel column was renamed).
    var missingRows = new IReadOnlyDictionary<string, string>?[] { new Dictionary<string, string> { ["OtherColumn"] = "x" } };
    var missingPreflight = printService.ValidateRows(template, missingRows);
    AssertEqual(false, missingPreflight.IsSuccess, "Preflight must fail when the bound field is missing from the row's Excel data");
    AssertEqual(true, missingPreflight.Issues.Any(issue => issue.Message.Contains("PartNo", StringComparison.OrdinalIgnoreCase)), "Preflight issue must name the missing field");

    return Task.CompletedTask;
}

static async Task TestPrintLogAppend()
{
    // Bug fix (2026-07-04): print-history used to be a live .xlsx re-opened and fully
    // re-saved via ClosedXML on every single print — the same memory-hungry parser that
    // caused the Import Excel hang, except this one got slower with every print job over
    // the product's whole lifetime instead of just once. Switched to append-only CSV
    // (File.AppendAllText, O(1) per print regardless of how large the log has grown).
    var directory = Path.Combine(Environment.CurrentDirectory, "TestOutput");
    Directory.CreateDirectory(directory);
    var service = new PrintLogService(Path.Combine(directory, $"print-history-{Guid.NewGuid():N}.csv"));

    await service.AppendAsync(new PrintLogEntry
    {
        TemplateName = "Test Template",
        PrinterName = "Test Printer",
        PrintMode = "Unit test",
        PartNo = "PN-100",
        ItemName = "Bracket, steel", // contains a comma — must be quoted in the CSV
        Notes = "Say \"hello\"" // contains a quote — must be escaped as ""
    });
    await service.AppendAsync(new PrintLogEntry
    {
        TemplateName = "Test Template",
        PrinterName = "Test Printer",
        PrintMode = "Unit test",
        PartNo = "PN-200"
    });

    AssertEqual(true, File.Exists(service.LogFilePath), "Print log CSV file should exist");
    var lines = await File.ReadAllLinesAsync(service.LogFilePath);
    AssertEqual(3, lines.Length, "CSV must have exactly 1 header line + 2 data lines (no header repeated on the second append)");
    AssertEqual(true, lines[0].StartsWith("PrintedAt,PartNo,"), "First line must be the header row");
    AssertEqual(true, lines[1].Contains("\"Bracket, steel\""), "A field containing a comma must be quoted");
    AssertEqual(true, lines[1].Contains("\"Say \"\"hello\"\"\""), "A field containing a quote must double it and be quoted");
    AssertEqual(true, lines[2].Contains("PN-200"), "Second data row must contain the second entry's PartNo");

    try { File.Delete(service.LogFilePath); } catch { }
}

static async Task TestPrintLogExportToExcel()
{
    // The occasional "Export to Excel" report replaces the old always-on live .xlsx —
    // ClosedXML is fine here since it only runs once per user click, not once per label.
    var directory = Path.Combine(Environment.CurrentDirectory, "TestOutput");
    Directory.CreateDirectory(directory);
    var csvPath = Path.Combine(directory, $"print-history-{Guid.NewGuid():N}.csv");
    var xlsxPath = Path.Combine(directory, $"print-history-export-{Guid.NewGuid():N}.xlsx");
    var service = new PrintLogService(csvPath);

    await service.AppendManyAsync(new[]
    {
        new PrintLogEntry { TemplateName = "T1", PrinterName = "Zebra ZT230", PrintMode = "Selected rows", PartNo = "PN-001", Quantity = "5" },
        new PrintLogEntry { TemplateName = "T1", PrinterName = "Zebra ZT230", PrintMode = "Selected rows", PartNo = "PN-002", Quantity = "10" }
    });

    await service.ExportToExcelAsync(xlsxPath);
    AssertEqual(true, File.Exists(xlsxPath), "Export must produce an .xlsx file");

    // Read the export back with our own Excel reader to confirm it is a well-formed,
    // readable workbook with the expected header and row data — not just "a file exists".
    var table = await new ExcelDataService().LoadSheetAsync(xlsxPath, "PrintHistory");
    AssertEqual(2, table.Rows.Count, "Exported report must contain one row per log entry");
    AssertEqual(true, table.Columns.Contains("PartNo"), "Exported report must keep the PartNo column header");
    AssertEqual("PN-001", table.Rows[0]["PartNo"], "First exported row must match the first log entry");
    AssertEqual("PN-002", table.Rows[1]["PartNo"], "Second exported row must match the second log entry");

    try { File.Delete(csvPath); } catch { }
    try { File.Delete(xlsxPath); } catch { }
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

static async Task TestExcelLinkSurvivesFolderMove()
{
    // Scenario: save template + Excel in same directory, copy both to a new location,
    // open from new location — the Excel link must resolve via RelativePath.
    var baseDir = Path.Combine(Environment.CurrentDirectory, "TestOutput", $"folder-move-{Guid.NewGuid():N}");
    var sourceDir = Path.Combine(baseDir, "Source");
    Directory.CreateDirectory(sourceDir);

    // 1. Create a real Excel file in sourceDir
    var excelPath = Path.Combine(sourceDir, "data.xlsx");
    using (var workbook = new XLWorkbook())
    {
        var sheet = workbook.AddWorksheet("Parts");
        sheet.Cell(1, 1).Value = "PartNo";
        sheet.Cell(1, 2).Value = "Qty";
        sheet.Cell(2, 1).Value = "PN-100";
        sheet.Cell(2, 2).Value = "50";
        workbook.SaveAs(excelPath);
    }

    // 2. Save a template via MainViewModel (which calls UpdateRelativePath)
    var vm = new MainViewModel();
    await vm.ImportExcelAsync(excelPath, "Parts");
    var templatePath = Path.Combine(sourceDir, "test-template.anlabel");
    await vm.SaveAsync(templatePath);

    // Verify RelativePath was stored (should be "data.xlsx" since same directory)
    AssertEqual(false, string.IsNullOrWhiteSpace(vm.Template.DatabaseConfig.RelativePath),
        "RelativePath must be stored after save");
    AssertEqual("data.xlsx", vm.Template.DatabaseConfig.RelativePath,
        "RelativePath should be the filename when Excel and template are in the same directory");

    // 3. Copy entire directory to a new location (simulating folder move)
    var destDir = Path.Combine(baseDir, "Dest");
    Directory.CreateDirectory(destDir);
    var destTemplate = Path.Combine(destDir, "test-template.anlabel");
    var destExcel = Path.Combine(destDir, "data.xlsx");
    File.Copy(templatePath, destTemplate);
    File.Copy(excelPath, destExcel);

    // 4. Delete originals to ensure the absolute path won't work
    File.Delete(templatePath);
    File.Delete(excelPath);
    Directory.Delete(sourceDir, true);

    // 5. Open from new location
    var vm2 = new MainViewModel();
    await vm2.OpenAsync(destTemplate);

    AssertEqual(false, vm2.IsExcelLinkBroken,
        "Excel link must be restored after folder move via RelativePath or same-directory fallback");
    AssertEqual(true, vm2.HasLinkedExcelSource,
        "Template must still report a linked Excel source after folder move");

    // Cleanup
    try { Directory.Delete(baseDir, true); } catch { }
}

static Task TestDataSourceRegistryCrud()
{
    var registryPath = Path.Combine(Environment.CurrentDirectory, "TestOutput", $"ds-registry-{Guid.NewGuid():N}.json");
    var registry = new DataSourceRegistry(registryPath);

    // Load empty (file does not exist yet)
    registry.Load();
    AssertEqual(0, registry.Sources.Count, "New registry should have no sources");

    // Upsert a source
    var source = new DataSource
    {
        Id = "test-ds-1",
        Name = "Production Excel",
        FilePath = @"C:\data\production.xlsx",
        SheetName = "Parts",
        HeaderRowIndex = 1
    };
    registry.Upsert(source);
    AssertEqual(1, registry.Sources.Count, "Registry should have 1 source after upsert");

    // Save and reload
    registry.Save();
    var registry2 = new DataSourceRegistry(registryPath);
    registry2.Load();
    AssertEqual(1, registry2.Sources.Count, "Reloaded registry should have 1 source");
    var loaded = registry2.GetById("test-ds-1");
    AssertEqual(true, loaded is not null, "GetById should find the source");
    AssertEqual("Production Excel", loaded!.Name, "Source name must survive round trip");
    AssertEqual(@"C:\data\production.xlsx", loaded.FilePath, "Source FilePath must survive round trip");
    AssertEqual("Parts", loaded.SheetName, "Source SheetName must survive round trip");

    // Update
    loaded.Name = "Production Excel v2";
    registry2.Upsert(loaded);
    registry2.Save();
    var registry3 = new DataSourceRegistry(registryPath);
    registry3.Load();
    AssertEqual("Production Excel v2", registry3.GetById("test-ds-1")!.Name, "Updated name must survive round trip");

    // Remove
    registry3.Remove("test-ds-1");
    AssertEqual(0, registry3.Sources.Count, "Registry should have 0 sources after remove");
    registry3.Save();
    var registry4 = new DataSourceRegistry(registryPath);
    registry4.Load();
    AssertEqual(0, registry4.Sources.Count, "Saved empty registry should persist");

    // Template references DataSourceId
    var template = new LabelTemplate();
    template.DatabaseConfig.DataSourceId = "test-ds-1";
    AssertEqual("test-ds-1", template.DatabaseConfig.DataSourceId, "DataSourceId must survive assignment");

    // Cleanup
    try { File.Delete(registryPath); } catch { }
    return Task.CompletedTask;
}

static Task TestDesignerPreviewRowKeepsGeometry()
{
    Exception? failure = null;
    var thread = new Thread(() =>
    {
        try
        {
            var text = new LabelObject
            {
                Type = ObjectType.Text,
                XMm = 12.5,
                YMm = 7.25,
                WidthMm = 18,
                HeightMm = 6,
                BindingExpression = "{PartNo}",
                Text = "Part"
            };
            var template = new LabelTemplate { WidthMm = 100, HeightMm = 50 };
            template.Objects.Add(text);
            var before = (text.XMm, text.YMm, text.WidthMm, text.HeightMm);

            var canvas = new LabelDesignerCanvas { Template = template };
            canvas.PreviewRow = new Dictionary<string, string> { ["PartNo"] = "A" };
            canvas.PreviewRow = new Dictionary<string, string>
            {
                ["PartNo"] = "A-VERY-LONG-PART-NUMBER-THAT-USED-TO-RESIZE-THE-MODEL"
            };

            var after = (text.XMm, text.YMm, text.WidthMm, text.HeightMm);
            AssertEqual(before, after, "Changing PreviewRow must not mutate designer object geometry");
        }
        catch (Exception ex)
        {
            failure = ex;
        }
    });

    thread.SetApartmentState(ApartmentState.STA);
    thread.Start();
    thread.Join();
    if (failure is not null)
    {
        throw failure;
    }

    return Task.CompletedTask;
}

static async Task TestExcelReadHonorsCancellation()
{
    var service = new ExcelDataService();
    using var cts = new CancellationTokenSource();
    cts.Cancel();

    try
    {
        await service.GetSheetNamesAsync("not-used.xlsx", cts.Token);
        throw new InvalidOperationException("Canceled Excel read unexpectedly completed");
    }
    catch (OperationCanceledException)
    {
        // Expected: the UI can leave a stalled/slow read without blocking its thread.
    }
}

static async Task TestExcelTestConnectionReportsStatus()
{
    // database-manager-module-plan.md M2: Test Connection must never throw — it reports
    // ok/failure in its return value so the Database Manager window can show it inline.
    var dir = Path.Combine(Environment.CurrentDirectory, "TestOutput", $"test-connection-{Guid.NewGuid():N}");
    Directory.CreateDirectory(dir);
    var excelPath = Path.Combine(dir, "data.xlsx");
    using (var workbook = new XLWorkbook())
    {
        var sheet = workbook.AddWorksheet("Parts");
        sheet.Cell(1, 1).Value = "PartNo";
        sheet.Cell(2, 1).Value = "PN-100";
        workbook.SaveAs(excelPath);
    }

    var service = new ExcelDataService();

    var ok = await service.TestConnectionAsync(excelPath, "Parts", 1);
    AssertEqual(true, ok.Ok, "Test connection should succeed for a valid file/sheet/header");

    var missingSheet = await service.TestConnectionAsync(excelPath, "DoesNotExist", 1);
    AssertEqual(false, missingSheet.Ok, "Test connection should fail for a missing sheet");
    AssertEqual(true, missingSheet.Message.Contains("DoesNotExist", StringComparison.OrdinalIgnoreCase), "Failure message should name the missing sheet");

    var missingFile = await service.TestConnectionAsync(Path.Combine(dir, "does-not-exist.xlsx"), "Parts", 1);
    AssertEqual(false, missingFile.Ok, "Test connection should fail for a missing file");

    try { Directory.Delete(dir, true); } catch { }
}

static async Task TestExcelPreviewRows()
{
    // database-plan.md Giai đoạn 3 item 8: PreviewRowsAsync must return the sheet's real,
    // absolute (1-based) row numbers — not an ordinal index — so a caller can assign the
    // chosen row straight to DatabaseConfig.HeaderRowIndex, even for a workbook whose data
    // does not start at physical row 1.
    var dir = Path.Combine(Environment.CurrentDirectory, "TestOutput", $"preview-rows-{Guid.NewGuid():N}");
    Directory.CreateDirectory(dir);
    var excelPath = Path.Combine(dir, "data.xlsx");
    using (var workbook = new XLWorkbook())
    {
        var sheet = workbook.AddWorksheet("Parts");
        sheet.Cell(1, 1).Value = "Report generated 2026-07-03";
        sheet.Cell(3, 1).Value = "PartNo";
        sheet.Cell(3, 2).Value = "Qty";
        sheet.Cell(4, 1).Value = "PN-100";
        sheet.Cell(4, 2).Value = "5";
        sheet.Cell(5, 1).Value = "PN-200";
        sheet.Cell(5, 2).Value = "9";
        workbook.SaveAs(excelPath);
    }

    var service = new ExcelDataService();
    var rows = await service.PreviewRowsAsync(excelPath, "Parts", maxRows: 15);

    AssertEqual(5, rows.Count, "Preview should include every used row when under maxRows");
    AssertEqual(1, rows[0].RowNumber, "First preview row must report the real sheet row number");
    AssertEqual(3, rows[2].RowNumber, "Header row must be reported by its real sheet row number, not an ordinal index");
    AssertEqual("PartNo", rows[2].Cells[0], "Preview must expose the header row's actual cell text");
    AssertEqual("PN-100", rows[3].Cells[0], "Preview must expose data rows below the header as-is");

    var capped = await service.PreviewRowsAsync(excelPath, "Parts", maxRows: 2);
    AssertEqual(2, capped.Count, "maxRows must cap how many rows are read");
    AssertEqual(1, capped[0].RowNumber, "Capped preview must still start from the real first row");
    AssertEqual(2, capped[1].RowNumber, "Capped preview must stop after maxRows real rows");

    try { Directory.Delete(dir, true); } catch { }
}

static Task TestResolveCopiesForRow()
{
    // database-manager-module-plan.md M4: "label copies per record" — a column can drive
    // how many copies to print per row. Must never throw or go negative; a blank/malformed
    // cell always falls back to 1 so a bad value in the sheet can't block Print Preview.
    var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["PartNo"] = "PN-100",
        ["Qty"] = "5",
        ["Blank"] = "",
        ["Negative"] = "-2",
        ["NotANumber"] = "abc"
    };

    AssertEqual(1, DatabaseConfig.ResolveCopiesForRow(string.Empty, row), "No CopiesField configured must default to 1");
    AssertEqual(1, DatabaseConfig.ResolveCopiesForRow("Qty", null), "A null row must default to 1, not throw");
    AssertEqual(5, DatabaseConfig.ResolveCopiesForRow("Qty", row), "A valid numeric column must be used as the copy count");
    AssertEqual(1, DatabaseConfig.ResolveCopiesForRow("MissingColumn", row), "A column absent from this row must default to 1");
    AssertEqual(1, DatabaseConfig.ResolveCopiesForRow("Blank", row), "A blank cell must default to 1");
    AssertEqual(1, DatabaseConfig.ResolveCopiesForRow("Negative", row), "A negative value must default to 1, not print a negative count");
    AssertEqual(1, DatabaseConfig.ResolveCopiesForRow("NotANumber", row), "A non-numeric cell must default to 1");
    AssertEqual(999, DatabaseConfig.ResolveCopiesForRow("Qty", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Qty"] = "5000" }), "An absurdly large value must be capped, not printed as-is");

    return Task.CompletedTask;
}

static async Task TestGetSheetsWithPreview()
{
    // Bug fix (2026-07-03): ExcelImportWindow used to call GetSheetNamesAsync then, after the
    // user picked a sheet, PreviewRowsAsync — two separate full parses of the same workbook
    // on top of the third parse LoadSheetAsync does during the real import. On a slow machine
    // or large file, the extra hidden parse (no wait cursor/Cancel shown) presented as the app
    // hanging. GetSheetsWithPreviewAsync must return every sheet's name AND preview rows from
    // a single open so the import flow only needs two opens total, same as before the
    // header-row picker feature existed.
    var dir = Path.Combine(Environment.CurrentDirectory, "TestOutput", $"sheets-with-preview-{Guid.NewGuid():N}");
    Directory.CreateDirectory(dir);
    var excelPath = Path.Combine(dir, "data.xlsx");
    using (var workbook = new XLWorkbook())
    {
        var parts = workbook.AddWorksheet("Parts");
        parts.Cell(1, 1).Value = "Report generated 2026-07-03";
        parts.Cell(3, 1).Value = "PartNo";
        parts.Cell(4, 1).Value = "PN-100";
        workbook.AddWorksheet("Empty");
        workbook.SaveAs(excelPath);
    }

    var service = new ExcelDataService();
    var sheets = await service.GetSheetsWithPreviewAsync(excelPath, maxRows: 15);

    AssertEqual(2, sheets.Count, "Every sheet in the workbook must be returned");
    AssertEqual("Parts", sheets[0].SheetName, "Sheets must be returned in workbook order");
    AssertEqual(4, sheets[0].Rows.Count, "Preview rows for a populated sheet must match its used range");
    AssertEqual(3, sheets[0].Rows[2].RowNumber, "Preview row numbers must be absolute sheet rows, matching PreviewRowsAsync");
    AssertEqual("PartNo", sheets[0].Rows[2].Cells[0], "Preview must expose the real header row's text");
    AssertEqual("Empty", sheets[1].SheetName, "An empty sheet must still be listed");
    AssertEqual(0, sheets[1].Rows.Count, "An empty sheet must return zero preview rows, not throw");

    try { Directory.Delete(dir, true); } catch { }
}

static async Task TestDataOperationLogRecordsImports()
{
    // database-plan TC6: every import/refresh/relink must leave a trace so a bad
    // print run can be traced back to which data was read and when.
    var dir = Path.Combine(Environment.CurrentDirectory, "TestOutput", $"data-log-{Guid.NewGuid():N}");
    Directory.CreateDirectory(dir);
    var logPath = Path.Combine(dir, "data-operations.jsonl");
    var excelPath = Path.Combine(dir, "data.xlsx");
    using (var workbook = new XLWorkbook())
    {
        var sheet = workbook.AddWorksheet("Parts");
        sheet.Cell(1, 1).Value = "PartNo";
        sheet.Cell(2, 1).Value = "PN-100";
        workbook.SaveAs(excelPath);
    }

    var vm = new MainViewModel(
        new ANLAbel.Project.SaveLoad.ProjectFileService(),
        new ExcelDataService(),
        new ANLAbel.Printing.PrinterProfiles.PrintService(),
        new ANLAbel.Printing.PrinterProfiles.PrinterDiscoveryService(),
        new PrintLogService(),
        new DataOperationLogService(logPath));

    await vm.ImportExcelAsync(excelPath, "Parts");

    // Failure case: sheet that does not exist must also be logged, then rethrown.
    var missingSheetThrew = false;
    try
    {
        await vm.ImportExcelAsync(excelPath, "DoesNotExist");
    }
    catch
    {
        missingSheetThrew = true;
    }
    AssertEqual(true, missingSheetThrew, "Importing a missing sheet must throw (and still be logged as a failure)");

    var lines = await WaitForLogLinesAsync(logPath, minLineCount: 2);
    AssertEqual(true, lines.Length >= 2, "Both the successful import and the failed import must be logged");

    var successLine = lines.FirstOrDefault(line => line.Contains("\"Success\":true", StringComparison.Ordinal));
    AssertEqual(false, successLine is null, "A successful import entry must be present in the log");
    AssertEqual(true, successLine!.Contains("\"Operation\":\"Import\"", StringComparison.Ordinal), "Successful entry must record the operation label");
    AssertEqual(true, successLine.Contains("\"RowCount\":1", StringComparison.Ordinal), "Successful entry must record the row count");

    var failureLine = lines.FirstOrDefault(line => line.Contains("\"Success\":false", StringComparison.Ordinal));
    AssertEqual(false, failureLine is null, "A failed import entry must be present in the log");
    AssertEqual(false, string.IsNullOrEmpty(ExtractJsonStringField(failureLine!, "ErrorMessage")), "Failed entry must record an error message");

    try { Directory.Delete(dir, true); } catch { }
}

static async Task<string[]> WaitForLogLinesAsync(string logPath, int minLineCount)
{
    for (var attempt = 0; attempt < 40; attempt++)
    {
        if (File.Exists(logPath))
        {
            var lines = (await File.ReadAllLinesAsync(logPath)).Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();
            if (lines.Length >= minLineCount)
            {
                return lines;
            }
        }

        await Task.Delay(50);
    }

    return File.Exists(logPath) ? await File.ReadAllLinesAsync(logPath) : Array.Empty<string>();
}

static string ExtractJsonStringField(string jsonLine, string fieldName)
{
    var marker = $"\"{fieldName}\":\"";
    var start = jsonLine.IndexOf(marker, StringComparison.Ordinal);
    if (start < 0)
    {
        return string.Empty;
    }

    start += marker.Length;
    var end = jsonLine.IndexOf('"', start);
    return end < 0 ? string.Empty : jsonLine[start..end];
}

static async Task TestLinkedExcelFileWatcherFlagsStaleData()
{
    // database-plan GĐ2 item 5: editing the linked Excel file outside the app must
    // raise a "data changed" notice instead of silently keeping stale data around
    // (and must NOT auto-reload — an in-progress print/design session should not
    // have its data swapped mid-way).
    var dir = Path.Combine(Environment.CurrentDirectory, "TestOutput", $"watcher-{Guid.NewGuid():N}");
    Directory.CreateDirectory(dir);
    var excelPath = Path.Combine(dir, "data.xlsx");
    using (var workbook = new XLWorkbook())
    {
        var sheet = workbook.AddWorksheet("Parts");
        sheet.Cell(1, 1).Value = "PartNo";
        sheet.Cell(2, 1).Value = "PN-100";
        workbook.SaveAs(excelPath);
    }

    var vm = new MainViewModel();
    await vm.ImportExcelAsync(excelPath, "Parts");
    AssertEqual(false, vm.IsExcelDataStale, "Freshly imported data must not be flagged stale");

    // Edit the file outside the app.
    await Task.Delay(50);
    using (var workbook = new XLWorkbook())
    {
        var sheet = workbook.AddWorksheet("Parts");
        sheet.Cell(1, 1).Value = "PartNo";
        sheet.Cell(2, 1).Value = "PN-999";
        workbook.SaveAs(excelPath);
    }
    File.SetLastWriteTimeUtc(excelPath, DateTime.UtcNow);

    var becameStale = await PollUntilAsync(() => vm.IsExcelDataStale, timeoutMs: 5000);
    AssertEqual(true, becameStale, "Editing the linked Excel file externally must eventually flag IsExcelDataStale");
    AssertEqual(true, vm.ExcelStaleNoticeText.Length > 0, "A stale notice message must be shown once flagged");

    // The app must not have reloaded the data on its own — ExcelDataView must still hold PN-100.
    var stillOldData = vm.ExcelDataView!.Cast<DataRowView>().Any(r => (string)r["PartNo"] == "PN-100");
    AssertEqual(true, stillOldData, "Watcher notice must not silently auto-reload data mid-session");

    // Explicitly refreshing must clear the stale flag and pick up the new data.
    await vm.RefreshExcelDataAsync();
    AssertEqual(false, vm.IsExcelDataStale, "Refreshing must clear the stale flag");
    var hasNewData = vm.ExcelDataView!.Cast<DataRowView>().Any(r => (string)r["PartNo"] == "PN-999");
    AssertEqual(true, hasNewData, "Refresh must pick up the externally edited data");

    try { Directory.Delete(dir, true); } catch { }
}

static async Task<bool> PollUntilAsync(Func<bool> condition, int timeoutMs)
{
    var elapsed = 0;
    const int step = 100;
    while (elapsed < timeoutMs)
    {
        if (condition())
        {
            return true;
        }

        await Task.Delay(step);
        elapsed += step;
    }

    return condition();
}

static async Task TestAddCurrentAsDataSourceIsIdempotent()
{
    // Audit fix (2026-07-03): clicking "Save current Excel link as shared source" more
    // than once (double-click, or clicking again after it was already added) must not
    // pile up duplicate registry entries pointing at the same file/sheet.
    var dir = Path.Combine(Environment.CurrentDirectory, "TestOutput", $"add-source-idempotent-{Guid.NewGuid():N}");
    Directory.CreateDirectory(dir);
    var registryPath = Path.Combine(dir, "data-sources.json");
    var excelPath = Path.Combine(dir, "data.xlsx");
    using (var workbook = new XLWorkbook())
    {
        var sheet = workbook.AddWorksheet("Parts");
        sheet.Cell(1, 1).Value = "PartNo";
        sheet.Cell(2, 1).Value = "PN-100";
        workbook.SaveAs(excelPath);
    }

    var vm = CreateViewModelWithRegistry(registryPath);
    await vm.ImportExcelAsync(excelPath, "Parts");

    vm.AddCurrentAsDataSourceCommand.Execute(null);
    AssertEqual(1, vm.DataSources.Count, "First click must create exactly one shared data source");
    var firstId = vm.Template.DatabaseConfig.DataSourceId;

    vm.AddCurrentAsDataSourceCommand.Execute(null);
    AssertEqual(1, vm.DataSources.Count, "Clicking again with the same file/sheet must not create a duplicate entry");
    AssertEqual(firstId, vm.Template.DatabaseConfig.DataSourceId, "Re-adding must point the template at the existing source, not a new one");

    try { Directory.Delete(dir, true); } catch { }
}

static async Task TestUnlinkExcelKeepsBindings()
{
    // database-manager-module-plan.md M1: Unlink must clear the file/sheet/rows/fields
    // so the template goes back to standalone, but must NOT touch object bindings —
    // re-importing a file with the same columns should resume working immediately.
    var dir = Path.Combine(Environment.CurrentDirectory, "TestOutput", $"unlink-excel-{Guid.NewGuid():N}");
    Directory.CreateDirectory(dir);
    var excelPath = Path.Combine(dir, "data.xlsx");
    using (var workbook = new XLWorkbook())
    {
        var sheet = workbook.AddWorksheet("Parts");
        sheet.Cell(1, 1).Value = "PartNo";
        sheet.Cell(2, 1).Value = "PN-100";
        workbook.SaveAs(excelPath);
    }

    var vm = new MainViewModel();
    await vm.ImportExcelAsync(excelPath, "Parts");

    var item = new LabelObject { Type = ObjectType.Text, Name = "Part", XMm = 1, YMm = 1, WidthMm = 20, HeightMm = 8 };
    vm.Template.Objects.Add(item);
    vm.SelectedObject = item;
    vm.BindSelectedAsExcelFieldCommand.Execute("PartNo");
    AssertEqual("{PartNo}", item.BindingExpression, "Setup: object should be bound to PartNo before unlinking");
    AssertEqual(true, vm.HasLinkedExcelSource, "Setup: template should report a linked Excel source before unlinking");

    vm.UnlinkExcel();

    AssertEqual(false, vm.HasLinkedExcelSource, "Unlink must clear the linked-source flag");
    AssertEqual(string.Empty, vm.Template.DatabaseConfig.FilePath, "Unlink must clear DatabaseConfig.FilePath");
    AssertEqual(string.Empty, vm.Template.DatabaseConfig.SheetName, "Unlink must clear DatabaseConfig.SheetName");
    AssertEqual(false, vm.HasExcelData, "Unlink must drop the in-memory Excel rows");
    AssertEqual(0, vm.LabelDatabaseFields.Count, "Unlink must clear the label database fields");
    AssertEqual("{PartNo}", item.BindingExpression, "Unlink must NOT touch the object's own BindingExpression");

    // Re-importing the same schema should resume without re-binding the object.
    await vm.ImportExcelAsync(excelPath, "Parts");
    AssertEqual(true, vm.HasLinkedExcelSource, "Re-import after unlink should restore the linked source");
    AssertEqual("{PartNo}", item.BindingExpression, "Re-import must not require re-binding the object");

    try { Directory.Delete(dir, true); } catch { }
}

static async Task TestUnlinkExcelWhenLinkBroken()
{
    // Unlink is the escape hatch for a permanently broken link (file deleted/moved with
    // no relink target available) — it must work even while IsExcelLinkBroken is true.
    var dir = Path.Combine(Environment.CurrentDirectory, "TestOutput", $"unlink-broken-{Guid.NewGuid():N}");
    Directory.CreateDirectory(dir);
    var excelPath = Path.Combine(dir, "data.xlsx");
    using (var workbook = new XLWorkbook())
    {
        var sheet = workbook.AddWorksheet("Parts");
        sheet.Cell(1, 1).Value = "PartNo";
        sheet.Cell(2, 1).Value = "PN-100";
        workbook.SaveAs(excelPath);
    }

    var vm = new MainViewModel();
    await vm.ImportExcelAsync(excelPath, "Parts");
    var templatePath = Path.Combine(dir, "template.anlabel");
    await vm.SaveAsync(templatePath);

    File.Delete(excelPath);

    var vm2 = new MainViewModel();
    await vm2.OpenAsync(templatePath);
    AssertEqual(true, vm2.IsExcelLinkBroken, "Setup: link must be broken after the Excel file is deleted");
    AssertEqual(true, vm2.HasLinkedExcelSource, "Setup: template should still report a (broken) linked source");

    vm2.UnlinkExcel();

    AssertEqual(false, vm2.IsExcelLinkBroken, "Unlink must clear the broken-link flag");
    AssertEqual(false, vm2.HasLinkedExcelSource, "Unlink must clear the linked-source flag even when the link was broken");
    AssertEqual(string.Empty, vm2.Template.DatabaseConfig.FilePath, "Unlink must clear DatabaseConfig.FilePath even when the link was broken");

    try { Directory.Delete(dir, true); } catch { }
}

static async Task TestDataSourceRecordsRecentUsage()
{
    // database-manager-module-plan.md M3: using a shared source must record LastUsedUtc
    // and push the current template's path onto RecentTemplates, so the Database Manager
    // can tell which templates would be affected by removing a source, and Clean Up can
    // tell which sources are actually stale.
    var dir = Path.Combine(Environment.CurrentDirectory, "TestOutput", $"usage-tracking-{Guid.NewGuid():N}");
    Directory.CreateDirectory(dir);
    var registryPath = Path.Combine(dir, "data-sources.json");
    var excelPath = Path.Combine(dir, "data.xlsx");
    using (var workbook = new XLWorkbook())
    {
        var sheet = workbook.AddWorksheet("Parts");
        sheet.Cell(1, 1).Value = "PartNo";
        sheet.Cell(2, 1).Value = "PN-100";
        workbook.SaveAs(excelPath);
    }

    var vm = CreateViewModelWithRegistry(registryPath);
    await vm.ImportExcelAsync(excelPath, "Parts");
    vm.AddCurrentAsDataSourceCommand.Execute(null);
    var source = vm.DataSources.Single();
    AssertEqual(true, source.LastUsedUtc is null, "A freshly-created source must not report a usage time yet");

    var templatePath = Path.Combine(dir, "template.anlabel");
    await vm.SaveAsync(templatePath);

    var before = DateTime.UtcNow;
    vm.UseDataSourceCommand.Execute(source);
    await PollUntilAsync(() => source.LastUsedUtc is not null, 2000);

    AssertEqual(true, source.LastUsedUtc is not null, "Using a shared source must set LastUsedUtc");
    AssertEqual(true, source.LastUsedUtc >= before.AddSeconds(-1), "LastUsedUtc must reflect the time of use, not an old default");
    AssertEqual(1, source.RecentTemplates.Count, "RecentTemplates must record the template that used this source");
    AssertEqual(templatePath, source.RecentTemplates[0], "RecentTemplates must contain the current template's path");

    // Using it again from the same template must not duplicate the entry.
    vm.UseDataSourceCommand.Execute(source);
    await PollUntilAsync(() => source.RecentTemplates.Count == 1, 2000);
    AssertEqual(1, source.RecentTemplates.Count, "Re-using from the same template must not duplicate RecentTemplates entries");

    // The usage record must survive a save/reload of the registry.
    var registry2 = new DataSourceRegistry(registryPath);
    registry2.Load();
    var reloaded = registry2.GetById(source.Id);
    AssertEqual(true, reloaded is not null, "Reloaded registry must still contain the source");
    AssertEqual(true, reloaded!.LastUsedUtc is not null, "LastUsedUtc must survive a registry save/reload round trip");
    AssertEqual(templatePath, reloaded.RecentTemplates.FirstOrDefault() ?? string.Empty, "RecentTemplates must survive a registry save/reload round trip");

    try { Directory.Delete(dir, true); } catch { }
}

static Task TestRegistryForwardCompatibility()
{
    // A registry file written before LastUsedUtc/RecentTemplates existed (or hand-edited
    // to omit them) must still load without throwing, defaulting to "never used".
    var dir = Path.Combine(Environment.CurrentDirectory, "TestOutput", $"registry-compat-{Guid.NewGuid():N}");
    Directory.CreateDirectory(dir);
    var registryPath = Path.Combine(dir, "data-sources.json");
    File.WriteAllText(registryPath, """
        [
          {
            "Id": "old-source-1",
            "Name": "Legacy Source",
            "FilePath": "C:\\data\\legacy.xlsx",
            "SheetName": "Sheet1",
            "HeaderRowIndex": 1
          }
        ]
        """);

    var registry = new DataSourceRegistry(registryPath);
    registry.Load();
    var loaded = registry.GetById("old-source-1");
    AssertEqual(true, loaded is not null, "A pre-M3 registry entry must still load");
    AssertEqual(true, loaded!.LastUsedUtc is null, "Missing LastUsedUtc must default to null, not throw");
    AssertEqual(0, loaded.RecentTemplates.Count, "Missing RecentTemplates must default to an empty list, not throw");

    // Re-saving must not corrupt the file for a subsequent load.
    registry.Save();
    var registry2 = new DataSourceRegistry(registryPath);
    registry2.Load();
    AssertEqual(1, registry2.Sources.Count, "Re-saved registry must still round-trip cleanly");

    try { Directory.Delete(dir, true); } catch { }
    return Task.CompletedTask;
}

static async Task TestImportKeepsCustomHeaderRow()
{
    // Bug fix (2026-07-03): ImportExcelAsync used to unconditionally reset
    // Template.DatabaseConfig.HeaderRowIndex back to 1 after every successful import,
    // silently discarding a non-default header row configured via a shared data source
    // (Database Manager M2) — the very next Import/Refresh would then try to read
    // headers from the wrong row.
    var dir = Path.Combine(Environment.CurrentDirectory, "TestOutput", $"header-row-{Guid.NewGuid():N}");
    Directory.CreateDirectory(dir);
    var registryPath = Path.Combine(dir, "data-sources.json");
    var excelPath = Path.Combine(dir, "data.xlsx");
    using (var workbook = new XLWorkbook())
    {
        var sheet = workbook.AddWorksheet("Parts");
        sheet.Cell(1, 1).Value = "Report generated 2026-07-03";
        sheet.Cell(3, 1).Value = "PartNo";
        sheet.Cell(4, 1).Value = "PN-100";
        workbook.SaveAs(excelPath);
    }

    var vm = CreateViewModelWithRegistry(registryPath);
    var source = new DataSource { FilePath = excelPath, SheetName = "Parts", HeaderRowIndex = 3, Name = "Header row 3" };
    vm.DataSources.Add(source);

    vm.UseDataSourceCommand.Execute(source);
    await PollUntilAsync(() => vm.HasExcelData, 2000);

    AssertEqual(3, vm.Template.DatabaseConfig.HeaderRowIndex, "Import must not reset a custom header row back to 1");
    AssertEqual(true, vm.ExcelHeaders.Contains("PartNo"), "Header row 3 must be read as the actual header, not row 1");

    // Re-import (as a plain Refresh/re-open would) must keep reading with the same
    // header row — if the bug were present, HeaderRowIndex would already be back to 1
    // by this point and this second read would silently pick up the wrong header.
    await vm.ImportExcelAsync(excelPath, "Parts");
    AssertEqual(3, vm.Template.DatabaseConfig.HeaderRowIndex, "A second import must keep the custom header row");
    AssertEqual(true, vm.ExcelHeaders.Contains("PartNo"), "A second import must still read the correct header row");

    try { Directory.Delete(dir, true); } catch { }
}

static MainViewModel CreateViewModelWithRegistry(string registryPath)
{
    return new MainViewModel(
        new ANLAbel.Project.SaveLoad.ProjectFileService(),
        new ExcelDataService(),
        new ANLAbel.Printing.PrinterProfiles.PrintService(),
        new ANLAbel.Printing.PrinterProfiles.PrinterDiscoveryService(),
        new PrintLogService(),
        new DataOperationLogService(),
        new DataSourceRegistry(registryPath));
}

static async Task TestSharedDataSourceRelinkFixesTemplate()
{
    // database-plan GĐ2 item 4: the payoff of a shared data source is that relinking
    // it once (e.g. the Excel file moved) fixes every template referencing it, instead
    // of each template needing its own relink.
    var dir = Path.Combine(Environment.CurrentDirectory, "TestOutput", $"shared-source-{Guid.NewGuid():N}");
    Directory.CreateDirectory(dir);
    var registryPath = Path.Combine(dir, "data-sources.json");
    var originalExcelPath = Path.Combine(dir, "original.xlsx");
    var movedExcelPath = Path.Combine(dir, "moved.xlsx");
    var templatePath = Path.Combine(dir, "template.anlabel");

    using (var workbook = new XLWorkbook())
    {
        var sheet = workbook.AddWorksheet("Parts");
        sheet.Cell(1, 1).Value = "PartNo";
        sheet.Cell(2, 1).Value = "PN-ORIGINAL";
        workbook.SaveAs(originalExcelPath);
    }

    var vm1 = CreateViewModelWithRegistry(registryPath);
    await vm1.ImportExcelAsync(originalExcelPath, "Parts");
    vm1.AddCurrentAsDataSourceCommand.Execute(null);

    AssertEqual(1, vm1.DataSources.Count, "Adding the current Excel link must create one shared data source");
    var sourceId = vm1.Template.DatabaseConfig.DataSourceId;
    AssertEqual(false, string.IsNullOrWhiteSpace(sourceId), "Template must reference the new shared source by Id");

    await vm1.SaveAsync(templatePath);

    // Simulate the shared file being moved: create it at a new path with different
    // data, then relink the registry entry directly (same effect as RelinkDataSourceAsync
    // without needing an interactive file dialog).
    using (var workbook = new XLWorkbook())
    {
        var sheet = workbook.AddWorksheet("Parts");
        sheet.Cell(1, 1).Value = "PartNo";
        sheet.Cell(2, 1).Value = "PN-MOVED";
        workbook.SaveAs(movedExcelPath);
    }

    var registryForRelink = new DataSourceRegistry(registryPath);
    registryForRelink.Load();
    var sharedSource = registryForRelink.GetById(sourceId);
    AssertEqual(false, sharedSource is null, "Shared source must be found in the persisted registry");
    sharedSource!.FilePath = movedExcelPath;
    registryForRelink.Upsert(sharedSource);
    registryForRelink.Save();

    // Open the template fresh (as if in a new session) — it must pick up the relinked
    // path automatically via DataSourceId, even though its own saved FilePath still
    // points at the original (now-stale) location.
    var vm2 = CreateViewModelWithRegistry(registryPath);
    await vm2.OpenAsync(templatePath);

    AssertEqual(false, vm2.IsExcelLinkBroken, "Template must resolve via the relinked shared source, not report a broken link");
    AssertEqual(movedExcelPath, vm2.Template.DatabaseConfig.FilePath, "Template's FilePath must be updated to the shared source's new location");
    var hasMovedData = vm2.ExcelDataView!.Cast<DataRowView>().Any(r => (string)r["PartNo"] == "PN-MOVED");
    AssertEqual(true, hasMovedData, "Template must load data from the relinked path, not the stale original path");

    try { Directory.Delete(dir, true); } catch { }
}

static async Task TestQuickPrintBlocksOnStaleData()
{
    // print-preview-reliability-plan R2: the ribbon "Print Current Row"/"Print All Rows"
    // shortcuts skip the Print Preview dialog entirely, so they must not silently print
    // stale data if the linked Excel file changed since the last read.
    var dir = Path.Combine(Environment.CurrentDirectory, "TestOutput", $"quickprint-stale-{Guid.NewGuid():N}");
    Directory.CreateDirectory(dir);
    var excelPath = Path.Combine(dir, "data.xlsx");
    using (var workbook = new XLWorkbook())
    {
        var sheet = workbook.AddWorksheet("Parts");
        sheet.Cell(1, 1).Value = "PartNo";
        sheet.Cell(2, 1).Value = "PN-100";
        workbook.SaveAs(excelPath);
    }

    var vm = new MainViewModel();
    await vm.ImportExcelAsync(excelPath, "Parts");

    await Task.Delay(50);
    using (var workbook = new XLWorkbook())
    {
        var sheet = workbook.AddWorksheet("Parts");
        sheet.Cell(1, 1).Value = "PartNo";
        sheet.Cell(2, 1).Value = "PN-999";
        workbook.SaveAs(excelPath);
    }
    File.SetLastWriteTimeUtc(excelPath, DateTime.UtcNow);

    var becameStale = await PollUntilAsync(() => vm.IsExcelDataStale, timeoutMs: 5000);
    AssertEqual(true, becameStale, "Setup: the watcher must flag the data stale before this test can check the print-block behavior");

    vm.PrintCurrentRowCommand.Execute(null);
    AssertEqual(true, vm.StatusText.Contains("Print blocked", StringComparison.OrdinalIgnoreCase), "PrintCurrentRow must refuse to print while the linked Excel data is stale");

    vm.PrintAllRowsCommand.Execute(null);
    AssertEqual(true, vm.StatusText.Contains("Print blocked", StringComparison.OrdinalIgnoreCase), "PrintAllRows must refuse to print while the linked Excel data is stale");

    try { Directory.Delete(dir, true); } catch { }
}

static async Task TestPrintOperationLogRecordsJob()
{
    // print-preview-reliability-plan item 3: every print job must leave a machine-parseable
    // trace (separate from the human-facing print-history.xlsx) so a bad print run can be
    // traced back to which template/printer/DPI produced it.
    var dir = Path.Combine(Environment.CurrentDirectory, "TestOutput", $"print-op-log-{Guid.NewGuid():N}");
    Directory.CreateDirectory(dir);
    var logPath = Path.Combine(dir, "print-operations.jsonl");

    var vm = new MainViewModel(
        new ANLAbel.Project.SaveLoad.ProjectFileService(),
        new ExcelDataService(),
        new ANLAbel.Printing.PrinterProfiles.PrintService(),
        new ANLAbel.Printing.PrinterProfiles.PrinterDiscoveryService(),
        new PrintLogService(Path.Combine(dir, "print-history.xlsx")),
        new DataOperationLogService(),
        dataSourceRegistry: null,
        printOperationLogService: new PrintOperationLogService(logPath));

    vm.Template.PrinterProfile.PrinterName = "Test Printer";
    vm.Template.PrinterProfile.Dpi = 203;
    var rows = new IReadOnlyDictionary<string, string>?[] { new Dictionary<string, string> { ["PartNo"] = "PN-100" } };

    await vm.WritePrintLogAsync("Current row", rows, rowCount: 1, labelCount: 1);

    var lines = await WaitForLogLinesAsync(logPath, minLineCount: 1);
    AssertEqual(true, lines.Length >= 1, "A print job must produce a log line");
    AssertEqual(true, lines[0].Contains("\"PrinterName\":\"Test Printer\"", StringComparison.Ordinal), "Log entry must record the printer name");
    AssertEqual(true, lines[0].Contains("\"PrintMode\":\"Current row\"", StringComparison.Ordinal), "Log entry must record the print mode");
    AssertEqual(true, lines[0].Contains("\"Success\":true", StringComparison.Ordinal), "A successful print must be logged as success");

    try { Directory.Delete(dir, true); } catch { }
}

static async Task TestKeyFieldSelectionTracksRow()
{
    // database-plan TC5: once the user picks a KeyField, the selected row must
    // survive a refresh even if rows above it were inserted/removed in Excel
    // (index-based tracking alone would silently jump to the wrong record).
    var dir = Path.Combine(Environment.CurrentDirectory, "TestOutput", $"keyfield-{Guid.NewGuid():N}");
    Directory.CreateDirectory(dir);
    var excelPath = Path.Combine(dir, "data.xlsx");
    using (var workbook = new XLWorkbook())
    {
        var sheet = workbook.AddWorksheet("Parts");
        sheet.Cell(1, 1).Value = "PartNo";
        sheet.Cell(1, 2).Value = "Qty";
        sheet.Cell(2, 1).Value = "PN-100";
        sheet.Cell(2, 2).Value = "10";
        sheet.Cell(3, 1).Value = "PN-200";
        sheet.Cell(3, 2).Value = "20";
        workbook.SaveAs(excelPath);
    }

    var vm = new MainViewModel();
    await vm.ImportExcelAsync(excelPath, "Parts");

    AssertEqual(true, vm.KeyFieldOptions.Contains(string.Empty), "KeyFieldOptions must offer a blank entry to clear the key");
    AssertEqual(true, vm.KeyFieldOptions.Contains("PartNo"), "KeyFieldOptions must include the imported column names");

    // Select PartNo as the key, then select the PN-200 row.
    vm.SelectedKeyFieldName = "PartNo";
    AssertEqual("PartNo", vm.Template.DatabaseConfig.KeyField, "Setting SelectedKeyFieldName must update DatabaseConfig.KeyField");

    var rows = vm.ExcelDataView!.Cast<DataRowView>().ToArray();
    var pn200Row = rows.First(r => (string)r["PartNo"] == "PN-200");
    vm.SelectedDataItem = pn200Row;
    AssertEqual("PN-200", vm.Template.DatabaseConfig.KeyValue, "Selecting a row must record its key value once a KeyField is set");

    // Insert a new row above PN-200 in the workbook, then refresh — PN-200 must
    // stay selected even though its row index shifted.
    await Task.Delay(50);
    using (var workbook = new XLWorkbook())
    {
        var sheet = workbook.AddWorksheet("Parts");
        sheet.Cell(1, 1).Value = "PartNo";
        sheet.Cell(1, 2).Value = "Qty";
        sheet.Cell(2, 1).Value = "PN-050";
        sheet.Cell(2, 2).Value = "5";
        sheet.Cell(3, 1).Value = "PN-100";
        sheet.Cell(3, 2).Value = "10";
        sheet.Cell(4, 1).Value = "PN-200";
        sheet.Cell(4, 2).Value = "20";
        workbook.SaveAs(excelPath);
    }
    File.SetLastWriteTimeUtc(excelPath, DateTime.UtcNow);

    await vm.RefreshExcelDataAsync();
    var selectedAfterRefresh = (DataRowView)vm.SelectedDataItem!;
    AssertEqual("PN-200", (string)selectedAfterRefresh["PartNo"], "Row must still be tracked by KeyField after a row was inserted above it");

    try { Directory.Delete(dir, true); } catch { }
}

static Task TestLayerForwardBackward()
{
    // properties-panel-plan Đợt C: Forward/Backward must swap with the immediate
    // neighbor above/below, leaving other objects' order untouched.
    var vm = new MainViewModel();
    var back = new LabelObject { Type = ObjectType.Text, Name = "Back", ZIndex = 1 };
    var middle = new LabelObject { Type = ObjectType.Text, Name = "Middle", ZIndex = 2 };
    var front = new LabelObject { Type = ObjectType.Text, Name = "Front", ZIndex = 3 };
    vm.Template.Objects.Add(back);
    vm.Template.Objects.Add(middle);
    vm.Template.Objects.Add(front);

    vm.SelectedObject = middle;
    vm.BringForwardCommand.Execute(null);
    AssertEqual(3, middle.ZIndex, "Bring Forward must swap ZIndex with the object above");
    AssertEqual(2, front.ZIndex, "The object that was above must take the old ZIndex");
    AssertEqual(1, back.ZIndex, "The object below must be untouched by Bring Forward");

    vm.SendBackwardCommand.Execute(null);
    vm.SendBackwardCommand.Execute(null);
    AssertEqual(1, middle.ZIndex, "Send Backward (applied twice) must move the object below the original bottom object");
    AssertEqual(2, back.ZIndex, "Swapped neighbor must take the vacated ZIndex");

    return Task.CompletedTask;
}

static Task TestSetRotationCommand()
{
    // properties-panel-plan Đợt C: the 4 quick buttons must set the exact degree
    // value passed as the command parameter, without needing SelectedObject.Rotation
    // bound through a ComboBox.
    var vm = new MainViewModel();
    var text = new LabelObject { Type = ObjectType.Text, Rotation = 0 };
    vm.Template.Objects.Add(text);
    vm.SelectedObject = text;

    vm.SetRotationCommand.Execute("90");
    AssertEqual(90, text.Rotation, "SetRotationCommand(\"90\") must set Rotation to 90");

    vm.SetRotationCommand.Execute("270");
    AssertEqual(270, text.Rotation, "SetRotationCommand(\"270\") must set Rotation to 270");

    return Task.CompletedTask;
}

static Task TestBarcodeModuleSizeWarningMatchesPrintDpi()
{
    // Audit fix (2026-07-03): BarcodeModuleSizeWarningText used to read only
    // Template.Dpi, but the real print-time DPI resolution (PrintService.CreatePlan,
    // used by PrintPreflightValidator) prioritizes Template.PrinterProfile.Dpi — the
    // field the "Label printer setup..." dialog in Print Preview actually updates
    // (PrintPreviewWindow.xaml.cs only ever sets PrinterProfile.Dpi, never Template.Dpi).
    // If the two DPI fields diverge, the Designer-side warning must follow
    // PrinterProfile.Dpi so it agrees with what preflight will actually enforce.
    var vm = new MainViewModel();
    vm.Template.Dpi = 600; // would NOT trigger the warning below if used
    vm.Template.PrinterProfile.Dpi = 203; // the real print DPI once PrinterSetupWindow sets it

    var qr = new LabelObject
    {
        Type = ObjectType.QRCode,
        QrSizingMode = QrSizingMode.FixedVersionAndModuleSize,
        QrModuleSizePx = 2,
        QrDpi = 300
    };
    vm.Template.Objects.Add(qr);
    vm.SelectedObject = qr;

    // 2 px module at 300 DPI => 2*203/300 ≈ 1.35 dots at the real print DPI (warn),
    // but 2*600/300 = 4 dots if Template.Dpi were used instead (no warn) — these two
    // outcomes disagree, so this test fails if PrinterProfile.Dpi is not honored.
    AssertEqual(true, vm.BarcodeModuleSizeWarningText.Length > 0, "Warning must use PrinterProfile.Dpi (the real print DPI), not Template.Dpi, when the two diverge");

    return Task.CompletedTask;
}

static Task TestBarcodeModuleSizeWarning()
{
    // print-preview-reliability-plan R5 / properties-panel-plan Đợt C: a fixed-size
    // matrix module that would print at under ~2 physical dots on this label's DPI
    // is effectively unscannable on industrial thermal printers and must be flagged
    // in the Properties panel, not just discovered after a bad print run.
    var vm = new MainViewModel();
    AssertEqual(203, vm.Template.Dpi, "Test assumes the default template DPI (203, a common industrial thermal printer resolution)");

    var qr = new LabelObject
    {
        Type = ObjectType.QRCode,
        QrSizingMode = QrSizingMode.FixedVersionAndModuleSize,
        QrModuleSizePx = 2,
        QrDpi = 300
    };
    vm.Template.Objects.Add(qr);
    vm.SelectedObject = qr;

    // 2 px module designed at 300 DPI, printed at 203 DPI => 2 * 203 / 300 ≈ 1.35 dots.
    AssertEqual(true, vm.BarcodeModuleSizeWarningText.Length > 0, "A sub-2-dot module must produce a warning");

    qr.QrModuleSizePx = 6;
    // 6 px module at 300 DPI, printed at 203 DPI => 6 * 203 / 300 ≈ 4.06 dots.
    AssertEqual(string.Empty, vm.BarcodeModuleSizeWarningText, "A comfortably-sized module must not warn");

    qr.QrModuleSizePx = 2;
    qr.QrSizingMode = QrSizingMode.AutoSizeByData;
    AssertEqual(string.Empty, vm.BarcodeModuleSizeWarningText, "Auto-sized barcodes are not user-controlled by Module px, so they must not warn here");

    return Task.CompletedTask;
}

static async Task TestDatabaseConfigRoundTrip()
{
    // database-plan TC2: every DatabaseConfig field must survive a full save/open
    // cycle, not just the handful (FilePath/SheetName/LastSelectedRow) already
    // covered by "template save/load". A dropped field here silently breaks
    // relink, key-based row tracking, or the shared data-source registry.
    var service = new ProjectFileService();
    var directory = Path.Combine(Environment.CurrentDirectory, "TestOutput");
    Directory.CreateDirectory(directory);
    var filePath = Path.Combine(directory, $"db-config-roundtrip-{Guid.NewGuid():N}.anlabel");

    var template = new LabelTemplate { Name = "DB Config Round Trip", WidthMm = 60, HeightMm = 40 };
    template.DatabaseConfig.DataSourceId = "src-001";
    template.DatabaseConfig.FilePath = @"C:\labels\data\parts.xlsx";
    template.DatabaseConfig.RelativePath = "parts.xlsx";
    template.DatabaseConfig.SheetName = "Parts";
    template.DatabaseConfig.HeaderRowIndex = 2;
    template.DatabaseConfig.KeyField = "PartNo";
    template.DatabaseConfig.KeyValue = "PN-100";
    template.DatabaseConfig.CopiesField = "Qty";
    template.DatabaseConfig.LastSelectedRow = 3;
    template.DatabaseConfig.AvailableFields.Add(new DatabaseField { Name = "PartNo", DisplayName = "Part No", SampleValue = "PN-100" });
    template.DatabaseConfig.AvailableFields.Add(new DatabaseField { Name = "Qty", DisplayName = "Quantity", SampleValue = "10" });
    template.DatabaseConfig.LabelFields.Add(new DatabaseField { Name = "PartNo", DisplayName = "Part No", SampleValue = "PN-100" });

    await service.SaveAsync(template, filePath);
    var loaded = await service.LoadAsync(filePath);

    AssertEqual("src-001", loaded.DatabaseConfig.DataSourceId, "DataSourceId must survive save/open");
    AssertEqual(@"C:\labels\data\parts.xlsx", loaded.DatabaseConfig.FilePath, "FilePath must survive save/open");
    AssertEqual("parts.xlsx", loaded.DatabaseConfig.RelativePath, "RelativePath must survive save/open");
    AssertEqual("Parts", loaded.DatabaseConfig.SheetName, "SheetName must survive save/open");
    AssertEqual(2, loaded.DatabaseConfig.HeaderRowIndex, "HeaderRowIndex must survive save/open");
    AssertEqual("PartNo", loaded.DatabaseConfig.KeyField, "KeyField must survive save/open");
    AssertEqual("PN-100", loaded.DatabaseConfig.KeyValue, "KeyValue must survive save/open");
    AssertEqual("Qty", loaded.DatabaseConfig.CopiesField, "CopiesField must survive save/open");
    AssertEqual(3, loaded.DatabaseConfig.LastSelectedRow, "LastSelectedRow must survive save/open");
    AssertEqual(2, loaded.DatabaseConfig.AvailableFields.Count, "AvailableFields must survive save/open");
    AssertEqual(1, loaded.DatabaseConfig.LabelFields.Count, "LabelFields must survive save/open");
    AssertEqual("PartNo", loaded.DatabaseConfig.LabelFields[0].Name, "LabelFields entries must keep their Name");
    AssertEqual("Part No", loaded.DatabaseConfig.LabelFields[0].DisplayName, "LabelFields entries must keep their DisplayName");

    try { File.Delete(filePath); } catch { }
}

static async Task TestExcelRefreshSkipsUnchangedFile()
{
    // Scenario: RefreshExcelDataAsync must not re-read the workbook when its
    // LastWriteTimeUtc has not changed since the last import (database-plan TC4) —
    // this avoids unnecessary I/O and gives the user a "data is fresh" signal.
    var dir = Path.Combine(Environment.CurrentDirectory, "TestOutput", $"refresh-cache-{Guid.NewGuid():N}");
    Directory.CreateDirectory(dir);
    var excelPath = Path.Combine(dir, "data.xlsx");
    using (var workbook = new XLWorkbook())
    {
        var sheet = workbook.AddWorksheet("Parts");
        sheet.Cell(1, 1).Value = "PartNo";
        sheet.Cell(2, 1).Value = "PN-100";
        workbook.SaveAs(excelPath);
    }

    var vm = new MainViewModel();
    await vm.ImportExcelAsync(excelPath, "Parts");
    var afterImportStatus = vm.StatusText;
    AssertEqual(true, afterImportStatus.StartsWith("Imported", StringComparison.Ordinal),
        "First import must report rows imported");

    // Refresh again without touching the file: must short-circuit, not re-import.
    await vm.RefreshExcelDataAsync();
    AssertEqual(true, vm.StatusText.Contains("already up to date", StringComparison.OrdinalIgnoreCase),
        "Refresh must report the cached/unchanged state instead of re-reading an untouched file");

    // Touch the file (new content + newer write time) then refresh: must re-import.
    await Task.Delay(50);
    using (var workbook = new XLWorkbook())
    {
        var sheet = workbook.AddWorksheet("Parts");
        sheet.Cell(1, 1).Value = "PartNo";
        sheet.Cell(2, 1).Value = "PN-200";
        workbook.SaveAs(excelPath);
    }
    File.SetLastWriteTimeUtc(excelPath, DateTime.UtcNow);

    await vm.RefreshExcelDataAsync();
    AssertEqual(true, vm.StatusText.StartsWith("Imported", StringComparison.Ordinal),
        "Refresh must re-import once the linked file's write time has changed");

    try { Directory.Delete(dir, true); } catch { }
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
