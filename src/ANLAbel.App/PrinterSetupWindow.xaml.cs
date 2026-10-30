using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;
using ANLAbel.App.Services;
using ANLAbel.Core.Enums;
using ANLAbel.Core.Geometry;
using ANLAbel.Core.Models;
using ANLAbel.Core.Printing;
using ANLAbel.Printing.PrinterProfiles;

namespace ANLAbel.App;

public partial class PrinterSetupWindow : Window
{
    private readonly IReadOnlyList<PrinterInfo> _printers;
    private readonly PrinterPreferencesService _prefsService = new();
    private readonly IPrinterCapabilityService _capabilityService;
    private readonly IPrinterPreferencesLauncher _preferencesLauncher;
    private readonly string _initialDriverMediaIdentity;
    private readonly int _authoredDpi;
    private readonly LabelTemplate? _workingTemplate;
    private readonly PrintService _printService;
    private CancellationTokenSource? _capabilityCts;
    private PrinterCapabilitySnapshot? _capabilitySnapshot;
    private string? _loadedQueueName;
    private bool _syncing;

    public PrinterSetupWindow(
        IReadOnlyList<PrinterInfo> printers,
        string? initialPrinterName = null,
        string? initialPaperName = null,
        LabelOrientation initialOrientation = LabelOrientation.Portrait,
        int initialDpi = 203,
        PaperSizeSource initialPageSizeSource = PaperSizeSource.Manual,
        string? initialDriverMediaIdentity = null,
        PrinterSettingsSource initialSettingsSource = PrinterSettingsSource.Label,
        IPrinterCapabilityService? capabilityService = null,
        IPrinterPreferencesLauncher? preferencesLauncher = null,
        double? initialWidthMm = null,
        double? initialHeightMm = null,
        double initialOffsetXMm = 0,
        double initialOffsetYMm = 0,
        double initialScaleX = 1,
        double initialScaleY = 1,
        LabelTemplate? workingTemplate = null,
        PrintService? printService = null,
        PageLayoutSelection? initialLayout = null)
    {
        _capabilityService = capabilityService ?? new PrinterCapabilityService();
        _preferencesLauncher = preferencesLauncher ?? new WindowsPrinterPreferencesLauncher();
        _initialDriverMediaIdentity = initialDriverMediaIdentity ?? string.Empty;
        _authoredDpi = initialDpi > 0 ? initialDpi : 203;
        _workingTemplate = workingTemplate;
        _printService = printService ?? new PrintService();
        InitializeComponent();
        _printers = printers;

        // Paper name may use a last-used hint when the document has none.
        // Queue, DPI, orientation and millimetres always come from the document.
        // Preferences are not a printer and 203/Portrait are authored values, not unset.
        var prefs = _prefsService.Load();
        var effectivePrinterName = DocumentPrinterIdentityContract.QueueNameFromDocument(
            initialPrinterName,
            prefs.PrinterName);
        var effectiveDpi = initialDpi;
        var effectiveOrientation = initialOrientation;

        _syncing = true;
        PrinterBox.ItemsSource = printers;
        PrinterBox.SelectedItem = PrinterDiscoveryService.ResolveNamedQueue(printers, effectivePrinterName);

        // Until a queue snapshot exists, only the authored value is evidence.
        // Generic thermal DPI values must not masquerade as capabilities of
        // whichever driver happens to be selected.
        DpiBox.ItemsSource = new[] { _authoredDpi };
        DpiBox.SelectedItem = effectiveDpi;
        PortraitRadio.IsChecked = effectiveOrientation == LabelOrientation.Portrait;
        LandscapeRadio.IsChecked = effectiveOrientation == LabelOrientation.Landscape;
        SelectedOrientation = effectiveOrientation;
        LabelSettingsRadio.IsChecked = initialSettingsSource != PrinterSettingsSource.Driver;
        DriverSettingsRadio.IsChecked = initialSettingsSource == PrinterSettingsSource.Driver;
        SelectedSettingsSource = initialSettingsSource;

        if (initialSettingsSource == PrinterSettingsSource.Driver)
        {
            DriverAutomaticRadio.IsChecked = true;
            ManualMediaRadio.IsChecked = false;
            ManualMediaRadio.IsEnabled = false;
            DriverMediaBox.IsEnabled = false;
            SelectedPageSizeSource = PaperSizeSource.DriverAutomatic;
        }
        else
        {
            DriverAutomaticRadio.IsChecked = initialPageSizeSource == PaperSizeSource.DriverAutomatic;
            ManualMediaRadio.IsChecked = initialPageSizeSource != PaperSizeSource.DriverAutomatic;
            ManualMediaRadio.IsEnabled = true;
            DriverMediaBox.IsEnabled = ManualMediaRadio.IsChecked == true;
            SelectedPageSizeSource = initialPageSizeSource;
        }

        OffsetXBox.Text = initialOffsetXMm.ToString("0.##");
        OffsetYBox.Text = initialOffsetYMm.ToString("0.##");
        var bothScaleAxesUnset = initialScaleX == 0 && initialScaleY == 0;
        ScaleXBox.Text = (bothScaleAxesUnset ? PrintScaleContract.Identity : initialScaleX).ToString("0.####");
        ScaleYBox.Text = (bothScaleAxesUnset ? PrintScaleContract.Identity : initialScaleY).ToString("0.####");

        if (initialWidthMm is > 0 && initialHeightMm is > 0)
        {
            WidthBox.Text = initialWidthMm.Value.ToString("0.##");
            HeightBox.Text = initialHeightMm.Value.ToString("0.##");
        }
        else if (double.TryParse(WidthBox.Text, out var initW) && double.TryParse(HeightBox.Text, out var initH))
        {
            var (orientedW, orientedH) = LabelGeometry.OrientSize(initW, initH, effectiveOrientation);
            WidthBox.Text = orientedW.ToString("0.##");
            HeightBox.Text = orientedH.ToString("0.##");
        }

        var layout = initialLayout ?? PageLayoutSelection.Single;
        LabelsAcrossBox.Text = Math.Max(1, layout.LabelsAcross).ToString();
        LabelsDownBox.Text = Math.Max(1, layout.LabelsDown).ToString();
        HorizontalGapBox.Text = layout.HorizontalGapMm.ToString("0.##");
        VerticalGapBox.Text = layout.VerticalGapMm.ToString("0.##");
        ProcessingOrderBox.ItemsSource = ProcessingOrders;
        ProcessingOrderBox.SelectedItem = ProcessingOrders.FirstOrDefault(order =>
            order.FillDownFirst == layout.FillDownFirst
            && order.StartFromRight == layout.StartFromRight
            && order.StartFromBottom == layout.StartFromBottom) ?? ProcessingOrders[0];
        Rotated180Box.IsChecked = layout.Rotated180;
        SelectedLayout = layout;
        _syncing = false;

        UpdateSizeSummary();
        UpdateCompatibilityPreview();
    }

    /// <summary>NiceLabel 10 processing orders: fill direction and the corner of the first label.</summary>
    private static readonly ProcessingOrderOption[] ProcessingOrders =
    [
        new("Ngang, bắt đầu từ trên trái", false, false, false),
        new("Ngang, bắt đầu từ trên phải", false, true, false),
        new("Ngang, bắt đầu từ dưới trái", false, false, true),
        new("Ngang, bắt đầu từ dưới phải", false, true, true),
        new("Dọc, bắt đầu từ trên trái", true, false, false),
        new("Dọc, bắt đầu từ trên phải", true, true, false),
        new("Dọc, bắt đầu từ dưới trái", true, false, true),
        new("Dọc, bắt đầu từ dưới phải", true, true, true)
    ];

    private sealed record ProcessingOrderOption(string Name, bool FillDownFirst, bool StartFromRight, bool StartFromBottom);

    public PrinterInfo? SelectedPrinter { get; private set; }
    public PrinterPaperInfo? SelectedPaper { get; private set; }
    public int SelectedDpi { get; private set; } = 203;
    public LabelOrientation SelectedOrientation { get; private set; } = LabelOrientation.Portrait;
    public PaperSizeSource SelectedPageSizeSource { get; private set; } = PaperSizeSource.Manual;
    public PrinterSettingsSource SelectedSettingsSource { get; private set; } = PrinterSettingsSource.Label;
    public PrinterPaperInfo? SelectedMedia { get; private set; }
    public PrinterMediaResolution? SelectedMediaResolution { get; private set; }
    public string SelectedDriverIdentity { get; private set; } = string.Empty;
    public double SelectedOffsetXMm { get; private set; }
    public double SelectedOffsetYMm { get; private set; }
    public double SelectedScaleX { get; private set; } = 1;
    public double SelectedScaleY { get; private set; } = 1;
    public PageLayoutSelection SelectedLayout { get; private set; } = PageLayoutSelection.Single;

    private async void PrinterBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded)
        {
            return;
        }

        _capabilityCts?.Cancel();
        _capabilityCts?.Dispose();
        _capabilityCts = new CancellationTokenSource();
        var token = _capabilityCts.Token;
        _capabilitySnapshot = null;
        DriverMediaBox.ItemsSource = null;

        if (PrinterBox.SelectedItem is not PrinterInfo printer)
        {
            CapabilityStatusText.Text = "Chọn máy in để tải các khổ giấy của trình điều khiển.";
            DriverText.Text = string.Empty;
            UpdateCompatibilityPreview();
            return;
        }

        var queueChanged = !string.IsNullOrEmpty(_loadedQueueName)
            && !string.Equals(_loadedQueueName, printer.Name, StringComparison.OrdinalIgnoreCase);
        if (queueChanged)
        {
            // NiceLabel: changing printer reloads capability and returns page
            // size to automatic. Authored millimetres stay until Apply.
            ReturnPageSizeToDriverAutomatic();
        }

        _loadedQueueName = printer.Name;

        // Queue changes only reload capabilities. They never write WidthBox or
        // HeightBox, so authored label geometry stays stable until Apply.
        DriverText.Text = string.IsNullOrWhiteSpace(printer.DriverName)
            ? printer.Name
            : $"Trình điều khiển: {printer.DriverName}";
        CapabilityStatusText.Text = "Đang tải khổ giấy từ trình điều khiển máy in…";
        CapabilityStatusText.ToolTip = null;
        UpdateCompatibilityPreview();

        try
        {
            var snapshot = await _capabilityService.GetSnapshotAsync(printer.Name, token);
            if (token.IsCancellationRequested
                || PrinterBox.SelectedItem is not PrinterInfo current
                || !string.Equals(current.Name, printer.Name, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _capabilitySnapshot = snapshot;
            SelectedDriverIdentity = snapshot.DriverIdentity;
            UpdateDpiOptions(snapshot);
            var media = snapshot.Media.Select(ToPaperInfo).ToArray();
            DriverMediaBox.ItemsSource = media;
            DriverMediaBox.SelectedItem = queueChanged
                ? null
                : media.FirstOrDefault(item =>
                    string.Equals(item.RawIdentity, _initialDriverMediaIdentity, StringComparison.OrdinalIgnoreCase));

            // Operator text in Vietnamese; the driver's own diagnostics stay in
            // the tooltip as technical detail (audit 2026-10-13).
            var technical = new List<string>();
            var status = !snapshot.IsAvailable
                ? "Không đọc được khả năng của trình điều khiển máy in. Khổ giấy sẽ được kiểm tra lại khi in."
                : media.Length == 0
                    ? "Trình điều khiển không công bố khổ giấy nào. Khổ giấy sẽ được kiểm tra lại khi in."
                    : $"Đã tải {media.Length} khổ giấy từ trình điều khiển. Đổi máy in không làm đổi kích thước tem đã thiết kế.";
            if (!string.IsNullOrWhiteSpace(snapshot.Diagnostic))
            {
                technical.Add(snapshot.Diagnostic);
                if (snapshot.IsAvailable)
                {
                    status += " Trình điều khiển chỉ trả về một phần thông tin.";
                }
            }

            if (snapshot.IsAvailable && snapshot.AvailableResolutions.Count > 0
                && !snapshot.AvailableResolutions.Any(resolution =>
                    resolution.IsSquare && resolution.DpiX == _authoredDpi))
            {
                status += $" Trình điều khiển không công bố DPI {_authoredDpi} đã lưu; DPI thực tế sẽ được kiểm tra lại trước khi in.";
            }

            if (snapshot.PageObservation is { HasEffectiveMedia: true } page)
            {
                status += $" Trang thực tế {page.MediaWidthMm:0.##} × {page.MediaHeightMm:0.##} mm";
                status += page.IsImageableAreaVerified
                    ? $"; vùng in {page.ImageableWidthMm:0.##} × {page.ImageableHeightMm:0.##} mm."
                    : "; chưa xác minh được vùng in.";
            }

            if (snapshot.CustomMediaRange is { IsValid: true } range)
            {
                status += $" Khổ tùy chỉnh từ {range.MinimumWidthMm:0.##} × {range.MinimumHeightMm:0.##} đến "
                    + $"{range.MaximumWidthMm:0.##} × {range.MaximumHeightMm:0.##} mm.";
            }

            if (snapshot.StandardFeatures is { } features)
            {
                if (!string.IsNullOrWhiteSpace(features.EffectiveMediaType))
                {
                    status += $" Loại vật liệu: {features.EffectiveMediaType}"
                        + (features.IsEffectiveMediaTypeAdvertised ? "." : " (giá trị hiện tại, không có trong danh sách chọn của trình điều khiển).");
                }

                if (!string.IsNullOrWhiteSpace(features.EffectiveOutputQuality))
                {
                    status += $" Chất lượng in: {features.EffectiveOutputQuality}"
                        + (features.IsEffectiveOutputQualityAdvertised ? "." : " (giá trị hiện tại, không có trong danh sách chọn của trình điều khiển).");
                }
            }

            var driverEvidence = PrinterDriverEvidenceContract.Evaluate(snapshot);
            status += driverEvidence.State == PrinterDriverEvidenceState.FullyObserved
                ? " Đã đọc đủ thông tin trình điều khiển."
                : " Thông tin trình điều khiển chưa đầy đủ; thiết lập sẽ được kiểm tra lại trước khi in.";
            technical.Add($"{driverEvidence.State}. {driverEvidence.Diagnostic}");

            CapabilityStatusText.Text = status;
            CapabilityStatusText.ToolTip = "Chi tiết kỹ thuật: " + string.Join(" ", technical);
            UpdateCompatibilityPreview();
        }
        catch (OperationCanceledException)
        {
            // A newer queue selection owns the status text.
        }
        catch (Exception ex)
        {
            if (!token.IsCancellationRequested)
            {
                CapabilityStatusText.Text = "Không đọc được khả năng của trình điều khiển: " + ANLAbel.App.Services.OperationFailureExplainer.StatusLine(ex);
                UpdateCompatibilityPreview();
            }
        }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        PrinterBox_SelectionChanged(PrinterBox, new SelectionChangedEventArgs(
            Selector.SelectionChangedEvent,
            Array.Empty<object>(),
            Array.Empty<object>()));
    }

    private void ReturnPageSizeToDriverAutomatic()
    {
        try
        {
            _syncing = true;
            DriverAutomaticRadio.IsChecked = true;
            ManualMediaRadio.IsChecked = false;
            if (DriverSettingsRadio.IsChecked == true)
            {
                ManualMediaRadio.IsEnabled = false;
            }

            DriverMediaBox.IsEnabled = false;
            DriverMediaBox.SelectedItem = null;
            SelectedPageSizeSource = PaperSizeSource.DriverAutomatic;
            SelectedMedia = null;
        }
        finally
        {
            _syncing = false;
        }
    }

    private void Window_Closed(object? sender, EventArgs e)
    {
        _capabilityCts?.Cancel();
        _capabilityCts?.Dispose();
        _capabilityCts = null;
    }

    private void SettingsSource_Changed(object sender, RoutedEventArgs e)
    {
        if (_syncing || DriverAutomaticRadio is null || ManualMediaRadio is null || DriverMediaBox is null || DriverSettingsRadio is null)
        {
            return;
        }

        try
        {
            _syncing = true;
            var isDriver = DriverSettingsRadio.IsChecked == true;
            SelectedSettingsSource = isDriver ? PrinterSettingsSource.Driver : PrinterSettingsSource.Label;

            if (isDriver)
            {
                // Current driver preferences: the driver owns the ticket, so manual media override is inactive.
                DriverAutomaticRadio.IsChecked = true;
                ManualMediaRadio.IsChecked = false;
                ManualMediaRadio.IsEnabled = false;
                DriverMediaBox.IsEnabled = false;
                SelectedPageSizeSource = PaperSizeSource.DriverAutomatic;
            }
            else
            {
                // Saved with label: operator can choose either DriverAutomatic or Manual media override.
                ManualMediaRadio.IsEnabled = true;
                DriverMediaBox.IsEnabled = ManualMediaRadio.IsChecked == true;
                SelectedPageSizeSource = DriverAutomaticRadio.IsChecked == true
                    ? PaperSizeSource.DriverAutomatic
                    : PaperSizeSource.Manual;
            }
        }
        finally
        {
            _syncing = false;
        }

        UpdateCompatibilityPreview();
    }

    private void PageSource_Changed(object sender, RoutedEventArgs e)
    {
        if (_syncing || DriverMediaBox is null || ManualMediaRadio is null || LabelSettingsRadio is null || DriverSettingsRadio is null)
        {
            return;
        }

        try
        {
            _syncing = true;
            var isDriverSettings = DriverSettingsRadio.IsChecked == true;
            if (isDriverSettings && ManualMediaRadio.IsChecked == true)
            {
                LabelSettingsRadio.IsChecked = true;
                DriverSettingsRadio.IsChecked = false;
                SelectedSettingsSource = PrinterSettingsSource.Label;
                ManualMediaRadio.IsEnabled = true;
            }

            DriverMediaBox.IsEnabled = ManualMediaRadio.IsChecked == true && DriverSettingsRadio.IsChecked != true;
            SelectedPageSizeSource = DriverAutomaticRadio.IsChecked == true
                ? PaperSizeSource.DriverAutomatic
                : PaperSizeSource.Manual;
        }
        finally
        {
            _syncing = false;
        }

        UpdateCompatibilityPreview();
    }

    private void LabelDimensions_Changed(object sender, TextChangedEventArgs e)
    {
        if (_syncing || SizeSummaryText is null || WidthBox is null || HeightBox is null || PortraitRadio is null || LandscapeRadio is null)
        {
            return;
        }

        if (double.TryParse(WidthBox.Text, out var w) && double.TryParse(HeightBox.Text, out var h) && w > 0 && h > 0)
        {
            var detected = LabelGeometry.ResolveOrientation(w, h);
            _syncing = true;
            if (detected == LabelOrientation.Landscape)
            {
                LandscapeRadio.IsChecked = true;
                PortraitRadio.IsChecked = false;
            }
            else
            {
                PortraitRadio.IsChecked = true;
                LandscapeRadio.IsChecked = false;
            }
            SelectedOrientation = detected;
            _syncing = false;
        }

        UpdateSizeSummary();
        UpdateCompatibilityPreview();
    }

    private void Orientation_Changed(object sender, RoutedEventArgs e)
    {
        if (_syncing || SizeSummaryText is null || WidthBox is null || HeightBox is null || PortraitRadio is null || LandscapeRadio is null)
        {
            return;
        }

        var isLandscape = LandscapeRadio.IsChecked == true;
        SelectedOrientation = isLandscape ? LabelOrientation.Landscape : LabelOrientation.Portrait;

        if (double.TryParse(WidthBox.Text, out var w) && double.TryParse(HeightBox.Text, out var h) && w > 0 && h > 0)
        {
            var (targetW, targetH) = LabelGeometry.OrientSize(w, h, SelectedOrientation);
            if (Math.Abs(targetW - w) > 0.001 || Math.Abs(targetH - h) > 0.001)
            {
                _syncing = true;
                WidthBox.Text = targetW.ToString("0.##");
                HeightBox.Text = targetH.ToString("0.##");
                _syncing = false;
            }
        }

        UpdateSizeSummary();
        UpdateCompatibilityPreview();
    }

    private void MediaSelection_Changed(object sender, SelectionChangedEventArgs e)
    {
        UpdateCompatibilityPreview();
    }

    private async void DriverPreferences_Click(object sender, RoutedEventArgs e)
    {
        if (PrinterBox.SelectedItem is not PrinterInfo printer)
        {
            MessageBox.Show(this, "Vui lòng chọn một hàng đợi máy in trước.", "Tùy chọn trình điều khiển", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        DriverPreferencesButton.IsEnabled = false;
        try
        {
            await _preferencesLauncher.OpenAsync(printer.Name);
            if (IsLoaded && PrinterBox.SelectedItem is PrinterInfo current
                && string.Equals(current.Name, printer.Name, StringComparison.OrdinalIgnoreCase))
            {
                // Preferences can change forms, resolution and effective ticket.
                // Discard the old observation and read a fresh snapshot.
                PrinterBox_SelectionChanged(PrinterBox, new SelectionChangedEventArgs(
                    Selector.SelectionChangedEvent,
                    Array.Empty<object>(),
                    Array.Empty<object>()));
            }
        }
        catch (OperationCanceledException)
        {
            // Window shutdown owns cancellation; the external Windows dialog is
            // not killed because it may contain operator edits.
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex is InvalidOperationException ? ex.Message : "Không mở được Tùy chọn trình điều khiển. Chi tiết kỹ thuật: " + ex.Message, "Tùy chọn trình điều khiển", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            if (IsLoaded)
            {
                DriverPreferencesButton.IsEnabled = true;
            }
        }
    }

    private void UpdateSizeSummary()
    {
        if (double.TryParse(WidthBox?.Text, out var w) && double.TryParse(HeightBox?.Text, out var h))
        {
            var orientLabel = LandscapeRadio?.IsChecked == true ? "Ngang" : "Dọc";
            SizeSummaryText.Text = $"{w:0.##} × {h:0.##} mm ({orientLabel})";
        }
    }

    private void UpdateCompatibilityPreview()
    {
        if (MediaCompatibilityText is null || CompatibilityCard is null || ApplyButton is null)
        {
            return;
        }

        UpdatePageLayout();

        if (!double.TryParse(WidthBox?.Text, out var widthMm) || widthMm <= 0
            || !double.TryParse(HeightBox?.Text, out var heightMm) || heightMm <= 0)
        {
            MediaCompatibilityText.Text = "Nhập chiều rộng và chiều cao tem hợp lệ (lớn hơn 0).";
            MediaCompatibilityText.Foreground = new SolidColorBrush(Color.FromRgb(180, 35, 24));
            CompatibilityCard.BorderBrush = new SolidColorBrush(Color.FromRgb(253, 162, 155));
            ApplyButton.IsEnabled = false;
            return;
        }

        if (PrinterBox?.SelectedItem is not PrinterInfo)
        {
            MediaCompatibilityText.Text = "Chọn đúng hàng đợi máy in trước khi áp dụng.";
            MediaCompatibilityText.Foreground = new SolidColorBrush(Color.FromRgb(180, 35, 24));
            CompatibilityCard.BorderBrush = new SolidColorBrush(Color.FromRgb(253, 162, 155));
            ApplyButton.IsEnabled = false;
            return;
        }

        if (_capabilitySnapshot is null)
        {
            MediaCompatibilityText.Text = "Đang tải khả năng của máy in. Có thể áp dụng sau khi kiểm tra xong trình điều khiển.";
            MediaCompatibilityText.Foreground = new SolidColorBrush(Color.FromRgb(71, 84, 103));
            CompatibilityCard.BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225));
            ApplyButton.IsEnabled = false;
            return;
        }

        var settingsSource = DriverSettingsRadio?.IsChecked == true
            ? PrinterSettingsSource.Driver
            : PrinterSettingsSource.Label;
        var source = (settingsSource == PrinterSettingsSource.Driver || DriverAutomaticRadio?.IsChecked == true)
            ? PaperSizeSource.DriverAutomatic
            : PaperSizeSource.Manual;
        var media = DriverMediaBox?.SelectedItem as PrinterPaperInfo;
        var resolution = PrinterMediaResolver.Resolve(
            _capabilitySnapshot,
            new PrinterMediaRequest(
                source,
                widthMm,
                heightMm,
                media?.WidthMm ?? 0,
                media?.HeightMm ?? 0,
                media?.RawIdentity ?? media?.Name ?? string.Empty,
                settingsSource));

        MediaCompatibilityText.Text = FormatCompatibilityMessage(resolution, widthMm, heightMm);
        MediaCompatibilityText.ToolTip = string.IsNullOrWhiteSpace(resolution.Message) ? null : "Chi tiết kỹ thuật: " + resolution.Message;
        ApplyButton.IsEnabled = resolution.CanDispatch;
        var color = resolution.CanDispatch
            ? Color.FromRgb(6, 118, 71)
            : Color.FromRgb(180, 35, 24);
        MediaCompatibilityText.Foreground = new SolidColorBrush(color);
        CompatibilityCard.BorderBrush = new SolidColorBrush(resolution.CanDispatch
            ? Color.FromRgb(110, 231, 183)
            : Color.FromRgb(253, 162, 155));
    }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        if (!double.TryParse(WidthBox.Text, out var w) || w <= 0 || !double.TryParse(HeightBox.Text, out var h) || h <= 0)
        {
            MessageBox.Show(this, "Vui lòng nhập chiều rộng và chiều cao nhãn hợp lệ (mm).", "Cài đặt máy in", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var previewName = (DriverMediaBox.SelectedItem as PrinterPaperInfo)?.Name ?? "Custom";
        var stock = LabelStockContract.Evaluate(w, h, w, h, previewName);
        if (!stock.IsAllowed)
        {
            MessageBox.Show(this, ANLAbel.App.Services.PreflightIssueText.Sentence(stock.Diagnostic), "Cài đặt máy in", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var dpiDecision = IndustrialPrintDpiContract.Evaluate(ReadDpi(), ReadDpi());
        if (!dpiDecision.IsAllowed)
        {
            MessageBox.Show(this, ANLAbel.App.Services.PreflightIssueText.Sentence(dpiDecision.Diagnostic), "Cài đặt máy in", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var scaleX = ReadScale(ScaleXBox);
        var scaleY = ReadScale(ScaleYBox);
        var scaleDecision = PrintScaleContract.Evaluate(scaleX, scaleY);
        if (!scaleDecision.IsAllowed)
        {
            MessageBox.Show(this, ANLAbel.App.Services.PreflightIssueText.Sentence(scaleDecision.Diagnostic), "Cài đặt máy in", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (scaleX == 0 && scaleY == 0)
        {
            scaleX = PrintScaleContract.Identity;
            scaleY = PrintScaleContract.Identity;
        }

        if (PrinterBox.SelectedItem is not PrinterInfo selectedPrinter)
        {
            MessageBox.Show(this, "Vui lòng chọn chính xác hàng đợi máy in trước khi áp dụng thiết lập.", "Cài đặt máy in", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_capabilitySnapshot is null)
        {
            MessageBox.Show(
                this,
                "Khả năng của máy in chưa được tải xong. Vui lòng đợi quá trình kiểm tra trình điều khiển hoàn tất.",
                "Tương thích khổ in máy in",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        SelectedPrinter = selectedPrinter;
        var selectedPaper = new PrinterPaperInfo
        {
            Name = "Custom",
            WidthMm = w,
            HeightMm = h,
            Source = PaperSizeSourceKind.UserCustom
        };
        SelectedPaper = selectedPaper;
        SelectedDpi = ReadDpi();
        SelectedOffsetXMm = ReadMm(OffsetXBox, 0);
        SelectedOffsetYMm = ReadMm(OffsetYBox, 0);
        SelectedScaleX = scaleX;
        SelectedScaleY = scaleY;
        SelectedOrientation = LandscapeRadio.IsChecked == true ? LabelOrientation.Landscape : LabelOrientation.Portrait;
        SelectedSettingsSource = DriverSettingsRadio.IsChecked == true
            ? PrinterSettingsSource.Driver
            : PrinterSettingsSource.Label;
        SelectedPageSizeSource = SelectedSettingsSource == PrinterSettingsSource.Driver
            ? PaperSizeSource.DriverAutomatic
            : (DriverAutomaticRadio.IsChecked == true ? PaperSizeSource.DriverAutomatic : PaperSizeSource.Manual);
        SelectedMedia = SelectedPageSizeSource == PaperSizeSource.Manual
            ? DriverMediaBox.SelectedItem as PrinterPaperInfo
            : null;
        if (SelectedPageSizeSource == PaperSizeSource.Manual && SelectedMedia is null)
        {
            MessageBox.Show(
                this,
                "Chọn khổ giấy do trình điều khiển công bố, hoặc dùng tự động đặt khổ trang theo kích thước tem.",
                "Tương thích khổ in máy in",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var layout = ReadLayout();
        if (layout is null)
        {
            MessageBox.Show(this, "Số tem ngang và dọc là số nguyên từ 1 đến 100; khoảng cách là số milimét từ 0 trở lên.", "Số tem trên trang", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (SelectedMedia is not null
            && PrintsWholeSheet(SelectedMedia, w, h, layout)
            && SheetLayoutContract.EvaluatePlacement(w, h, SelectedMedia.WidthMm, SelectedMedia.HeightMm, SelectedOffsetXMm, SelectedOffsetYMm,
                layout.LabelsAcross, layout.LabelsDown, layout.HorizontalGapMm, layout.VerticalGapMm) is { IsAllowed: false } placement)
        {
            MessageBox.Show(this, ANLAbel.App.Services.PreflightIssueText.Sentence(placement.Diagnostic), "Số tem trên trang", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        SelectedLayout = layout;

        if (_capabilitySnapshot is not null)
        {
            SelectedMediaResolution = PrinterMediaResolver.Resolve(
                _capabilitySnapshot,
                new PrinterMediaRequest(
                    SelectedPageSizeSource,
                    w,
                    h,
                    SelectedMedia?.WidthMm ?? 0,
                    SelectedMedia?.HeightMm ?? 0,
                    SelectedMedia?.RawIdentity ?? SelectedMedia?.Name ?? string.Empty,
                    SelectedSettingsSource));

            if (SelectedMediaResolution.Status == PrinterMediaResolutionStatus.Compatible
                && SelectedMediaResolution.MatchedMedia is not null)
            {
                SelectedMedia = ToPaperInfo(SelectedMediaResolution.MatchedMedia);
            }

            if (!SelectedMediaResolution.CanDispatch)
            {
                // Fail closed on ANY non-dispatchable resolution (SoftwareStockOnly,
                // Unknown, QueueBlocked, Mismatch, Unavailable, and any future status).
                // Only Compatible/CustomSupported/DriverAutomatic authorize a save.
                var warning = string.IsNullOrWhiteSpace(SelectedMediaResolution.Message)
                    ? "Khổ giấy đã chọn không thể xác thực là khổ giấy vật lý do trình điều khiển công bố, "
                      + "nên không thể cho phép in. Vui lòng chọn khổ do trình điều khiển hỗ trợ hoặc bật "
                      + "tùy chọn khổ giấy tự động từ trình điều khiển."
                    : SelectedMediaResolution.Message;
                MessageBox.Show(
                    this,
                    warning,
                    "Tương thích khổ in máy in",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }
        }

        // Save preferences for next time
        _prefsService.Save(new PrinterPreferences
        {
            PrinterName = SelectedPrinter?.Name ?? string.Empty,
            PaperName = SelectedMedia?.Name ?? SelectedPaper?.Name ?? string.Empty,
            PaperCategory = string.Empty,
            Dpi = SelectedDpi,
            Orientation = SelectedOrientation == LabelOrientation.Landscape ? "Landscape" : "Portrait"
        });

        DialogResult = true;
    }

    private void Skip_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void Layout_Changed(object sender, TextChangedEventArgs e)
    {
        if (!_syncing)
        {
            UpdatePageLayout();
        }
    }

    private void Layout_Toggled(object sender, RoutedEventArgs e) => Layout_Changed(sender, null!);

    private void ProcessingOrder_Changed(object sender, SelectionChangedEventArgs e) => Layout_Changed(sender, null!);

    /// <summary>NiceLabel 10 "Automatic labels across": as many labels as the paper holds from the margins.</summary>
    private void AutoLabelsAcross_Click(object sender, RoutedEventArgs e)
    {
        if (!IsSheetPaper() || DriverMediaBox.SelectedItem is not PrinterPaperInfo paper
            || !double.TryParse(WidthBox.Text, out var w) || !double.TryParse(HeightBox.Text, out var h) || w <= 0 || h <= 0)
        {
            return;
        }

        LabelsAcrossBox.Text = SheetLayoutContract.FitCount(paper.WidthMm, ReadMm(OffsetXBox, 0), w, Math.Max(0, ReadMm(HorizontalGapBox, 0))).ToString();
        LabelsDownBox.Text = SheetLayoutContract.FitCount(paper.HeightMm, ReadMm(OffsetYBox, 0), h, Math.Max(0, ReadMm(VerticalGapBox, 0))).ToString();
    }

    /// <summary>Sheets of paper (manual page size with label settings) may hold several labels.</summary>
    private bool IsSheetPaper()
        => ManualMediaRadio?.IsChecked == true && DriverSettingsRadio?.IsChecked != true;

    /// <summary>The page layout as typed; labels on a roll are one upright label per page plus the 180° turn.</summary>
    private PageLayoutSelection? ReadLayout()
    {
        var rotated = Rotated180Box?.IsChecked == true;
        if (!IsSheetPaper())
        {
            return PageLayoutSelection.Single with { Rotated180 = rotated };
        }

        var order = ProcessingOrderBox?.SelectedItem as ProcessingOrderOption ?? ProcessingOrders[0];
        if (!int.TryParse(LabelsAcrossBox?.Text, out var across) || !int.TryParse(LabelsDownBox?.Text, out var down)
            || across is < 1 or > 100 || down is < 1 or > 100
            || !double.TryParse(HorizontalGapBox?.Text, out var horizontalGap) || !double.TryParse(VerticalGapBox?.Text, out var verticalGap)
            || !double.IsFinite(horizontalGap) || !double.IsFinite(verticalGap) || horizontalGap < 0 || verticalGap < 0)
        {
            return null;
        }

        return new PageLayoutSelection(across, down, horizontalGap, verticalGap, order.FillDownFirst, order.StartFromRight, order.StartFromBottom, rotated);
    }

    /// <summary>Same rule as the print service: a grid, or one label on a larger sheet, prints whole sheets.</summary>
    private static bool PrintsWholeSheet(PrinterPaperInfo paper, double labelWidthMm, double labelHeightMm, PageLayoutSelection layout)
        => SheetLayoutContract.PrintsWholeSheet(
            new PrinterProfile
            {
                SettingsSource = PrinterSettingsSource.Label,
                PaperSizeSource = PaperSizeSource.Manual,
                PhysicalWidthMm = paper.WidthMm,
                PhysicalHeightMm = paper.HeightMm,
                LabelsAcross = layout.LabelsAcross,
                LabelsDown = layout.LabelsDown
            },
            labelWidthMm,
            labelHeightMm);

    /// <summary>
    /// Margins left on the right and bottom, the labels-per-page summary and
    /// the preview of the page (NiceLabel 10 Label Dimensions and Paper).
    /// </summary>
    private void UpdatePageLayout()
    {
        if (LayoutPreview is null || SheetSummaryText is null || RightMarginBox is null || BottomMarginBox is null
            || PaperSizeText is null || LabelsAcrossGroup is null || LabelsAcrossHint is null)
        {
            return;
        }

        var isSheet = IsSheetPaper();
        LabelsAcrossGroup.IsEnabled = isSheet;
        LabelsAcrossHint.Text = isSheet
            ? "Đặt nhiều tem trên một tờ giấy; mỗi dòng dữ liệu in vào một ô."
            : "Tem trên cuộn in một tem mỗi trang. Chọn Đặt khổ trang thủ công (tờ giấy) ở trang Giấy để đặt nhiều tem trên một tờ.";
        var paper = isSheet ? DriverMediaBox?.SelectedItem as PrinterPaperInfo : null;
        PaperSizeText.Text = paper is not null
            ? $"{paper.WidthMm:0.##} × {paper.HeightMm:0.##} mm"
            : isSheet ? "Chọn khổ giấy trong danh sách của máy in." : "Khổ trang theo kích thước tem.";
        LayoutPreview.Children.Clear();
        var layout = ReadLayout();
        if (!double.TryParse(WidthBox?.Text, out var w) || !double.TryParse(HeightBox?.Text, out var h) || w <= 0 || h <= 0 || layout is null)
        {
            RightMarginBox.Text = "—";
            BottomMarginBox.Text = "—";
            SheetSummaryText.Text = "Nhập kích thước tem, số tem (1–100) và khoảng cách hợp lệ.";
            SheetSummaryText.Foreground = new SolidColorBrush(Color.FromRgb(180, 35, 24));
            return;
        }

        var offsetX = ReadMm(OffsetXBox, 0);
        var offsetY = ReadMm(OffsetYBox, 0);
        var pageWidth = paper?.WidthMm ?? w;
        var pageHeight = paper?.HeightMm ?? h;
        var right = pageWidth - SheetLayoutContract.ExtentMm(offsetX, w, layout.LabelsAcross, layout.HorizontalGapMm);
        var bottom = pageHeight - SheetLayoutContract.ExtentMm(offsetY, h, layout.LabelsDown, layout.VerticalGapMm);
        RightMarginBox.Text = paper is null ? "—" : right.ToString("0.##");
        BottomMarginBox.Text = paper is null ? "—" : bottom.ToString("0.##");

        var fits = true;
        string summary;
        if (paper is null)
        {
            summary = "Một tem mỗi trang; khổ trang theo kích thước tem.";
        }
        else if (PrintsWholeSheet(paper, w, h, layout))
        {
            var placement = SheetLayoutContract.EvaluatePlacement(w, h, paper.WidthMm, paper.HeightMm, offsetX, offsetY,
                layout.LabelsAcross, layout.LabelsDown, layout.HorizontalGapMm, layout.VerticalGapMm);
            fits = placement.IsAllowed;
            summary = !fits
                ? ANLAbel.App.Services.PreflightIssueText.Sentence(placement.Diagnostic)
                : layout.LabelsAcross * layout.LabelsDown == 1
                    ? $"Một tem mỗi trang, cách mép trái {offsetX:0.##} mm và mép trên {offsetY:0.##} mm của tờ {paper.WidthMm:0.##} × {paper.HeightMm:0.##} mm."
                    : $"{layout.LabelsAcross} × {layout.LabelsDown} tem ({layout.LabelsAcross * layout.LabelsDown} tem mỗi trang) trên tờ {paper.WidthMm:0.##} × {paper.HeightMm:0.##} mm; mỗi dòng dữ liệu in vào một ô.";
        }
        else
        {
            summary = "Một tem mỗi trang trên giấy cùng kích thước tem.";
        }

        if (layout.Rotated180)
        {
            summary += " Trang in xoay 180°.";
        }

        SheetSummaryText.Text = summary;
        SheetSummaryText.Foreground = fits
            ? new SolidColorBrush(Color.FromRgb(15, 76, 129))
            : new SolidColorBrush(Color.FromRgb(180, 35, 24));
        DrawLayoutPreview(pageWidth, pageHeight, w, h, offsetX, offsetY, layout, fits);
    }

    private void DrawLayoutPreview(double pageWidth, double pageHeight, double labelWidth, double labelHeight,
        double offsetX, double offsetY, PageLayoutSelection layout, bool fits)
    {
        if (pageWidth <= 0 || pageHeight <= 0)
        {
            return;
        }

        var scale = Math.Min(LayoutPreview.Width / pageWidth, LayoutPreview.Height / pageHeight);
        var left = (LayoutPreview.Width - pageWidth * scale) / 2;
        var top = (LayoutPreview.Height - pageHeight * scale) / 2;
        var page = new Rectangle
        {
            Width = pageWidth * scale,
            Height = pageHeight * scale,
            Fill = Brushes.White,
            Stroke = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
            StrokeThickness = 1
        };
        Canvas.SetLeft(page, left);
        Canvas.SetTop(page, top);
        LayoutPreview.Children.Add(page);

        var count = layout.LabelsAcross * layout.LabelsDown;
        var cellBrush = new SolidColorBrush(fits ? Color.FromRgb(219, 234, 254) : Color.FromRgb(254, 226, 226));
        var cellStroke = new SolidColorBrush(fits ? Color.FromRgb(37, 99, 235) : Color.FromRgb(220, 38, 38));
        for (var index = 0; index < count; index++)
        {
            var cell = SheetLayoutContract.CellOrigin(index, labelWidth, labelHeight, layout.LabelsAcross, layout.LabelsDown,
                layout.HorizontalGapMm, layout.VerticalGapMm, layout.FillDownFirst, layout.StartFromRight, layout.StartFromBottom);
            var x = offsetX + cell.XMm;
            var y = offsetY + cell.YMm;
            if (layout.Rotated180)
            {
                // The page turns as a whole: a cell lands mirrored through the page centre.
                x = pageWidth - x - labelWidth;
                y = pageHeight - y - labelHeight;
            }

            var rect = new Rectangle
            {
                Width = Math.Max(1, labelWidth * scale),
                Height = Math.Max(1, labelHeight * scale),
                Fill = cellBrush,
                Stroke = cellStroke,
                StrokeThickness = 1
            };
            Canvas.SetLeft(rect, left + x * scale);
            Canvas.SetTop(rect, top + y * scale);
            LayoutPreview.Children.Add(rect);
            if (labelWidth * scale >= 14 && labelHeight * scale >= 12)
            {
                var number = new TextBlock { Text = (index + 1).ToString(), FontSize = 9, Foreground = cellStroke };
                Canvas.SetLeft(number, left + x * scale + 2);
                Canvas.SetTop(number, top + y * scale + 1);
                LayoutPreview.Children.Add(number);
            }
        }
    }

    private async void PrintCalibration_Click(object sender, RoutedEventArgs e)
    {
        if (_workingTemplate is null)
        {
            MessageBox.Show(this, "Mở thiết lập từ tem đang soạn để in hiệu chỉnh.", "Căn chỉnh khổ in", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (PrinterBox.SelectedItem is not PrinterInfo printer)
        {
            MessageBox.Show(this, "Chọn hàng đợi máy in trước khi in hiệu chỉnh.", "Căn chỉnh khổ in", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var scaleX = ReadScale(ScaleXBox);
        var scaleY = ReadScale(ScaleYBox);
        var scaleDecision = PrintScaleContract.Evaluate(scaleX, scaleY);
        if (!scaleDecision.IsAllowed)
        {
            MessageBox.Show(this, ANLAbel.App.Services.PreflightIssueText.Sentence(scaleDecision.Diagnostic), "Căn chỉnh khổ in", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (scaleX == 0 && scaleY == 0)
        {
            scaleX = PrintScaleContract.Identity;
            scaleY = PrintScaleContract.Identity;
        }

        // Plan 13 H21: the calibration print uses an isolated copy. The open
        // label changes only when the operator confirms the dialog (OK applies
        // the Selected* values); Cancel must leave it exactly as it was.
        var calibrationTemplate = PrintSessionTemplateContract.Capture(_workingTemplate);
        calibrationTemplate.PrinterProfile.PrinterName = printer.Name;
        calibrationTemplate.PrinterProfile.OffsetXMm = ReadMm(OffsetXBox, 0);
        calibrationTemplate.PrinterProfile.OffsetYMm = ReadMm(OffsetYBox, 0);
        calibrationTemplate.PrinterProfile.ScaleX = scaleX;
        calibrationTemplate.PrinterProfile.ScaleY = scaleY;
        calibrationTemplate.PrinterProfile.Dpi = ReadDpi();

        try
        {
            var result = await _printService.PrintCalibrationWithResultAsync(calibrationTemplate);
            if (!result.IsAccepted)
            {
                MessageBox.Show(this, ANLAbel.App.Services.PrintStatusText.Result(result), "Hiệu chỉnh chưa hoàn tất", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ANLAbel.App.Services.PrintFailureExplainer.Explain(ex.Message).StatusLine, "In hiệu chỉnh thất bại", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ResetCalibration_Click(object sender, RoutedEventArgs e)
    {
        OffsetXBox.Text = "0";
        OffsetYBox.Text = "0";
        ScaleXBox.Text = "1";
        ScaleYBox.Text = "1";
    }

    private string FormatCompatibilityMessage(PrinterMediaResolution resolution, double labelWidthMm, double labelHeightMm)
    {
        if (resolution.Status == PrinterMediaResolutionStatus.Mismatch)
        {
            var media = DriverMediaBox.SelectedItem as PrinterPaperInfo;
            var mediaW = media?.WidthMm ?? 0;
            var mediaH = media?.HeightMm ?? 0;
            return $"Khổ giấy {mediaW:0.##} × {mediaH:0.##} mm không chứa được tem {labelWidthMm:0.##} × {labelHeightMm:0.##} mm. "
                + "Với tem trên cuộn hãy chọn Tự động đặt khổ trang theo kích thước tem. "
                + "Hoặc chọn vật liệu đủ lớn. Không đổi kích thước tem khi đổi máy in.";
        }

        return ANLAbel.App.Services.MediaCompatibilityText.Describe(resolution);
    }

    private static double ReadMm(TextBox? box, double fallback)
        => double.TryParse(box?.Text, out var value) && double.IsFinite(value) ? value : fallback;

    private static double ReadScale(TextBox? box)
    {
        if (double.TryParse(box?.Text, out var value) && double.IsFinite(value))
        {
            return value;
        }

        return 0;
    }

    private int ReadDpi()
    {
        if (DpiBox.SelectedItem is int dpiValue && dpiValue > 0)
        {
            return dpiValue;
        }

        if (DpiBox.SelectedItem is ComboBoxItem item && int.TryParse(item.Content?.ToString(), out var dpi))
        {
            return dpi;
        }

        return _authoredDpi;
    }

    private void UpdateDpiOptions(PrinterCapabilitySnapshot snapshot)
    {
        if (!snapshot.IsAvailable)
        {
            return;
        }

        var observed = snapshot.AvailableResolutions.AsEnumerable();
        if (snapshot.EffectiveDpiX is int effectiveX
            && snapshot.EffectiveDpiY is int effectiveY
            && effectiveX > 0 && effectiveY > 0)
        {
            observed = observed.Append(new PrinterResolutionCandidate(effectiveX, effectiveY));
        }

        var choices = PrinterResolutionCatalog.BuildSquareDpiChoices(
            observed,
            _authoredDpi);
        if (choices.Count == 0)
        {
            return;
        }

        DpiBox.ItemsSource = choices;
        DpiBox.SelectedItem = _authoredDpi;
    }

    private static PrinterPaperInfo ToPaperInfo(PrinterMediaCandidate media) => new()
    {
        Name = media.Name,
        WidthMm = media.WidthMm,
        HeightMm = media.HeightMm,
        Category = "Printer driver media",
        Source = media.Provenance == PrinterMediaProvenance.DriverWpf
            ? PaperSizeSourceKind.DriverWpf
            : PaperSizeSourceKind.DriverWin32,
        RawIdentity = media.RawIdentity
    };
}
