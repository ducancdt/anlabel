using System.Globalization;
using System.IO;
using ANLAbel.Core.Automation.Job;
using ANLAbel.Core.Models;
using ANLAbel.Data.Automation;
using ANLAbel.Printing.PrinterProfiles;
using ANLAbel.Project.SaveLoad;

namespace ANLAbel.App.Automation;

public sealed record JobRunReport(
    bool IsSuccess,
    int TotalActions,
    int ExecutedActions,
    long TotalLabelsPrinted,
    IReadOnlyList<string> Diagnostics,
    IReadOnlyList<PrintJobResult> SpoolResults);

/// <summary>
/// Execution runner for NiceLabel JOB command files.
/// Conforms to NiceLabel 2019 User Guide Chapter 9.1.1.
/// Coordinates parsing, planning, preflight safety, and STA print dispatch.
/// </summary>
public sealed class NiceLabelJobRunner
{
    private readonly IProjectFileService _files;
    private readonly PrintPreflightValidator _validator;
    private readonly IPrinterQueueLookup _queueLookup;
    private readonly PrintService _printService;
    private readonly AutomationJobHistoryStore? _historyStore;

    /// <summary>
    /// CHANGE 1: absolute LABEL template paths are honored (NiceLabel parity) ONLY when they
    /// resolve, after normalization, under the JOB base directory (always allowed) or under one
    /// of these configured allowed roots. Empty by default (only the base directory is allowed).
    /// File WRITES/DELETES (CREATEFILE/DELETEFILE) are deliberately NOT widened by this list;
    /// they stay confined to the base directory.
    /// </summary>
    private readonly IReadOnlyList<string> _allowedTemplateRoots;

    /// <summary>Maximum number of physical labels a single print action may request (anti-DoS ceiling).</summary>
    public const long MaxLabelsPerAction = 1_000_000;

    /// <summary>Maximum number of physical labels an entire JOB may request (anti-DoS ceiling).</summary>
    public const long MaxLabelsPerJob = 5_000_000;

    public NiceLabelJobRunner(
        IProjectFileService? files = null,
        PrintPreflightValidator? validator = null,
        IPrinterQueueLookup? queueLookup = null,
        PrintService? printService = null,
        AutomationJobHistoryStore? historyStore = null,
        IEnumerable<string>? allowedTemplateRoots = null)
    {
        _files = files ?? new ProjectFileService();
        _validator = validator ?? new PrintPreflightValidator();
        _queueLookup = queueLookup ?? new WindowsPrinterQueueLookup();
        _printService = printService ?? new PrintService(queueLookup: _queueLookup);
        _historyStore = historyStore;
        _allowedTemplateRoots = NormalizeAllowedRoots(allowedTemplateRoots);
    }

    /// <summary>
    /// Normalizes configured allowed template roots to absolute, comparable full paths.
    /// Relative, empty, or unparseable entries are dropped (fail closed by omission).
    /// </summary>
    private static IReadOnlyList<string> NormalizeAllowedRoots(IEnumerable<string>? roots)
    {
        if (roots is null)
        {
            return Array.Empty<string>();
        }

        var normalized = new List<string>();
        foreach (var root in roots)
        {
            if (string.IsNullOrWhiteSpace(root) || !Path.IsPathRooted(root))
            {
                continue;
            }

            try
            {
                normalized.Add(Path.GetFullPath(root));
            }
            catch
            {
                // Drop unparseable roots rather than widening the sandbox on bad input.
            }
        }

        return normalized;
    }

    public async Task<JobRunReport> RunFileAsync(
        string jobFilePath,
        bool preflightOnly = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(jobFilePath);
        if (!File.Exists(jobFilePath))
        {
            return new JobRunReport(
                IsSuccess: false,
                TotalActions: 0,
                ExecutedActions: 0,
                TotalLabelsPrinted: 0,
                Diagnostics: new[] { $"JOB command file not found: '{jobFilePath}'" },
                SpoolResults: Array.Empty<PrintJobResult>());
        }

        var content = await File.ReadAllTextAsync(jobFilePath, cancellationToken);
        var baseDir = Path.GetDirectoryName(Path.GetFullPath(jobFilePath));
        return await RunAsync(content, baseDir, preflightOnly, cancellationToken);
    }

    public async Task<JobRunReport> RunAsync(
        string jobScriptContent,
        string? baseDirectory = null,
        bool preflightOnly = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(jobScriptContent);
        cancellationToken.ThrowIfCancellationRequested();

        var diagnostics = new List<string>();
        var spoolResults = new List<PrintJobResult>();
        long totalLabels = 0;
        var executedActions = 0;

        // 1. Parse JOB script.
        // FIX H1: A JOB file is UNTRUSTED input. If parsing produced any hard error
        // (including an unknown/unrecognized directive), fail closed immediately:
        // do not build a plan, run file operations, or dispatch. IGNOREERROR cannot
        // suppress parse errors.
        var parseResult = NiceLabelJobParser.Parse(jobScriptContent);
        if (parseResult.HasErrors)
        {
            foreach (var diag in parseResult.Diagnostics.Where(d => !d.IsWarning))
            {
                diagnostics.Add($"Parse Error Line {diag.LineNumber}: {diag.Message}");
            }

            return new JobRunReport(
                IsSuccess: false,
                TotalActions: 0,
                ExecutedActions: 0,
                TotalLabelsPrinted: 0,
                Diagnostics: diagnostics,
                SpoolResults: spoolResults);
        }

        // 2. Build execution plan.
        // Any structural planner diagnostic (PRINT without LABEL, unclosed session, ...)
        // indicates a malformed JOB and must fail closed regardless of IGNOREERROR.
        var plan = JobExecutionPlanner.Build(parseResult.Commands);
        if (plan.Diagnostics.Count > 0)
        {
            diagnostics.AddRange(plan.Diagnostics);
            return new JobRunReport(
                IsSuccess: false,
                TotalActions: plan.Actions.Count,
                ExecutedActions: 0,
                TotalLabelsPrinted: 0,
                Diagnostics: diagnostics,
                SpoolResults: spoolResults);
        }

        // Sandbox base directory: all JOB-honoring paths are confined to this directory.
        var workingDir = baseDirectory ?? Directory.GetCurrentDirectory();

        // 3. Execute file operations (CREATEFILE / DELETEFILE).
        // FIX H2: sandbox-confined; any escape or failure fails the whole run closed.
        if (!preflightOnly)
        {
            if (!ExecuteFileOperations(plan.FileActions, workingDir, diagnostics))
            {
                return new JobRunReport(false, plan.Actions.Count, executedActions, totalLabels, diagnostics, spoolResults);
            }
        }

        // FIX H3: enforce a global per-job label ceiling before allocating anything.
        if (plan.TotalLabelsToPrint > MaxLabelsPerJob)
        {
            diagnostics.Add($"JOB requests {plan.TotalLabelsToPrint} labels which exceeds the per-job maximum of {MaxLabelsPerJob}.");
            return new JobRunReport(false, plan.Actions.Count, executedActions, totalLabels, diagnostics, spoolResults);
        }

        // 4. Process each Print Action
        foreach (var action in plan.Actions)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // FIX M5: directives that are parsed into the plan but never applied by the
            // runner must fail loud rather than silently printing to the real printer.
            if (!string.IsNullOrWhiteSpace(action.PortRedirectPath))
            {
                diagnostics.Add($"Action {action.ActionIndex}: directive PORT (output redirection) is not supported; refusing to print to the physical printer.");
                return new JobRunReport(false, plan.Actions.Count, executedActions, totalLabels, diagnostics, spoolResults);
            }
            // CHANGE 2: SkipFirstSlots must be non-negative and, together with the copy count,
            // must stay under the per-action ceiling because skipped slots are physically fed.
            if (action.SkipFirstSlots < 0
                || action.SkipFirstSlots > MaxLabelsPerAction
                || action.TotalLabelCount > MaxLabelsPerAction
                || (long)action.SkipFirstSlots + action.TotalLabelCount > MaxLabelsPerAction)
            {
                diagnostics.Add($"Action {action.ActionIndex}: SkipFirstSlots {action.SkipFirstSlots} plus the requested labels exceeds the per-action maximum of {MaxLabelsPerAction}.");
                return new JobRunReport(false, plan.Actions.Count, executedActions, totalLabels, diagnostics, spoolResults);
            }

            // CHANGE 1: confine the LABEL template path to the JOB base directory OR an allowed root.
            if (!TryResolveTemplatePath(action.TemplatePath, workingDir, _allowedTemplateRoots, out var templatePath, out var pathError))
            {
                diagnostics.Add($"Action {action.ActionIndex}: {pathError}");
                return new JobRunReport(false, plan.Actions.Count, executedActions, totalLabels, diagnostics, spoolResults);
            }
            if (!File.Exists(templatePath))
            {
                // FIX M4: missing template is a safety-critical guard; halt regardless of IGNOREERROR.
                var errorMsg = $"Action {action.ActionIndex}: Label template file not found '{templatePath}'.";
                diagnostics.Add(errorMsg);
                return new JobRunReport(false, plan.Actions.Count, executedActions, totalLabels, diagnostics, spoolResults);
            }

            LabelTemplate template;
            try
            {
                template = await _files.LoadAsync(templatePath, cancellationToken);
            }
            catch (Exception ex)
            {
                // FIX M4: safety-critical; halt regardless of IGNOREERROR.
                var errorMsg = $"Action {action.ActionIndex}: Failed to load template '{templatePath}': {ex.Message}";
                diagnostics.Add(errorMsg);
                return new JobRunReport(false, plan.Actions.Count, executedActions, totalLabels, diagnostics, spoolResults);
            }

            // CHANGE 2: SETPRINTPARAM — map supported keys onto the real print settings.
            // Unsupported keys fail closed (never silently dropped).
            if (action.PrintParameters.Count > 0
                && !TryApplyPrintParameters(template.PrinterProfile, action.PrintParameters, out var paramError))
            {
                diagnostics.Add($"Action {action.ActionIndex}: {paramError}");
                return new JobRunReport(false, plan.Actions.Count, executedActions, totalLabels, diagnostics, spoolResults);
            }

            // Target printer
            var targetPrinter = !string.IsNullOrWhiteSpace(action.TargetPrinterName)
                ? action.TargetPrinterName
                : template.PrinterProfile?.PrinterName;

            if (string.IsNullOrWhiteSpace(targetPrinter))
            {
                // FIX M4: missing printer is safety-critical; halt regardless of IGNOREERROR.
                var errorMsg = $"Action {action.ActionIndex}: No printer specified in JOB script or label template.";
                diagnostics.Add(errorMsg);
                return new JobRunReport(false, plan.Actions.Count, executedActions, totalLabels, diagnostics, spoolResults);
            }

            // Verify printer queue health
            var queue = _queueLookup.Resolve(targetPrinter);
            if (!queue.CanSubmit)
            {
                // FIX M4: queue not ready is safety-critical; halt regardless of IGNOREERROR.
                var errorMsg = $"Action {action.ActionIndex}: Target printer queue '{targetPrinter}' is not ready ({queue.ErrorMessage}).";
                diagnostics.Add(errorMsg);
                return new JobRunReport(false, plan.Actions.Count, executedActions, totalLabels, diagnostics, spoolResults);
            }

            // Build data rows from variable record
            var varDict = (IReadOnlyDictionary<string, string>)action.Variables.Values.ToDictionary(
                kv => kv.Key,
                kv => kv.Value ?? string.Empty,
                StringComparer.OrdinalIgnoreCase);

            // FIX H3: compute the copy count in LONG and enforce a ceiling BEFORE allocating.
            // A hostile JOB could otherwise request a huge/overflowing count and crash the host.
            var totalCopiesLong = action.TotalLabelCount;
            if (totalCopiesLong < 1 || totalCopiesLong > MaxLabelsPerAction)
            {
                diagnostics.Add($"Action {action.ActionIndex}: requested label count {totalCopiesLong} is invalid or exceeds the per-action maximum of {MaxLabelsPerAction}.");
                return new JobRunReport(false, plan.Actions.Count, executedActions, totalLabels, diagnostics, spoolResults);
            }
            var totalCopiesForAction = (int)totalCopiesLong;
            var rows = Enumerable.Repeat(varDict, totalCopiesForAction).ToArray();

            // Run Preflight
            var dpi = (template.PrinterProfile != null && template.PrinterProfile.Dpi > 0) ? template.PrinterProfile.Dpi : template.Dpi;
            var preflight = _validator.Validate(template, rows, dpi, dpi, cancellationToken);
            if (!preflight.IsSuccess)
            {
                // FIX M4: preflight failure is safety-critical; halt regardless of IGNOREERROR.
                var errorMsg = $"Action {action.ActionIndex}: Preflight failed for '{templatePath}': {preflight.ToUserMessage()}";
                diagnostics.Add(errorMsg);
                return new JobRunReport(false, plan.Actions.Count, executedActions, totalLabels, diagnostics, spoolResults);
            }

            // Dispatch if not preflight-only
            if (!preflightOnly)
            {
                // CHANGE 2: SkipFirstSlots — the print stack emits one label per physical media
                // position (no multi-slot sheet concept), so "skip first N slots" is applied as
                // N leading blank label positions fed before the data labels. Preflight above
                // validates the data rows; the blank feed rows carry no variable data.
                var dispatchRows = BuildDispatchRows(varDict, totalCopiesForAction, action.SkipFirstSlots);

                try
                {
                    var printResult = await _printService.PrintRowsWithResultAsync(
                        template,
                        dispatchRows,
                        queue.CanonicalName,
                        action.JobName,
                        expectedOutputContractHash: null,
                        cancellationToken: cancellationToken);

                    spoolResults.Add(printResult);
                    if (!printResult.IsAccepted)
                    {
                        // FIX M4: spooler rejection is safety-critical; halt regardless of IGNOREERROR.
                        var errorMsg = $"Action {action.ActionIndex}: Spooler submission failed: {printResult.ErrorMessage}";
                        diagnostics.Add(errorMsg);
                        return new JobRunReport(false, plan.Actions.Count, executedActions, totalLabels, diagnostics, spoolResults);
                    }
                    else
                    {
                        executedActions++;
                        totalLabels += totalCopiesForAction;

                        var jobId = printResult.SpoolJobId.HasValue
                            ? printResult.SpoolJobId.Value.ToString()
                            : $"JOB-{action.ActionIndex}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
                        var manifestFingerprint = printResult.Manifest?.Fingerprint ?? printResult.ManifestFingerprint;
                        if (string.IsNullOrWhiteSpace(manifestFingerprint))
                        {
                            manifestFingerprint = "UNKNOWN";
                        }

                        if (_historyStore != null && !string.IsNullOrWhiteSpace(jobId))
                        {
                            _historyStore.TryAppend(
                                eventId: $"JOB-{action.ActionIndex}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
                                batchId: action.SessionId ?? $"SINGLE-{action.ActionIndex}",
                                jobId: jobId,
                                manifestFingerprint: manifestFingerprint,
                                out _,
                                out _);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // FIX M4: dispatch exception is safety-critical; halt regardless of IGNOREERROR.
                    var errorMsg = $"Action {action.ActionIndex}: Print dispatch exception: {ex.Message}";
                    diagnostics.Add(errorMsg);
                    return new JobRunReport(false, plan.Actions.Count, executedActions, totalLabels, diagnostics, spoolResults);
                }
            }
            else
            {
                executedActions++;
                totalLabels += totalCopiesForAction;
            }
        }

        // Safety takes priority over IGNOREERROR: any residual diagnostic fails the run.
        var success = diagnostics.Count == 0;
        return new JobRunReport(
            IsSuccess: success,
            TotalActions: plan.Actions.Count,
            ExecutedActions: executedActions,
            TotalLabelsPrinted: totalLabels,
            Diagnostics: diagnostics,
            SpoolResults: spoolResults);
    }

    /// <summary>
    /// Executes CREATEFILE / DELETEFILE operations confined to <paramref name="baseDirectory"/>.
    /// FIX H2: any path that is rooted or escapes the base directory fails the whole run closed.
    /// Returns false if any operation is rejected or fails.
    /// </summary>
    private static bool ExecuteFileOperations(
        IReadOnlyList<JobFileAction> fileActions,
        string baseDirectory,
        List<string> diagnostics)
    {
        foreach (var fa in fileActions)
        {
            if (!TryResolveSandboxedPath(fa.TargetPath, baseDirectory, out var fullPath, out var pathError))
            {
                diagnostics.Add($"File operation {fa.Action} '{fa.TargetPath}' rejected: {pathError}");
                return false;
            }

            try
            {
                if (fa.Action == FileOperationAction.Create)
                {
                    // Subdirectories are within the sandbox base; create only those.
                    var dir = Path.GetDirectoryName(fullPath);
                    if (!string.IsNullOrWhiteSpace(dir)) Directory.CreateDirectory(dir);
                    File.WriteAllText(fullPath, fa.Content ?? string.Empty);
                }
                else if (fa.Action == FileOperationAction.Delete)
                {
                    if (File.Exists(fullPath)) File.Delete(fullPath);
                }
            }
            catch (Exception ex)
            {
                diagnostics.Add($"File operation {fa.Action} '{fa.TargetPath}' failed: {ex.Message}");
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// FIX H2: resolves an untrusted JOB-supplied relative path against the JOB base directory,
    /// rejecting rooted/absolute paths and any path that escapes the base directory after
    /// normalization (containment check via a trailing-separator prefix).
    /// </summary>
    private static bool TryResolveSandboxedPath(string path, string baseDirectory, out string fullPath, out string? error)
    {
        fullPath = string.Empty;
        error = null;

        if (string.IsNullOrWhiteSpace(path))
        {
            error = "Path is empty.";
            return false;
        }

        if (Path.IsPathRooted(path))
        {
            error = $"Absolute/rooted paths are not permitted for untrusted JOB input: '{path}'.";
            return false;
        }

        string baseFull;
        try
        {
            baseFull = Path.GetFullPath(baseDirectory);
        }
        catch (Exception ex)
        {
            error = $"Invalid JOB base directory: {ex.Message}";
            return false;
        }

        string candidate;
        try
        {
            candidate = Path.GetFullPath(Path.Combine(baseFull, path));
        }
        catch (Exception ex)
        {
            error = $"Invalid path '{path}': {ex.Message}";
            return false;
        }

        var normalizedBase = baseFull.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

        if (!candidate.StartsWith(normalizedBase, StringComparison.OrdinalIgnoreCase))
        {
            error = $"Path '{path}' escapes the JOB base directory.";
            return false;
        }

        fullPath = candidate;
        return true;
    }

    /// <summary>
    /// CHANGE 1: resolves an untrusted JOB-supplied LABEL template path.
    /// Relative paths resolve under the base directory (same behavior as file operations).
    /// ABSOLUTE paths are honored (NiceLabel parity) ONLY when, after GetFullPath normalization,
    /// they are contained under the base directory OR under one of <paramref name="allowedRoots"/>.
    /// UNC (\\server\share), device (\\?\, \\.\) and drive-relative (C:rel) paths are always
    /// rejected. Absolute paths outside every allowed root fail closed.
    /// </summary>
    internal static bool TryResolveTemplatePath(
        string path,
        string baseDirectory,
        IReadOnlyList<string> allowedRoots,
        out string fullPath,
        out string? error)
    {
        fullPath = string.Empty;
        error = null;

        if (string.IsNullOrWhiteSpace(path))
        {
            error = "Path is empty.";
            return false;
        }

        string baseFull;
        try
        {
            baseFull = Path.GetFullPath(baseDirectory);
        }
        catch (Exception ex)
        {
            error = $"Invalid JOB base directory: {ex.Message}";
            return false;
        }

        if (!Path.IsPathRooted(path))
        {
            // Relative path: resolve under base and reject any '..' escape (unchanged policy).
            return TryResolveSandboxedPath(path, baseFull, out fullPath, out error);
        }

        // Preserve H2 protections for the newly-permitted absolute path space.
        if (IsUncOrDevicePath(path))
        {
            error = $"UNC/device paths are not permitted for JOB template input: '{path}'.";
            return false;
        }

        if (IsDriveRelativePath(path))
        {
            error = $"Drive-relative paths are not permitted for JOB template input: '{path}'.";
            return false;
        }

        string candidate;
        try
        {
            candidate = Path.GetFullPath(path);
        }
        catch (Exception ex)
        {
            error = $"Invalid template path '{path}': {ex.Message}";
            return false;
        }

        // The base directory is always an allowed root.
        if (IsContainedUnder(candidate, baseFull))
        {
            fullPath = candidate;
            return true;
        }

        foreach (var root in allowedRoots)
        {
            if (IsContainedUnder(candidate, root))
            {
                fullPath = candidate;
                return true;
            }
        }

        error = $"Absolute template path '{path}' is outside the JOB base directory and every allowed template root.";
        return false;
    }

    /// <summary>Containment check via a trailing-separator prefix, OrdinalIgnoreCase.</summary>
    private static bool IsContainedUnder(string candidateFullPath, string rootDirectory)
    {
        string rootFull;
        try
        {
            rootFull = Path.GetFullPath(rootDirectory);
        }
        catch
        {
            return false;
        }

        var normalizedRoot = rootFull.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        return candidateFullPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsUncOrDevicePath(string path)
    {
        if (path.Length < 2)
        {
            return false;
        }

        var c0 = path[0];
        var c1 = path[1];
        var sep0 = c0 == '\\' || c0 == '/';
        var sep1 = c1 == '\\' || c1 == '/';
        return sep0 && sep1; // \\server, //server, \\?\, \\.\
    }

    private static bool IsDriveRelativePath(string path)
    {
        // "C:rel" (drive letter + colon NOT followed by a separator) resolves against the
        // process current directory on that drive, which is not the JOB sandbox. Reject it.
        return path.Length >= 2
            && char.IsLetter(path[0])
            && path[1] == ':'
            && (path.Length == 2 || (path[2] != '\\' && path[2] != '/'));
    }

    /// <summary>
    /// CHANGE 2: maps SETPRINTPARAM key/values onto the real print settings sink.
    /// Supported keys (the only ones with an actual sink in the current print stack):
    ///   PRINTOFFSETX -> PrinterProfile.OffsetXMm (millimeters)
    ///   PRINTOFFSETY -> PrinterProfile.OffsetYMm (millimeters)
    /// These flow through PrintService.CreatePlan into the PrintRenderPlan and the
    /// EffectiveOutputContract, so they materially change the dispatched output.
    /// Any other key (PRINTSPEED, PRINTDARKNESS, PAPERBIN, PRINTERSETTINGS, unknown, ...) has
    /// NO sink in this stack and fails closed with a clear diagnostic (never silently dropped).
    /// </summary>
    public static bool TryApplyPrintParameters(
        PrinterProfile profile,
        IReadOnlyDictionary<string, string> printParameters,
        out string? error)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(printParameters);
        error = null;

        foreach (var kv in printParameters)
        {
            var key = (kv.Key ?? string.Empty).Trim();
            var raw = (kv.Value ?? string.Empty).Trim();
            switch (key.ToUpperInvariant())
            {
                case "PRINTOFFSETX":
                    if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var offsetX))
                    {
                        error = $"SETPRINTPARAM PRINTOFFSETX value '{raw}' is not a valid number.";
                        return false;
                    }
                    if (!double.IsFinite(offsetX))
                    {
                        error = $"SETPRINTPARAM PRINTOFFSETX value '{raw}' is not a finite number: only finite offsets may be applied.";
                        return false;
                    }
                    profile.OffsetXMm = offsetX;
                    break;

                case "PRINTOFFSETY":
                    if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var offsetY))
                    {
                        error = $"SETPRINTPARAM PRINTOFFSETY value '{raw}' is not a valid number.";
                        return false;
                    }
                    if (!double.IsFinite(offsetY))
                    {
                        error = $"SETPRINTPARAM PRINTOFFSETY value '{raw}' is not a finite number: only finite offsets may be applied.";
                        return false;
                    }
                    profile.OffsetYMm = offsetY;
                    break;

                default:
                    error = $"SETPRINTPARAM '{key}' is not supported: the current print stack has no sink for this parameter. Supported keys: PRINTOFFSETX, PRINTOFFSETY.";
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// CHANGE 2: builds the physical dispatch sequence for a print action.
    /// Because the print stack emits one label per media position (no multi-slot sheet concept),
    /// SkipFirstSlots is implemented as <paramref name="skipFirstSlots"/> leading BLANK label
    /// positions (empty variable record) that are fed before the <paramref name="copies"/> data
    /// labels. Total media positions = skipFirstSlots + copies; printed data labels = copies.
    /// </summary>
    public static IReadOnlyList<IReadOnlyDictionary<string, string>?> BuildDispatchRows(
        IReadOnlyDictionary<string, string> dataRow,
        int copies,
        int skipFirstSlots)
    {
        ArgumentNullException.ThrowIfNull(dataRow);
        var rows = new List<IReadOnlyDictionary<string, string>?>(Math.Max(0, skipFirstSlots) + Math.Max(0, copies));
        for (var i = 0; i < skipFirstSlots; i++)
        {
            rows.Add(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
        }

        for (var i = 0; i < copies; i++)
        {
            rows.Add(dataRow);
        }

        return rows;
    }
}
