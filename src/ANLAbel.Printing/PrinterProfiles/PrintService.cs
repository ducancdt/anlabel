using System.Printing;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using ANLAbel.Core.Enums;
using ANLAbel.Core.Geometry;
using ANLAbel.Core.Models;
using ANLAbel.Core.Printing;
using ANLAbel.Core.Scene;
using ANLAbel.Printing.RenderPipeline;
using WpfPrintDialog = System.Windows.Controls.PrintDialog;

namespace ANLAbel.Printing.PrinterProfiles;

public sealed class PrintService
{
    private readonly LabelVisualRenderer _renderer = new();
    private readonly PrintPreflightValidator _preflightValidator = new();
    private readonly SpoolJobMonitor _spoolJobMonitor;
    private readonly IPrinterQueueLookup _queueLookup;
    private readonly object _sceneCacheGate = new();
    private static readonly JsonSerializerOptions DispatchSnapshotJsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() }
    };
    private CachedSceneIdentity? _cachedScene;
    private long _sceneCompileCount;
    private long _sceneCacheHitCount;

    /// <summary>
    /// Diagnostic counters for preview/print scene reuse. A template is captured
    /// and hashed on every request for invalidation, but the expensive compiler
    /// result is reused while that immutable document hash is unchanged.
    /// </summary>
    public long SceneCompileCount => Interlocked.Read(ref _sceneCompileCount);
    public long SceneCacheHitCount => Interlocked.Read(ref _sceneCacheHitCount);

    public PrintService(
        ISpoolJobStatusReader? spoolJobStatusReader = null,
        IPrinterQueueLookup? queueLookup = null)
    {
        _spoolJobMonitor = new SpoolJobMonitor(spoolJobStatusReader ?? new WindowsSpoolJobStatusReader());
        _queueLookup = queueLookup ?? new WindowsPrinterQueueLookup();
    }

    /// <summary>
    /// Observes the queue after a successful spool submission. This method is
    /// intentionally separate from <see cref="PrintRowsWithResult"/> so a slow
    /// or disconnected printer never blocks dispatch and does not turn a queue
    /// status into a false physical-completion claim.
    /// </summary>
    public Task<SpoolJobMonitorResult> MonitorSpoolJobAsync(
        PrintJobResult printResult,
        TimeSpan? timeout = null,
        TimeSpan? pollInterval = null,
        CancellationToken cancellationToken = default,
        IProgress<SpoolJobObservation>? progress = null)
    {
        ArgumentNullException.ThrowIfNull(printResult);
        if (printResult.SpoolJobId is not int spoolJobId || spoolJobId <= 0)
        {
            var observation = new SpoolJobObservation(
                printResult.PrinterName,
                0,
                SpoolJobState.Unknown,
                "No spool job identity was captured; queue status cannot be correlated safely.",
                IsTerminal: true,
                ObservedAtUtc: DateTimeOffset.UtcNow);
            return Task.FromResult(new SpoolJobMonitorResult(observation, 0, TimeSpan.Zero, TimedOut: false));
        }

        return _spoolJobMonitor.MonitorAsync(
            printResult.PrinterName,
            spoolJobId,
            timeout ?? TimeSpan.FromSeconds(10),
            pollInterval ?? TimeSpan.FromMilliseconds(250),
            cancellationToken,
            progress);
    }

    /// <summary>
    /// Some drivers publish a spool job asynchronously after PrintDocument has
    /// returned. Resolve that identity on a worker without blocking the WPF
    /// dispatcher. Ambiguous or unavailable queue evidence leaves the original
    /// result untouched, so callers cannot monitor an unrelated job.
    /// </summary>
    public async Task<PrintJobResult> ResolveSpoolJobIdentityAsync(
        PrintJobResult printResult,
        TimeSpan? timeout = null,
        TimeSpan? pollInterval = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(printResult);
        if (printResult.HasSpoolIdentity
            || printResult.SubmissionEvidence is not { IsValid: true } evidence)
        {
            return printResult;
        }

        var boundedTimeout = timeout ?? TimeSpan.FromSeconds(1);
        var boundedPoll = pollInterval ?? TimeSpan.FromMilliseconds(100);
        if (boundedTimeout <= TimeSpan.Zero || boundedPoll < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), "Spool identity polling bounds are invalid.");
        }

        var started = Stopwatch.GetTimestamp();
        while (Stopwatch.GetElapsedTime(started) < boundedTimeout)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var currentJobs = await Task.Run(
                () => CaptureSpoolJobs(evidence.PrinterName),
                cancellationToken).ConfigureAwait(false);
            var spoolJobId = SpoolJobIdentityResolver.TryResolve(
                evidence.PreDispatchJobs,
                currentJobs,
                evidence.Description);
            if (spoolJobId is int resolvedId && resolvedId > 0)
            {
                return printResult with { SpoolJobId = resolvedId };
            }

            var remaining = boundedTimeout - Stopwatch.GetElapsedTime(started);
            if (remaining <= TimeSpan.Zero)
            {
                break;
            }

            var delay = boundedPoll <= remaining ? boundedPoll : remaining;
            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }

        return printResult;
    }

    public IReadOnlyList<PrintPreviewPage> CreatePreviewPages(LabelTemplate template, IEnumerable<IReadOnlyDictionary<string, string>?> rows)
    {
        return rows.Select((row, index) => CreatePreviewPage(template, row, index + 1))
            .ToArray();
    }

    /// <summary>
    /// Creates one preview visual on demand.  The legacy CreatePreviewPages API
    /// remains for callers that explicitly need every visual, while the WPF
    /// preview window uses this method so a 10,000-label batch does not eagerly
    /// allocate 10,000 visuals/bitmaps on the UI thread.
    /// </summary>
    public PrintPreviewPage CreatePreviewPage(LabelTemplate template, IReadOnlyDictionary<string, string>? row, int pageNumber)
        => CreatePreviewPage(template, row, pageNumber, plan: null);

    /// <summary>
    /// Creates a preview from an already-resolved plan.  Passing the effective
    /// plan is important for industrial labels: the preview must use the same
    /// DPI, offsets, rotation, media and imageable-area contract that dispatch
    /// will use, rather than silently falling back to the design-only plan.
    /// </summary>
    public PrintPreviewPage CreatePreviewPage(
        LabelTemplate template,
        IReadOnlyDictionary<string, string>? row,
        int pageNumber,
        PrintRenderPlan? plan)
    {
        if (plan is not null && !string.IsNullOrWhiteSpace(plan.DocumentHash))
        {
            var currentDocumentHash = CreatePlan(template, null).DocumentHash;
            if (!string.Equals(plan.DocumentHash, currentDocumentHash, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("The preview plan no longer matches the current template. Refresh preview before rendering or printing.");
            }
        }

        plan ??= CreatePlan(template, null);
        return new PrintPreviewPage
        {
            PageNumber = pageNumber,
            Visual = _renderer.Render(template, row, plan),
            WidthDip = MmConverter.MmToDip(plan.LabelWidthMm),
            HeightDip = MmConverter.MmToDip(plan.LabelHeightMm),
            DocumentHash = plan.DocumentHash,
            TextResourceFingerprint = plan.TextResourceFingerprint,
            ImageRasterFingerprint = plan.ImageRasterFingerprint,
            SceneHash = plan.SceneHash,
            SceneCompilationVerified = plan.SceneCompilationVerified,
            OutputContractHash = plan.OutputContractHash,
            OutputContractTicketVerified = plan.OutputContractTicketVerified,
            ThermalRasterGolden = plan.ThermalRasterGolden,
            DpiX = plan.DpiX,
            DpiY = plan.DpiY,
            DeviceGeometry = plan.DeviceGeometry,
            PrintableAreaVerified = plan.PrintableAreaVerified
        };
    }

    /// <summary>
    /// Returns the same design-time plan used by preview and preflight.  The
    /// effective print path adds the validated printer contract on top of this
    /// identity, so callers can compare a preview with a later print request.
    /// </summary>
    public PrintRenderPlan CreateDesignPlan(LabelTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);
        return CreatePlan(template, null);
    }

    /// <summary>
    /// Resolves the selected queue's effective PrintTicket without dispatching a
    /// job.  Preview/reprint workflows use this as the preparation boundary so
    /// the manifest contains the same driver contract that will be required at
    /// dispatch time (DPI, media and imageable-area evidence included in the
    /// immutable plan).
    /// </summary>
    public PrintRenderPlan CreateEffectivePlan(LabelTemplate template, string printerName)
    {
        ArgumentNullException.ThrowIfNull(template);
        if (string.IsNullOrWhiteSpace(printerName))
        {
            throw new InvalidOperationException("No printer queue was selected. Choose a verified industrial printer before preparing the print contract.");
        }

        var dialog = CreatePrintDialog(template, printerName);
        return CreateEffectivePlan(dialog, template);
    }

    /// <summary>
    /// Resolves the selected queue's effective driver contract away from the
    /// WPF dispatcher.  Printer drivers can block while opening a queue or
    /// merging capabilities; preview preparation must not make the designer
    /// appear hung before the actual print worker is even started.
    /// </summary>
    public async Task<PrintRenderPlan> CreateEffectivePlanAsync(
        LabelTemplate template,
        string printerName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(template);
        if (string.IsNullOrWhiteSpace(printerName))
        {
            throw new InvalidOperationException("No printer queue was selected. Choose a verified industrial printer before preparing the print contract.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        var templateSnapshot = CloneTemplateForDispatch(template);
        return await RunOnDedicatedStaAsync(
                () => CreateEffectivePlan(templateSnapshot, printerName),
                cancellationToken)
            .ConfigureAwait(true);
    }

    /// <summary>
    /// Builds a frozen drawing snapshot for preview rasterization. A frozen
    /// Drawing is free-threadable, so the UI can hand it to a dedicated STA
    /// raster worker without handing a live WPF Visual across threads.
    /// </summary>
    public (Drawing Drawing, double WidthDip, double HeightDip) CreatePreviewDrawing(
        LabelTemplate template,
        IReadOnlyDictionary<string, string>? row,
        PrintRenderPlan? plan = null)
    {
        var page = CreatePreviewPage(template, row, 1, plan);
        if (page.Visual is not DrawingVisual visual || visual.Drawing is not { } drawing)
        {
            throw new InvalidOperationException("Preview renderer did not produce a drawing snapshot.");
        }

        if (!drawing.CanFreeze)
        {
            var clone = drawing.Clone();
            if (!clone.CanFreeze)
            {
                throw new InvalidOperationException("Preview drawing contains a thread-affine resource.");
            }

            drawing = clone;
        }

        drawing.Freeze();
        return (drawing, page.WidthDip, page.HeightDip);
    }

    public PrintPreflightResult ValidateRows(LabelTemplate template, IReadOnlyList<IReadOnlyDictionary<string, string>?> rows)
    {
        // Best-known print DPI before an actual PrintTicket exists. The print path
        // validates again after the queue has produced an effective ticket.
        var plan = CreatePlan(template, null);
        return ValidateRows(template, rows, plan);
    }

    public PrintPreflightResult ValidateRows(LabelTemplate template, IReadOnlyList<IReadOnlyDictionary<string, string>?> rows, PrintRenderPlan plan)
    {
        return _preflightValidator.Validate(template, rows, plan.DpiX, plan.DpiY);
    }

    /// <summary>
    /// Runs preflight away from the WPF dispatcher and supports cancellation for
    /// large industrial batches.  The synchronous overloads remain available
    /// for the print paginator's fail-closed check immediately before dispatch.
    /// </summary>
    public Task<PrintPreflightResult> ValidateRowsAsync(
        LabelTemplate template,
        IReadOnlyList<IReadOnlyDictionary<string, string>?> rows,
        CancellationToken cancellationToken = default,
        IProgress<PrintPreflightProgress>? progress = null)
    {
        var plan = CreatePlan(template, null);
        return ValidateRowsAsync(template, rows, plan, cancellationToken, progress);
    }

    public Task<PrintPreflightResult> ValidateRowsAsync(
        LabelTemplate template,
        IReadOnlyList<IReadOnlyDictionary<string, string>?> rows,
        PrintRenderPlan plan,
        CancellationToken cancellationToken = default,
        IProgress<PrintPreflightProgress>? progress = null)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(rows);
        return Task.Run(
            () => _preflightValidator.Validate(template, rows, plan.DpiX, plan.DpiY, cancellationToken, progress),
            cancellationToken);
    }

    public bool PrintCurrentRow(LabelTemplate template, IReadOnlyDictionary<string, string>? row)
    {
        return PrintRows(template, new[] { row }, $"{template.Name} label");
    }

    public bool PrintAllRows(LabelTemplate template, IEnumerable<IReadOnlyDictionary<string, string>> rows)
    {
        return PrintRows(template, rows.Cast<IReadOnlyDictionary<string, string>?>(), $"{template.Name} labels");
    }

    public bool PrintRows(LabelTemplate template, IEnumerable<IReadOnlyDictionary<string, string>?> rows, string description)
    {
        return PrintRowsWithResult(template, rows, description).IsAccepted;
    }

    /// <summary>
    /// Dispatches a prepared print on a dedicated STA so a slow thermal driver
    /// cannot block the WPF dispatcher. The template and row dictionaries are
    /// serialized before the worker starts; edits made while the job is being
    /// submitted therefore cannot race the paginator or change the prepared
    /// document under it. Cancellation is honored before the worker begins;
    /// once a driver call has started it is allowed to finish and its result is
    /// still reported truthfully.
    /// </summary>
    public Task<PrintJobResult> PrintRowsWithResultAsync(
        LabelTemplate template,
        IEnumerable<IReadOnlyDictionary<string, string>?> rows,
        string printerName,
        string description,
        string? expectedOutputContractHash = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(rows);
        cancellationToken.ThrowIfCancellationRequested();

        var templateSnapshot = CloneTemplateForDispatch(template);
        var rowSnapshot = rows
            .Select(row => row is null
                ? null
                : (IReadOnlyDictionary<string, string>?)new Dictionary<string, string>(row, StringComparer.Ordinal))
            .ToArray();

        return RunOnDedicatedStaAsync(
            () => PrintRowsWithResult(
                templateSnapshot,
                rowSnapshot,
                printerName,
                description,
                expectedOutputContractHash),
            cancellationToken);
    }

    public PrintJobResult PrintRowsWithResult(LabelTemplate template, IEnumerable<IReadOnlyDictionary<string, string>?> rows, string description)
    {
        try
        {
            var dialog = CreatePrintDialog(template);
            var initialQueueName = dialog.PrintQueue?.FullName;
            if (dialog.ShowDialog() != true)
            {
                return new PrintJobResult(PrintJobOutcome.Cancelled, template.PrinterProfile.PrinterName, description, 0);
            }

            var selectedQueueName = dialog.PrintQueue?.FullName;
            selectedQueueName = RequireExplicitInteractiveQueue(
                template.PrinterProfile.PrinterName,
                initialQueueName,
                selectedQueueName);
            return PrintRowsWithDialog(template, rows, dialog, description,
                selectedQueueName);
        }
        catch (Exception ex)
        {
            return CreateFailedResult(template.PrinterProfile.PrinterName, description, ex);
        }
    }

    public void PrintRows(LabelTemplate template, IEnumerable<IReadOnlyDictionary<string, string>?> rows, WpfPrintDialog dialog, string description)
    {
        _ = PrintRowsWithResult(template, rows, dialog, description);
    }

    public PrintJobResult PrintRowsWithResult(LabelTemplate template, IEnumerable<IReadOnlyDictionary<string, string>?> rows, WpfPrintDialog dialog, string description)
    {
        var selectedQueueName = dialog.PrintQueue?.FullName;
        return PrintRowsWithDialog(template, rows, dialog, description,
            string.IsNullOrWhiteSpace(selectedQueueName) ? template.PrinterProfile.PrinterName : selectedQueueName);
    }

    public void PrintRows(LabelTemplate template, IEnumerable<IReadOnlyDictionary<string, string>?> rows, string printerName, string description)
    {
        _ = PrintRowsWithResult(template, rows, printerName, description);
    }

    public PrintJobResult PrintRowsWithResult(
        LabelTemplate template,
        IEnumerable<IReadOnlyDictionary<string, string>?> rows,
        string printerName,
        string description,
        string? expectedOutputContractHash = null)
    {
        if (string.IsNullOrWhiteSpace(printerName))
        {
            return new PrintJobResult(
                PrintJobOutcome.Failed,
                string.Empty,
                description,
                0,
                "No printer queue was explicitly selected. Choose a verified industrial printer before printing; the Windows default queue will not be used implicitly.");
        }

        try
        {
            var dialog = CreatePrintDialog(template, printerName);
            return PrintRowsWithDialog(template, rows, dialog, description, printerName, expectedOutputContractHash);
        }
        catch (Exception ex)
        {
            return CreateFailedResult(printerName, description, ex);
        }
    }

    private static PrintJobResult CreateFailedResult(string? printerName, string description, Exception exception)
    {
        var message = string.IsNullOrWhiteSpace(exception.Message)
            ? "The selected printer queue is unavailable."
            : exception.Message;
        return new PrintJobResult(PrintJobOutcome.Failed, printerName ?? string.Empty, description, 0, message);
    }

    private static LabelTemplate CloneTemplateForDispatch(LabelTemplate template)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(template, DispatchSnapshotJsonOptions);
        return JsonSerializer.Deserialize<LabelTemplate>(bytes, DispatchSnapshotJsonOptions)
            ?? throw new InvalidOperationException("The print snapshot could not be reconstructed before dispatch.");
    }

    private static Task<T> RunOnDedicatedStaAsync<T>(Func<T> operation, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<T>(cancellationToken);
        }

        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                completion.TrySetResult(operation());
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                completion.TrySetCanceled(cancellationToken);
            }
            catch (Exception ex)
            {
                completion.TrySetException(ex);
            }
        })
        {
            IsBackground = true,
            Name = "ANLAbel.PrintDispatch.STA"
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return completion.Task;
    }

    private PrintJobResult PrintRowsWithDialog(
        LabelTemplate template,
        IEnumerable<IReadOnlyDictionary<string, string>?> rows,
        WpfPrintDialog dialog,
        string description,
        string? requestedPrinterName,
        string? expectedOutputContractHash = null)
    {
        var rowList = rows.ToArray();
        if (rowList.Length == 0)
        {
            return new PrintJobResult(PrintJobOutcome.Cancelled, requestedPrinterName ?? string.Empty, description, 0);
        }

        EnsurePrintQueue(dialog, requestedPrinterName);
        if (dialog.PrintQueue is null)
        {
            throw new InvalidOperationException("No printer is selected.");
        }

        ApplyTemplateTicket(dialog, template);
        // Keep the operator-requested ticket as the preparation identity. The
        // first merge replaces dialog.PrintTicket with the driver's validated
        // ticket; reusing that mutated property for the last-mile comparison
        // would make RequestedTicketHash change even when the effective output
        // contract stayed identical.
        var requestedTicket = dialog.PrintTicket
            ?? dialog.PrintQueue.DefaultPrintTicket
            ?? new PrintTicket();
        var requestedTicketHash = HashPrintTicket(requestedTicket);
        var plan = CreateEffectivePlan(dialog, template, requestedTicket, requestedTicketHash);
        if (!PrintContractGuard.Matches(
                expectedOutputContractHash,
                plan.OutputContractHash,
                plan.OutputContractTicketVerified))
        {
            throw new InvalidOperationException(
                "The printer output contract is missing verified driver-ticket evidence or changed after preview (DPI, media, margins or driver settings). Reopen preview and review the updated contract before printing.");
        }

        EnsureSceneCompilation(plan);
        var preflight = ValidateRows(template, rowList, plan);
        if (!preflight.IsSuccess)
        {
            throw new InvalidOperationException(preflight.ToUserMessage());
        }

        // Re-read the driver contract after preflight and immediately before
        // paginator/dispatch creation. A queue, DPI, media or ticket change in
        // this window invalidates the prepared output; no label is submitted.
        plan = RevalidateDispatchPlan(dialog, template, plan, requestedTicket, requestedTicketHash, expectedOutputContractHash);
        var pageSize = CreatePageSize(plan);
        var knownSpoolJobs = CaptureSpoolJobs(dialog.PrintQueue);
        var paginator = new VisualDocumentPaginator(
            rowList.Length,
            pageSize,
            pageNumber => _renderer.Render(template, rowList[pageNumber], plan));

        dialog.PrintDocument(paginator, description);
        var spoolJobId = TryFindNewSpoolJobId(dialog.PrintQueue, knownSpoolJobs, description);
        var result = new PrintJobResult(PrintJobOutcome.SpoolAccepted, dialog.PrintQueue.FullName, description, rowList.Length, DpiX: plan.DpiX, DpiY: plan.DpiY, PrintableAreaVerified: plan.PrintableAreaVerified, SpoolJobId: spoolJobId, OutputContractHash: plan.OutputContractHash, OutputContractTicketVerified: plan.OutputContractTicketVerified, DocumentHash: plan.DocumentHash, SceneHash: plan.SceneHash, SceneCompilationVerified: plan.SceneCompilationVerified, TextResourceFingerprint: plan.TextResourceFingerprint, ImageRasterFingerprint: plan.ImageRasterFingerprint)
        {
            ThermalRasterGoldenFingerprint = plan.ThermalRasterGolden?.Fingerprint ?? string.Empty,
            SubmissionEvidence = knownSpoolJobs is null
                ? null
                : new SpoolJobSubmissionEvidence(
                    dialog.PrintQueue.FullName,
                    description,
                    knownSpoolJobs,
                    DateTimeOffset.UtcNow)
        };
        return AttachSupportEvidence(result, plan, durableJobIdHint: description);
    }

    public bool PrintCalibration(LabelTemplate template)
    {
        return PrintCalibrationWithResult(template).IsAccepted;
    }

    public PrintJobResult PrintCalibrationWithResult(LabelTemplate template)
    {
        WpfPrintDialog dialog;
        try
        {
            dialog = CreatePrintDialog(template);
            var initialQueueName = dialog.PrintQueue?.FullName;
            if (dialog.ShowDialog() != true)
            {
                return new PrintJobResult(PrintJobOutcome.Cancelled, template.PrinterProfile.PrinterName, $"{template.Name} calibration", 0);
            }

            var selectedQueueName = RequireExplicitInteractiveQueue(
                template.PrinterProfile.PrinterName,
                initialQueueName,
                dialog.PrintQueue?.FullName);
            EnsurePrintQueue(dialog, selectedQueueName);
            return PrintCalibrationWithDialog(template, dialog);
        }
        catch (Exception ex)
        {
            return new PrintJobResult(PrintJobOutcome.Failed, template.PrinterProfile.PrinterName, $"{template.Name} calibration", 0, ex.Message);
        }
    }

    /// <summary>
    /// Keeps queue selection on the UI thread, then snapshots and submits the
    /// calibration page on a dedicated STA. Thermal drivers may block during
    /// PrintDocument just like a normal batch; calibration must not freeze the
    /// designer after the operator has selected a queue.
    /// </summary>
    public async Task<PrintJobResult> PrintCalibrationWithResultAsync(
        LabelTemplate template,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(template);
        cancellationToken.ThrowIfCancellationRequested();

        var dialog = CreatePrintDialog(template);
        var initialQueueName = dialog.PrintQueue?.FullName;
        if (dialog.ShowDialog() != true)
        {
            return new PrintJobResult(PrintJobOutcome.Cancelled, template.PrinterProfile.PrinterName, $"{template.Name} calibration", 0);
        }

        var selectedQueueName = RequireExplicitInteractiveQueue(
            template.PrinterProfile.PrinterName,
            initialQueueName,
            dialog.PrintQueue?.FullName);
        var templateSnapshot = CloneTemplateForDispatch(template);
        return await RunOnDedicatedStaAsync(
                () => PrintCalibrationWithQueue(templateSnapshot, selectedQueueName),
                cancellationToken)
            .ConfigureAwait(true);
    }

    private PrintJobResult PrintCalibrationWithQueue(LabelTemplate template, string printerName)
    {
        var dialog = CreatePrintDialog(template, printerName);
        return PrintCalibrationWithDialog(template, dialog);
    }

    private PrintJobResult PrintCalibrationWithDialog(LabelTemplate template, WpfPrintDialog dialog)
    {
        ApplyTemplateTicket(dialog, template);
        var requestedTicket = dialog.PrintTicket
            ?? dialog.PrintQueue?.DefaultPrintTicket
            ?? new PrintTicket();
        var requestedTicketHash = HashPrintTicket(requestedTicket);
        var plan = CreateEffectivePlan(dialog, template, requestedTicket, requestedTicketHash);
        EnsureSceneCompilation(plan);
        plan = RevalidateDispatchPlan(dialog, template, plan, requestedTicket, requestedTicketHash, expectedOutputContractHash: null);
        var queue = dialog.PrintQueue ?? throw new InvalidOperationException("No printer is selected.");
        var knownSpoolJobs = CaptureSpoolJobs(queue);
        var paginator = new VisualDocumentPaginator(
            1,
            CreatePageSize(plan),
            _ => _renderer.RenderCalibration(plan));

        dialog.PrintDocument(paginator, $"{template.Name} calibration");
        var spoolJobId = TryFindNewSpoolJobId(queue, knownSpoolJobs, $"{template.Name} calibration");
        var result = new PrintJobResult(PrintJobOutcome.SpoolAccepted, queue.FullName, $"{template.Name} calibration", 1, DpiX: plan.DpiX, DpiY: plan.DpiY, PrintableAreaVerified: plan.PrintableAreaVerified, SpoolJobId: spoolJobId, OutputContractHash: plan.OutputContractHash, OutputContractTicketVerified: plan.OutputContractTicketVerified, DocumentHash: plan.DocumentHash, SceneHash: plan.SceneHash, SceneCompilationVerified: plan.SceneCompilationVerified, TextResourceFingerprint: plan.TextResourceFingerprint, ImageRasterFingerprint: plan.ImageRasterFingerprint)
        {
            ThermalRasterGoldenFingerprint = plan.ThermalRasterGolden?.Fingerprint ?? string.Empty,
            SubmissionEvidence = knownSpoolJobs is null
                ? null
                : new SpoolJobSubmissionEvidence(
                    queue.FullName,
                    $"{template.Name} calibration",
                    knownSpoolJobs,
                    DateTimeOffset.UtcNow)
        };
        return AttachSupportEvidence(result, plan, durableJobIdHint: $"{template.Name} calibration");
    }

    /// <summary>
    /// Builds a redacted support evidence record on the shipped print path so
    /// preparation→dispatch→queue outcomes can be exported without raw label
    /// payloads.  Spool acceptance never sets physical verification.
    /// </summary>
    public static PrintJobResult AttachSupportEvidence(
        PrintJobResult result,
        PrintRenderPlan plan,
        string durableJobIdHint)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(plan);

        var jobId = !string.IsNullOrWhiteSpace(result.ManifestFingerprint)
            ? result.ManifestFingerprint
            : !string.IsNullOrWhiteSpace(result.DocumentHash)
                ? $"{result.DocumentHash}:{result.Outcome}"
                : durableJobIdHint;
        if (string.IsNullOrWhiteSpace(jobId))
        {
            jobId = $"print:{result.PrinterName}:{result.Outcome}";
        }

        var bundle = PrintSupportEvidenceContract.Build(
            jobId: jobId,
            queueName: result.PrinterName,
            spoolJobId: result.SpoolJobId?.ToString(System.Globalization.CultureInfo.InvariantCulture),
            documentHash: result.DocumentHash,
            sceneHash: result.SceneHash,
            outputContractHash: result.OutputContractHash,
            manifestFingerprint: result.ManifestFingerprint,
            textResourceFingerprint: result.TextResourceFingerprint,
            imageRasterFingerprint: result.ImageRasterFingerprint,
            thermalGoldenFingerprint: result.ThermalRasterGoldenFingerprint,
            outcome: result.Outcome.ToString(),
            physicalOutputVerified: result.IsPhysicalCompletionVerified,
            metadata: new[]
            {
                new KeyValuePair<string, string?>("labelCount", result.LabelCount.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new KeyValuePair<string, string?>("description", result.Description),
                new KeyValuePair<string, string?>("dpiX", result.DpiX.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new KeyValuePair<string, string?>("dpiY", result.DpiY.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new KeyValuePair<string, string?>("printableAreaVerified", result.PrintableAreaVerified ? "1" : "0"),
                new KeyValuePair<string, string?>("sceneCompilationVerified", result.SceneCompilationVerified ? "1" : "0")
            },
            lifecycleStates: new[] { "Preparing", "PreflightPassed", result.Outcome.ToString() });

        return result with
        {
            SupportEvidenceFingerprint = bundle.EvidenceFingerprint,
            SupportEvidenceJson = PrintSupportEvidenceContract.ToCanonicalJson(bundle)
        };
    }

    public WpfPrintDialog CreatePrintDialog(LabelTemplate template)
    {
        return CreatePrintDialog(template, template.PrinterProfile.PrinterName);
    }

    public WpfPrintDialog CreatePrintDialog(LabelTemplate template, string? printerName)
    {
        var dialog = new WpfPrintDialog();
        EnsurePrintQueue(dialog, printerName);
        ApplyTemplateTicket(dialog, template);
        return dialog;
    }

    private void EnsurePrintQueue(WpfPrintDialog dialog, string? printerName)
    {
        if (!string.IsNullOrWhiteSpace(printerName))
        {
            var lookup = _queueLookup.Resolve(printerName);
            if (!lookup.IsAvailable)
            {
                throw new InvalidOperationException(
                    $"Requested printer '{printerName}' is unavailable. {lookup.ErrorMessage} Select a verified queue before printing; the Windows default queue will not be used implicitly.");
            }

            try
            {
                using var server = new LocalPrintServer();
                dialog.PrintQueue = server.GetPrintQueue(printerName);
                if (!string.Equals(dialog.PrintQueue.FullName, lookup.CanonicalName, StringComparison.OrdinalIgnoreCase)
                    || !string.Equals(dialog.PrintQueue.FullName, printerName, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"Windows resolved '{printerName}' to a different printer queue '{dialog.PrintQueue.FullName}'.");
                }

                return;
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                throw new InvalidOperationException($"Requested printer '{printerName}' is unavailable. Select a verified queue before printing.", ex);
            }
        }

        if (dialog.PrintQueue is not null)
        {
            return;
        }

        // Never fill an unconfigured dialog with Windows' default queue. A
        // default office printer can be a materially wrong destination for an
        // industrial label, and the operator would see no evidence that the
        // queue was inferred. Leave the queue empty so the print dialog can ask
        // for an explicit selection; dispatch paths then fail closed if none
        // was selected.
    }

    private static string RequireExplicitInteractiveQueue(
        string? configuredPrinterName,
        string? initialQueueName,
        string? selectedQueueName)
    {
        if (!string.IsNullOrWhiteSpace(configuredPrinterName))
        {
            return string.IsNullOrWhiteSpace(selectedQueueName)
                ? configuredPrinterName
                : selectedQueueName;
        }

        if (string.IsNullOrWhiteSpace(selectedQueueName))
        {
            throw new InvalidOperationException(
                "No printer queue was explicitly selected. Choose a verified industrial printer before printing; the Windows default queue will not be used implicitly.");
        }

        // WPF PrintDialog opens on the Windows default queue even when the
        // template has no saved printer. If the operator accepts that same
        // queue without changing it, there is no explicit industrial-queue
        // evidence; require a deliberate queue selection instead.
        if (!string.IsNullOrWhiteSpace(initialQueueName)
            && string.Equals(initialQueueName, selectedQueueName, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Printer queue '{selectedQueueName}' was inherited from Windows' default and was not explicitly selected. Choose a verified industrial queue or save it in Printer Setup before printing.");
        }

        return selectedQueueName;
    }

    private PrintRenderPlan CreatePlan(
        LabelTemplate template,
        PrintTicket? ticket,
        PageImageableArea? imageableArea = null,
        double? mediaWidthDip = null,
        double? mediaHeightDip = null)
    {
        HydrateImageMetadata(template);
        var scene = CompileSceneIdentity(template);
        var defaultDpi = template.PrinterProfile.Dpi > 0 ? template.PrinterProfile.Dpi : template.Dpi;
        var dpiX = defaultDpi;
        var dpiY = defaultDpi;
        if (ticket?.PageResolution?.X is not null && ticket.PageResolution.X.Value > 0)
        {
            dpiX = ticket.PageResolution.X.Value;
        }

        if (ticket?.PageResolution?.Y is not null && ticket.PageResolution.Y.Value > 0)
        {
            dpiY = ticket.PageResolution.Y.Value;
        }

        var printableArea = imageableArea is null
            ? new PrintableAreaValidation(false, false, "imageable-area-missing")
            : PrintableAreaContract.Validate(
                imageableArea.OriginWidth,
                imageableArea.OriginHeight,
                imageableArea.ExtentWidth,
                imageableArea.ExtentHeight,
                mediaWidthDip,
                mediaHeightDip);

        return new PrintRenderPlan
        {
            Dpi = dpiX,
            DpiX = dpiX,
            DpiY = dpiY,
            DeviceGeometry = DeviceRenderGeometry.Create(
                template.WidthMm,
                template.HeightMm,
                dpiX,
                dpiY,
                imageableArea?.OriginWidth ?? 0,
                imageableArea?.OriginHeight ?? 0,
                imageableArea?.ExtentWidth ?? 0,
                imageableArea?.ExtentHeight ?? 0,
                printableArea.IsVerified),
            LabelWidthMm = template.WidthMm,
            LabelHeightMm = template.HeightMm,
            GapMm = template.PrinterProfile.GapMm > 0 ? template.PrinterProfile.GapMm : template.GapMm,
            MarginMm = template.MarginMm,
            OffsetXMm = template.PrinterProfile.OffsetXMm,
            OffsetYMm = template.PrinterProfile.OffsetYMm,
            Rotated180 = template.PrinterProfile.Rotated180,
            MediaType = template.PrinterProfile.MediaType,
            FeedDirection = template.PrinterProfile.FeedDirection,
            ScaleX = template.PrinterProfile.ScaleX == 0 ? 1 : template.PrinterProfile.ScaleX,
            ScaleY = template.PrinterProfile.ScaleY == 0 ? 1 : template.PrinterProfile.ScaleY,
            PrintableOriginXDip = imageableArea?.OriginWidth ?? 0,
            PrintableOriginYDip = imageableArea?.OriginHeight ?? 0,
            PrintableWidthDip = imageableArea?.ExtentWidth ?? 0,
            PrintableHeightDip = imageableArea?.ExtentHeight ?? 0,
            PrintableAreaVerified = printableArea.IsVerified,
            DocumentHash = scene.DocumentHash,
            TextResourceFingerprint = scene.TextResourceFingerprint,
            ImageRasterFingerprint = scene.ImageRasterFingerprint,
            SceneHash = scene.SceneHash,
            SceneCompilationVerified = scene.Verified,
            SceneDiagnostics = scene.Diagnostics,
            CompiledScene = scene.Verified ? scene.Compilation : null
        };
    }

    private (string DocumentHash, string TextResourceFingerprint, string ImageRasterFingerprint, string SceneHash, bool Verified, string Diagnostics, SceneCompilationResult Compilation) CompileSceneIdentity(LabelTemplate template)
    {
        var snapshot = DocumentSnapshot.Capture(template);
        lock (_sceneCacheGate)
        {
            if (_cachedScene is { } cached
                && string.Equals(cached.DocumentHash, snapshot.DocumentHash, StringComparison.Ordinal))
            {
                Interlocked.Increment(ref _sceneCacheHitCount);
                return cached.ToTuple();
            }

            var compilation = SceneCompiler.Compile(snapshot);
            var errors = compilation.Diagnostics
                .Where(item => item.Severity == SceneDiagnosticSeverity.Error)
                .Select(item => $"{item.Code}: {item.Message}")
                .ToArray();
            var identity = new CachedSceneIdentity(
                snapshot.DocumentHash,
                snapshot.TextResourceFingerprint,
                snapshot.ImageRasterFingerprint,
                compilation.Succeeded ? compilation.SceneHash : string.Empty,
                compilation.Succeeded,
                string.Join(" | ", errors),
                compilation);
            _cachedScene = identity;
            Interlocked.Increment(ref _sceneCompileCount);
            return identity.ToTuple();
        }
    }

    private static void HydrateImageMetadata(LabelTemplate template)
    {
        foreach (var item in template.Objects.Where(item => item.Type == ObjectType.Image))
        {
            if (string.IsNullOrWhiteSpace(item.ImageDataBase64))
            {
                item.ImagePixelWidth = 0;
                item.ImagePixelHeight = 0;
                continue;
            }

            if (!ImageRasterizer.TryGetPixelDimensions(item.ImageDataBase64, out var width, out var height))
            {
                continue;
            }

            // A non-zero mismatch means the embedded bytes changed without the
            // resource metadata being updated. Refuse to silently rewrite the
            // identity; preflight will surface the same remediation path.
            if ((item.ImagePixelWidth > 0 && item.ImagePixelWidth != width)
                || (item.ImagePixelHeight > 0 && item.ImagePixelHeight != height))
            {
                throw new InvalidOperationException(
                    $"Image '{item.Name}' metadata does not match its embedded payload ({item.ImagePixelWidth}x{item.ImagePixelHeight} vs {width}x{height}). Reinsert the image before printing.");
            }

            item.ImagePixelWidth = width;
            item.ImagePixelHeight = height;
        }
    }

    private static void EnsureSceneCompilation(PrintRenderPlan plan)
    {
        if (plan.SceneCompilationVerified)
        {
            return;
        }

        var detail = string.IsNullOrWhiteSpace(plan.SceneDiagnostics)
            ? "the scene compiler did not produce a verified scene hash"
            : plan.SceneDiagnostics;
        throw new InvalidOperationException($"Printing stopped because the label design is invalid ({detail}). Fix the design and try again.");
    }

    private PrintRenderPlan RevalidateDispatchPlan(
        WpfPrintDialog dialog,
        LabelTemplate template,
        PrintRenderPlan preparedPlan,
        PrintTicket? requestedTicket,
        string? requestedTicketHash,
        string? expectedOutputContractHash)
    {
        PrintRenderPlan finalPlan;
        try
        {
            finalPlan = CreateEffectivePlan(dialog, template, requestedTicket, requestedTicketHash);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Printing stopped because the printer output contract could not be revalidated immediately before dispatch; no label was submitted.",
                ex);
        }

        // Prefer field-level evaluation when both plans still carry the full
        // EffectiveOutputContract so DPI/media/ticket/imageable drift is named.
        // Fall back to fingerprint-only comparison for legacy callers that only
        // retained hashes.
        var revalidation = preparedPlan.EffectiveOutput is not null && finalPlan.EffectiveOutput is not null
            ? DispatchRevalidationContract.Evaluate(
                preparedPlan.DocumentHash,
                preparedPlan.EffectiveOutput,
                preparedPlan.OutputContractTicketVerified,
                finalPlan.DocumentHash,
                finalPlan.EffectiveOutput,
                finalPlan.OutputContractTicketVerified,
                expectedOutputContractHash)
            : DispatchRevalidationContract.EvaluateFingerprints(
                preparedPlan.DocumentHash,
                preparedPlan.OutputContractHash,
                preparedPlan.OutputContractTicketVerified,
                finalPlan.DocumentHash,
                finalPlan.OutputContractHash,
                finalPlan.OutputContractTicketVerified,
                expectedOutputContractHash);
        if (!revalidation.SubmissionAllowed)
        {
            throw new InvalidOperationException(revalidation.Diagnostic);
        }

        EnsureSceneCompilation(finalPlan);
        return finalPlan;
    }

    private sealed record CachedSceneIdentity(
        string DocumentHash,
        string TextResourceFingerprint,
        string ImageRasterFingerprint,
        string SceneHash,
        bool Verified,
        string Diagnostics,
        SceneCompilationResult Compilation)
    {
        public (string DocumentHash, string TextResourceFingerprint, string ImageRasterFingerprint, string SceneHash, bool Verified, string Diagnostics, SceneCompilationResult Compilation) ToTuple()
            => (DocumentHash, TextResourceFingerprint, ImageRasterFingerprint, SceneHash, Verified, Diagnostics, Compilation);
    }

    private PrintRenderPlan CreateEffectivePlan(
        WpfPrintDialog dialog,
        LabelTemplate template,
        PrintTicket? requestedTicketOverride = null,
        string? requestedTicketHashOverride = null)
    {
        if (dialog.PrintQueue is null)
        {
            throw new InvalidOperationException("No printer is selected.");
        }

        var requestedTicket = requestedTicketOverride
            ?? dialog.PrintTicket
            ?? dialog.PrintQueue.DefaultPrintTicket
            ?? new PrintTicket();
        var validation = dialog.PrintQueue.MergeAndValidatePrintTicket(dialog.PrintQueue.UserPrintTicket, requestedTicket);
        if (validation.ValidatedPrintTicket is null)
        {
            throw new InvalidOperationException("The printer driver did not return a valid effective PrintTicket.");
        }

        if (validation.ConflictStatus != ConflictStatus.NoConflict)
        {
            throw new InvalidOperationException("The printer driver reported a PrintTicket conflict. Review printer settings before printing.");
        }

        dialog.PrintTicket = validation.ValidatedPrintTicket;
        EnsureEffectiveMediaSize(validation.ValidatedPrintTicket, template);
        PrintCapabilities? capabilities = null;
        try
        {
            capabilities = dialog.PrintQueue.GetPrintCapabilities(validation.ValidatedPrintTicket);
        }
        catch
        {
            // Some thermal drivers do not expose capabilities. Keep the effective
            // ticket, but mark printable bounds unverified in the plan.
        }

        var effectiveMedia = validation.ValidatedPrintTicket.PageMediaSize;
        double? mediaWidthDip = effectiveMedia?.Width;
        double? mediaHeightDip = effectiveMedia?.Height;
        var imageableArea = capabilities?.PageImageableArea;
        if (imageableArea is not null)
        {
            var areaValidation = PrintableAreaContract.Validate(
                imageableArea.OriginWidth,
                imageableArea.OriginHeight,
                imageableArea.ExtentWidth,
                imageableArea.ExtentHeight,
                mediaWidthDip,
                mediaHeightDip);
            if (!areaValidation.HasUsableGeometry)
            {
                throw new InvalidOperationException(
                    $"The printer driver returned an invalid imageable area ({areaValidation.FailureCode}). Review the selected stock/driver before printing.");
            }
        }

        var plan = CreatePlan(
            template,
            validation.ValidatedPrintTicket,
            imageableArea,
            mediaWidthDip,
            mediaHeightDip);
        var dpiValidation = EffectiveDpiContract.Validate(plan.DpiX, plan.DpiY);
        if (!dpiValidation.IsValid)
        {
            throw new InvalidOperationException(
                $"The printer driver returned an invalid effective DPI ({dpiValidation.FailureCode}). Review the printer profile/driver before printing.");
        }

        var contract = new EffectiveOutputContract
        {
            PrinterName = dialog.PrintQueue.FullName,
            RequestedTicketHash = requestedTicketHashOverride ?? HashPrintTicket(requestedTicket),
            EffectiveTicketHash = HashPrintTicket(validation.ValidatedPrintTicket),
            DpiX = plan.DpiX,
            DpiY = plan.DpiY,
            LabelWidthDots = plan.DeviceGeometry.LabelWidthDots,
            LabelHeightDots = plan.DeviceGeometry.LabelHeightDots,
            PrintableOriginXDots = plan.DeviceGeometry.PrintableOriginXDots,
            PrintableOriginYDots = plan.DeviceGeometry.PrintableOriginYDots,
            PrintableWidthDots = plan.DeviceGeometry.PrintableWidthDots,
            PrintableHeightDots = plan.DeviceGeometry.PrintableHeightDots,
            LabelWidthMm = plan.LabelWidthMm,
            LabelHeightMm = plan.LabelHeightMm,
            GapMm = plan.GapMm,
            MarginMm = plan.MarginMm,
            OffsetXMm = plan.OffsetXMm,
            OffsetYMm = plan.OffsetYMm,
            ScaleX = plan.ScaleX,
            ScaleY = plan.ScaleY,
            MediaType = plan.MediaType,
            FeedDirection = plan.FeedDirection,
            Rotated180 = plan.Rotated180,
            PrintableOriginXDip = plan.PrintableOriginXDip,
            PrintableOriginYDip = plan.PrintableOriginYDip,
            PrintableWidthDip = plan.PrintableWidthDip,
            PrintableHeightDip = plan.PrintableHeightDip,
            PrintableAreaVerified = plan.PrintableAreaVerified
        };
        // Retain the full contract on the plan so last-mile revalidation can
        // name DPI/media/ticket/imageable drift instead of only a hash mismatch.
        return plan.WithEffectiveOutput(contract);
    }

    private static void EnsureEffectiveMediaSize(PrintTicket ticket, LabelTemplate template)
    {
        var media = ticket.PageMediaSize;
        if (media?.Width is not double effectiveWidthDip
            || media.Height is not double effectiveHeightDip
            || effectiveWidthDip <= 0
            || effectiveHeightDip <= 0)
        {
            // A number of thermal drivers omit custom media dimensions even
            // after accepting the ticket. Keep the contract explicitly
            // unverified through PrintableAreaVerified instead of inventing a
            // size; the queue hash is still retained for later reconciliation.
            return;
        }

        var expectedWidthMm = template.PrinterProfile.PhysicalWidthMm > 0
            ? template.PrinterProfile.PhysicalWidthMm
            : template.WidthMm;
        var expectedHeightMm = template.PrinterProfile.PhysicalHeightMm > 0
            ? template.PrinterProfile.PhysicalHeightMm
            : template.HeightMm;
        const double toleranceDip = 1.0;
        if (!MediaDimensionContract.Matches(expectedWidthMm, expectedHeightMm, effectiveWidthDip, effectiveHeightDip, toleranceDip))
        {
            throw new InvalidOperationException(
                $"The printer driver coerced media to {MmConverter.DipToMm(effectiveWidthDip):0.##} × {MmConverter.DipToMm(effectiveHeightDip):0.##} mm, "
                + $"but the label requires {expectedWidthMm:0.##} × {expectedHeightMm:0.##} mm. Select a matching stock/profile before printing.");
        }
    }

    private static string HashPrintTicket(PrintTicket? ticket)
    {
        if (ticket is null)
        {
            return string.Empty;
        }

        try
        {
            using var xml = ticket.GetXmlStream();
            return Convert.ToHexString(SHA256.HashData(xml.ToArray())).ToLowerInvariant();
        }
        catch
        {
            // A driver may expose a valid ticket but deny XML serialization.
            // Keep the contract explicitly unverified instead of inventing a hash.
            return string.Empty;
        }
    }

    private static void ApplyTemplateTicket(WpfPrintDialog dialog, LabelTemplate template)
    {
        if (dialog.PrintQueue is null)
        {
            return;
        }

        var ticket = dialog.PrintTicket ?? dialog.PrintQueue.DefaultPrintTicket ?? new PrintTicket();
        // Use physical paper dimensions (from PaperSizePrinter profile) for the PageMediaSize ticket.
        // When design orientation is Landscape, Template.WidthMm/HeightMm may be swapped for display.
        // The physical dimensions are always the original paper size selected by the user.
        var physicalWidthMm = template.PrinterProfile.PhysicalWidthMm > 0 ? template.PrinterProfile.PhysicalWidthMm : template.WidthMm;
        var physicalHeightMm = template.PrinterProfile.PhysicalHeightMm > 0 ? template.PrinterProfile.PhysicalHeightMm : template.HeightMm;
        var widthDip = MmConverter.MmToDip(physicalWidthMm);
        var heightDip = MmConverter.MmToDip(physicalHeightMm);

        // IMPORTANT: Do NOT set PageOrientation for thermal/label printers.
        // Thermal printer drivers (Zebra, TSC, Godex, etc.) interpret PageOrientation
        // as a rotation command on the physical media. Since PageMediaSize already carries
        // the exact label dimensions, setting orientation causes the driver to rotate
        // content, breaking the print alignment. By leaving PageOrientation unset,
        // the driver receives the exact physical dimensions and prints correctly.

        try
        {
            // PageMediaSizeName.Unknown signals a custom size not from the driver's paper list.
            // Thermal/label printer drivers rely on the exact width/height rather than named paper sizes.
            ticket.PageMediaSize = new PageMediaSize(PageMediaSizeName.Unknown, widthDip, heightDip);
        }
        catch
        {
            // Fall back to unnamed constructor if the driver rejects Unknown name.
            try
            {
                ticket.PageMediaSize = new PageMediaSize(widthDip, heightDip);
            }
            catch
            {
                // Last resort: keep whatever the driver default is and let the paginator handle sizing.
            }
        }

        var ticketDpi = template.PrinterProfile.Dpi > 0 ? template.PrinterProfile.Dpi : template.Dpi;
        if (ticketDpi > 0)
        {
            try
            {
                ticket.PageResolution = new PageResolution(ticketDpi, ticketDpi);
            }
            catch
            {
                // Keep the driver resolution if it rejects an explicit DPI.
            }
        }

        dialog.PrintTicket = ticket;
    }

    private static System.Windows.Size CreatePageSize(PrintRenderPlan plan)
    {
        return new System.Windows.Size(MmConverter.MmToDip(plan.LabelWidthMm), MmConverter.MmToDip(plan.LabelHeightMm));
    }

    /// <summary>
    /// Captures the queue's current job metadata before dispatch. A thermal
    /// driver may deny job enumeration; in that case null is deliberately used
    /// rather than guessing that an unrelated job belongs to this submission.
    /// </summary>
    private static IReadOnlyList<SpoolJobIdentityCandidate>? CaptureSpoolJobs(PrintQueue queue)
    {
        try
        {
            using var jobs = queue.GetPrintJobInfoCollection();
            return jobs.Cast<PrintSystemJobInfo>()
                .Select(job => new SpoolJobIdentityCandidate(
                    job.JobIdentifier,
                    SafeReadJobName(job)))
                .Where(candidate => candidate.JobId > 0)
                .ToArray();
        }
        catch
        {
            return null;
        }
    }

    private static IReadOnlyList<SpoolJobIdentityCandidate>? CaptureSpoolJobs(string printerName)
    {
        if (string.IsNullOrWhiteSpace(printerName))
        {
            return null;
        }

        try
        {
            using var server = new LocalPrintServer();
            using var queue = server.GetPrintQueue(printerName);
            return CaptureSpoolJobs(queue);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Observes a newly-created spool job when the driver exposes the queue.
    /// This is evidence of spool identity only, never physical completion.
    /// </summary>
    private static int? TryFindNewSpoolJobId(
        PrintQueue queue,
        IReadOnlyCollection<SpoolJobIdentityCandidate>? knownJobs,
        string description)
    {
        if (knownJobs is null)
        {
            return null;
        }

        try
        {
            queue.Refresh();
            using var jobs = queue.GetPrintJobInfoCollection();
            var currentJobs = jobs.Cast<PrintSystemJobInfo>()
                .Select(job => new SpoolJobIdentityCandidate(
                    job.JobIdentifier,
                    SafeReadJobName(job)))
                .Where(candidate => candidate.JobId > 0)
                .ToArray();
            return SpoolJobIdentityResolver.TryResolve(knownJobs, currentJobs, description);
        }
        catch
        {
            return null;
        }
    }

    private static string SafeReadJobName(PrintSystemJobInfo job)
    {
        try
        {
            return job.JobName ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private sealed class VisualDocumentPaginator : DocumentPaginator
    {
        private readonly int _pageCount;
        private readonly Func<int, Visual> _visualFactory;
        private System.Windows.Size _pageSize;

        public VisualDocumentPaginator(int pageCount, System.Windows.Size pageSize, Func<int, Visual> visualFactory)
        {
            _pageCount = pageCount;
            _pageSize = pageSize;
            _visualFactory = visualFactory;
        }

        public override bool IsPageCountValid => true;

        public override int PageCount => _pageCount;

        public override System.Windows.Size PageSize
        {
            get => _pageSize;
            set => _pageSize = value;
        }

        public override IDocumentPaginatorSource? Source => null;

        public override DocumentPage GetPage(int pageNumber)
        {
            if (pageNumber < 0 || pageNumber >= _pageCount)
            {
                return DocumentPage.Missing;
            }

            var pageRect = new Rect(_pageSize);
            var container = new ContainerVisual();
            var background = new DrawingVisual();
            using (var drawingContext = background.RenderOpen())
            {
                drawingContext.DrawRectangle(System.Windows.Media.Brushes.White, null, pageRect);
            }

            container.Children.Add(background);
            container.Children.Add(_visualFactory(pageNumber));
            return new DocumentPage(container, _pageSize, pageRect, pageRect);
        }
    }
}
