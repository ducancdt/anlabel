using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ANLAbel.App.Services;
using ANLAbel.App.ViewModels;
using ANLAbel.Core.Data;
using ANLAbel.Core.Enums;
using ANLAbel.Core.Expressions;
using ANLAbel.Core.Expressions.Formulas;
using ANLAbel.Core.Geometry;
using ANLAbel.Core.Models;
using ANLAbel.Core.Printing;
using ANLAbel.Data.PrintLogs;
using ANLAbel.Printing.PrinterProfiles;
using ANLAbel.Printing.RenderPipeline;

namespace ANLAbel.App;

public partial class PrintPreviewWindow : Window
{
    private readonly LabelTemplate _template;
    private readonly IReadOnlyDictionary<string, string>? _currentRow;
    private readonly DataView? _excelDataView;
    private readonly PrintService _printService;
    private readonly PrintLogService _printLogService;
    private readonly PrintOperationLogService _printOperationLogService = new();
    private readonly PrintJobStateStore _printJobStateStore = new();
    private readonly PrinterDiscoveryService _printerDiscoveryService = new();
    private readonly string _templateFilePath;
    private readonly string? _approvedReprintJobId;
    private readonly PrintJobManifest? _approvedReprintManifest;
    private readonly IReadOnlyList<IReadOnlyDictionary<string, string>?>? _preparedRows;
    private string _selectedPrinterName = string.Empty;
    private IReadOnlyDictionary<string, string>?[] _previewRows = Array.Empty<IReadOnlyDictionary<string, string>?>();
    private double _previewZoom = 1.0;
    private int _currentPageIndex;
    private string _pageInput = "1";
    private PrintPreflightResult _preflightResult = new(Array.Empty<PrintPreflightIssue>());
    private readonly List<TrackingRowViewModel> _trackingRows = new();
    private bool _isRefreshing;
    private bool _isPreviewBusy;
    private bool _isPrintBusy;
    private int _previewProgressPercent;
    private string _previewProgressText = string.Empty;
    private CancellationTokenSource? _previewOperationCts;
    private readonly FileSourceIdentity? _excelKnownSourceIdentity;
    private IReadOnlyDictionary<string, string>?[] _allRowsCache = Array.Empty<IReadOnlyDictionary<string, string>?>();
    private readonly DispatcherTimer _filterDebounceTimer = new() { Interval = TimeSpan.FromMilliseconds(250) };
    private const string AllColumnsFilterOption = "(All columns)";
    private const int PreviewCacheCapacity = 8;
    private readonly Dictionary<int, PreviewRasterResult> _previewImageCache = new();
    private readonly LinkedList<int> _previewImageLru = new();
    private PrintRenderPlan? _previewPlan;
    private string _previewPlanPrinterName = string.Empty;
    private PrintPreflightIssue? _previewPlanIssue;

    public PrintPreviewWindow(
        LabelTemplate template,
        IReadOnlyDictionary<string, string>? currentRow,
        DataView? excelDataView,
        PrintService printService,
        PrintLogService printLogService,
        string templateFilePath,
        string? approvedReprintJobId = null,
        PrintJobManifest? approvedReprintManifest = null,
        IReadOnlyList<IReadOnlyDictionary<string, string>?>? preparedRows = null,
        FileSourceIdentity? excelSourceIdentity = null)
    {
        InitializeComponent();
        _template = template;

        // Clamp window size to screen working area
        var workArea = SystemParameters.WorkArea;
        Width = Math.Min(Width, workArea.Width);
        Height = Math.Min(Height, workArea.Height);
        if (workArea.Width <= 1366 || workArea.Height <= 768)
        {
            WindowState = WindowState.Maximized;
        }
        _currentRow = currentRow;
        _excelDataView = excelDataView;
        _printService = printService;
        _printLogService = printLogService;
        _templateFilePath = templateFilePath;
        _approvedReprintJobId = string.IsNullOrWhiteSpace(approvedReprintJobId) ? null : approvedReprintJobId;
        _approvedReprintManifest = approvedReprintManifest;
        _preparedRows = preparedRows;
        _selectedPrinterName = DocumentPrinterIdentityContract.QueueNameFromDocument(
            template.PrinterProfile.PrinterName) ?? string.Empty;
        _excelKnownSourceIdentity = excelSourceIdentity ?? TryGetExcelSourceIdentity();

        DataContext = this;
        _filterDebounceTimer.Tick += (_, _) =>
        {
            _filterDebounceTimer.Stop();
            RefreshPreviewPagesOnly();
        };
        Closed += (_, _) => CancelPreviewOperation();

        try
        {
            RefreshPreview();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Preview init error: {ex.Message}");
        }
    }

    public string PreviewTitle => $"Print Preview - {_template.Name}";
    public LabelTemplate LabelTemplate => _template;
    public IReadOnlyList<PrintMethodOption> PrintMethodOptions { get; } = new[]
    {
        new PrintMethodOption(PrintMethod.ApplicationGraphic, "Application graphic (Designer parity)"),
        new PrintMethodOption(PrintMethod.PrinterNative, "Printer native (Thermal commands)")
    };
    public List<PrintPreviewPageViewModel> Pages { get; } = new();
    public PrintPreviewPageViewModel? CurrentPage => Pages.Count == 0 ? null : Pages[Math.Max(0, Math.Min(Pages.Count - 1, _currentPageIndex))];
    public string PrinterName => string.IsNullOrWhiteSpace(_selectedPrinterName) ? "(no printer selected)" : _selectedPrinterName;
    public string LabelSizeText
    {
        get
        {
            var plan = _previewPlan;
            var dpi = plan is { DpiX: > 0, DpiY: > 0 }
                ? $"{plan.DpiX}×{plan.DpiY}"
                : $"{_template.PrinterProfile.Dpi}";
            return $"Label: {(plan?.LabelWidthMm ?? _template.WidthMm):0.##} × {(plan?.LabelHeightMm ?? _template.HeightMm):0.##} mm | DPI: {dpi}";
        }
    }

    /// <summary>
    /// Print-preview-reliability-plan Đợt 2 item 6: makes explicit that this preview is
    /// simulating the exact <see cref="ANLAbel.Printing.RenderPipeline.PrintRenderPlan"/>
    /// values (offset/rotate/margin from <see cref="PrinterProfile"/>) that will be used
    /// for the real print job — not a generic "as designed" view.
    /// </summary>
    public string PrintPlanSummaryText
    {
        get
        {
            var profile = _template.PrinterProfile;
            var plan = _previewPlan;
            var parts = new List<string>
            {
                plan is null
                    ? $"Plan: {profile.Dpi} DPI (design-only)"
                    : $"Plan: {plan.DpiX}×{plan.DpiY} DPI{(plan.OutputContractTicketVerified ? string.Empty : " (design-only)")}",
                $"offset {profile.OffsetXMm:0.##}/{profile.OffsetYMm:0.##} mm"
            };
            parts.Add(plan?.PrintableAreaVerified == true ? "imageable area verified" : "imageable area unverified");
            if (profile.Rotated180)
            {
                parts.Add("rotated 180°");
            }
            if (_template.MarginMm > 0)
            {
                parts.Add($"margin {_template.MarginMm:0.##} mm");
            }
            return string.Join(" · ", parts);
        }
    }
    public string PageCountText => $"Labels/pages: {Pages.Count}";
    public string PageInput
    {
        get => _pageInput;
        set => _pageInput = value;
    }
    public string PageStatusText => Pages.Count == 0 ? "No labels" : $"Label {_currentPageIndex + 1} of {Pages.Count}";
    public string CurrentRowSummary => CreateRowSummary(_previewRows.ElementAtOrDefault(_currentPageIndex));
    public string SelectedRowsText
    {
        get
        {
            var selectedCount = _trackingRows.Count(r => r.IsSelected);
            var totalCount = _trackingRows.Count;
            var totalCopies = _trackingRows.Where(r => r.IsSelected).Sum(r => r.Copies);
            return $"{selectedCount}/{totalCount} selected ({totalCopies} labels)";
        }
    }
    public bool HasPreflightIssues => !_preflightResult.IsSuccess;
    public IReadOnlyList<PrintPreflightIssue> PreflightIssues => _preflightResult.Issues.Take(8).ToArray();
    public string PreflightStatusText => _preflightResult.IsSuccess
        ? "Preflight passed. Content is ready to print."
        : _preflightResult.ToUserMessage(3);
    public string PreflightIssuesSummary => _preflightResult.Issues.Count <= 8
        ? $"{_preflightResult.Issues.Count} issue(s) found."
        : $"Showing first 8 of {_preflightResult.Issues.Count} issue(s).";
    public bool IsPreviewBusy => _isPreviewBusy;
    public bool CanCancelPreview => _isPreviewBusy;
    public int PreviewProgressPercent => _previewProgressPercent;
    public string PreviewProgressText => _previewProgressText;
    public bool IsPrintBusy => _isPrintBusy;
    public bool CanPrint => CanBeginPrint(_isPrintBusy, _isPreviewBusy, _previewRows.Length);
    public double PreviewZoom
    {
        get => _previewZoom;
        private set
        {
            _previewZoom = Math.Max(0.25, Math.Min(4, Math.Round(value, 2)));
            OnPropertyChanged();
        }
    }

    private void LabelPrinterSetup_Click(object sender, RoutedEventArgs e)
    {
        var printers = _printerDiscoveryService.GetInstalledPrinters();
        if (printers.Count == 0)
        {
            MessageBox.Show(this, "No Windows printers were found.", "Printer setup", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new PrinterSetupWindow(
            printers,
            _selectedPrinterName,
            _template.PrinterProfile.PaperName,
            _template.Orientation,
            _template.PrinterProfile.Dpi) { Owner = this };
        if (dialog.ShowDialog() == true)
        {
            if (dialog.SelectedPrinter is not null)
            {
                _selectedPrinterName = dialog.SelectedPrinter.Name;
                _template.PrinterProfile.PrinterName = _selectedPrinterName;
            }

            _previewPlan = null;
            _previewPlanPrinterName = string.Empty;
            _previewPlanIssue = null;

            if (dialog.SelectedPaper is not null)
            {
                var (widthMm, heightMm) = LabelGeometry.OrientSize(dialog.SelectedPaper.WidthMm, dialog.SelectedPaper.HeightMm, dialog.SelectedOrientation);
                var stock = LabelStockContract.Evaluate(
                    widthMm,
                    heightMm,
                    dialog.SelectedPaper.WidthMm,
                    dialog.SelectedPaper.HeightMm,
                    dialog.SelectedPaper.Name);
                if (!stock.IsAllowed)
                {
                    MessageBox.Show(this, stock.Diagnostic, "Printer setup", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _template.WidthMm = widthMm;
                _template.HeightMm = heightMm;
                _template.PrinterProfile.LabelWidthMm = widthMm;
                _template.PrinterProfile.LabelHeightMm = heightMm;
                _template.PrinterProfile.PhysicalWidthMm = dialog.SelectedPaper.WidthMm;
                _template.PrinterProfile.PhysicalHeightMm = dialog.SelectedPaper.HeightMm;
                _template.PrinterProfile.PaperSizeSource = LabelStockContract.SourceForOperatorStock();
            }

            _template.PrinterProfile.Dpi = dialog.SelectedDpi;
            _template.Orientation = dialog.SelectedOrientation;
            RefreshPreview();
        }
    }

    private async void Print_Click(object sender, RoutedEventArgs e)
    {
        if (!CanBeginPrint(_isPrintBusy, _isPreviewBusy, _previewRows.Length))
        {
            return;
        }

        _isPrintBusy = true;
        OnPropertyChanged();
        var printJobId = string.Empty;
        PrintJobManifest? manifest = null;
        try
        {
            if (_isPreviewBusy)
            {
                return;
            }

            if (TryBlockStaleLinkedExcelData())
            {
                return;
            }

            ApplyPrintSetup();
            var selected = GetSelectedRowsWithSource();
            if (selected.Length == 0)
            {
                MessageBox.Show(this, "No rows selected for printing. Use checkboxes to select rows.", "Print", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Resolve the queue contract before creating the durable manifest.
            // The design-time plan is useful for editing, but it cannot prove
            // what a driver coerced (DPI/media/imageable area) for this queue.
            // Keeping this effective plan through preparation makes approved
            // reprints compare the same physical output contract as dispatch.
            PrintRenderPlan effectivePlan;
            try
            {
                effectivePlan = await _printService.CreateEffectivePlanAsync(_template, _selectedPrinterName ?? string.Empty);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    $"Print preparation stopped because the selected printer contract could not be validated.\n\n{ex.Message}",
                    "Printer contract unavailable",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // Đợt 4 item 11 — "chống trùng tem": warn before re-sending a row that was
            // already printed earlier in this same preview session.
            // Keep the exact effective queue/driver plan for any preview
            // refresh or rerender that follows this preparation step.
            _previewPlan = effectivePlan;
            _previewPlanPrinterName = _selectedPrinterName ?? string.Empty;
            _previewPlanIssue = null;

            var alreadyPrintedRows = selected.Select(entry => entry.SourceRowNumber).Distinct()
                .Where(rowNumber => _trackingRows.FirstOrDefault(r => r.SourceRowNumber == rowNumber)?.IsPrinted == true)
                .OrderBy(n => n)
                .ToArray();
            if (alreadyPrintedRows.Length > 0)
            {
                var choice = MessageBox.Show(
                    this,
                    $"{alreadyPrintedRows.Length} selected row(s) were already printed earlier in this session (Row {string.Join(", ", alreadyPrintedRows)}).\n\nPrinting again may create duplicate labels. Continue?",
                    "Rows already printed this session",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                if (choice != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            var rows = selected.Select(entry => entry.Row).ToArray();
            var preflight = await ValidateRowsForUiAsync(rows, "Checking selected labels", effectivePlan);
            var skippedSourceRows = Array.Empty<int>();
            if (!preflight.IsSuccess)
            {
                // Đợt 4 item 10 — "bỏ qua dòng lỗi, in các dòng còn lại": map the
                // flattened-row-index issues back to their originating tracking rows so
                // the user can choose to print just the rows that pass preflight.
                var badSourceRows = preflight.Issues
                    .Where(issue => issue.RowNumber > 0 && issue.RowNumber <= selected.Length)
                    .Select(issue => selected[issue.RowNumber - 1].SourceRowNumber)
                    .Distinct()
                    .OrderBy(n => n)
                    .ToArray();
                var totalSelectedRows = selected.Select(entry => entry.SourceRowNumber).Distinct().Count();

                if (badSourceRows.Length == 0 || badSourceRows.Length >= totalSelectedRows)
                {
                    // Template-level issue (object outside label, etc.) or every row is
                    // bad — nothing safe to print, block the whole job as before.
                    _preflightResult = preflight;
                    OnPropertyChanged();
                    MessageBox.Show(this, preflight.ToUserMessage(), "Print blocked", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var remainingCount = totalSelectedRows - badSourceRows.Length;
                var choice = MessageBox.Show(
                    this,
                    $"{badSourceRows.Length} row(s) have preflight issues and will be skipped:\nRow {string.Join(", ", badSourceRows)}\n\n{preflight.ToUserMessage(5)}\n\nPrint the remaining {remainingCount} row(s) instead?",
                    "Some rows blocked",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Warning);
                if (choice != MessageBoxResult.Yes)
                {
                    _preflightResult = preflight;
                    OnPropertyChanged();
                    return;
                }

                foreach (var vm in _trackingRows.Where(r => badSourceRows.Contains(r.SourceRowNumber)))
                {
                    vm.IsSelected = false;
                }
                skippedSourceRows = badSourceRows;
                selected = GetSelectedRowsWithSource();
                rows = selected.Select(entry => entry.Row).ToArray();
                if (rows.Length == 0)
                {
                    MessageBox.Show(this, "No rows left to print after skipping the blocked ones.", "Print", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                preflight = await ValidateRowsForUiAsync(rows, "Re-checking remaining labels", effectivePlan);
                if (!preflight.IsSuccess)
                {
                    _preflightResult = preflight;
                    OnPropertyChanged();
                    MessageBox.Show(this, preflight.ToUserMessage(), "Print blocked", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            // Preflight can take long enough for an upstream spreadsheet save to
            // finish after the initial click-time check. Revalidate immediately
            // before a job/manifest is created, so a stale source never crosses
            // the durable dispatch boundary.
            if (TryBlockStaleLinkedExcelData())
            {
                return;
            }

            printJobId = _approvedReprintJobId ?? Guid.NewGuid().ToString("N");
            manifest = PrintJobManifest.Create(
                _template.Name,
                _templateFilePath,
                $"{_template.Name} preview print",
                _selectedPrinterName ?? string.Empty,
                _template.WidthMm,
                _template.HeightMm,
                effectivePlan.DpiX > 0 ? effectivePlan.DpiX : _template.PrinterProfile.Dpi,
                effectivePlan.DpiY > 0 ? effectivePlan.DpiY : _template.PrinterProfile.Dpi,
                rows.Length,
                selected.Select(entry => entry.SourceRowNumber).Distinct().Count(),
                rows,
                effectivePlan.DocumentHash,
                effectivePlan.TextResourceFingerprint,
                effectivePlan.SceneHash,
                effectivePlan.OutputContractHash,
                imageRasterFingerprint: effectivePlan.ImageRasterFingerprint,
                thermalRasterGoldenFingerprint: effectivePlan.ThermalRasterGolden?.Fingerprint ?? string.Empty);
            if (_approvedReprintJobId is not null)
            {
                var recoverySnapshot = await _printJobStateStore.ReadRecoverySnapshotAsync();
                var currentEvent = recoverySnapshot.LatestEvents.FirstOrDefault(item =>
                    string.Equals(item.JobId, _approvedReprintJobId, StringComparison.Ordinal));
                if (_approvedReprintManifest is null
                    || !_approvedReprintManifest.IsFingerprintValid
                    || recoverySnapshot.StoreDiagnostics.Count > 0
                    || currentEvent is null
                    || currentEvent.To != PrintJobLifecycleState.Created
                    || currentEvent.OperatorAction != PrintJobOperatorAction.ReprintApproved
                    || currentEvent.Manifest is null
                    || !currentEvent.Manifest.IsFingerprintValid
                    || !string.Equals(currentEvent.ManifestFingerprint, _approvedReprintManifest.Fingerprint, StringComparison.Ordinal)
                    || !string.Equals(manifest.Fingerprint, _approvedReprintManifest.Fingerprint, StringComparison.Ordinal))
                {
                    MessageBox.Show(
                        this,
                        "Reprint blocked: the selected template, printer, DPI or data no longer matches the approved manifest.",
                        "Manifest mismatch",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }
            }
            await RecordPrintJobTransitionAsync(new PrintJobStateTransition(
                printJobId,
                PrintJobLifecycleState.Created,
                PrintJobLifecycleState.Preparing,
                DateTimeOffset.UtcNow,
                "Print preview accepted the batch for preparation.",
                PrinterName: _selectedPrinterName ?? string.Empty,
                DocumentHash: effectivePlan.DocumentHash,
                TextResourceFingerprint: effectivePlan.TextResourceFingerprint,
                SceneHash: effectivePlan.SceneHash,
                ManifestFingerprint: manifest.Fingerprint,
                Manifest: manifest));
            await RecordPrintJobTransitionAsync(new PrintJobStateTransition(
                printJobId,
                PrintJobLifecycleState.Preparing,
                PrintJobLifecycleState.PreflightPassed,
                DateTimeOffset.UtcNow,
                "Preflight passed for the selected rows.",
                PrinterName: _selectedPrinterName ?? string.Empty,
                DocumentHash: effectivePlan.DocumentHash,
                TextResourceFingerprint: effectivePlan.TextResourceFingerprint,
                SceneHash: effectivePlan.SceneHash,
                ManifestFingerprint: manifest.Fingerprint,
                Manifest: manifest));
            await RecordPrintJobTransitionAsync(new PrintJobStateTransition(
                printJobId,
                PrintJobLifecycleState.PreflightPassed,
                PrintJobLifecycleState.Dispatching,
                DateTimeOffset.UtcNow,
                "Dispatching the validated batch to the selected queue.",
                PrinterName: _selectedPrinterName ?? string.Empty,
                DocumentHash: effectivePlan.DocumentHash,
                TextResourceFingerprint: effectivePlan.TextResourceFingerprint,
                SceneHash: effectivePlan.SceneHash,
                ManifestFingerprint: manifest.Fingerprint,
                Manifest: manifest));

            var printResult = (await _printService.PrintRowsWithResultAsync(
                    _template,
                    rows,
                    _selectedPrinterName ?? string.Empty,
                    $"{_template.Name} preview print",
                    expectedOutputContractHash: effectivePlan.OutputContractHash))
                with { ManifestFingerprint = manifest.Fingerprint, Manifest = manifest };
            printResult = await _printService.ResolveSpoolJobIdentityAsync(
                printResult,
                timeout: TimeSpan.FromSeconds(1),
                pollInterval: TimeSpan.FromMilliseconds(100));
            SpoolJobMonitorResult? spoolStatus = null;
            if (printResult.IsAccepted && printResult.SpoolJobId is int)
            {
                // Queue polling is bounded and read-only. It gives the operator a
                // useful spool/driver signal without delaying dispatch or claiming
                // that a physical label was verified.
                spoolStatus = await _printService.MonitorSpoolJobAsync(
                    printResult,
                    timeout: TimeSpan.FromSeconds(3),
                    pollInterval: TimeSpan.FromMilliseconds(250));
            }

            var dispatchState = MapPrintResultToLifecycleState(printResult);
            await RecordPrintJobTransitionAsync(new PrintJobStateTransition(
                printJobId,
                PrintJobLifecycleState.Dispatching,
                dispatchState,
                DateTimeOffset.UtcNow,
                printResult.UserFacingStatus,
                PrinterName: printResult.PrinterName,
                SpoolJobId: printResult.SpoolJobId,
                DocumentHash: printResult.DocumentHash,
                TextResourceFingerprint: printResult.TextResourceFingerprint,
                SceneHash: printResult.SceneHash,
                OutputContractHash: printResult.OutputContractHash,
                ManifestFingerprint: printResult.ManifestFingerprint,
                Manifest: printResult.Manifest,
                PhysicalOutputVerified: printResult.IsPhysicalCompletionVerified));

            if (spoolStatus is not null && dispatchState == PrintJobLifecycleState.SpoolAccepted)
            {
                await RecordPrintJobTransitionAsync(new PrintJobStateTransition(
                    printJobId,
                    PrintJobLifecycleState.SpoolAccepted,
                    PrintJobLifecycleState.QueueObserved,
                    spoolStatus.FinalObservation.ObservedAtUtc ?? DateTimeOffset.UtcNow,
                    spoolStatus.FinalObservation.Message,
                    PrinterName: printResult.PrinterName,
                    SpoolJobId: printResult.SpoolJobId,
                    QueueState: spoolStatus.FinalObservation.State.ToString(),
                    DocumentHash: printResult.DocumentHash,
                    TextResourceFingerprint: printResult.TextResourceFingerprint,
                    SceneHash: printResult.SceneHash,
                    OutputContractHash: printResult.OutputContractHash,
                    ManifestFingerprint: printResult.ManifestFingerprint,
                    Manifest: printResult.Manifest));
            }

            LogPrintOperation(rows.Length, printResult, spoolStatus, printJobId);
            if (!printResult.IsAccepted)
            {
                MessageBox.Show(this, printResult.UserFacingStatus, "Print not completed", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Only a device-confirmed terminal outcome may mark rows as printed.
            // Spool acceptance (even with a queue job id) is deliberately not
            // enough evidence for the duplicate-label guard.
            if (printResult.IsPhysicalCompletionVerified)
            {
                var confirmedRows = selected.Select(entry => entry.SourceRowNumber).ToHashSet();
                foreach (var row in _trackingRows.Where(item => confirmedRows.Contains(item.SourceRowNumber)))
                {
                    row.IsPrinted = true;
                }
            }

            var statusText = printResult.UserFacingStatus;
            if (spoolStatus is not null)
            {
                statusText += $"\nQueue status: {spoolStatus.UserFacingStatus}";
            }

            await WritePrintHistoryAsync(rows, statusText);

            // Đợt 4 item 10 — batch report so the user knows exactly what happened,
            // not just "a print job was sent".
            var printedRowCount = selected.Select(entry => entry.SourceRowNumber).Distinct().Count();
            var summary = $"Submitted {rows.Length} label(s) from {printedRowCount} row(s) to {printResult.PrinterName}.\n\n{statusText}";
            if (skippedSourceRows.Length > 0)
            {
                summary += $"\nSkipped {skippedSourceRows.Length} row(s) with preflight issues: Row {string.Join(", ", skippedSourceRows)}.";
            }
            summary += $"\n\nHistory: {_printLogService.LogFilePath}";
            MessageBox.Show(this, summary, "Print submitted", MessageBoxButton.OK, MessageBoxImage.Information);
            OnPropertyChanged();
        }
        catch (OperationCanceledException)
        {
            // Cancellation is a normal operator action, not a failed print job.
            if (!string.IsNullOrWhiteSpace(printJobId))
            {
                var currentState = _printJobStateStore.GetCurrentState(printJobId);
                if (currentState is PrintJobLifecycleState current
                    && PrintJobStateMachine.CanTransition(current, PrintJobLifecycleState.Cancelled))
                {
                    await RecordPrintJobTransitionAsync(new PrintJobStateTransition(
                        printJobId,
                        current,
                        PrintJobLifecycleState.Cancelled,
                        DateTimeOffset.UtcNow,
                        "Operator canceled the print operation.",
                        PrinterName: _selectedPrinterName,
                        ManifestFingerprint: manifest?.Fingerprint ?? string.Empty,
                        Manifest: manifest));
                }
            }

            _previewProgressText = "Print preflight canceled.";
            OnPropertyChanged();
        }
        catch (Exception ex)
        {
            if (!string.IsNullOrWhiteSpace(printJobId))
            {
                var currentState = _printJobStateStore.GetCurrentState(printJobId);
                if (currentState is PrintJobLifecycleState current
                    && PrintJobStateMachine.CanTransition(current, PrintJobLifecycleState.Failed))
                {
                    await RecordPrintJobTransitionAsync(new PrintJobStateTransition(
                        printJobId,
                        current,
                        PrintJobLifecycleState.Failed,
                        DateTimeOffset.UtcNow,
                        ex.Message,
                        PrinterName: _selectedPrinterName,
                        ManifestFingerprint: manifest?.Fingerprint ?? string.Empty,
                        Manifest: manifest));
                }
            }

            LogPrintOperation(0, new PrintJobResult(PrintJobOutcome.Failed, _selectedPrinterName, $"{_template.Name} preview print", 0, ex.Message, ManifestFingerprint: manifest?.Fingerprint ?? string.Empty, Manifest: manifest), jobId: printJobId);
            MessageBox.Show(this, ex.Message, "Print failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _isPrintBusy = false;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Fire-and-forget job-level print trace (print-preview-reliability-plan.md item 3),
    /// separate from the human-facing print-history.csv written by
    /// <see cref="WritePrintHistoryAsync"/>.
    /// </summary>
    private void LogPrintOperation(int labelsPrinted, PrintJobResult result, SpoolJobMonitorResult? spoolStatus = null, string jobId = "")
    {
        var entry = new PrintOperationLogEntry
        {
            JobId = jobId,
            TemplateName = _template.Name,
            TemplateFilePath = _templateFilePath,
            PrinterName = string.IsNullOrWhiteSpace(result.PrinterName) ? _selectedPrinterName : result.PrinterName,
            LabelWidthMm = _template.PrinterProfile.LabelWidthMm,
            LabelHeightMm = _template.PrinterProfile.LabelHeightMm,
            Dpi = result.DpiX > 0 ? result.DpiX : _template.PrinterProfile.Dpi,
            DpiX = result.DpiX > 0 ? result.DpiX : _template.PrinterProfile.Dpi,
            DpiY = result.DpiY > 0 ? result.DpiY : _template.PrinterProfile.Dpi,
            PrintMode = "Print Preview",
            PrintMethod = _template.PrinterProfile.PrintMethod.ToString(),
            NativeCommandsUsed = _template.PrinterProfile.PrintMethod == PrintMethod.PrinterNative,
            Outcome = result.Outcome.ToString(),
            OutcomeEvidence = result.IsPhysicalCompletionVerified
                ? "device-confirmed"
                : result.OutputContractTicketVerified
                    ? result.PrintableAreaVerified ? "effective-ticket-and-imageable-area; physical-output-unverified" : "effective-ticket; printable-area-unverified; physical-output-unverified"
                    : "output-contract-ticket-unverified; physical-output-unverified",
            SpoolJobId = result.SpoolJobId,
            SpoolState = spoolStatus?.FinalObservation.State.ToString() ?? string.Empty,
            SpoolStatusMessage = spoolStatus?.FinalObservation.Message ?? string.Empty,
            SpoolStatusPollCount = spoolStatus?.PollCount ?? 0,
            SpoolStatusTimedOut = spoolStatus?.TimedOut ?? false,
            SpoolStatusObservedAtUtc = spoolStatus?.FinalObservation.ObservedAtUtc,
            OutputContractHash = result.OutputContractHash,
            OutputContractTicketVerified = result.OutputContractTicketVerified,
            DocumentHash = result.DocumentHash,
            TextResourceFingerprint = result.TextResourceFingerprint,
            ImageRasterFingerprint = result.ImageRasterFingerprint,
            ThermalRasterGoldenFingerprint = result.ThermalRasterGoldenFingerprint,
            ManifestFingerprint = result.ManifestFingerprint,
            Manifest = result.Manifest,
            SupportEvidenceFingerprint = result.SupportEvidenceFingerprint,
            SceneHash = result.SceneHash,
            SceneCompilationVerified = result.SceneCompilationVerified,
            RowsSelected = _trackingRows.Count(r => r.IsSelected),
            LabelsPrinted = labelsPrinted,
            Success = result.IsAccepted,
            ErrorMessage = result.ErrorMessage
        };
        _ = _printOperationLogService.AppendAsync(entry);
    }

    private async Task RecordPrintJobTransitionAsync(PrintJobStateTransition transition)
    {
        try
        {
            await _printJobStateStore.AppendAsync(transition).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            // The state log is an audit/recovery aid, not a reason to block a
            // validated print. Keep the operator path alive while exposing the
            // failure in diagnostics for support.
            System.Diagnostics.Debug.WriteLine($"Print job state log failed: {ex.Message}");
        }
    }

    private static PrintJobLifecycleState MapPrintResultToLifecycleState(PrintJobResult result)
    {
        return result.Outcome switch
        {
            PrintJobOutcome.Cancelled => PrintJobLifecycleState.Cancelled,
            PrintJobOutcome.Failed => PrintJobLifecycleState.Failed,
            PrintJobOutcome.Unknown => PrintJobLifecycleState.Unknown,
            PrintJobOutcome.Completed when result.IsPhysicalCompletionVerified => PrintJobLifecycleState.Completed,
            PrintJobOutcome.SpoolAccepted => PrintJobLifecycleState.SpoolAccepted,
            PrintJobOutcome.DeviceAcknowledged => PrintJobLifecycleState.SpoolAccepted,
            _ => PrintJobLifecycleState.Unknown
        };
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    /// <summary>
    /// Reads the linked Excel file's last-write time for the freshness check below.
    /// Best-effort only — a missing/locked file must not block printing here (the
    /// preflight/print pipeline already handles the "no data" case separately).
    /// </summary>
    private FileSourceIdentity? TryGetExcelSourceIdentity()
    {
        return FileSourceIdentity.TryCapture(_template.DatabaseConfig.FilePath, out var identity)
            ? identity
            : null;
    }

    /// <summary>
    /// Detects a source snapshot that no longer matches the linked Excel file.
    /// Stale data is a hard print-preparation block; preview pages remain a visual
    /// record of the captured snapshot only.
    /// </summary>
    private bool IsLinkedExcelDataStale()
    {
        return IsSourceSnapshotStale(_excelKnownSourceIdentity, TryGetExcelSourceIdentity());
    }

    private bool TryBlockStaleLinkedExcelData()
    {
        if (!IsLinkedExcelDataStale())
        {
            return false;
        }

        MessageBox.Show(
            this,
            "The linked Excel file has changed since this preview was opened. Print preparation is blocked because the displayed rows may be outdated.\n\nClose this window, click Update Excel, then open Print Preview again.",
            "Excel data changed",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
        return true;
    }

    public static bool IsSourceSnapshotStale(FileSourceIdentity? knownIdentity, FileSourceIdentity? currentIdentity) =>
        FileSourceIdentity.IsStale(knownIdentity, currentIdentity);

    private void PreviewScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control)
        {
            return;
        }

        e.Handled = true;
        PreviewZoom = e.Delta > 0 ? PreviewZoom + 0.1 : PreviewZoom - 0.1;
    }

    private void PreviousPage_Click(object sender, RoutedEventArgs e)
    {
        if (_isPreviewBusy)
        {
            return;
        }

        if (_currentPageIndex > 0)
        {
            _currentPageIndex--;
            _pageInput = (_currentPageIndex + 1).ToString();
            EnsureCurrentPreviewImage();
            OnPropertyChanged();
        }
    }

    private void NextPage_Click(object sender, RoutedEventArgs e)
    {
        if (_isPreviewBusy)
        {
            return;
        }

        if (_currentPageIndex < Pages.Count - 1)
        {
            _currentPageIndex++;
            _pageInput = (_currentPageIndex + 1).ToString();
            EnsureCurrentPreviewImage();
            OnPropertyChanged();
        }
    }

    private void ApplyPrintSetup_Click(object sender, RoutedEventArgs e)
    {
        ApplyPrintSetup();
        RefreshPreview();
    }

    private async void PrintCalibration_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ApplyPrintSetup();
            var result = await _printService.PrintCalibrationWithResultAsync(_template);
            if (!result.IsAccepted)
            {
                MessageBox.Show(this, result.UserFacingStatus, "Calibration not completed", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            OnPropertyChanged();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Calibration print failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ResetCalibration_Click(object sender, RoutedEventArgs e)
    {
        _template.PrinterProfile.OffsetXMm = 0;
        _template.PrinterProfile.OffsetYMm = 0;
        _template.PrinterProfile.ScaleX = 1;
        _template.PrinterProfile.ScaleY = 1;
        RefreshPreview();
    }

    private void PreflightIssue_Click(object sender, RoutedEventArgs e)
    {
        if (_isPreviewBusy)
        {
            return;
        }

        if (sender is not Button { Tag: int rowNumber })
        {
            return;
        }

        if (Pages.Count == 0)
        {
            return;
        }

        _currentPageIndex = Math.Max(0, Math.Min(Pages.Count - 1, rowNumber - 1));
        _pageInput = (_currentPageIndex + 1).ToString();
        EnsureCurrentPreviewImage();
        OnPropertyChanged();
    }

    private void PageInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || _isPreviewBusy)
        {
            return;
        }

        if (int.TryParse(PageInput, out var pageNumber) && pageNumber >= 1 && pageNumber <= Pages.Count)
        {
            _currentPageIndex = pageNumber - 1;
            _pageInput = pageNumber.ToString();
            EnsureCurrentPreviewImage();
            OnPropertyChanged();
        }
    }

    // ==================== Checkbox toggle & copies handlers ====================

    private void ToggleRowCheck_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not int sourceRowNumber)
        {
            return;
        }

        var vm = _trackingRows.FirstOrDefault(r => r.SourceRowNumber == sourceRowNumber);
        if (vm is null)
        {
            return;
        }

        vm.IsSelected = !vm.IsSelected;
        RefreshPreviewPagesOnly();
    }

    private void CopiesUp_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not int sourceRowNumber)
        {
            return;
        }

        var vm = _trackingRows.FirstOrDefault(r => r.SourceRowNumber == sourceRowNumber);
        if (vm is null)
        {
            return;
        }

        vm.Copies = Math.Min(999, vm.Copies + 1);
        if (vm.Copies > 0 && !vm.IsSelected)
        {
            vm.IsSelected = true;
        }

        RefreshPreviewPagesOnly();
    }

    private void CopiesDown_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not int sourceRowNumber)
        {
            return;
        }

        var vm = _trackingRows.FirstOrDefault(r => r.SourceRowNumber == sourceRowNumber);
        if (vm is null)
        {
            return;
        }

        vm.Copies = Math.Max(0, vm.Copies - 1);
        if (vm.Copies == 0)
        {
            vm.IsSelected = false;
        }

        RefreshPreviewPagesOnly();
    }

    private void SelectAllToggle_Click(object sender, RoutedEventArgs e)
    {
        // Acts on whatever rows are currently visible, so it stays intuitive while a
        // row filter is active (e.g. "select all" only checks the matching rows, not
        // the hundreds of rows currently hidden by the filter).
        var visibleRows = (TrackingList.ItemsSource as IEnumerable<TrackingRowViewModel> ?? _trackingRows).ToArray();
        var allSelected = visibleRows.Length > 0 && visibleRows.All(r => r.IsSelected);
        var newState = !allSelected;
        foreach (var vm in visibleRows)
        {
            vm.IsSelected = newState;
        }
        RefreshPreviewPagesOnly();
    }

    // ==================== Row filter ====================

    /// <summary>
    /// Lets the user find rows among a large Excel import (e.g. 1000 rows) by typing a
    /// value — a product code, lot number, anything — instead of scrolling or editing
    /// the source file. Matching rows are shown and auto-selected for printing; every
    /// other row is deselected, so hitting the existing Print button prints exactly the
    /// filtered rows. Clearing the filter restores the original "everything selected"
    /// state. Runs live per keystroke (filtering/selection is cheap); the heavier label
    /// preview re-render is debounced so it only happens once typing pauses.
    /// </summary>
    private void RowFilterBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isRefreshing)
        {
            return;
        }

        ApplyRowFilter();
        _filterDebounceTimer.Stop();
        _filterDebounceTimer.Start();
    }

    private void RowFilterBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
        {
            return;
        }

        _filterDebounceTimer.Stop();
        RefreshPreviewPagesOnly();
    }

    private void RowFilterColumnCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isRefreshing)
        {
            return;
        }

        ApplyRowFilter();
        RefreshPreviewPagesOnly();
    }

    private void ClearRowFilter_Click(object sender, RoutedEventArgs e)
    {
        RowFilterBox.Text = string.Empty;
        RowFilterColumnCombo.SelectedIndex = 0;
        _filterDebounceTimer.Stop();
        RefreshPreviewPagesOnly();
    }

    private void ApplyRowFilter()
    {
        var filterText = RowFilterBox.Text.Trim();
        var selectedColumn = RowFilterColumnCombo.SelectedItem as string;
        var searchAllColumns = string.IsNullOrEmpty(selectedColumn) || selectedColumn == AllColumnsFilterOption;

        if (filterText.Length == 0)
        {
            // No filter: restore the default "everything selected" state so the
            // existing Print button behaves exactly as it did before filtering.
            foreach (var vm in _trackingRows)
            {
                vm.IsSelected = true;
            }
            TrackingList.ItemsSource = _trackingRows;
            FilterMatchCountText.Visibility = Visibility.Collapsed;
            return;
        }

        var visible = new List<TrackingRowViewModel>();
        foreach (var vm in _trackingRows)
        {
            var row = _allRowsCache.ElementAtOrDefault(vm.SourceRowNumber - 1);
            var isMatch = RowMatchesFilter(row, searchAllColumns ? null : selectedColumn, filterText);
            vm.IsSelected = isMatch;
            if (isMatch)
            {
                visible.Add(vm);
            }
        }

        TrackingList.ItemsSource = visible;
        FilterMatchCountText.Visibility = Visibility.Visible;
        FilterMatchCountText.Text = visible.Count == 0
            ? $"No rows match \"{filterText}\""
            : $"{visible.Count} of {_trackingRows.Count} row(s) match — selected for printing";
    }

    private static bool RowMatchesFilter(IReadOnlyDictionary<string, string>? row, string? column, string filterText)
    {
        if (row is null)
        {
            return false;
        }

        if (string.IsNullOrEmpty(column))
        {
            return row.Values.Any(value => value.Contains(filterText, StringComparison.OrdinalIgnoreCase));
        }

        return row.TryGetValue(column, out var columnValue) && columnValue.Contains(filterText, StringComparison.OrdinalIgnoreCase);
    }

    // ==================== Refresh logic ====================

    private void RefreshPreviewLegacy()
    {
        if (_isRefreshing)
        {
            return;
        }
        _isRefreshing = true;
        try
        {
            ApplyPrintSetup();
            Pages.Clear();

            // Build tracking rows
            _trackingRows.Clear();
            var allRows = GetRows().ToArray();
            _allRowsCache = allRows;
            var excelColumns = CollectExcelColumns(allRows);
            var pageNumber = 1;
            var sourceRowNumber = 0;

            foreach (var row in allRows)
            {
                sourceRowNumber++;
                var isSelected = true;
                var copies = GetCopiesFromRow(row);
                var vm = new TrackingRowViewModel
                {
                    SourceRowNumber = sourceRowNumber,
                    PageNumber = pageNumber,
                    IsSelected = isSelected,
                    Copies = copies,
                    Col1 = GetPreviewCol(row, excelColumns, 0),
                    Col2 = GetPreviewCol(row, excelColumns, 1),
                    Col3 = GetPreviewCol(row, excelColumns, 2),
                    Col4 = GetPreviewCol(row, excelColumns, 3)
                };
                _trackingRows.Add(vm);
                pageNumber += copies;
            }

            // Bind to ItemsControl
            TrackingList.ItemsSource = _trackingRows;

            // Reset the row filter (it references columns/rows that were just rebuilt).
            RowFilterColumnCombo.ItemsSource = new[] { AllColumnsFilterOption }.Concat(excelColumns).ToList();
            RowFilterColumnCombo.SelectedIndex = 0;
            RowFilterBox.Text = string.Empty;
            FilterMatchCountText.Visibility = Visibility.Collapsed;

            // Build preview rows
            _previewRows = BuildExpandedRowsFromTracking();

            _preflightResult = _printService.ValidateRows(_template, _previewRows);
            BuildPreviewPageMetadata();
            _currentPageIndex = Math.Min(_currentPageIndex, Math.Max(0, Pages.Count - 1));
            _pageInput = Pages.Count == 0 ? "0" : (_currentPageIndex + 1).ToString();
            EnsureCurrentPreviewImage();
            OnPropertyChanged();
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    /// <summary>
    /// Lightweight refresh: rebuilds preview pages from current tracking state.
    /// Does NOT touch _trackingRows or TrackingList.ItemsSource — safe to call from click handlers.
    /// </summary>
    private void RefreshPreviewPagesOnlyLegacy()
    {
        if (_isRefreshing)
        {
            return;
        }
        _isRefreshing = true;
        try
        {
            ApplyPrintSetup();
            Pages.Clear();

            _previewRows = BuildExpandedRowsFromTracking();

            _preflightResult = _printService.ValidateRows(_template, _previewRows);
            BuildPreviewPageMetadata();
            _currentPageIndex = Math.Min(_currentPageIndex, Math.Max(0, Pages.Count - 1));
            _pageInput = Pages.Count == 0 ? "0" : (_currentPageIndex + 1).ToString();
            EnsureCurrentPreviewImage();
            OnPropertyChanged();
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    /// <summary>
    /// Materializes only page metadata.  Visuals and 300-DPI bitmaps are created
    /// by EnsureCurrentPreviewImage for the current page and a small LRU cache.
    /// This keeps large industrial batches bounded instead of allocating one
    /// visual/bitmap per label during window construction.
    /// </summary>
    private void BuildPreviewPageMetadata()
    {
        Pages.Clear();
        _previewImageCache.Clear();
        _previewImageLru.Clear();

        var plan = _previewPlan ?? _printService.CreateDesignPlan(_template);
        var width = MmConverter.MmToDip(plan.LabelWidthMm);
        var height = MmConverter.MmToDip(plan.LabelHeightMm);
        Pages.AddRange(PrintPreviewPageViewModel.CreateMetadata(_previewRows.Length, width, height));
    }

    private void EnsureCurrentPreviewImage()
    {
        if (_isPreviewBusy || Pages.Count == 0 || _previewRows.Length == 0)
        {
            return;
        }

        var operation = BeginPreviewOperation("Rendering label preview");
        _ = RenderCurrentPreviewImageAsync(operation);
    }

    private async Task RenderCurrentPreviewImageAsync(CancellationTokenSource operation)
    {
        var keepStatus = false;
        try
        {
            await EnsureCurrentPreviewImageAsync(operation.Token, operation);
            if (IsCurrentPreviewOperation(operation))
            {
                _previewProgressPercent = 100;
                _previewProgressText = "Preview ready.";
                OnPropertyChanged();
            }
        }
        catch (OperationCanceledException)
        {
            if (IsCurrentPreviewOperation(operation))
            {
                keepStatus = true;
                _previewProgressText = "Preview render canceled.";
                OnPropertyChanged();
            }
        }
        catch (Exception ex)
        {
            if (IsCurrentPreviewOperation(operation))
            {
                keepStatus = true;
                _previewProgressText = $"Preview render failed: {ex.Message}";
                OnPropertyChanged();
            }
        }
        finally
        {
            EndPreviewOperation(operation, keepStatus);
        }
    }

    private async Task EnsureCurrentPreviewImageAsync(
        CancellationToken cancellationToken,
        CancellationTokenSource? operation = null)
    {
        if (Pages.Count == 0 || _previewRows.Length == 0)
        {
            return;
        }

        var pageIndex = Math.Max(0, Math.Min(Pages.Count - 1, _currentPageIndex));
        _currentPageIndex = pageIndex;
        if (_previewImageCache.TryGetValue(pageIndex, out var cached))
        {
            TouchPreviewCache(pageIndex);
            Pages[pageIndex].PreviewImage = cached.Image;
            Pages[pageIndex].PreviewRasterIdentity = cached.RasterIdentity;
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();
        var drawing = _printService.CreatePreviewDrawing(
            _template,
            _previewRows.ElementAtOrDefault(pageIndex),
            _previewPlan);
        var raster = await PreviewRasterizer.RenderSnapshotAsync(
            drawing.Drawing,
            drawing.WidthDip,
            drawing.HeightDip,
            cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        if (operation is not null && !IsCurrentPreviewOperation(operation))
        {
            return;
        }

        _previewImageCache[pageIndex] = raster;
        _previewImageLru.AddLast(pageIndex);
        if (pageIndex == _currentPageIndex)
        {
            Pages[pageIndex].PreviewImage = raster.Image;
            Pages[pageIndex].PreviewRasterIdentity = raster.RasterIdentity;
        }

        while (_previewImageLru.Count > PreviewCacheCapacity)
        {
            var oldest = _previewImageLru.First;
            if (oldest is null)
            {
                break;
            }

            _previewImageLru.RemoveFirst();
            _previewImageCache.Remove(oldest.Value);
            if (oldest.Value != _currentPageIndex && oldest.Value >= 0 && oldest.Value < Pages.Count)
            {
                Pages[oldest.Value].PreviewImage = null;
                Pages[oldest.Value].PreviewRasterIdentity = null;
            }
        }
    }

    private void TouchPreviewCache(int pageIndex)
    {
        _previewImageLru.Remove(pageIndex);
        _previewImageLru.AddLast(pageIndex);
    }

    private IReadOnlyDictionary<string, string>?[] BuildExpandedRowsFromTracking()
    {
        return BuildExpandedRowsFromTracking(_trackingRows, GetRows().ToArray());
    }

    private static IReadOnlyDictionary<string, string>?[] BuildExpandedRowsFromTracking(
        IReadOnlyList<TrackingRowViewModel> trackingRows,
        IReadOnlyList<IReadOnlyDictionary<string, string>?> allRows)
    {
        var result = new List<IReadOnlyDictionary<string, string>?>();
        foreach (var vm in trackingRows)
        {
            if (!vm.IsSelected || vm.Copies <= 0)
            {
                continue;
            }

            var row = vm.SourceRowNumber > 0 && vm.SourceRowNumber <= allRows.Count
                ? allRows[vm.SourceRowNumber - 1]
                : null;
            for (var copy = 0; copy < vm.Copies; copy++)
            {
                result.Add(row);
            }
        }

        return result.ToArray();
    }

    // ==================== Data helpers ====================

    private IEnumerable<IReadOnlyDictionary<string, string>?> GetRows()
    {
        if (_preparedRows is not null)
        {
            foreach (var row in _preparedRows)
            {
                yield return row;
            }

            yield break;
        }

        if (_excelDataView is null || _excelDataView.Count == 0)
        {
            yield return _currentRow;
            yield break;
        }

        foreach (DataRowView rowView in _excelDataView)
        {
            yield return rowView.Row.Table.Columns
                .Cast<DataColumn>()
                .ToDictionary(column => column.ColumnName, column => rowView.Row[column]?.ToString() ?? string.Empty, StringComparer.OrdinalIgnoreCase);
        }
    }

    private IEnumerable<IReadOnlyDictionary<string, string>?> GetSelectedRows()
    {
        return GetSelectedRowsWithSource().Select(entry => entry.Row);
    }

    /// <summary>
    /// Same rows as <see cref="GetSelectedRows"/> (copies expanded), paired with the
    /// originating <see cref="TrackingRowViewModel.SourceRowNumber"/> so preflight issues
    /// (which reference a 1-based index into this flattened list) can be mapped back to
    /// the tracking row that produced them — used for the partial-print offer and the
    /// per-row "printed" marking (print-preview-reliability-plan Đợt 4).
    /// </summary>
    private (IReadOnlyDictionary<string, string>? Row, int SourceRowNumber)[] GetSelectedRowsWithSource()
    {
        var allRows = GetRows().ToArray();
        var result = new List<(IReadOnlyDictionary<string, string>? Row, int SourceRowNumber)>();
        foreach (var vm in _trackingRows)
        {
            if (!vm.IsSelected || vm.Copies <= 0) continue;
            var row = allRows.ElementAtOrDefault(vm.SourceRowNumber - 1);
            for (var i = 0; i < vm.Copies; i++)
            {
                result.Add((row, vm.SourceRowNumber));
            }
        }
        return result.ToArray();
    }

    private static List<string> CollectExcelColumns(IReadOnlyDictionary<string, string>?[] allRows)
    {
        var columns = new List<string>();
        foreach (var row in allRows)
        {
            if (row is not null)
            {
                foreach (var key in row.Keys)
                {
                    if (!columns.Contains(key, StringComparer.OrdinalIgnoreCase))
                    {
                        columns.Add(key);
                    }
                }
            }
        }
        return columns;
    }

    private static string? GetPreviewCol(IReadOnlyDictionary<string, string>? row, List<string> columns, int index)
    {
        if (row is null || index >= columns.Count) return null;
        var colName = columns[index];
        return row.TryGetValue(colName, out var val) ? val : null;
    }

    private int GetCopiesFromRow(IReadOnlyDictionary<string, string>? row)
    {
        return DatabaseConfig.ResolveCopiesForRow(_template.DatabaseConfig.CopiesField, row);
    }

    private void ApplyPrintSetup()
    {
        _template.PrinterProfile.LabelWidthMm = _template.WidthMm;
        _template.PrinterProfile.LabelHeightMm = _template.HeightMm;
        _template.Orientation = LabelGeometry.ResolveOrientation(_template.WidthMm, _template.HeightMm);
    }

    private void OnPropertyChanged()
    {
        if (!Dispatcher.CheckAccess())
        {
            _ = Dispatcher.BeginInvoke(OnPropertyChanged);
            return;
        }

        DataContext = null;
        DataContext = this;
    }

    private static bool CanBeginPrint(bool isPrintBusy, bool isPreviewBusy, int previewRowCount)
        => !isPrintBusy && !isPreviewBusy && previewRowCount > 0;

    // ==================== Summary & formatting ====================

    private static string CreateRowSummary(IReadOnlyDictionary<string, string>? row)
    {
        if (row is null || row.Count == 0)
        {
            return "No Excel row selected";
        }

        var partNo = GetRowValue(row, "PartNo", "Part No", "PN", "MaHang", "Ma Hang", "Mã hàng");
        var name = GetRowValue(row, "Name", "ItemName", "Item Name", "Ten", "Tên", "TenHang", "Ten Hang", "Tên hàng");
        var lot = GetRowValue(row, "Lot", "LotNo", "Lot No", "Batch");
        var qty = GetRowValue(row, "Qty", "Quantity", "SoLuong", "So Luong", "Số lượng");
        var summary = string.Join(" | ", new[]
        {
            string.IsNullOrWhiteSpace(partNo) ? string.Empty : $"PartNo: {partNo}",
            string.IsNullOrWhiteSpace(name) ? string.Empty : $"Name: {name}",
            string.IsNullOrWhiteSpace(lot) ? string.Empty : $"Lot: {lot}",
            string.IsNullOrWhiteSpace(qty) ? string.Empty : $"Qty: {qty}"
        }.Where(value => !string.IsNullOrWhiteSpace(value)));

        return string.IsNullOrWhiteSpace(summary) ? string.Join(" | ", row.Take(4).Select(pair => $"{pair.Key}: {pair.Value}")) : summary;
    }

    private static string GetRowValue(IReadOnlyDictionary<string, string>? row, params string[] names)
    {
        if (row is null)
        {
            return string.Empty;
        }

        foreach (var name in names)
        {
            if (row.TryGetValue(name, out var value))
            {
                return value;
            }
        }

        var normalizedNames = names.Select(NormalizeKey).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in row)
        {
            if (normalizedNames.Contains(NormalizeKey(pair.Key)))
            {
                return pair.Value;
            }
        }

        return string.Empty;
    }

    private static string NormalizeKey(string value)
    {
        return new string(value.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());
    }

    // ==================== Print history ====================

    private PrintLogEntry CreatePrintLogEntry(IReadOnlyDictionary<string, string>? row, int labelCount, int labelIndex, string outcomeStatus)
    {
        return new PrintLogEntry
        {
            TemplateName = _template.Name,
            TemplateFilePath = _templateFilePath,
            PrinterName = PrinterName,
            LabelWidthMm = _template.WidthMm,
            LabelHeightMm = _template.HeightMm,
            Dpi = _template.PrinterProfile.Dpi,
            PrintMode = "Selected rows",
            RowCount = _trackingRows.Count(r => r.IsSelected),
            LabelCount = labelCount,
            LabelIndex = labelIndex,
            ExcelFilePath = _template.DatabaseConfig.FilePath,
            ExcelSheetName = _template.DatabaseConfig.SheetName,
            PartNo = GetRowValue(row, "PartNo", "Part No", "PN", "MaHang", "Ma Hang", "Mã hàng"),
            ItemName = GetRowValue(row, "Name", "ItemName", "Item Name", "Ten", "Tên", "TenHang", "Ten Hang", "Tên hàng"),
            Lot = GetRowValue(row, "Lot", "LotNo", "Lot No", "Batch"),
            Quantity = GetRowValue(row, "Qty", "Quantity", "SoLuong", "So Luong", "Số lượng"),
            LabelContent = CreateLabelContent(row),
            RowData = row is null ? string.Empty : string.Join("; ", row.Select(pair => $"{pair.Key}={pair.Value}")),
            Notes = $"Submitted from Ctrl+P preview. {outcomeStatus}"
        };
    }

    private string CreateLabelContent(IReadOnlyDictionary<string, string>? row)
    {
        return string.Join(" | ", _template.Objects
            .Where(item => item.IsVisible)
            .OrderBy(item => item.ZIndex)
            .Select(item => CreateObjectContent(item, row))
            .Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static string CreateObjectContent(LabelObject item, IReadOnlyDictionary<string, string>? row)
    {
        var value = string.IsNullOrWhiteSpace(item.BindingExpression)
            ? item.Text
            : ResolveExpression(item.BindingExpression, row);
        return string.IsNullOrWhiteSpace(value) ? string.Empty : $"{item.Name}={value}";
    }

    private static string ResolveExpression(string expression, IReadOnlyDictionary<string, string>? row)
    {
        if (row is null)
        {
            return expression;
        }

        return FormulaBindingEvaluator.LooksLikeFormula(expression)
            ? FormulaBindingEvaluator.Evaluate(expression, row).Value
            : BindingExpressionEvaluator.Evaluate(expression, row);
    }

    private async Task WritePrintHistoryAsync(IReadOnlyDictionary<string, string>?[] rows, string outcomeStatus)
    {
        try
        {
            await _printLogService.AppendManyAsync(rows.Select((row, index) => CreatePrintLogEntry(row, rows.Length, index + 1, outcomeStatus)));
        }
        catch (IOException ex)
        {
            MessageBox.Show(
                this,
                $"Print job was sent, but print history could not be saved.\n\n{ex.Message}",
                "Print history not saved",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }
}

public class BoolToCheckmarkConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is true ? "☑" : "☐";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public sealed record PrintMethodOption(PrintMethod Value, string DisplayName);
