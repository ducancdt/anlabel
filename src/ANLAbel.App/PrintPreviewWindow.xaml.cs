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
    private readonly LabelTemplate _sourceTemplate;
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
    private IReadOnlyList<LabelInstance> _previewInstances = Array.Empty<LabelInstance>();
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
    private readonly bool _excelBaselineMissing;
    private IReadOnlyDictionary<string, string>?[] _allRowsCache = Array.Empty<IReadOnlyDictionary<string, string>?>();
    private readonly DispatcherTimer _filterDebounceTimer = new() { Interval = TimeSpan.FromMilliseconds(250) };
    private const string AllColumnsFilterOption = "(Tất cả cột)";
    private const int PreviewCacheCapacity = 8;
    private readonly Dictionary<int, PreviewRasterResult> _previewImageCache = new();
    private readonly LinkedList<int> _previewImageLru = new();
    private PrintRenderPlan? _previewPlan;
    private string _previewPlanPrinterName = string.Empty;
    private PrintPreflightIssue? _previewPlanIssue;
    private PreviewTrustLevel _previewTrustLevel = PreviewTrustLevel.DesignOnly;

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
        _sourceTemplate = template;
        _template = PrintSessionTemplateContract.Capture(template);

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
        // The baseline is the identity captured when the rows were imported
        // (DB06). Linked rows without one cannot be proven current.
        _excelKnownSourceIdentity = excelSourceIdentity;
        _excelBaselineMissing = excelSourceIdentity is null
            && excelDataView is { Count: > 0 }
            && !string.IsNullOrWhiteSpace(template.DatabaseConfig.FilePath);

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

    public string PreviewTitle => $"Xem trước khi in - {_template.Name}";
    public LabelTemplate LabelTemplate => _template;
    public IReadOnlyList<PrintMethodOption> PrintMethodOptions { get; } = new[]
    {
        new PrintMethodOption(PrintMethod.ApplicationGraphic, "Đồ họa ứng dụng (Khớp bản thiết kế)"),
        new PrintMethodOption(PrintMethod.PrinterNative, "Lệnh máy in gốc (Lệnh nhiệt native)")
    };
    public List<PrintPreviewPageViewModel> Pages { get; } = new();
    public PrintPreviewPageViewModel? CurrentPage => Pages.Count == 0 ? null : Pages[Math.Max(0, Math.Min(Pages.Count - 1, _currentPageIndex))];
    public string PrinterName => string.IsNullOrWhiteSpace(_selectedPrinterName) ? "(chưa chọn máy in)" : _selectedPrinterName;
    public string LabelSizeText
    {
        get
        {
            var plan = _previewPlan;
            var dpi = plan is { DpiX: > 0, DpiY: > 0 }
                ? $"{plan.DpiX}×{plan.DpiY}"
                : $"{_template.PrinterProfile.Dpi}";
            return $"Nhãn: {(plan?.LabelWidthMm ?? _template.WidthMm):0.##} × {(plan?.LabelHeightMm ?? _template.HeightMm):0.##} mm | DPI: {dpi}";
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
                    ? $"Kế hoạch: {profile.Dpi} DPI (chỉ thiết kế)"
                    : $"Kế hoạch: {plan.DpiX}×{plan.DpiY} DPI{(plan.OutputContractTicketVerified ? string.Empty : " (chỉ thiết kế)")}",
                $"độ lệch {profile.OffsetXMm:0.##}/{profile.OffsetYMm:0.##} mm"
            };
            parts.Add(plan?.PrintableAreaVerified == true ? "vùng in đã xác thực" : "chưa xác thực vùng in");
            if (profile.Rotated180)
            {
                parts.Add("xoay 180°");
            }
            if (_template.MarginMm > 0)
            {
                parts.Add($"lề {_template.MarginMm:0.##} mm");
            }
            return string.Join(" · ", parts);
        }
    }
    public string PageCountText => $"Số nhãn: {Pages.Count}";
    public string PageInput
    {
        get => _pageInput;
        set => _pageInput = value;
    }
    public string PageStatusText => Pages.Count == 0 ? "Không có nhãn" : $"Nhãn {_currentPageIndex + 1} / {Pages.Count}";
    public string CurrentRowSummary => CreateRowSummary(_previewRows.ElementAtOrDefault(_currentPageIndex));
    public string SelectedRowsText
    {
        get
        {
            var selectedCount = _trackingRows.Count(r => r.IsSelected);
            var totalCount = _trackingRows.Count;
            var totalCopies = _trackingRows.Where(r => r.IsSelected).Sum(r => r.Copies);
            return $"Đã chọn {selectedCount}/{totalCount} ({totalCopies} nhãn)";
        }
    }
    public bool HasPreflightIssues => !_isPreviewBusy && _preflightResult.Issues.Count > 0;
    public bool HasBlockingPreflightIssues => !_isPreviewBusy && !_preflightResult.IsSuccess;
    public IReadOnlyList<PreviewContentIssue> PreflightIssues => _contentIssues;
    public string PreflightStatusText => _isPreviewBusy
        ? "Đang kiểm tra các nhãn đã chọn..."
        : _previewTrustLevel == PreviewTrustLevel.Stale
            ? "Kiểm tra nội dung chưa hoàn tất. Hãy làm mới danh sách chọn trước khi in."
            : _previewRows.Length == 0
                ? "Chưa chọn nhãn nào. Chọn các dòng bên dưới để xem trước."
                : !_preflightResult.IsSuccess
                    ? "Nội dung cần chú ý. Sửa các vấn đề bên dưới hoặc bỏ chọn các dòng bị ảnh hưởng."
                    : _preflightResult.Issues.Count > 0
                        ? "Chỉ là cảnh báo rủi ro quét mã — vẫn cho phép in. Hãy mở rộng mã vạch hoặc tăng DPI nếu đầu đọc khó quét."
                        : "Nội dung các nhãn đã chọn đã vượt qua kiểm tra.";
    public string PreflightIssuesSummary => $"{_contentIssues.Count} vấn đề nội dung riêng biệt. Các bản sao lặp lại đã được gộp nhóm.";
    public string PrinterStatusText => _isPreviewBusy
        ? "Đang kiểm tra xem trước..."
        : _previewPlanIssue?.Message
          ?? (_previewPlan?.OutputContractTicketVerified == true
              ? "Cài đặt máy in đã chọn đã được xác thực."
              : "Cài đặt máy in chưa được xác thực. Hãy xem lại Cài đặt máy in trước khi in.");
    public bool IsPreviewBusy => _isPreviewBusy;
    public bool CanCancelPreview => _isPreviewBusy;
    public int PreviewProgressPercent => _previewProgressPercent;
    public string PreviewProgressText => _previewProgressText;
    public bool IsPrintBusy => _isPrintBusy;
    public PreviewTrustLevel PreviewTrustLevel => _previewTrustLevel;
    public string PreviewTrustStatusText => _previewTrustLevel switch
    {
        PreviewTrustLevel.EffectiveReady => "Sẵn sàng in",
        PreviewTrustLevel.EffectiveUnverified => "Chỉ xem trước — máy in chưa xác thực",
        PreviewTrustLevel.EffectiveBlocked => _previewPlanIssue is null
            ? "Đã chặn in — nội dung cần chú ý"
            : "Chỉ xem trước — cài đặt máy in cần chú ý",
        PreviewTrustLevel.Stale => "Đang kiểm tra xem trước",
        _ => "Chỉ xem trước — chưa chọn máy in"
    };
    public bool CanPrint => _previewTrustLevel == PreviewTrustLevel.EffectiveReady
        && CanBeginPrint(_isPrintBusy, _isPreviewBusy, _previewRows.Length);
    public bool CanRequestPrint => CanBeginPrint(_isPrintBusy, _isPreviewBusy, _previewRows.Length);
    public double PreviewZoom
    {
        get => _previewZoom;
        private set
        {
            _previewZoom = Math.Max(0.25, Math.Min(4, Math.Round(value, 2)));
            OnPropertyChanged();
        }
    }

    private async void LabelPrinterSetup_Click(object sender, RoutedEventArgs e)
    {
        var printers = await ANLAbel.App.Services.PrinterQueueLoader.LoadForDialogAsync(this, _printerDiscoveryService.DiscoverInstalledPrinters);
        if (printers is null)
        {
            return;
        }

        var dialog = new PrinterSetupWindow(
            printers,
            _selectedPrinterName,
            string.IsNullOrWhiteSpace(_template.PrinterProfile.LabelStockName)
                ? _template.PrinterProfile.PaperName
                : _template.PrinterProfile.LabelStockName,
            _template.Orientation,
            _template.PrinterProfile.Dpi,
            _template.PrinterProfile.PaperSizeSource,
            _template.PrinterProfile.DriverMediaIdentity,
            _template.PrinterProfile.SettingsSource,
            initialWidthMm: _template.WidthMm,
            initialHeightMm: _template.HeightMm,
            initialOffsetXMm: _template.PrinterProfile.OffsetXMm,
            initialOffsetYMm: _template.PrinterProfile.OffsetYMm,
            initialScaleX: _template.PrinterProfile.ScaleX,
            initialScaleY: _template.PrinterProfile.ScaleY,
            workingTemplate: _template,
            printService: _printService) { Owner = this };
        if (dialog.ShowDialog() == true)
        {
            if (dialog.SelectedPrinter is not null && dialog.SelectedPaper is not null)
            {
                try
                {
                    PrinterSelectionApplier.Apply(
                        _template,
                        dialog.SelectedPrinter,
                        dialog.SelectedPaper,
                        dialog.SelectedDpi,
                        dialog.SelectedOrientation,
                        dialog.SelectedMedia,
                        dialog.SelectedPageSizeSource,
                        dialog.SelectedDriverIdentity,
                        dialog.SelectedSettingsSource,
                        dialog.SelectedOffsetXMm,
                        dialog.SelectedOffsetYMm,
                        dialog.SelectedScaleX,
                        dialog.SelectedScaleY);
                    _selectedPrinterName = dialog.SelectedPrinter.Name;
                }
                catch (InvalidOperationException ex)
                {
                    MessageBox.Show(this, ANLAbel.App.Services.PreflightIssueText.Sentence(ex.Message), "Cài đặt máy in", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            _previewPlan = null;
            _previewPlanPrinterName = string.Empty;
            _previewPlanIssue = null;
            RefreshPreview();
        }
    }

    private void SavePrintSettingsToLabel_Click(object sender, RoutedEventArgs e)
    {
        if (!ValidateLabelDimensions())
        {
            return;
        }

        ApplyPrintSetup();
        PrintSessionTemplateContract.CommitPrintConfiguration(_template, _sourceTemplate);
        MessageBox.Show(
            this,
            "Kích thước nhãn và cài đặt máy in hiện tại đã được lưu vào mẫu tem đang mở. Sử dụng lệnh Lưu trong trình thiết kế để ghi vào tệp trên đĩa.",
            "Đã lưu cài đặt in vào mẫu tem",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private async void Print_Click(object sender, RoutedEventArgs e)
    {
        if (!CanRequestPrint)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_selectedPrinterName))
        {
            LabelPrinterSetup_Click(sender, e);
            return;
        }

        _isPrintBusy = true;
        OnPropertyChanged();
        var printJobId = string.Empty;
        PrintJobManifest? manifest = null;
        PrintJobResult? dispatchedPrintResult = null;
        try
        {
            if (_isPreviewBusy)
            {
                return;
            }

            if (await TryBlockStaleLinkedExcelDataAsync())
            {
                return;
            }

            // DB08/DB10: invalid configured CopiesField on any still-selected tracking
            // row must block the whole batch. Do this before expanding copies — Resolve
            // returning 0 would otherwise silently drop the bad row and print the rest.
            if (!TryAuthorizeCopiesPrepare(out var copiesIssues))
            {
                if (_previewTrustLevel != PreviewTrustLevel.DesignOnly)
                {
                    _previewTrustLevel = PreviewTrustLevel.EffectiveBlocked;
                }
                SetPreflightResult(ToCopiesPreflightResult(copiesIssues));
                OnPropertyChanged();
                MessageBox.Show(
                    this,
                    ANLAbel.App.Services.PreflightIssueText.Describe(ToCopiesPreflightResult(copiesIssues)),
                    "Lệnh in bị chặn",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            ApplyPrintSetup();
            var selected = GetSelectedRowsWithSource();
            if (selected.Length == 0)
            {
                MessageBox.Show(this, "Chưa chọn dòng nào để in. Sử dụng các hộp kiểm để chọn dòng.", "In", MessageBoxButton.OK, MessageBoxImage.Information);
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
                var contractStop = ANLAbel.App.Services.PrintFailureExplainer.Explain(ex.Message);
                OperatorDialog.Ask(
                    this,
                    "Chưa xác nhận được cài đặt máy in",
                    contractStop.IsGeneric
                        ? ["ANLAbel chưa xác nhận được cài đặt của máy in đã chọn nên đã dừng chuẩn bị in. Chưa có tem nào được gửi.", "Kiểm tra máy in đang bật, mở Cài đặt máy in rồi thử lại."]
                        : [contractStop.Problem, contractStop.NextAction],
                    $"Chi tiết kỹ thuật: {contractStop.Technical}",
                    OperatorDialogTone.Warning,
                    new OperatorDialogChoice("ok", "Đóng", IsDefault: true, IsCancel: true));
                return;
            }

            // A freshly read ticket or a quantity edit that has not been prepared
            // again is not the snapshot Ready named. Drop that mark before any
            // dialog can return and leave the old status on screen.
            var outputUnchanged = PreviewInitializationTrust.OutputSnapshotUnchanged(
                _previewPlan?.DocumentHash,
                _previewPlan?.OutputContractHash,
                _previewPlan?.OutputContractTicketVerified == true,
                effectivePlan.DocumentHash,
                effectivePlan.OutputContractHash,
                effectivePlan.OutputContractTicketVerified);
            var quantityUnchanged = PreviewInitializationTrust.QuantitySnapshotMatches(
                _previewInstances,
                _trackingRows.Select(row => (row.SourceRowNumber, row.IsSelected ? row.Copies : 0)));
            _previewTrustLevel = PreviewInitializationTrust.RetainForCurrentSnapshot(
                _previewTrustLevel,
                outputUnchanged && quantityUnchanged && _previewPlan is not null);
            if (_previewPlan is not null && !(outputUnchanged && quantityUnchanged))
            {
                // The operator prints what the preview shows: a printer setting or
                // selection that changed since it was prepared is shown first
                // (audit 2026-10-13). Nothing is sent on this click.
                _previewPlan = effectivePlan;
                _previewPlanPrinterName = _selectedPrinterName ?? string.Empty;
                _previewPlanIssue = null;
                OnPropertyChanged();
                RefreshPreview();
                OperatorDialog.Ask(
                    this,
                    "Bản xem trước đã được cập nhật",
                    [
                        "Cài đặt máy in hoặc các dòng và số bản đã thay đổi so với bản đang xem trước. ANLAbel đã làm mới bản xem trước và chưa gửi lệnh in.",
                        "Kiểm tra lại các tem rồi bấm In."
                    ],
                    null,
                    OperatorDialogTone.Normal,
                    new OperatorDialogChoice("ok", "Đóng", IsDefault: true, IsCancel: true));
                return;
            }
            // Đợt 4 item 11 — "chống trùng tem": warn before re-sending a row that was
            // already printed earlier in this same preview session.
            // Keep the exact effective queue/driver plan for any preview
            // refresh or rerender that follows this preparation step.
            _previewPlan = effectivePlan;
            OnPropertyChanged();
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
                    $"{alreadyPrintedRows.Length} dòng đã chọn (Dòng {string.Join(", ", alreadyPrintedRows)}) đã được gửi tới máy in trong phiên này.\n\nIn lại có thể tạo tem trùng. Tiếp tục?",
                    "Các dòng đã gửi tới máy in",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                if (choice != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            var rows = selected.Select(entry => entry.Row).ToArray();
            var preflight = await ValidateRowsForUiAsync(rows, selected.Select(entry => entry.SourceRowNumber).ToArray(), "Đang kiểm tra các nhãn đã chọn", effectivePlan);
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
                    SetPreflightResult(preflight, selected.Select(entry => entry.SourceRowNumber).ToArray());
                    if (_previewTrustLevel != PreviewTrustLevel.DesignOnly)
                    {
                        _previewTrustLevel = PreviewTrustLevel.EffectiveBlocked;
                    }
                    OnPropertyChanged();
                    MessageBox.Show(this, ANLAbel.App.Services.PreflightIssueText.Describe(preflight, SourceRowOf(selected)), "Lệnh in bị chặn", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var remainingCount = totalSelectedRows - badSourceRows.Length;
                var choice = MessageBox.Show(
                    this,
                    $"{badSourceRows.Length} dòng có lỗi và sẽ được bỏ qua: dòng {string.Join(", ", badSourceRows)}.\n\n{string.Join("\n", ANLAbel.App.Services.PreflightIssueText.Lines(preflight, SourceRowOf(selected)))}\n\nTiếp tục in {remainingCount} dòng còn lại?",
                    "Một số dòng bị chặn",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Warning);
                if (choice != MessageBoxResult.Yes)
                {
                    SetPreflightResult(preflight, selected.Select(entry => entry.SourceRowNumber).ToArray());
                    if (_previewTrustLevel != PreviewTrustLevel.DesignOnly)
                    {
                        _previewTrustLevel = PreviewTrustLevel.EffectiveBlocked;
                    }
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
                    MessageBox.Show(this, "Không còn dòng nào để in sau khi bỏ qua các dòng lỗi.", "In", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                preflight = await ValidateRowsForUiAsync(rows, selected.Select(entry => entry.SourceRowNumber).ToArray(), "Đang kiểm tra lại các nhãn còn lại", effectivePlan);
                if (!preflight.IsSuccess)
                {
                    SetPreflightResult(preflight, selected.Select(entry => entry.SourceRowNumber).ToArray());
                    if (_previewTrustLevel != PreviewTrustLevel.DesignOnly)
                    {
                        _previewTrustLevel = PreviewTrustLevel.EffectiveBlocked;
                    }
                    OnPropertyChanged();
                    MessageBox.Show(this, ANLAbel.App.Services.PreflightIssueText.Describe(preflight, SourceRowOf(selected)), "Lệnh in bị chặn", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            // Preflight can take long enough for an upstream spreadsheet save to
            // finish after the initial click-time check. Revalidate immediately
            // before a job/manifest is created, so a stale source never crosses
            // the durable dispatch boundary.
            if (await TryBlockStaleLinkedExcelDataAsync())
            {
                return;
            }

            printJobId = _approvedReprintJobId ?? Guid.NewGuid().ToString("N");
            var (previewLabelCount, previewSourceRowCount) = PrintJobEvidencePolicy.ResolveManifestCounts(
                rows.Length,
                selected.Select(entry => entry.SourceRowNumber).Distinct().Count());
            manifest = PrintJobManifest.Create(
                _template.Name,
                _templateFilePath,
                $"{_template.Name} preview print",
                _selectedPrinterName ?? string.Empty,
                _template.WidthMm,
                _template.HeightMm,
                effectivePlan.DpiX > 0 ? effectivePlan.DpiX : _template.PrinterProfile.Dpi,
                effectivePlan.DpiY > 0 ? effectivePlan.DpiY : _template.PrinterProfile.Dpi,
                previewLabelCount,
                previewSourceRowCount,
                rows,
                effectivePlan.DocumentHash,
                effectivePlan.TextResourceFingerprint,
                effectivePlan.SceneHash,
                effectivePlan.OutputContractHash,
                imageRasterFingerprint: effectivePlan.ImageRasterFingerprint,
                thermalRasterGoldenFingerprint: effectivePlan.ThermalRasterGolden?.Fingerprint ?? string.Empty,
                sequence: selected.Select(entry => entry.Copy).ToArray());
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
                        "Lệnh in lại bị chặn: mẫu tem, máy in, DPI hoặc dữ liệu đã chọn không còn khớp với bản kê khai (manifest) đã phê duyệt.",
                        "Sai lệch kê khai",
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
            dispatchedPrintResult = printResult;
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
            if (printResult.Outcome == PrintJobOutcome.Unknown)
            {
                // P07: part of the job may have printed. Mark the rows as sent so a
                // second print in this session warns about duplicates, and send the
                // operator to reconcile instead of retrying.
                var uncertainRows = selected.Select(entry => entry.SourceRowNumber).ToHashSet();
                foreach (var row in _trackingRows.Where(item => uncertainRows.Contains(item.SourceRowNumber)))
                {
                    row.IsPrinted = true;
                }

                var uncertain = ANLAbel.App.Services.PrintFailureExplainer.Explain(printResult.ErrorMessage);
                OperatorDialog.Ask(
                    this,
                    "Chưa rõ kết quả in",
                    uncertain.IsGeneric
                        ? ["Không xác định được lệnh in đã ra máy in hay chưa.", "Kiểm tra máy in và đối soát lệnh này trong Trung tâm in trước khi in lại. ANLAbel không tự in lại."]
                        : [uncertain.Problem, uncertain.NextAction],
                    string.IsNullOrWhiteSpace(printResult.ErrorMessage) ? null : $"Chi tiết kỹ thuật: {printResult.ErrorMessage}",
                    OperatorDialogTone.Warning,
                    new OperatorDialogChoice("ok", "Đóng", IsDefault: true, IsCancel: true));
                return;
            }

            if (!printResult.IsAccepted)
            {
                if (printResult.Outcome == PrintJobOutcome.Failed && !string.IsNullOrWhiteSpace(printResult.ErrorMessage))
                {
                    var notCompleted = ANLAbel.App.Services.PrintFailureExplainer.Explain(printResult.ErrorMessage);
                    OperatorDialog.Ask(this, "Lệnh in chưa hoàn tất", [notCompleted.Problem, notCompleted.NextAction], $"Chi tiết kỹ thuật: {notCompleted.Technical}", OperatorDialogTone.Warning, new OperatorDialogChoice("ok", "Đóng", IsDefault: true, IsCancel: true));
                }
                else
                {
                    MessageBox.Show(this, ANLAbel.App.Services.PrintStatusText.Result(printResult), "Lệnh in chưa hoàn tất", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                return;
            }

            // ADR-023 / plan 13 QĐ-10: rows the spooler accepted are marked as
            // "sent", so a second print of the same rows in this session warns
            // about duplicate labels. This is not a claim of physical output.
            if (printResult.IsAccepted)
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
                statusText += $"\nTrạng thái hàng đợi: {ANLAbel.App.Services.PrintStatusText.Queue(spoolStatus)}";
            }

            var printedRowCount = selected.Select(entry => entry.SourceRowNumber).Distinct().Count();
            var summary = $"Đã gửi {rows.Length} nhãn từ {printedRowCount} dòng đến máy in {printResult.PrinterName}. {statusText}";
            if (skippedSourceRows.Length > 0)
            {
                summary += $" Đã bỏ qua {skippedSourceRows.Length} dòng gặp lỗi tiền kiểm: Dòng {string.Join(", ", skippedSourceRows)}.";
            }
            summary += $" Nhật ký: {_printLogService.LogFilePath}";
            _previewProgressText = summary;
            _ = WritePrintHistoryAsync(rows, statusText);
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

            _previewProgressText = "Đã hủy tiền kiểm in.";
            OnPropertyChanged();
        }
        catch (Exception ex)
        {
            // P07: after the spooler accepted the job, a later failure leaves the
            // outcome uncertain. Recording Failed would invite a duplicate reprint.
            var acceptedBeforeFailure = dispatchedPrintResult?.IsAccepted == true;
            var failureState = acceptedBeforeFailure ? PrintJobLifecycleState.Unknown : PrintJobLifecycleState.Failed;
            if (!string.IsNullOrWhiteSpace(printJobId))
            {
                var currentState = _printJobStateStore.GetCurrentState(printJobId);
                if (currentState is PrintJobLifecycleState current
                    && PrintJobStateMachine.CanTransition(current, failureState))
                {
                    await RecordPrintJobTransitionAsync(new PrintJobStateTransition(
                        printJobId,
                        current,
                        failureState,
                        DateTimeOffset.UtcNow,
                        ex.Message,
                        PrinterName: _selectedPrinterName,
                        ManifestFingerprint: manifest?.Fingerprint ?? string.Empty,
                        Manifest: manifest));
                }
            }

            LogPrintOperation(0, new PrintJobResult(acceptedBeforeFailure ? PrintJobOutcome.Unknown : PrintJobOutcome.Failed, _selectedPrinterName, $"{_template.Name} preview print", 0, ex.Message, ManifestFingerprint: manifest?.Fingerprint ?? string.Empty, Manifest: manifest), jobId: printJobId);
            if (acceptedBeforeFailure)
            {
                MessageBox.Show(
                    this,
                    $"Hàng đợi Windows đã nhận lệnh in nhưng sau đó gặp lỗi.\n\nChưa rõ tem đã ra hay chưa. ANLAbel không tự in lại; hãy kiểm tra máy in và đối soát lệnh trong Trung tâm in trước khi in lại.\n\nChi tiết kỹ thuật: {ex.Message}",
                    "Chưa rõ kết quả in",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            else
            {
                var failure = ANLAbel.App.Services.PrintFailureExplainer.Explain(ex.Message);
                OperatorDialog.Ask(this, "In thất bại", [failure.Problem, failure.NextAction], $"Chi tiết kỹ thuật: {failure.Technical}", OperatorDialogTone.Error, new OperatorDialogChoice("ok", "Đóng", IsDefault: true, IsCancel: true));
            }
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
        // Pre-dispatch Preparing/PreflightPassed/Dispatching must persist or block
        // send (CORE P08). Post-dispatch appends stay best-effort.
        await PrintJobEvidenceAppend.AppendAsync(_printJobStateStore, transition).ConfigureAwait(true);
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
    private async Task<bool> IsLinkedExcelDataStaleAsync()
    {
        if (_excelBaselineMissing)
        {
            return true;
        }

        if (_excelKnownSourceIdentity is null)
        {
            return false;
        }

        // Hashing a large workbook must not freeze the preview (plan 13 H17).
        var current = await Task.Run(TryGetExcelSourceIdentity).ConfigureAwait(true);
        return IsSourceSnapshotStale(_excelKnownSourceIdentity, current);
    }

    private async Task<bool> TryBlockStaleLinkedExcelDataAsync()
    {
        if (!await IsLinkedExcelDataStaleAsync())
        {
            return false;
        }

        _previewTrustLevel = PreviewInitializationTrust.RetainForCurrentSnapshot(
            _previewTrustLevel,
            snapshotStillCurrent: false);
        OnPropertyChanged();
        MessageBox.Show(
            this,
            "Tệp dữ liệu liên kết đã thay đổi hoặc không đọc được kể từ lần nhập dữ liệu cuối. Việc in bị chặn vì các dòng đang hiển thị có thể đã lỗi thời.\n\nĐóng cửa sổ này, bấm Cập nhật Excel, rồi mở lại Xem trước khi in.",
            "Dữ liệu Excel đã thay đổi",
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

    private void PreflightIssue_Click(object sender, RoutedEventArgs e)
    {
        if (_isPreviewBusy)
        {
            return;
        }

        if (sender is not Button { Tag: int rowNumber } || rowNumber <= 0)
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
            DemoteReadyIfPreparedQuantityDisagrees();
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
            ? $"Không có dòng nào khớp \"{filterText}\""
            : $"Khớp {visible.Count} / {_trackingRows.Count} dòng — đã chọn để in";
        DemoteReadyIfPreparedQuantityDisagrees();
    }

    private void DemoteReadyIfPreparedQuantityDisagrees()
    {
        var quantityUnchanged = PreviewInitializationTrust.QuantitySnapshotMatches(
            _previewInstances,
            _trackingRows.Select(row => (row.SourceRowNumber, row.IsSelected ? row.Copies : 0)));
        var next = PreviewInitializationTrust.RetainForCurrentSnapshot(_previewTrustLevel, quantityUnchanged);
        if (next == _previewTrustLevel)
        {
            return;
        }

        _previewTrustLevel = next;
        OnPropertyChanged();
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

        var operation = BeginPreviewOperation("Đang vẽ bản xem trước tem", invalidateContent: false);
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
                _previewProgressText = "Bản xem trước đã sẵn sàng.";
                OnPropertyChanged();
            }
        }
        catch (OperationCanceledException)
        {
            if (IsCurrentPreviewOperation(operation))
            {
                keepStatus = true;
                _previewProgressText = "Đã hủy vẽ bản xem trước.";
                OnPropertyChanged();
            }
        }
        catch (Exception ex)
        {
            if (IsCurrentPreviewOperation(operation))
            {
                keepStatus = true;
                _previewProgressText = "Không vẽ được bản xem trước: " + ANLAbel.App.Services.OperationFailureExplainer.StatusLine(ex);
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


    private static PreparedPreviewRows BuildPreparedRowsFromTracking(
        IReadOnlyList<TrackingRowViewModel> trackingRows,
        IReadOnlyList<IReadOnlyDictionary<string, string>?> allRows)
    {
        var quantities = new List<PrintRecordQuantity>(trackingRows.Count);
        foreach (var vm in trackingRows)
        {
            quantities.Add(new PrintRecordQuantity(
                string.IsNullOrWhiteSpace(vm.RecordId)
                    ? CreateSessionRecordId(vm.SourceRowNumber)
                    : vm.RecordId,
                vm.SourceRowNumber,
                vm.IsSelected ? vm.Copies : 0));
        }

        var instances = PrintSequenceBuilder.Build(new PrintIntent(new QuantityPlan(quantities)));

        var rows = instances.Select(instance =>
            instance.SourceRowNumber > 0 && instance.SourceRowNumber <= allRows.Count
                ? allRows[instance.SourceRowNumber - 1]
                : null).ToArray();
        return new PreparedPreviewRows(rows, instances);
    }

    /// <summary>
    /// Writes the flattened <see cref="LabelInstance.SequenceOrdinal"/> back onto the
    /// data-bound tracking rows (fires PropertyChanged on the visible grid). This is a
    /// deliberate observable side effect and MUST only be invoked for the winning/current
    /// preview operation on the UI thread — never from a superseded refresh or a Get* accessor.
    /// </summary>
    private static void ApplyTrackingPageNumbers(
        IReadOnlyList<TrackingRowViewModel> trackingRows,
        IReadOnlyList<LabelInstance> instances)
    {
        foreach (var vm in trackingRows)
        {
            vm.PageNumber = instances.FirstOrDefault(instance =>
                instance.SourceRowNumber == vm.SourceRowNumber)?.SequenceOrdinal ?? 0;
        }
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

        // Source-file order, so row numbers do not follow a sort of the data grid (DB07).
        var records = _excelDataView.Table is { } table
            ? table.Rows.Cast<DataRow>()
            : _excelDataView.Cast<DataRowView>().Select(item => item.Row);
        foreach (var record in records)
        {
            yield return record.Table.Columns
                .Cast<DataColumn>()
                .ToDictionary(column => column.ColumnName, column => record[column]?.ToString() ?? string.Empty, StringComparer.OrdinalIgnoreCase);
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
    private (IReadOnlyDictionary<string, string>? Row, int SourceRowNumber, PrintDispatchCopy Copy)[] GetSelectedRowsWithSource()
    {
        var prepared = BuildPreparedRowsFromTracking(_trackingRows, GetRows().ToArray());
        return prepared.Instances.Select((instance, index) =>
            (prepared.Rows[index],
             instance.SourceRowNumber,
             new PrintDispatchCopy(instance.SourceRowNumber, instance.CopyOrdinal, instance.SequenceOrdinal))).ToArray();
    }

    private static string CreateSessionRecordId(int sourceRowNumber) => $"source-row-{sourceRowNumber}";

    private sealed record PreparedPreviewRows(
        IReadOnlyDictionary<string, string>?[] Rows,
        IReadOnlyList<LabelInstance> Instances);

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
        // Quantity for the grid only. Invalid configured cells resolve to 0;
        // ValidateCopiesFieldForSelectedTracking blocks prepare/print with the diagnostic.
        return DatabaseConfig.TryResolveCopiesForRow(
            _template.DatabaseConfig.CopiesField,
            row,
            out var copies,
            out _)
            ? copies
            : 0;
    }

    /// <summary>
    /// Prepare/print gate for configured CopiesField (CORE DB08/DB10). Must run
    /// against still-selected tracking rows before copy expansion.
    /// </summary>
    private bool TryAuthorizeCopiesPrepare(out IReadOnlyList<CopiesFieldIssue> blockingIssues)
    {
        var allRows = _allRowsCache.Length > 0 ? _allRowsCache : GetRows().ToArray();
        return CopiesFieldContract.TryAuthorizePrepare(
            _template.DatabaseConfig.CopiesField,
            _trackingRows
                .Where(row => row.IsSelected)
                .Select(row => (
                    row.SourceRowNumber,
                    row.SourceRowNumber > 0 && row.SourceRowNumber <= allRows.Length
                        ? allRows[row.SourceRowNumber - 1]
                        : null)),
            out blockingIssues);
    }

    private const string CopiesIssueObjectType = "Copies";

    // Validator issue rows are label ordinals; the operator sees data rows.
    private static Func<int, int> SourceRowOf((IReadOnlyDictionary<string, string>? Row, int SourceRowNumber, PrintDispatchCopy Copy)[] selected)
        => row => row > 0 && row <= selected.Length ? selected[row - 1].SourceRowNumber : 0;

    private static PrintPreflightResult ToCopiesPreflightResult(IReadOnlyList<CopiesFieldIssue> issues)
    {
        return new PrintPreflightResult(issues
            .Select(issue => new PrintPreflightIssue(
                issue.SourceRowNumber,
                issue.FieldName,
                CopiesIssueObjectType,
                issue.Message,
                IsBlocking: true))
            .ToArray());
    }

    private void ApplyPrintSetup()
    {
        _template.PrinterProfile.LabelWidthMm = _template.WidthMm;
        _template.PrinterProfile.LabelHeightMm = _template.HeightMm;
        _template.Orientation = LabelGeometry.ResolveOrientation(_template.WidthMm, _template.HeightMm);
    }

    private bool ValidateLabelDimensions()
    {
        var physicalWidth = _template.PrinterProfile.PhysicalWidthMm > 0 ? _template.PrinterProfile.PhysicalWidthMm : _template.WidthMm;
        var physicalHeight = _template.PrinterProfile.PhysicalHeightMm > 0 ? _template.PrinterProfile.PhysicalHeightMm : _template.HeightMm;
        var stock = LabelStockContract.Evaluate(
            _template.WidthMm,
            _template.HeightMm,
            physicalWidth,
            physicalHeight,
            _template.PrinterProfile.PaperName);
        if (!stock.IsAllowed)
        {
            MessageBox.Show(this, ANLAbel.App.Services.PreflightIssueText.Sentence(stock.Diagnostic), "Thiết lập nhãn", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        return true;
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
            return "Chưa chọn dòng Excel nào";
        }

        var partNo = GetRowValue(row, "PartNo", "Part No", "PN", "MaHang", "Ma Hang", "Mã hàng");
        var name = GetRowValue(row, "Name", "ItemName", "Item Name", "Ten", "Tên", "TenHang", "Ten Hang", "Tên hàng");
        var lot = GetRowValue(row, "Lot", "LotNo", "Lot No", "Batch");
        var qty = GetRowValue(row, "Qty", "Quantity", "SoLuong", "So Luong", "Số lượng");
        var summary = string.Join(" | ", new[]
        {
            string.IsNullOrWhiteSpace(partNo) ? string.Empty : $"Mã hàng: {partNo}",
            string.IsNullOrWhiteSpace(name) ? string.Empty : $"Tên: {name}",
            string.IsNullOrWhiteSpace(lot) ? string.Empty : $"Lô: {lot}",
            string.IsNullOrWhiteSpace(qty) ? string.Empty : $"SL: {qty}"
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

    /// <summary>
    /// What a print-history row needs from this window, read once on the UI
    /// thread. The rows are built on the history writer's thread and must not
    /// read live state: a preview refresh right after printing used to clear
    /// the row list mid-read and lose the job's history (audit 2026-10-13).
    /// </summary>
    private sealed record PrintHistoryContext(
        DateTime PrintedAt,
        string TemplateName,
        string TemplateFilePath,
        string PrinterName,
        double LabelWidthMm,
        double LabelHeightMm,
        int Dpi,
        int RowCount,
        string ExcelFilePath,
        string ExcelSheetName,
        (string Name, string Text, string BindingExpression)[] VisibleObjects);

    private PrintHistoryContext CapturePrintHistoryContext() => new(
        DateTime.Now,
        _template.Name,
        _templateFilePath,
        PrinterName,
        _template.WidthMm,
        _template.HeightMm,
        _template.PrinterProfile.Dpi,
        _trackingRows.Count(r => r.IsSelected),
        _template.DatabaseConfig.FilePath,
        _template.DatabaseConfig.SheetName,
        _template.Objects
            .Where(item => item.IsVisible)
            .OrderBy(item => item.ZIndex)
            .Select(item => (item.Name, item.Text, item.BindingExpression))
            .ToArray());

    private static PrintLogEntry CreatePrintLogEntry(PrintHistoryContext context, IReadOnlyDictionary<string, string>? row, int labelCount, int labelIndex, string outcomeStatus)
    {
        return new PrintLogEntry
        {
            PrintedAt = context.PrintedAt,
            TemplateName = context.TemplateName,
            TemplateFilePath = context.TemplateFilePath,
            PrinterName = context.PrinterName,
            LabelWidthMm = context.LabelWidthMm,
            LabelHeightMm = context.LabelHeightMm,
            Dpi = context.Dpi,
            PrintMode = "Selected rows",
            RowCount = context.RowCount,
            LabelCount = labelCount,
            LabelIndex = labelIndex,
            ExcelFilePath = context.ExcelFilePath,
            ExcelSheetName = context.ExcelSheetName,
            PartNo = GetRowValue(row, "PartNo", "Part No", "PN", "MaHang", "Ma Hang", "Mã hàng"),
            ItemName = GetRowValue(row, "Name", "ItemName", "Item Name", "Ten", "Tên", "TenHang", "Ten Hang", "Tên hàng"),
            Lot = GetRowValue(row, "Lot", "LotNo", "Lot No", "Batch"),
            Quantity = GetRowValue(row, "Qty", "Quantity", "SoLuong", "So Luong", "Số lượng"),
            LabelContent = CreateLabelContent(context.VisibleObjects, row),
            RowData = row is null ? string.Empty : string.Join("; ", row.Select(pair => $"{pair.Key}={pair.Value}")),
            Notes = $"Submitted from Ctrl+P preview. {outcomeStatus}"
        };
    }

    private static string CreateLabelContent(IEnumerable<(string Name, string Text, string BindingExpression)> visibleObjects, IReadOnlyDictionary<string, string>? row)
    {
        return string.Join(" | ", visibleObjects
            .Select(item => CreateObjectContent(item, row))
            .Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static string CreateObjectContent((string Name, string Text, string BindingExpression) item, IReadOnlyDictionary<string, string>? row)
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
            var context = CapturePrintHistoryContext();
            await _printLogService.AppendManyAsync(rows.Select((row, index) => CreatePrintLogEntry(context, row, rows.Length, index + 1, outcomeStatus)));
        }
        catch (Exception ex)
        {
            // Fire-and-forget: any failure is said here, never lost unobserved.
            await Dispatcher.InvokeAsync(() =>
            {
                _previewProgressText = "Đã gửi lệnh in nhưng chưa ghi được lịch sử in. " + ANLAbel.App.Services.OperationFailureExplainer.Explain(ex).StatusLine;
                OnPropertyChanged();
            });
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
