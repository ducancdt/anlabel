/// <summary>
/// Plan 13 GĐ5 source contracts for print-side safety that cannot be exercised
/// without a real spooler. Each method is one named gate in Program.cs.
/// </summary>
internal static class PrintSafetyRegression
{
    public static Task CalibrationPrintUsesAnIsolatedCopy()
    {
        var method = Method(Path.Combine("src", "ANLAbel.App", "PrinterSetupWindow.xaml.cs"), "private async void PrintCalibration_Click(");
        Require(method.Contains("PrintSessionTemplateContract.Capture(_workingTemplate)", StringComparison.Ordinal),
            "The calibration print must run on an isolated copy of the open label.");
        Require(!method.Contains("_workingTemplate.PrinterProfile.", StringComparison.Ordinal),
            "The calibration print must not write printer settings into the open label before OK (Cancel would keep them).");
        return Task.CompletedTask;
    }

    public static Task QuickPrintUsesOneSnapshotAndAsyncContract()
    {
        var method = Method(Path.Combine("src", "ANLAbel.App", "ViewModels", "MainViewModel.cs"), "private async Task<TrackedPrintResult> DispatchTrackedPrintAsync(");
        Require(method.Contains("PrintSessionTemplateContract.Capture(Template)", StringComparison.Ordinal),
            "Quick print must snapshot the label once and use it for contract, preflight, manifest and dispatch.");
        Require(method.Contains("CreateEffectivePlanAsync(printTemplate", StringComparison.Ordinal)
                && method.Contains("ValidateRowsAsync(printTemplate", StringComparison.Ordinal),
            "Quick print must resolve the driver contract and run preflight off the UI thread.");
        Require(!method.Contains("CreateEffectivePlan(Template", StringComparison.Ordinal)
                && !method.Contains("ValidateRows(Template", StringComparison.Ordinal),
            "Quick print must not call the blocking contract/preflight overloads on the live label.");
        var dispatchCall = method.IndexOf("PrintRowsWithResultAsync(", StringComparison.Ordinal);
        Require(dispatchCall > 0 && method.IndexOf("printTemplate", dispatchCall, StringComparison.Ordinal) - dispatchCall < 80,
            "Quick print must dispatch the same snapshot that was validated.");
        return Task.CompletedTask;
    }

    public static Task FailureAfterSpoolAcceptanceIsUnknownNotFailed()
    {
        var quick = Method(Path.Combine("src", "ANLAbel.App", "ViewModels", "MainViewModel.cs"), "private async Task<TrackedPrintResult> DispatchTrackedPrintAsync(");
        Require(quick.Contains("dispatched?.IsAccepted == true", StringComparison.Ordinal)
                && quick.Contains("? PrintJobLifecycleState.Unknown", StringComparison.Ordinal),
            "Quick print: a failure after the spooler accepted the job must be recorded as Unknown (P07).");
        var preview = Method(Path.Combine("src", "ANLAbel.App", "PrintPreviewWindow.xaml.cs"), "private async void Print_Click(");
        Require(preview.Contains("dispatchedPrintResult?.IsAccepted == true", StringComparison.Ordinal)
                && preview.Contains("? PrintJobLifecycleState.Unknown", StringComparison.Ordinal),
            "Print Preview: a failure after the spooler accepted the job must be recorded as Unknown (P07).");
        Require(preview.Contains("Chưa rõ kết quả in", StringComparison.Ordinal),
            "Print Preview must tell the operator the outcome is uncertain instead of failed.");
        return Task.CompletedTask;
    }

    public static Task FailureWhileSendingIsUncertainNotFailed()
    {
        // P07: an exception from PrintDocument may come after pages reached the
        // spooler or printer. It must be recorded as Unknown (listed for review,
        // rows marked as sent), never as Failed with a "try again" message.
        var service = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src", "ANLAbel.Printing", "PrinterProfiles", "PrintService.cs")).Replace("\r\n", "\n");
        var sends = 0;
        for (var at = service.IndexOf("dialog.PrintDocument(", StringComparison.Ordinal); at >= 0; at = service.IndexOf("dialog.PrintDocument(", at + 1, StringComparison.Ordinal))
        {
            sends++;
            var before = service[Math.Max(0, at - 80)..at];
            var after = service.Substring(at, Math.Min(600, service.Length - at));
            Require(before.TrimEnd().EndsWith("{", StringComparison.Ordinal) && before.Contains("try", StringComparison.Ordinal)
                    && after.Contains("CreateUncertainDispatchResult(", StringComparison.Ordinal),
                "Every PrintDocument call must turn its exception into an uncertain (Unknown) result.");
        }

        Require(sends == 2, $"Expected the batch and calibration sends, found {sends}.");
        var explanation = ANLAbel.App.Services.PrintFailureExplainer.Explain(
            ANLAbel.Printing.PrinterProfiles.PrintService.UncertainDispatchPrefix + "; part of it may already be in the queue or printed (The RPC server is unavailable).");
        Require(!explanation.IsGeneric && explanation.Problem.Contains("Có thể một phần tem đã được in", StringComparison.Ordinal)
                && explanation.NextAction.Contains("không tự in lại", StringComparison.OrdinalIgnoreCase),
            "An uncertain send must tell the operator to reconcile, not to retry: " + explanation.Problem);

        var printClick = Method(Path.Combine("src", "ANLAbel.App", "PrintPreviewWindow.xaml.cs"), "private async void Print_Click(");
        var unknownAt = printClick.IndexOf("printResult.Outcome == PrintJobOutcome.Unknown", StringComparison.Ordinal);
        Require(unknownAt > 0 && printClick.IndexOf("row.IsPrinted = true", unknownAt, StringComparison.Ordinal) > unknownAt,
            "Print Preview must mark rows of an uncertain job as sent so a reprint warns about duplicates.");
        return Task.CompletedTask;
    }

    public static Task PrintStopsAreExplainedInPlainVietnamese()
    {
        // The first two are the XP-DT426B stops recorded in print-operations.jsonl.
        (string Message, string Expected)[] cases =
        [
            ("The printer output contract is missing verified driver-ticket evidence or changed after preview (DPI, media, margins or driver settings). Reopen preview and review the updated contract before printing.",
                "Thiết lập máy in đã khác lúc xem trước"),
            ($"{ANLAbel.Core.Printing.DispatchRevalidationContract.BlockDiagnosticPrefix} (effective-ticket, prepared-expectation). Reopen preview and review the updated contract; no label was submitted.",
                "(thiết lập driver thực tế, thiết lập đã chuẩn bị)"),
            ("The printer reports a paper/media jam.", "kẹt giấy"),
            ("The printer queue reports offline.", "ngoại tuyến"),
            ("Printer queue 'Microsoft Print to PDF' was inherited from Windows' default and was not explicitly selected. Choose a verified industrial queue or save it in Printer Setup before printing.",
                "“Microsoft Print to PDF”"),
            ("Printing is blocked. Resolve the following issues:\n- Row 3, Barcode1 (BarcodeCode128): Invalid Code 128 data. Value is empty.\n- ...and 2 more distinct issue(s).",
                "phát hiện 3 vấn đề trong mẫu hoặc dữ liệu. Ví dụ: “Barcode1” (mã vạch) — dữ liệu không hợp lệ cho mã Code 128."),
            ("Print was blocked because required job evidence could not be saved (disk full). Fix local log access and try again.",
                "nhật ký lệnh in"),
        ];
        foreach (var (message, expected) in cases)
        {
            var explanation = ANLAbel.App.Services.PrintFailureExplainer.Explain(message);
            Require(!explanation.IsGeneric, $"A known print stop fell back to the generic text: {message}");
            Require(explanation.Problem.Contains(expected, StringComparison.Ordinal),
                $"Expected '{expected}' in the explanation of '{message}', got '{explanation.Problem}'.");
            Require(explanation.Technical == message.Trim(), "The technical text must be kept unchanged for details and logs.");
            Require(!explanation.Problem.Contains("contract", StringComparison.OrdinalIgnoreCase)
                    && !explanation.Problem.Contains("ticket", StringComparison.OrdinalIgnoreCase),
                $"The operator text must not repeat technical English: {explanation.Problem}");
        }

        Require(ANLAbel.App.Services.PrintFailureExplainer.Explain(cases[0].Message).Problem.Contains("Chưa có tem nào được gửi.", StringComparison.Ordinal),
            "A stop before dispatch must say that no label was sent.");
        var unknown = ANLAbel.App.Services.PrintFailureExplainer.Explain("Unexpected driver text");
        Require(unknown.IsGeneric && unknown.Technical == "Unexpected driver text",
            "An unknown message must fall back to plain text and keep the technical message.");

        // Each rule anchors on a core message; rewording it must fail here, not silently fall back.
        var root = FindRepositoryRoot();
        var printSources = string.Concat(new[] { "ANLAbel.Core", "ANLAbel.Data", "ANLAbel.Printing", "ANLAbel.App" }
            .SelectMany(project => Directory.EnumerateFiles(Path.Combine(root, "src", project), "*.cs", SearchOption.AllDirectories))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                           && !path.EndsWith("PrintFailureExplainer.cs", StringComparison.Ordinal))
            .Select(File.ReadAllText));
        string[] anchors =
        [
            "The printer output contract is missing verified driver-ticket evidence or changed after preview",
            "Printing stopped because the printer output contract changed immediately before dispatch",
            "Printing stopped because the printer output contract could not be revalidated",
            "Printing stopped because the saved driver '",
            "Printing stopped because the current printer driver identity could not be",
            "Printer queue health could not be read",
            "The printer reports a paper/media jam",
            "The printer reports that paper/media is unavailable",
            "The printer queue reports offline",
            "The printer requires operator intervention",
            "The printer queue is paused",
            "The printer queue reports an error",
            "No printer queue was selected",
            "No printer queue was explicitly selected",
            "No printer is selected",
            "because no verified printer queue is selected",
            "' was inherited from Windows' default",
            "Requested printer '",
            "Windows resolved '",
            "The printer driver coerced media to ",
            "The printer driver returned an invalid effective DPI",
            "The printer driver did not return a valid effective PrintTicket",
            "A4/Letter/Legal/A3 office sheets are not thermal label stock",
            " is not a known industrial thermal resolution",
            "because the label design is invalid",
            "Printing is blocked because one or more selected rows have an invalid copies value",
            "Printing is blocked. Resolve the following issues:",
            "The preview plan no longer matches the current template",
            "' metadata does not match its embedded payload",
            "Print was blocked because required job evidence could not be saved",
            "dispatch was blocked because the newly compiled inputs do not match the approved manifest",
            "the current row/label counts do not match the approved manifest",
            "the current template, printer, DPI or data no longer matches the approved manifest",
        ];
        foreach (var anchor in anchors)
        {
            Require(printSources.Contains(anchor, StringComparison.Ordinal),
                $"Print stop message changed; update PrintFailureExplainer: {anchor}");
        }

        var printClick = Method(Path.Combine("src", "ANLAbel.App", "PrintPreviewWindow.xaml.cs"), "private async void Print_Click(");
        Require(printClick.Contains("PrintFailureExplainer.Explain(ex.Message)", StringComparison.Ordinal)
                && printClick.Contains("PrintFailureExplainer.Explain(printResult.ErrorMessage)", StringComparison.Ordinal),
            "Print Preview must explain print stops in plain language.");
        var viewModel = File.ReadAllText(Path.Combine(root, "src", "ANLAbel.App", "ViewModels", "MainViewModel.cs"));
        Require(!viewModel.Contains("$\"In thất bại: {ex.Message}\"", StringComparison.Ordinal)
                && viewModel.Contains("PrintFailureExplainer.Explain(ex.Message).StatusLine", StringComparison.Ordinal),
            "Quick print must explain print stops in plain language.");
        var report = Method(Path.Combine("src", "ANLAbel.App", "App.xaml.cs"), "private void ReportFailedOperation(");
        Require(report.Contains("OperationFailureExplainer.Explain(exception)", StringComparison.Ordinal),
            "Failed commands must be explained in plain language.");
        return Task.CompletedTask;
    }

    public static Task PreflightIssuesNameTheDataRowInVietnamese()
    {
        const string missing = "Missing field(s) in Excel data: Part. This object will print blank for this row.";
        var result = new ANLAbel.Printing.PrinterProfiles.PrintPreflightResult(
        [
            new ANLAbel.Printing.PrinterProfiles.PrintPreflightIssue(3, "SKU", "QRCode", missing),
            new ANLAbel.Printing.PrinterProfiles.PrintPreflightIssue(4, "SKU", "QRCode", missing),
            new ANLAbel.Printing.PrinterProfiles.PrintPreflightIssue(0, "Template", "LabelStock",
                "A4/Letter/Legal/A3 office sheets are not thermal label stock. Choose a catalog label size or enter the physical die size in millimetres."),
            new ANLAbel.Printing.PrinterProfiles.PrintPreflightIssue(0, "Logo", "Image", "Image has no embedded data.", IsBlocking: false),
        ]);

        // Labels 3 and 4 are two copies of data row 2: the operator must see row 2 once.
        var text = ANLAbel.App.Services.PreflightIssueText.Describe(result, label => label is 3 or 4 ? 2 : 0);
        Require(text.StartsWith("Kiểm tra trước khi in phát hiện 2 vấn đề. Chưa có tem nào được gửi.", StringComparison.Ordinal), text);
        Require(text.Contains("- Dòng 2 · “SKU” (mã QR): dữ liệu thiếu cột Part; đối tượng sẽ in trống", StringComparison.Ordinal), text);
        Require(text.Contains("- Mẫu (khổ tem): khổ giấy văn phòng (A4, Letter…) không phải khổ tem", StringComparison.Ordinal), text);
        Require(!text.Contains("Logo", StringComparison.Ordinal), "Advisory issues must not be listed as print blockers: " + text);
        Require(ANLAbel.App.Services.PreflightIssueText.Message("A brand-new validator message.") == "A brand-new validator message.",
            "An unmapped validator message must be shown unchanged, not hidden.");
        Require(ANLAbel.App.Services.PreflightIssueText.Sentence("Copies field 'Qty' value '2,5' is not a whole number.")
                == "Số bản in “2,5” (cột “Qty”) không phải số nguyên.",
            "Copies problems must be worded for the operator.");

        // Each rule anchors on a validator/contract message; rewording it must fail here.
        var root = FindRepositoryRoot();
        var sources = string.Concat(new[] { "ANLAbel.Core", "ANLAbel.Data", "ANLAbel.Printing", "ANLAbel.App" }
            .SelectMany(project => Directory.EnumerateFiles(Path.Combine(root, "src", project), "*.cs", SearchOption.AllDirectories))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                           && !path.EndsWith("PreflightIssueText.cs", StringComparison.Ordinal))
            .Select(File.ReadAllText));
        string[] anchors =
        [
            "Missing field(s) in Excel data: ",
            "Formula binding error",
            "Text extends outside the design label for this row",
            "Fixed text frame overflow",
            "Text box overflow",
            "Object extends outside the design label bounds",
            "' is not installed on this machine. Preview and print will use '",
            "' has no glyph(s) for ",
            "Image has no embedded data",
            "Image data is not a valid embedded bitmap",
            "Image could not be decoded for printing",
            "Image raster mode '",
            "Image source density is only ",
            "Invalid {barcodeType} data.",
            " data cannot be empty or whitespace.",
            " check-digit policy is Verify but the barcode data is empty.",
            " check-digit policy is Verify: payload does not end with a valid check digit.",
            "GS1 data is empty.",
            "GS1 data must use (AI)value notation",
            "GS1 AI {ai}",
            "Fixed QR capacity exceeded: ",
            "2D barcode frame is too small for this row: requires at least ",
            "Module is only ~",
            "Linear module is only ",
            "below the industrial floor",
            " module(s); this profile requires at least ",
            "Code 39 quiet zone is ",
            "Code 39 wide:narrow ratio",
            "Bearer bars are only supported on 1D",
            "Bearer bar thickness is ",
            "HRI font size must be between",
            "Print scale ",
            "Label stock ",
            "Physical stock ",
            "Print method is set to PrinterNative",
            "' is blank.",
            "' is not a whole number.",
            " is negative.",
            " exceeds the limit of ",
            "' is missing from the row.",
        ];
        foreach (var anchor in anchors)
        {
            Require(sources.Contains(anchor, StringComparison.Ordinal),
                $"Preflight message changed; update PreflightIssueText: {anchor}");
        }

        var diagnostics = File.ReadAllText(Path.Combine(root, "src", "ANLAbel.App", "PrintPreviewWindow.Diagnostics.cs"));
        Require(diagnostics.Contains("PreflightIssueText.Line(", StringComparison.Ordinal)
                && !diagnostics.Contains("\"Excel row", StringComparison.Ordinal)
                && !diagnostics.Contains("Go to first affected label", StringComparison.Ordinal),
            "The Print Preview issue list must be worded in Vietnamese.");
        var printClick = Method(Path.Combine("src", "ANLAbel.App", "PrintPreviewWindow.xaml.cs"), "private async void Print_Click(");
        Require(!printClick.Contains("preflight.ToUserMessage(", StringComparison.Ordinal)
                && !printClick.Contains("CopiesFieldContract.ToUserMessage(", StringComparison.Ordinal)
                && printClick.Contains("PreflightIssueText.Describe(preflight, SourceRowOf(selected))", StringComparison.Ordinal),
            "Print blocks must name data rows and problems in Vietnamese.");
        return Task.CompletedTask;
    }

    private static string Method(string relativePath, string signature)
    {
        var source = File.ReadAllText(Path.Combine(FindRepositoryRoot(), relativePath)).Replace("\r\n", "\n");
        var start = source.IndexOf(signature, StringComparison.Ordinal);
        Require(start >= 0, $"{signature} must exist in {relativePath}.");
        var end = source.IndexOf("\n    private ", start + 1, StringComparison.Ordinal);
        var nextPublic = source.IndexOf("\n    public ", start + 1, StringComparison.Ordinal);
        if (nextPublic > 0 && (end < 0 || nextPublic < end))
        {
            end = nextPublic;
        }

        return source[start..(end < 0 ? source.Length : end)];
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
