using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
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
        PrintService? printService = null)
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
        _syncing = false;

        UpdateSizeSummary();
        UpdateCompatibilityPreview();
    }

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
            CapabilityStatusText.Text = "Select a printer to load driver media.";
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
            : $"Driver: {printer.DriverName}";
        CapabilityStatusText.Text = "Loading media from the selected printer driver…";
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

            var status = !snapshot.IsAvailable
                ? snapshot.Diagnostic
                : media.Length == 0
                    ? "The driver exposed no sized media. Compatibility will be revalidated at print time."
                    : $"Loaded {media.Length} driver media option(s). Changing printer did not change the authored label.";
            if (snapshot.IsAvailable && !string.IsNullOrWhiteSpace(snapshot.Diagnostic))
            {
                status += $" Partial capability warning: {snapshot.Diagnostic}";
            }

            if (snapshot.IsAvailable && snapshot.AvailableResolutions.Count > 0
                && !snapshot.AvailableResolutions.Any(resolution =>
                    resolution.IsSquare && resolution.DpiX == _authoredDpi))
            {
                status += $" Saved DPI {_authoredDpi} is not advertised by this driver; effective DPI will be revalidated before printing.";
            }

            if (snapshot.PageObservation is { HasEffectiveMedia: true } page)
            {
                status += $" Effective page {page.MediaWidthMm:0.##} × {page.MediaHeightMm:0.##} mm";
                status += page.IsImageableAreaVerified
                    ? $"; imageable {page.ImageableWidthMm:0.##} × {page.ImageableHeightMm:0.##} mm."
                    : "; imageable area is unavailable or unverified.";
            }

            if (snapshot.CustomMediaRange is { IsValid: true } range)
            {
                status += $" Custom media {range.MinimumWidthMm:0.##} × {range.MinimumHeightMm:0.##} to "
                    + $"{range.MaximumWidthMm:0.##} × {range.MaximumHeightMm:0.##} mm.";
            }

            if (snapshot.StandardFeatures is { } features)
            {
                if (!string.IsNullOrWhiteSpace(features.EffectiveMediaType))
                {
                    status += $" Media type: {features.EffectiveMediaType}"
                        + (features.IsEffectiveMediaTypeAdvertised ? "." : " (current ticket value, not advertised as a selectable capability)." );
                }

                if (!string.IsNullOrWhiteSpace(features.EffectiveOutputQuality))
                {
                    status += $" Output quality: {features.EffectiveOutputQuality}"
                        + (features.IsEffectiveOutputQualityAdvertised ? "." : " (current ticket value, not advertised as a selectable capability)." );
                }
            }

            var driverEvidence = PrinterDriverEvidenceContract.Evaluate(snapshot);
            status += $" Driver evidence: {driverEvidence.State}. {driverEvidence.Diagnostic}";

            CapabilityStatusText.Text = status;
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
                CapabilityStatusText.Text = $"Failed to read driver capabilities: {ex.Message}";
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
            MessageBox.Show(this, ex.Message, "Tùy chọn trình điều khiển", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            var orientLabel = LandscapeRadio?.IsChecked == true ? "Landscape" : "Portrait";
            SizeSummaryText.Text = $"{w:0.##} × {h:0.##} mm ({orientLabel})";
        }
    }

    private void UpdateCompatibilityPreview()
    {
        if (MediaCompatibilityText is null || CompatibilityCard is null || ApplyButton is null)
        {
            return;
        }

        if (!double.TryParse(WidthBox?.Text, out var widthMm) || widthMm <= 0
            || !double.TryParse(HeightBox?.Text, out var heightMm) || heightMm <= 0)
        {
            MediaCompatibilityText.Text = "Enter a valid positive label width and height.";
            MediaCompatibilityText.Foreground = new SolidColorBrush(Color.FromRgb(180, 35, 24));
            CompatibilityCard.BorderBrush = new SolidColorBrush(Color.FromRgb(253, 162, 155));
            ApplyButton.IsEnabled = false;
            return;
        }

        if (PrinterBox?.SelectedItem is not PrinterInfo)
        {
            MediaCompatibilityText.Text = "Select an exact printer queue before applying this setup.";
            MediaCompatibilityText.Foreground = new SolidColorBrush(Color.FromRgb(180, 35, 24));
            CompatibilityCard.BorderBrush = new SolidColorBrush(Color.FromRgb(253, 162, 155));
            ApplyButton.IsEnabled = false;
            return;
        }

        if (_capabilitySnapshot is null)
        {
            MediaCompatibilityText.Text = "Loading printer capabilities. Apply will become available after the driver has been checked.";
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
            MessageBox.Show(this, stock.Diagnostic, "Cài đặt máy in", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var dpiDecision = IndustrialPrintDpiContract.Evaluate(ReadDpi(), ReadDpi());
        if (!dpiDecision.IsAllowed)
        {
            MessageBox.Show(this, dpiDecision.Diagnostic, "Cài đặt máy in", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var scaleX = ReadScale(ScaleXBox);
        var scaleY = ReadScale(ScaleYBox);
        var scaleDecision = PrintScaleContract.Evaluate(scaleX, scaleY);
        if (!scaleDecision.IsAllowed)
        {
            MessageBox.Show(this, scaleDecision.Diagnostic, "Cài đặt máy in", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            MessageBox.Show(this, scaleDecision.Diagnostic, "Căn chỉnh khổ in", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (scaleX == 0 && scaleY == 0)
        {
            scaleX = PrintScaleContract.Identity;
            scaleY = PrintScaleContract.Identity;
        }

        _workingTemplate.PrinterProfile.PrinterName = printer.Name;
        _workingTemplate.PrinterProfile.OffsetXMm = ReadMm(OffsetXBox, 0);
        _workingTemplate.PrinterProfile.OffsetYMm = ReadMm(OffsetYBox, 0);
        _workingTemplate.PrinterProfile.ScaleX = scaleX;
        _workingTemplate.PrinterProfile.ScaleY = scaleY;
        _workingTemplate.PrinterProfile.Dpi = ReadDpi();

        try
        {
            var result = await _printService.PrintCalibrationWithResultAsync(_workingTemplate);
            if (!result.IsAccepted)
            {
                MessageBox.Show(this, result.UserFacingStatus, "Hiệu chỉnh chưa hoàn tất", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "In hiệu chỉnh thất bại", MessageBoxButton.OK, MessageBoxImage.Error);
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

        return resolution.Message;
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
