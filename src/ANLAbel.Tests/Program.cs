using ANLAbel.Barcode.Options;
using ANLAbel.Barcode.Renderers;
using ANLAbel.Core.Barcode;
using ANLAbel.Core.Enums;
using ANLAbel.Core.Expressions;
using ANLAbel.Core.Expressions.Formulas;
using ANLAbel.Core.Geometry;
using ANLAbel.Core.Models;
using ANLAbel.Core.Printing;
using ANLAbel.Core.Scene;
using ANLAbel.Core.Text;
using ANLAbel.Core.Updates;
using ANLAbel.Data.DataLogs;
using ANLAbel.Data.Excel;
using System.Data;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using ANLAbel.Data.PrintLogs;
using ANLAbel.App.ViewModels;
using ANLAbel.App;
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
    ("release metadata stays synchronized", TestReleaseMetadataIsSynchronized),
    ("github release update parser and comparison", TestGitHubReleaseUpdateParser),
    ("template schema envelope migrates legacy and rejects future versions", TestTemplateSchemaEnvelope),
    ("wrong format envelope must not recover from backup", TestWrongFormatEnvelopeDoesNotRecoverFromBackup),
    ("template backup rotates and recovery is explicit", TestTemplateBackupRecovery),
    ("template revision history validates and rolls back atomically", TestTemplateRevisionHistoryAndRollback),
    ("failed template open preserves the current document", TestFailedTemplateOpenPreservesCurrentDocument),
    ("persistent ruler guides save, hash and one gesture", TestPersistentRulerGuides),
    ("keyboard nudge uses physical steps and one gesture", TestKeyboardNudgePrecisionAndGesture),
    ("xlsx import", TestXlsxImport),
    ("csv import supports UTF-8, quoted commas and semicolon delimiter", TestCsvImport),
    ("excel object binding preview", TestExcelObjectBindingPreview),
    ("barcode render validation", TestBarcodeRender),
    ("qr fills authored frame when object is enlarged", TestQrFillsAuthoredFrameWhenEnlarged),
    ("barcode render preserves non-square effective DPI", TestBarcodeRenderPreservesNonSquareDpi),
    ("barcode application profile preflight", TestBarcodeApplicationProfilePreflight),
    ("code 39 lowercase input decodes correctly in standard mode", TestCode39LowercaseDecodesCorrectly),
    ("print visual render", TestPrintVisualRender),
    ("compiled print renderer preserves text box frame policy", TestCompiledRendererPreservesTextFramePolicy),
    ("preview drawing snapshot is frozen", TestPreviewDrawingSnapshotIsFrozen),
    ("preview raster worker is cancellable", TestPreviewRasterWorker),
    ("preview raster carries a deterministic golden identity", TestPreviewRasterCarriesGoldenIdentity),
    ("preview raster rejects invalid dimensions", TestPreviewRasterRejectsInvalidDimensions),
    ("preview metadata stays lazy at 10k pages", TestPreviewMetadataStaysLazyAt10k),
    ("preview raster coalesces a 10k request burst", TestPreviewRasterCoalesces10kRequests),
    ("preview 10k stress stays within memory and cancel budget", TestPreview10kStressBudget),
    ("preview raster long-soak reuses worker and clears queue", TestPreviewRasterLongSoak),
    ("designer and print share text layout bounds", TestTextLayoutPolicyParity),
    ("text padding uses one physical shared frame", TestTextPaddingUsesSharedFrame),
    ("static text renderer honors physical left padding", TestStaticTextRendererHonorsPadding),
    ("text layout identity is stable across repeated render paths", TestTextLayoutIdentityFingerprint),
    ("text baseline snap uses shared text metrics", TestTextBaselineSnapUsesSharedMetrics),
    ("optical text alignment uses visible ink and one edit gesture", TestOpticalTextAlignment),
    ("smart spacing and grid snap use semantic priority", TestSmartSpacingAndGridSnap),
    ("rotated group move uses transformed hull bounds", TestRotatedGroupMoveUsesTransformedBounds),
    ("line stroke bounds stay aligned with print preflight", TestLineStrokeBoundsParity),
    ("text direction resolves RTL and survives save/load", TestTextDirectionPolicy),
    ("print preview follows design label size", TestPrintPreviewUsesDesignLabelSize),
    ("print renderer keeps edge content", TestPrintRendererKeepsEdgeContent),
    ("print barcode uses plan (real print) dpi", TestPrintBarcodeUsesPlanDpi),
    ("vector barcode geometry uses independent device dots", TestVectorBarcodeGeometryUsesDeviceDots),
    ("barcode HRI reserves a shared symbol layout", TestBarcodeHriReservesSharedLayout),
    ("barcode HRI above reserves top strip", TestBarcodeHriAboveReservesTopStrip),
    ("barcode HRI placement survives clone and save", TestBarcodeHriPlacementSurvivesCloneAndSave),
    ("barcode check digit verify fails closed in preflight", TestBarcodeCheckDigitVerifyFailsClosedInPreflight),
    ("barcode HRI hide check digit does not alter modules", TestBarcodeHriHideCheckDigitDoesNotAlterModules),
    ("print preflight blocks object outside label", TestPrintPreflightBlocksObjectOutsideLabel),
    ("print preflight blocks text outside label", TestPrintPreflightBlocksTextOutsideLabel),
    ("print preflight blocks undersized bound matrix barcode", TestPrintPreflightBlocksUndersizedBoundMatrixBarcode),
    ("print preflight uses exact fixed QR capacity", TestPrintPreflightUsesExactFixedQrCapacity),
    ("Text stays free while TextBox stays bounded", TestFixedFrameTextReportsOverflow),
    ("ellipsis text overflow stays bounded without blocking", TestEllipsisTextOverflowPolicy),
    ("shrink-font text stays bounded without mutating authored size", TestShrinkFontTextFit),
    ("scale-width text preserves font and fits horizontally", TestScaleWidthTextFit),
    ("Text shrink-frame compresses glyphs", TestTextShrinkFrameCompressesGlyphs),
    ("Text border-drag locks frame and compresses glyphs", TestTextBorderDragLocksFrameAndCompressesGlyphs),
    ("TextBox fit modes preserve frame and honor configured bounds", TestTextBoxFitModesPreserveFrame),
    ("snap tolerance and hysteresis remain stable across zoom", TestSnapToleranceStableAcrossZoom),
    ("drawing endpoints share object and grid snap contract", TestDrawingPointSnapContract),
    ("line dragging shares object snap and stroke bounds", TestLineDraggingUsesSharedSnapContract),
    ("pointer frame telemetry remains bounded by zoom and display scale", TestPointerFrameTelemetry),
    ("pointer telemetry overlay is opt-in and render-safe", TestPointerTelemetryOverlay),
    ("designer style edits refresh the text visual", TestDesignerStyleEditsRefreshVisual),
    ("text autofit includes authored physical padding", TestTextAutoFitIncludesPhysicalPadding),
    ("text box does not resize object from text content", TestTextBoxDoesNotResizeFromText),
    ("text box reflows to fit frame when user resizes", TestTextBoxReflowsWhenUserResizes),
    ("normal resize capture release does not cancel gesture", TestNormalResizeCaptureReleaseDoesNotCancelGesture),
    ("new text box uses compact label-aware frame", TestNewTextBoxUsesCompactLabelAwareFrame),
    ("text box has no outline stroke by default", TestTextBoxHasNoOutlineStroke),
    ("fixed frame text box reports overflow without rewriting size", TestFixedFrameTextBoxOverflowKeepsSize),
    ("printer setup preserves a saved standard stock selection", TestPrinterSetupPreservesSavedStock),
    ("text layout identity records display pixels-per-DIP", TestTextLayoutRecordsDisplayScale),
    ("print preflight validation", TestPrintPreflightValidation),
    ("print preflight reports missing text font", TestPrintPreflightReportsMissingFont),
    ("print preflight reports missing glyph coverage", TestPrintPreflightReportsMissingGlyph),
    ("font catalog install/remove and Unicode policy", TestFontCatalogAndUnicodePolicy),
    ("image preflight validates decode and effective PPI", TestImagePreflightValidatesResolution),
    ("image raster policy is deterministic and carried by the print identity", TestImageRasterPolicyIdentity),
    ("image alpha compositing and 1-bpp modes share deterministic raster policy", TestImageRasterAlphaAndMonochromeFixtures),
    ("print preflight honors cancellation and progress", TestPrintPreflightHonorsCancellation),
    ("print log CSV append is fast and escapes fields correctly", TestPrintLogAppend),
    ("print log exports to a readable Excel report", TestPrintLogExportToExcel),
    ("template library standalone (no sample-data link)", TestTemplateLibraryStandalone),
    ("template excel link survives folder move", TestExcelLinkSurvivesFolderMove),
    ("data source registry CRUD", TestDataSourceRegistryCrud),
    ("designer preview row keeps object geometry", TestDesignerPreviewRowKeepsGeometry),
    ("canvas zoom and collection reconcile preserve identity", TestCanvasRefreshPreservesIdentity),
    ("key object changes without collapsing multi-selection", TestKeyObjectPreservesMultiSelection),
    ("resize cancel restores the single-object frame", TestResizeCancelRestoresSingleObject),
    ("canvas edit gesture is one undo step and cancel restores", TestEditGestureHistory),
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
    ("linear barcode X-dim warning flags sub-2-dot modules", TestLinearBarcodeXDimModuleSizeWarning),
    ("print preflight blocks undersized linear X-dim at print dpi", TestPreflightBlocksUndersizedLinearXDim),
    ("print preflight accepts comfortable linear X-dim", TestPreflightAcceptsComfortableLinearXDim),
    ("legacy linear preflight uses logical modules not pixel columns", TestLegacyLinearPreflightUsesLogicalModules),
    ("linear barcode width follows quantized X-dim when SizedFromX", TestLinearBarcodeWidthFollowsQuantizedXDim),
    ("code 39 wide narrow ratio and physical quiet zone preflight", TestCode39RatioAndQuietZonePreflight),
    ("compiled scene print uses SizedFromX production width", TestCompiledScenePrintUsesSizedFromXWidth),
    ("legacy frame-owned width not auto-sized when X is zero", TestLegacyFrameOwnedWidthNotAutoSized),
    ("print preflight blocks missing bound field", TestPrintPreflightBlocksMissingField),
    ("quick print blocks when linked excel data is stale", TestQuickPrintBlocksOnStaleData),
    ("print operation log records job-level trace", TestPrintOperationLogRecordsJob),
    ("quick print log carries bounded queue observation", TestQuickPrintLogCarriesQueueObservation),
    ("preview and print render the same geometry, offset by the plan", TestPreviewAndPrintRenderSameGeometry),
    ("preflight warns when barcode module too small at real print dpi", TestPreflightWarnsSmallModuleAtPrintDpi),
    ("preflight reports non-square effective printer DPI", TestPreflightReportsNonSquareDpi),
    ("effective output contract hash survives plan binding", TestEffectiveOutputContractHash),
    ("preview and print plans carry the same immutable scene identity", TestPrintPlanCarriesSceneIdentity),
    ("bound rows preserve preview and print geometry identity", TestBoundRowsPreserveGeometryIdentity),
    ("scene compilation cache stays bounded under repeated requests", TestSceneCompilationCacheStress),
    ("compiled scene presenter is immutable and legacy plans still fall back", TestCompiledScenePresenter),
    ("spool accepted does not claim physical completion", TestSpoolAcceptedDoesNotClaimPhysicalCompletion),
    ("spool acceptance without identity is explicit and non-correlatable", TestSpoolAcceptanceWithoutIdentityIsExplicit),
    ("spool monitor preserves queue evidence and timeout semantics", TestSpoolMonitorPreservesQueueEvidence),
    ("explicit print path fails closed without printer queue", TestExplicitPrintPathFailsClosed),
    ("interactive print selection rejects implicit Windows default", TestInteractivePrintSelectionRejectsImplicitDefault),
    ("print dispatch worker is dedicated STA and honors pre-start cancellation", TestPrintDispatchWorkerIsSta),
    ("calibration dispatch honors pre-start cancellation", TestCalibrationDispatchCancellation),
    ("effective print-plan preparation honors pre-start cancellation", TestEffectivePlanPreparationCancellation),
    ("print preview busy gate blocks duplicate dispatch", TestPrintPreviewBusyGate),
    ("missing named printer queue fails closed through lookup seam", TestMissingNamedPrinterQueueFailsClosed),
    ("quick print does not record preflight before queue preparation", TestQuickPrintPreflightOrdering),
    ("view model warns when saved printer queue disappears", TestViewModelShowsMissingPrinterWarning),
    ("tracking row printed indicator toggles with IsPrinted", TestTrackingRowPrintedIndicator),
    ("barcode module size warning uses same dpi as real preflight", TestBarcodeModuleSizeWarningMatchesPrintDpi),
    ("add current as data source is idempotent", TestAddCurrentAsDataSourceIsIdempotent),
    ("unlink excel clears database config but keeps bindings", TestUnlinkExcelKeepsBindings),
    ("unlink excel works when link is broken", TestUnlinkExcelWhenLinkBroken),
    ("properties excel verification is evidence based and refreshes stale rows", TestPropertiesExcelVerification),
    ("test connection reports ok, missing sheet, and missing file", TestExcelTestConnectionReportsStatus),
    ("data source records recent template usage", TestDataSourceRecordsRecentUsage),
    ("registry with unknown extra fields still loads", TestRegistryForwardCompatibility),
    ("import keeps a non-default header row instead of resetting to 1", TestImportKeepsCustomHeaderRow),
    ("excel preview rows use absolute row numbers and respect maxRows", TestExcelPreviewRows),
    ("copies-per-record resolves from Excel column, defaults to 1", TestResolveCopiesForRow),
    ("get sheets with preview reads every sheet in a single file open", TestGetSheetsWithPreview),
    ("excel cell value formatting after switching to ExcelDataReader", TestExcelCellValueFormatting),
    ("snap path matrix shares acquire/release rules across interaction paths", TestSnapPathMatrixSoftwareFixtures),
    ("dispatch revalidation names output-contract drift and blocks submission", TestDispatchRevalidationBlocksDrift),
    ("print support evidence redacts raw payloads", TestPrintSupportEvidenceRedaction),
    ("async relay command rejects re-entry until the first call completes", TestAsyncRelayCommandRejectsReentry),
    ("designer canvas routes snap through the path matrix", TestDesignerCanvasRoutesSnapThroughPathMatrix),
    ("print service attaches redacted support evidence on the shipped path", TestPrintServiceAttachesSupportEvidence),
    ("dispatch revalidation uses full effective-output fields before PrintDocument", TestDispatchRevalidationUsesFullEffectiveOutput),
    ("mixed-object canvas soak keeps selection identity and cancels cleanly", TestMixedObjectCanvasSoak),
    ("print center exports redacted support evidence from durable jobs", TestPrintCenterExportsSupportEvidence),
    ("gs1 industrial AI subset validates weight and variable fields", TestGs1IndustrialAiSubset),
    ("main shell regions match NiceLabel map AutomationIds", TestMainShellRegionsMatchNiceLabelMap),
    ("designer header commands are unique", TestDesignerHeaderCommandsAreUnique),
    ("designer shell layout at target scales", TestDesignerShellLayoutAtTargetScales)
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

static Task TestReleaseMetadataIsSynchronized()
{
    // A commercial/trial package is not releasable when the binary, title bar,
    // help text and installer advertise different versions.  Keep this check in
    // the application regression runner so a release build cannot silently
    // reintroduce the v0.086/v0.053-style drift found in the original audit.
    var root = FindRepositoryRoot();
    var csprojPath = Path.Combine(root, "src", "ANLAbel.App", "ANLAbel.App.csproj");
    var csproj = File.ReadAllText(csprojPath);
    var versionMatch = System.Text.RegularExpressions.Regex.Match(
        csproj,
        @"<Version>(?<version>[0-9]+\.[0-9]+)</Version>",
        System.Text.RegularExpressions.RegexOptions.CultureInvariant);
    AssertEqual(true, versionMatch.Success, "The app project must declare one numeric release version");
    var version = versionMatch.Groups["version"].Value;
    var displayVersion = $"v{version}";

    var appProject = new[]
    {
        "<AssemblyVersion>" + version + ".0.0</AssemblyVersion>",
        "<FileVersion>" + version + ".0.0</FileVersion>",
        "<InformationalVersion>" + version + "</InformationalVersion>"
    };
    foreach (var marker in appProject)
    {
        AssertEqual(true, csproj.Contains(marker, StringComparison.Ordinal),
            $"The app project must keep {marker} aligned with Version");
    }

    var appMetadata = new[]
    {
        Path.Combine(root, "src", "ANLAbel.App", "App.xaml.cs"),
        Path.Combine(root, "src", "ANLAbel.App", "HelpWindow.xaml.cs"),
        Path.Combine(root, "src", "ANLAbel.App", "MainWindow.xaml")
    };
    foreach (var path in appMetadata)
    {
        var content = File.ReadAllText(path);
        AssertEqual(true, content.Contains(displayVersion, StringComparison.Ordinal),
            $"{Path.GetFileName(path)} must advertise {displayVersion}");
    }

    foreach (var installerName in new[] { "ANLAbel-Commercial-x64.iss", "ANLAbel-Trial-x64.iss" })
    {
        var installer = File.ReadAllText(Path.Combine(root, "installer", installerName));
        AssertEqual(true, installer.Contains($"AppVersion={version}", StringComparison.Ordinal),
            $"{installerName} must use AppVersion={version}");
        AssertEqual(true, installer.Contains($"VersionInfoVersion={version}.0.0", StringComparison.Ordinal),
            $"{installerName} must use VersionInfoVersion={version}.0.0");
        AssertEqual(true, installer.Contains(displayVersion, StringComparison.Ordinal),
            $"{installerName} output name must carry {displayVersion}");
    }

    return Task.CompletedTask;
}

static Task TestGitHubReleaseUpdateParser()
{
    AssertEqual(true, GitHubReleaseParser.IsNewerVersion("0.257", "v0.258"), "0.258 should be newer than 0.257");
    AssertEqual(false, GitHubReleaseParser.IsNewerVersion("0.258", "0.258"), "Same version should not report newer");
    AssertEqual(false, GitHubReleaseParser.IsNewerVersion("0.258", "0.257"), "Older version should not report newer");
    AssertEqual(0, GitHubReleaseParser.CompareVersions("v0.258", "0.258"), "Normalized tags should compare equal");

    var json = """
    {
      "tag_name": "v0.258",
      "name": "Release 0.258",
      "body": "Changelog notes",
      "html_url": "https://github.com/ducancdt/anlabel/releases/tag/v0.258",
      "assets": [
        {
          "name": "ANLAbel-v0.258-Setup-x64.exe",
          "browser_download_url": "https://github.com/ducancdt/anlabel/releases/download/v0.258/ANLAbel-v0.258-Setup-x64.exe",
          "size": 47185920,
          "content_type": "application/x-msdownload"
        }
      ]
    }
    """;

    var release = GitHubReleaseParser.ParseReleaseJson(json);
    AssertEqual(true, release != null, "Release JSON must parse successfully");
    AssertEqual("v0.258", release!.TagName, "TagName must match");
    AssertEqual("0.258", release.VersionString, "VersionString must normalize");
    AssertEqual(true, release.InstallerAsset != null, "InstallerAsset must be detected");
    AssertEqual("ANLAbel-v0.258-Setup-x64.exe", release.InstallerAsset!.Name, "Installer name must match");
    AssertEqual(true, release.InstallerAsset.IsInstaller, "IsInstaller must be true for .exe");

    return Task.CompletedTask;
}

static string FindRepositoryRoot()
{
    var directory = new DirectoryInfo(Environment.CurrentDirectory);
    while (directory is not null)
    {
        if (File.Exists(Path.Combine(directory.FullName, "ANLAbel.slnx")))
        {
            return directory.FullName;
        }

        directory = directory.Parent;
    }

    throw new InvalidOperationException("Could not locate the ANLAbel repository root for release metadata validation.");
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
    template.Guides.Add(new LabelGuide
    {
        Id = "vertical-guide",
        Orientation = LabelGuideOrientation.Vertical,
        PositionMm = 12.345,
        IsLocked = true
    });
    template.Objects.Add(new LabelObject
    {
        Type = ObjectType.Text,
        BindingExpression = "{PartNo}",
        Text = "Mã hàng {PartNo}",
        XMm = 2.5,
        YMm = 3.5,
        WidthMm = 40,
        HeightMm = 8,
        Style =
        {
            VerticalAlignment = TextVerticalAlignmentMode.Bottom,
            TextDirection = TextDirectionMode.RightToLeft,
            TextSizing = TextSizingMode.FixedFrame,
            TextOverflow = TextOverflowMode.Clip,
            LineHeightPt = 18,
            TextPaddingLeftMm = 1.25,
            TextPaddingRightMm = 2.5,
            TextPaddingTopMm = 0.75,
            TextPaddingBottomMm = 3
        }
    });
    template.Objects.Add(new LabelObject
    {
        Type = ObjectType.TextBox,
        Name = "NiceLabel fit",
        Text = "Variable description",
        WidthMm = 24,
        HeightMm = 7,
        Style =
        {
            TextSizing = TextSizingMode.FixedFrame,
            TextOverflow = TextOverflowMode.Error,
            TextFitMinimumFontSizePt = 5,
            TextFitMaximumFontSizePt = 16,
            TextFitMinimumScale = 0.6,
            TextFitMaximumScale = 1.4
        }
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
    AssertEqual(2, loaded.Objects.Count, "Objects must survive JSON round trip");
    AssertEqual("{PartNo}", loaded.Objects[0].BindingExpression, "Binding expression must survive JSON round trip");
    AssertEqual(false, loaded.Objects[0].HasBindingIssue, "Designer-only binding issue state must not be persisted");
    AssertEqual(string.Empty, loaded.Objects[0].BindingStateDisplayText, "Designer-only binding status text must not be persisted");
    AssertEqual("Mã hàng {PartNo}", loaded.Objects[0].Text, "Unicode object text must survive JSON round trip");
    AssertEqual(TextVerticalAlignmentMode.Bottom, loaded.Objects[0].Style.VerticalAlignment, "Text vertical alignment must survive JSON round trip");
    AssertEqual(TextDirectionMode.RightToLeft, loaded.Objects[0].Style.TextDirection, "Text direction must survive JSON round trip");
    AssertEqual(TextSizingMode.FixedFrame, loaded.Objects[0].Style.TextSizing, "Text sizing mode must survive JSON round trip");
    AssertEqual(TextOverflowMode.Clip, loaded.Objects[0].Style.TextOverflow, "Text overflow policy must survive JSON round trip");
    AssertNear(18, loaded.Objects[0].Style.LineHeightPt, 0.001, "Explicit text line height must survive JSON round trip");
    AssertNear(0, loaded.Objects[0].Style.TextPaddingMm, 0.001, "Non-uniform text padding must clear the uniform shorthand");
    AssertNear(1.25, loaded.Objects[0].Style.TextPaddingLeftMm, 0.001, "Left text padding must survive JSON round trip");
    AssertNear(2.5, loaded.Objects[0].Style.TextPaddingRightMm, 0.001, "Right text padding must survive JSON round trip");
    AssertNear(0.75, loaded.Objects[0].Style.TextPaddingTopMm, 0.001, "Top text padding must survive JSON round trip");
    AssertNear(3, loaded.Objects[0].Style.TextPaddingBottomMm, 0.001, "Bottom text padding must survive JSON round trip");
    AssertEqual(TextSizingMode.FixedFrame, loaded.Objects[1].Style.TextSizing, "Fixed-frame TextBox mode must survive JSON round trip");
    AssertNear(5, loaded.Objects[1].Style.TextFitMinimumFontSizePt, 0.001, "Text fit minimum font must survive JSON round trip");
    AssertNear(16, loaded.Objects[1].Style.TextFitMaximumFontSizePt, 0.001, "Text fit maximum font must survive JSON round trip");
    AssertNear(0.6, loaded.Objects[1].Style.TextFitMinimumScale, 0.001, "Text fit minimum scale must survive JSON round trip");
    AssertNear(1.4, loaded.Objects[1].Style.TextFitMaximumScale, 0.001, "Text fit maximum scale must survive JSON round trip");
    AssertEqual(1, loaded.Guides.Count, "Persistent guides must survive JSON round trip");
    AssertEqual("vertical-guide", loaded.Guides[0].Id, "Guide ID must survive JSON round trip");
    AssertEqual(LabelGuideOrientation.Vertical, loaded.Guides[0].Orientation, "Guide orientation must survive JSON round trip");
    AssertNear(12.345, loaded.Guides[0].PositionMm, 0.001, "Guide position must survive JSON round trip");
    AssertEqual(true, loaded.Guides[0].IsLocked, "Guide lock state must survive JSON round trip");
}

static async Task TestTemplateSchemaEnvelope()
{
    var service = new ProjectFileService();
    var directory = Path.Combine(Environment.CurrentDirectory, "TestOutput", "schema-envelope");
    Directory.CreateDirectory(directory);
    var filePath = Path.Combine(directory, "current.anlabel");
    var legacyPath = Path.Combine(directory, "legacy.anlabel");
    var futurePath = Path.Combine(directory, "future.anlabel");
    var invalidPath = Path.Combine(directory, "invalid.anlabel");
    var malformedPath = Path.Combine(directory, "malformed.anlabel");
    var extensionPath = Path.Combine(directory, "v1-extension.anlabel");
    var extensionRoundTripPath = Path.Combine(directory, "v2-extension-roundtrip.anlabel");

    var template = new LabelTemplate
    {
        Name = "Schema contract",
        WidthMm = 50,
        HeightMm = 25,
        Dpi = 203
    };
    template.Objects.Add(new LabelObject
    {
        Id = "schema-object",
        Type = ObjectType.Text,
        Text = "LOT-001",
        XMm = 2,
        YMm = 3,
        WidthMm = 20,
        HeightMm = 5
    });

    await service.SaveAsync(template, filePath);
    using (var document = System.Text.Json.JsonDocument.Parse(await File.ReadAllTextAsync(filePath)))
    {
        var root = document.RootElement;
        AssertEqual("anlabel", root.GetProperty("format").GetString(), "Saved files must advertise the ANLAbel format");
        AssertEqual(ProjectFileService.CurrentSchemaVersion, root.GetProperty("schemaVersion").GetInt32(), "Saved files must advertise the current schema version");
        AssertEqual(System.Text.Json.JsonValueKind.Object, root.GetProperty("template").ValueKind, "Saved files must isolate the document under the versioned envelope");
    }

    var loaded = await service.LoadAsync(filePath);
    AssertEqual("schema-object", loaded.Objects[0].Id, "Versioned envelope round-trip must preserve stable object IDs");
    AssertNear(2, loaded.Objects[0].XMm, 0.001, "Versioned envelope round-trip must preserve physical geometry");

    var jsonOptions = new System.Text.Json.JsonSerializerOptions
    {
        WriteIndented = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };
    await File.WriteAllTextAsync(legacyPath, System.Text.Json.JsonSerializer.Serialize(template, jsonOptions));
    var migrated = await service.LoadAsync(legacyPath);
    AssertEqual("schema-object", migrated.Objects[0].Id, "Raw pre-envelope files must migrate on load");

    await File.WriteAllTextAsync(
        extensionPath,
        "{\"format\":\"anlabel\",\"schemaVersion\":1,\"template\":{\"Id\":\"extension-template\",\"Name\":\"Extension migration\",\"vendorExtension\":{\"mode\":\"strict\",\"limit\":7}}}");
    var extensionMigrated = await service.LoadAsync(extensionPath);
    AssertEqual(true, extensionMigrated.ExtensionData.ContainsKey("vendorExtension"), "A v1 extension member must be retained by the v2 model");
    await service.SaveAsync(extensionMigrated, extensionRoundTripPath);
    using (var extensionDocument = System.Text.Json.JsonDocument.Parse(await File.ReadAllTextAsync(extensionRoundTripPath)))
    {
        var extension = extensionDocument.RootElement.GetProperty("template").GetProperty("vendorExtension");
        AssertEqual("strict", extension.GetProperty("mode").GetString(), "Unknown extension metadata must survive v1-to-v2 round trip");
        AssertEqual(7, extension.GetProperty("limit").GetInt32(), "Unknown extension values must not be silently normalized or dropped");
    }

    var futureEnvelope = new
    {
        format = ProjectFileService.FileFormat,
        schemaVersion = ProjectFileService.CurrentSchemaVersion + 99,
        template
    };
    await File.WriteAllTextAsync(futurePath, System.Text.Json.JsonSerializer.Serialize(futureEnvelope, jsonOptions));
    var rejectedFuture = false;
    try
    {
        await service.LoadAsync(futurePath);
    }
    catch (InvalidDataException)
    {
        rejectedFuture = true;
    }

    AssertEqual(true, rejectedFuture, "A future schema must fail closed instead of silently dropping fields");

    await File.WriteAllTextAsync(invalidPath, "[]");
    var rejectedRoot = false;
    try
    {
        await service.LoadAsync(invalidPath);
    }
    catch (InvalidDataException)
    {
        rejectedRoot = true;
    }

    AssertEqual(true, rejectedRoot, "A non-object template root must fail closed");

    await File.WriteAllTextAsync(malformedPath, "{\"format\":\"");
    var rejectedMalformed = false;
    try
    {
        await service.LoadAsync(malformedPath);
    }
    catch (InvalidDataException)
    {
        rejectedMalformed = true;
    }

    AssertEqual(true, rejectedMalformed, "Malformed JSON must surface as a document error instead of leaking parser details");

    var beforeCanceledSave = await File.ReadAllTextAsync(filePath);
    using var canceled = new CancellationTokenSource();
    canceled.Cancel();
    var canceledSave = false;
    try
    {
        await service.SaveAsync(template, filePath, canceled.Token);
    }
    catch (OperationCanceledException)
    {
        canceledSave = true;
    }

    AssertEqual(true, canceledSave, "Canceled save must surface cancellation");
    AssertEqual(beforeCanceledSave, await File.ReadAllTextAsync(filePath), "Canceled save must leave the last committed file untouched");
    AssertEqual(0, Directory.GetFiles(directory, $".{Path.GetFileName(filePath)}.*.tmp").Length, "Canceled save must clean its temporary file");
}

static async Task TestWrongFormatEnvelopeDoesNotRecoverFromBackup()
{
    var service = new ProjectFileService();
    var directory = Path.Combine(Environment.CurrentDirectory, "TestOutput", $"envelope-format-{Guid.NewGuid():N}");
    Directory.CreateDirectory(directory);
    var filePath = Path.Combine(directory, "production.anlabel");

    await service.SaveAsync(new LabelTemplate { Name = "Last known good", WidthMm = 40, HeightMm = 20, Dpi = 203 }, filePath);
    await service.SaveAsync(new LabelTemplate { Name = "Current revision", WidthMm = 50, HeightMm = 25, Dpi = 203 }, filePath);
    AssertEqual(true, File.Exists(ProjectFileService.GetBackupPath(filePath)), "Setup: a backup must exist so recovery would have something to open");

    var foreignEnvelope = """{"format":"not-anlabel","schemaVersion":2,"template":{"Name":"Foreign","WidthMm":10,"HeightMm":10,"Dpi":203}}""";
    await File.WriteAllTextAsync(filePath, foreignEnvelope);

    var loadRejected = false;
    try
    {
        await service.LoadAsync(filePath);
    }
    catch (InvalidDataException ex)
    {
        loadRejected = ex.Message.Contains("format", StringComparison.OrdinalIgnoreCase);
    }

    AssertEqual(true, loadRejected, "LoadAsync must reject a versioned envelope whose format marker is not anlabel");

    var recovered = false;
    var recoveredFromBackup = false;
    try
    {
        var result = await service.LoadWithRecoveryAsync(filePath);
        recovered = true;
        recoveredFromBackup = result.RecoveredFromBackup;
    }
    catch (UnsupportedProjectSchemaException)
    {
        recovered = false;
    }
    catch (InvalidDataException)
    {
        recovered = false;
    }

    AssertEqual(false, recovered, "A foreign format marker must fail closed instead of opening the last .bak");
    AssertEqual(false, recoveredFromBackup, "Recovery must not silently downgrade a foreign envelope to the backup");

    try { Directory.Delete(directory, true); } catch { }
}

static async Task TestTemplateBackupRecovery()
{
    var service = new ProjectFileService();
    var directory = Path.Combine(Environment.CurrentDirectory, "TestOutput", $"recovery-{Guid.NewGuid():N}");
    Directory.CreateDirectory(directory);
    var filePath = Path.Combine(directory, "production.anlabel");
    var backupPath = ProjectFileService.GetBackupPath(filePath);

    var first = new LabelTemplate { Name = "Last known good", WidthMm = 60, HeightMm = 30, Dpi = 203 };
    var second = new LabelTemplate { Name = "Current revision", WidthMm = 80, HeightMm = 40, Dpi = 300 };
    await service.SaveAsync(first, filePath);
    AssertEqual(false, File.Exists(backupPath), "The first save has no previous committed revision to rotate");

    await service.SaveAsync(second, filePath);
    AssertEqual(true, File.Exists(backupPath), "A subsequent save must preserve the previous committed revision as .bak");
    var backup = await service.LoadAsync(backupPath);
    AssertEqual(first.Name, backup.Name, "The backup must contain the previous complete template, not the new revision");

    await File.WriteAllTextAsync(filePath, "{\"format\":\"anlabel\"");
    var recovered = await service.LoadWithRecoveryAsync(filePath);
    AssertEqual(true, recovered.RecoveredFromBackup, "Malformed primary JSON must report explicit backup recovery");
    AssertEqual(Path.GetFullPath(backupPath), recovered.SourcePath, "Recovery result must identify the backup source path");
    AssertEqual(first.Name, recovered.Template.Name, "Recovery must load the last known-good template");
    AssertEqual(true, recovered.PrimaryError?.Contains("malformed", StringComparison.OrdinalIgnoreCase) == true, "Recovery must retain the primary corruption reason");

    var preservePath = Path.Combine(directory, "preserve-backup.anlabel");
    var preserveBackupPath = ProjectFileService.GetBackupPath(preservePath);
    await service.SaveAsync(first, preservePath);
    await service.SaveAsync(second, preservePath);
    await File.WriteAllTextAsync(preservePath, "{\"format\":\"");
    await service.SaveAsync(new LabelTemplate { Name = "Recovered edit" }, preservePath);
    var preserved = await service.LoadAsync(preserveBackupPath);
    AssertEqual(first.Name, preserved.Name, "Saving over a corrupt primary must keep the previous good backup intact");

    var futureEnvelope = new
    {
        format = ProjectFileService.FileFormat,
        schemaVersion = ProjectFileService.CurrentSchemaVersion + 1,
        template = second
    };
    var jsonOptions = new System.Text.Json.JsonSerializerOptions
    {
        WriteIndented = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };
    await File.WriteAllTextAsync(filePath, System.Text.Json.JsonSerializer.Serialize(futureEnvelope, jsonOptions));
    var rejectedFuture = false;
    try
    {
        await service.LoadWithRecoveryAsync(filePath);
    }
    catch (UnsupportedProjectSchemaException)
    {
        rejectedFuture = true;
    }

    AssertEqual(true, rejectedFuture, "A future primary schema must fail closed instead of silently loading an older backup");

    var futureBytes = await File.ReadAllTextAsync(filePath);
    var refusedOverwrite = false;
    try
    {
        await service.SaveAsync(second, filePath);
    }
    catch (UnsupportedProjectSchemaException)
    {
        refusedOverwrite = true;
    }

    AssertEqual(true, refusedOverwrite, "Save must refuse to overwrite a future schema that this build cannot understand");
    AssertEqual(futureBytes, await File.ReadAllTextAsync(filePath), "Refusing a future-schema overwrite must preserve the original bytes");

    await File.WriteAllTextAsync(filePath, "{\"format\":\"");
    await File.WriteAllTextAsync(backupPath, "[]");
    var rejectedBoth = false;
    try
    {
        await service.LoadWithRecoveryAsync(filePath);
    }
    catch (InvalidDataException ex)
    {
        rejectedBoth = ex.Message.Contains("both invalid", StringComparison.OrdinalIgnoreCase);
    }

    AssertEqual(true, rejectedBoth, "Corrupt primary and corrupt backup must surface a combined operator-facing error");
}

static async Task TestFailedTemplateOpenPreservesCurrentDocument()
{
    var directory = Path.Combine(Environment.CurrentDirectory, "TestOutput", $"open-failure-{Guid.NewGuid():N}");
    Directory.CreateDirectory(directory);
    var filePath = Path.Combine(directory, "future.anlabel");
    var futureEnvelope = new
    {
        format = ProjectFileService.FileFormat,
        schemaVersion = ProjectFileService.CurrentSchemaVersion + 1,
        template = new LabelTemplate { Name = "Future document" }
    };
    var jsonOptions = new System.Text.Json.JsonSerializerOptions
    {
        WriteIndented = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };
    await File.WriteAllTextAsync(filePath, System.Text.Json.JsonSerializer.Serialize(futureEnvelope, jsonOptions));

    var vm = new MainViewModel();
    vm.Template.Name = "Unsaved operator work";
    var rejected = false;
    try
    {
        await vm.OpenAsync(filePath);
    }
    catch (UnsupportedProjectSchemaException)
    {
        rejected = true;
    }

    AssertEqual(true, rejected, "Opening a future document must fail closed");
    AssertEqual("Unsaved operator work", vm.Template.Name, "A failed open must leave the current document untouched");
    AssertEqual(string.Empty, vm.CurrentFilePath, "A failed open must not change the current file path");
}

static async Task TestTemplateRevisionHistoryAndRollback()
{
    var fileService = new ProjectFileService();
    var revisionService = new ProjectRevisionService(fileService);
    var directory = Path.Combine(Environment.CurrentDirectory, "TestOutput", $"revision-history-{Guid.NewGuid():N}");
    Directory.CreateDirectory(directory);
    var filePath = Path.Combine(directory, "production.anlabel");
    var backupPath = ProjectFileService.GetBackupPath(filePath);

    var first = new LabelTemplate { Name = "Revision one", WidthMm = 60, HeightMm = 30, Dpi = 203 };
    var second = new LabelTemplate { Name = "Revision two", WidthMm = 80, HeightMm = 40, Dpi = 300 };
    await fileService.SaveAsync(first, filePath);
    await fileService.SaveAsync(second, filePath);

    var entries = await revisionService.ListAsync(filePath);
    AssertEqual(2, entries.Count, "Revision history must expose primary and managed backup slots");
    var primary = entries.Single(entry => entry.Kind == ProjectRevisionKind.Primary);
    var backup = entries.Single(entry => entry.Kind == ProjectRevisionKind.Backup);
    AssertEqual(true, primary.IsValid, "The current primary must be reported as valid");
    AssertEqual("Revision two", primary.TemplateName, "Primary history entry must identify the current template");
    AssertEqual(true, backup.IsValid, "The managed backup must be reported as valid");
    AssertEqual("Revision one", backup.TemplateName, "Backup history entry must identify the previous template");
    AssertEqual(true, backup.CanRestore, "Only a valid managed backup may be offered for rollback");

    var diff = await revisionService.CompareAsync(filePath);
    AssertEqual(true, diff.IsComparable, "A valid primary/backup pair must produce a comparable diff");
    AssertEqual(true, diff.HasChanges, "Different revisions must be reported as changed");
    AssertEqual(true, diff.Differences.Any(item => item.StartsWith("Label size:", StringComparison.Ordinal)), "The revision diff must expose physical label-size changes");
    AssertEqual(true, diff.Differences.Any(item => item.StartsWith("Design DPI:", StringComparison.Ordinal)), "The revision diff must expose design-DPI changes");

    var archiveEntries = await revisionService.ListArchiveAsync(filePath);
    AssertEqual(1, archiveEntries.Count, "The second save must preserve the first primary in the archive history");
    AssertEqual(true, archiveEntries[0].IsValid, "A saved archive snapshot must remain parseable");
    AssertEqual("Revision one", archiveEntries[0].TemplateName, "The archive must preserve the exact prior template");
    AssertEqual(true, archiveEntries[0].CanRestore, "A valid archived snapshot must be an explicit rollback candidate");
    AssertEqual(true, (await revisionService.ListAllAsync(filePath)).Count >= 3, "The complete revision view must include primary, backup and archive");
    AssertEqual(true, (await revisionService.ListAuditAsync(filePath)).Count >= 1, "Saving a replacement must append an audit event");

    var currentBytes = await File.ReadAllBytesAsync(filePath);
    var backupBytes = await File.ReadAllBytesAsync(backupPath);
    var restored = await revisionService.RestoreBackupAsync(filePath);
    AssertEqual("Revision one", restored.TemplateName, "Rollback must restore the validated backup revision");
    AssertEqual(true, currentBytes.SequenceEqual(await File.ReadAllBytesAsync(backupPath)), "Rollback must preserve the previous primary bytes in the new backup slot");
    AssertEqual(true, backupBytes.SequenceEqual(await File.ReadAllBytesAsync(filePath)), "Rollback must publish the selected backup bytes without reserializing the document");
    AssertEqual(true, (await revisionService.ListArchiveAsync(filePath)).Count >= 2, "Rollback must retain the replaced primary in archive history");

    var futureEnvelope = new
    {
        format = ProjectFileService.FileFormat,
        schemaVersion = ProjectFileService.CurrentSchemaVersion + 1,
        template = second
    };
    var jsonOptions = new System.Text.Json.JsonSerializerOptions
    {
        WriteIndented = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };
    await File.WriteAllTextAsync(filePath, System.Text.Json.JsonSerializer.Serialize(futureEnvelope, jsonOptions));
    entries = await revisionService.ListAsync(filePath);
    AssertEqual("Unsupported schema", entries.Single(entry => entry.Kind == ProjectRevisionKind.Primary).Status, "Revision history must distinguish a future schema from malformed JSON");
    AssertEqual(true, entries.Single(entry => entry.Kind == ProjectRevisionKind.Backup).CanRestore, "A valid backup remains an explicit rollback option when primary schema is newer");
    diff = await revisionService.CompareAsync(filePath);
    AssertEqual(false, diff.IsComparable, "A future-schema primary must not be compared as if it were a valid document");
    AssertEqual(true, diff.Summary.Contains("unsupported schema", StringComparison.OrdinalIgnoreCase), "The diff must explain why a future-schema pair is not comparable");

    await revisionService.RestoreBackupAsync(filePath);
    AssertEqual("Revision two", (await fileService.LoadAsync(filePath)).Name, "Rollback must recover from a valid backup even when primary schema is newer");

    var cancelPath = Path.Combine(directory, "cancel.anlabel");
    var cancelBackupPath = ProjectFileService.GetBackupPath(cancelPath);
    await fileService.SaveAsync(first, cancelPath);
    await fileService.SaveAsync(second, cancelPath);
    var cancelPrimaryBytes = await File.ReadAllBytesAsync(cancelPath);
    var cancelBackupBytes = await File.ReadAllBytesAsync(cancelBackupPath);
    using var canceled = new CancellationTokenSource();
    canceled.Cancel();
    var canceledRestore = false;
    try
    {
        await revisionService.RestoreBackupAsync(cancelPath, canceled.Token);
    }
    catch (OperationCanceledException)
    {
        canceledRestore = true;
    }

    AssertEqual(true, canceledRestore, "A canceled rollback must surface cancellation before commit");
    AssertEqual(true, cancelPrimaryBytes.SequenceEqual(await File.ReadAllBytesAsync(cancelPath)), "A canceled rollback must leave the primary bytes untouched");
    AssertEqual(true, cancelBackupBytes.SequenceEqual(await File.ReadAllBytesAsync(cancelBackupPath)), "A canceled rollback must leave the backup bytes untouched");
    AssertEqual(0, Directory.GetFiles(directory, $".{Path.GetFileName(cancelPath)}.*.rollback.tmp").Length, "A canceled rollback must clean its temporary file");

    var retentionPath = Path.Combine(directory, "retention.anlabel");
    for (var revision = 0; revision < ProjectRevisionArchive.DefaultRetentionCount + 3; revision++)
    {
        await fileService.SaveAsync(new LabelTemplate { Name = $"Retention {revision}" }, retentionPath);
    }

    var retained = await revisionService.ListArchiveAsync(retentionPath);
    AssertEqual(ProjectRevisionArchive.DefaultRetentionCount, retained.Count, "Archive retention must remove only the oldest snapshots after the bounded limit");
    AssertEqual(true, retained.All(entry => entry.IsValid), "Every retained archive snapshot must remain parseable");
    AssertEqual("Retention 10", (await fileService.LoadAsync(retentionPath)).Name, "The newest primary must remain committed after retention cleanup");
    AssertEqual(true, (await revisionService.ListAuditAsync(retentionPath)).Count >= ProjectRevisionArchive.DefaultRetentionCount + 2, "Audit history must outlive bounded archive cleanup");
}

static Task TestPersistentRulerGuides()
{
    var template = new LabelTemplate { WidthMm = 100, HeightMm = 50 };
    var started = 0;
    var completed = 0;
    var canceled = 0;
    Exception? failure = null;

    var thread = new Thread(() =>
    {
        try
        {
            var canvas = new LabelDesignerCanvas { Template = template, Zoom = 1 };
            canvas.EditGestureStarted += (_, _) => started++;
            canvas.EditGestureCompleted += (_, _) => completed++;
            canvas.EditGestureCanceled += (_, _) => canceled++;

            AssertEqual(true, canvas.BeginGuideDrag(LabelGuideOrientation.Vertical, 10), "Ruler drag should create a vertical guide");
            canvas.UpdateGuideDrag(24.5);
            canvas.CompleteGuideDrag(24.5);
            AssertEqual(1, template.Guides.Count, "Guide drag should leave one persistent guide");
            AssertNear(24.5, template.Guides[0].PositionMm, 0.001, "Guide drag should commit physical millimetre position");

            template.Guides[0].IsLocked = true;
            AssertEqual(false, canvas.BeginGuideDrag(LabelGuideOrientation.Vertical, 24.5), "Locked guides must reject ruler drag");
            AssertEqual(1, template.Guides.Count, "Locked guide rejection must not create another guide");
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

    AssertEqual(1, started, "A guide drag must start exactly one edit gesture");
    AssertEqual(1, completed, "A guide drag must commit exactly one edit gesture");
    AssertEqual(0, canceled, "A successful guide drag must not cancel");
    return Task.CompletedTask;
}

static Task TestKeyboardNudgePrecisionAndGesture()
{
    var template = new LabelTemplate { WidthMm = 100, HeightMm = 50 };
    var item = new LabelObject
    {
        Id = "nudge-object",
        Type = ObjectType.Rectangle,
        Name = "Nudge rectangle",
        XMm = 10,
        YMm = 10,
        WidthMm = 12,
        HeightMm = 6
    };
    template.Objects.Add(item);
    var started = 0;
    var completed = 0;
    var canceled = 0;
    Exception? failure = null;

    var thread = new Thread(() =>
    {
        try
        {
            var canvas = new LabelDesignerCanvas { Template = template, Zoom = 1, SelectedObject = item };
            canvas.EditGestureStarted += (_, _) => started++;
            canvas.EditGestureCompleted += (_, _) => completed++;
            canvas.EditGestureCanceled += (_, _) => canceled++;

            AssertEqual(true, canvas.NudgeSelectedObjects(NudgeDirection.Right, NudgeStepMode.Standard), "Standard nudge should move the selected object");
            AssertEqual(true, canvas.NudgeSelectedObjects(NudgeDirection.Right, NudgeStepMode.Standard), "Repeated nudge should move the selected object");
            AssertNear(10.2, item.XMm, 0.0001, "Standard keyboard nudge must use 0.1 mm physical steps");
            AssertEqual(1, started, "Repeated keyboard nudges must share one edit gesture");
            AssertEqual(0, completed, "Nudge gesture must remain open until idle/explicit commit");

            canvas.CommitNudgeGesture();
            AssertEqual(1, completed, "Explicit nudge commit must close one history gesture");

            AssertEqual(true, canvas.NudgeSelectedObjects(NudgeDirection.Left, NudgeStepMode.Fine), "Fine nudge should move the selected object");
            AssertNear(10.19, item.XMm, 0.0001, "Fine keyboard nudge must use 0.01 mm physical steps");
            canvas.CancelNudgeGesture();
            AssertNear(10.2, item.XMm, 0.0001, "Cancel must restore the exact pre-nudge geometry");
            AssertEqual(1, canceled, "Canceled keyboard nudge must emit one cancellation");
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

static async Task TestCsvImport()
{
    var directory = Path.Combine(Environment.CurrentDirectory, "TestOutput");
    Directory.CreateDirectory(directory);
    var commaPath = Path.Combine(directory, $"csv-comma-{Guid.NewGuid():N}.csv");
    var semicolonPath = Path.Combine(directory, $"csv-semicolon-{Guid.NewGuid():N}.csv");
    var tabPath = Path.Combine(directory, $"csv-tab-{Guid.NewGuid():N}.csv");
    var multilineHeaderPath = Path.Combine(directory, $"csv-multiline-header-{Guid.NewGuid():N}.csv");
    var invalidQuotePath = Path.Combine(directory, $"csv-invalid-quote-{Guid.NewGuid():N}.csv");

    try
    {
        await File.WriteAllTextAsync(commaPath, "PartNo,Description,Qty\r\nPN-001,\"Valve, stainless\",20\r\n");
        await File.WriteAllTextAsync(semicolonPath, "Code;Lot;Qty\nPN-002;Lô A;5\n");
        await File.WriteAllTextAsync(tabPath, "Code\tLot\nPN-003\tLô B\n");
        var service = new ExcelDataService();
        await File.WriteAllTextAsync(multilineHeaderPath, "PartNo;\"Description\n(EN)\";Qty\nPN-004;Valve;7\n");
        await File.WriteAllTextAsync(invalidQuotePath, "PartNo,Description\nPN-005,\"missing end\n");

        var sheets = await service.GetSheetNamesAsync(commaPath);
        AssertEqual(ExcelDataService.CsvSheetName, sheets.Single(), "A CSV source must expose one stable pseudo-sheet");
        var preview = await service.PreviewRowsAsync(commaPath, ExcelDataService.CsvSheetName, 2);
        AssertEqual(2, preview.Count, "CSV preview must include header and the first data row");
        AssertEqual("Valve, stainless", preview[1].Cells[1], "Quoted commas must remain in one CSV field");

        var comma = await service.LoadSheetAsync(commaPath, ExcelDataService.CsvSheetName);
        AssertEqual(1, comma.Rows.Count, "CSV import must materialize data rows after the header");
        AssertEqual("Valve, stainless", comma.Rows[0]["Description"], "CSV fields must preserve quoted punctuation");

        var semicolon = await service.LoadSheetAsync(semicolonPath, ExcelDataService.CsvSheetName);
        AssertEqual("PN-002", semicolon.Rows[0]["Code"], "CSV delimiter detection must support semicolon-delimited files");
        AssertEqual("Lô A", semicolon.Rows[0]["Lot"], "CSV import must preserve UTF-8 values");

        var tab = await service.LoadSheetAsync(tabPath, ExcelDataService.CsvSheetName);
        AssertEqual("Lô B", tab.Rows[0]["Lot"], "CSV delimiter detection must support tab-delimited files");

        var multilineHeader = await service.LoadSheetAsync(multilineHeaderPath, ExcelDataService.CsvSheetName);
        AssertEqual(3, multilineHeader.Columns.Count, "CSV delimiter discovery must read the first logical record when a quoted header spans lines");
        AssertEqual("PN-004", multilineHeader.Rows[0]["PartNo"], "CSV data after a multiline quoted header must retain the detected delimiter");

        try
        {
            await service.LoadSheetAsync(invalidQuotePath, ExcelDataService.CsvSheetName);
            throw new InvalidOperationException("An unterminated CSV quote must fail closed.");
        }
        catch (ExcelDataReadException exception)
        {
            AssertEqual(ExcelDataReadError.InvalidData, exception.Error, "An unterminated CSV quote must report structured invalid-data evidence");
        }

        var viewModel = new MainViewModel();
        viewModel.Template.DataTransforms.Add(new ANLAbel.Core.Data.DataTransformDefinition("PrintName", "CONCAT(FIELD(\"PartNo\"), \"-PRINT\")"));
        await viewModel.ImportExcelAsync(commaPath, ExcelDataService.CsvSheetName);
        AssertEqual("csv", viewModel.DataConnector?.Descriptor.Kind, "CSV import must publish the typed CSV connector alongside the legacy DataView");
        AssertEqual(false, viewModel.DataConnector?.Descriptor.SupportsRefresh, "An immutable imported connector must not advertise an in-place refresh operation");
        var connectorPage = await viewModel.DataConnector!.ReadPageAsync(new ANLAbel.Core.Data.DataReadRequest(Limit: 1));
        AssertEqual("PN-001", connectorPage.Records[0].Values["PartNo"], "The typed connector must expose the same imported row used by binding/preview");
        AssertEqual("PN-001-PRINT", viewModel.PreviewRow!["PrintName"], "Persisted typed transforms must flow into the preview row used by binding and print");
        viewModel.NewTemplate(new NewTemplateRequest("Fresh", 30, 20, 203));
        AssertEqual<object?>(null, viewModel.DataConnector, "Starting a new template must not retain the previous source connector");
    }
    finally
    {
        try { File.Delete(commaPath); } catch { }
        try { File.Delete(semicolonPath); } catch { }
        try { File.Delete(tabPath); } catch { }
        try { File.Delete(multilineHeaderPath); } catch { }
        try { File.Delete(invalidQuotePath); } catch { }
    }
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

static Task TestQrFillsAuthoredFrameWhenEnlarged()
{
    var renderer = new ZxingBarcodeRenderer();
    var options = new BarcodeRenderOptions { QuietZoneModules = 2 };
    var compact = renderer.RenderBarcode("QR Code", BarcodeType.QRCode, 10, 10, 300, options);
    var enlarged = renderer.RenderBarcode("QR Code", BarcodeType.QRCode, 40, 40, 300, options);
    var skewed = renderer.RenderBarcode("QR Code", BarcodeType.QRCode, 40, 28, 300, options);
    var expectedLarge = (int)Math.Round(40 / 25.4 * 300, MidpointRounding.AwayFromZero);

    int FirstDark(BarcodePixelImage image)
    {
        for (var x = 0; x < image.WidthPixels; x++)
        {
            for (var y = 0; y < image.HeightPixels; y++)
            {
                if (image.BgraPixels[(y * image.WidthPixels + x) * 4] < 128)
                {
                    return x;
                }
            }
        }

        return -1;
    }

    int LastDark(BarcodePixelImage image)
    {
        for (var x = image.WidthPixels - 1; x >= 0; x--)
        {
            for (var y = 0; y < image.HeightPixels; y++)
            {
                if (image.BgraPixels[(y * image.WidthPixels + x) * 4] < 128)
                {
                    return x;
                }
            }
        }

        return -1;
    }

    AssertEqual(true, enlarged.WidthPixels == enlarged.HeightPixels, "Square-DPI QR must stay square");
    AssertEqual(true, enlarged.WidthPixels <= expectedLarge, "Fitted QR must not exceed the authored frame");
    AssertEqual(true, skewed.WidthPixels == skewed.HeightPixels, "A non-square box must not stretch QR modules");

    var compactInset = FirstDark(compact);
    var enlargedInset = FirstDark(enlarged);
    AssertEqual(true, compactInset > 0, "10 mm QR must keep its authored quiet zone");
    AssertEqual(true, enlargedInset > 0, "40 mm QR must keep its authored quiet zone");
    var compactBody = LastDark(compact) - compactInset + 1;
    var enlargedBody = LastDark(enlarged) - enlargedInset + 1;
    AssertEqual(true, enlargedBody > compactBody * 2,
        $"Dragging the object larger must grow the QR symbol ({compactBody}px -> {enlargedBody}px)");
    AssertEqual(true, enlargedInset / (double)enlarged.WidthPixels < 0.12,
        "Enlarged-frame leftover must stay inside the authored quiet zone, not an extra ZXing pad");
    AssertEqual(true, (expectedLarge - enlarged.WidthPixels) / 2.0 < enlargedBody / 20.0,
        "Leftover pad on each side must be smaller than one module");

    var rendererSource = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src", "ANLAbel.Printing", "RenderPipeline", "LabelVisualRenderer.cs"));
    AssertEqual(true, rendererSource.Contains("!isMatrix", StringComparison.Ordinal),
        "Print/preview must skip independent frame resize for matrix codes");
    AssertEqual(true, rendererSource.Contains("(symbolRect.Width - destWidth) / 2", StringComparison.Ordinal),
        "Print/preview must paint the fitted square centered, not stretch into symbolRect");
    var designer = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src", "ANLAbel.App", "Controls", "LabelDesignerCanvas.cs"));
    AssertEqual(true, designer.Contains("Stretch.Uniform", StringComparison.Ordinal),
        "Designer must not WPF-Fill a square 2D bitmap into a different aspect");
    return Task.CompletedTask;
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

static Task TestBarcodeRenderPreservesNonSquareDpi()
{
    var renderer = new ZxingBarcodeRenderer();
    const double widthMm = 40;
    const double heightMm = 12;
    const int dpiX = 305;
    const int dpiY = 609;

    var pixels = renderer.RenderBarcode("ABC123", BarcodeType.Code128, widthMm, heightMm, dpiX, dpiY);
    AssertEqual(MmConverter.MmToPrinterDots(widthMm, dpiX), pixels.WidthPixels,
        "A non-square output must quantize the barcode width to effective X DPI");
    AssertEqual(MmConverter.MmToPrinterDots(heightMm, dpiY), pixels.HeightPixels,
        "A non-square output must quantize the barcode height to effective Y DPI");
    AssertEqual(pixels.WidthPixels * pixels.HeightPixels * 4, pixels.BgraPixels.Length,
        "Non-square barcode output must have a complete BGRA buffer");

    var resized = pixels.ResizeNearest(17, 9);
    AssertEqual(17, resized.WidthPixels, "Nearest resize must expose the requested device width");
    AssertEqual(9, resized.HeightPixels, "Nearest resize must expose the requested device height");
    AssertEqual(17 * 9 * 4, resized.BgraPixels.Length, "Nearest resize must preserve four channels per pixel");
    return Task.CompletedTask;
}

static Task TestBarcodeApplicationProfilePreflight()
{
    const string gs1Data = "(01)09506000134352(17)250101(10)ABC";
    var renderer = new ZxingBarcodeRenderer();
    var gs1Image = renderer.RenderBarcode(
        gs1Data,
        BarcodeType.Code128,
        50,
        12,
        300,
        new BarcodeRenderOptions { IsGs1 = true, QuietZoneModules = 10 });
    AssertEqual(true, gs1Image.WidthPixels > 100, "GS1 Code 128 must render after the shared AI/FNC1 normalization");

    var template = new LabelTemplate
    {
        Name = "GS1 profile",
        WidthMm = 80,
        HeightMm = 30,
        Dpi = 300,
        PrinterProfile = new PrinterProfile { Dpi = 300 }
    };
    template.Objects.Add(new LabelObject
    {
        Type = ObjectType.BarcodeCode128,
        Name = "GS1",
        Text = gs1Data,
        WidthMm = 50,
        HeightMm = 12,
        XMm = 2,
        YMm = 2,
        BarcodeSymbology = BarcodeSymbology.Code128,
        BarcodeApplicationProfile = BarcodeApplicationProfile.Gs1,
        QrQuietZoneModules = 4,
        ShowBarcodeText = true,
        BarcodeTextFontSizePt = 7,
        // Explicit industrial X-dim so this gate isolates quiet-zone policy from
        // the linear module-dot preflight (frame-derived GS1 modules in 50 mm are often sub-threshold).
        BarcodeModuleWidthMm = LinearBarcodeModuleContract.RecommendedDefaultXDimensionMm
    });

    var blocked = new PrintService().ValidateRows(template, new IReadOnlyDictionary<string, string>?[] { null });
    AssertEqual(false, blocked.IsSuccess, "GS1 preflight must block a linear symbol with an undersized quiet zone");
    AssertEqual(true, blocked.Issues.Any(issue => issue.Message.Contains("Quiet zone", StringComparison.OrdinalIgnoreCase)),
        "GS1 quiet-zone failure must identify the missing production margin");

    template.Objects[0].QrQuietZoneModules = 10;
    var accepted = new PrintService().ValidateRows(template, new IReadOnlyDictionary<string, string>?[] { null });
    AssertEqual(true, accepted.IsSuccess,
        "A valid GS1 payload with the required quiet zone must pass software preflight: " + accepted.ToUserMessage());
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
        WidthMm = 35,
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

static Task TestCompiledRendererPreservesTextFramePolicy()
{
    var template = new LabelTemplate
    {
        Name = "Compiled text policy",
        WidthMm = 40,
        HeightMm = 20,
        Dpi = 300
    };
    template.Objects.Add(new LabelObject
    {
        Type = ObjectType.TextBox,
        Name = "Fixed frame",
        Text = "W",
        XMm = 2,
        YMm = 2,
        WidthMm = 0.5,
        HeightMm = 8,
        Style =
        {
            FontSizePt = 14,
            TextSizing = TextSizingMode.FixedFrame,
            VerticalAlignment = TextVerticalAlignmentMode.Top
        }
    });

    // CreateDesignPlan forces the same immutable SceneSnapshot path used by
    // production preview/print. The renderer must hydrate TextBox frame policy
    // from that snapshot instead of silently reverting to free-flowing Text.
    var plan = new PrintService().CreateDesignPlan(template);
    var visual = new LabelVisualRenderer().Render(template, null, plan);
    var bitmap = RenderTestBitmap(
        visual,
        MmConverter.MmToDip(plan.LabelWidthMm),
        MmConverter.MmToDip(plan.LabelHeightMm));
    var pixels = new byte[bitmap.PixelWidth * bitmap.PixelHeight * 4];
    bitmap.CopyPixels(pixels, bitmap.PixelWidth * 4, 0);
    var redPixels = 0;
    for (var index = 0; index < pixels.Length; index += 4)
    {
        // RenderTargetBitmap uses BGRA32; the overflow frame is red.
        if (pixels[index + 2] > 160 && pixels[index + 1] < 110 && pixels[index] < 110 && pixels[index + 3] > 160)
        {
            redPixels++;
        }
    }

    AssertEqual(true, redPixels > 0,
        "Compiled preview/print rendering must preserve the TextBox frame and show the same overflow diagnostic as the designer");
    return Task.CompletedTask;
}

static Task TestPreviewDrawingSnapshotIsFrozen()
{
    var template = new LabelTemplate
    {
        Name = "Frozen preview drawing",
        WidthMm = 50,
        HeightMm = 25,
        Dpi = 300
    };
    template.Objects.Add(new LabelObject
    {
        Type = ObjectType.Text,
        Text = "Frozen",
        XMm = 2,
        YMm = 2,
        WidthMm = 20,
        HeightMm = 6
    });

    var snapshot = new PrintService().CreatePreviewDrawing(template, null);
    AssertEqual(true, snapshot.Drawing.IsFrozen, "Preview drawing must be frozen before crossing the UI thread boundary");
    AssertEqual(true, snapshot.WidthDip > 0 && snapshot.HeightDip > 0, "Preview drawing must retain label dimensions");
    return Task.CompletedTask;
}

static async Task TestPreviewRasterWorker()
{
    var template = new LabelTemplate
    {
        Name = "Raster worker",
        WidthMm = 35,
        HeightMm = 15,
        Dpi = 300
    };
    template.Objects.Add(new LabelObject
    {
        Type = ObjectType.Rectangle,
        XMm = 1,
        YMm = 1,
        WidthMm = 10,
        HeightMm = 5,
        Style = { FillStyle = FillStyle.Solid, FillColor = "#000000" }
    });

    var snapshot = new PrintService().CreatePreviewDrawing(template, null);
    var image = await PreviewRasterizer.RenderAsync(snapshot.Drawing, snapshot.WidthDip, snapshot.HeightDip, CancellationToken.None);
    AssertEqual(true, image is BitmapSource { IsFrozen: true }, "Raster worker must return a frozen cross-thread bitmap");
    var workerThreadId = PreviewRasterizer.WorkerThreadId;
    AssertEqual(true, workerThreadId > 0, "Preview rasterization must start a dedicated worker thread");
    AssertEqual(1, PreviewRasterizer.WorkerStartCount, "Preview rasterization must use one reusable worker instead of one thread per page");

    using var canceled = new CancellationTokenSource();
    canceled.Cancel();
    var didCancel = false;
    try
    {
        await PreviewRasterizer.RenderAsync(snapshot.Drawing, snapshot.WidthDip, snapshot.HeightDip, canceled.Token);
    }
    catch (OperationCanceledException)
    {
        didCancel = true;
    }

    AssertEqual(true, didCancel, "Raster worker must honor cancellation before allocating a bitmap");
    var followUp = await PreviewRasterizer.RenderAsync(snapshot.Drawing, snapshot.WidthDip, snapshot.HeightDip, CancellationToken.None);
    AssertEqual(true, followUp is BitmapSource { IsFrozen: true }, "Reusable raster worker must continue serving a request after cancellation");
    AssertEqual(workerThreadId, PreviewRasterizer.WorkerThreadId, "Preview raster requests must stay on the same STA worker");
    AssertEqual(1, PreviewRasterizer.WorkerStartCount, "Canceled preview work must not create a replacement thread");
}

static async Task TestPreviewRasterCarriesGoldenIdentity()
{
    var template = new LabelTemplate
    {
        Name = "Raster identity",
        WidthMm = 24,
        HeightMm = 12,
        Dpi = 300
    };
    template.Objects.Add(new LabelObject
    {
        Type = ObjectType.Rectangle,
        XMm = 2,
        YMm = 2,
        WidthMm = 8,
        HeightMm = 4,
        Style = { FillStyle = FillStyle.Solid, FillColor = "#000000" }
    });

    var service = new PrintService();
    var snapshot = service.CreatePreviewDrawing(template, null);
    var first = await PreviewRasterizer.RenderSnapshotAsync(
        snapshot.Drawing,
        snapshot.WidthDip,
        snapshot.HeightDip,
        CancellationToken.None);
    AssertEqual(true, first.IsValid, "Preview raster snapshot must carry a valid golden identity");
    var bitmap = (BitmapSource)first.Image;
    AssertEqual(bitmap.PixelWidth, first.RasterIdentity.WidthPixels, "Raster identity width must match the frozen bitmap");
    AssertEqual(bitmap.PixelHeight, first.RasterIdentity.HeightPixels, "Raster identity height must match the frozen bitmap");
    AssertEqual(300, first.RasterIdentity.DpiX, "Preview raster identity must record the requested X DPI");
    AssertEqual(300, first.RasterIdentity.DpiY, "Preview raster identity must record the requested Y DPI");
    AssertEqual("PBGRA32", first.RasterIdentity.PixelFormat, "Preview raster identity must record the worker pixel format");

    var repeated = await PreviewRasterizer.RenderSnapshotAsync(
        snapshot.Drawing,
        snapshot.WidthDip,
        snapshot.HeightDip,
        CancellationToken.None);
    AssertEqual(first.RasterIdentity.Fingerprint, repeated.RasterIdentity.Fingerprint, "Repeated preview renders must have the same golden fingerprint");

    var changedFrame = await PreviewRasterizer.RenderSnapshotAsync(
        snapshot.Drawing,
        snapshot.WidthDip + 10,
        snapshot.HeightDip,
        CancellationToken.None);
    AssertEqual(false, string.Equals(first.RasterIdentity.Fingerprint, changedFrame.RasterIdentity.Fingerprint, StringComparison.Ordinal), "A changed device frame must not reuse the previous golden fingerprint");
}

static Task TestPreviewRasterRejectsInvalidDimensions()
{
    var drawing = new DrawingGroup();
    drawing.Freeze();
    var rejected = 0;
    foreach (var dimensions in new[]
    {
        (Width: double.NaN, Height: 100d),
        (Width: double.PositiveInfinity, Height: 100d),
        (Width: -1d, Height: 100d)
    })
    {
        try
        {
            _ = PreviewRasterizer.RenderAsync(drawing, dimensions.Width, dimensions.Height, CancellationToken.None);
        }
        catch (InvalidOperationException)
        {
            rejected++;
        }
    }

    AssertEqual(3, rejected, "Invalid preview dimensions must fail before entering the worker queue");
    AssertEqual(0, PreviewRasterizer.PendingRequestCount, "Rejected dimensions must not leave a pending raster request");
    return Task.CompletedTask;
}

static Task TestPreviewMetadataStaysLazyAt10k()
{
    var pages = PrintPreviewPageViewModel.CreateMetadata(10_000, 120, 80);
    AssertEqual(10_000, pages.Count, "Preview metadata must represent every requested page");
    AssertEqual(1, pages[0].PageNumber, "Preview metadata must start page numbering at one");
    AssertEqual(10_000, pages[^1].PageNumber, "Preview metadata must preserve the last page number");
    AssertNear(120, pages[1234].Width, 0.001, "Preview metadata must preserve the label width without rendering");
    AssertNear(80, pages[1234].Height, 0.001, "Preview metadata must preserve the label height without rendering");
    AssertEqual(true, pages.All(page => page.PreviewImage is null), "10k-page metadata must not eagerly allocate preview bitmaps");
    return Task.CompletedTask;
}

static async Task TestPreviewRasterCoalesces10kRequests()
{
    var template = new LabelTemplate
    {
        Name = "Raster 10k burst",
        WidthMm = 20,
        HeightMm = 10,
        Dpi = 300
    };
    template.Objects.Add(new LabelObject
    {
        Type = ObjectType.Rectangle,
        XMm = 1,
        YMm = 1,
        WidthMm = 8,
        HeightMm = 4,
        Style = { FillStyle = FillStyle.Solid, FillColor = "#000000" }
    });

    var snapshot = new PrintService().CreatePreviewDrawing(template, null);
    var requestsBefore = PreviewRasterizer.RequestCount;
    var supersededBefore = PreviewRasterizer.SupersededRequestCount;
    var workerStartsBefore = PreviewRasterizer.WorkerStartCount;
    var tasks = new Task<ImageSource>[10_000];
    for (var index = 0; index < tasks.Length; index++)
    {
        tasks[index] = PreviewRasterizer.RenderAsync(
            snapshot.Drawing,
            snapshot.WidthDip,
            snapshot.HeightDip,
            CancellationToken.None);
    }

    try
    {
        await Task.WhenAll(tasks);
    }
    catch (OperationCanceledException)
    {
        // Every superseded request is intentionally canceled; the newest request
        // is awaited below so this test still proves a usable final preview.
    }

    var finalImage = await tasks[^1];
    AssertEqual(true, finalImage is BitmapSource { IsFrozen: true }, "The newest 10k-burst request must produce a frozen bitmap");
    AssertEqual(10_000L, PreviewRasterizer.RequestCount - requestsBefore, "The worker telemetry must account for every burst request");
    AssertEqual(true, PreviewRasterizer.SupersededRequestCount - supersededBefore > 0, "The pending queue must coalesce at least one superseded request instead of retaining every burst item");
    AssertEqual(1, PreviewRasterizer.MaxPendingRequestCountObserved, "Preview raster queue high-water must remain one pending request");
    AssertEqual(workerStartsBefore, PreviewRasterizer.WorkerStartCount, "A 10k burst must reuse the existing STA worker");
    AssertEqual(0, PreviewRasterizer.PendingRequestCount, "The pending request slot must be empty after the burst completes");
}

static async Task TestPreview10kStressBudget()
{
    var template = new LabelTemplate
    {
        Name = "Preview stress budget",
        WidthMm = 120,
        HeightMm = 80,
        Dpi = 300
    };
    template.Objects.Add(new LabelObject
    {
        Type = ObjectType.Rectangle,
        XMm = 2,
        YMm = 2,
        WidthMm = 116,
        HeightMm = 76,
        Style = { FillStyle = FillStyle.Solid, FillColor = "#FFFFFFFF", BorderThicknessMm = 0.3 }
    });
    template.Objects.Add(new LabelObject
    {
        Type = ObjectType.Text,
        Text = "10k preview stress",
        XMm = 4,
        YMm = 4,
        WidthMm = 60,
        HeightMm = 8
    });

    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();
    var process = Process.GetCurrentProcess();
    var managedBefore = GC.GetTotalMemory(forceFullCollection: true);
    var workingSetBefore = process.WorkingSet64;
    var privateBefore = process.PrivateMemorySize64;
    var pages = PrintPreviewPageViewModel.CreateMetadata(10_000, 120, 80);
    var snapshot = new PrintService().CreatePreviewDrawing(template, null);
    var cacheSizedImages = new List<ImageSource>(8);
    for (var index = 0; index < 8; index++)
    {
        cacheSizedImages.Add(await PreviewRasterizer.RenderAsync(
            snapshot.Drawing,
            snapshot.WidthDip,
            snapshot.HeightDip,
            CancellationToken.None));
    }

    var managedAfter = GC.GetTotalMemory(forceFullCollection: false);
    var workingSetAfter = process.WorkingSet64;
    var privateAfter = process.PrivateMemorySize64;
    var managedDelta = Math.Max(0, managedAfter - managedBefore);
    var workingSetDelta = Math.Max(0, workingSetAfter - workingSetBefore);
    var privateDelta = Math.Max(0, privateAfter - privateBefore);
    var measuredDelta = Math.Max(managedDelta, Math.Max(workingSetDelta, privateDelta));
    var measuredDeltaMb = measuredDelta / (1024d * 1024d);
    var estimatedBitmapMb = cacheSizedImages.OfType<BitmapSource>()
        .Sum(bitmap => (long)bitmap.PixelWidth * bitmap.PixelHeight * 4) / (1024d * 1024d);
    Console.WriteLine($"INFO preview stress: pages={pages.Count}; cacheImages={cacheSizedImages.Count}; managedDeltaMB={managedDelta / (1024d * 1024d):0.0}; workingSetDeltaMB={workingSetDelta / (1024d * 1024d):0.0}; privateDeltaMB={privateDelta / (1024d * 1024d):0.0}; measuredDeltaMB={measuredDeltaMb:0.0}; estimatedBitmapMB={estimatedBitmapMb:0.0}");
    AssertEqual(true, measuredDelta < 300L * 1024 * 1024, $"10k metadata plus an 8-image cache must stay below 300 MB (measured {measuredDeltaMb:0.0} MB)");
    AssertEqual(10_000, pages.Count, "Stress metadata must retain all 10k page entries");
    AssertEqual(8, cacheSizedImages.Count, "Stress cache simulation must retain only the intended LRU capacity");

    using var canceled = new CancellationTokenSource();
    var cancelWatch = Stopwatch.StartNew();
    canceled.Cancel();
    var canceledBeforeStart = false;
    try
    {
        await PreviewRasterizer.RenderAsync(snapshot.Drawing, snapshot.WidthDip, snapshot.HeightDip, canceled.Token);
    }
    catch (OperationCanceledException)
    {
        canceledBeforeStart = true;
    }

    cancelWatch.Stop();
    Console.WriteLine($"INFO preview cancel: latencyMs={cancelWatch.Elapsed.TotalMilliseconds:0.0}; beforeStart={canceledBeforeStart}");
    AssertEqual(true, canceledBeforeStart, "Canceled preview requests must be rejected without allocating a bitmap");
    AssertEqual(true, cancelWatch.Elapsed < TimeSpan.FromSeconds(1), "Canceled preview request must return within the one-second operator budget");
}

static async Task TestPreviewRasterLongSoak()
{
    var template = new LabelTemplate
    {
        Name = "Preview long soak",
        WidthMm = 30,
        HeightMm = 15,
        Dpi = 300
    };
    template.Objects.Add(new LabelObject
    {
        Type = ObjectType.Rectangle,
        XMm = 1,
        YMm = 1,
        WidthMm = 28,
        HeightMm = 13,
        Style = { FillStyle = FillStyle.Solid, FillColor = "#202020" }
    });

    var snapshot = new PrintService().CreatePreviewDrawing(template, null);
    var workerStartsBefore = PreviewRasterizer.WorkerStartCount;
    var completedBefore = PreviewRasterizer.RenderCompletedCount;
    var canceledBefore = PreviewRasterizer.RenderCanceledCount;
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();
    var privateBefore = Process.GetCurrentProcess().PrivateMemorySize64;
    ImageSource? lastImage = null;
    const int cycles = 300;
    for (var index = 0; index < cycles; index++)
    {
        using var cycleCancellation = new CancellationTokenSource();
        var task = PreviewRasterizer.RenderAsync(snapshot.Drawing, snapshot.WidthDip, snapshot.HeightDip, cycleCancellation.Token);
        if (index % 17 == 0)
        {
            cycleCancellation.Cancel();
            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
                // Expected: cancellation is part of the page-navigation soak.
            }
        }
        else
        {
            lastImage = await task;
        }

        if (index % 75 == 0)
        {
            lastImage = null;
            GC.Collect();
        }
    }

    lastImage = null;
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();
    var privateAfter = Process.GetCurrentProcess().PrivateMemorySize64;
    var privateDeltaMb = Math.Max(0, privateAfter - privateBefore) / (1024d * 1024d);
    Console.WriteLine($"INFO preview long-soak: cycles={cycles}; completedDelta={PreviewRasterizer.RenderCompletedCount - completedBefore}; canceledDelta={PreviewRasterizer.RenderCanceledCount - canceledBefore}; privateDeltaMB={privateDeltaMb:0.0}; pending={PreviewRasterizer.PendingRequestCount}");

    AssertEqual(workerStartsBefore, PreviewRasterizer.WorkerStartCount, "Long-soak preview must reuse one STA worker");
    AssertEqual(0, PreviewRasterizer.PendingRequestCount, "Long-soak preview must leave no pending request");
    AssertEqual(true, PreviewRasterizer.RenderCompletedCount - completedBefore > cycles - 30, "Most long-soak requests must complete rather than leak or stall");
    AssertEqual(true, PreviewRasterizer.RenderCanceledCount - canceledBefore > 0, "Long-soak navigation must record cancellation evidence");
    AssertEqual(true, privateDeltaMb < 180, $"Long-soak private memory growth must stay below 180 MB (measured {privateDeltaMb:0.0} MB)");
}

static Task TestTextLayoutPolicyParity()
{
    const double widthDip = 100;
    const double heightDip = 24;
    var left = new LabelObject
    {
        Type = ObjectType.Text,
        Text = "left aligned static text",
        Style = { Alignment = TextAlignmentMode.Left }
    };
    var centered = new LabelObject
    {
        Type = ObjectType.Text,
        Text = "centered static text",
        Style = { Alignment = TextAlignmentMode.Center }
    };
    var justified = new LabelObject
    {
        Type = ObjectType.Text,
        Text = "justified static text with a long enough line",
        Style = { Alignment = TextAlignmentMode.Justify }
    };
    var textBox = new LabelObject
    {
        Type = ObjectType.TextBox,
        Text = "wrapped text",
        Style = { Alignment = TextAlignmentMode.Left, LineHeightPt = 18 }
    };

    var leftMetrics = TextBoxOverflowDetector.CreateFormattedText(left, left.Text, Brushes.Black);
    var centeredMetrics = TextBoxOverflowDetector.CreateFormattedText(centered, centered.Text, Brushes.Black);
    var justifiedMetrics = TextBoxOverflowDetector.CreateFormattedText(justified, justified.Text, Brushes.Black);
    var boxMetrics = TextBoxOverflowDetector.CreateFormattedText(textBox, TextBoxOverflowDetector.WrapTextToBox(textBox, textBox.Text, widthDip), Brushes.Black);

    TextBoxOverflowDetector.ApplyLayoutBounds(leftMetrics, left, widthDip, heightDip, constrainToBox: false);
    TextBoxOverflowDetector.ApplyLayoutBounds(centeredMetrics, centered, widthDip, heightDip, constrainToBox: false);
    TextBoxOverflowDetector.ApplyLayoutBounds(justifiedMetrics, justified, widthDip, heightDip, constrainToBox: false);
    TextBoxOverflowDetector.ApplyLayoutBounds(boxMetrics, textBox, widthDip, heightDip, constrainToBox: true);

    AssertEqual(true, leftMetrics.MaxTextWidth <= 0 || leftMetrics.MaxTextWidth > widthDip * 10,
        "Left-aligned static Text must keep its persisted auto-size/unbounded width policy");
    AssertNear(widthDip - 4, centeredMetrics.MaxTextWidth, 0.001,
        "Centered static Text must use the same 2-DIP inset alignment frame in designer and print");
    AssertEqual(TextAlignment.Justify, justifiedMetrics.TextAlignment,
        "Justified text must use the same WPF paragraph alignment in designer and print");
    AssertNear(widthDip - 4, justifiedMetrics.MaxTextWidth, 0.001,
        "Justified static Text must use the same bounded alignment frame as centered/right text");
    AssertNear(widthDip, boxMetrics.MaxTextWidth, 0.001,
        "TextBox must keep its wrapped content width bounded to the object");
    AssertNear(heightDip, boxMetrics.MaxTextHeight, 0.001,
        "TextBox must keep its clipped content height bounded to the object");
    var boxLayout = TextBoxOverflowDetector.Measure(boxMetrics, textBox, widthDip, heightDip, constrainToBox: true);
    AssertEqual(true, boxLayout.BaselineDip > 0 && boxLayout.LineHeightDip > 0 && boxLayout.InkExtentDip > 0,
        "Shared text metrics must expose baseline, line-height and ink extent for alignment diagnostics");
    var leftInk = TextBoxOverflowDetector.GetInkBoundsDip(leftMetrics, new Point(2, 0));
    AssertEqual(true, !leftInk.IsEmpty && leftInk.Width > 0 && leftInk.Height > 0,
        "Optical alignment must measure actual glyph geometry, not only the authored frame");
    AssertEqual(true, boxLayout.LineCount >= 1, "Shared text metrics must report at least one display line");
    var explicitLayout = TextBoxOverflowDetector.CreateTextLayout(textBox, "line one\nline two", widthDip, 60, constrainToBox: true, Brushes.Black);
    var explicitInk = TextBoxOverflowDetector.GetInkBoundsDip(explicitLayout, new Point(0, 0));
    AssertEqual(true, !explicitInk.IsEmpty && explicitInk.Bottom > explicitInk.Top,
        "Explicit multi-line layouts must expose a combined visible ink bound");
    AssertNear(24, explicitLayout.Metrics.LineHeightDip, 0.001, "Explicit 18pt line height must resolve to 24 WPF DIP");
    AssertNear(48, explicitLayout.Metrics.HeightDip, 0.001, "Explicit line-height layout must use one line box per line");
    AssertEqual(2, explicitLayout.Metrics.LineCount, "Explicit line-height layout must preserve explicit line breaks");
    AssertEqual(false, explicitLayout.Metrics.IsOverflowing, "Explicit line-height layout must fit when the frame contains all lines");
    AssertNear(0, TextBoxOverflowDetector.ResolveVerticalOffset(textBox, boxMetrics.Height, heightDip, constrainToBox: true), 0.001,
        "TextBox default vertical policy must be top-aligned in both designer and print");
    return Task.CompletedTask;
}

static Task TestTextPaddingUsesSharedFrame()
{
    const double widthDip = 120;
    const double heightDip = 60;
    const double paddingMm = 1.5;
    var paddingDip = MmConverter.MmToDip(paddingMm);
    var box = new LabelObject
    {
        Type = ObjectType.TextBox,
        Text = "one\ntwo",
        Style =
        {
            TextPaddingMm = paddingMm,
            TextSizing = TextSizingMode.FixedFrame,
            VerticalAlignment = TextVerticalAlignmentMode.Top,
            LineHeightPt = 12
        }
    };

    var contentWidth = TextBoxOverflowDetector.GetContentWidthDip(box, widthDip, constrainToBox: true);
    var contentHeight = TextBoxOverflowDetector.GetContentHeightDip(box, heightDip, constrainToBox: true);
    AssertNear(widthDip - paddingDip * 2, contentWidth, 0.001,
        "TextBox content width must subtract the persisted physical padding on both sides");
    AssertNear(heightDip - paddingDip * 2, contentHeight, 0.001,
        "TextBox content height must subtract the persisted physical padding on both sides");
    AssertNear(paddingDip, TextBoxOverflowDetector.GetHorizontalOriginDip(box, constrainToBox: true), 0.001,
        "The text origin must use the same physical padding as the content rectangle");

    var formatted = TextBoxOverflowDetector.CreateFormattedText(box, box.Text, Brushes.Black);
    TextBoxOverflowDetector.ApplyLayoutBounds(formatted, box, widthDip, heightDip, constrainToBox: true);
    AssertNear(contentWidth, formatted.MaxTextWidth, 0.001,
        "Designer/print formatted text must receive the shared padded width");
    AssertNear(contentHeight, formatted.MaxTextHeight, 0.001,
        "Designer/print formatted text must receive the shared padded height");

    var layout = TextBoxOverflowDetector.CreateTextLayout(
        box,
        box.Text,
        widthDip,
        heightDip,
        constrainToBox: true,
        Brushes.Black);
    AssertNear(paddingDip, layout.Metrics.VerticalOffsetDip, 0.001,
        "Top-aligned text must start after the same physical top padding");
    AssertEqual(false, string.IsNullOrWhiteSpace(layout.Metrics.IdentityFingerprint),
        "Padding-aware text layout must still carry a deterministic identity");

    var firstIdentity = layout.Metrics.IdentityFingerprint;
    box.Style.TextPaddingMm = 0.5;
    var changedLayout = TextBoxOverflowDetector.CreateTextLayout(
        box,
        box.Text,
        widthDip,
        heightDip,
        constrainToBox: true,
        Brushes.Black);
    AssertEqual(false, string.Equals(firstIdentity, changedLayout.Metrics.IdentityFingerprint, StringComparison.Ordinal),
        "Changing physical text padding must invalidate the shared layout identity");

    var staticText = new LabelObject
    {
        Type = ObjectType.Text,
        Text = "center",
        Style = { Alignment = TextAlignmentMode.Center, TextPaddingMm = paddingMm }
    };
    var staticOrigin = TextBoxOverflowDetector.GetHorizontalOriginDip(staticText, constrainToBox: false);
    AssertNear(2 + paddingDip, staticOrigin, 0.001,
        "Static text must retain its legacy inset while adding explicit physical padding");
    AssertNear(widthDip - 2 * (2 + paddingDip),
        TextBoxOverflowDetector.GetContentWidthDip(staticText, widthDip, constrainToBox: false),
        0.001,
        "Static centered text must use the same padded alignment frame in preview and print");

    var edgeBox = new LabelObject
    {
        Type = ObjectType.TextBox,
        Text = "edge padding",
        Style =
        {
            TextPaddingLeftMm = 1,
            TextPaddingRightMm = 2,
            TextPaddingTopMm = 3,
            TextPaddingBottomMm = 4,
            VerticalAlignment = TextVerticalAlignmentMode.Top
        }
    };
    var leftDip = MmConverter.MmToDip(1);
    var rightDip = MmConverter.MmToDip(2);
    var topDip = MmConverter.MmToDip(3);
    var bottomDip = MmConverter.MmToDip(4);
    AssertNear(leftDip, TextBoxOverflowDetector.GetHorizontalOriginDip(edgeBox, constrainToBox: true), 0.001,
        "Per-edge layout must use the authored left inset as the text origin");
    AssertNear(widthDip - leftDip - rightDip,
        TextBoxOverflowDetector.GetContentWidthDip(edgeBox, widthDip, constrainToBox: true),
        0.001,
        "Per-edge layout must subtract independent left and right insets");
    AssertNear(heightDip - topDip - bottomDip,
        TextBoxOverflowDetector.GetContentHeightDip(edgeBox, heightDip, constrainToBox: true),
        0.001,
        "Per-edge layout must subtract independent top and bottom insets");
    var edgeLayout = TextBoxOverflowDetector.CreateTextLayout(edgeBox, edgeBox.Text, widthDip, heightDip, true, Brushes.Black);
    AssertNear(topDip, edgeLayout.Metrics.VerticalOffsetDip, 0.001,
        "Per-edge top padding must be included in the shared vertical origin");
    return Task.CompletedTask;
}

static Task TestStaticTextRendererHonorsPadding()
{
    Exception? failure = null;
    var thread = new Thread(() =>
    {
        try
        {
            const double paddingMm = 1.5;
            var plan = new PrintRenderPlan
            {
                Dpi = 300,
                DpiX = 300,
                DpiY = 300,
                LabelWidthMm = 50,
                LabelHeightMm = 30
            };

            static LabelTemplate CreateTemplate(double paddingMm)
            {
                var template = new LabelTemplate
                {
                    Name = "Static text padding parity",
                    WidthMm = 50,
                    HeightMm = 30
                };
                template.Objects.Add(new LabelObject
                {
                    Type = ObjectType.Text,
                    Text = "LEFT",
                    XMm = 5,
                    YMm = 5,
                    WidthMm = 25,
                    HeightMm = 8,
                    Style =
                    {
                        Alignment = TextAlignmentMode.Left,
                        TextPaddingMm = paddingMm
                    }
                });
                return template;
            }

            var renderer = new LabelVisualRenderer(new CapturingBarcodeRenderer());
            var withoutPadding = FindBlackGlyphBounds(renderer.Render(CreateTemplate(0), null, plan).Drawing);
            var withPadding = FindBlackGlyphBounds(renderer.Render(CreateTemplate(paddingMm), null, plan).Drawing);
            AssertNear(
                MmConverter.MmToDip(paddingMm),
                withPadding.Left - withoutPadding.Left,
                0.05,
                "Static text render origin must move by the authored physical left padding");
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

static Task TestTextLayoutIdentityFingerprint()
{
    var item = new LabelObject
    {
        Type = ObjectType.TextBox,
        Text = "שלום 123\nA\u0301 😀",
        Style =
        {
            FontFamily = "Arial",
            FontSizePt = 11,
            TextDirection = TextDirectionMode.Auto,
            LineHeightPt = 15,
            VerticalAlignment = TextVerticalAlignmentMode.Center
        }
    };

    var first = TextBoxOverflowDetector.CreateTextLayout(
        item,
        item.Text,
        widthDip: 120,
        heightDip: 60,
        constrainToBox: true,
        Brushes.Black,
        pixelsPerDip: 1.25);
    var second = TextBoxOverflowDetector.CreateTextLayout(
        item,
        item.Text,
        widthDip: 120,
        heightDip: 60,
        constrainToBox: true,
        Brushes.Black,
        pixelsPerDip: 1.25);

    AssertEqual(true, !string.IsNullOrWhiteSpace(first.Metrics.IdentityFingerprint),
        "Shared text layout must expose a value-only identity fingerprint");
    AssertEqual(first.Metrics.IdentityFingerprint, second.Metrics.IdentityFingerprint,
        "Repeated designer/preview/print layout calls must produce the same identity");

    item.Style.LineHeightPt = 16;
    var changed = TextBoxOverflowDetector.CreateTextLayout(
        item,
        item.Text,
        widthDip: 120,
        heightDip: 60,
        constrainToBox: true,
        Brushes.Black,
        pixelsPerDip: 1.25);
    AssertEqual(false, string.Equals(first.Metrics.IdentityFingerprint, changed.Metrics.IdentityFingerprint, StringComparison.Ordinal),
        "A changed persisted line-height policy must invalidate the layout identity");
    return Task.CompletedTask;
}

static Task TestTextBaselineSnapUsesSharedMetrics()
{
    var target = new LabelObject
    {
        Type = ObjectType.Text,
        Name = "Target",
        Text = "Target",
        XMm = 10,
        YMm = 10,
        WidthMm = 24,
        HeightMm = 8,
        Style = { FontSizePt = 12, Alignment = TextAlignmentMode.Left }
    };
    var dragged = new LabelObject
    {
        Type = ObjectType.Text,
        Name = "Dragged",
        Text = "Dragged",
        XMm = 45,
        YMm = 16,
        WidthMm = 24,
        HeightMm = 8,
        Style = { FontSizePt = 12, Alignment = TextAlignmentMode.Left }
    };
    var template = new LabelTemplate { WidthMm = 100, HeightMm = 50 };
    template.Objects.Add(target);
    template.Objects.Add(dragged);

    var widthDip = MmConverter.MmToDip(target.WidthMm);
    var heightDip = MmConverter.MmToDip(target.HeightMm);
    var targetText = TextBoxOverflowDetector.CreateFormattedText(target, target.Text, Brushes.Black);
    TextBoxOverflowDetector.ApplyLayoutBounds(targetText, target, widthDip, heightDip, constrainToBox: false);
    var targetMetrics = TextBoxOverflowDetector.Measure(targetText, target, widthDip, heightDip, constrainToBox: false);
    var targetBaselineMm = target.YMm + MmConverter.DipToMm(targetMetrics.VerticalOffsetDip + targetMetrics.BaselineDip);

    var draggedText = TextBoxOverflowDetector.CreateFormattedText(dragged, dragged.Text, Brushes.Black);
    TextBoxOverflowDetector.ApplyLayoutBounds(draggedText, dragged, widthDip, heightDip, constrainToBox: false);
    var draggedMetrics = TextBoxOverflowDetector.Measure(draggedText, dragged, widthDip, heightDip, constrainToBox: false);
    var draggedBaselineOffsetMm = MmConverter.DipToMm(draggedMetrics.VerticalOffsetDip + draggedMetrics.BaselineDip);
    var proposedY = targetBaselineMm - draggedBaselineOffsetMm + 0.4;

    double? snapY = null;
    Exception? failure = null;
    var thread = new Thread(() =>
    {
        try
        {
            var canvas = new LabelDesignerCanvas { Zoom = 1, IsSnapToObjectsEnabled = true, Template = template };
            var snapMethod = typeof(LabelDesignerCanvas).GetMethod(
                "ComputePriorityAlignmentSnap",
                BindingFlags.Instance | BindingFlags.NonPublic);
            AssertEqual(true, snapMethod is not null, "Designer must expose one priority snap path for the interaction contract");

            var snap = snapMethod!.Invoke(canvas, new object[] { dragged, dragged.XMm, proposedY });
            snapY = (double?)snap!.GetType().GetProperty("SnapY")!.GetValue(snap);
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

    AssertEqual(true, snapY is not null, "Text baseline should be an eligible snap target within the acquire tolerance");
    AssertNear(targetBaselineMm - draggedBaselineOffsetMm, snapY!.Value, 0.001,
        "Baseline snapping must resolve to the target first baseline using the shared text metrics");
    return Task.CompletedTask;
}

static Task TestOpticalTextAlignment()
{
    var target = new LabelObject
    {
        Type = ObjectType.Text,
        Name = "Optical target",
        Text = "A",
        XMm = 10,
        YMm = 10,
        WidthMm = 20,
        HeightMm = 8
    };
    var dragged = new LabelObject
    {
        Type = ObjectType.Text,
        Name = "Optical source",
        Text = "A",
        XMm = 40,
        YMm = 10,
        WidthMm = 20,
        HeightMm = 8
    };
    var template = new LabelTemplate { WidthMm = 100, HeightMm = 50 };
    template.Objects.Add(target);
    template.Objects.Add(dragged);

    var completed = 0;
    var canceled = 0;
    Exception? failure = null;
    var thread = new Thread(() =>
    {
        try
        {
            var canvas = new LabelDesignerCanvas { Template = template, SelectedObject = target };
            canvas.EditGestureCompleted += (_, _) => completed++;
            canvas.EditGestureCanceled += (_, _) => canceled++;
            var selectedField = typeof(LabelDesignerCanvas).GetField(
                "_selectedObjects",
                BindingFlags.Instance | BindingFlags.NonPublic);
            AssertEqual(true, selectedField is not null, "Canvas must retain one internal selection set for arrange commands");
            var selected = (HashSet<LabelObject>)selectedField!.GetValue(canvas)!;
            selected.Add(target);
            selected.Add(dragged);

            var changed = canvas.AlignSelectedTextOptically();
            AssertEqual(true, changed, "Optical alignment should move equal glyphs with different frame origins");
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

    AssertNear(10, dragged.XMm, 0.001, "Optical ink center alignment must move the source by the measured visible-ink delta");
    AssertEqual(1, completed, "Optical alignment must commit exactly one edit gesture");
    AssertEqual(0, canceled, "Successful optical alignment must not report a canceled gesture");
    return Task.CompletedTask;
}

static Task TestSmartSpacingAndGridSnap()
{
    var first = new LabelObject { Type = ObjectType.Rectangle, Name = "First", XMm = 10, YMm = 10, WidthMm = 10, HeightMm = 10 };
    var second = new LabelObject { Type = ObjectType.Rectangle, Name = "Second", XMm = 30, YMm = 10, WidthMm = 10, HeightMm = 10 };
    var moving = new LabelObject { Type = ObjectType.Rectangle, Name = "Moving", XMm = 55, YMm = 30, WidthMm = 10, HeightMm = 10 };
    var template = new LabelTemplate { WidthMm = 100, HeightMm = 60 };
    template.Objects.Add(first);
    template.Objects.Add(second);
    template.Objects.Add(moving);

    double? snappedX = null;
    Exception? failure = null;
    var thread = new Thread(() =>
    {
        try
        {
            var canvas = new LabelDesignerCanvas
            {
                Zoom = 1,
                IsSnapToObjectsEnabled = true,
                IsSnapToGridEnabled = true,
                GridStepMm = 1,
                Template = template
            };
            var snapMethod = typeof(LabelDesignerCanvas).GetMethod(
                "ComputePriorityAlignmentSnap",
                BindingFlags.Instance | BindingFlags.NonPublic);
            AssertEqual(true, snapMethod is not null, "Designer must use one priority path for grid and spacing candidates");
            var candidateMethod = typeof(LabelDesignerCanvas).GetMethod(
                "AddSmartSpacingCandidates",
                BindingFlags.Instance | BindingFlags.NonPublic);
            AssertEqual(true, candidateMethod is not null, "Designer must expose the shared smart-spacing candidate builder");
            var xCandidates = new List<SnapCandidate>();
            var yCandidates = new List<SnapCandidate>();
            candidateMethod!.Invoke(canvas, new object[]
            {
                xCandidates,
                yCandidates,
                51d,
                61d,
                moving.YMm,
                moving.YMm + moving.HeightMm,
                moving.WidthMm,
                moving.HeightMm,
                new HashSet<LabelObject> { moving },
                "fixture"
            });
            var spacingCandidate = xCandidates.FirstOrDefault(candidate => candidate.StableKey.Contains(":spacing:x:", StringComparison.Ordinal)
                && Math.Abs(candidate.TargetPosition - 50) < 0.001);
            AssertEqual(true, spacingCandidate.StableKey is not null && spacingCandidate.TargetPosition == 50,
                "The equal-spacing builder must offer the measured 50 mm leading position");
            var snap = snapMethod!.Invoke(canvas, new object[] { moving, 51d, moving.YMm });
            snappedX = (double?)snap!.GetType().GetProperty("SnapX")!.GetValue(snap);
            var snapCaption = (string?)snap.GetType().GetProperty("XCaption")!.GetValue(snap);
            AssertEqual("gap 10 mm", snapCaption,
                "The winning spacing candidate must expose a truthful gap caption for the guide overlay");

            canvas.IsSnapToObjectsEnabled = false;
            canvas.IsSnapToGridEnabled = true;
            var gridOnlySnap = snapMethod.Invoke(canvas, new object[] { moving, 50.4d, moving.YMm });
            var gridOnlyX = (double?)gridOnlySnap!.GetType().GetProperty("SnapX")!.GetValue(gridOnlySnap);
            AssertNear(50, gridOnlyX ?? double.NaN, 0.001,
                "Grid snap must remain independently usable when object snapping is disabled");
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

    AssertEqual(true, snappedX is not null, "Equal-spacing candidate should be eligible inside the acquire tolerance");
    AssertNear(50, snappedX!.Value, 0.001,
        "Semantic equal-spacing snap must win over the closer but lower-priority grid candidate");
    return Task.CompletedTask;
}

static Task TestRotatedGroupMoveUsesTransformedBounds()
{
    var rotated = new LabelObject
    {
        Id = "rotated-group",
        Type = ObjectType.Rectangle,
        Name = "Rotated group member",
        XMm = 10,
        YMm = 10,
        WidthMm = 20,
        HeightMm = 6,
        Rotation = 90
    };
    var peer = new LabelObject
    {
        Id = "peer-group",
        Type = ObjectType.Rectangle,
        Name = "Peer group member",
        XMm = 40,
        YMm = 20,
        WidthMm = 5,
        HeightMm = 5
    };
    var template = new LabelTemplate { WidthMm = 100, HeightMm = 60 };
    template.Objects.Add(rotated);
    template.Objects.Add(peer);

    Rect? observed = null;
    Exception? failure = null;
    var thread = new Thread(() =>
    {
        try
        {
            var canvas = new LabelDesignerCanvas { Zoom = 1, Template = template };
            var capture = typeof(LabelDesignerCanvas).GetMethod(
                "CaptureGroupDragStarts",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var boundsMethod = typeof(LabelDesignerCanvas).GetMethod(
                "GetGroupBoundsMm",
                BindingFlags.Instance | BindingFlags.NonPublic);
            AssertEqual(true, capture is not null && boundsMethod is not null,
                "Group move must expose one internal start/hull path for transformed geometry");

            capture!.Invoke(canvas, new object[] { new[] { rotated, peer } });
            observed = (Rect)boundsMethod!.Invoke(canvas, null)!;
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

    AssertEqual(true, observed is not null, "Group move must produce a finite selection hull");
    AssertNear(17, observed!.Value.Left, 0.001,
        "A 90-degree member must contribute its transformed left edge to the group hull");
    AssertNear(3, observed.Value.Top, 0.001,
        "A 90-degree member must contribute its transformed top edge to the group hull");
    AssertNear(45, observed.Value.Right, 0.001,
        "The group hull must include the peer's right edge");
    AssertNear(25, observed.Value.Bottom, 0.001,
        "The group hull must include the peer's bottom edge");
    return Task.CompletedTask;
}

static Task TestLineStrokeBoundsParity()
{
    var line = new LabelObject
    {
        Id = "edge-line",
        Type = ObjectType.Line,
        Name = "Edge line",
        XMm = 1,
        YMm = 5,
        WidthMm = 28.9,
        HeightMm = 0.5,
        LineEndXMm = 29.9,
        LineEndYMm = 5,
        Style = { OutlineStyle = OutlineStyle.Solid, BorderThicknessMm = 0.4 }
    };
    var template = new LabelTemplate { WidthMm = 30, HeightMm = 20, Dpi = 300 };
    template.Objects.Add(line);

    Rect? observed = null;
    Exception? failure = null;
    var thread = new Thread(() =>
    {
        try
        {
            var canvas = new LabelDesignerCanvas { Zoom = 1, Template = template };
            var capture = typeof(LabelDesignerCanvas).GetMethod(
                "CaptureGroupDragStarts",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var boundsMethod = typeof(LabelDesignerCanvas).GetMethod(
                "GetGroupBoundsMm",
                BindingFlags.Instance | BindingFlags.NonPublic);
            AssertEqual(true, capture is not null && boundsMethod is not null,
                "Line group geometry must expose the shared stroke-aware bounds path");

            capture!.Invoke(canvas, new object[] { new[] { line } });
            observed = (Rect)boundsMethod!.Invoke(canvas, null)!;
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

    AssertEqual(true, observed is not null, "Line group geometry must produce a finite safety hull");
    AssertNear(0.8, observed!.Value.Left, 0.001,
        "Designer group bounds must include half the physical line stroke on the leading edge");
    AssertNear(30.1, observed.Value.Right, 0.001,
        "Designer group bounds must include half the physical line stroke at the label edge");

    var preflight = new PrintService().ValidateRows(template, new IReadOnlyDictionary<string, string>?[] { null });
    AssertEqual(false, preflight.IsSuccess,
        "Print preflight must block a line whose visible stroke crosses the design label");
    AssertEqual(true, preflight.ToUserMessage().Contains("outside the design label", StringComparison.OrdinalIgnoreCase),
        "Line stroke diagnostics must explain that the visible safety hull is outside the label");
    return Task.CompletedTask;
}

static Task TestTextDirectionPolicy()
{
    var arabic = new LabelObject
    {
        Type = ObjectType.Text,
        Text = "مرحبا ABC-123"
    };
    var latin = new LabelObject
    {
        Type = ObjectType.Text,
        Text = "ABC-123"
    };

    AssertEqual(System.Windows.FlowDirection.RightToLeft,
        TextBoxOverflowDetector.ResolveFlowDirection(arabic, arabic.Text),
        "Auto text direction must resolve from the first strong RTL letter");
    AssertEqual(System.Windows.FlowDirection.LeftToRight,
        TextBoxOverflowDetector.ResolveFlowDirection(latin, latin.Text),
        "Auto text direction must keep the legacy LTR default for Latin identifiers");

    arabic.Style.TextDirection = TextDirectionMode.LeftToRight;
    AssertEqual(System.Windows.FlowDirection.LeftToRight,
        TextBoxOverflowDetector.ResolveFlowDirection(arabic, arabic.Text),
        "Explicit LTR direction must override automatic content detection");
    arabic.Style.TextDirection = TextDirectionMode.RightToLeft;
    var formatted = TextBoxOverflowDetector.CreateFormattedText(arabic, arabic.Text, Brushes.Black);
    AssertEqual(System.Windows.FlowDirection.RightToLeft, formatted.FlowDirection,
        "FormattedText must carry the resolved RTL base direction");

    var graphemeItem = new LabelObject { Type = ObjectType.TextBox, Style = { FontSizePt = 12 } };
    var wrapped = TextBoxOverflowDetector.WrapTextToBox(graphemeItem, "e\u0301X", 1);
    AssertEqual(false, wrapped.Contains("e\n\u0301", StringComparison.Ordinal),
        "Text wrapping must not split a combining-mark grapheme cluster");

    return Task.CompletedTask;
}

static Task TestEditGestureHistory()
{
    var vm = new MainViewModel();
    vm.BeginTemplateEditGesture();
    vm.Template.Objects.Add(new LabelObject
    {
        Id = "gesture-object",
        Type = ObjectType.Rectangle,
        Name = "Gesture rectangle",
        XMm = 5,
        YMm = 5,
        WidthMm = 10,
        HeightMm = 6
    });
    var edited = vm.Template.Objects.Single();
    edited.XMm = 12;
    edited.XMm = 18;
    vm.CommitTemplateEditGesture();

    AssertEqual(1, vm.Template.Objects.Count, "Committed gesture must preserve the final object");
    AssertNear(18, vm.Template.Objects[0].XMm, 0.001, "Committed gesture must preserve the final geometry");
    vm.UndoCommand.Execute(null);
    AssertEqual(0, vm.Template.Objects.Count, "One committed gesture must undo as one step");
    vm.RedoCommand.Execute(null);
    AssertNear(18, vm.Template.Objects.Single().XMm, 0.001, "Redo must restore the committed gesture geometry");

    var beforeCancel = vm.Template.Objects.Single().XMm;
    vm.BeginTemplateEditGesture();
    vm.Template.Objects.Single().XMm = beforeCancel + 20;
    vm.CancelTemplateEditGesture();
    AssertNear(beforeCancel, vm.Template.Objects.Single().XMm, 0.001, "Canceled gesture must restore the exact pre-gesture geometry");
    return Task.CompletedTask;
}

static Task TestCanvasRefreshPreservesIdentity()
{
    var first = new LabelObject
    {
        Id = "canvas-first",
        Type = ObjectType.Rectangle,
        Name = "First",
        XMm = 8,
        YMm = 6,
        WidthMm = 12,
        HeightMm = 8
    };
    var template = new LabelTemplate { WidthMm = 100, HeightMm = 50 };
    template.Objects.Add(first);

    Exception? failure = null;
    var thread = new Thread(() =>
    {
        try
        {
            var canvas = new LabelDesignerCanvas { Template = template, Zoom = 1 };
            var elementsField = typeof(LabelDesignerCanvas).GetField(
                "_objectElements",
                BindingFlags.Instance | BindingFlags.NonPublic);
            AssertEqual(true, elementsField is not null, "Canvas must retain an object-to-visual identity map");

            var elements = (Dictionary<LabelObject, FrameworkElement>)elementsField!.GetValue(canvas)!;
            var firstElement = elements[first];
            // This is the shell/VM binding path, not the pointer handler.  It
            // must still hydrate the canvas-owned selection set and adorner.
            canvas.SelectedObject = first;

            canvas.Zoom = 2.5;
            AssertEqual(true, ReferenceEquals(firstElement, elements[first]),
                "Zoom refresh must reposition the existing visual instead of rebuilding it");
            AssertEqual(1, canvas.SelectedObjectCount,
                "Zoom refresh must retain the active multi-selection");
            AssertEqual(true, ReferenceEquals(first, canvas.SelectedObject),
                "Zoom refresh must retain the key object identity");

            var second = new LabelObject
            {
                Id = "canvas-second",
                Type = ObjectType.Ellipse,
                Name = "Second",
                XMm = 30,
                YMm = 10,
                WidthMm = 10,
                HeightMm = 10
            };
            template.Objects.Add(second);
            AssertEqual(2, elements.Count,
                "Adding one object must reconcile only the new visual into the existing map");
            AssertEqual(true, ReferenceEquals(firstElement, elements[first]),
                "Adding an object must not replace existing text/image/shape hosts");
            AssertEqual(true, ReferenceEquals(first, canvas.SelectedObject),
                "Adding an object must not change the key object");

            template.Objects.Remove(second);
            AssertEqual(1, elements.Count,
                "Removing one object must remove only its visual");
            AssertEqual(true, ReferenceEquals(firstElement, elements[first]),
                "Removing an object must keep surviving visuals stable");

            var replacement = new LabelObject
            {
                Id = first.Id,
                Type = ObjectType.Rectangle,
                Name = "Replacement",
                XMm = first.XMm,
                YMm = first.YMm,
                WidthMm = first.WidthMm,
                HeightMm = first.HeightMm
            };
            template.Objects[0] = replacement;
            AssertEqual(1, elements.Count,
                "Replacing one object must leave one reconciled visual");
            AssertEqual(true, elements.ContainsKey(replacement),
                "Replace must attach the new object to the visual map");
            AssertEqual(true, ReferenceEquals(replacement, canvas.SelectedObject),
                "Replace with a stable ID must restore the key object by identity contract");
            AssertEqual(1, canvas.SelectedObjectCount,
                "Replace with a stable ID must preserve one selected object");
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

static Task TestKeyObjectPreservesMultiSelection()
{
    var first = new LabelObject
    {
        Id = "key-first",
        Type = ObjectType.Rectangle,
        Name = "First",
        XMm = 5,
        YMm = 5,
        WidthMm = 10,
        HeightMm = 8
    };
    var second = new LabelObject
    {
        Id = "key-second",
        Type = ObjectType.Rectangle,
        Name = "Second",
        XMm = 25,
        YMm = 5,
        WidthMm = 10,
        HeightMm = 8
    };
    var template = new LabelTemplate { WidthMm = 100, HeightMm = 50 };
    template.Objects.Add(first);
    template.Objects.Add(second);

    Exception? failure = null;
    var thread = new Thread(() =>
    {
        try
        {
            var canvas = new LabelDesignerCanvas { Template = template, SelectedObject = first };
            var selectedField = typeof(LabelDesignerCanvas).GetField(
                "_selectedObjects",
                BindingFlags.Instance | BindingFlags.NonPublic);
            AssertEqual(true, selectedField is not null, "Canvas must retain its internal multi-selection set");
            var selected = (HashSet<LabelObject>)selectedField!.GetValue(canvas)!;
            selected.Add(second);

            AssertEqual(true, canvas.SetKeyObject(second), "A selected peer must be eligible as the key object");
            AssertEqual(2, canvas.SelectedObjectCount, "Changing the key object must preserve every selected peer");
            AssertEqual(true, ReferenceEquals(second, canvas.SelectedObject), "The requested peer must become the key object");

            var outside = new LabelObject { Id = "outside-key", Type = ObjectType.Rectangle };
            AssertEqual(false, canvas.SetKeyObject(outside), "An object outside the current selection must not become key");
            AssertEqual(2, canvas.SelectedObjectCount, "A rejected key change must not mutate the selection");
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

static Task TestResizeCancelRestoresSingleObject()
{
    var item = new LabelObject
    {
        Id = "resize-cancel",
        Type = ObjectType.Rectangle,
        Name = "Resize cancel",
        XMm = 12,
        YMm = 8,
        WidthMm = 24,
        HeightMm = 10
    };
    var template = new LabelTemplate { WidthMm = 100, HeightMm = 50 };
    template.Objects.Add(item);

    Exception? failure = null;
    var thread = new Thread(() =>
    {
        try
        {
            var canvas = new LabelDesignerCanvas { Template = template };
            var capture = typeof(LabelDesignerCanvas).GetMethod(
                "CaptureGroupResizeSnapshot",
                BindingFlags.Static | BindingFlags.NonPublic);
            var restore = typeof(LabelDesignerCanvas).GetMethod(
                "RestoreSingleResizeStart",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var startField = typeof(LabelDesignerCanvas).GetField(
                "_singleResizeStart",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var activeField = typeof(LabelDesignerCanvas).GetField(
                "_singleResizeActive",
                BindingFlags.Instance | BindingFlags.NonPublic);
            AssertEqual(true, capture is not null && restore is not null && startField is not null && activeField is not null,
                "Resize cancel must retain a private start-frame restore seam");

            startField!.SetValue(canvas, capture!.Invoke(null, new object[] { item }));
            activeField!.SetValue(canvas, true);
            AssertEqual(true, (bool)activeField.GetValue(canvas)!, "Resize restore fixture must mark the gesture active");
            item.XMm = 30;
            item.YMm = 20;
            item.WidthMm = 40;
            item.HeightMm = 18;

            AssertEqual(true, (bool)activeField.GetValue(canvas)!, "Geometry updates must not clear an active resize gesture");
            restore!.Invoke(canvas, new object[] { item });
            AssertNear(12, item.XMm, 0.0001, "Canceled resize must restore the original X");
            AssertNear(8, item.YMm, 0.0001, "Canceled resize must restore the original Y");
            AssertNear(24, item.WidthMm, 0.0001, "Canceled resize must restore the original width");
            AssertNear(10, item.HeightMm, 0.0001, "Canceled resize must restore the original height");
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

static Task TestVectorBarcodeGeometryUsesDeviceDots()
{
    const int dpiX = 305;
    const int dpiY = 609;
    var bits = new[] { false, true, true, false, true, false, true, true, false, true, true, false };
    var template = new LabelTemplate
    {
        Name = "Vector barcode dot-grid test",
        WidthMm = 50,
        HeightMm = 30,
        Dpi = dpiX
    };
    var item = new LabelObject
    {
        Type = ObjectType.BarcodeCode128,
        Name = "Vector barcode",
        Text = "DOT-GRID",
        XMm = 2.35,
        YMm = 3.17,
        WidthMm = 12.7,
        HeightMm = 8.4,
        ShowBarcodeText = false
    };
    template.Objects.Add(item);

    var fakeRenderer = new CapturingBarcodeRenderer
    {
        VectorData = new BarcodeVectorData(bits.Length, 1, bits)
    };
    var renderer = new LabelVisualRenderer(fakeRenderer);
    var plan = new PrintRenderPlan
    {
        Dpi = dpiX,
        DpiX = dpiX,
        DpiY = dpiY,
        LabelWidthMm = template.WidthMm,
        LabelHeightMm = template.HeightMm
    };
    var visual = renderer.Render(template, null, plan);
    var blackBounds = FindBlackGeometryBounds(visual.Drawing);
    var expected = DeviceBarcodeLayout.Create(
        MmConverter.MmToDip(item.XMm),
        MmConverter.MmToDip(item.YMm),
        MmConverter.MmToDip(item.WidthMm),
        MmConverter.MmToDip(item.HeightMm),
        dpiX,
        dpiY,
        bits.Length,
        bits);

    AssertEqual(dpiX, fakeRenderer.LastDpi, "Vector barcode generation must use effective X DPI");
    var expectedDarkStartDot = expected.LeftDot + expected.DarkRuns.Min(run => run.StartDot);
    var expectedDarkEndDot = expected.LeftDot + expected.DarkRuns.Max(run => run.EndDotExclusive);
    AssertNear(DeviceDotQuantizer.DotsToDip(expectedDarkStartDot, dpiX), blackBounds.Left, 0.0001,
        "Vector barcode dark-run left edge must be quantized in X printer dots");
    AssertNear(DeviceDotQuantizer.DotsToDip(expected.TopDot, dpiY), blackBounds.Top, 0.0001,
        "Vector barcode top edge must be quantized in Y printer dots");
    AssertNear(DeviceDotQuantizer.DotsToDip(expectedDarkEndDot - expectedDarkStartDot, dpiX), blackBounds.Width, 0.0001,
        "Vector barcode dark-run width must cover the exact quantized X dot span");
    AssertNear(DeviceDotQuantizer.DotsToDip(expected.HeightDots, dpiY), blackBounds.Height, 0.0001,
        "Vector barcode height must cover the exact quantized Y dot span");
    return Task.CompletedTask;
}

static Rect FindBlackGeometryBounds(Drawing drawing)
{
    Rect? union = null;

    void Visit(Drawing current)
    {
        if (current is DrawingGroup group)
        {
            foreach (var child in group.Children)
            {
                Visit(child);
            }

            return;
        }

        if (current is not GeometryDrawing geometry
            || geometry.Brush is not SolidColorBrush brush
            || brush.Color != Colors.Black)
        {
            return;
        }

        var bounds = geometry.Geometry.Bounds;
        union = union is null ? bounds : Rect.Union(union.Value, bounds);
    }

    Visit(drawing);
    return union ?? throw new InvalidOperationException("Vector barcode produced no black geometry.");
}

static Rect FindBlackGlyphBounds(Drawing drawing)
{
    Rect? union = null;

    void Visit(Drawing current)
    {
        if (current is DrawingGroup group)
        {
            foreach (var child in group.Children)
            {
                Visit(child);
            }

            return;
        }

        if (current is not GlyphRunDrawing glyph)
        {
            return;
        }

        union = union is null ? glyph.Bounds : Rect.Union(union.Value, glyph.Bounds);
    }

    Visit(drawing);
    return union ?? throw new InvalidOperationException("Text renderer produced no black glyph geometry.");
}

static Task TestBarcodeHriReservesSharedLayout()
{
    var template = new LabelTemplate
    {
        Name = "HRI layout test",
        WidthMm = 60,
        HeightMm = 30,
        Dpi = 300
    };
    var item = new LabelObject
    {
        Type = ObjectType.BarcodeCode128,
        Name = "HRI barcode",
        Text = "ABC-123",
        XMm = 2,
        YMm = 2,
        WidthMm = 40,
        HeightMm = 12,
        BarcodeHriPlacement = BarcodeHriPlacement.Below,
        BarcodeTextFontSizePt = 7
    };
    template.Objects.Add(item);

    var fakeRenderer = new CapturingBarcodeRenderer
    {
        VectorData = new BarcodeVectorData(8, 1, new[] { false, true, true, false, true, false, true, false })
    };
    var expected = BarcodeHriTextLayout.Measure(
        BarcodeType.Code128,
        item.Text,
        item.WidthMm,
        item.HeightMm,
        item.BarcodeHriPlacement,
        item.BarcodeTextFontSizePt);
    AssertEqual(true, expected.IsValid && expected.IsEnabled, "The HRI fixture must have a valid visible strip");
    AssertNear(0, expected.SymbolTopMm, 0.0001, "Below placement keeps the symbol at the top of the frame");

    var renderer = new LabelVisualRenderer(fakeRenderer);
    renderer.Render(template, null, new PrintRenderPlan
    {
        Dpi = 300,
        DpiX = 300,
        DpiY = 300,
        LabelWidthMm = template.WidthMm,
        LabelHeightMm = template.HeightMm
    });

    AssertNear(expected.SymbolHeightMm, fakeRenderer.LastHeightMm, 0.0001,
        "Preview/print must render bars using the HRI-reserved symbol height");
    AssertNear(item.WidthMm, fakeRenderer.LastWidthMm, 0.0001,
        "HRI must not change the authored barcode width");
    return Task.CompletedTask;
}

static Task TestBarcodeHriAboveReservesTopStrip()
{
    var template = new LabelTemplate
    {
        Name = "HRI above test",
        WidthMm = 60,
        HeightMm = 30,
        Dpi = 300
    };
    var item = new LabelObject
    {
        Type = ObjectType.BarcodeCode128,
        Name = "HRI above barcode",
        Text = "ABC-123",
        XMm = 2,
        YMm = 2,
        WidthMm = 40,
        HeightMm = 12,
        BarcodeHriPlacement = BarcodeHriPlacement.Above,
        BarcodeTextFontSizePt = 7
    };
    template.Objects.Add(item);

    var fakeRenderer = new CapturingBarcodeRenderer
    {
        VectorData = new BarcodeVectorData(8, 1, new[] { false, true, true, false, true, false, true, false })
    };
    var expected = BarcodeHriTextLayout.Measure(
        BarcodeType.Code128,
        item.Text,
        item.WidthMm,
        item.HeightMm,
        item.BarcodeHriPlacement,
        item.BarcodeTextFontSizePt);
    AssertEqual(true, expected.IsValid && expected.IsEnabled, "Above HRI must produce a valid layout");
    AssertEqual(true, expected.SymbolTopMm > 0.5, "Above placement must push the symbol below the HRI strip");
    AssertNear(expected.HriHeightMm + expected.GapMm, expected.SymbolTopMm, 0.0001,
        "Symbol top must start after HRI strip + gap");

    var noneLayout = BarcodeHriTextLayout.Measure(
        BarcodeType.Code128,
        item.Text,
        item.WidthMm,
        item.HeightMm,
        BarcodeHriPlacement.None,
        item.BarcodeTextFontSizePt);
    AssertEqual(false, noneLayout.IsEnabled, "None placement disables HRI");
    AssertNear(item.HeightMm, noneLayout.SymbolHeightMm, 0.0001, "None uses full frame height for bars");

    var belowLayout = BarcodeHriTextLayout.Measure(
        BarcodeType.Code128,
        item.Text,
        item.WidthMm,
        item.HeightMm,
        BarcodeHriPlacement.Below,
        item.BarcodeTextFontSizePt);
    AssertNear(expected.SymbolHeightMm, belowLayout.SymbolHeightMm, 0.0001,
        "Above and Below reserve the same symbol height; only vertical origin differs");
    AssertNear(0, belowLayout.SymbolTopMm, 0.0001, "Below keeps SymbolTop at zero");

    var renderer = new LabelVisualRenderer(fakeRenderer);
    renderer.Render(template, null, new PrintRenderPlan
    {
        Dpi = 300,
        DpiX = 300,
        DpiY = 300,
        LabelWidthMm = template.WidthMm,
        LabelHeightMm = template.HeightMm
    });

    AssertNear(expected.SymbolHeightMm, fakeRenderer.LastHeightMm, 0.0001,
        "Print path must use Above-reserved symbol height from the shared contract");
    return Task.CompletedTask;
}

static Task TestBarcodeCheckDigitVerifyFailsClosedInPreflight()
{
    var template = new LabelTemplate
    {
        Name = "check-digit-verify",
        WidthMm = 60,
        HeightMm = 30,
        Dpi = 300
    };
    template.Objects.Add(new LabelObject
    {
        Type = ObjectType.BarcodeCode128,
        Name = "C39",
        BarcodeSymbology = BarcodeSymbology.Code39,
        Text = "ABC123", // no valid check digit
        XMm = 2,
        YMm = 2,
        WidthMm = 40,
        HeightMm = 12,
        BarcodeCheckDigitPolicy = BarcodeCheckDigitPolicy.Verify,
        BarcodeHriPlacement = BarcodeHriPlacement.None
    });

    var validator = new PrintPreflightValidator(new ZxingBarcodeRenderer());
    var bad = validator.Validate(template, new IReadOnlyDictionary<string, string>?[] { null }, 300);
    AssertEqual(true, bad.Issues.Any(i => i.Message.Contains("Verify", StringComparison.OrdinalIgnoreCase)),
        "Verify policy must fail closed when Code 39 payload lacks a valid check digit");

    var body = "ABC123";
    var good = body + BarcodeCheckDigitContract.ComputeCode39CheckDigit(body);
    template.Objects[0].Text = good;
    var ok = validator.Validate(template, new IReadOnlyDictionary<string, string>?[] { null }, 300);
    AssertEqual(false, ok.Issues.Any(i => i.Message.Contains("check-digit", StringComparison.OrdinalIgnoreCase)
            || i.Message.Contains("Verify", StringComparison.OrdinalIgnoreCase)),
        "Valid Code 39 check digit must pass Verify policy");
    return Task.CompletedTask;
}

static Task TestBarcodeHriHideCheckDigitDoesNotAlterModules()
{
    var body = "WIDGET";
    var check = BarcodeCheckDigitContract.ComputeCode39CheckDigit(body);
    var payload = body + check;

    var itemShow = new LabelObject
    {
        Type = ObjectType.BarcodeCode128,
        BarcodeSymbology = BarcodeSymbology.Code39,
        Text = payload,
        WidthMm = 40,
        HeightMm = 12,
        BarcodeCheckDigitPolicy = BarcodeCheckDigitPolicy.Verify,
        BarcodeHriShowCheckDigit = true,
        BarcodeHriPlacement = BarcodeHriPlacement.Below
    };
    var itemHide = new LabelObject
    {
        Type = ObjectType.BarcodeCode128,
        BarcodeSymbology = BarcodeSymbology.Code39,
        Text = payload,
        WidthMm = 40,
        HeightMm = 12,
        BarcodeCheckDigitPolicy = BarcodeCheckDigitPolicy.Verify,
        BarcodeHriShowCheckDigit = false,
        BarcodeHriPlacement = BarcodeHriPlacement.Below
    };

    var renderer = new ZxingBarcodeRenderer();
    var showVec = renderer.RenderBarcodeVector(
        payload, BarcodeType.Code39, itemShow.WidthMm, itemShow.HeightMm, 300, null);
    var hideVec = renderer.RenderBarcodeVector(
        payload, BarcodeType.Code39, itemHide.WidthMm, itemHide.HeightMm, 300, null);
    AssertEqual(true, showVec is not null && hideVec is not null, "Code 39 vectors must render");
    AssertEqual(showVec!.WidthModules, hideVec!.WidthModules, "HRI hide must not change module width");
    AssertEqual(showVec.RowBits.Length, hideVec.RowBits.Length, "module pattern length must match");
    for (var i = 0; i < showVec.RowBits.Length; i++)
    {
        AssertEqual(showVec.RowBits[i], hideVec.RowBits[i], $"module bit {i} must be identical when only HRI display changes");
    }

    var hriShown = BarcodeCheckDigitContract.FormatHriText(
        BarcodeSymbology.Code39, payload, BarcodeCheckDigitPolicy.Verify, true);
    var hriHidden = BarcodeCheckDigitContract.FormatHriText(
        BarcodeSymbology.Code39, payload, BarcodeCheckDigitPolicy.Verify, false);
    AssertEqual(payload, hriShown, "HRI show keeps check digit");
    AssertEqual(body, hriHidden, "HRI hide strips validated check digit only");
    return Task.CompletedTask;
}

static Task TestBarcodeHriPlacementSurvivesCloneAndSave()
{
    var source = new LabelObject
    {
        Type = ObjectType.BarcodeCode128,
        Name = "placement-src",
        Text = "SAVE-ME",
        WidthMm = 35,
        HeightMm = 14,
        BarcodeHriPlacement = BarcodeHriPlacement.Above,
        BarcodeTextFontSizePt = 8
    };
    var clone = LabelObjectCloner.Clone(source);
    AssertEqual(BarcodeHriPlacement.Above, clone.BarcodeHriPlacement, "Clone must preserve HRI placement");
    AssertEqual(true, clone.ShowBarcodeText, "Above implies ShowBarcodeText true");

    var template = new LabelTemplate
    {
        Name = "placement-save",
        WidthMm = 50,
        HeightMm = 30,
        Dpi = 300
    };
    template.Objects.Add(source);
    var options = new System.Text.Json.JsonSerializerOptions
    {
        WriteIndented = false,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };
    var json = System.Text.Json.JsonSerializer.Serialize(template, options);
    var loaded = System.Text.Json.JsonSerializer.Deserialize<LabelTemplate>(json, options)
        ?? throw new InvalidOperationException("deserialize failed");
    AssertEqual(1, loaded.Objects.Count, "round-trip object count");
    AssertEqual(BarcodeHriPlacement.Above, loaded.Objects[0].BarcodeHriPlacement,
        "Save/load must retain BarcodeHriPlacement.Above");

    // Legacy bool-only JSON still maps false → None
    var legacyJson = """
        {"Name":"legacy","WidthMm":40,"HeightMm":20,"Dpi":300,"Objects":[{"Type":"BarcodeCode128","Name":"b","Text":"X","WidthMm":30,"HeightMm":12,"ShowBarcodeText":false,"BarcodeTextFontSizePt":7}]}
        """;
    var legacy = System.Text.Json.JsonSerializer.Deserialize<LabelTemplate>(legacyJson, options)
        ?? throw new InvalidOperationException("legacy deserialize failed");
    AssertEqual(BarcodeHriPlacement.None, legacy.Objects[0].BarcodeHriPlacement,
        "Legacy ShowBarcodeText:false must map to None");
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

    var rotatedTemplate = new LabelTemplate
    {
        Name = "Rotated Outside Object Test",
        WidthMm = 20,
        HeightMm = 20,
        Dpi = 300
    };
    rotatedTemplate.Objects.Add(new LabelObject
    {
        Type = ObjectType.Rectangle,
        Name = "Rotated Frame Outside",
        XMm = 15,
        YMm = 5,
        WidthMm = 4,
        HeightMm = 10,
        Rotation = 90,
        Style = { OutlineStyle = OutlineStyle.None }
    });
    var rotatedPreflight = new PrintService().ValidateRows(
        rotatedTemplate,
        new IReadOnlyDictionary<string, string>?[] { null });
    AssertEqual(false, rotatedPreflight.IsSuccess,
        "Preflight must evaluate transformed bounds when a rotated object crosses the label edge");
    AssertEqual(true, rotatedPreflight.ToUserMessage().Contains("outside the design label", StringComparison.OrdinalIgnoreCase),
        "Rotated-object diagnostics must explain the transformed frame is outside the label");
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

static Task TestPrintPreflightBlocksUndersizedBoundMatrixBarcode()
{
    var template = new LabelTemplate
    {
        Name = "Bound matrix geometry test",
        WidthMm = 50,
        HeightMm = 30,
        Dpi = 300
    };
    template.Objects.Add(new LabelObject
    {
        Type = ObjectType.QRCode,
        Name = "Bound QR",
        XMm = 2,
        YMm = 2,
        WidthMm = 8,
        HeightMm = 8,
        BindingExpression = "{Code}",
        QrSizingMode = QrSizingMode.AutoSizeByData,
        QrModuleSizePx = 6,
        QrQuietZoneModules = 4,
        QrDpi = 300
    });

    var rows = new IReadOnlyDictionary<string, string>?[]
    {
        new Dictionary<string, string> { ["Code"] = new string('A', 2331) }
    };
    var preflight = new PrintService().ValidateRows(template, rows);

    AssertEqual(false, preflight.IsSuccess,
        "Preflight must block a bound matrix barcode whose row needs a larger module frame");
    AssertEqual(true, preflight.ToUserMessage().Contains("2D barcode frame is too small", StringComparison.OrdinalIgnoreCase),
        "The diagnostic must name the undersized matrix frame and provide a remediation");
    return Task.CompletedTask;
}

static Task TestPrintPreflightUsesExactFixedQrCapacity()
{
    var template = new LabelTemplate
    {
        Name = "Fixed QR capacity test",
        WidthMm = 50,
        HeightMm = 30,
        Dpi = 300
    };
    template.Objects.Add(new LabelObject
    {
        Type = ObjectType.QRCode,
        Name = "Fixed QR",
        XMm = 2,
        YMm = 2,
        WidthMm = 20,
        HeightMm = 20,
        BindingExpression = "{Code}",
        QrSizingMode = QrSizingMode.FixedVersionAndModuleSize,
        QrFixedVersion = 1,
        QrErrorCorrection = QrErrorCorrection.M,
        QrModuleSizePx = 6,
        QrQuietZoneModules = 4,
        QrDpi = 300
    });

    var rows = new IReadOnlyDictionary<string, string>?[]
    {
        new Dictionary<string, string> { ["Code"] = "12345678901234567890" }
    };
    var preflight = new PrintService().ValidateRows(template, rows);

    AssertEqual(false, preflight.IsSuccess,
        "Fixed QR version 1 must block data beyond its exact byte-mode capacity even when the visual frame is large");
    AssertEqual(true, preflight.ToUserMessage().Contains("version 1", StringComparison.OrdinalIgnoreCase),
        "The fixed QR diagnostic must name the selected version");
    return Task.CompletedTask;
}

static Task TestFixedFrameTextReportsOverflow()
{
    var template = new LabelTemplate
    {
        Name = "Separate Text and TextBox contracts",
        WidthMm = 100,
        HeightMm = 20,
        Dpi = 300
    };
    template.Objects.Add(new LabelObject
    {
        Type = ObjectType.Text,
        Name = "Free text",
        BindingExpression = "{Name}",
        XMm = 2,
        YMm = 2,
        WidthMm = 4,
        HeightMm = 4,
        Style =
        {
            FontSizePt = 14,
            TextSizing = TextSizingMode.FixedFrame,
            VerticalAlignment = TextVerticalAlignmentMode.Top
        }
    });

    var rows = new IReadOnlyDictionary<string, string>?[]
    {
        new Dictionary<string, string> { ["Name"] = "FREE-TEXT-RUNS-NORMALLY" }
    };
    var preflight = new PrintService().ValidateRows(template, rows);

    AssertEqual(true, preflight.IsSuccess,
        "Text must remain free-flowing when the resolved value exceeds its authored selection width but still fits the label");
    AssertEqual(false, TextBoxOverflowDetector.ShouldConstrainToBox(template.Objects[0]),
        "A persisted FixedFrame value must not turn Text into a TextBox");
    AssertEqual(false, TextBoxOverflowDetector.ShouldBlockOverflow(template.Objects[0]),
        "Text must not use the TextBox overflow blocker");

    var legacyBox = new LabelObject
    {
        Type = ObjectType.TextBox,
        Text = "A TextBox loaded with legacy AllowOverflow must still stay inside its frame.",
        WidthMm = 12,
        HeightMm = 3,
        Style =
        {
            FontSizePt = 11,
            TextSizing = TextSizingMode.FixedFrame,
            TextOverflow = TextOverflowMode.AllowOverflow,
            VerticalAlignment = TextVerticalAlignmentMode.Top
        }
    };
    var legacyWidthDip = MmConverter.MmToDip(legacyBox.WidthMm);
    var legacyHeightDip = MmConverter.MmToDip(legacyBox.HeightMm);
    AssertEqual(true, TextBoxOverflowDetector.ShouldConstrainToBox(legacyBox),
        "TextBox must remain constrained even when an older file contains AllowOverflow");
    AssertEqual(true, TextBoxOverflowDetector.ShouldBlockOverflow(legacyBox),
        "Legacy AllowOverflow must fail closed as Error for TextBox");
    AssertEqual(true, TextBoxOverflowDetector.IsOverflowing(
            legacyBox,
            legacyBox.Text,
            legacyWidthDip,
            legacyHeightDip),
        "Legacy TextBox overflow must remain visible to production preflight");
    return Task.CompletedTask;
}

static Task TestEllipsisTextOverflowPolicy()
{
    var item = new LabelObject
    {
        Type = ObjectType.TextBox,
        Name = "Ellipsis frame",
        Text = "Alpha\nBeta\nGamma",
        WidthMm = 16,
        HeightMm = 4,
        Style =
        {
            FontSizePt = 10,
            LineHeightPt = 10,
            TextOverflow = TextOverflowMode.Ellipsis,
            VerticalAlignment = TextVerticalAlignmentMode.Top
        }
    };
    var widthDip = MmConverter.MmToDip(item.WidthMm);
    var heightDip = MmConverter.MmToDip(item.HeightMm);

    var formatted = TextBoxOverflowDetector.CreateFormattedText(item, item.Text, Brushes.Black);
    TextBoxOverflowDetector.ApplyLayoutBounds(formatted, item, widthDip, heightDip, constrainToBox: true);
    AssertEqual(TextTrimming.CharacterEllipsis, formatted.Trimming,
        "Ellipsis policy must configure the single-pass WPF text path with character trimming");
    AssertEqual(false, TextBoxOverflowDetector.ShouldBlockOverflow(item),
        "Ellipsis must be an explicit non-blocking remediation policy");

    var layout = TextBoxOverflowDetector.CreateTextLayout(
        item,
        item.Text,
        widthDip,
        heightDip,
        constrainToBox: true,
        Brushes.Black);
    AssertEqual(true, layout.Lines.Count <= 1,
        "Explicit line-height ellipsis must cap the rendered line count to the frame height");
    AssertEqual(true, layout.Lines[^1].Text.Contains('…'),
        "Explicit line-height ellipsis must mark truncated content on the final visible line");

    var template = new LabelTemplate { Name = "Ellipsis preflight", WidthMm = 40, HeightMm = 20, Dpi = 300 };
    template.Objects.Add(item);
    var rows = new IReadOnlyDictionary<string, string>?[] { null };
    var preflight = new PrintService().ValidateRows(template, rows);
    AssertEqual(true, preflight.IsSuccess,
        "An explicit Ellipsis policy must allow bounded long text without changing its authored frame");
    return Task.CompletedTask;
}

static Task TestShrinkFontTextFit()
{
    var item = new LabelObject
    {
        Type = ObjectType.TextBox,
        Name = "Shrink frame",
        Text = "Industrial label batch 1234567890",
        WidthMm = 24,
        HeightMm = 7,
        Style =
        {
            FontSizePt = 24,
            TextSizing = TextSizingMode.ShrinkFont,
            TextOverflow = TextOverflowMode.Error,
            VerticalAlignment = TextVerticalAlignmentMode.Center
        }
    };
    var authoredFontSize = item.Style.FontSizePt;
    var layout = TextBoxOverflowDetector.CreateTextLayout(
        item,
        item.Text,
        MmConverter.MmToDip(item.WidthMm),
        MmConverter.MmToDip(item.HeightMm),
        constrainToBox: true,
        Brushes.Black);

    AssertEqual(true, TextBoxOverflowDetector.UsesShrinkFont(item), "ShrinkFont must be an explicit persisted sizing policy");
    AssertEqual(true, TextBoxOverflowDetector.ShouldConstrainToBox(item), "ShrinkFont must own the authored text frame");
    var debugText = TextBoxOverflowDetector.CreateFormattedText(item, "Industrial", Brushes.Black);
    AssertEqual(true, layout.Metrics.EffectiveFontSizePt < authoredFontSize,
        $"A long value should reduce only its effective font size when the authored frame is too small (effective={layout.Metrics.EffectiveFontSizePt:0.####}, authored={authoredFontSize:0.####}, lines={layout.Metrics.LineCount}, height={layout.Metrics.HeightDip:0.####}, frame={MmConverter.MmToDip(item.HeightMm):0.####}, lineHeight={debugText.LineHeight:0.####}, heightMetric={debugText.Height:0.####}, width={debugText.WidthIncludingTrailingWhitespace:0.####})");
    AssertEqual(true, layout.Metrics.EffectiveFontSizePt >= TextBoxOverflowDetector.MinimumShrinkFontSizePt,
        "ShrinkFont must never reduce below the bounded minimum");
    AssertEqual(false, layout.Metrics.IsOverflowing,
        "A value that fits inside the bounded minimum should pass the shared layout contract");
    AssertNear(authoredFontSize, item.Style.FontSizePt, 0.0001,
        "ShrinkFont must not mutate the authored font size in the document model");

    var clone = LabelObjectCloner.Clone(item);
    AssertEqual(TextSizingMode.ShrinkFont, clone.Style.TextSizing,
        "ShrinkFont must survive the shared object clone path");
    var identityBefore = DocumentSnapshot.Capture(new LabelTemplate
    {
        Name = "Shrink identity",
        WidthMm = 40,
        HeightMm = 20,
        Objects = { item }
    }).DocumentHash;
    item.Style.TextSizing = TextSizingMode.FixedFrame;
    var identityAfter = DocumentSnapshot.Capture(new LabelTemplate
    {
        Name = "Shrink identity",
        WidthMm = 40,
        HeightMm = 20,
        Objects = { item }
    }).DocumentHash;
    item.Style.TextSizing = TextSizingMode.ShrinkFont;
    AssertEqual(false, string.Equals(identityBefore, identityAfter, StringComparison.Ordinal),
        "Changing the text sizing policy must invalidate the immutable document identity");

    var template = new LabelTemplate { Name = "Shrink preflight", WidthMm = 40, HeightMm = 20, Dpi = 300 };
    template.Objects.Add(item);
    var preflight = new PrintService().ValidateRows(template, new IReadOnlyDictionary<string, string>?[] { null });
    AssertEqual(true, preflight.IsSuccess,
        "An explicit ShrinkFont policy should allow a fitting value through production preflight");

    item.WidthMm = 1.5;
    item.HeightMm = 1.5;
    var minimumLayout = TextBoxOverflowDetector.CreateTextLayout(
        item,
        item.Text,
        MmConverter.MmToDip(item.WidthMm),
        MmConverter.MmToDip(item.HeightMm),
        constrainToBox: true,
        Brushes.Black);
    AssertNear(TextBoxOverflowDetector.MinimumShrinkFontSizePt, minimumLayout.Metrics.EffectiveFontSizePt, 0.001,
        "Impossible frames must clamp at the minimum instead of shrinking without a bound");
    AssertEqual(true, minimumLayout.Metrics.IsOverflowing,
        "A frame that cannot fit at the minimum must remain visible to the Error preflight policy");
    return Task.CompletedTask;
}

static Task TestScaleWidthTextFit()
{
    var item = new LabelObject
    {
        Type = ObjectType.TextBox,
        Name = "Scale-width frame",
        Text = "INDUSTRIAL-BATCH-2026",
        WidthMm = 35,
        HeightMm = 18,
        Style =
        {
            FontSizePt = 16,
            Alignment = TextAlignmentMode.Center,
            TextSizing = TextSizingMode.ScaleWidth,
            TextOverflow = TextOverflowMode.Error,
            VerticalAlignment = TextVerticalAlignmentMode.Center
        }
    };

    var authoredFontSize = item.Style.FontSizePt;
    var layout = TextBoxOverflowDetector.CreateTextLayout(
        item,
        item.Text,
        MmConverter.MmToDip(item.WidthMm),
        MmConverter.MmToDip(item.HeightMm),
        constrainToBox: true,
        Brushes.Black);

    AssertEqual(true, TextBoxOverflowDetector.UsesScaleWidth(item), "ScaleWidth must be an explicit persisted sizing policy");
    AssertEqual(true, layout.Metrics.HorizontalScale < 1, "A long one-line value should receive a horizontal scale below 1");
    AssertEqual(true, layout.Metrics.HorizontalScale >= TextBoxOverflowDetector.MinimumScaleWidthFactor,
        "ScaleWidth must never compress below its bounded minimum");
    AssertNear(authoredFontSize, layout.Metrics.EffectiveFontSizePt, 0.0001,
        "ScaleWidth must preserve the authored font size");
    AssertNear(0.5, layout.Metrics.HorizontalScaleAnchorFraction, 0.0001,
        "Centered text must scale around the center of its content frame");
    AssertEqual(false, layout.Metrics.IsOverflowing,
        $"A value within the supported scale range must pass bounded preflight (scale={layout.Metrics.HorizontalScale:0.####}, width={layout.Metrics.WidthDip:0.####}, content={layout.Metrics.ContentWidthDip:0.####}, height={layout.Metrics.HeightDip:0.####}, frame={MmConverter.MmToDip(item.HeightMm):0.####}, lines={layout.Metrics.LineCount})");

    var template = new LabelTemplate { Name = "Scale preflight", WidthMm = 40, HeightMm = 25, Dpi = 300 };
    template.Objects.Add(item);
    var preflight = new PrintService().ValidateRows(template, new IReadOnlyDictionary<string, string>?[] { null });
    AssertEqual(true, preflight.IsSuccess,
        "An explicit ScaleWidth policy should allow a horizontally fitting value through production preflight");

    item.WidthMm = 4;
    var minimumLayout = TextBoxOverflowDetector.CreateTextLayout(
        item,
        item.Text,
        MmConverter.MmToDip(item.WidthMm),
        MmConverter.MmToDip(item.HeightMm),
        constrainToBox: true,
        Brushes.Black);
    AssertNear(TextBoxOverflowDetector.MinimumScaleWidthFactor, minimumLayout.Metrics.HorizontalScale, 0.0001,
        "Impossible horizontal frames must clamp at the minimum scale");
    AssertEqual(true, minimumLayout.Metrics.IsOverflowing,
        "A frame that cannot fit at the minimum scale must remain visible to Error preflight");
    return Task.CompletedTask;
}

static Task TestTextShrinkFrameCompressesGlyphs()
{
    Exception? failure = null;
    var thread = new Thread(() =>
    {
        try
        {
            const string value = "INDUSTRIAL-BATCH-2026-LONG";
            var naturalProbe = new LabelObject
            {
                Type = ObjectType.Text,
                Name = "Free text natural",
                Text = value,
                WidthMm = 120,
                HeightMm = 40,
                Style =
                {
                    FontFamily = "Arial",
                    FontSizePt = 18,
                    Alignment = TextAlignmentMode.Left,
                    VerticalAlignment = TextVerticalAlignmentMode.Top,
                    TextSizing = TextSizingMode.AutoFit
                }
            };

            var naturalLayout = TextBoxOverflowDetector.CreateTextLayout(
                naturalProbe,
                value,
                MmConverter.MmToDip(naturalProbe.WidthMm),
                MmConverter.MmToDip(naturalProbe.HeightMm),
                constrainToBox: false,
                Brushes.Black);
            AssertEqual(true, naturalLayout.Metrics.WidthDip > 1, "Natural Text measure must produce a positive width");
            AssertEqual(true, naturalLayout.Metrics.HeightDip > 1, "Natural Text measure must produce a positive height");
            AssertNear(1.0, naturalLayout.Metrics.HorizontalScale, 0.0001,
                "A Text frame larger than natural ink must not compress horizontally");
            AssertNear(1.0, naturalLayout.Metrics.VerticalScale, 0.0001,
                "A Text frame larger than natural ink must not compress vertically");

            // Simulate a post-border-drag frame clearly below natural ink.
            var shrinkWidthMm = Math.Max(4.0, MmConverter.DipToMm(naturalLayout.Metrics.WidthDip) * 0.45);
            var shrinkHeightMm = Math.Max(2.0, MmConverter.DipToMm(naturalLayout.Metrics.HeightDip) * 0.55);
            var shrunk = new LabelObject
            {
                Type = ObjectType.Text,
                Name = "Free text shrunk",
                Text = value,
                WidthMm = shrinkWidthMm,
                HeightMm = shrinkHeightMm,
                Style =
                {
                    FontFamily = "Arial",
                    FontSizePt = 18,
                    Alignment = TextAlignmentMode.Left,
                    VerticalAlignment = TextVerticalAlignmentMode.Top,
                    TextSizing = TextSizingMode.AutoFit
                }
            };

            var frameWidthDip = MmConverter.MmToDip(shrunk.WidthMm);
            var frameHeightDip = MmConverter.MmToDip(shrunk.HeightMm);
            var layout = TextBoxOverflowDetector.CreateTextLayout(
                shrunk,
                value,
                frameWidthDip,
                frameHeightDip,
                constrainToBox: false,
                Brushes.Black);

            AssertEqual(false, TextBoxOverflowDetector.ShouldConstrainToBox(shrunk),
                "Text frame-fit compress must not flip Text into TextBox constraint");
            AssertEqual(false, TextBoxOverflowDetector.UsesScaleWidth(shrunk),
                "Text frame-fit compress must not enable TextBox ScaleWidth policy");
            AssertEqual(false, TextBoxOverflowDetector.UsesShrinkFont(shrunk),
                "Text frame-fit compress must not enable TextBox ShrinkFont policy");
            AssertEqual(true, TextBoxOverflowDetector.UsesTextFrameFitCompress(shrunk),
                "Free Text must opt into the shared frame-fit compress path");
            AssertEqual(true, layout.Metrics.HorizontalScale < 1.0,
                $"Shrunk Text width must compress horizontally (scale={layout.Metrics.HorizontalScale:0.####}, naturalW={naturalLayout.Metrics.WidthDip:0.####}, frameW={frameWidthDip:0.####})");
            AssertEqual(true, layout.Metrics.VerticalScale < 1.0,
                $"Shrunk Text height must compress vertically (scale={layout.Metrics.VerticalScale:0.####}, naturalH={naturalLayout.Metrics.HeightDip:0.####}, frameH={frameHeightDip:0.####})");
            AssertEqual(true, layout.Metrics.HorizontalScale >= 0.01,
                "Frame-fit horizontal scale must stay above the shared floor");
            AssertEqual(true, layout.Metrics.VerticalScale >= 0.01,
                "Frame-fit vertical scale must stay above the shared floor");

            var contentWidth = TextBoxOverflowDetector.GetContentWidthDip(shrunk, frameWidthDip, constrainToBox: false);
            var contentHeight = TextBoxOverflowDetector.GetContentHeightDip(shrunk, frameHeightDip, constrainToBox: false);
            const double tol = 0.75;
            AssertEqual(true, layout.Metrics.WidthDip <= contentWidth + tol,
                $"Scaled Text width must fit the authored content frame (width={layout.Metrics.WidthDip:0.####}, content={contentWidth:0.####})");
            AssertEqual(true, layout.Metrics.HeightDip <= contentHeight + tol,
                $"Scaled Text height must fit the authored content frame (height={layout.Metrics.HeightDip:0.####}, content={contentHeight:0.####})");

            var origin = new Point(
                TextBoxOverflowDetector.GetHorizontalOriginDip(shrunk, constrainToBox: false),
                layout.Metrics.VerticalOffsetDip);
            var ink = TextBoxOverflowDetector.GetInkBoundsDip(layout, origin);
            AssertEqual(false, ink.IsEmpty, "Compressed Text must still produce ink bounds");
            AssertEqual(true, ink.Right <= frameWidthDip + tol,
                $"Compressed ink must stay within the authored frame width (right={ink.Right:0.####}, frame={frameWidthDip:0.####})");
            AssertEqual(true, ink.Bottom <= frameHeightDip + tol,
                $"Compressed ink must stay within the authored frame height (bottom={ink.Bottom:0.####}, frame={frameHeightDip:0.####})");

            // Same model dimensions must yield the same scale whether measured
            // again (shared choke point used by designer and print).
            var second = TextBoxOverflowDetector.CreateTextLayout(
                shrunk,
                value,
                frameWidthDip,
                frameHeightDip,
                constrainToBox: false,
                Brushes.Black);
            AssertNear(layout.Metrics.HorizontalScale, second.Metrics.HorizontalScale, 0.0001,
                "Text frame-fit horizontal scale must be deterministic for the same model");
            AssertNear(layout.Metrics.VerticalScale, second.Metrics.VerticalScale, 0.0001,
                "Text frame-fit vertical scale must be deterministic for the same model");
            AssertEqual(layout.Metrics.IdentityFingerprint, second.Metrics.IdentityFingerprint,
                "Shared layout identity must match for identical Text frame-fit inputs");

            // Pure scale helper must compress when natural exceeds content (no hardcoded scale).
            var helper = TextBoxOverflowDetector.ResolveTextFrameFitScale(
                naturalLayout.Metrics.WidthDip,
                naturalLayout.Metrics.HeightDip,
                layout.Metrics.ContentWidthDip,
                TextBoxOverflowDetector.GetContentHeightDip(shrunk, frameHeightDip, false),
                naturalLayout.Metrics.LineHeightDip);
            AssertEqual(true, helper.ScaleX < 1.0,
                $"ResolveTextFrameFitScale must shrink X when frame is tight (sx={helper.ScaleX:0.####})");
            AssertEqual(true, helper.ScaleY < 1.0,
                $"ResolveTextFrameFitScale must shrink Y when frame is tight (sy={helper.ScaleY:0.####})");
            AssertNear(layout.Metrics.HorizontalScale, helper.ScaleX, 0.05,
                "CreateTextLayout horizontal compress must track ResolveTextFrameFitScale");

            // TextBox ScaleWidth stays a separate policy and is not auto-enabled
            // merely because a free Text frame is tight.
            var box = new LabelObject
            {
                Type = ObjectType.TextBox,
                Text = value,
                WidthMm = shrinkWidthMm,
                HeightMm = Math.Max(8, shrinkHeightMm),
                Style =
                {
                    FontSizePt = 18,
                    TextSizing = TextSizingMode.FixedFrame,
                    TextOverflow = TextOverflowMode.Error,
                    VerticalAlignment = TextVerticalAlignmentMode.Top
                }
            };
            AssertEqual(false, TextBoxOverflowDetector.UsesTextFrameFitCompress(box),
                "TextBox must not use free-Text frame-fit compress");
            AssertEqual(true, TextBoxOverflowDetector.ShouldConstrainToBox(box),
                "TextBox must remain frame-owned while Text compresses");
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

static Task TestTextBorderDragLocksFrameAndCompressesGlyphs()
{
    Exception? failure = null;
    var thread = new Thread(() =>
    {
        try
        {
            // NiceLabel Text cannot be hand-resized (size follows font; Font Scaling
            // is a style factor). ANLAbel maps border-drag to: lock selection frame
            // (stop AutoFit re-expand) + shared frame-fit glyph compress.
            var item = new LabelObject
            {
                Id = "text-border-drag",
                Type = ObjectType.Text,
                Name = "Drag text",
                Text = "Long text can overflow horizontally",
                XMm = 5,
                YMm = 5,
                WidthMm = 35,
                HeightMm = 10,
                Style =
                {
                    FontFamily = "Arial",
                    FontSizePt = 14,
                    TextSizing = TextSizingMode.AutoFit,
                    TextOverflow = TextOverflowMode.AllowOverflow,
                    VerticalAlignment = TextVerticalAlignmentMode.Center
                }
            };
            var template = new LabelTemplate { Name = "Text drag", WidthMm = 100, HeightMm = 40, Dpi = 300 };
            // Attach canvas first so collection Add runs the real AutoFit path.
            var canvas = new LabelDesignerCanvas { Template = template };
            template.Objects.Add(item);

            // Content-owned AutoFit should size to natural ink first.
            var autoWidth = item.WidthMm;
            var autoHeight = item.HeightMm;
            AssertEqual(true, autoWidth > 10, $"AutoFit must grow Text width (actual {autoWidth:0.##} mm)");
            AssertEqual(true, autoHeight >= 2, $"AutoFit must produce a usable Text height (actual {autoHeight:0.##} mm)");
            AssertEqual(TextSizingMode.AutoFit, item.Style.TextSizing, "New Text stays AutoFit until border-drag");

            var naturalLayout = TextBoxOverflowDetector.CreateTextLayout(
                item,
                item.Text,
                MmConverter.MmToDip(item.WidthMm),
                MmConverter.MmToDip(item.HeightMm),
                constrainToBox: false,
                Brushes.Black);
            AssertNear(1.0, naturalLayout.Metrics.HorizontalScale, 0.001,
                "AutoFit Text frame must not compress glyphs");

            // Simulate a border-drag shrink (same model writes as ResizeSelectedObject).
            var startWidth = item.WidthMm;
            var startHeight = item.HeightMm;
            var shrinkWidth = Math.Max(4, Math.Round(startWidth * 0.4, 2));
            var shrinkHeight = Math.Max(2, Math.Round(startHeight * 0.5, 2));
            if (item.Style.TextSizing == TextSizingMode.AutoFit
                && (Math.Abs(shrinkWidth - item.WidthMm) > 0.01 || Math.Abs(shrinkHeight - item.HeightMm) > 0.01))
            {
                item.Style.TextSizing = TextSizingMode.FixedFrame;
            }

            item.WidthMm = shrinkWidth;
            item.HeightMm = shrinkHeight;

            AssertEqual(TextSizingMode.FixedFrame, item.Style.TextSizing,
                "Border-drag must lock free Text as FixedFrame so AutoFit cannot re-expand the selection");
            AssertEqual(false, TextBoxOverflowDetector.ShouldConstrainToBox(item),
                "Locked Text frame must not become TextBox-constrained");

            // Content edit must not re-expand a user-locked Text frame.
            var lockedWidth = item.WidthMm;
            var lockedHeight = item.HeightMm;
            item.Text = item.Text + " MORE";
            AssertNear(lockedWidth, item.WidthMm, 0.01,
                "User-locked Text width must not AutoFit-grow when content changes");
            AssertNear(lockedHeight, item.HeightMm, 0.01,
                "User-locked Text height must not AutoFit-grow when content changes");

            var layout = TextBoxOverflowDetector.CreateTextLayout(
                item,
                item.Text,
                MmConverter.MmToDip(item.WidthMm),
                MmConverter.MmToDip(item.HeightMm),
                constrainToBox: false,
                Brushes.Black);
            AssertEqual(true, layout.Metrics.HorizontalScale < 1.0,
                $"Shrunk locked Text must compress horizontally (scale={layout.Metrics.HorizontalScale:0.####})");
            AssertEqual(true, layout.Metrics.VerticalScale < 1.0
                    || layout.Metrics.HeightDip <= TextBoxOverflowDetector.GetContentHeightDip(
                        item, MmConverter.MmToDip(item.HeightMm), false) + 1,
                $"Shrunk locked Text must compress vertically or already fit height (vScale={layout.Metrics.VerticalScale:0.####})");

            var contentWidth = TextBoxOverflowDetector.GetContentWidthDip(item, MmConverter.MmToDip(item.WidthMm), false);
            AssertEqual(true, layout.Metrics.WidthDip <= contentWidth + 0.75,
                $"Compressed ink width must fit the locked frame (w={layout.Metrics.WidthDip:0.##}, content={contentWidth:0.##})");

            // Keep canvas alive through the model edits used by the real host.
            AssertEqual(true, canvas.Template is not null, "Designer canvas must keep the template host");
            AssertEqual(shrinkWidth, item.WidthMm, "Locked frame width must remain the dragged size");
            AssertEqual(shrinkHeight, item.HeightMm, "Locked frame height must remain the dragged size");
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

static Task TestTextBoxFitModesPreserveFrame()
{
    var fontFit = new LabelObject
    {
        Type = ObjectType.TextBox,
        Text = "FIT",
        WidthMm = 45,
        HeightMm = 18,
        Style =
        {
            FontSizePt = 6,
            TextSizing = TextSizingMode.ShrinkFont,
            TextFitMinimumFontSizePt = 5,
            TextFitMaximumFontSizePt = 14
        }
    };
    var fontLayout = TextBoxOverflowDetector.CreateTextLayout(
        fontFit,
        fontFit.Text,
        MmConverter.MmToDip(fontFit.WidthMm),
        MmConverter.MmToDip(fontFit.HeightMm),
        constrainToBox: true,
        Brushes.Black);
    AssertEqual(true, fontLayout.Metrics.EffectiveFontSizePt > fontFit.Style.FontSizePt,
        "NiceLabel font-size fit may increase font size when the configured maximum still fits");
    AssertEqual(true, fontLayout.Metrics.EffectiveFontSizePt is >= 5 and <= 14,
        "Font-size fit must remain inside the persisted minimum/maximum range");
    AssertEqual(45d, fontFit.WidthMm, "Font-size fit must not change the authored TextBox width");
    AssertEqual(18d, fontFit.HeightMm, "Font-size fit must not change the authored TextBox height");

    var scaleFit = new LabelObject
    {
        Type = ObjectType.TextBox,
        Text = "A",
        WidthMm = 35,
        HeightMm = 10,
        Style =
        {
            FontSizePt = 10,
            TextSizing = TextSizingMode.ScaleWidth,
            TextFitMinimumScale = 0.6,
            TextFitMaximumScale = 1.5
        }
    };
    var scaleLayout = TextBoxOverflowDetector.CreateTextLayout(
        scaleFit,
        scaleFit.Text,
        MmConverter.MmToDip(scaleFit.WidthMm),
        MmConverter.MmToDip(scaleFit.HeightMm),
        constrainToBox: true,
        Brushes.Black);
    AssertEqual(1.5, Math.Round(scaleLayout.Metrics.HorizontalScale, 1),
        "NiceLabel font scaling may stretch up to the configured maximum");
    AssertEqual(35d, scaleFit.WidthMm, "Font scaling must not change the authored TextBox width");
    AssertEqual(10d, scaleFit.HeightMm, "Font scaling must not change the authored TextBox height");

    var clone = LabelObjectCloner.Clone(fontFit);
    AssertEqual(5d, clone.Style.TextFitMinimumFontSizePt,
        "Clone must preserve TextBox fit minimum");
    AssertEqual(14d, clone.Style.TextFitMaximumFontSizePt,
        "Clone must preserve TextBox fit maximum");
    var snapshot = ObjectStyleSnapshot.Capture(scaleFit.Style);
    AssertEqual(0.6, snapshot.TextFitMinimumScale,
        "Immutable scene snapshot must preserve minimum font scale");
    AssertEqual(1.5, snapshot.TextFitMaximumScale,
        "Immutable scene snapshot must preserve maximum font scale");
    return Task.CompletedTask;
}

static Task TestSnapToleranceStableAcrossZoom()
{
    foreach (var zoom in new[] { 0.25, 0.5, 1.0, 2.0, 4.0 })
    {
        var acquireMm = SnapToleranceContract.AcquireToleranceMm(zoom);
        var releaseMm = SnapToleranceContract.ReleaseToleranceMm(zoom);
        AssertNear(SnapToleranceContract.DefaultAcquireToleranceDip, MmConverter.MmToDip(acquireMm) * zoom, 0.0001,
            $"Acquire tolerance must represent the same screen budget at zoom {zoom:0.##}");
        AssertNear(SnapToleranceContract.DefaultReleaseToleranceDip, MmConverter.MmToDip(releaseMm) * zoom, 0.0001,
            $"Release tolerance must represent the same screen budget at zoom {zoom:0.##}");
    }

    var selector = SnapCandidateSelector.Choose(
        new[]
        {
            new SnapCandidate(10, 10.5, 0.5, 10, "low"),
            new SnapCandidate(10, 10.8, 0.8, 20, "semantic")
        },
        acquireTolerance: 0.8);
    AssertEqual("semantic", selector?.StableKey,
        "Snap selection must prefer semantic priority over a merely closer lower-priority candidate");

    var state = new SnapHysteresisState();
    AssertNear(20, state.Resolve(20.1, 20, SnapToleranceContract.ReleaseToleranceMm(1))!.Value, 0.0001,
        "A newly acquired snap candidate must lock the target");
    AssertNear(20, state.Resolve(20.2, null, SnapToleranceContract.ReleaseToleranceMm(1))!.Value, 0.0001,
        "A target must remain locked while the pointer is inside the release window");
    AssertEqual<double?>(null, state.Resolve(20 + SnapToleranceContract.ReleaseToleranceMm(1) + 0.01, null, SnapToleranceContract.ReleaseToleranceMm(1)),
        "A target must release after the pointer leaves the release window");
    return Task.CompletedTask;
}

static Task TestDrawingPointSnapContract()
{
    var target = new LabelObject
    {
        Id = "draw-target",
        Type = ObjectType.Rectangle,
        Name = "Draw target",
        XMm = 20,
        YMm = 20,
        WidthMm = 10,
        HeightMm = 10
    };
    var template = new LabelTemplate { WidthMm = 100, HeightMm = 60 };
    template.Objects.Add(target);
    template.Guides.Add(new LabelGuide
    {
        Id = "draw-guide",
        Orientation = LabelGuideOrientation.Vertical,
        PositionMm = 40,
        IsVisible = true
    });

    Exception? failure = null;
    Point? objectSnapped = null;
    Point? guideSnapped = null;
    Point? gridSnapped = null;
    var thread = new Thread(() =>
    {
        try
        {
            var canvas = new LabelDesignerCanvas
            {
                Template = template,
                Zoom = 1,
                IsSnapToObjectsEnabled = true,
                IsSnapToGridEnabled = false
            };
            var snapMethod = typeof(LabelDesignerCanvas).GetMethod(
                "SnapDrawingPoint",
                BindingFlags.Instance | BindingFlags.NonPublic);
            AssertEqual(true, snapMethod is not null, "Drawing must use a dedicated point snap path");

            objectSnapped = (Point)snapMethod!.Invoke(
                canvas,
                new object[] { new Point(MmConverter.MmToDip(20.4), MmConverter.MmToDip(20.4)) })!;
            guideSnapped = (Point)snapMethod.Invoke(
                canvas,
                new object[] { new Point(MmConverter.MmToDip(40.4), MmConverter.MmToDip(12.0)) })!;

            canvas.IsSnapToObjectsEnabled = false;
            canvas.IsSnapToGridEnabled = true;
            canvas.GridStepMm = 5;
            gridSnapped = (Point)snapMethod.Invoke(
                canvas,
                new object[] { new Point(MmConverter.MmToDip(20.4), MmConverter.MmToDip(25.4)) })!;
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

    AssertNear(20, objectSnapped!.Value.X, 0.001, "Drawing endpoint X must snap to the target edge");
    AssertNear(20, objectSnapped.Value.Y, 0.001, "Drawing endpoint Y must snap to the target edge");
    AssertNear(40, guideSnapped!.Value.X, 0.001, "Drawing endpoint X must honor a visible ruler guide");
    AssertNear(12, guideSnapped.Value.Y, 0.001, "Unmatched drawing endpoint axis must remain at its authored position");
    AssertNear(20, gridSnapped!.Value.X, 0.001, "Drawing endpoint X must use the physical grid when object snap is disabled");
    AssertNear(25, gridSnapped.Value.Y, 0.001, "Drawing endpoint Y must use the physical grid when object snap is disabled");
    return Task.CompletedTask;
}

static Task TestLineDraggingUsesSharedSnapContract()
{
    var line = new LabelObject
    {
        Id = "drag-line",
        Type = ObjectType.Line,
        Name = "Drag line",
        XMm = 10,
        YMm = 10,
        WidthMm = 20,
        HeightMm = 0.5,
        LineEndXMm = 30,
        LineEndYMm = 10,
        Style = { OutlineStyle = OutlineStyle.Solid, BorderThicknessMm = 0.2 }
    };
    var target = new LabelObject
    {
        Id = "line-target",
        Type = ObjectType.Rectangle,
        Name = "Line target",
        XMm = 40,
        YMm = 10,
        WidthMm = 10,
        HeightMm = 5
    };
    var template = new LabelTemplate { WidthMm = 80, HeightMm = 40 };
    template.Objects.Add(line);
    template.Objects.Add(target);

    Exception? failure = null;
    double? snappedX = null;
    var finalStartX = 0.0;
    var finalEndX = 0.0;
    LabelLayoutBounds? finalBounds = null;
    var thread = new Thread(() =>
    {
        try
        {
            var canvas = new LabelDesignerCanvas
            {
                Template = template,
                Zoom = 1,
                IsSnapToObjectsEnabled = true,
                IsSnapToGridEnabled = false
            };
            // The real mouse-down path stores these snapshots before the first
            // move. Seed the same fields so this regression exercises the
            // actual line-drag helper, not only the candidate calculator.
            typeof(LabelDesignerCanvas).GetField("_startXMm", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(canvas, line.XMm);
            typeof(LabelDesignerCanvas).GetField("_startYMm", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(canvas, line.YMm);
            typeof(LabelDesignerCanvas).GetField("_startLineEndXMm", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(canvas, line.LineEndXMm);
            typeof(LabelDesignerCanvas).GetField("_startLineEndYMm", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(canvas, line.LineEndYMm);

            var moveMethod = typeof(LabelDesignerCanvas).GetMethod(
                "MoveSingleLine",
                BindingFlags.Instance | BindingFlags.NonPublic);
            AssertEqual(true, moveMethod is not null,
                "Line dragging must have one shared movement path for snap and stroke bounds");
            var snap = moveMethod!.Invoke(canvas, new object[] { line, 9.8d, 0d });
            snappedX = (double?)snap!.GetType().GetProperty("SnapX")!.GetValue(snap);
            finalStartX = line.XMm;
            finalEndX = line.LineEndXMm;
            finalBounds = LineBoundsContract.GetBounds(line);
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

    AssertNear(19.9, snappedX ?? double.NaN, 0.001,
        "A dragged line must acquire the target edge using its stroke-aware trailing bound");
    AssertNear(19.9, finalStartX, 0.001,
        "Line movement must commit the snapped start point, not only draw a guide");
    AssertNear(39.9, finalEndX, 0.001,
        "Line movement must translate both endpoints by the same snapped delta");
    AssertNear(40, finalBounds!.Value.Right, 0.001,
        "Line movement must keep the visible stroke hull aligned with the target edge");
    return Task.CompletedTask;
}

static Task TestPointerFrameTelemetry()
{
    var telemetry = new PointerFrameTelemetry(capacity: 4);
    telemetry.Record(1, 0.25, 1.25);
    telemetry.Record(2, 0.25, 1.25);
    telemetry.Record(3, 0.25, 1.25);
    telemetry.Record(4, 0.25, 1.25);
    telemetry.Record(20, 0.25, 1.25);

    var snapshot = telemetry.Snapshot(0.25, 1.25);
    AssertEqual(5L, snapshot.TotalFrames, "Telemetry must retain total frame count beyond its bounded sample ring");
    AssertEqual(4, snapshot.SampleCount, "Telemetry samples must remain bounded by the configured ring capacity");
    AssertNear(20, snapshot.P95Milliseconds, 0.0001, "P95 must expose a slow pointer frame instead of hiding it in the average");
    AssertEqual(false, snapshot.MeetsBudget(), "A frame stream above the 16.667 ms budget must be reported as over budget");

    var normalized = PointerFrameTelemetry.NormalizePixelsPerDip(double.NaN);
    AssertNear(1, normalized, 0.0001, "Unknown display scale must use a deterministic 1.0 pixels-per-DIP fallback");
    return Task.CompletedTask;
}

static Task TestPointerTelemetryOverlay()
{
    Exception? failure = null;
    var thread = new Thread(() =>
    {
        try
        {
            var canvas = new LabelDesignerCanvas
            {
                Width = 180,
                Height = 120,
                Template = new LabelTemplate { WidthMm = 40, HeightMm = 25 }
            };
            AssertEqual(false, canvas.ShowPointerTelemetry, "Pointer telemetry overlay must be opt-in by default");
            canvas.PointerTelemetry.Record(4, 1, 1);
            canvas.ShowPointerTelemetry = true;
            AssertEqual(true, canvas.ShowPointerTelemetry, "The canvas must expose an explicit diagnostic toggle");
            canvas.Measure(new Size(180, 120));
            canvas.Arrange(new Rect(0, 0, 180, 120));
            var bitmap = new RenderTargetBitmap(180, 120, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(canvas);
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

static Task TestDesignerStyleEditsRefreshVisual()
{
    Exception? failure = null;
    var visualChanged = false;
    var thread = new Thread(() =>
    {
        try
        {
            var item = new LabelObject
            {
                Id = "style-refresh",
                Type = ObjectType.Text,
                Text = "Initial text",
                XMm = 2,
                YMm = 2,
                WidthMm = 24,
                HeightMm = 8
            };
            item.Style.TextSizing = TextSizingMode.FixedFrame;
            var template = new LabelTemplate { WidthMm = 40, HeightMm = 20 };
            template.Objects.Add(item);
            var canvas = new LabelDesignerCanvas { Template = template };
            var elementsField = typeof(LabelDesignerCanvas).GetField(
                "_objectElements",
                BindingFlags.Instance | BindingFlags.NonPublic);
            AssertEqual(true, elementsField is not null, "Designer canvas must retain object hosts for incremental refresh");
            var elements = elementsField!.GetValue(canvas) as IDictionary<LabelObject, FrameworkElement>;
            FrameworkElement? element = null;
            var hasElement = elements is not null && elements.TryGetValue(item, out element);
            AssertEqual(true, hasElement, "Text object host must be created when a template is attached");
            var border = element as Border;
            var host = border?.Child as VisualPreviewHost;
            var before = host?.PreviewVisual;
            AssertEqual(true, before is not null, "Text object must have an initial preview visual");

            // This is the same binding path used by the Properties panel. It
            // must replace the drawing immediately; waiting for a zoom/rebuild
            // would leave the designer visibly stale after a font edit.
            item.Style.FontSizePt = 18;
            visualChanged = before is not null && host?.PreviewVisual is not null
                && !ReferenceEquals(before, host.PreviewVisual);
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

    AssertEqual(true, visualChanged,
        "Changing text style must refresh the designer visual immediately instead of using the transform-only hot path");
    return Task.CompletedTask;
}

static Task TestTextBoxDoesNotResizeFromText()
{
    // Object must not follow text content — user owns frame by drag/properties.
    Exception? failure = null;
    var widthAfter = 0.0;
    var heightAfter = 0.0;
    var thread = new Thread(() =>
    {
        try
        {
            var item = new LabelObject
            {
                Id = "textbox-no-content-fit",
                Type = ObjectType.TextBox,
                Text = "Short",
                WidthMm = 42,
                HeightMm = 16
            };
            item.Style.TextSizing = TextSizingMode.FixedFrame;
            item.Style.FontSizePt = 10;

            var template = new LabelTemplate { WidthMm = 100, HeightMm = 50 };
            var canvas = new LabelDesignerCanvas { Template = template };
            template.Objects.Add(item);

            // Even AutoFit flag must not rewrite TextBox size from content.
            item.Style.TextSizing = TextSizingMode.AutoFit;
            item.Text = "Much longer text must not force the object frame to follow content size.";
            widthAfter = item.WidthMm;
            heightAfter = item.HeightMm;

            var measured = TextBoxOverflowDetector.MeasureAutoFitFrameMm(item, item.Text);
            AssertNear(42, measured.WidthMm, 0.01, "MeasureAutoFitFrameMm must not invent TextBox width from text");
            AssertNear(16, measured.HeightMm, 0.01, "MeasureAutoFitFrameMm must not invent TextBox height from text");
            _ = canvas.Width;
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

    AssertNear(42, widthAfter, 0.01, "Text-only edit must not change TextBox width");
    AssertNear(16, heightAfter, 0.01, "Text-only edit must not change TextBox height");
    return Task.CompletedTask;
}

static Task TestTextBoxReflowsWhenUserResizes()
{
    // When the user drags/edits the frame, wrap and host layout must fit the
    // new size (shared constrained path), without content rewriting the frame.
    Exception? failure = null;
    var wideLines = 0;
    var narrowLines = 0;
    var hostWidthAfter = 0.0;
    var hostHeightAfter = 0.0;
    var originUsesPadding = false;
    var thread = new Thread(() =>
    {
        try
        {
            var item = new LabelObject
            {
                Id = "textbox-drag-fit",
                Type = ObjectType.TextBox,
                Text = "Alpha bravo charlie delta echo foxtrot golf hotel india juliet kilo",
                WidthMm = 45,
                HeightMm = 20
            };
            item.Style.TextSizing = TextSizingMode.FixedFrame;
            item.Style.TextOverflow = TextOverflowMode.Clip;
            item.Style.FontSizePt = 9;
            item.Style.TextPaddingLeftMm = 2;
            item.Style.TextPaddingRightMm = 1;

            var template = new LabelTemplate { WidthMm = 100, HeightMm = 50 };
            var canvas = new LabelDesignerCanvas { Template = template, Zoom = 1.0 };
            template.Objects.Add(item);
            canvas.Measure(new Size(900, 600));
            canvas.Arrange(new Rect(0, 0, 900, 600));
            canvas.UpdateLayout();

            wideLines = TextBoxOverflowDetector.WrapTextToBox(
                item,
                item.Text,
                TextBoxOverflowDetector.GetContentWidthDip(item, MmConverter.MmToDip(item.WidthMm), true))
                .Split('\n').Length;

            // Exercise the exact private method wired to SelectionResizeAdorner,
            // not a direct model edit: TextBox must remain drag-resizable.
            var resizeMethod = typeof(LabelDesignerCanvas).GetMethod(
                "ResizeSelectedObject",
                BindingFlags.Instance | BindingFlags.NonPublic)!;
            resizeMethod.Invoke(canvas, new object[]
            {
                item,
                new ResizeDelta(
                    deltaX: 0,
                    deltaY: 0,
                    deltaWidth: MmConverter.MmToDip(18 - 45),
                    deltaHeight: MmConverter.MmToDip(22 - 20),
                    handle: ResizeHandle.BottomRight,
                    disableSnapping: true)
            });
            AssertNear(18, item.WidthMm, 0.01, "Drag resize must change TextBox width");
            AssertNear(22, item.HeightMm, 0.01, "Drag resize must change TextBox height");

            narrowLines = TextBoxOverflowDetector.WrapTextToBox(
                item,
                item.Text,
                TextBoxOverflowDetector.GetContentWidthDip(item, MmConverter.MmToDip(item.WidthMm), true))
                .Split('\n').Length;

            AssertEqual(true, TextBoxOverflowDetector.ShouldConstrainToBox(item),
                "TextBox must stay constrained so text fits inside the dragged frame");
            AssertEqual(true, narrowLines > wideLines,
                $"Narrower dragged width must reflow to more lines (wide {wideLines}, narrow {narrowLines})");

            // Host bounds must match the user frame after resize (fit together).
            var map = typeof(LabelDesignerCanvas)
                .GetField("_objectElements", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(canvas) as System.Collections.IDictionary;
            FrameworkElement? host = null;
            foreach (System.Collections.DictionaryEntry entry in map!)
            {
                if (ReferenceEquals(entry.Key, item))
                {
                    host = entry.Value as FrameworkElement;
                    break;
                }
            }

            AssertEqual(true, host is not null, "Canvas must keep a TextBox host after resize");
            hostWidthAfter = host!.Width;
            hostHeightAfter = host.Height;
            AssertNear(MmConverter.MmToDip(18), hostWidthAfter, 0.5,
                "Host width must track the user-resized frame");
            AssertNear(MmConverter.MmToDip(22), hostHeightAfter, 0.5,
                "Host height must track the user-resized frame");

            // Horizontal origin includes left padding (draw path must not use x=0 only).
            var origin = TextBoxOverflowDetector.GetHorizontalOriginDip(item, constrainToBox: true);
            originUsesPadding = origin > 1.0;
            AssertEqual(true, originUsesPadding,
                "Text origin must include left padding so glyphs fit inside the frame when resizing");
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

static Task TestNormalResizeCaptureReleaseDoesNotCancelGesture()
{
    Exception? failure = null;
    var thread = new Thread(() =>
    {
        try
        {
            var adorned = new Border { Width = 120, Height = 60 };
            var adorner = new SelectionResizeAdorner(adorned);
            var canceled = 0;
            var completed = 0;
            adorner.ResizeCanceled += (_, _) => canceled++;
            adorner.ResizeCompleted += (_, _) => completed++;

            var begin = typeof(SelectionResizeAdorner).GetMethod(
                "BeginResize",
                BindingFlags.Instance | BindingFlags.NonPublic)!;
            var complete = typeof(SelectionResizeAdorner).GetMethod(
                "CompleteResize",
                BindingFlags.Instance | BindingFlags.NonPublic)!;
            var bottomRight = typeof(SelectionResizeAdorner).GetField(
                "_bottomRight",
                BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(adorner) as UIElement;

            begin.Invoke(adorner, null);
            AssertEqual(true, adorner.IsResizeActive, "Resize fixture must begin an active gesture");

            bottomRight!.RaiseEvent(new System.Windows.Input.MouseEventArgs(System.Windows.Input.Mouse.PrimaryDevice, 0)
            {
                RoutedEvent = System.Windows.Input.Mouse.LostMouseCaptureEvent
            });

            AssertEqual(true, adorner.IsResizeActive,
                "Normal capture release must wait for DragCompleted instead of canceling the resize");
            AssertEqual(0, canceled, "Normal capture release must not emit ResizeCanceled");

            complete.Invoke(adorner, new object[] { false });
            AssertEqual(false, adorner.IsResizeActive, "Successful DragCompleted must close the gesture");
            AssertEqual(1, completed, "Successful DragCompleted must emit ResizeCompleted once");
            AssertEqual(0, canceled, "Successful resize must never emit ResizeCanceled");
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

static Task TestNewTextBoxUsesCompactLabelAwareFrame()
{
    Exception? failure = null;
    var thread = new Thread(() =>
    {
        try
        {
            var regularVm = new MainViewModel();
            regularVm.AddTextBoxCommand.Execute(null);
            var regular = regularVm.Template.Objects.Last(item => item.Type == ObjectType.TextBox);
            AssertNear(32, regular.WidthMm, 0.001, "Regular label must use the compact TextBox width");
            AssertNear(6, regular.HeightMm, 0.001, "Regular label must use a one-line compact TextBox height");
            AssertEqual("Text Box", regular.Text, "New TextBox must not reserve space for instructional sample copy");
            AssertNear(0.2, regular.Style.TextPaddingMm, 0.001, "New TextBox must use compact physical padding");
            AssertEqual(TextVerticalAlignmentMode.Center, regular.Style.VerticalAlignment,
                "Compact one-line TextBox must align its initial text with the frame");

            var smallVm = new MainViewModel();
            smallVm.Template.WidthMm = 20;
            smallVm.Template.HeightMm = 8;
            smallVm.AddTextBoxCommand.Execute(null);
            var small = smallVm.Template.Objects.Last(item => item.Type == ObjectType.TextBox);
            AssertEqual(true, small.XMm >= 0 && small.YMm >= 0,
                "Small-label TextBox must start inside the label");
            AssertEqual(true, small.XMm + small.WidthMm <= smallVm.Template.WidthMm + 0.001,
                "Small-label TextBox width must remain inside the label");
            AssertEqual(true, small.YMm + small.HeightMm <= smallVm.Template.HeightMm + 0.001,
                "Small-label TextBox height must remain inside the label");

            var frameArea = small.WidthMm * small.HeightMm;
            var contentWidthMm = MmConverter.DipToMm(TextBoxOverflowDetector.GetContentWidthDip(
                small,
                MmConverter.MmToDip(small.WidthMm),
                constrainToBox: true));
            var contentHeightMm = MmConverter.DipToMm(TextBoxOverflowDetector.GetContentHeightDip(
                small,
                MmConverter.MmToDip(small.HeightMm),
                constrainToBox: true));
            var contentRatio = contentWidthMm * contentHeightMm / frameArea;
            AssertEqual(true, contentRatio >= 0.90,
                $"Compact default must retain at least 90% printable area (actual {contentRatio:P1})");
            AssertEqual(false, TextBoxOverflowDetector.IsOverflowing(
                    small,
                    small.Text,
                    MmConverter.MmToDip(small.WidthMm),
                    MmConverter.MmToDip(small.HeightMm)),
                "Default compact text must fit the default compact frame");

            var hitSize = (double)typeof(SelectionResizeAdorner).GetField(
                "HandleHitSize",
                BindingFlags.Static | BindingFlags.NonPublic)!.GetRawConstantValue()!;
            var markerSize = (double)typeof(SelectionResizeAdorner).GetField(
                "HandleMarkerSize",
                BindingFlags.Static | BindingFlags.NonPublic)!.GetRawConstantValue()!;
            AssertNear(10, hitSize, 0.001, "Compact selection handle must retain a usable 10-DIP hit target");
            AssertNear(5, markerSize, 0.001, "Compact selection handle marker must not obscure small TextBox content");

            var propertiesXaml = File.ReadAllText(Path.Combine(
                FindRepositoryRoot(),
                "src",
                "ANLAbel.App",
                "MainWindow.xaml"));
            AssertEqual(true,
                propertiesXaml.Contains("Content=\"Tight 0\" Tag=\"0\"", StringComparison.Ordinal)
                && propertiesXaml.Contains("Content=\"Compact .2\" Tag=\"0.2\"", StringComparison.Ordinal)
                && propertiesXaml.Contains("Content=\"Comfort 1\" Tag=\"1\"", StringComparison.Ordinal),
                "TextBox Properties must retain all three approved physical-padding presets");
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

static Task TestTextBoxHasNoOutlineStroke()
{
    Exception? failure = null;
    OutlineStyle outline = OutlineStyle.Solid;
    var thickness = 1.0;
    var thread = new Thread(() =>
    {
        try
        {
            var vm = new MainViewModel();
            vm.AddTextBoxCommand.Execute(null);
            var box = vm.Template.Objects.Last(item => item.Type == ObjectType.TextBox);
            outline = box.Style.OutlineStyle;
            thickness = box.Style.BorderThicknessMm;

            // Canvas must force zero border thickness for TextBox even if style asked for a stroke.
            box.Style.OutlineStyle = OutlineStyle.Solid;
            box.Style.BorderThicknessMm = 0.5;
            var template = new LabelTemplate { WidthMm = 80, HeightMm = 40 };
            template.Objects.Add(box);
            var canvas = new LabelDesignerCanvas { Template = template };
            canvas.Measure(new Size(800, 600));
            canvas.Arrange(new Rect(0, 0, 800, 600));
            canvas.UpdateLayout();
            var border = typeof(LabelDesignerCanvas)
                .GetField("_objectElements", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(canvas) as System.Collections.IDictionary;
            AssertEqual(true, border is not null, "Canvas must keep object visuals");
            FrameworkElement? element = null;
            foreach (System.Collections.DictionaryEntry entry in border!)
            {
                if (ReferenceEquals(entry.Key, box))
                {
                    element = entry.Value as FrameworkElement;
                    break;
                }
            }

            AssertEqual(true, element is Border, "TextBox host must be a Border");
            var host = (Border)element!;
            AssertEqual(0, host.BorderThickness.Left, "TextBox must not paint a permanent outline stroke");
            AssertEqual(0, host.BorderThickness.Top, "TextBox must not paint a permanent outline stroke");
            _ = canvas.Width;
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

    AssertEqual(OutlineStyle.None, outline, "New TextBox must default OutlineStyle.None (no viền line)");
    AssertEqual(0, thickness, "New TextBox must default BorderThicknessMm 0");
    // Also assert FixedFrame default (user-owned drag frame, not content-hug).
    Exception? sizingFailure = null;
    TextSizingMode sizing = TextSizingMode.AutoFit;
    var sizingThread = new Thread(() =>
    {
        try
        {
            var vm = new MainViewModel();
            vm.AddTextBoxCommand.Execute(null);
            sizing = vm.Template.Objects.Last(item => item.Type == ObjectType.TextBox).Style.TextSizing;
        }
        catch (Exception ex)
        {
            sizingFailure = ex;
        }
    });
    sizingThread.SetApartmentState(ApartmentState.STA);
    sizingThread.Start();
    sizingThread.Join();
    if (sizingFailure is not null)
    {
        throw sizingFailure;
    }

    AssertEqual(TextSizingMode.FixedFrame, sizing, "New TextBox must default FixedFrame so object size is drag-owned");
    return Task.CompletedTask;
}

static Task TestFixedFrameTextBoxOverflowKeepsSize()
{
    Exception? failure = null;
    var widthBefore = 0.0;
    var heightBefore = 0.0;
    var widthAfter = 0.0;
    var heightAfter = 0.0;
    var overflows = false;
    var thread = new Thread(() =>
    {
        try
        {
            var item = new LabelObject
            {
                Id = "textbox-fixed",
                Type = ObjectType.TextBox,
                Text = "This fixed frame is intentionally too small for the full sentence content.",
                WidthMm = 18,
                HeightMm = 5
            };
            item.Style.TextSizing = TextSizingMode.FixedFrame;
            item.Style.TextOverflow = TextOverflowMode.Error;
            item.Style.FontSizePt = 11;
            widthBefore = item.WidthMm;
            heightBefore = item.HeightMm;

            var template = new LabelTemplate { WidthMm = 80, HeightMm = 40 };
            var canvas = new LabelDesignerCanvas { Template = template };
            template.Objects.Add(item);
            item.Text = item.Text + " Extra words that must not rewrite the frame.";

            widthAfter = item.WidthMm;
            heightAfter = item.HeightMm;
            overflows = TextBoxOverflowDetector.IsOverflowing(
                item,
                item.Text,
                MmConverter.MmToDip(item.WidthMm),
                MmConverter.MmToDip(item.HeightMm));
            _ = canvas.Width;
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

    AssertEqual(widthBefore, widthAfter, "FixedFrame TextBox must not rewrite width on text edits");
    AssertEqual(heightBefore, heightAfter, "FixedFrame TextBox must not rewrite height on text edits");
    AssertEqual(true, overflows, "FixedFrame overflow with Error policy must be detectable on the shared preflight path");
    return Task.CompletedTask;
}

static Task TestTextAutoFitIncludesPhysicalPadding()
{
    Exception? failure = null;
    var measuredWidthMm = 0.0;
    var expectedMinimumWidthMm = 0.0;
    var measuredHeightMm = 0.0;
    var expectedMinimumHeightMm = 0.0;
    var thread = new Thread(() =>
    {
        try
        {
            var item = new LabelObject
            {
                Id = "autofit-padding",
                Type = ObjectType.Text,
                Text = "Padded label"
            };
            item.Style.TextPaddingLeftMm = 3;
            item.Style.TextPaddingRightMm = 4;
            item.Style.TextPaddingTopMm = 2;
            item.Style.TextPaddingBottomMm = 1;
            var template = new LabelTemplate { WidthMm = 60, HeightMm = 30 };
            var canvas = new LabelDesignerCanvas { Template = template };
            template.Objects.Add(item);

            var text = TextBoxOverflowDetector.CreateFormattedText(item, item.Text, Brushes.Black);
            var horizontalPaddingDip = TextBoxOverflowDetector.GetHorizontalPaddingDip(item, constrainToBox: false);
            var verticalPaddingDip = TextBoxOverflowDetector.GetVerticalPaddingDip(item, constrainToBox: false);
            expectedMinimumWidthMm = MmConverter.DipToMm(Math.Ceiling(text.WidthIncludingTrailingWhitespace) + horizontalPaddingDip) + 0.6;
            expectedMinimumHeightMm = MmConverter.DipToMm(Math.Ceiling(text.Height) + verticalPaddingDip) + 0.6;
            measuredWidthMm = item.WidthMm;
            measuredHeightMm = item.HeightMm;
            // Keep the canvas alive through the event/render cycle; this also
            // proves the real collection path, not a direct private helper.
            _ = canvas.Width;
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

    // LabelObject persists dimensions at 0.01 mm precision; allow that
    // quantization while still requiring the full physical padding budget.
    AssertEqual(true, measuredWidthMm + 0.01 >= expectedMinimumWidthMm,
        $"AutoFit width must include configured physical left/right padding (actual {measuredWidthMm:0.###} mm, expected at least {expectedMinimumWidthMm:0.###} mm)");
    AssertEqual(true, measuredHeightMm + 0.01 >= expectedMinimumHeightMm,
        $"AutoFit height must include configured physical top/bottom padding (actual {measuredHeightMm:0.###} mm, expected at least {expectedMinimumHeightMm:0.###} mm)");
    return Task.CompletedTask;
}

static Task TestPrinterSetupPreservesSavedStock()
{
    var expected = StandardLabelSizes.All.First(item =>
        item.Category == "Standard Thermal Labels"
        && Math.Abs(item.WidthMm - 50) < 0.001
        && Math.Abs(item.HeightMm - 20) < 0.001);
    Exception? failure = null;
    PrinterPaperInfo? selected = null;
    var thread = new Thread(() =>
    {
        try
        {
            var window = new PrinterSetupWindow(
                Array.Empty<PrinterInfo>(),
                initialPaperName: expected.Name);
            var listField = typeof(PrinterSetupWindow).GetField(
                "PaperSizesList",
                BindingFlags.Instance | BindingFlags.NonPublic);
            AssertEqual(true, listField is not null, "Printer setup must expose its stock list to the WPF view");
            selected = (listField!.GetValue(window) as ListBox)?.SelectedItem as PrinterPaperInfo;
            window.Close();
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

    AssertEqual(expected.Name, selected?.Name,
        "Reopening a saved non-first standard stock must keep that stock selected instead of silently converting it to Custom");
    return Task.CompletedTask;
}

static Task TestTextLayoutRecordsDisplayScale()
{
    Exception? failure = null;
    var oneDipIdentity = string.Empty;
    var twoDipIdentity = string.Empty;
    var thread = new Thread(() =>
    {
        try
        {
            var item = new LabelObject
            {
                Type = ObjectType.TextBox,
                Text = "Display scale",
                WidthMm = 30,
                HeightMm = 8,
                Style = { TextOverflow = TextOverflowMode.Error }
            };
            var widthDip = MmConverter.MmToDip(item.WidthMm);
            var heightDip = MmConverter.MmToDip(item.HeightMm);
            foreach (var pixelsPerDip in new[] { 1.0, 2.0 })
            {
                var formatted = TextBoxOverflowDetector.CreateFormattedText(item, item.Text, Brushes.Black, pixelsPerDip);
                var metrics = TextBoxOverflowDetector.Measure(
                    formatted,
                    item,
                    widthDip,
                    heightDip,
                    constrainToBox: true,
                    sourceValue: item.Text,
                    pixelsPerDip: pixelsPerDip);
                if (pixelsPerDip == 1.0)
                {
                    oneDipIdentity = metrics.IdentityFingerprint;
                }
                else
                {
                    twoDipIdentity = metrics.IdentityFingerprint;
                }
            }
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

    AssertEqual(false, string.IsNullOrWhiteSpace(oneDipIdentity), "Display-scale layout must carry an identity at 1.0 pixels-per-DIP");
    AssertEqual(false, string.IsNullOrWhiteSpace(twoDipIdentity), "Display-scale layout must carry an identity at 2.0 pixels-per-DIP");
    AssertEqual(false, string.Equals(oneDipIdentity, twoDipIdentity, StringComparison.Ordinal), "Different display scales must not masquerade as one text metric identity");
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

static Task TestPrintPreflightReportsMissingFont()
{
    var template = new LabelTemplate
    {
        Name = "Missing font preflight",
        WidthMm = 50,
        HeightMm = 25,
        Dpi = 300
    };
    template.Objects.Add(new LabelObject
    {
        Type = ObjectType.Text,
        Name = "Font check",
        Text = "Production label",
        XMm = 2,
        YMm = 2,
        WidthMm = 30,
        HeightMm = 8,
        Style = { FontFamily = "ANLAbel-Missing-Font-For-Preflight-9F3D" }
    });

    var result = new PrintService().ValidateRows(template, new IReadOnlyDictionary<string, string>?[] { null });
    AssertEqual(false, result.IsSuccess, "Preflight must block a text object whose requested font is unavailable");
    AssertEqual(true, result.Issues.Any(issue => issue.Message.Contains("not installed", StringComparison.OrdinalIgnoreCase)),
        "Missing-font preflight must explain the fallback before production printing");
    return Task.CompletedTask;
}

static Task TestPrintPreflightReportsMissingGlyph()
{
    var template = new LabelTemplate
    {
        Name = "Missing glyph preflight",
        WidthMm = 50,
        HeightMm = 25,
        Dpi = 300
    };
    template.Objects.Add(new LabelObject
    {
        Type = ObjectType.Text,
        Name = "Glyph check",
        Text = "Part \U0010FFFF",
        XMm = 2,
        YMm = 2,
        WidthMm = 30,
        HeightMm = 8,
        Style = { FontFamily = "Arial" }
    });

    var observation = TextBoxOverflowDetector.ObserveFont(template.Objects[0], template.Objects[0].Text);
    AssertEqual(true, observation.RequestedFamilyAvailable, "The glyph fixture must use the installed baseline Arial family");
    AssertEqual(true, observation.HasMissingGlyphs, "The unassigned code point must be reported as missing glyph coverage");

    var result = new PrintService().ValidateRows(template, new IReadOnlyDictionary<string, string>?[] { null });
    AssertEqual(false, result.IsSuccess, "Preflight must block a label whose requested font has no glyph for the resolved data");
    AssertEqual(true, result.Issues.Any(issue => issue.Message.Contains("no glyph", StringComparison.OrdinalIgnoreCase)),
        "Missing-glyph preflight must explain the font coverage failure");
    return Task.CompletedTask;
}

static Task TestFontCatalogAndUnicodePolicy()
{
    var installed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Arial" };
    AssertEqual(false, TextBoxOverflowDetector.IsFontAvailable("ANLAbel-Fixture", installed),
        "The removable-font fixture must start as unavailable");
    installed.Add("ANLAbel-Fixture");
    AssertEqual(true, TextBoxOverflowDetector.IsFontAvailable("anlabel-fixture", installed),
        "Font family lookup must be case-insensitive after installation");
    AssertEqual("ANLAbel-Fixture", TextBoxOverflowDetector.ResolveFontFamilyName("anlabel-fixture", installed),
        "An installed family must remain the requested resolved family");
    installed.Remove("ANLAbel-Fixture");
    AssertEqual("Arial", TextBoxOverflowDetector.ResolveFontFamilyName("ANLAbel-Fixture", installed),
        "Removing the family must select the deterministic Arial fallback");

    var item = new LabelObject
    {
        Type = ObjectType.TextBox,
        Text = "A\u0301 مرحبا 中 😀 \U0010FFFF",
        Style = { FontFamily = "Arial" }
    };
    var observation = TextBoxOverflowDetector.ObserveFont(item, item.Text, installed);
    AssertEqual(true, observation.HasMissingGlyphs, "The unassigned scalar must be reported even beside international text");
    AssertEqual(true, observation.MissingGlyphCodePoints.Contains(0x10FFFF), "Missing-glyph evidence must preserve the scalar code point");
    AssertEqual(System.Windows.FlowDirection.RightToLeft,
        TextBoxOverflowDetector.ResolveFlowDirection(TextDirectionMode.Auto, "שלום 123"),
        "Auto direction must resolve Hebrew text as RTL");

    var snapshot = TextLayoutContract.Capture("A\u0301 中 😀");
    AssertEqual(true, snapshot.GraphemeClusters.Contains("A\u0301"),
        "Grapheme segmentation must keep a combining sequence intact");
    AssertEqual(true, snapshot.GraphemeClusters.Contains("中"),
        "Grapheme segmentation must keep a CJK scalar intact");
    AssertEqual(true, snapshot.GraphemeClusters.Contains("😀"),
        "Grapheme segmentation must keep an emoji surrogate pair intact");
    return Task.CompletedTask;
}

static Task TestImagePreflightValidatesResolution()
{
    var invalidTemplate = new LabelTemplate
    {
        Name = "Invalid image preflight",
        WidthMm = 50,
        HeightMm = 30,
        Dpi = 300,
        PrinterProfile = new PrinterProfile { Dpi = 300 }
    };
    invalidTemplate.Objects.Add(new LabelObject
    {
        Type = ObjectType.Image,
        Name = "Corrupt image",
        XMm = 2,
        YMm = 2,
        WidthMm = 10,
        HeightMm = 10,
        ImageDataBase64 = "not-a-base64-image"
    });

    var invalidResult = new PrintService().ValidateRows(
        invalidTemplate,
        new IReadOnlyDictionary<string, string>?[] { null });
    AssertEqual(false, invalidResult.IsSuccess, "Corrupt embedded images must block production preflight");
    AssertEqual(true, invalidResult.Issues.Any(issue => issue.Message.Contains("valid embedded bitmap", StringComparison.OrdinalIgnoreCase)),
        "Corrupt image diagnostics must tell the operator to replace the embedded bitmap");

    var lowResolutionTemplate = new LabelTemplate
    {
        Name = "Low-resolution image preflight",
        WidthMm = 50,
        HeightMm = 30,
        Dpi = 300,
        // PrintService resolves the effective grid from the saved printer
        // profile first. Keep the fixture explicit so it cannot silently use
        // the model's 203-DPI default and let a 100 px source pass.
        PrinterProfile = new PrinterProfile { Dpi = 300 }
    };
    lowResolutionTemplate.Objects.Add(new LabelObject
    {
        Type = ObjectType.Image,
        Name = "Low resolution",
        XMm = 2,
        YMm = 2,
        WidthMm = 10,
        HeightMm = 10,
        ImageDataBase64 = CreatePngBase64(100, 100)
    });

    var lowResolutionResult = new PrintService().ValidateRows(
        lowResolutionTemplate,
        new IReadOnlyDictionary<string, string>?[] { null });
    AssertEqual(false, lowResolutionResult.IsSuccess, "An image below the effective printer grid must block production preflight");
    AssertEqual(true, lowResolutionResult.Issues.Any(issue => issue.Message.Contains("source density", StringComparison.OrdinalIgnoreCase)),
        "Low-resolution image diagnostics must report the effective source density");

    var adequateTemplate = new LabelTemplate
    {
        Name = "Adequate image preflight",
        WidthMm = 50,
        HeightMm = 30,
        Dpi = 300,
        PrinterProfile = new PrinterProfile { Dpi = 300 }
    };
    adequateTemplate.Objects.Add(new LabelObject
    {
        Type = ObjectType.Image,
        Name = "Adequate resolution",
        XMm = 2,
        YMm = 2,
        WidthMm = 10,
        HeightMm = 10,
        ImageDataBase64 = CreatePngBase64(120, 120)
    });

    var adequateResult = new PrintService().ValidateRows(
        adequateTemplate,
        new IReadOnlyDictionary<string, string>?[] { null });
    AssertEqual(true, adequateResult.IsSuccess, "An image with at least one source pixel per target printer dot must pass image preflight");
    return Task.CompletedTask;
}

static Task TestImageRasterPolicyIdentity()
{
    var payload = CreatePngBase64(24, 24);
    var template = new LabelTemplate
    {
        Name = "Raster identity",
        WidthMm = 50,
        HeightMm = 30,
        Dpi = 300,
        PrinterProfile = new PrinterProfile { Dpi = 300 }
    };
    template.Objects.Add(new LabelObject
    {
        Type = ObjectType.Image,
        Name = "Raster",
        XMm = 2,
        YMm = 2,
        WidthMm = 10,
        HeightMm = 10,
        ImageDataBase64 = payload,
        ImageRasterMode = ImageRasterMode.MonochromeOrderedDither
    });

    var service = new PrintService();
    var orderedPlan = service.CreateDesignPlan(template);
    AssertEqual(true, !string.IsNullOrWhiteSpace(orderedPlan.ImageRasterFingerprint),
        "A decoded image must contribute payload, mode and dimensions to the print identity");
    AssertEqual(24, template.Objects[0].ImagePixelWidth,
        "Plan creation must hydrate source pixel width for legacy templates");
    AssertEqual(24, template.Objects[0].ImagePixelHeight,
        "Plan creation must hydrate source pixel height for legacy templates");

    var thresholdTemplate = new LabelTemplate
    {
        Name = template.Name,
        WidthMm = template.WidthMm,
        HeightMm = template.HeightMm,
        Dpi = template.Dpi,
        PrinterProfile = new PrinterProfile { Dpi = 300 }
    };
    thresholdTemplate.Objects.Add(LabelObjectCloner.Clone(template.Objects[0]));
    thresholdTemplate.Objects[0].ImageRasterMode = ImageRasterMode.MonochromeThreshold;
    var thresholdPlan = service.CreateDesignPlan(thresholdTemplate);
    AssertEqual(false,
        string.Equals(orderedPlan.ImageRasterFingerprint, thresholdPlan.ImageRasterFingerprint, StringComparison.Ordinal),
        "Changing the application raster policy must invalidate the scene/print identity");

    var orderedA = ImageRasterizer.Decode(payload, ImageRasterMode.MonochromeOrderedDither)
        ?? throw new InvalidOperationException("ordered-dither fixture did not decode");
    var orderedB = ImageRasterizer.Decode(payload, ImageRasterMode.MonochromeOrderedDither)
        ?? throw new InvalidOperationException("ordered-dither fixture did not decode twice");
    var bytesA = new byte[orderedA.PixelWidth * orderedA.PixelHeight * 4];
    var bytesB = new byte[orderedB.PixelWidth * orderedB.PixelHeight * 4];
    orderedA.CopyPixels(bytesA, orderedA.PixelWidth * 4, 0);
    orderedB.CopyPixels(bytesB, orderedB.PixelWidth * 4, 0);
    AssertEqual(true, bytesA.SequenceEqual(bytesB),
        "Preview and print raster transforms must be deterministic for the same payload/mode");

    var staleTemplate = new LabelTemplate
    {
        Name = "Stale raster metadata",
        WidthMm = 50,
        HeightMm = 30,
        Dpi = 300,
        PrinterProfile = new PrinterProfile { Dpi = 300 }
    };
    staleTemplate.Objects.Add(new LabelObject
    {
        Type = ObjectType.Image,
        Name = "Stale",
        XMm = 2,
        YMm = 2,
        WidthMm = 10,
        HeightMm = 10,
        ImageDataBase64 = payload,
        ImagePixelWidth = 1,
        ImagePixelHeight = 1
    });
    var staleFailure = false;
    try
    {
        _ = new PrintService().CreateDesignPlan(staleTemplate);
    }
    catch (InvalidOperationException ex)
    {
        staleFailure = ex.Message.Contains("metadata", StringComparison.OrdinalIgnoreCase);
    }

    AssertEqual(true, staleFailure,
        "Stale stored dimensions must fail closed instead of silently changing the image identity");
    return Task.CompletedTask;
}

static Task TestImageRasterAlphaAndMonochromeFixtures()
{
    // BGRA pixels: opaque black, transparent black, half-transparent black,
    // opaque white. The monochrome policy must composite transparency over
    // white label stock before converting it to thermal 1-bpp output.
    var alphaPayload = CreatePngBase64FromBgra(
        4,
        1,
        new byte[]
        {
            0, 0, 0, 255,
            0, 0, 0, 0,
            0, 0, 0, 127,
            255, 255, 255, 255
        });
    var threshold = ImageRasterizer.Decode(alphaPayload, ImageRasterMode.MonochromeThreshold)
        ?? throw new InvalidOperationException("alpha threshold fixture did not decode");
    var thresholdBytes = CopyBgraPixels(threshold);
    AssertEqual(true,
        thresholdBytes.SequenceEqual(new byte[]
        {
            0, 0, 0, 255,
            255, 255, 255, 255,
            255, 255, 255, 255,
            255, 255, 255, 255
        }),
        "Threshold conversion must keep opaque black while alpha-compositing transparent and half-transparent black over white stock");

    // A 4x4 mid-grey patch gives a known 50% black Bayer result. A simple
    // threshold keeps every pixel white (128 is not below the threshold), so
    // this proves that the two persisted monochrome modes are distinct.
    var midGrey = Enumerable.Repeat(new byte[] { 128, 128, 128, 255 }, 16)
        .SelectMany(pixel => pixel)
        .ToArray();
    var ditherPayload = CreatePngBase64FromBgra(4, 4, midGrey);
    var dither = ImageRasterizer.Decode(ditherPayload, ImageRasterMode.MonochromeOrderedDither)
        ?? throw new InvalidOperationException("ordered-dither fixture did not decode");
    var ditherBytes = CopyBgraPixels(dither);
    var blackPixels = Enumerable.Range(0, 16).Count(index => ditherBytes[index * 4] == 0);
    AssertEqual(8, blackPixels,
        "The Bayer 4x4 fixture must produce the documented 50% thermal black coverage for mid-grey");
    AssertEqual(true, ditherBytes.All(channel => channel is 0 or 255),
        "Application-owned monochrome output must be a deterministic binary black/white raster");

    return Task.CompletedTask;
}

static string CreatePngBase64(int width, int height)
{
    var pixels = new byte[width * height * 4];
    for (var index = 0; index < pixels.Length; index += 4)
    {
        pixels[index] = 32;
        pixels[index + 1] = 96;
        pixels[index + 2] = 160;
        pixels[index + 3] = 255;
    }

    var bitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, pixels, width * 4);
    var encoder = new PngBitmapEncoder();
    encoder.Frames.Add(BitmapFrame.Create(bitmap));
    using var stream = new MemoryStream();
    encoder.Save(stream);
    return Convert.ToBase64String(stream.ToArray());
}

static string CreatePngBase64FromBgra(int width, int height, byte[] pixels)
{
    if (pixels.Length != width * height * 4)
    {
        throw new ArgumentException("Fixture pixels must be BGRA32.", nameof(pixels));
    }

    var bitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, pixels, width * 4);
    var encoder = new PngBitmapEncoder();
    encoder.Frames.Add(BitmapFrame.Create(bitmap));
    using var stream = new MemoryStream();
    encoder.Save(stream);
    return Convert.ToBase64String(stream.ToArray());
}

static byte[] CopyBgraPixels(BitmapSource bitmap)
{
    var converted = new FormatConvertedBitmap(bitmap, PixelFormats.Bgra32, null, 0);
    converted.Freeze();
    var bytes = new byte[converted.PixelWidth * converted.PixelHeight * 4];
    converted.CopyPixels(bytes, converted.PixelWidth * 4, 0);
    return bytes;
}

static Task TestPrintPreflightHonorsCancellation()
{
    var template = new LabelTemplate { Name = "Cancelable preflight", WidthMm = 50, HeightMm = 25, Dpi = 203 };
    template.Objects.Add(new LabelObject
    {
        Type = ObjectType.TextBox,
        Name = "Description",
        Text = "Industrial label",
        XMm = 2,
        YMm = 2,
        WidthMm = 30,
        HeightMm = 8
    });

    var rows = Enumerable.Range(0, 500)
        .Select(_ => (IReadOnlyDictionary<string, string>?)new Dictionary<string, string>())
        .ToArray();
    using var cancellation = new CancellationTokenSource();
    var progressReports = 0;
    var progress = new InlineProgress<PrintPreflightProgress>(_ =>
    {
        progressReports++;
        cancellation.Cancel();
    });

    var canceled = false;
    try
    {
        new PrintPreflightValidator().Validate(template, rows, cancellationToken: cancellation.Token, progress: progress);
    }
    catch (OperationCanceledException)
    {
        canceled = true;
    }

    AssertEqual(true, canceled, "Preflight must stop when its cancellation token is signaled");
    AssertEqual(true, progressReports > 0, "Preflight must report progress before cancellation");
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

static Task TestPreflightReportsNonSquareDpi()
{
    var template = new LabelTemplate { Name = "Non-square DPI Test", WidthMm = 40, HeightMm = 25, Dpi = 203 };
    template.Objects.Add(new LabelObject
    {
        Type = ObjectType.QRCode,
        Text = "DPI-X-Y",
        XMm = 2,
        YMm = 2,
        WidthMm = 12,
        HeightMm = 12,
        QrDpi = 300,
        QrSizingMode = QrSizingMode.FixedVersionAndModuleSize,
        QrModuleSizePx = 1
    });

    var result = new PrintPreflightValidator().Validate(
        template,
        new IReadOnlyDictionary<string, string>?[] { null },
        printDpi: 305,
        printDpiY: 609);

    AssertEqual(false, result.IsSuccess, "Preflight must evaluate both effective printer DPI axes");
    AssertEqual(true, result.Issues.Any(issue => issue.Message.Contains("305", StringComparison.Ordinal)
        && issue.Message.Contains("609", StringComparison.Ordinal)), "The diagnostic must expose the effective non-square DPI");
    return Task.CompletedTask;
}

static Task TestEffectiveOutputContractHash()
{
    var contract = new EffectiveOutputContract
    {
        PrinterName = "Queue-A",
        RequestedTicketHash = "requested",
        EffectiveTicketHash = "effective",
        DpiX = 203,
        DpiY = 305,
        LabelWidthMm = 100,
        LabelHeightMm = 50,
        GapMm = 2,
        MarginMm = 1,
        MediaType = LabelMediaType.Gap,
        FeedDirection = FeedDirection.TopToBottom,
        PrintableWidthDip = 370,
        PrintableHeightDip = 180,
        PrintableAreaVerified = true
    };
    var plan = new PrintRenderPlan
    {
        Dpi = 203,
        DpiX = 203,
        DpiY = 305,
        LabelWidthMm = 100,
        LabelHeightMm = 50,
        GapMm = 2,
        MarginMm = 1,
        PrintableWidthDip = 370,
        PrintableHeightDip = 180,
        PrintableAreaVerified = true,
        TextResourceFingerprint = "text-resource-plan"
    };

    var thermalProfile = ThermalRasterGoldenContract.CreateProfile(
        "Queue-A",
        "ThermalDriver",
        "DRV-1",
        "FW-1",
        "STOCK-A",
        "RIBBON-NONE",
        "CAL-2026-08",
        LabelMediaType.Gap,
        FeedDirection.TopToBottom,
        false,
        203,
        305,
        100,
        50,
        2,
        0,
        0,
        1,
        1);
    var thermalRaster = RasterGoldenContract.Describe(4, 2, 203, 305, 16, "Pbgra32", new byte[32]);
    var thermalBinding = ThermalRasterGoldenContract.CreateBinding("golden-queue-a", thermalProfile, thermalRaster);
    var bound = plan
        .WithThermalRasterGolden(thermalBinding)
        .WithOutputContractHash(contract.Fingerprint, contract.IsTicketValidated);
    AssertEqual(string.Empty, plan.OutputContractHash, "A design-time plan must remain unbound");
    AssertEqual(contract.Fingerprint, bound.OutputContractHash, "Effective plan must carry the reviewed contract fingerprint");
    AssertEqual(true, bound.OutputContractTicketVerified, "Effective plan must expose whether ticket evidence was included");
    AssertEqual(plan.LabelWidthMm, bound.LabelWidthMm, "Binding a contract must not mutate label geometry");
    AssertEqual(plan.TextResourceFingerprint, bound.TextResourceFingerprint, "Binding a contract must preserve the text resource identity");
    AssertEqual(thermalBinding.Fingerprint, bound.ThermalRasterGolden?.Fingerprint ?? string.Empty, "Output-contract binding must preserve the thermal golden identity");

    var previewTemplate = new LabelTemplate { Name = "Thermal golden preview", WidthMm = 100, HeightMm = 50, Dpi = 203 };
    var previewPage = new PrintService().CreatePreviewPage(previewTemplate, null, 1, bound);
    AssertEqual(thermalBinding.Fingerprint, previewPage.ThermalRasterGolden?.Fingerprint ?? string.Empty, "Preview metadata must carry the thermal golden identity");

    var changed = contract with { DpiY = 609 };
    AssertEqual(false, string.Equals(contract.Fingerprint, changed.Fingerprint, StringComparison.Ordinal), "Changing effective DPI must invalidate the contract fingerprint");
    return Task.CompletedTask;
}

static Task TestPrintPlanCarriesSceneIdentity()
{
    var template = new LabelTemplate
    {
        Name = "Scene Identity Test",
        WidthMm = 80,
        HeightMm = 40,
        Dpi = 203
    };
    template.Objects.Add(new LabelObject
    {
        Id = "part-number",
        Type = ObjectType.Text,
        Text = "PN-100",
        XMm = 3,
        YMm = 4,
        WidthMm = 30,
        HeightMm = 7,
        Rotation = 90
    });

    var service = new PrintService();
    var plan = service.CreateDesignPlan(template);
    var snapshot = DocumentSnapshot.Capture(template);
    var compilation = SceneCompiler.Compile(snapshot);

    AssertEqual(true, plan.SceneCompilationVerified, "A valid template must produce a verified scene plan");
    AssertEqual(snapshot.DocumentHash, plan.DocumentHash, "The print plan must carry the immutable document hash");
    AssertEqual(snapshot.TextResourceFingerprint, plan.TextResourceFingerprint, "The print plan must carry the text resource fingerprint");
    AssertEqual(compilation.SceneHash, plan.SceneHash, "The print plan must carry the compiled scene hash");
    AssertEqual(true, compilation.Succeeded, "The test scene must compile");
    AssertEqual(MmConverter.MmToPrinterDots(template.WidthMm, plan.DpiX), plan.DeviceGeometry.LabelWidthDots,
        "The print plan must expose the exact device-dot label width");
    AssertEqual(MmConverter.MmToPrinterDots(template.HeightMm, plan.DpiY), plan.DeviceGeometry.LabelHeightDots,
        "The print plan must expose the exact device-dot label height");

    var samePlan = service.CreateDesignPlan(template);
    AssertEqual(true, ReferenceEquals(plan.CompiledScene, samePlan.CompiledScene), "Repeated requests for an unchanged template must reuse the immutable compiled scene");
    AssertEqual(1L, service.SceneCompileCount, "An unchanged template must compile only once");
    AssertEqual(true, service.SceneCacheHitCount >= 1, "Repeated design/preview requests must record a scene-cache hit");

    var previewPage = service.CreatePreviewPage(template, null, 1);
    AssertEqual(plan.DocumentHash, previewPage.DocumentHash, "Preview must use the same document identity as the design plan");
    AssertEqual(plan.TextResourceFingerprint, previewPage.TextResourceFingerprint, "Preview must carry the same text resource fingerprint as the design plan");
    AssertEqual(plan.SceneHash, previewPage.SceneHash, "Preview must use the same scene identity as the design plan");
    AssertEqual(true, previewPage.SceneCompilationVerified, "Preview must expose scene verification state");

    var effective = plan.WithOutputContractHash("effective-preview-contract", outputContractTicketVerified: true);
    var effectivePage = service.CreatePreviewPage(template, null, 1, effective);
    AssertEqual(effective.OutputContractHash, effectivePage.OutputContractHash, "Preview must retain the effective output contract identity");
    AssertEqual(true, effectivePage.OutputContractTicketVerified, "Preview must expose verified ticket evidence");
    AssertEqual(effective.DpiX, effectivePage.DpiX, "Preview must retain effective horizontal DPI");
    AssertEqual(effective.DpiY, effectivePage.DpiY, "Preview must retain effective vertical DPI");
    AssertEqual(effective.DeviceGeometry.LabelWidthDots, effectivePage.DeviceGeometry.LabelWidthDots,
        "Preview must retain the effective device-dot geometry");
    var effectiveDrawing = service.CreatePreviewDrawing(template, null, effective);
    AssertEqual(true, effectiveDrawing.WidthDip > 0 && effectiveDrawing.HeightDip > 0, "Preview drawing must accept the effective output plan");

    template.Objects[0].Text = "PN-200";
    var changed = service.CreateDesignPlan(template);
    AssertEqual(false, string.Equals(plan.DocumentHash, changed.DocumentHash, StringComparison.Ordinal), "Changing authored text must invalidate the document identity");
    AssertEqual(false, string.Equals(plan.SceneHash, changed.SceneHash, StringComparison.Ordinal), "Changing authored text must invalidate the scene identity");
    AssertEqual(2L, service.SceneCompileCount, "Changing authored text must invalidate the cached compilation");

    template.Objects.Add(new LabelObject
    {
        Id = "part-number",
        Type = ObjectType.Text,
        Text = "duplicate",
        XMm = 40,
        YMm = 4,
        WidthMm = 20,
        HeightMm = 7
    });
    var invalid = service.CreateDesignPlan(template);
    AssertEqual(false, invalid.SceneCompilationVerified, "A duplicate object ID must fail scene verification");
    AssertEqual(string.Empty, invalid.SceneHash, "An invalid scene must not expose a dispatchable scene hash");
    AssertEqual(true, invalid.SceneDiagnostics.Contains("SCN002", StringComparison.Ordinal), "The plan must retain the actionable scene diagnostic");

    var staleTemplate = new LabelTemplate
    {
        Name = "Stale preview template",
        WidthMm = 80,
        HeightMm = 40,
        Dpi = 203
    };
    staleTemplate.Objects.Add(new LabelObject
    {
        Id = "different-scene",
        Type = ObjectType.Text,
        Text = "Different",
        WidthMm = 20,
        HeightMm = 6
    });
    var staleRejected = false;
    try
    {
        service.CreatePreviewPage(staleTemplate, null, 1, effective);
    }
    catch (InvalidOperationException)
    {
        staleRejected = true;
    }

    AssertEqual(true, staleRejected, "Preview must reject an effective plan whose document identity no longer matches the template");
    return Task.CompletedTask;
}

static Task TestBoundRowsPreserveGeometryIdentity()
{
    // A production batch may render thousands of rows through one authored
    // template.  Row data is allowed to change text/barcode payloads, but it
    // must never mutate the authored frame or replace the compiled scene used
    // by preview, preflight and print.  This fixture deliberately exercises a
    // short value, a longer value and a third value through the same plan.
    var template = new LabelTemplate
    {
        Name = "Bound-row geometry identity",
        WidthMm = 80,
        HeightMm = 40,
        Dpi = 203
    };

    template.Objects.Add(new LabelObject
    {
        Id = "part-number",
        Type = ObjectType.TextBox,
        Name = "Part number",
        BindingExpression = "{PartNo}",
        XMm = 3,
        YMm = 3,
        WidthMm = 34,
        HeightMm = 8,
        Style =
        {
            TextSizing = TextSizingMode.FixedFrame,
            TextOverflow = TextOverflowMode.Ellipsis,
            FontSizePt = 11,
            VerticalAlignment = TextVerticalAlignmentMode.Center
        }
    });

    var qr = new LabelObject
    {
        Id = "batch-code",
        Type = ObjectType.QRCode,
        Name = "Batch code",
        BindingExpression = "{Code}",
        XMm = 45,
        YMm = 3,
        WidthMm = 20,
        HeightMm = 20,
        QrSizingMode = QrSizingMode.FixedVersionAndModuleSize,
        QrFixedVersion = 4,
        QrErrorCorrection = QrErrorCorrection.M,
        QrModuleSizePx = 2,
        QrQuietZoneModules = 4,
        QrDpi = 203,
        ShowBarcodeText = false
    };
    // QR convenience setters can calculate a fixed target before the authored
    // frame is assigned.  Restore the deliberate production frame afterwards.
    qr.WidthMm = 20;
    qr.HeightMm = 20;
    template.Objects.Add(qr);

    var rows = new IReadOnlyDictionary<string, string>?[]
    {
        new Dictionary<string, string> { ["PartNo"] = "PN-001", ["Code"] = "A1" },
        new Dictionary<string, string> { ["PartNo"] = "PN-002-LONG-BATCH", ["Code"] = new string('A', 40) },
        new Dictionary<string, string> { ["PartNo"] = "PN-003", ["Code"] = "B2-REV-C" }
    };

    var service = new PrintService();
    var plan = service.CreateDesignPlan(template);
    var baselineSnapshot = DocumentSnapshot.Capture(template);
    var baselineGeometry = CreateSceneGeometryFingerprint(plan.CompiledScene);

    var preflight = service.ValidateRows(template, rows, plan);
    AssertEqual(true, preflight.IsSuccess,
        "All representative bound rows must pass the same production preflight before rendering");
    AssertEqual(true, plan.SceneCompilationVerified,
        "The shared bound-row fixture must start from a verified immutable scene");

    for (var index = 0; index < rows.Length; index++)
    {
        var row = rows[index];
        var preview = service.CreatePreviewPage(template, row, index + 1, plan);
        AssertEqual(plan.DocumentHash, preview.DocumentHash,
            "Changing a bound row must not change the authored document identity");
        AssertEqual(plan.TextResourceFingerprint, preview.TextResourceFingerprint,
            "Changing a bound row must not change the text-resource identity");
        AssertEqual(plan.SceneHash, preview.SceneHash,
            "Preview and print must keep one compiled scene identity for every row");
        AssertEqual(plan.DeviceGeometry.LabelWidthDots, preview.DeviceGeometry.LabelWidthDots,
            "Every bound row must use the same device label width");
        AssertEqual(plan.DeviceGeometry.LabelHeightDots, preview.DeviceGeometry.LabelHeightDots,
            "Every bound row must use the same device label height");
        AssertEqual(true, preview.Visual is not null && preview.WidthDip > 0 && preview.HeightDip > 0,
            "Every bound row must produce a renderable preview page");

        var currentSnapshot = DocumentSnapshot.Capture(template);
        AssertEqual(baselineSnapshot.DocumentHash, currentSnapshot.DocumentHash,
            "Rendering a bound row must not mutate the authored template");
        AssertEqual(baselineGeometry, CreateSceneGeometryFingerprint(plan.CompiledScene),
            "The compiled physical geometry must remain stable across bound rows");
    }

    return Task.CompletedTask;
}

static string CreateSceneGeometryFingerprint(SceneCompilationResult? compilation)
{
    if (compilation is null)
    {
        return string.Empty;
    }

    return string.Join("|", compilation.Nodes
        .OrderBy(node => node.ZIndex)
        .ThenBy(node => node.Id, StringComparer.Ordinal)
        .Select(node => string.Join(":",
            node.Id,
            node.Type,
            node.Rotation,
            node.LayoutBoundsMm.LeftMm.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
            node.LayoutBoundsMm.TopMm.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
            node.LayoutBoundsMm.WidthMm.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
            node.LayoutBoundsMm.HeightMm.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
            node.VisualBoundsMm.LeftMm.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
            node.VisualBoundsMm.TopMm.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
            node.VisualBoundsMm.WidthMm.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
            node.VisualBoundsMm.HeightMm.ToString("R", System.Globalization.CultureInfo.InvariantCulture))));
}

static Task TestSceneCompilationCacheStress()
{
    var template = new LabelTemplate
    {
        Name = "Scene cache stress",
        WidthMm = 100,
        HeightMm = 50,
        Dpi = 203
    };
    for (var index = 0; index < 24; index++)
    {
        template.Objects.Add(new LabelObject
        {
            Id = $"cache-{index:00}",
            Type = index % 3 == 0 ? ObjectType.Text : ObjectType.Rectangle,
            Text = $"Part {index:00}",
            XMm = 2 + (index % 6) * 15,
            YMm = 2 + (index / 6) * 10,
            WidthMm = 10,
            HeightMm = 5
        });
    }

    var service = new PrintService();
    var startBytes = GC.GetTotalMemory(forceFullCollection: true);
    var first = service.CreateDesignPlan(template);
    for (var index = 0; index < 9_999; index++)
    {
        var current = service.CreateDesignPlan(template);
        if (!ReferenceEquals(first.CompiledScene, current.CompiledScene))
        {
            throw new InvalidOperationException("Scene cache returned a different compilation for an unchanged document.");
        }
    }

    var endBytes = GC.GetTotalMemory(forceFullCollection: true);
    var deltaMb = Math.Max(0, endBytes - startBytes) / (1024d * 1024d);
    Console.WriteLine($"INFO scene cache stress: requests=10000; compiles={service.SceneCompileCount}; cacheHits={service.SceneCacheHitCount}; managedDeltaMB={deltaMb:0.0}");
    AssertEqual(1L, service.SceneCompileCount, "Repeated unchanged scene requests must compile once");
    AssertEqual(9_999L, service.SceneCacheHitCount, "Every request after the first must hit the scene cache");
    AssertEqual(true, deltaMb < 128, "Scene cache stress must not retain an unbounded managed allocation");
    return Task.CompletedTask;
}

static Task TestCompiledScenePresenter()
{
    var template = new LabelTemplate
    {
        Name = "Compiled Presenter Test",
        WidthMm = 40,
        HeightMm = 25,
        Dpi = 300
    };
    template.Objects.Add(new LabelObject
    {
        Id = "compiled-rectangle",
        Type = ObjectType.Rectangle,
        XMm = 5,
        YMm = 6,
        WidthMm = 8,
        HeightMm = 4,
        Style = { FillStyle = FillStyle.Solid, FillColor = "#000000", OutlineStyle = OutlineStyle.None }
    });

    var service = new PrintService();
    var renderer = new LabelVisualRenderer();
    var compiledPlan = service.CreateDesignPlan(template);
    AssertEqual(true, compiledPlan.SceneCompilationVerified, "The compiled presenter fixture must be scene-verified");
    AssertEqual(true, compiledPlan.CompiledScene is not null, "A verified design plan must retain its compiled scene");

    // Before mutation, the new presenter and the legacy compatibility path must
    // place the same primitive at the same physical coordinates.
    var compiledBefore = RenderAndFindDarkBoundsMm(renderer, template, compiledPlan);
    var legacyPlan = new PrintRenderPlan { Dpi = 300, LabelWidthMm = 40, LabelHeightMm = 25 };
    var legacyBefore = RenderAndFindDarkBoundsMm(renderer, template, legacyPlan);
    AssertNear(legacyBefore.Left, compiledBefore.Left, 0.3, "Compiled and legacy presenters must agree on the rectangle X edge");
    AssertNear(legacyBefore.Top, compiledBefore.Top, 0.3, "Compiled and legacy presenters must agree on the rectangle Y edge");

    // The authoring object is deliberately changed after plan creation. The
    // compiled presenter must keep the immutable plan geometry, while a legacy
    // plan without a scene continues to reflect the current authoring model.
    template.Objects[0].XMm = 20;
    var compiledAfter = RenderAndFindDarkBoundsMm(renderer, template, compiledPlan);
    var legacyAfter = RenderAndFindDarkBoundsMm(renderer, template, legacyPlan);
    AssertNear(5, compiledAfter.Left, 0.3, "Compiled rendering must not drift when the mutable authoring model changes");
    AssertNear(20, legacyAfter.Left, 0.3, "Legacy compatibility rendering must still use the current authoring model");

    // Invalid snapshots remain previewable through the explicit compatibility
    // fallback, but never produce a compiled presenter that could be dispatched.
    template.Objects.Add(new LabelObject
    {
        Id = "compiled-rectangle",
        Type = ObjectType.Text,
        Text = "duplicate",
        XMm = 1,
        YMm = 1,
        WidthMm = 5,
        HeightMm = 3
    });
    var invalidPlan = service.CreateDesignPlan(template);
    AssertEqual(false, invalidPlan.SceneCompilationVerified, "Duplicate IDs must disable compiled dispatch");
    AssertEqual(null, invalidPlan.CompiledScene, "An invalid plan must not retain a dispatchable compiled scene");
    var fallbackVisual = renderer.Render(template, null, invalidPlan);
    AssertEqual(true, fallbackVisual is not null, "Invalid preview scenes must use the explicit legacy fallback rather than crash");
    return Task.CompletedTask;
}

static Task TestSpoolAcceptedDoesNotClaimPhysicalCompletion()
{
    var result = new PrintJobResult(PrintJobOutcome.SpoolAccepted, "Test Queue", "test", 1, SpoolJobId: 42);
    AssertEqual(true, result.IsAccepted, "SpoolAccepted is a successful submission outcome");
    AssertEqual(false, result.IsPhysicalCompletionVerified, "SpoolAccepted must not claim physical completion");
    AssertEqual(true, result.UserFacingStatus.Contains("not verified", StringComparison.OrdinalIgnoreCase), "The user status must explain the evidence boundary");
    AssertEqual(true, result.UserFacingStatus.Contains("42", StringComparison.Ordinal), "A discovered spool identifier must be visible without implying physical completion");
    return Task.CompletedTask;
}

static Task TestSpoolAcceptanceWithoutIdentityIsExplicit()
{
    var result = new PrintJobResult(PrintJobOutcome.SpoolAccepted, "Test Queue", "test", 1);
    AssertEqual(true, result.IsAccepted, "A PrintDocument return remains an accepted submission outcome");
    AssertEqual(false, result.HasSpoolIdentity, "Missing queue enumeration must not invent a spool identity");
    AssertEqual(true, result.UserFacingStatus.Contains("No spool job identity", StringComparison.OrdinalIgnoreCase), "The operator status must explain that queue correlation is unavailable");
    AssertEqual(true, result.UserFacingStatus.Contains("Do not retry automatically", StringComparison.OrdinalIgnoreCase), "The operator status must prevent an unsafe duplicate retry");
    return Task.CompletedTask;
}

static async Task TestSpoolMonitorPreservesQueueEvidence()
{
    var service = new PrintService(new SequenceSpoolReader(
        new SpoolJobObservation("Test Queue", 42, SpoolJobState.Printing, IsTerminal: false),
        new SpoolJobObservation("Test Queue", 42, SpoolJobState.Completed,
            "Queue completed; physical output remains unverified.", IsTerminal: true)));
    var result = new PrintJobResult(PrintJobOutcome.SpoolAccepted, "Test Queue", "test", 1, SpoolJobId: 42);

    var monitor = await service.MonitorSpoolJobAsync(
        result,
        timeout: TimeSpan.FromSeconds(1),
        pollInterval: TimeSpan.Zero);
    AssertEqual(SpoolJobState.Completed, monitor.FinalObservation.State, "Monitor must expose the terminal queue state");
    AssertEqual(false, monitor.PhysicalOutputVerified, "Queue completion must not be reported as physical completion");
    AssertEqual(false, monitor.TimedOut, "A terminal queue state must not be reported as a timeout");

    var noIdentity = await service.MonitorSpoolJobAsync(
        result with { SpoolJobId = null },
        timeout: TimeSpan.FromMilliseconds(1),
        pollInterval: TimeSpan.Zero);
    AssertEqual(SpoolJobState.Unknown, noIdentity.FinalObservation.State, "Missing spool identity must fail closed");
    AssertEqual(0, noIdentity.PollCount, "Missing spool identity must not query an unrelated queue job");
}

static Task TestExplicitPrintPathFailsClosed()
{
    var template = new LabelTemplate { Name = "Queue guard" };
    var rows = new IReadOnlyDictionary<string, string>?[] { null };
    var result = new PrintService().PrintRowsWithResult(
        template,
        rows,
        printerName: " ",
        description: "queue guard test");

    AssertEqual(PrintJobOutcome.Failed, result.Outcome, "An explicit print path without a queue must fail closed");
    AssertEqual(false, result.IsAccepted, "A missing queue must never be treated as an accepted print");
    AssertEqual(true, result.ErrorMessage.Contains("default queue", StringComparison.OrdinalIgnoreCase), "The error must explain that Windows default fallback is disabled");
    AssertEqual(true, result.ErrorMessage.Contains("verified", StringComparison.OrdinalIgnoreCase), "The error must tell the operator to choose a verified queue");
    return Task.CompletedTask;
}

static Task TestInteractivePrintSelectionRejectsImplicitDefault()
{
    var guard = typeof(PrintService).GetMethod(
        "RequireExplicitInteractiveQueue",
        BindingFlags.Static | BindingFlags.NonPublic);
    AssertEqual(true, guard is not null,
        "Interactive print must have one explicit queue-selection guard");

    var rejected = false;
    try
    {
        guard!.Invoke(null, new object?[] { null, "Office Default", "Office Default" });
    }
    catch (TargetInvocationException ex) when (ex.InnerException is InvalidOperationException inner
        && inner.Message.Contains("not explicitly selected", StringComparison.OrdinalIgnoreCase))
    {
        rejected = true;
    }

    AssertEqual(true, rejected,
        "Accepting the unchanged Windows default must be rejected when no industrial queue was configured");

    var selected = (string)guard!.Invoke(null, new object?[] { null, "Office Default", "Zebra-203" })!;
    AssertEqual("Zebra-203", selected,
        "A deliberately changed queue must pass through to normal queue verification");

    var configured = (string)guard.Invoke(null, new object?[] { "Saved Zebra", "Saved Zebra", "Saved Zebra" })!;
    AssertEqual("Saved Zebra", configured,
        "A previously verified saved queue must remain valid even when the dialog opens on it");
    return Task.CompletedTask;
}

static async Task TestPrintDispatchWorkerIsSta()
{
    var runner = typeof(PrintService).GetMethod(
        "RunOnDedicatedStaAsync",
        BindingFlags.Static | BindingFlags.NonPublic);
    AssertEqual(true, runner is not null,
        "Print dispatch must expose one dedicated worker boundary");

    var staRunner = runner!.MakeGenericMethod(typeof(int));
    var workerTask = (Task<int>)staRunner.Invoke(
        null,
        new object?[]
        {
            new Func<int>(() => Thread.CurrentThread.GetApartmentState() == ApartmentState.STA ? 1 : 0),
            CancellationToken.None
        })!;
    var isSta = await workerTask;
    AssertEqual(1, isSta,
        "PrintDocument must run on a dedicated STA so a slow driver does not use the UI dispatcher");

    using var canceled = new CancellationTokenSource();
    canceled.Cancel();
    var cancellationObserved = false;
    try
    {
        await new PrintService().PrintRowsWithResultAsync(
            new LabelTemplate { Name = "Canceled dispatch" },
            new IReadOnlyDictionary<string, string>?[] { null },
            printerName: " ",
            description: "canceled dispatch",
            cancellationToken: canceled.Token);
    }
    catch (OperationCanceledException)
    {
        cancellationObserved = true;
    }

    AssertEqual(true, cancellationObserved,
        "Cancellation requested before dispatch must stop before creating a worker or print snapshot");
}

static async Task TestCalibrationDispatchCancellation()
{
    using var canceled = new CancellationTokenSource();
    canceled.Cancel();

    var cancellationObserved = false;
    try
    {
        await new PrintService().PrintCalibrationWithResultAsync(
            new LabelTemplate { Name = "Canceled calibration" },
            canceled.Token);
    }
    catch (OperationCanceledException)
    {
        cancellationObserved = true;
    }

    AssertEqual(true, cancellationObserved,
        "Calibration cancellation requested before dialog/dispatch must stop without touching the printer driver");
}

static async Task TestEffectivePlanPreparationCancellation()
{
    using var canceled = new CancellationTokenSource();
    canceled.Cancel();

    var cancellationObserved = false;
    try
    {
        await new PrintService().CreateEffectivePlanAsync(
            new LabelTemplate { Name = "Canceled effective plan" },
            "queue-not-used",
            canceled.Token);
    }
    catch (OperationCanceledException)
    {
        cancellationObserved = true;
    }

    AssertEqual(true, cancellationObserved,
        "Canceled preview preparation must stop before cloning or opening a printer queue");
}

static Task TestPrintPreviewBusyGate()
{
    var gate = typeof(PrintPreviewWindow).GetMethod(
        "CanBeginPrint",
        BindingFlags.Static | BindingFlags.NonPublic);
    AssertEqual(true, gate is not null,
        "Print Preview must have one shared busy gate for its button and click handler");

    var canStart = (bool)gate!.Invoke(null, new object[] { false, false, 1 })!;
    var blockedByPrint = (bool)gate.Invoke(null, new object[] { true, false, 1 })!;
    var blockedByPreview = (bool)gate.Invoke(null, new object[] { false, true, 1 })!;
    var blockedWithoutRows = (bool)gate.Invoke(null, new object[] { false, false, 0 })!;
    AssertEqual(true, canStart, "Print Preview must allow one dispatch when rows exist and no operation is busy");
    AssertEqual(false, blockedByPrint, "Print Preview must reject a second click while dispatch is running");
    AssertEqual(false, blockedByPreview, "Print Preview must reject printing while preview raster work is running");
    AssertEqual(false, blockedWithoutRows, "Print Preview must reject dispatch with no rows");
    return Task.CompletedTask;
}

static Task TestMissingNamedPrinterQueueFailsClosed()
{
    var template = new LabelTemplate { Name = "Missing queue" };
    var rows = new IReadOnlyDictionary<string, string>?[] { null };
    var service = new PrintService(queueLookup: new MissingPrinterQueueLookup());
    var result = service.PrintRowsWithResult(
        template,
        rows,
        printerName: "Saved Zebra Queue",
        description: "missing queue test");

    AssertEqual(PrintJobOutcome.Failed, result.Outcome, "A saved queue that disappeared must fail closed");
    AssertEqual(false, result.IsAccepted, "A missing saved queue must not be accepted for printing");
    AssertEqual(true, result.ErrorMessage.Contains("no longer installed", StringComparison.OrdinalIgnoreCase), "The missing queue diagnostic must be preserved");
    AssertEqual(true, result.ErrorMessage.Contains("default", StringComparison.OrdinalIgnoreCase), "The missing queue path must not silently select Windows default");
    return Task.CompletedTask;
}

static async Task TestQuickPrintPreflightOrdering()
{
    var lookup = new MissingPrinterQueueLookup();
    var vm = new MainViewModel(
        new ProjectFileService(),
        new ExcelDataService(),
        new PrintService(queueLookup: lookup),
        new PrinterDiscoveryService(),
        new PrintLogService(),
        printerQueueLookup: lookup);
    vm.Template.Name = "Quick print preparation";
    vm.Template.PrinterProfile.PrinterName = "Saved Zebra Queue";

    var jobId = $"quick-print-order-{Guid.NewGuid():N}";
    var dispatch = typeof(MainViewModel).GetMethod(
        "DispatchTrackedPrintAsync",
        BindingFlags.Instance | BindingFlags.NonPublic);
    AssertEqual(true, dispatch is not null, "Quick print must have one tracked dispatch preparation boundary");

    Exception? failure = null;
    try
    {
        var task = (Task)dispatch!.Invoke(
            vm,
            new object[]
            {
                new List<IReadOnlyDictionary<string, string>?>
                {
                    new Dictionary<string, string> { ["PartNo"] = "PN-001" }
                },
                "quick print ordering",
                jobId,
                null!,
                null!
            })!;
        await task;
    }
    catch (TargetInvocationException ex)
    {
        failure = ex.InnerException ?? ex;
    }
    catch (Exception ex)
    {
        // Async methods capture exceptions thrown before their first await in
        // the returned Task rather than wrapping them in reflection's
        // TargetInvocationException.
        failure = ex;
    }

    AssertEqual(true, failure is not null, "Quick print must stop before lifecycle transitions when the saved queue cannot be resolved");
    AssertEqual(true, failure!.Message.Contains("no longer installed", StringComparison.OrdinalIgnoreCase), "Quick print must preserve the missing queue diagnostic");

    var stateStore = (PrintJobStateStore)typeof(MainViewModel)
        .GetField("_printJobStateStore", BindingFlags.Instance | BindingFlags.NonPublic)!
        .GetValue(vm)!;
    AssertEqual(null, stateStore.GetCurrentState(jobId), "Quick print must not record Created/PreflightPassed before effective queue preparation succeeds");
}

static async Task TestViewModelShowsMissingPrinterWarning()
{
    var lookup = new MissingPrinterQueueLookup();
    var vm = new MainViewModel(
        new ProjectFileService(),
        new ExcelDataService(),
        new PrintService(queueLookup: lookup),
        new PrinterDiscoveryService(),
        new PrintLogService(),
        printerQueueLookup: lookup);

    vm.Template.PrinterProfile.PrinterName = "Saved Zebra Queue";
    await vm.RefreshPrinterQueueStatusAsync();

    AssertEqual(true, vm.HasPrinterQueueWarning, "The editor must warn before dispatch when the saved queue is unavailable");
    AssertEqual("Printer unavailable", vm.PrinterQueueStatusText, "The status bar must identify the unavailable saved queue");
    AssertEqual(true, vm.PrinterQueueStatusMessage.Contains("no longer installed", StringComparison.OrdinalIgnoreCase), "The UI must preserve the actionable missing-queue reason");
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
            var textBox = new LabelObject
            {
                Type = ObjectType.TextBox,
                XMm = 36,
                YMm = 7.25,
                WidthMm = 24,
                HeightMm = 9,
                BindingExpression = "{PartNo}",
                Text = "Part",
                Style =
                {
                    TextSizing = TextSizingMode.FixedFrame,
                    TextOverflow = TextOverflowMode.Error
                }
            };
            template.Objects.Add(textBox);
            var before = (text.XMm, text.YMm, text.WidthMm, text.HeightMm);
            var textBoxBefore = (textBox.XMm, textBox.YMm, textBox.WidthMm, textBox.HeightMm);

            var canvas = new LabelDesignerCanvas { Template = template };
            canvas.PreviewRow = new Dictionary<string, string> { ["PartNo"] = "A" };
            canvas.PreviewRow = new Dictionary<string, string>
            {
                ["PartNo"] = "A-VERY-LONG-PART-NUMBER-THAT-USED-TO-RESIZE-THE-MODEL"
            };

            var after = (text.XMm, text.YMm, text.WidthMm, text.HeightMm);
            var textBoxAfter = (textBox.XMm, textBox.YMm, textBox.WidthMm, textBox.HeightMm);
            AssertEqual(before, after, "Changing PreviewRow must not mutate designer object geometry");
            AssertEqual(textBoxBefore, textBoxAfter,
                "Changing PreviewRow must never mutate the user-authored TextBox frame");
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

static async Task TestPropertiesExcelVerification()
{
    var dir = Path.Combine(Environment.CurrentDirectory, "TestOutput", $"properties-excel-verification-{Guid.NewGuid():N}");
    Directory.CreateDirectory(dir);
    var excelPath = Path.Combine(dir, "data.xlsx");

    static void WriteWorkbook(string path, string partNumber)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Parts");
        sheet.Cell(1, 1).Value = "PartNo";
        sheet.Cell(1, 2).Value = "Qty";
        sheet.Cell(2, 1).Value = partNumber;
        sheet.Cell(2, 2).Value = 5;
        workbook.SaveAs(path);
    }

    WriteWorkbook(excelPath, "PN-100");
    var vm = new MainViewModel();
    await vm.ImportExcelAsync(excelPath, "Parts");

    AssertEqual(ExcelLinkVerificationState.Verified, vm.ExcelLinkVerificationState,
        "A successful import must be treated as real verification because it opened the workbook and read the sheet/header");
    AssertEqual(true, vm.ExcelLinkVerificationTrustText.Contains("2 columns", StringComparison.Ordinal),
        "Verified state must expose evidence instead of a generic success claim");

    await Task.Delay(50);
    WriteWorkbook(excelPath, "PN-200");
    File.SetLastWriteTimeUtc(excelPath, DateTime.UtcNow.AddSeconds(1));
    await vm.VerifyExcelLinkAsync();

    AssertEqual(ExcelLinkVerificationState.Verified, vm.ExcelLinkVerificationState,
        "Verification must return to Verified after refreshing a changed workbook");
    AssertEqual(true, vm.ExcelDataView!.Cast<DataRowView>().Any(r => (string)r["PartNo"] == "PN-200"),
        "Verification must refresh changed rows before certifying the Excel link");

    File.Delete(excelPath);
    await vm.VerifyExcelLinkAsync();
    AssertEqual(ExcelLinkVerificationState.Failed, vm.ExcelLinkVerificationState,
        "A missing workbook must fail verification instead of keeping a green status");
    AssertEqual(true, vm.ExcelLinkVerificationDetail.Contains("not", StringComparison.OrdinalIgnoreCase)
        || vm.ExcelLinkVerificationDetail.Contains("find", StringComparison.OrdinalIgnoreCase),
        "Failed verification must expose an actionable file error");

    vm.UnlinkExcel();
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
    await ((AsyncRelayCommand)vm.UseDataSourceCommand).ExecuteAsync(source);
    await PollUntilAsync(() => source.LastUsedUtc is not null, 2000);

    AssertEqual(true, source.LastUsedUtc is not null, "Using a shared source must set LastUsedUtc");
    AssertEqual(true, source.LastUsedUtc >= before.AddSeconds(-1), "LastUsedUtc must reflect the time of use, not an old default");
    AssertEqual(1, source.RecentTemplates.Count, "RecentTemplates must record the template that used this source");
    AssertEqual(templatePath, source.RecentTemplates[0], "RecentTemplates must contain the current template's path");

    // Using it again from the same template must not duplicate the entry.
    await ((AsyncRelayCommand)vm.UseDataSourceCommand).ExecuteAsync(source);
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
    using (var savedRegistryDocument = System.Text.Json.JsonDocument.Parse(File.ReadAllText(registryPath)))
    {
        AssertEqual(System.Text.Json.JsonValueKind.Object, savedRegistryDocument.RootElement.ValueKind, "Re-saved registry must use the versioned document shape");
        AssertEqual(1, savedRegistryDocument.RootElement.GetProperty("SchemaVersion").GetInt32(), "Re-saved registry schema version must be current");
    }
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
    var printOperationLogService = new PrintOperationLogService(logPath);

    var vm = new MainViewModel(
        new ANLAbel.Project.SaveLoad.ProjectFileService(),
        new ExcelDataService(),
        new ANLAbel.Printing.PrinterProfiles.PrintService(),
        new ANLAbel.Printing.PrinterProfiles.PrinterDiscoveryService(),
        new PrintLogService(Path.Combine(dir, "print-history.xlsx")),
        new DataOperationLogService(),
        dataSourceRegistry: null,
        printOperationLogService: printOperationLogService);

    vm.Template.PrinterProfile.PrinterName = "Test Printer";
    vm.Template.PrinterProfile.Dpi = 203;
    var rows = new IReadOnlyDictionary<string, string>?[] { new Dictionary<string, string> { ["PartNo"] = "PN-100" } };

    const string contractHash = "contract-hash-test";
    const string documentHash = "document-hash-test";
    const string textResourceFingerprint = "text-resource-hash-test";
    const string sceneHash = "scene-hash-test";
    const string jobId = "job-log-test";
    var plan = new PrintRenderPlan
    {
        DocumentHash = documentHash,
        SceneHash = sceneHash,
        OutputContractHash = contractHash,
        TextResourceFingerprint = textResourceFingerprint,
        SceneCompilationVerified = true
    };
    var result = new PrintJobResult(
        PrintJobOutcome.SpoolAccepted,
        "Test Printer",
        "Current row",
        1,
        OutputContractHash: contractHash,
        DocumentHash: documentHash,
        SceneHash: sceneHash,
        SceneCompilationVerified: true,
        TextResourceFingerprint: textResourceFingerprint);
    result = PrintService.AttachSupportEvidence(result, plan, durableJobIdHint: jobId);
    AssertEqual(true, result.SupportEvidenceFingerprint.Length == 64,
        "Shipped print result must carry a support evidence fingerprint before logging");
    await vm.WritePrintLogAsync("Current row", rows, rowCount: 1, labelCount: 1, result: result, jobId: jobId);

    await printOperationLogService.WaitForPendingWritesAsync();
    var lines = await WaitForLogLinesAsync(logPath, minLineCount: 1);
    AssertEqual(true, lines.Length >= 1, "A print job must produce a log line");
    AssertEqual(true, lines[0].Contains("\"PrinterName\":\"Test Printer\"", StringComparison.Ordinal), "Log entry must record the printer name");
    AssertEqual(true, lines[0].Contains("\"PrintMode\":\"Current row\"", StringComparison.Ordinal), "Log entry must record the print mode");
    AssertEqual(true, lines[0].Contains("\"Success\":true", StringComparison.Ordinal), "A successful print must be logged as success");
    AssertEqual(true, lines[0].Contains($"\"OutputContractHash\":\"{contractHash}\"", StringComparison.Ordinal), "Job log must carry the reviewed output contract fingerprint");
    AssertEqual(true, lines[0].Contains($"\"DocumentHash\":\"{documentHash}\"", StringComparison.Ordinal), "Job log must carry the document identity");
    AssertEqual(true, lines[0].Contains($"\"TextResourceFingerprint\":\"{textResourceFingerprint}\"", StringComparison.Ordinal), "Job log must carry the text resource identity");
    AssertEqual(true, lines[0].Contains($"\"SceneHash\":\"{sceneHash}\"", StringComparison.Ordinal), "Job log must carry the compiled scene identity");
    AssertEqual(true, lines[0].Contains($"\"JobId\":\"{jobId}\"", StringComparison.Ordinal), "Job log must carry the durable lifecycle identifier");
    AssertEqual(true, lines[0].Contains($"\"SupportEvidenceFingerprint\":\"{result.SupportEvidenceFingerprint}\"", StringComparison.Ordinal),
        "Job log must carry the redacted support-evidence fingerprint from the shipped print path");

    try { Directory.Delete(dir, true); } catch { }
}

static async Task TestQuickPrintLogCarriesQueueObservation()
{
    // The ribbon quick-print path must expose the same queue evidence as Print
    // Preview.  This test exercises the public log seam without requiring a real
    // printer, and makes sure a queue observation remains distinct from physical
    // completion.
    var dir = Path.Combine(Environment.CurrentDirectory, "TestOutput", $"quickprint-queue-log-{Guid.NewGuid():N}");
    Directory.CreateDirectory(dir);
    var logPath = Path.Combine(dir, "print-operations.jsonl");
    var printOperationLogService = new PrintOperationLogService(logPath);
    var vm = new MainViewModel(
        new ANLAbel.Project.SaveLoad.ProjectFileService(),
        new ExcelDataService(),
        new ANLAbel.Printing.PrinterProfiles.PrintService(),
        new ANLAbel.Printing.PrinterProfiles.PrinterDiscoveryService(),
        new PrintLogService(Path.Combine(dir, "print-history.xlsx")),
        new DataOperationLogService(),
        dataSourceRegistry: null,
        printOperationLogService: printOperationLogService);

    var observedAt = DateTimeOffset.UtcNow;
    var queueObservation = new SpoolJobObservation(
        "Test Printer",
        42,
        SpoolJobState.Printing,
        "Driver reports the job is printing.",
        IsTerminal: false,
        ObservedAtUtc: observedAt);
    var queueStatus = new SpoolJobMonitorResult(
        queueObservation,
        PollCount: 2,
        Elapsed: TimeSpan.FromMilliseconds(40),
        TimedOut: false);
    var result = new PrintJobResult(
        PrintJobOutcome.SpoolAccepted,
        "Test Printer",
        "Current row",
        1,
        SpoolJobId: 42);

    var rows = new IReadOnlyDictionary<string, string>?[]
    {
        new Dictionary<string, string> { ["PartNo"] = "PN-QUEUE" }
    };
    await vm.WritePrintLogAsync(
        "Current row",
        rows,
        rowCount: 1,
        labelCount: 1,
        result: result,
        jobId: "quick-job-queue",
        spoolStatus: queueStatus);

    // WritePrintLogAsync intentionally remains fire-and-forget in the product;
    // use the service barrier before reading the file so this regression never
    // races a Windows append handle.
    await printOperationLogService.WaitForPendingWritesAsync();
    var lines = await WaitForLogLinesAsync(logPath, minLineCount: 1);
    AssertEqual(true, lines.Length >= 1, "Quick print must write a job-level trace");
    AssertEqual(true, lines[0].Contains("\"SpoolJobId\":42", StringComparison.Ordinal), "Quick print log must preserve the spool identity");
    AssertEqual(true, lines[0].Contains("\"SpoolState\":\"Printing\"", StringComparison.Ordinal), "Quick print log must preserve the queue state");
    AssertEqual(true, lines[0].Contains("\"SpoolStatusPollCount\":2", StringComparison.Ordinal), "Quick print log must preserve bounded poll evidence");
    AssertEqual(true, lines[0].Contains("\"SpoolStatusTimedOut\":false", StringComparison.Ordinal), "A completed observation must not be marked timed out");

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

static Task TestLinearBarcodeXDimModuleSizeWarning()
{
    // Industrial 1D X-dimension: Properties must warn when the authored module
    // quantizes to under ~2 printer dots at the real print-plan DPI.
    var vm = new MainViewModel();
    vm.Template.Dpi = 600;
    vm.Template.PrinterProfile.Dpi = 203;

    var barcode = new LabelObject
    {
        Type = ObjectType.BarcodeCode128,
        BarcodeSymbology = BarcodeSymbology.Code128,
        Text = "ABC123",
        XMm = 2,
        YMm = 2,
        WidthMm = 40,
        HeightMm = 12,
        // 0.08 mm @ 203 DPI => 1 physical dot (fail closed).
        BarcodeModuleWidthMm = 0.08
    };
    vm.Template.Objects.Add(barcode);
    vm.SelectedObject = barcode;

    AssertEqual(true, vm.BarcodeModuleSizeWarningText.Length > 0,
        "Authored X-dim that yields sub-2-dot modules at PrinterProfile.Dpi must warn");
    AssertEqual(true,
        vm.BarcodeModuleSizeWarningText.Contains("dot", StringComparison.OrdinalIgnoreCase)
        || vm.BarcodeModuleSizeWarningText.Contains("module", StringComparison.OrdinalIgnoreCase)
        || vm.BarcodeModuleSizeWarningText.Contains("X-dimension", StringComparison.OrdinalIgnoreCase),
        "Linear X-dim warning must mention dots, module, or X-dimension");

    barcode.BarcodeModuleWidthMm = 0.33;
    AssertEqual(string.Empty, vm.BarcodeModuleSizeWarningText,
        "A comfortable industrial X-dim (~0.33 mm / 3 dots @ 203) must not warn");

    barcode.BarcodeModuleWidthMm = 0;
    AssertEqual(string.Empty, vm.BarcodeModuleSizeWarningText,
        "Legacy zero X-dim (frame-derived) must not use the authored-X warning path");

    return Task.CompletedTask;
}

static Task TestPreflightBlocksUndersizedLinearXDim()
{
    // Drive the shipped PrintService preflight path (not a re-implemented policy):
    // authored linear X-dim that quantizes to <2 dots at plan DPI must fail closed.
    var template = new LabelTemplate { Name = "Linear X-dim undersized", WidthMm = 50, HeightMm = 25, Dpi = 203 };
    template.PrinterProfile.Dpi = 203;
    template.Objects.Add(new LabelObject
    {
        Type = ObjectType.BarcodeCode128,
        BarcodeSymbology = BarcodeSymbology.Code128,
        Name = "Code128-thin",
        Text = "ABC123",
        XMm = 2,
        YMm = 2,
        WidthMm = 40,
        HeightMm = 12,
        BarcodeModuleWidthMm = 0.08
    });

    var preflight = new PrintService().ValidateRows(template, new IReadOnlyDictionary<string, string>?[] { null });
    AssertEqual(false, preflight.IsSuccess,
        "Preflight must block a linear barcode whose authored X-dim is under 2 printer dots");
    AssertEqual(true, preflight.Issues.Any(issue =>
            issue.Message.Contains("dot", StringComparison.OrdinalIgnoreCase)
            || issue.Message.Contains("module", StringComparison.OrdinalIgnoreCase)
            || issue.Message.Contains("X-dimension", StringComparison.OrdinalIgnoreCase)),
        "Linear X-dim preflight issue must mention dots/module/X-dimension");

    return Task.CompletedTask;
}

static Task TestPreflightAcceptsComfortableLinearXDim()
{
    var template = new LabelTemplate { Name = "Linear X-dim comfortable", WidthMm = 50, HeightMm = 25, Dpi = 203 };
    template.PrinterProfile.Dpi = 203;
    template.Objects.Add(new LabelObject
    {
        Type = ObjectType.BarcodeCode128,
        BarcodeSymbology = BarcodeSymbology.Code128,
        Name = "Code128-ok",
        Text = "ABC123",
        XMm = 2,
        YMm = 2,
        WidthMm = 40,
        HeightMm = 12,
        // ~0.33 mm ≈ 3 dots @ 203 — above both the 2-dot and 0.19 mm industrial floors.
        BarcodeModuleWidthMm = 0.33,
        ShowBarcodeText = false
    });

    var preflight = new PrintService().ValidateRows(template, new IReadOnlyDictionary<string, string>?[] { null });
    var linearModuleIssues = preflight.Issues
        .Where(issue => issue.Message.Contains("Linear module", StringComparison.OrdinalIgnoreCase)
                        || (issue.Message.Contains("X-dimension", StringComparison.OrdinalIgnoreCase)
                            && issue.Message.Contains("dot", StringComparison.OrdinalIgnoreCase)))
        .ToList();
    AssertEqual(0, linearModuleIssues.Count,
        "Comfortable authored X-dim must not raise the linear module industrial-risk rule: "
        + string.Join("; ", linearModuleIssues.Select(i => i.Message)));
    AssertEqual(true, preflight.IsSuccess,
        "Comfortable linear X-dim fixture must pass production preflight when no other rules fire: "
        + preflight.ToUserMessage());

    return Task.CompletedTask;
}

static Task TestLinearBarcodeWidthFollowsQuantizedXDim()
{
    // P1.a: SizedFromX production width = EffectiveModuleWidthMm * CountLinearModules at plan DPI.
    var renderer = new ZxingBarcodeRenderer();
    const string payload = "ABC123";
    const int dpi = 203;
    const double authoredX = 0.33;
    var options = new BarcodeRenderOptions { QuietZoneModules = 10 };
    var modules = renderer.CountLinearModules(payload, BarcodeType.Code128, options);
    AssertEqual(true, modules is > 1, "Logical module count must be available on the shipped encoder path");

    var expectedWidth = LinearBarcodeModuleContract.SizedFromXWidthMm(authoredX, modules!.Value, dpi);
    var tol = LinearBarcodeModuleContract.OnePrinterDotMm(dpi);

    var item = new LabelObject
    {
        Type = ObjectType.BarcodeCode128,
        BarcodeSymbology = BarcodeSymbology.Code128,
        Text = payload,
        WidthMm = 12, // intentional wrong frame — SizedFromX must replace it
        HeightMm = 12,
        BarcodeModuleWidthMm = authoredX,
        BarcodeWidthMode = BarcodeWidthMode.SizedFromX,
        QrQuietZoneModules = 10,
        ShowBarcodeText = false
    };

    var production = LinearBarcodeProductionWidth.ResolveSymbolWidthMm(item, renderer, dpi, payload);
    AssertEqual(true, Math.Abs(production - expectedWidth) <= tol + 1e-6,
        $"Production width {production:0.####} must match eff×modules {expectedWidth:0.####} within one-dot {tol:0.####}");

    AssertEqual(true, LinearBarcodeProductionWidth.TryApplySizedFromXWidth(item, renderer, dpi, payload),
        "SizedFromX must update WidthMm when the frame does not already match");
    AssertEqual(true, Math.Abs(item.WidthMm - expectedWidth) <= tol + 0.02,
        $"Applied WidthMm {item.WidthMm:0.####} must track production width {expectedWidth:0.####}");

    // Print path uses the same helper (parity with designer).
    var again = LinearBarcodeProductionWidth.ResolveSymbolWidthMm(item, renderer, dpi, payload);
    AssertEqual(true, Math.Abs(again - production) <= 1e-6, "Designer/print production width must share one formula");

    return Task.CompletedTask;
}

static Task TestCode39RatioAndQuietZonePreflight()
{
    var validator = new PrintPreflightValidator(new ZxingBarcodeRenderer());

    // 1. Illegal ratio 2.0:1 on sub-0.508 mm module
    var templateIllegalRatio = new LabelTemplate
    {
        WidthMm = 100,
        HeightMm = 50,
        Dpi = 300,
        Objects =
        [
            new LabelObject
            {
                Name = "Barcode1",
                Type = ObjectType.BarcodeCode128,
                BarcodeSymbology = BarcodeSymbology.Code39,
                Text = "CODE39",
                BarcodeModuleWidthMm = 0.33,
                Code39WideNarrowRatio = Code39WideNarrowRatio.Ratio2_0,
                QrQuietZoneModules = 10,
                WidthMm = 60,
                HeightMm = 20
            }
        ]
    };
    var resultIllegal = validator.Validate(templateIllegalRatio, Array.Empty<IReadOnlyDictionary<string, string>>(), 300);
    AssertEqual(true, resultIllegal.Issues.Any(i => i.Message.Contains("Code 39 wide:narrow ratio 2.0:1 requires")), "Ratio 2.0:1 with X < 0.508 mm must fail closed in preflight");

    // 2. Sub-standard quiet zone (< max(10X, 2.54 mm))
    var templateSubQz = new LabelTemplate
    {
        WidthMm = 100,
        HeightMm = 50,
        Dpi = 300,
        Objects =
        [
            new LabelObject
            {
                Name = "Barcode1",
                Type = ObjectType.BarcodeCode128,
                BarcodeSymbology = BarcodeSymbology.Code39,
                Text = "CODE39",
                BarcodeModuleWidthMm = 0.33,
                Code39WideNarrowRatio = Code39WideNarrowRatio.Ratio2_5,
                QrQuietZoneModules = 2,
                WidthMm = 60,
                HeightMm = 20
            }
        ]
    };
    var resultSubQz = validator.Validate(templateSubQz, Array.Empty<IReadOnlyDictionary<string, string>>(), 300);
    AssertEqual(true, resultSubQz.Issues.Any(i => i.Message.Contains("Code 39 quiet zone is")), "Quiet zone below standard minimum must fail closed in preflight");

    // 3. Legal ratio and compliant quiet zone
    var templateLegal = new LabelTemplate
    {
        WidthMm = 100,
        HeightMm = 50,
        Dpi = 300,
        Objects =
        [
            new LabelObject
            {
                Name = "Barcode1",
                Type = ObjectType.BarcodeCode128,
                BarcodeSymbology = BarcodeSymbology.Code39,
                Text = "CODE39",
                BarcodeModuleWidthMm = 0.33,
                Code39WideNarrowRatio = Code39WideNarrowRatio.Ratio2_5,
                QrQuietZoneModules = 10,
                WidthMm = 60,
                HeightMm = 20
            }
        ]
    };
    var resultLegal = validator.Validate(templateLegal, Array.Empty<IReadOnlyDictionary<string, string>>(), 300);
    AssertEqual(0, resultLegal.Issues.Count, "Legal Code 39 ratio and quiet zone must pass preflight");

    return Task.CompletedTask;
}

static Task TestCompiledScenePrintUsesSizedFromXWidth()
{
    // Skeptic fix: CreateRenderObject must hydrate BarcodeWidthMode + X so the
    // compiled-scene path (CreateDesignPlan → SceneCompilationVerified) does not
    // silently drop SizedFromX and stretch into the intentional wrong WidthMm.
    const string payload = "ABC123";
    const int dpi = 203;
    const double authoredX = 0.33;
    const int logicalModules = 100;
    var expectedWidth = LinearBarcodeModuleContract.SizedFromXWidthMm(authoredX, logicalModules, dpi);
    var tol = LinearBarcodeModuleContract.OnePrinterDotMm(dpi);

    var template = new LabelTemplate
    {
        Name = "Compiled SizedFromX",
        WidthMm = 120,
        HeightMm = 40,
        Dpi = dpi,
        PrinterProfile = new PrinterProfile { Dpi = dpi }
    };
    var item = new LabelObject
    {
        Type = ObjectType.BarcodeCode128,
        BarcodeSymbology = BarcodeSymbology.Code128,
        Name = "SizedFromX-compiled",
        Text = payload,
        XMm = 2,
        YMm = 2,
        // Deliberately wrong authored frame — compiled render must not use this.
        WidthMm = 10,
        HeightMm = 12,
        BarcodeModuleWidthMm = authoredX,
        BarcodeWidthMode = BarcodeWidthMode.SizedFromX,
        QrQuietZoneModules = 10,
        QrDpi = 300, // must not drive production width when plan DPI is 203
        ShowBarcodeText = false
    };
    template.Objects.Add(item);

    // Snapshot must capture the industrial fields before compile/render.
    var snapshot = DocumentSnapshot.Capture(template);
    AssertEqual(BarcodeWidthMode.SizedFromX, snapshot.Objects[0].BarcodeWidthMode,
        "DocumentSnapshot must persist BarcodeWidthMode for compiled-scene rehydration");
    AssertEqual(authoredX, snapshot.Objects[0].BarcodeModuleWidthMm,
        "DocumentSnapshot must persist BarcodeModuleWidthMm for compiled-scene rehydration");

    var plan = new PrintService().CreateDesignPlan(template);
    AssertEqual(true, plan.SceneCompilationVerified && plan.CompiledScene is not null,
        "Design plan must attach a verified compiled scene so CreateRenderObject is exercised");

    var bits = Enumerable.Range(0, logicalModules).Select(i => i % 2 == 0).ToArray();
    var fake = new CapturingBarcodeRenderer
    {
        LogicalModuleCount = logicalModules,
        VectorData = new BarcodeVectorData(logicalModules, 1, bits)
    };
    new LabelVisualRenderer(fake).Render(template, null, plan);

    AssertEqual(true, Math.Abs(fake.LastWidthMm - expectedWidth) <= tol + 1e-6,
        $"Compiled-scene print width {fake.LastWidthMm:0.####} must equal effMm×modules {expectedWidth:0.####} (tol {tol:0.####}), not intentional wrong WidthMm=10");
    AssertEqual(false, Math.Abs(fake.LastWidthMm - 10) < 0.5,
        "Compiled-scene render must not keep the intentional wrong frame width");

    return Task.CompletedTask;
}

static Task TestLegacyFrameOwnedWidthNotAutoSized()
{
    var renderer = new ZxingBarcodeRenderer();
    const double authoredWidth = 47.25;
    var item = new LabelObject
    {
        Type = ObjectType.BarcodeCode128,
        BarcodeSymbology = BarcodeSymbology.Code128,
        Text = "ABC123",
        WidthMm = authoredWidth,
        HeightMm = 12,
        BarcodeModuleWidthMm = 0,
        BarcodeWidthMode = BarcodeWidthMode.FrameOwned,
        QrQuietZoneModules = 10,
        ShowBarcodeText = false
    };

    var production = LinearBarcodeProductionWidth.ResolveSymbolWidthMm(item, renderer, 203, item.Text);
    AssertEqual(authoredWidth, production, "FrameOwned + X=0 must keep authored WidthMm");
    AssertEqual(false, LinearBarcodeProductionWidth.TryApplySizedFromXWidth(item, renderer, 203, item.Text),
        "FrameOwned must not mutate WidthMm");
    AssertEqual(authoredWidth, item.WidthMm, "Legacy open path must not silent-shrink WidthMm");

    // SizedFromX without positive X is also non-mutating (UsesSizedFromX false).
    item.BarcodeWidthMode = BarcodeWidthMode.SizedFromX;
    item.BarcodeModuleWidthMm = 0;
    AssertEqual(authoredWidth,
        LinearBarcodeProductionWidth.ResolveSymbolWidthMm(item, renderer, 203, item.Text),
        "SizedFromX with X=0 must fall back to frame-owned width");

    return Task.CompletedTask;
}

static Task TestLegacyLinearPreflightUsesLogicalModules()
{
    // P1.0b: BarcodeModuleWidthMm == 0 must estimate X from frame / pure logical
    // module count — not frame-scaled vector pixel columns (~1-dot false positive).
    var renderer = new ZxingBarcodeRenderer();
    const string payload = "ABC123";
    var logical = renderer.CountLinearModules(payload, BarcodeType.Code128, new BarcodeRenderOptions { QuietZoneModules = 10 });
    AssertEqual(true, logical is > 1, "Shipped CountLinearModules must return a pure multi-module count");

    var scaled = renderer.RenderBarcodeVector(payload, BarcodeType.Code128, 50, 12, 203, new BarcodeRenderOptions { QuietZoneModules = 10 });
    AssertEqual(true, scaled is not null && scaled.WidthModules > logical, "Scaled vector pixel width must exceed pure logical modules");

    // Comfortable frame-owned barcode (no authored X): must pass industrial module preflight.
    var comfortable = new LabelTemplate { Name = "Legacy linear comfortable", WidthMm = 80, HeightMm = 30, Dpi = 203 };
    comfortable.PrinterProfile.Dpi = 203;
    comfortable.Objects.Add(new LabelObject
    {
        Type = ObjectType.BarcodeCode128,
        BarcodeSymbology = BarcodeSymbology.Code128,
        Name = "Legacy-ok",
        Text = payload,
        XMm = 2,
        YMm = 2,
        WidthMm = 50,
        HeightMm = 12,
        BarcodeModuleWidthMm = 0,
        QrQuietZoneModules = 10,
        ShowBarcodeText = false
    });

    var ok = new PrintService().ValidateRows(comfortable, new IReadOnlyDictionary<string, string>?[] { null });
    var moduleIssues = ok.Issues
        .Where(i => i.Message.Contains("Linear module", StringComparison.OrdinalIgnoreCase))
        .ToList();
    AssertEqual(0, moduleIssues.Count,
        "Comfortable legacy frame-owned linear barcode must not fail as ~1-dot from pixel columns: "
        + string.Join("; ", moduleIssues.Select(i => i.Message)));
    AssertEqual(true, ok.IsSuccess,
        "Comfortable legacy linear preflight must pass: " + ok.ToUserMessage());

    // Truly undersized frame vs logical modules: still fail closed.
    var thin = new LabelTemplate { Name = "Legacy linear thin", WidthMm = 40, HeightMm = 25, Dpi = 203 };
    thin.PrinterProfile.Dpi = 203;
    thin.Objects.Add(new LabelObject
    {
        Type = ObjectType.BarcodeCode128,
        BarcodeSymbology = BarcodeSymbology.Code128,
        Name = "Legacy-thin",
        Text = payload,
        XMm = 1,
        YMm = 1,
        WidthMm = 4,
        HeightMm = 10,
        BarcodeModuleWidthMm = 0,
        QrQuietZoneModules = 10,
        ShowBarcodeText = false
    });

    var bad = new PrintService().ValidateRows(thin, new IReadOnlyDictionary<string, string>?[] { null });
    AssertEqual(false, bad.IsSuccess,
        "A frame that yields sub-2-dot modules from pure logical count must still fail preflight");
    AssertEqual(true, bad.Issues.Any(i =>
            i.Message.Contains("dot", StringComparison.OrdinalIgnoreCase)
            || i.Message.Contains("module", StringComparison.OrdinalIgnoreCase)),
        "Undersized legacy estimate must still report module/dot industrial risk");

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

static Task TestSnapPathMatrixSoftwareFixtures()
{
    // D1/DP-129 software fixture: every interaction path must share one
    // zoom-normalized acquire/release budget and the same semantic winner.
    foreach (var path in new[]
             {
                 SnapPathKind.SingleMove,
                 SnapPathKind.GroupMove,
                 SnapPathKind.Resize,
                 SnapPathKind.Draw
             })
    {
        var sameWinner = SnapPathMatrixContract.SameWinnerAcrossZooms(
            path,
            SnapPathMatrixContract.SoftwareZoomLadder,
            zoom =>
            {
                var acquireMm = SnapPathMatrixContract.AcquireToleranceMm(zoom);
                return new[]
                {
                    new SnapCandidate(0, acquireMm, acquireMm, Priority: 85, StableKey: "edge"),
                    new SnapCandidate(0, acquireMm * 0.25, acquireMm * 0.25, Priority: 40, StableKey: "grid")
                };
            },
            proposedPositionMm: 0);
        AssertEqual(true, sameWinner,
            $"{path} must keep the same semantic winner across the software zoom ladder");
    }

    var hysteresis = new SnapHysteresisState();
    var bypassed = SnapPathMatrixContract.Resolve(
        SnapPathKind.Resize,
        zoom: 1,
        candidates: new[] { new SnapCandidate(1, 1, 0, Priority: 100, StableKey: "would-win") },
        hysteresis,
        proposedPositionMm: 1,
        bypassSnap: true);
    AssertEqual(true, bypassed.Bypassed, "Alt/typed-dimension bypass must skip snap");
    AssertEqual(false, bypassed.Snapped, "Bypass must not commit a snap target");
    AssertEqual(true, hysteresis.LockedTarget is null, "Bypass must leave hysteresis idle");
    return Task.CompletedTask;
}

static Task TestDispatchRevalidationBlocksDrift()
{
    // IR-131/D4 software fault fixture: a ticket/DPI/media/imageable change
    // between preparation and dispatch must name the field and forbid spool
    // submission.  This is not physical-printer evidence.
    var prepared = new EffectiveOutputContract
    {
        PrinterName = "Industrial-A",
        RequestedTicketHash = "req",
        EffectiveTicketHash = "eff",
        DpiX = 203,
        DpiY = 203,
        LabelWidthMm = 50,
        LabelHeightMm = 30,
        GapMm = 2,
        PrintableWidthDots = 400,
        PrintableHeightDots = 240,
        PrintableAreaVerified = true
    };
    var dpiDrift = prepared with { DpiX = 300 };
    var blocked = DispatchRevalidationContract.Evaluate(
        "doc-a",
        prepared,
        preparedTicketVerified: true,
        "doc-a",
        dpiDrift,
        finalTicketVerified: true,
        expectedOutputContractHash: prepared.Fingerprint);
    AssertEqual(false, blocked.SubmissionAllowed, "DPI drift must block PrintDocument");
    AssertEqual(true, blocked.ChangedFields.Contains("dpi"), "Diagnostic must name dpi");
    AssertEqual(true, blocked.Diagnostic.Contains("no label was submitted", StringComparison.OrdinalIgnoreCase),
        "Operator message must state that nothing was submitted");

    var stable = DispatchRevalidationContract.EvaluateFingerprints(
        "doc-a",
        prepared.Fingerprint,
        preparedTicketVerified: true,
        "doc-a",
        prepared.Fingerprint,
        finalTicketVerified: true,
        expectedOutputContractHash: prepared.Fingerprint);
    AssertEqual(true, stable.SubmissionAllowed, "Stable fingerprints must still authorize dispatch");

    // PrintService last-mile path must prefer field-level Evaluate.
    var serviceSource = File.ReadAllText(
        Path.Combine(FindRepositoryRoot(), "src", "ANLAbel.Printing", "PrinterProfiles", "PrintService.cs"));
    AssertEqual(true, serviceSource.Contains("DispatchRevalidationContract.Evaluate(", StringComparison.Ordinal),
        "PrintService must revalidate through field-level DispatchRevalidationContract.Evaluate before PrintDocument");
    AssertEqual(true, serviceSource.Contains("WithEffectiveOutput(contract)", StringComparison.Ordinal),
        "CreateEffectivePlan must retain EffectiveOutputContract for named drift diagnostics");
    return Task.CompletedTask;
}

static Task TestPrintSupportEvidenceRedaction()
{
    const string secret = "CUSTOMER-PRIVATE-LOT-42";
    var bundle = PrintSupportEvidenceContract.Build(
        jobId: "job-support-1",
        queueName: "Godex",
        spoolJobId: "11",
        documentHash: "doc",
        sceneHash: "scene",
        outputContractHash: "out",
        manifestFingerprint: "man",
        textResourceFingerprint: "text",
        imageRasterFingerprint: "img",
        thermalGoldenFingerprint: "therm",
        outcome: "SpoolAccepted",
        physicalOutputVerified: false,
        metadata: new[]
        {
            new KeyValuePair<string, string?>("payload", secret),
            new KeyValuePair<string, string?>("rowCount", "3")
        },
        lifecycleStates: new[] { "Created", "SpoolAccepted" });

    AssertEqual(false, PrintSupportEvidenceContract.ContainsRawPayloadLeak(bundle, secret),
        "Support evidence must not embed raw label payloads");
    AssertEqual(false, bundle.PhysicalOutputVerified,
        "Support evidence must not claim physical verification from spool acceptance");
    AssertEqual(true, bundle.EvidenceFingerprint.Length == 64,
        "Support evidence fingerprint must be a SHA-256 hex digest");
    return Task.CompletedTask;
}

static async Task TestAsyncRelayCommandRejectsReentry()
{
    // D7/IR-134 software fixture: a second click while an async command is
    // running must not start a duplicate import/print operation.
    var started = 0;
    var completed = 0;
    var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var command = new AsyncRelayCommand(async () =>
    {
        Interlocked.Increment(ref started);
        await gate.Task.ConfigureAwait(false);
        Interlocked.Increment(ref completed);
    });

    var first = command.ExecuteAsync(null);
    await command.ExecuteAsync(null);
    AssertEqual(1, started, "Only the first async command invocation may start work");
    AssertEqual(false, command.CanExecute(null), "Command must report busy while executing");

    gate.SetResult();
    await first;
    AssertEqual(1, completed, "The single started operation must complete exactly once");
    AssertEqual(true, command.CanExecute(null), "Command must become available after completion");
}

static Task TestDesignerCanvasRoutesSnapThroughPathMatrix()
{
    // D1/DP-129 product path: LabelDesignerCanvas must call the shared matrix
    // helpers, not SnapCandidateSelector.Choose directly, for interactive snap.
    var canvasSource = File.ReadAllText(
        Path.Combine(FindRepositoryRoot(), "src", "ANLAbel.App", "Controls", "LabelDesignerCanvas.cs"));
    AssertEqual(true, canvasSource.Contains("SnapPathMatrixContract.Choose", StringComparison.Ordinal),
        "Canvas must rank snap candidates through SnapPathMatrixContract.Choose");
    AssertEqual(true, canvasSource.Contains("SnapPathMatrixContract.ApplyHysteresis", StringComparison.Ordinal),
        "Canvas must apply release hysteresis through SnapPathMatrixContract.ApplyHysteresis");
    AssertEqual(false, canvasSource.Contains("SnapCandidateSelector.Choose", StringComparison.Ordinal),
        "Canvas must not bypass the matrix by calling SnapCandidateSelector.Choose directly");
    AssertEqual(true, canvasSource.Contains("SnapPathKind.SingleMove", StringComparison.Ordinal),
        "Single-move path must identify itself to the matrix");
    AssertEqual(true, canvasSource.Contains("SnapPathKind.GroupMove", StringComparison.Ordinal),
        "Group-move path must identify itself to the matrix");
    AssertEqual(true, canvasSource.Contains("SnapPathKind.Resize", StringComparison.Ordinal),
        "Resize path must identify itself to the matrix");
    AssertEqual(true, canvasSource.Contains("SnapPathKind.Draw", StringComparison.Ordinal),
        "Draw path must identify itself to the matrix");
    return Task.CompletedTask;
}

static Task TestPrintServiceAttachesSupportEvidence()
{
    // IR-130 product path: AttachSupportEvidence runs on the shipped print
    // result and redacts payloads while keeping correlation identities.
    var plan = new PrintRenderPlan
    {
        DocumentHash = "doc-support",
        SceneHash = "scene-support",
        OutputContractHash = "out-support",
        TextResourceFingerprint = "text-support",
        ImageRasterFingerprint = "img-support",
        SceneCompilationVerified = true
    };
    var result = new PrintJobResult(
        PrintJobOutcome.SpoolAccepted,
        "Industrial-Queue",
        "Batch A",
        3,
        DpiX: 203,
        DpiY: 203,
        SpoolJobId: 42,
        OutputContractHash: plan.OutputContractHash,
        DocumentHash: plan.DocumentHash,
        SceneHash: plan.SceneHash,
        SceneCompilationVerified: true,
        TextResourceFingerprint: plan.TextResourceFingerprint,
        ImageRasterFingerprint: plan.ImageRasterFingerprint,
        ManifestFingerprint: "manifest-support");

    var decorated = PrintService.AttachSupportEvidence(result, plan, durableJobIdHint: "Batch A");
    AssertEqual(true, decorated.SupportEvidenceFingerprint.Length == 64,
        "Shipped print path must attach a SHA-256 support evidence fingerprint");
    AssertEqual(true, decorated.SupportEvidenceJson.Contains("Industrial-Queue", StringComparison.Ordinal),
        "Support JSON must retain the queue name");
    AssertEqual(true, decorated.SupportEvidenceJson.Contains("manifest-support", StringComparison.Ordinal),
        "Support JSON must retain the manifest fingerprint");
    AssertEqual(false, decorated.SupportEvidenceJson.Contains("CUSTOMER-SECRET", StringComparison.Ordinal),
        "Support JSON must not invent raw payloads");
    AssertEqual(false, decorated.IsPhysicalCompletionVerified,
        "Spool acceptance must not claim physical verification in support evidence");

    var serviceSource = File.ReadAllText(
        Path.Combine(FindRepositoryRoot(), "src", "ANLAbel.Printing", "PrinterProfiles", "PrintService.cs"));
    AssertEqual(true, serviceSource.Contains("AttachSupportEvidence(result, plan", StringComparison.Ordinal),
        "PrintService must call AttachSupportEvidence on the shipped spool-accept path");
    return Task.CompletedTask;
}

static Task TestDispatchRevalidationUsesFullEffectiveOutput()
{
    // IR-131 product path: CreateEffectivePlan retains EffectiveOutput and
    // RevalidateDispatchPlan prefers DispatchRevalidationContract.Evaluate.
    var serviceSource = File.ReadAllText(
        Path.Combine(FindRepositoryRoot(), "src", "ANLAbel.Printing", "PrinterProfiles", "PrintService.cs"));
    AssertEqual(true, serviceSource.Contains("WithEffectiveOutput(contract)", StringComparison.Ordinal),
        "CreateEffectivePlan must retain the full EffectiveOutputContract on the plan");
    AssertEqual(true, serviceSource.Contains("DispatchRevalidationContract.Evaluate(", StringComparison.Ordinal),
        "RevalidateDispatchPlan must call field-level Evaluate when contracts are present");
    AssertEqual(true, serviceSource.Contains("preparedPlan.EffectiveOutput is not null", StringComparison.Ordinal),
        "Revalidation must branch on retained EffectiveOutput objects");

    var prepared = new EffectiveOutputContract
    {
        PrinterName = "Q",
        RequestedTicketHash = "r",
        EffectiveTicketHash = "e",
        DpiX = 203,
        DpiY = 203,
        LabelWidthMm = 50,
        LabelHeightMm = 30,
        PrintableWidthDots = 400,
        PrintableHeightDots = 240,
        PrintableAreaVerified = true
    };
    var drifted = prepared with { DpiY = 300 };
    var preparedPlan = new PrintRenderPlan
    {
        DocumentHash = "doc-1",
        OutputContractHash = prepared.Fingerprint,
        OutputContractTicketVerified = true,
        EffectiveOutput = prepared
    };
    var finalPlan = new PrintRenderPlan
    {
        DocumentHash = "doc-1",
        OutputContractHash = drifted.Fingerprint,
        OutputContractTicketVerified = true,
        EffectiveOutput = drifted
    };
    var decision = DispatchRevalidationContract.Evaluate(
        preparedPlan.DocumentHash,
        preparedPlan.EffectiveOutput,
        preparedPlan.OutputContractTicketVerified,
        finalPlan.DocumentHash,
        finalPlan.EffectiveOutput,
        finalPlan.OutputContractTicketVerified,
        preparedPlan.OutputContractHash);
    AssertEqual(false, decision.SubmissionAllowed, "Field-level revalidation must block DPI drift");
    AssertEqual(true, decision.ChangedFields.Contains("dpi"), "Diagnostic must name dpi when only DPI changes");
    return Task.CompletedTask;
}

static async Task TestPrintCenterExportsSupportEvidence()
{
    // IR-130 operator path: Print Center builds support evidence from a durable
    // recovery candidate and writes redacted JSON without raw label payloads.
    const string secretLot = "CUSTOMER-PRIVATE-LOT-7788";
    var longReason = new string('x', 140) + secretLot;
    var candidate = new PrintJobRecoveryCandidate(
        JobId: "job-export-1",
        State: PrintJobLifecycleState.SpoolAccepted,
        Action: PrintJobRecoveryAction.OperatorDecision,
        LastEventUtc: DateTimeOffset.UtcNow,
        PrinterName: "Zebra-ZTL",
        SpoolJobId: 88,
        QueueState: "Printing",
        DocumentHash: "doc-e",
        SceneHash: "scene-e",
        OutputContractHash: "out-e",
        Reason: longReason,
        OperatorAction: PrintJobOperatorAction.None,
        RelatedJobId: "",
        Actor: "line-a",
        ManifestFingerprint: "man-e");

    var bundle = PrintCenterWindow.BuildSupportEvidence(candidate);
    AssertEqual(false, bundle.PhysicalOutputVerified, "Recovery export must not claim physical completion");
    AssertEqual("job-export-1", bundle.JobId, "Export must retain the durable job id");
    AssertEqual(true, bundle.EvidenceFingerprint.Length == 64, "Export fingerprint must be SHA-256 hex");
    AssertEqual(false, PrintSupportEvidenceContract.ContainsRawPayloadLeak(bundle, secretLot),
        "Long free-text reasons must be redacted before export");

    var directory = Path.Combine(Path.GetTempPath(), "anlabel-pc-export-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(directory);
    var path = Path.Combine(directory, "support.json");
    try
    {
        await PrintSupportEvidenceContract.WriteJsonAsync(bundle, path);
        AssertEqual(true, File.Exists(path), "Export must write the support JSON file");
        var json = await File.ReadAllTextAsync(path);
        AssertEqual(true, json.Contains("job-export-1", StringComparison.Ordinal), "Exported JSON must retain job id");
        AssertEqual(false, json.Contains(secretLot, StringComparison.Ordinal),
            "Exported JSON must not contain raw label-like free text from a long reason field");
        AssertEqual(true, json.Contains("[redacted-long-value]", StringComparison.Ordinal),
            "Long reason metadata must be replaced with the redacted-long-value marker");
    }
    finally
    {
        try { Directory.Delete(directory, true); } catch { }
    }

    var centerSource = File.ReadAllText(
        Path.Combine(FindRepositoryRoot(), "src", "ANLAbel.App", "PrintCenterWindow.xaml.cs"));
    AssertEqual(true, centerSource.Contains("BuildSupportEvidence", StringComparison.Ordinal),
        "Print Center must expose the durable-job support evidence builder");
    AssertEqual(true, centerSource.Contains("ExportSupportButton_Click", StringComparison.Ordinal),
        "Print Center must wire an export button click handler");
}

static Task TestGs1IndustrialAiSubset()
{
    var ok = BarcodeApplicationContract.ValidateData(
        BarcodeApplicationProfile.Gs1,
        BarcodeSymbology.Code128,
        "(01)09506000134352(3103)000250(10)BATCH1(91)LINE-7");
    AssertEqual(0, ok.Count, "Industrial GS1 AI subset must accept GTIN+weight+lot+company-internal");

    var badWeight = BarcodeApplicationContract.ValidateData(
        BarcodeApplicationProfile.Gs1,
        BarcodeSymbology.Code128,
        "(3103)25");
    AssertEqual(true, badWeight.Any(message => message.Contains("6 numeric", StringComparison.OrdinalIgnoreCase)),
        "Fixed measure AIs must require six numeric digits");

    var normalizedOk = BarcodeApplicationContract.TryNormalizeGs1Data(
        "(240)SKU-1(10)LOT-2",
        out var normalized,
        out var errors);
    AssertEqual(true, normalizedOk, "Variable industrial AIs must normalize");
    AssertEqual(0, errors.Count, "Valid industrial AI sequence must not report parse errors");
    AssertEqual(true, normalized.Contains(BarcodeApplicationContract.GroupSeparator),
        "Variable-length industrial fields must insert FNC1 group separators between AIs");
    return Task.CompletedTask;
}

static Task TestMainShellRegionsMatchNiceLabelMap()
{
    // Structural gate: shipped MainWindow chrome exposes the NiceLabel→ANLAbel
    // region AutomationIds from docs/NICELABEL_DESIGNER_SHELL_RESEARCH.md.
    Exception? failure = null;
    string[] found = Array.Empty<string>();
    var thread = new Thread(() =>
    {
        try
        {
            var window = new MainWindow();
            window.DataContext = new MainViewModel();
            // Force measure so the visual tree is realized without showing UI.
            window.Measure(new Size(1440, 900));
            window.Arrange(new Rect(0, 0, 1440, 900));
            window.UpdateLayout();

            var required = new[]
            {
                "Shell.QuickAccess",
                "Shell.Ribbon",
                "Shell.Toolbox",
                "Shell.Workspace",
                "Shell.Canvas",
                "Shell.Properties",
                "Shell.Status"
            };
            var hit = new List<string>();
            void Walk(DependencyObject? node)
            {
                if (node is null)
                {
                    return;
                }

                if (node is UIElement element)
                {
                    var id = System.Windows.Automation.AutomationProperties.GetAutomationId(element);
                    if (!string.IsNullOrWhiteSpace(id) && required.Contains(id, StringComparer.Ordinal))
                    {
                        hit.Add(id);
                    }
                }

                var count = VisualTreeHelper.GetChildrenCount(node);
                for (var i = 0; i < count; i++)
                {
                    Walk(VisualTreeHelper.GetChild(node, i));
                }

                if (node is FrameworkElement fe && fe.ContextMenu is not null)
                {
                    // no-op; keep walk on visual tree only
                }
            }

            Walk(window);
            // Content may live under ContentPresenter; also walk logical children of DockPanel root.
            if (window.Content is DependencyObject content)
            {
                Walk(content);
            }

            found = hit.Distinct(StringComparer.Ordinal).OrderBy(s => s, StringComparer.Ordinal).ToArray();
            foreach (var id in required)
            {
                AssertEqual(true, found.Contains(id, StringComparer.Ordinal),
                    $"Main shell must expose AutomationId '{id}' (NiceLabel region map). Found: {string.Join(", ", found)}");
            }

            // Properties header uses NiceLabel-aligned "Object Properties" label.
            var xamlPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "ANLAbel.App", "MainWindow.xaml");
            xamlPath = Path.GetFullPath(xamlPath);
            AssertEqual(true, File.Exists(xamlPath), $"MainWindow.xaml must be discoverable for shell chrome checks: {xamlPath}");
            var xaml = File.ReadAllText(xamlPath);
            AssertEqual(true, xaml.Contains("Object Properties", StringComparison.Ordinal),
                "Properties panel title must match NiceLabel Object Properties editor naming");
            AssertEqual(true, xaml.Contains("Shell.Status", StringComparison.Ordinal),
                "Status bar AutomationId must remain in shipped XAML");
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

static string[] RequiredHeaderAutomationIds() =>
[
    "Shell.QuickAccess",
    "Shell.QuickAccess.New",
    "Shell.QuickAccess.Open",
    "Shell.QuickAccess.Save",
    "Shell.QuickAccess.Undo",
    "Shell.QuickAccess.Redo",
    "Shell.QuickAccess.Revisions",
    "Shell.Ribbon",
    "Shell.Ribbon.Templates",
    "Shell.Ribbon.ImportExcel",
    "Shell.Ribbon.UpdateExcel",
    "Shell.Ribbon.PrinterSetup",
    "Shell.Ribbon.Preview",
    "Shell.Ribbon.PrintCurrent",
    "Shell.Ribbon.PrintAllRows",
    "Shell.Ribbon.PrintHistory",
    "Shell.Ribbon.ExportExcel",
    "Shell.Ribbon.TestPrint",
    "Shell.Ribbon.Panels",
    "Shell.Ribbon.SnapObjects",
    "Shell.Ribbon.SnapGrid",
    "Shell.Ribbon.DeleteSelection",
    "Shell.Ribbon.Help"
];

static Task TestDesignerHeaderCommandsAreUnique()
{
    var xamlPath = Path.GetFullPath(Path.Combine(FindRepositoryRoot(), "src", "ANLAbel.App", "MainWindow.xaml"));
    var xaml = File.ReadAllText(xamlPath);
    var ribbonStart = xaml.IndexOf("AutomationId=\"Shell.Ribbon\"", StringComparison.Ordinal);
    var statusStart = xaml.IndexOf("AutomationId=\"Shell.Status\"", StringComparison.Ordinal);
    AssertEqual(true, ribbonStart > 0 && statusStart > ribbonStart, "Ribbon and status markers must exist in shipped XAML");
    var ribbonXaml = xaml.Substring(ribbonStart, statusStart - ribbonStart);
    AssertEqual(false, ribbonXaml.Contains("zoom_minus.png", StringComparison.Ordinal),
        "Ribbon must not reuse the status zoom-minus glyph");
    AssertEqual(false, ribbonXaml.Contains("zoom_plus.png", StringComparison.Ordinal),
        "Ribbon must not reuse the status zoom-plus glyph");
    AssertEqual(false, ribbonXaml.Contains("ZoomOutCommand", StringComparison.Ordinal),
        "Zoom out is a status-bar command, not a ribbon command");
    AssertEqual(false, ribbonXaml.Contains("ZoomInCommand", StringComparison.Ordinal),
        "Zoom in is a status-bar command, not a ribbon command");
    AssertEqual(true, ribbonXaml.Contains("Icons/snap_objects.png", StringComparison.Ordinal),
        "Snap-to-objects must use its own header glyph");
    AssertEqual(true, ribbonXaml.Contains("Icons/snap_grid.png", StringComparison.Ordinal),
        "Snap-to-grid must use its own header glyph");
    AssertEqual(false, ribbonXaml.Contains("Icons/cursor_select.png", StringComparison.Ordinal),
        "Header must not reuse the select-cursor glyph for snap-to-objects");
    AssertEqual(false, ribbonXaml.Contains("Icons/table.png", StringComparison.Ordinal),
        "Header must not reuse the table glyph for snap-to-grid");

    var qatSlice = xaml.Substring(
        xaml.IndexOf("AutomationId=\"Shell.QuickAccess\"", StringComparison.Ordinal),
        ribbonStart - xaml.IndexOf("AutomationId=\"Shell.QuickAccess\"", StringComparison.Ordinal));
    var headerIcons = System.Text.RegularExpressions.Regex.Matches(qatSlice + ribbonXaml, @"Source=""(Icons/[^""]+)""")
        .Select(m => m.Groups[1].Value)
        .ToList();
    var duplicateIcons = headerIcons.GroupBy(v => v, StringComparer.OrdinalIgnoreCase)
        .Where(g => g.Count() > 1)
        .Select(g => g.Key)
        .ToArray();
    AssertEqual(0, duplicateIcons.Length,
        "Two header actions must not share one PNG: " + string.Join(", ", duplicateIcons));

    Exception? failure = null;
    var thread = new Thread(() =>
    {
        try
        {
            var window = new MainWindow();
            window.DataContext = new MainViewModel();
            window.Measure(new Size(1440, 900));
            window.Arrange(new Rect(0, 0, 1440, 900));
            window.UpdateLayout();
            var found = CollectAutomationIds(window);
            foreach (var id in RequiredHeaderAutomationIds())
            {
                AssertEqual(true, found.Contains(id, StringComparer.Ordinal),
                    $"Shipped header must expose AutomationId '{id}'. Found: {string.Join(", ", found)}");
            }

            AssertEqual(true, found.Contains("Shell.Status.Zoom", StringComparer.Ordinal),
                "Status bar remains the only zoom placement");
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

static Task TestDesignerShellLayoutAtTargetScales()
{
    var scratch = Path.Combine(FindRepositoryRoot(), "TestOutput", "header-scale");
    Directory.CreateDirectory(scratch);
    Exception? failure = null;
    var notes = new List<string>();
    var thread = new Thread(() =>
    {
        try
        {
            var window = new MainWindow();
            window.DataContext = new MainViewModel();
            var cases = new (string Name, double Width, double Height)[]
            {
                ("100pct-1024x600", 1024, 600),
                ("125pct-logical", 1024 / 1.25, 600 / 1.25),
                ("150pct-logical", 1024 / 1.50, 600 / 1.50)
            };
            foreach (var (name, width, height) in cases)
            {
                window.MinWidth = 1;
                window.MinHeight = 1;
                window.Width = width;
                window.Height = height;
                window.Measure(new Size(width, height));
                window.Arrange(new Rect(0, 0, width, height));
                if (window.Content is UIElement content)
                {
                    content.Measure(new Size(width, height));
                    content.Arrange(new Rect(0, 0, width, height));
                }

                window.UpdateLayout();
                var found = CollectAutomationIds(window);
                foreach (var id in RequiredHeaderAutomationIds())
                {
                    AssertEqual(true, found.Contains(id, StringComparer.Ordinal),
                        $"{name}: header AutomationId '{id}' must remain reachable at {width:0}x{height:0}");
                }

                AssertEqual(true, found.Contains("Shell.Status.Zoom", StringComparer.Ordinal),
                    $"{name}: status zoom must remain reachable");

                var qat = FindByAutomationId(window, "Shell.QuickAccess") as FrameworkElement;
                var ribbon = FindByAutomationId(window, "Shell.Ribbon") as FrameworkElement;
                AssertEqual(true, qat is not null && ribbon is not null, $"{name}: header bands must realize");
                var qatWidth = Math.Max(qat!.ActualWidth, qat.DesiredSize.Width);
                var qatHeight = Math.Max(qat.ActualHeight, qat.DesiredSize.Height);
                var ribbonWidth = Math.Max(ribbon!.ActualWidth, ribbon.DesiredSize.Width);
                var ribbonHeight = Math.Max(ribbon.ActualHeight, ribbon.DesiredSize.Height);
                AssertEqual(true, qatWidth > 0 && qatHeight > 0, $"{name}: Quick Access must have a non-zero arranged size");
                AssertEqual(true, ribbonWidth > 0 && ribbonHeight > 0, $"{name}: Ribbon must have a non-zero arranged size");
                AssertEqual(true, qatHeight + ribbonHeight < height,
                    $"{name}: header bands must leave room for the canvas at {width:0}x{height:0}");

                try
                {
                    var raster = new RenderTargetBitmap(
                        Math.Max(1, (int)Math.Ceiling(width)),
                        Math.Max(1, (int)Math.Ceiling(Math.Min(height, qat.ActualHeight + ribbon.ActualHeight + 8))),
                        96, 96, PixelFormats.Pbgra32);
                    raster.Render(window);
                    var file = Path.Combine(scratch, name + ".png");
                    using var stream = File.Create(file);
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(raster));
                    encoder.Save(stream);
                    notes.Add($"{name}: raster {file} {new FileInfo(file).Length} bytes");
                }
                catch (Exception ex)
                {
                    notes.Add($"{name}: offscreen raster unavailable: {ex.GetType().Name}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            failure = ex;
        }
    });
    thread.SetApartmentState(ApartmentState.STA);
    thread.Start();
    thread.Join();
    File.WriteAllLines(Path.Combine(scratch, "notes.txt"), notes);
    if (failure is not null)
    {
        throw failure;
    }

    return Task.CompletedTask;
}

static HashSet<string> CollectAutomationIds(DependencyObject root)
{
    var hit = new HashSet<string>(StringComparer.Ordinal);
    void Walk(DependencyObject? node)
    {
        if (node is null)
        {
            return;
        }

        if (node is UIElement element)
        {
            var id = System.Windows.Automation.AutomationProperties.GetAutomationId(element);
            if (!string.IsNullOrWhiteSpace(id))
            {
                hit.Add(id);
            }
        }

        var count = VisualTreeHelper.GetChildrenCount(node);
        for (var i = 0; i < count; i++)
        {
            Walk(VisualTreeHelper.GetChild(node, i));
        }
    }

    Walk(root);
    if (root is ContentControl contentControl && contentControl.Content is DependencyObject content)
    {
        Walk(content);
    }

    return hit;
}

static DependencyObject? FindByAutomationId(DependencyObject root, string automationId)
{
    DependencyObject? found = null;
    void Walk(DependencyObject? node)
    {
        if (node is null || found is not null)
        {
            return;
        }

        if (node is UIElement element &&
            string.Equals(System.Windows.Automation.AutomationProperties.GetAutomationId(element), automationId, StringComparison.Ordinal))
        {
            found = node;
            return;
        }

        var count = VisualTreeHelper.GetChildrenCount(node);
        for (var i = 0; i < count; i++)
        {
            Walk(VisualTreeHelper.GetChild(node, i));
        }
    }

    Walk(root);
    if (found is null && root is ContentControl contentControl && contentControl.Content is DependencyObject content)
    {
        Walk(content);
    }

    return found;
}

static Task TestMixedObjectCanvasSoak()
{
    // D2 software soak: 500 mixed objects, multi-select + key object, zoom and
    // resize-cancel must preserve selection identity without a physical printer.
    Exception? failure = null;
    var thread = new Thread(() =>
    {
        try
        {
            var template = new LabelTemplate
            {
                Name = "Soak",
                WidthMm = 200,
                HeightMm = 200
            };
            for (var i = 0; i < 500; i++)
            {
                var kind = i % 5;
                template.Objects.Add(kind switch
                {
                    0 => new LabelObject
                    {
                        Id = $"text-{i}",
                        Type = ObjectType.Text,
                        XMm = (i % 20) * 8,
                        YMm = (i / 20) * 4,
                        WidthMm = 6,
                        HeightMm = 3,
                        Text = $"T{i}"
                    },
                    1 => new LabelObject
                    {
                        Id = $"box-{i}",
                        Type = ObjectType.Rectangle,
                        XMm = (i % 20) * 8,
                        YMm = (i / 20) * 4,
                        WidthMm = 5,
                        HeightMm = 3
                    },
                    2 => new LabelObject
                    {
                        Id = $"line-{i}",
                        Type = ObjectType.Line,
                        XMm = (i % 20) * 8,
                        YMm = (i / 20) * 4,
                        WidthMm = 4,
                        HeightMm = 0,
                        LineEndXMm = (i % 20) * 8 + 4,
                        LineEndYMm = (i / 20) * 4
                    },
                    3 => new LabelObject
                    {
                        Id = $"qr-{i}",
                        Type = ObjectType.QRCode,
                        XMm = (i % 20) * 8,
                        YMm = (i / 20) * 4,
                        WidthMm = 4,
                        HeightMm = 4,
                        Text = $"Q{i}"
                    },
                    _ => new LabelObject
                    {
                        Id = $"ell-{i}",
                        Type = ObjectType.Ellipse,
                        XMm = (i % 20) * 8,
                        YMm = (i / 20) * 4,
                        WidthMm = 4,
                        HeightMm = 3
                    }
                });
            }

            var first = template.Objects[0];
            var second = template.Objects[1];
            var third = template.Objects[2];
            var canvas = new LabelDesignerCanvas { Template = template, Zoom = 1.0, SelectedObject = first };
            var selectedField = typeof(LabelDesignerCanvas).GetField(
                "_selectedObjects",
                BindingFlags.Instance | BindingFlags.NonPublic);
            AssertEqual(true, selectedField is not null, "Canvas must expose multi-selection for soak fixtures");
            var selected = (HashSet<LabelObject>)selectedField!.GetValue(canvas)!;
            selected.Add(second);
            selected.Add(third);
            AssertEqual(true, canvas.SetKeyObject(third), "Soak must allow an explicit key object inside the multi-selection");
            AssertEqual(3, canvas.SelectedObjectCount, "Soak must establish a three-object multi-selection");
            AssertEqual(true, ReferenceEquals(third, canvas.SelectedObject), "Key object must be the explicit selection primary");
            AssertEqual(500, template.Objects.Count, "Soak template must retain 500 mixed objects");

            foreach (var zoom in new[] { 0.25, 1.0, 4.0 })
            {
                canvas.Zoom = zoom;
                AssertEqual(3, canvas.SelectedObjectCount, $"Selection membership must survive zoom {zoom}");
                AssertEqual(true, ReferenceEquals(third, canvas.SelectedObject), $"Key object must survive zoom {zoom}");
                AssertEqual(500, template.Objects.Count, "Soak must not mutate object count during zoom");
            }

            // Canceled single-object resize on one selected peer must restore
            // geometry and leave multi-selection/key identity intact.
            var capture = typeof(LabelDesignerCanvas).GetMethod(
                "CaptureGroupResizeSnapshot",
                BindingFlags.Static | BindingFlags.NonPublic);
            var restore = typeof(LabelDesignerCanvas).GetMethod(
                "RestoreSingleResizeStart",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var startField = typeof(LabelDesignerCanvas).GetField(
                "_singleResizeStart",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var activeField = typeof(LabelDesignerCanvas).GetField(
                "_singleResizeActive",
                BindingFlags.Instance | BindingFlags.NonPublic);
            AssertEqual(true, capture is not null && restore is not null && startField is not null && activeField is not null,
                "Soak cancel path must use the shipped resize start-frame restore seam");

            var startX = first.XMm;
            var startY = first.YMm;
            var startW = first.WidthMm;
            var startH = first.HeightMm;
            startField!.SetValue(canvas, capture!.Invoke(null, new object[] { first }));
            activeField!.SetValue(canvas, true);
            first.XMm += 3;
            first.YMm += 2;
            first.WidthMm += 5;
            first.HeightMm += 4;
            restore!.Invoke(canvas, new object[] { first });
            AssertNear(startX, first.XMm, 0.0001, "Canceled soak resize must restore X");
            AssertNear(startY, first.YMm, 0.0001, "Canceled soak resize must restore Y");
            AssertNear(startW, first.WidthMm, 0.0001, "Canceled soak resize must restore width");
            AssertNear(startH, first.HeightMm, 0.0001, "Canceled soak resize must restore height");
            AssertEqual(3, canvas.SelectedObjectCount, "Cancel must not collapse multi-selection on a 500-object canvas");
            AssertEqual(true, ReferenceEquals(third, canvas.SelectedObject), "Cancel must keep the key object on a 500-object canvas");
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
    public double LastWidthMm { get; private set; }
    public double LastHeightMm { get; private set; }
    public BarcodeVectorData? VectorData { get; init; }

    public BarcodePixelImage RenderBarcode(string data, BarcodeType type, double widthMm, double heightMm, int dpi, BarcodeRenderOptions? options = null)
    {
        LastDpi = dpi;
        LastWidthMm = widthMm;
        LastHeightMm = heightMm;
        return new BarcodePixelImage(1, 1, new byte[] { 0, 0, 0, 255 });
    }

    public bool ValidateData(string data, BarcodeType type) => true;

    public string GetBarcodeInfo(string data, BarcodeType type) => string.Empty;

    public BarcodeVectorData? RenderBarcodeVector(string data, BarcodeType type, double widthMm, double heightMm, int dpi, BarcodeRenderOptions? options = null)
    {
        LastDpi = dpi;
        LastWidthMm = widthMm;
        LastHeightMm = heightMm;
        return VectorData;
    }

    /// <summary>When set, pure logical module count for SizedFromX (not scaled pixel width).</summary>
    public int? LogicalModuleCount { get; init; }

    public int? CountLinearModules(string data, BarcodeType type, BarcodeRenderOptions? options = null)
        => LogicalModuleCount
           ?? (VectorData is { WidthModules: > 0 } ? VectorData.WidthModules : 11);
}

sealed class InlineProgress<T>(Action<T> callback) : IProgress<T>
{
    public void Report(T value) => callback(value);
}

sealed class SequenceSpoolReader(params SpoolJobObservation[] observations) : ISpoolJobStatusReader
{
    private readonly Queue<SpoolJobObservation> _queue = new(observations);
    private SpoolJobObservation _last = observations.LastOrDefault()
        ?? new SpoolJobObservation("", 0, SpoolJobState.Unknown, IsTerminal: true);

    public ValueTask<SpoolJobObservation> ReadAsync(
        string printerName,
        int spoolJobId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_queue.Count > 0)
        {
            _last = _queue.Dequeue();
        }

        return ValueTask.FromResult(_last);
    }
}

sealed class MissingPrinterQueueLookup : IPrinterQueueLookup
{
    public PrinterQueueLookupResult Resolve(string printerName)
    {
        return PrinterQueueLookupResult.Missing(
            printerName,
            "The saved queue is no longer installed.");
    }
}
