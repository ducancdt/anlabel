using ANLAbel.App.Services;
using ANLAbel.Core.Printing;
using ANLAbel.Data.Excel;
using ANLAbel.Printing.PrinterProfiles;

/// <summary>
/// Audit 2026-10-13: printer, print-result, template-file and data-link texts
/// the operator sees are Vietnamese. Core messages stay English for logs and
/// evidence; each rule is anchored to its core message so a rewording fails
/// here instead of silently falling back to a generic text.
/// </summary>
internal static class OperatorLanguageRegression
{
    public static async Task FailuresAndStatusesAreExplainedInVietnamese()
    {
        // Printer chip and startup status: nothing was printed yet.
        var none = PrintFailureExplainer.ExplainQueueStatus("No printer queue is selected. Choose a verified industrial printer before printing.");
        Require(!none.IsGeneric && none.Problem == "Chưa chọn máy in cho mẫu này.", $"Startup printer warning: {none.Problem}");
        var offline = PrintFailureExplainer.ExplainQueueStatus("The printer queue reports offline. Resolve the queue/device condition and prepare again; no label was submitted.");
        Require(offline.Problem.Contains("ngoại tuyến", StringComparison.Ordinal)
                && !offline.Problem.Contains("Chưa có tem nào được gửi", StringComparison.Ordinal),
            $"A queue warning before printing must not talk about a label not sent: {offline.Problem}");
        Require(PrintFailureExplainer.ExplainQueueStatus("Windows could not open the saved queue: Access denied").Problem.StartsWith("Windows không mở được", StringComparison.Ordinal),
            "A queue that cannot be opened must be explained.");

        // Queue observation after a print.
        var printing = new SpoolJobMonitorResult(new SpoolJobObservation("P", 12, SpoolJobState.Printing, "Driver reports printing."), 2, TimeSpan.FromSeconds(1), TimedOut: false);
        Require(PrintStatusText.Queue(printing) == "Lệnh hàng đợi #12: đang in. Chưa xác minh tem đã ra.", $"Queue status: {PrintStatusText.Queue(printing)}");
        var slow = printing with { TimedOut = true, Elapsed = TimeSpan.FromSeconds(3) };
        Require(PrintStatusText.Queue(slow).StartsWith("Đã chờ 3 giây", StringComparison.Ordinal) && PrintStatusText.Queue(slow).Contains("không tự in lại", StringComparison.Ordinal),
            $"A queue that did not answer in time: {PrintStatusText.Queue(slow)}");

        // Job result with a technical reason.
        var failed = new PrintJobResult(PrintJobOutcome.Failed, "P", "Current row", 0, "The printer queue reports offline.");
        Require(PrintStatusText.Result(failed).StartsWith("In thất bại: Máy in đang ngoại tuyến", StringComparison.Ordinal), $"Failed result: {PrintStatusText.Result(failed)}");
        Require(PrintStatusText.Result(new PrintJobResult(PrintJobOutcome.Cancelled, "P", "Current row", 0)) == "Đã hủy in.", "A cancelled job keeps its text.");

        // Template files.
        Require(TemplateFailureExplainer.Explain(new InvalidDataException("The template file contains malformed JSON.")).Problem == "Tệp mẫu bị hỏng hoặc không đọc được.",
            "A corrupt template must be explained.");
        Require(TemplateFailureExplainer.Explain(new InvalidDataException("The template uses schema version 9, but this build supports up to 2. Update ANLAbel before opening it.")).Problem.Contains("mới hơn", StringComparison.Ordinal),
            "A template from a newer ANLAbel must be explained.");
        var locked = TemplateFailureExplainer.Explain(new IOException("The process cannot access the file.", unchecked((int)0x80070020)));
        Require(locked.Problem.StartsWith("Tệp mẫu đang bị chương trình khác", StringComparison.Ordinal), $"A locked template: {locked.Problem}");
        var withBackup = new InvalidDataException("Không mở được tệp mẫu.");
        withBackup.Data[TemplateFailureExplainer.BackupPathKey] = "C:/Tem/a.anlabel.bak";
        withBackup.Data[TemplateFailureExplainer.PrimaryErrorKey] = "The template file contains malformed JSON.";
        var backup = TemplateFailureExplainer.Explain(withBackup);
        Require(backup.Problem.Contains("C:/Tem/a.anlabel.bak", StringComparison.Ordinal) && backup.Problem.StartsWith("Tệp mẫu bị hỏng", StringComparison.Ordinal),
            $"An open with a readable backup must name the backup: {backup.Problem}");
        var unknown = new InvalidOperationException("Something unexpected");
        Require(OperationFailureExplainer.StatusLine(unknown).EndsWith("Chi tiết kỹ thuật: Something unexpected", StringComparison.Ordinal),
            "An unrecognised failure keeps its technical text as a labelled detail.");

        // Data link check.
        var missing = await new ExcelDataService().CheckConnectionAsync(Path.Combine(Path.GetTempPath(), "anlabel-missing-" + Guid.NewGuid().ToString("N") + ".xlsx"), "Sheet1");
        Require(!missing.Ok && missing.Failure is not null, "A missing data file must fail the link check.");
        Require(OperationFailureExplainer.StatusLine(missing.Failure!).StartsWith("Không tìm thấy tệp dữ liệu", StringComparison.Ordinal),
            $"A missing data file: {OperationFailureExplainer.StatusLine(missing.Failure!)}");

        // Designer panels: type names, option lists, formula and barcode texts.
        Require(DisplayNames.ForObjectType(ANLAbel.Core.Enums.ObjectType.Image) == "Hình ảnh"
                && DisplayNames.For(ANLAbel.Core.Enums.ObjectType.TextBox) == "Ô chữ",
            "Object types must use the toolbox names.");
        Require(DisplayNames.For(ANLAbel.Core.Enums.OutlineStyle.Dash) == "Gạch"
                && DisplayNames.For(ANLAbel.Core.Enums.ImageRasterMode.MonochromeOrderedDither) == "Đơn sắc hòa sắc"
                && DisplayNames.For(ANLAbel.Core.Enums.BarcodeHriPlacement.Below) == "Dưới",
            "Option lists must show Vietnamese names.");
        Require(FormulaErrorText.Translate("Field 'Qty' was not found.") == "Không tìm thấy trường “Qty”.",
            "Formula errors must be Vietnamese.");
        Require(FormulaErrorText.Translate("Transform 'Lot2': Unknown function 'FOO'.") == "Trường chuyển đổi “Lot2”: Hàm “FOO” không được hỗ trợ.",
            "Transform errors must be Vietnamese, including the inner formula error.");
        Require(FormulaErrorText.Translate("Some new engine message.") == "Some new engine message.",
            "An unknown formula message must be shown unchanged, not hidden.");
        Require(PreflightIssueText.Sentence("HRI text is 32 mm wide but the barcode frame is only 20 mm. Increase the frame or reduce the HRI font size.")
                == "Dòng chữ dưới mã rộng 32 mm nhưng khung mã chỉ 20 mm; nới khung hoặc giảm cỡ chữ.",
            "Barcode HRI layout problems must be Vietnamese.");
        Require(ANLAbel.Core.Barcode.QrOptionLists.SizingModes[0].DisplayName == "Tự động theo dữ liệu"
                && ANLAbel.Core.Barcode.QrOptionLists.Versions[0].DisplayName.StartsWith("Phiên bản 1 = 21 × 21 mô-đun", StringComparison.Ordinal),
            "QR option lists must be Vietnamese.");

        // Print Preview and Printer Setup.
        Require(ANLAbel.App.Services.MediaCompatibilityText.Describe(new PrinterMediaResolution(PrinterMediaResolutionStatus.DriverAutomatic, null,
                    "Physical page sizing will be resolved by the printer driver at dispatch time.")) == "Trình điều khiển sẽ đặt khổ trang theo kích thước tem khi in.",
            "The compatibility card must be Vietnamese.");
        Require(ANLAbel.App.Services.MediaCompatibilityText.Describe(new PrinterMediaResolution(PrinterMediaResolutionStatus.Compatible, null,
                    "Driver media '4x2 in' matches the requested physical size.")).Contains("“4x2 in”", StringComparison.Ordinal),
            "A matching driver media keeps its name in the Vietnamese text.");
        Require(ANLAbel.App.Services.MediaCompatibilityText.Describe(new PrinterMediaResolution(PrinterMediaResolutionStatus.Unknown, null, "A new resolver text."))
                .StartsWith("Chưa xác định được", StringComparison.Ordinal),
            "An unknown resolver message falls back to a Vietnamese status text.");
        Require(PreflightIssueText.Message(PreviewInitializationTrust.SceneBlockMessage("scene hash missing")).StartsWith("thiết kế tem có lỗi", StringComparison.Ordinal),
            "The preview's invalid-design stop must be Vietnamese.");
        Require(new PrinterInfo { Name = "Zebra", IsDefault = true }.DisplayName == "Zebra (mặc định của Windows)",
            "The Windows default printer must be marked in Vietnamese.");

        var root = FindRepositoryRoot();
        var setup = File.ReadAllText(Path.Combine(root, "src", "ANLAbel.App", "PrinterSetupWindow.xaml.cs"));
        Require(!setup.Contains("Driver evidence:", StringComparison.Ordinal)
                && !setup.Contains("driver media option(s)", StringComparison.Ordinal)
                && setup.Contains("var orientLabel = LandscapeRadio?.IsChecked == true ? \"Ngang\" : \"Dọc\";", StringComparison.Ordinal),
            "Printer Setup status lines must be Vietnamese.");
        var window = File.ReadAllText(Path.Combine(root, "src", "ANLAbel.App", "MainWindow.xaml"));
        foreach (var list in new[] { "FillStyles", "OutlineStyles", "BarcodeApplicationProfiles", "BarcodeHriPlacementOptions", "BarcodeCheckDigitPolicyOptions", "Code39WideNarrowRatioOptions", "BearerBarStyleOptions", "ImageRasterModes" })
        {
            Require(window.Contains($"ItemsSource=\"{{Binding {list}}}\" ItemTemplate=\"{{StaticResource DisplayNameItem}}\"", StringComparison.Ordinal),
                $"The {list} drop-down must show Vietnamese names, not enum names.");
        }

        Require(window.Contains("Text=\"{Binding Type, Converter={StaticResource DisplayName}}\"", StringComparison.Ordinal)
                && !window.Contains("StringFormat=Zoom:", StringComparison.Ordinal),
            "The layer list and status bar must not show English names.");

        // Each rule anchors on a core message.
        var sources = string.Concat(new[] { "ANLAbel.Core", "ANLAbel.Data", "ANLAbel.Printing", "ANLAbel.Project", "ANLAbel.App" }
            .SelectMany(project => Directory.EnumerateFiles(Path.Combine(root, "src", project), "*.cs", SearchOption.AllDirectories))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                           && !path.EndsWith("PrintFailureExplainer.cs", StringComparison.Ordinal)
                           && !path.EndsWith("TemplateFailureExplainer.cs", StringComparison.Ordinal)
                           && !path.EndsWith("FormulaErrorText.cs", StringComparison.Ordinal)
                           && !path.EndsWith("PreflightIssueText.cs", StringComparison.Ordinal)
                           && !path.EndsWith("MediaCompatibilityText.cs", StringComparison.Ordinal))
            .Select(File.ReadAllText));
        string[] anchors =
        [
            "No printer queue is selected",
            "No verified printer queue is selected",
            "No printer queue was selected",
            "The requested Windows printer queue is unavailable",
            "Windows resolved the requested queue to '",
            "Windows could not open the saved queue",
            "Printer queue lookup failed",
            "The template uses schema version ",
            "The existing template cannot be overwritten because it uses an unsupported schema",
            "The template file format marker is missing or unsupported",
            "The template and its recovery backup are both invalid",
            "The template file contains malformed JSON",
            "The template file root must be a JSON object",
            "The template schemaVersion is invalid",
            "The template schemaVersion is missing",
            "The versioned template envelope",
            "The versioned template payload is invalid",
            "The legacy template payload is invalid",
            "Formula is empty.",
            "Unexpected token '",
            "Expected expression but reached end of formula.",
            "Expected '(' after function name '",
            "Expected ',' or ')' after argument in function '",
            "Expected ')' to close function '",
            "Expected string literal.",
            "Unterminated escape sequence in string literal.",
            "Unterminated string literal.",
            "Unknown function '",
            "FIELD expects exactly one argument.",
            "FIELD field name is empty.",
            "' was not found.",
            "A transform output field name is required.",
            "' conflicts with a source field.",
            "' is defined more than once.",
            "Transform dependency cycle detected: ",
            "HRI text is ",
            "Barcode frame is too short for the HRI strip",
            "HRI text has no measurable width or height",
            "Barcode frame must have positive finite dimensions before HRI",
            "Unsupported HRI placement '",
            "Current driver preferences own the print ticket, but driver configured page ",
            "Current driver preferences own the print ticket (effective page ",
            "Current driver preferences own the print ticket. Physical page sizing",
            "Physical page sizing will be resolved by the printer driver at dispatch time",
            "The requested physical media dimensions are invalid",
            "mm does not contain the authored label ",
            "The requested physical size is within the custom-media range",
            "The driver claims custom-media support without usable size limits",
            "The requested physical size is outside the driver custom-media range ",
            "Printer media capabilities are incomplete",
            "No same-orientation driver media matches the requested physical size",
            "' matches the requested physical size.",
            "' matches the requested physical size but is a software stock ",
            "The selected printer queue is unavailable",
            "Printing is blocked because the label design is invalid (",
        ];
        foreach (var anchor in anchors)
        {
            Require(sources.Contains(anchor, StringComparison.Ordinal), $"Core message changed; update the Vietnamese explanation: {anchor}");
        }
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ANLAbel.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("Could not locate the repository root.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
