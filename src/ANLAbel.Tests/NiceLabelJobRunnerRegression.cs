using ANLAbel.App;
using ANLAbel.App.Automation;
using ANLAbel.Core.Models;
using ANLAbel.Printing.PrinterProfiles;
using ANLAbel.Project.SaveLoad;

internal static class NiceLabelJobRunnerRegression
{
    public static async Task Run()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "anlabel_job_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            var files = new ProjectFileService();
            var queueName = "Industrial Zebra Queue";
            var availableLookup = new TestQueueLookup(queueName, available: true);
            var missingLookup = new TestQueueLookup(queueName, available: false);

            var templatePath = Path.Combine(tempDir, "label1.anlabel");
            var template = new LabelTemplate
            {
                Name = "Job Test Template",
                WidthMm = 50,
                HeightMm = 30,
                Dpi = 203
            };
            template.PrinterProfile.PrinterName = queueName;
            template.PrinterProfile.Dpi = 203;
            template.Objects.Add(new LabelObject
            {
                Text = "SKU: {SKU}",
                BindingExpression = "{SKU}",
                XMm = 2,
                YMm = 2,
                WidthMm = 40,
                HeightMm = 10
            });
            await files.SaveAsync(template, templatePath);

            var runner = new NiceLabelJobRunner(
                files: files,
                queueLookup: availableLookup);

            // 1. Missing JOB file fails closed
            var missingReport = await runner.RunFileAsync(Path.Combine(tempDir, "nonexistent.job"));
            Require(!missingReport.IsSuccess, "Missing JOB file must fail execution.");
            Require(missingReport.Diagnostics.Count > 0, "Missing JOB file must produce diagnostic.");

            // 2. Syntax/planner error (unclosed session) fails execution
            const string unclosedSessionScript = """
                LABEL "label1.anlabel"
                SESSIONSTART
                SESSIONPRINT 1
                """;
            var badReport = await runner.RunAsync(unclosedSessionScript, baseDirectory: tempDir);
            Require(!badReport.IsSuccess, "Unclosed session must fail execution plan.");
            Require(badReport.Diagnostics.Any(d => d.Contains("SESSIONEND")), "Diagnostic must report missing SESSIONEND.");

            // 3. Preflight-only dry run on valid script succeeds without spooling
            var dryRunScript = $"""
                LABEL "{Path.GetFileName(templatePath)}"
                SET SKU="WIDGET-99"
                PRINT 5
                """;
            var dryReport = await runner.RunAsync(dryRunScript, baseDirectory: tempDir, preflightOnly: true);
            Require(dryReport.IsSuccess, $"Valid script dry-run must succeed. Diagnostics: {string.Join("; ", dryReport.Diagnostics)}");
            Require(dryReport.TotalActions == 1, "Plan must contain 1 action.");
            Require(dryReport.ExecutedActions == 1, "Dry run preflight validates 1 action.");
            Require(dryReport.TotalLabelsPrinted == 5, "Dry run calculates 5 total labels.");
            Require(dryReport.SpoolResults.Count == 0, "Dry run must produce 0 spool results.");

            // 4. Missing / unhealthy printer queue halts execution
            var runnerMissingQueue = new NiceLabelJobRunner(
                files: files,
                queueLookup: missingLookup);

            var queueFailReport = await runnerMissingQueue.RunAsync(dryRunScript, baseDirectory: tempDir, preflightOnly: false);
            Require(!queueFailReport.IsSuccess, "Unavailable queue must fail execution.");
            Require(queueFailReport.Diagnostics.Any(d => d.Contains("not ready")), "Diagnostic must report queue not ready.");

            // 5. IgnoreErrors allows continuation past a missing template
            var ignoreErrorsScript = $"""
                IGNOREERROR on
                LABEL "missing_template.anlabel"
                PRINT 1
                LABEL "{Path.GetFileName(templatePath)}"
                SET SKU="RECOVERED-01"
                PRINT 2
                """;
            var ignoreReport = await runner.RunAsync(ignoreErrorsScript, baseDirectory: tempDir, preflightOnly: true);
            Require(ignoreReport.TotalActions == 2, "Plan with IGNOREERROR must have 2 actions.");
            Require(ignoreReport.Diagnostics.Any(d => d.Contains("missing_template.anlabel")), "Diagnostics must record first missing template.");

            // 6. File operations (CREATEFILE & DELETEFILE)
            var flagFile = Path.Combine(tempDir, "signal.txt");
            var fileOpScript = $"""
                CREATEFILE "signal.txt", "JOB_ACTIVE"
                LABEL "{Path.GetFileName(templatePath)}"
                PRINT 1
                DELETEFILE "signal.txt"
                """;
            // Preflight only: file ops skipped
            await runner.RunAsync(fileOpScript, baseDirectory: tempDir, preflightOnly: true);
            Require(!File.Exists(flagFile), "Preflight-only must not execute file modifications.");

            var validName = Path.GetFileName(templatePath);

            // 7. FIX H1: an unknown/unrecognized directive is a hard parse error and halts
            //    before any dispatch.
            var unknownCmdScript = $"""
                LABEL "{validName}"
                FOOBAR something
                PRINT 1
                """;
            var unknownReport = await runner.RunAsync(unknownCmdScript, baseDirectory: tempDir, preflightOnly: false);
            Require(!unknownReport.IsSuccess, "Unknown command must fail closed.");
            Require(unknownReport.ExecutedActions == 0, "Unknown command must halt before executing any action.");
            Require(unknownReport.Diagnostics.Any(d => d.Contains("Unrecognized")), "Unknown command must produce a parse diagnostic.");

            // 8. FIX H1: a plain parse error (invalid PRINT quantity) halts before dispatch.
            var parseErrScript = $"""
                LABEL "{validName}"
                PRINT notanumber
                """;
            var parseErrReport = await runner.RunAsync(parseErrScript, baseDirectory: tempDir, preflightOnly: false);
            Require(!parseErrReport.IsSuccess, "Parse error must fail closed.");
            Require(parseErrReport.ExecutedActions == 0, "Parse error must halt before executing any action.");

            // 9. FIX H2: path traversal escaping the base directory is rejected.
            var traversalScript = """
                LABEL "..\escape.anlabel"
                PRINT 1
                """;
            var traversalReport = await runner.RunAsync(traversalScript, baseDirectory: tempDir, preflightOnly: true);
            Require(!traversalReport.IsSuccess, "Path traversal must fail closed.");
            Require(traversalReport.Diagnostics.Any(d => d.Contains("escapes")), "Traversal diagnostic must be emitted.");

            // 10. CHANGE 1: an absolute template path OUTSIDE the base directory and every allowed
            //     root fails closed. (Updated from the old "reject ALL absolute paths" policy:
            //     absolute paths are now honored when contained under an allowed root — see #19.)
            var rootedScript = """
                LABEL "C:\Windows\evil.anlabel"
                PRINT 1
                """;
            var rootedReport = await runner.RunAsync(rootedScript, baseDirectory: tempDir, preflightOnly: true);
            Require(!rootedReport.IsSuccess, "Absolute path outside all allowed roots must fail closed.");
            Require(rootedReport.Diagnostics.Any(d => d.Contains("outside")), "Out-of-sandbox absolute path diagnostic must be emitted.");

            // 11. FIX H3: a huge copy count is rejected before allocation.
            var overflowScript = $"""
                LABEL "{validName}"
                SET SKU="X"
                PRINT 2000000
                """;
            var overflowReport = await runner.RunAsync(overflowScript, baseDirectory: tempDir, preflightOnly: true);
            Require(!overflowReport.IsSuccess, "Huge copy count must fail closed.");
            Require(overflowReport.Diagnostics.Any(d => d.Contains("per-action maximum")), "Overflow diagnostic must be emitted.");

            // 12. FIX M4: IGNOREERROR must NOT bypass the missing-printer/queue safety halt.
            var runnerMissingQueue2 = new NiceLabelJobRunner(files: files, queueLookup: missingLookup);
            var ignoreQueueScript = $"""
                IGNOREERROR on
                LABEL "{validName}"
                SET SKU="X"
                PRINT 1
                """;
            var ignoreQueueReport = await runnerMissingQueue2.RunAsync(ignoreQueueScript, baseDirectory: tempDir, preflightOnly: false);
            Require(!ignoreQueueReport.IsSuccess, "IGNOREERROR must not bypass unavailable-queue safety halt.");
            Require(ignoreQueueReport.Diagnostics.Any(d => d.Contains("not ready")), "Queue-not-ready diagnostic must be emitted despite IGNOREERROR.");

            // 13. FIX M5: an unsupported-but-parsed PORT directive fails loud instead of
            //     silently printing to the physical printer.
            var portScript = $"""
                LABEL "{validName}"
                PORT "capture.prn"
                SET SKU="X"
                PRINT 1
                """;
            var portReport = await runner.RunAsync(portScript, baseDirectory: tempDir, preflightOnly: true);
            Require(!portReport.IsSuccess, "PORT directive must fail loud.");
            Require(portReport.Diagnostics.Any(d => d.Contains("PORT")), "PORT unsupported diagnostic must be emitted.");

            // 14. F1: a two-action JOB whose per-job label SUM overflows long must fail
            //     closed with a per-job-maximum diagnostic and MUST NOT throw
            //     OverflowException (the old Enumerable.Sum was checked and crashed the host).
            var overflowSumScript = $"""
                LABEL "{validName}"
                SET SKU="X"
                PRINT 2000000000, 0, 2000000000, 2000000000
                PRINT 2000000000, 0, 2000000000, 2000000000
                """;
            JobRunReport overflowSumReport;
            try
            {
                overflowSumReport = await runner.RunAsync(overflowSumScript, baseDirectory: tempDir, preflightOnly: true);
            }
            catch (OverflowException)
            {
                throw new InvalidOperationException("F1 regression: per-job total must saturate, not throw OverflowException.");
            }
            Require(!overflowSumReport.IsSuccess, "Overflowing per-job total must fail closed.");
            Require(overflowSumReport.ExecutedActions == 0, "Overflowing per-job total must halt before any action.");
            Require(overflowSumReport.SpoolResults.Count == 0, "Overflowing per-job total must not spool.");
            Require(
                overflowSumReport.Diagnostics.Any(d => d.Contains("per-job maximum")),
                "Overflowing per-job total must emit a per-job-maximum diagnostic.");

            // 15. F2: a single action whose Quantity*IdenticalCopies*NumberOfSets overflows
            //     long must saturate (never WRAP to a small valid count) and be rejected by
            //     a ceiling guard rather than silently printing a wrong count.
            var overflowActionScript = $"""
                LABEL "{validName}"
                SET SKU="X"
                PRINT 2000000000, 0, 2000000000, 2000000000
                """;
            var overflowActionReport = await runner.RunAsync(overflowActionScript, baseDirectory: tempDir, preflightOnly: true);
            Require(!overflowActionReport.IsSuccess, "Overflowing action count must fail closed.");
            Require(overflowActionReport.ExecutedActions == 0, "Overflowing action count must not execute.");
            Require(overflowActionReport.TotalLabelsPrinted == 0, "Overflowing action count must print zero labels (no wrap).");
            Require(
                overflowActionReport.Diagnostics.Any(d => d.Contains("maximum")),
                "Overflowing action count must emit a ceiling diagnostic.");

            // 16. F2 (unit): the saturating TotalLabelCount clamps a hostile triple to
            //     long.MaxValue instead of wrapping into a small positive value.
            var hostileAction = new ANLAbel.Core.Automation.Job.JobPrintAction(
                ActionIndex: 1,
                TemplatePath: validName,
                TargetPrinterName: queueName,
                JobName: "overflow",
                Variables: ANLAbel.Core.Data.DataRecord.Create(
                    Array.Empty<KeyValuePair<string, string?>>()),
                Quantity: 2_000_000_000,
                SkipFirstSlots: 0,
                IdenticalCopies: 2_000_000_000,
                NumberOfSets: 2_000_000_000,
                PrintParameters: new Dictionary<string, string>(),
                PortRedirectPath: null,
                PortAppend: false,
                SessionId: null);
            Require(
                hostileAction.TotalLabelCount == long.MaxValue,
                "Overflowing TotalLabelCount must saturate to long.MaxValue, not wrap.");

            // 17. F1 (unit): the saturating per-job sum clamps two long.MaxValue actions to
            //     long.MaxValue instead of throwing (checked Enumerable.Sum overflow).
            var saturatingPlan = new ANLAbel.Core.Automation.Job.JobExecutionPlan(
                Actions: new[] { hostileAction, hostileAction },
                FileActions: Array.Empty<ANLAbel.Core.Automation.Job.JobFileAction>(),
                Diagnostics: Array.Empty<string>(),
                IgnoreErrors: false);
            long saturatedTotal = 0;
            try
            {
                saturatedTotal = saturatingPlan.TotalLabelsToPrint;
            }
            catch (OverflowException)
            {
                throw new InvalidOperationException("F1 regression: TotalLabelsToPrint must saturate, not throw.");
            }
            Require(saturatedTotal == long.MaxValue, "Per-job sum must saturate to long.MaxValue.");

            // 18. Normal valid values are unaffected by saturation.
            var normalAction = new ANLAbel.Core.Automation.Job.JobPrintAction(
                ActionIndex: 1,
                TemplatePath: validName,
                TargetPrinterName: queueName,
                JobName: "normal",
                Variables: ANLAbel.Core.Data.DataRecord.Create(
                    Array.Empty<KeyValuePair<string, string?>>()),
                Quantity: 10,
                SkipFirstSlots: 0,
                IdenticalCopies: 3,
                NumberOfSets: 2,
                PrintParameters: new Dictionary<string, string>(),
                PortRedirectPath: null,
                PortAppend: false,
                SessionId: null);
            Require(normalAction.TotalLabelCount == 60, "Normal TotalLabelCount must be exact (10*3*2=60).");

            // 19. CHANGE 1: an ABSOLUTE template path CONTAINED under a configured allowed root
            //     succeeds (NiceLabel parity). The template lives in a directory OTHER than the
            //     JOB base directory, proving the allowlist (not just the base dir) is honored.
            var allowedRoot = Path.Combine(Path.GetTempPath(), "anlabel_job_allowed_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(allowedRoot);
            try
            {
                var allowedTemplatePath = Path.Combine(allowedRoot, "allowed_label.anlabel");
                await files.SaveAsync(template, allowedTemplatePath);

                var runnerWithRoot = new NiceLabelJobRunner(
                    files: files,
                    queueLookup: availableLookup,
                    allowedTemplateRoots: new[] { allowedRoot });

                var absoluteAllowedScript = $"""
                    LABEL "{allowedTemplatePath}"
                    SET SKU="ABS-OK"
                    PRINT 1
                    """;
                var absoluteAllowedReport = await runnerWithRoot.RunAsync(absoluteAllowedScript, baseDirectory: tempDir, preflightOnly: true);
                Require(absoluteAllowedReport.IsSuccess, $"Absolute template under an allowed root must succeed. Diagnostics: {string.Join("; ", absoluteAllowedReport.Diagnostics)}");
                Require(absoluteAllowedReport.ExecutedActions == 1, "Absolute-allowed action must validate.");

                // 20. CHANGE 1: an absolute template path OUTSIDE every allowed root still fails
                //     closed even when an allowlist is configured.
                var outsidePath = Path.Combine(Path.GetTempPath(), "anlabel_not_allowed_" + Guid.NewGuid().ToString("N"), "x.anlabel");
                var absoluteDeniedScript = $"""
                    LABEL "{outsidePath}"
                    PRINT 1
                    """;
                var absoluteDeniedReport = await runnerWithRoot.RunAsync(absoluteDeniedScript, baseDirectory: tempDir, preflightOnly: true);
                Require(!absoluteDeniedReport.IsSuccess, "Absolute path outside all allowed roots must fail closed.");
                Require(absoluteDeniedReport.Diagnostics.Any(d => d.Contains("outside")), "Denied absolute path diagnostic must be emitted.");

                // 21. CHANGE 1: CREATEFILE with an absolute path is STILL rejected — writes/deletes
                //     are confined to the base directory and are NOT widened by the allowlist.
                var createAbsScript = $"""
                    CREATEFILE "{Path.Combine(allowedRoot, "should_not_write.txt")}", "DATA"
                    LABEL "{validName}"
                    PRINT 1
                    """;
                var createAbsReport = await runnerWithRoot.RunAsync(createAbsScript, baseDirectory: tempDir, preflightOnly: false);
                Require(!createAbsReport.IsSuccess, "CREATEFILE with absolute path must be rejected even with an allowlist.");
                Require(createAbsReport.Diagnostics.Any(d => d.Contains("rejected")), "CREATEFILE rejection diagnostic must be emitted.");
                Require(!File.Exists(Path.Combine(allowedRoot, "should_not_write.txt")), "CREATEFILE must not write outside the base directory.");
            }
            finally
            {
                try { Directory.Delete(allowedRoot, recursive: true); } catch { }
            }

            // 22. CHANGE 2: SETPRINTPARAM known key (PRINTOFFSETX/Y) reaches the print settings.
            var offsetProfile = new PrinterProfile { OffsetXMm = 0, OffsetYMm = 0 };
            var offsetOk = NiceLabelJobRunner.TryApplyPrintParameters(
                offsetProfile,
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["PRINTOFFSETX"] = "2.5",
                    ["PRINTOFFSETY"] = "-1.25"
                },
                out var offsetError);
            Require(offsetOk, $"Known SETPRINTPARAM keys must apply. Error: {offsetError}");
            Require(Math.Abs(offsetProfile.OffsetXMm - 2.5) < 0.001, "PRINTOFFSETX must reach PrinterProfile.OffsetXMm.");
            Require(Math.Abs(offsetProfile.OffsetYMm - (-1.25)) < 0.001, "PRINTOFFSETY must reach PrinterProfile.OffsetYMm.");

            // 23. CHANGE 2: SETPRINTPARAM unknown/unsupported key fails closed (never dropped).
            var unknownParamOk = NiceLabelJobRunner.TryApplyPrintParameters(
                new PrinterProfile(),
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["PRINTSPEED"] = "6" },
                out var unknownParamError);
            Require(!unknownParamOk, "Unsupported SETPRINTPARAM key must fail closed.");
            Require(unknownParamError is not null && unknownParamError.Contains("not supported"), "Unsupported SETPRINTPARAM diagnostic must be emitted.");

            // 23a. CHANGE 2: non-finite PRINTOFFSET values (NaN/Infinity/overflow) fail closed and are NOT applied.
            foreach (var badOffset in new[] { "NaN", "Infinity", "-Infinity", "1E400" })
            {
                var nonFiniteProfile = new PrinterProfile { OffsetXMm = 0, OffsetYMm = 0 };
                var nonFiniteXOk = NiceLabelJobRunner.TryApplyPrintParameters(
                    nonFiniteProfile,
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["PRINTOFFSETX"] = badOffset },
                    out var nonFiniteXError);
                Require(!nonFiniteXOk, $"Non-finite PRINTOFFSETX '{badOffset}' must fail closed.");
                Require(nonFiniteXError is not null && nonFiniteXError.Contains("finite"), $"Non-finite PRINTOFFSETX '{badOffset}' must emit a finite-number diagnostic.");
                Require(nonFiniteProfile.OffsetXMm == 0, $"Non-finite PRINTOFFSETX '{badOffset}' must not be applied to OffsetXMm.");

                var nonFiniteYOk = NiceLabelJobRunner.TryApplyPrintParameters(
                    nonFiniteProfile,
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["PRINTOFFSETY"] = badOffset },
                    out var nonFiniteYError);
                Require(!nonFiniteYOk, $"Non-finite PRINTOFFSETY '{badOffset}' must fail closed.");
                Require(nonFiniteYError is not null && nonFiniteYError.Contains("finite"), $"Non-finite PRINTOFFSETY '{badOffset}' must emit a finite-number diagnostic.");
                Require(nonFiniteProfile.OffsetYMm == 0, $"Non-finite PRINTOFFSETY '{badOffset}' must not be applied to OffsetYMm.");
            }

            // 23b. CHANGE 2: unsupported SETPRINTPARAM key fails closed end-to-end through the runner.
            var setParamScript = $"""
                LABEL "{validName}"
                SETPRINTPARAM PRINTSPEED = 6
                SET SKU="X"
                PRINT 1
                """;
            var setParamReport = await runner.RunAsync(setParamScript, baseDirectory: tempDir, preflightOnly: true);
            Require(!setParamReport.IsSuccess, "Unsupported SETPRINTPARAM must fail the run.");
            Require(setParamReport.Diagnostics.Any(d => d.Contains("SETPRINTPARAM")), "Runner must surface the SETPRINTPARAM diagnostic.");

            // 24. CHANGE 2: SkipFirstSlots offsets the dispatch — N leading blank rows then N data rows.
            var dataRow = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["SKU"] = "S1" };
            var dispatchRows = NiceLabelJobRunner.BuildDispatchRows(dataRow, copies: 3, skipFirstSlots: 2);
            Require(dispatchRows.Count == 5, "Dispatch must feed skipFirstSlots + copies positions (2 + 3 = 5).");
            Require(dispatchRows[0] is { Count: 0 } && dispatchRows[1] is { Count: 0 }, "First N dispatch rows must be blank feed positions.");
            Require(dispatchRows[2] is { } r2 && r2.TryGetValue("SKU", out var v2) && v2 == "S1", "Data rows must follow the blank feed positions.");
            Require(dispatchRows[4] is { } r4 && r4.TryGetValue("SKU", out var v4) && v4 == "S1", "All copies must be present after the blank feed positions.");
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                try
                {
                    Directory.Delete(tempDir, recursive: true);
                }
                catch
                {
                    // Ignore cleanup exceptions
                }
            }
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private sealed class TestQueueLookup : IPrinterQueueLookup
    {
        private readonly string _printerName;
        private readonly bool _available;

        public TestQueueLookup(string printerName, bool available)
        {
            _printerName = printerName;
            _available = available;
        }

        public PrinterQueueLookupResult Resolve(string printerName)
        {
            return _available
                ? PrinterQueueLookupResult.Available(printerName, _printerName)
                : PrinterQueueLookupResult.Missing(printerName, "Test queue is unavailable.");
        }
    }
}
