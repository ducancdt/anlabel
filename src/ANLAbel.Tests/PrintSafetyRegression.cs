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

    public static Task DroppedPostDispatchEvidenceIsToldToTheOperator()
    {
        // Audit 2026-10-13: an event that could not be saved after dispatch was
        // only written to the debugger, so a printed job later showed up as
        // uncertain with no explanation, inviting a duplicate reprint.
        var viewModelRecord = Method(Path.Combine("src", "ANLAbel.App", "ViewModels", "MainViewModel.cs"), "private async Task RecordPrintJobTransitionAsync(");
        var previewRecord = Method(Path.Combine("src", "ANLAbel.App", "PrintPreviewWindow.xaml.cs"), "private async Task RecordPrintJobTransitionAsync(");
        Require(viewModelRecord.Contains("_printEvidenceDropped = true", StringComparison.Ordinal)
                && previewRecord.Contains("_printEvidenceDropped = true", StringComparison.Ordinal),
            "A post-dispatch event that could not be saved must be remembered for the operator.");
        var viewModel = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src", "ANLAbel.App", "ViewModels", "MainViewModel.cs"));
        Require(viewModel.Split("+ EvidenceNote(tracked)").Length - 1 == 4,
            "Every quick-print result line must carry the evidence note.");
        var printClick = Method(Path.Combine("src", "ANLAbel.App", "PrintPreviewWindow.xaml.cs"), "private async void Print_Click(");
        Require(printClick.Contains("if (_printEvidenceDropped)", StringComparison.Ordinal),
            "The Print Preview result must say when the job's outcome could not be logged.");

        // Closing or updating while a quick print is still being sent must not
        // cut the job off silently.
        var closing = Method(Path.Combine("src", "ANLAbel.App", "MainWindow.DocumentLifecycle.cs"), "private void MainWindow_Closing(");
        var update = Method(Path.Combine("src", "ANLAbel.App", "MainWindow.DocumentLifecycle.cs"), "internal async Task<bool> PrepareForUpdateInstallAsync(");
        Require(closing.Contains("_viewModel.IsQuickPrinting", StringComparison.Ordinal) && closing.Contains("\"Chờ in xong\"", StringComparison.Ordinal)
                && update.Contains("_viewModel.IsQuickPrinting", StringComparison.Ordinal),
            "Closing or installing an update during a quick print must ask or wait first.");
        return Task.CompletedTask;
    }

    public static async Task SavedPrinterCheckGivesUpOnAHungSpooler()
    {
        // Audit 2026-10-13: the saved-printer check had no timeout; with a hung
        // spooler the chip never updated and every check parked a thread.
        var lookup = new HangingQueueLookup();
        var previous = ANLAbel.App.ViewModels.MainViewModel.PrinterQueueLookupTimeout;
        ANLAbel.App.ViewModels.MainViewModel.PrinterQueueLookupTimeout = TimeSpan.FromMilliseconds(200);
        try
        {
            var vm = new ANLAbel.App.ViewModels.MainViewModel(
                new ANLAbel.Project.SaveLoad.ProjectFileService(),
                new ANLAbel.Data.Excel.ExcelDataService(),
                new ANLAbel.Printing.PrinterProfiles.PrintService(queueLookup: lookup),
                new ANLAbel.Printing.PrinterProfiles.PrinterDiscoveryService(),
                new ANLAbel.Data.PrintLogs.PrintLogService(),
                printerQueueLookup: lookup);
            vm.Template.PrinterProfile.PrinterName = "Zebra treo";
            await vm.RefreshPrinterQueueStatusAsync();
            Require(vm.HasPrinterQueueWarning && vm.PrinterQueueStatusMessage.StartsWith("Không kiểm tra được máy in đã lưu", StringComparison.Ordinal),
                $"A hung spooler must give a warning instead of no answer: {vm.PrinterQueueStatusMessage}");
            await vm.RefreshPrinterQueueStatusAsync();
            Require(Volatile.Read(ref lookup.Calls) == 1, "A second check must reuse the stuck lookup, not park another thread.");
        }
        finally
        {
            lookup.Release.Set();
            ANLAbel.App.ViewModels.MainViewModel.PrinterQueueLookupTimeout = previous;
        }
    }

    private sealed class HangingQueueLookup : ANLAbel.Printing.PrinterProfiles.IPrinterQueueLookup
    {
        public int Calls;

        public ManualResetEventSlim Release { get; } = new();

        public ANLAbel.Printing.PrinterProfiles.PrinterQueueLookupResult Resolve(string printerName)
        {
            Interlocked.Increment(ref Calls);
            Release.Wait();
            return ANLAbel.Printing.PrinterProfiles.PrinterQueueLookupResult.Missing(printerName, "released");
        }
    }

    public static async Task PrinterListWaitIsBoundedAndReusesAStuckEnumeration()
    {
        // Audit 2026-10-13: a hung Print Spooler left a wait cursor and no
        // answer, and every further click stacked another stuck thread.
        var release = new ManualResetEventSlim();
        var calls = 0;
        ANLAbel.Printing.PrinterProfiles.PrinterDiscoveryResult Stuck()
        {
            Interlocked.Increment(ref calls);
            release.Wait();
            return new ANLAbel.Printing.PrinterProfiles.PrinterDiscoveryResult(Array.Empty<ANLAbel.Printing.PrinterProfiles.PrinterInfo>());
        }

        try
        {
            var first = await ANLAbel.App.Services.PrinterQueueLoader.DiscoverAsync(Stuck, TimeSpan.FromMilliseconds(100));
            Require(first is null, "A printer list Windows does not return must stop waiting instead of hanging.");
            var second = await ANLAbel.App.Services.PrinterQueueLoader.DiscoverAsync(Stuck, TimeSpan.FromMilliseconds(100));
            Require(second is null, "A second attempt on a hung spooler must also stop waiting.");
            Require(Volatile.Read(ref calls) == 1, "Opening the dialog again must not stack another enumeration on a hung spooler.");
        }
        finally
        {
            release.Set();
        }

        var recovered = await ANLAbel.App.Services.PrinterQueueLoader.DiscoverAsync(
            () => new ANLAbel.Printing.PrinterProfiles.PrinterDiscoveryResult(Array.Empty<ANLAbel.Printing.PrinterProfiles.PrinterInfo>()),
            TimeSpan.FromSeconds(10));
        Require(recovered is { IsSuccess: true }, "Once Windows answers, the printer list is used.");
        Require(ANLAbel.App.Services.PrinterQueueLoader.DefaultTimeout <= TimeSpan.FromSeconds(30),
            "The operator must get an answer within half a minute.");
    }

    public static async Task QuickPrintRefusesPlaceholdersAndRunsOneJobAtATime()
    {
        // A bound label with no data row must not print "{PartNo}" as text.
        var vm = new ANLAbel.App.ViewModels.MainViewModel();
        vm.Template.PrinterProfile.PrinterName = "Audit-Queue";
        vm.Template.Objects.Add(new ANLAbel.Core.Models.LabelObject
        {
            Id = "bound",
            Type = ANLAbel.Core.Enums.ObjectType.Text,
            Name = "Mã",
            Text = "{PartNo}",
            BindingExpression = "{PartNo}",
            XMm = 2,
            YMm = 2,
            WidthMm = 20,
            HeightMm = 5
        });
        await ((ANLAbel.App.ViewModels.AsyncRelayCommand)vm.PrintCurrentRowCommand).ExecuteAsync(null);
        Require(vm.StatusText.StartsWith("Đã chặn in: mẫu có trường dữ liệu nhưng chưa có dòng dữ liệu", StringComparison.Ordinal),
            "Quick print must refuse a bound label without data: " + vm.StatusText);
        Require(!vm.IsQuickPrinting, "The print gate must be released after a refused print.");

        // Both quick-print commands share one gate and release it in finally.
        var source = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src", "ANLAbel.App", "ViewModels", "MainViewModel.cs")).Replace("\r\n", "\n");
        foreach (var name in new[] { "private async Task PrintCurrentRowAsync()", "private async Task PrintAllRowsAsync()" })
        {
            var body = Method(Path.Combine("src", "ANLAbel.App", "ViewModels", "MainViewModel.cs"), name);
            Require(body.Contains("if (!TryBeginQuickPrint())", StringComparison.Ordinal)
                    && body.Contains("IsQuickPrinting = false;", StringComparison.Ordinal),
                name + " must take the shared quick-print gate and release it.");
        }

        var printClick = Method(Path.Combine("src", "ANLAbel.App", "PrintPreviewWindow.xaml.cs"), "private async void Print_Click(");
        var drift = printClick.IndexOf("if (_previewPlan is not null && !(outputUnchanged && quantityUnchanged))", StringComparison.Ordinal);
        Require(drift > 0 && printClick.IndexOf("RefreshPreview();", drift, StringComparison.Ordinal) > drift
                && printClick.IndexOf("return;", drift, StringComparison.Ordinal) < printClick.IndexOf("PrintRowsWithResultAsync(", StringComparison.Ordinal),
            "Print Preview must show a changed setup or selection again instead of printing it unseen.");
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
            // An unplugged USB XP-DT426B (2026-10-14): Windows still has the queue.
            ("Requested printer 'Xprinter XP-DT426B' is unavailable. The printer queue reports offline. Resolve the queue/device condition and prepare again; no label was submitted. Select a verified queue before printing; the Windows default queue will not be used implicitly.",
                "Máy in “Xprinter XP-DT426B” đang ngoại tuyến (offline). Chưa có tem nào được gửi."),
            ("Requested printer 'Zebra ZT411' is unavailable. The requested Windows printer queue is unavailable. Select a verified queue before printing; the Windows default queue will not be used implicitly.",
                "Không tìm thấy máy in “Zebra ZT411” trong Windows."),
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
            // The row-count and template-drift reprint stops came from an unused
            // view-model dispatch removed in the 2026-10-13 audit; the explainer
            // keeps their rule for job records written by older versions.
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

        // Check-digit messages are produced by the contract itself, not retyped here.
        var customItf = new ANLAbel.Core.Models.LabelObject
        {
            Type = ANLAbel.Core.Enums.ObjectType.BarcodeCode128,
            BarcodeSymbology = ANLAbel.Core.Enums.BarcodeSymbology.ITF,
            BarcodeCheckDigitPolicy = ANLAbel.Core.Enums.BarcodeCheckDigitPolicy.Generate,
            CheckDigitModulus = 1
        };
        var producedMessages = new List<string>();
        producedMessages.AddRange(ANLAbel.Core.Barcode.BarcodeCheckDigitContract.Validate(ANLAbel.Core.Enums.BarcodeSymbology.ITF, "", ANLAbel.Core.Enums.BarcodeCheckDigitPolicy.Generate));
        producedMessages.AddRange(ANLAbel.Core.Barcode.BarcodeCheckDigitContract.Validate(ANLAbel.Core.Enums.BarcodeSymbology.ITF, "12", ANLAbel.Core.Enums.BarcodeCheckDigitPolicy.Generate));
        producedMessages.AddRange(ANLAbel.Core.Barcode.BarcodeCheckDigitContract.Validate(ANLAbel.Core.Enums.BarcodeSymbology.ITF, "1A", ANLAbel.Core.Enums.BarcodeCheckDigitPolicy.Generate));
        producedMessages.AddRange(ANLAbel.Core.Barcode.BarcodeCheckDigitContract.Validate(ANLAbel.Core.Enums.BarcodeSymbology.Code39, "a#", ANLAbel.Core.Enums.BarcodeCheckDigitPolicy.Generate));
        producedMessages.AddRange(ANLAbel.Core.Barcode.BarcodeCheckDigitContract.Validate(customItf, ANLAbel.Core.Enums.BarcodeSymbology.ITF, "123"));
        customItf.CheckDigitModulus = 10;
        customItf.CheckDigitWeights = "x";
        producedMessages.AddRange(ANLAbel.Core.Barcode.BarcodeCheckDigitContract.Validate(customItf, ANLAbel.Core.Enums.BarcodeSymbology.ITF, "123"));
        customItf.CheckDigitWeights = "3,1";
        producedMessages.AddRange(ANLAbel.Core.Barcode.BarcodeCheckDigitContract.Validate(customItf, ANLAbel.Core.Enums.BarcodeSymbology.ITF, "12A"));
        customItf.BarcodeCheckDigitPolicy = ANLAbel.Core.Enums.BarcodeCheckDigitPolicy.Verify;
        producedMessages.AddRange(ANLAbel.Core.Barcode.BarcodeCheckDigitContract.Validate(customItf, ANLAbel.Core.Enums.BarcodeSymbology.ITF, "1230"));
        Require(producedMessages.Count == 8, $"Expected 8 check-digit messages, got {producedMessages.Count}.");
        foreach (var produced in producedMessages)
        {
            Require(ANLAbel.App.Services.PreflightIssueText.IsKnown(produced),
                $"Check-digit message has no Vietnamese rule: {produced}");
        }

        Require(ANLAbel.App.Services.PreflightIssueText.IsKnown(
                "Frame bearer bars are 2 mm thick but the quiet zone is only 1 mm (2 modules), so the side bars cover the first and last bars of the barcode. Increase quiet zone to at least 4 modules, make the bearer thinner, or use top/bottom bearer bars.")
            && ANLAbel.App.Services.PreflightIssueText.IsKnown(
                "Bar width reduction of 2 dot(s) leaves the narrow bar 1 dot(s) wide (3 dot(s) at 203 DPI), below the 2-dot minimum, so the barcode may not scan. Use at most 1 dot(s) of reduction or increase X-dimension."),
            "Bearer overlap and bar width reduction messages need Vietnamese rules.");
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
            " check-digit policy is Verify: payload does not end with a valid custom check digit.",
            " check-digit policy is Generate but the barcode data is empty.",
            "Code 39 check digit cannot be generated",
            "ITF check digit cannot be generated",
            "A custom check digit can only be generated for data made of digits",
            "ITF needs an even number of digits including the check digit; ",
            "Custom check digit modulus must be",
            "Custom check digit weights ",
            "Custom check digit value map ",
            "Frame bearer bars are ",
            "HRI mask has ",
            "HRI mask character must be a single character.",
            "HRI mask is longer than ",
            "Bar width reduction of ",
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
            " magnification (X ",
            " is thinner than one printer dot (",
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

    public static Task SerialNumbersAreReservedBeforeTheManifestAndExplainedInVietnamese()
    {
        var quickPrint = Method(Path.Combine("src", "ANLAbel.App", "ViewModels", "MainViewModel.cs"), "private async Task<TrackedPrintResult> DispatchTrackedPrintAsync(");
        var apply = quickPrint.IndexOf("SerialCounterContract.TryApply(", StringComparison.Ordinal);
        var preflight = quickPrint.IndexOf("_printService.ValidateRowsAsync(", StringComparison.Ordinal);
        var reserve = quickPrint.IndexOf("SerialCounterStore.ForCounter(counter).TryReserve(", StringComparison.Ordinal);
        var manifest = quickPrint.IndexOf("PrintJobManifest.Create(", StringComparison.Ordinal);
        Require(apply > 0 && apply < preflight && preflight < reserve && reserve < manifest,
            "Quick print must number rows before preflight and reserve serials after preflight but before the manifest.");

        var previewPrint = Method(Path.Combine("src", "ANLAbel.App", "PrintPreviewWindow.xaml.cs"), "private async void Print_Click(");
        var previewReserve = previewPrint.IndexOf("SerialCounterStore.ForCounter(counter).TryReserve(", StringComparison.Ordinal);
        var previewManifest = previewPrint.IndexOf("PrintJobManifest.Create(", StringComparison.Ordinal);
        Require(previewReserve > 0 && previewReserve < previewManifest,
            "Print Preview must reserve serial numbers before it creates the manifest.");
        Require(previewPrint.Contains("SerialCounterContract.IsActive(_template) && _approvedReprintJobId is null", StringComparison.Ordinal),
            "A reprint must reuse the original job's serials, never reserve new ones.");
        Require(previewPrint.Contains("_serialFirstValue != _serialPreviewedFirstValue", StringComparison.Ordinal),
            "Print Preview must refuse to print serials other than the ones it showed.");
        var viewModelSource = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src", "ANLAbel.App", "ViewModels", "MainViewModel.cs"));
        Require(viewModelSource.Contains("return new TrackedPrintResult(jobId, result, spoolStatus, printTemplate, _printEvidenceDropped, rows);", StringComparison.Ordinal)
                && System.Text.RegularExpressions.Regex.Matches(viewModelSource, @"WritePrintLogAsync\(\s*""(?:Current row|All rows)"",\s*tracked\.PrintedRows \?\? rows,").Count == 2,
            "Quick print history must record the numbered rows that were printed, including each serial.");

        var counter = new ANLAbel.Core.Models.SerialCounterDefinition
        {
            Enabled = true,
            StartValue = 98,
            MaximumValue = 99
        };
        var produced = new List<string>();
        produced.AddRange(ANLAbel.Core.Printing.SerialCounterContract.ValidateDefinition(new ANLAbel.Core.Models.SerialCounterDefinition
        {
            Step = 0,
            ChangeEvery = 0,
            MinimumValue = 5,
            MaximumValue = 4,
            Digits = 40,
            Prefix = new string('x', 40)
        }));
        produced.AddRange(ANLAbel.Core.Printing.SerialCounterContract.ValidateDefinition(new ANLAbel.Core.Models.SerialCounterDefinition
        {
            Enabled = true,
            StartValue = 100,
            MaximumValue = 99
        }));
        ANLAbel.Core.Printing.SerialCounterContract.TryApply(counter, 98, new IReadOnlyDictionary<string, string>?[3], out _, out var overflow);
        produced.Add(overflow);

        var directory = Path.Combine(Path.GetTempPath(), $"anl-serial-reg-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var store = new ANLAbel.Data.PrintLogs.SerialCounterStore(Path.Combine(directory, "r.jsonl"));
            store.TryReserve(counter, "job-a", 98, 1, out _, out _);
            store.TryReserve(counter, "job-b", 98, 1, out _, out var changed);
            store.TryReserve(counter, "job-a", 50, 9, out _, out var alreadyReserved);
            produced.Add(changed);
            produced.Add(alreadyReserved);
            File.WriteAllText(Path.Combine(directory, "bad.jsonl"), "x\n");
            try
            {
                new ANLAbel.Data.PrintLogs.SerialCounterStore(Path.Combine(directory, "bad.jsonl")).PeekNextValue(counter);
            }
            catch (InvalidDataException exception)
            {
                produced.Add(exception.Message);
            }
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }

        Require(produced.Count >= 10, $"Expected every serial counter message to be produced, got {produced.Count}.");
        foreach (var message in produced)
        {
            Require(!string.IsNullOrWhiteSpace(message) && ANLAbel.App.Services.SerialCounterText.IsKnown(message),
                $"Serial counter message has no Vietnamese rule: {message}");
        }

        return Task.CompletedTask;
    }

    public static Task JobValuesAreRecordedBeforeTheManifestAndExplainedInVietnamese()
    {
        var quickPrint = Method(Path.Combine("src", "ANLAbel.App", "ViewModels", "MainViewModel.cs"), "private async Task<TrackedPrintResult> DispatchTrackedPrintAsync(");
        var apply = quickPrint.IndexOf("JobVariableContract.TryApply(", StringComparison.Ordinal);
        var preflight = quickPrint.IndexOf("_printService.ValidateRowsAsync(", StringComparison.Ordinal);
        var record = quickPrint.IndexOf("_printJobValueStore.Record(", StringComparison.Ordinal);
        var manifest = quickPrint.IndexOf("PrintJobManifest.Create(", StringComparison.Ordinal);
        Require(apply > 0 && apply < preflight && preflight < record && record < manifest,
            "Quick print must fill job values before preflight and record them after preflight but before the manifest.");

        var previewPrint = Method(Path.Combine("src", "ANLAbel.App", "PrintPreviewWindow.xaml.cs"), "private async void Print_Click(");
        var previewRecord = previewPrint.IndexOf("_printJobValueStore.Record(", StringComparison.Ordinal);
        var previewManifest = previewPrint.IndexOf("PrintJobManifest.Create(", StringComparison.Ordinal);
        Require(previewRecord > 0 && previewRecord < previewManifest,
            "Print Preview must record job values before it creates the manifest.");
        Require(previewPrint.Contains("JobVariableContract.UsesJobVariables(_template) && _approvedReprintJobId is null", StringComparison.Ordinal)
                && previewPrint.Contains("!SameJobValues(_currentJobValues, _previewedJobValues)", StringComparison.Ordinal),
            "Print Preview must reuse a reprint's values and refuse values other than the ones it showed.");
        var openPreview = Method(Path.Combine("src", "ANLAbel.App", "MainWindow.xaml.cs"), "private void OpenPrintPreviewDialog(");
        Require(openPreview.Contains("approvedReprintJobId is null && prompts.Count > 0", StringComparison.Ordinal),
            "A reprint must not ask for new prompt values.");

        var template = new ANLAbel.Core.Models.LabelTemplate();
        template.Objects.Add(new ANLAbel.Core.Models.LabelObject { BindingExpression = "{?Lot}{#NOW:MM}{#NOW:mm}" });
        var produced = new List<string>();
        var definition = new ANLAbel.Core.Models.PromptVariableDefinition { Name = "Lot", MaxLength = 2, DigitsOnly = true };
        produced.Add(ANLAbel.Core.Printing.JobVariableContract.ValidatePromptValue(definition, ""));
        produced.Add(ANLAbel.Core.Printing.JobVariableContract.ValidatePromptValue(definition, "123"));
        produced.Add(ANLAbel.Core.Printing.JobVariableContract.ValidatePromptValue(definition, "a"));
        ANLAbel.Core.Printing.JobVariableContract.TryFormatDate("#NOW+1x", DateTimeOffset.Now, out _, out var badToken);
        produced.Add(badToken);
        ANLAbel.Core.Printing.JobVariableContract.TryFormatDate("#NOW:%", DateTimeOffset.Now, out _, out var badFormat);
        produced.Add(badFormat);
        ANLAbel.Core.Printing.JobVariableContract.TryResolveValues(template, DateTimeOffset.Now, new Dictionary<string, string> { ["Lot"] = "1" }, out _, out var clash);
        produced.Add(clash);
        var directory = Path.Combine(Path.GetTempPath(), $"anl-jobvars-reg-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, "v.jsonl");
            File.WriteAllText(path, "x\n");
            try
            {
                new ANLAbel.Data.PrintLogs.PrintJobValueStore(path).Find("job");
            }
            catch (InvalidDataException exception)
            {
                produced.Add(exception.Message);
            }
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }

        Require(produced.Count == 7, $"Expected 7 job value messages, got {produced.Count}.");
        foreach (var message in produced)
        {
            Require(!string.IsNullOrWhiteSpace(message) && ANLAbel.App.Services.JobVariableText.IsKnown(message),
                $"Job value message has no Vietnamese rule: {message}");
        }

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
