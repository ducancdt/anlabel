using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ANLAbel.App.Services;
using ANLAbel.App.ViewModels;
using ANLAbel.Core.Enums;
using ANLAbel.Core.Expressions;
using ANLAbel.Core.Geometry;
using ANLAbel.Core.Models;
using ANLAbel.Data.PrintLogs;
using ANLAbel.Printing.PrinterProfiles;

namespace ANLAbel.App;

public partial class PrintPreviewWindow : Window
{
    private readonly LabelTemplate _template;
    private readonly IReadOnlyDictionary<string, string>? _currentRow;
    private readonly DataView? _excelDataView;
    private readonly PrintService _printService;
    private readonly PrintLogService _printLogService;
    private readonly PrinterDiscoveryService _printerDiscoveryService = new();
    private readonly string _templateFilePath;
    private string _selectedPrinterName = string.Empty;
    private IReadOnlyDictionary<string, string>?[] _previewRows = Array.Empty<IReadOnlyDictionary<string, string>?>();
    private double _previewZoom = 1.0;
    private int _currentPageIndex;
    private string _pageInput = "1";
    private PrintPreflightResult _preflightResult = new(Array.Empty<PrintPreflightIssue>());
    private readonly List<TrackingRowViewModel> _trackingRows = new();
    private bool _isRefreshing;

    public PrintPreviewWindow(LabelTemplate template, IReadOnlyDictionary<string, string>? currentRow, DataView? excelDataView, PrintService printService, PrintLogService printLogService, string templateFilePath)
    {
        InitializeComponent();
        _template = template;

        // Clamp window size to screen working area
        var workArea = SystemParameters.WorkArea;
        Width = Math.Min(Width, workArea.Width);
        Height = Math.Min(Height, workArea.Height);
        _currentRow = currentRow;
        _excelDataView = excelDataView;
        _printService = printService;
        _printLogService = printLogService;
        _templateFilePath = templateFilePath;
        _selectedPrinterName = template.PrinterProfile.PrinterName;

        // Restore saved printer preferences if no printer configured yet
        if (string.IsNullOrWhiteSpace(_selectedPrinterName))
        {
            var prefs = new PrinterPreferencesService().Load();
            if (!string.IsNullOrWhiteSpace(prefs.PrinterName))
            {
                _selectedPrinterName = prefs.PrinterName;
                template.PrinterProfile.PrinterName = prefs.PrinterName;
            }
            if (prefs.Dpi > 0)
            {
                template.PrinterProfile.Dpi = prefs.Dpi;
            }
        }

        DataContext = this;

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
    public List<PrintPreviewPageViewModel> Pages { get; } = new();
    public PrintPreviewPageViewModel? CurrentPage => Pages.Count == 0 ? null : Pages[Math.Max(0, Math.Min(Pages.Count - 1, _currentPageIndex))];
    public string PrinterName => string.IsNullOrWhiteSpace(_selectedPrinterName) ? "(no printer selected)" : _selectedPrinterName;
    public string LabelSizeText => $"Label: {_template.WidthMm:0.##} × {_template.HeightMm:0.##} mm | DPI: {_template.PrinterProfile.Dpi}";
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

        var dialog = new PrinterSetupWindow(printers, _selectedPrinterName, null, _template.Orientation, _template.PrinterProfile.Dpi) { Owner = this };
        if (dialog.ShowDialog() == true)
        {
            if (dialog.SelectedPrinter is not null)
            {
                _selectedPrinterName = dialog.SelectedPrinter.Name;
                _template.PrinterProfile.PrinterName = _selectedPrinterName;
            }

            if (dialog.SelectedPaper is not null)
            {
                var (widthMm, heightMm) = LabelGeometry.OrientSize(dialog.SelectedPaper.WidthMm, dialog.SelectedPaper.HeightMm, dialog.SelectedOrientation);
                _template.WidthMm = widthMm;
                _template.HeightMm = heightMm;
                _template.PrinterProfile.LabelWidthMm = widthMm;
                _template.PrinterProfile.LabelHeightMm = heightMm;
                _template.PrinterProfile.PhysicalWidthMm = dialog.SelectedPaper.WidthMm;
                _template.PrinterProfile.PhysicalHeightMm = dialog.SelectedPaper.HeightMm;
            }

            _template.PrinterProfile.Dpi = dialog.SelectedDpi;
            _template.Orientation = dialog.SelectedOrientation;
            RefreshPreview();
        }
    }

    private async void Print_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ApplyPrintSetup();
            var rows = GetSelectedRows().ToArray();
            if (rows.Length == 0)
            {
                MessageBox.Show(this, "No rows selected for printing. Use checkboxes to select rows.", "Print", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var preflight = _printService.ValidateRows(_template, rows);
            if (!preflight.IsSuccess)
            {
                _preflightResult = preflight;
                OnPropertyChanged();
                MessageBox.Show(this, preflight.ToUserMessage(), "Print blocked", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _printService.PrintRows(_template, rows, _selectedPrinterName, $"{_template.Name} preview print");
            await WritePrintHistoryAsync(rows);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Print failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

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
        if (_currentPageIndex > 0)
        {
            _currentPageIndex--;
            _pageInput = (_currentPageIndex + 1).ToString();
            OnPropertyChanged();
        }
    }

    private void NextPage_Click(object sender, RoutedEventArgs e)
    {
        if (_currentPageIndex < Pages.Count - 1)
        {
            _currentPageIndex++;
            _pageInput = (_currentPageIndex + 1).ToString();
            OnPropertyChanged();
        }
    }

    private void ApplyPrintSetup_Click(object sender, RoutedEventArgs e)
    {
        ApplyPrintSetup();
        RefreshPreview();
    }

    private void PrintCalibration_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ApplyPrintSetup();
            _printService.PrintCalibration(_template);
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
        OnPropertyChanged();
    }

    private void PageInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
        {
            return;
        }

        if (int.TryParse(PageInput, out var pageNumber) && pageNumber >= 1 && pageNumber <= Pages.Count)
        {
            _currentPageIndex = pageNumber - 1;
            _pageInput = pageNumber.ToString();
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
        var allSelected = _trackingRows.Count > 0 && _trackingRows.All(r => r.IsSelected);
        var newState = !allSelected;
        foreach (var vm in _trackingRows)
        {
            vm.IsSelected = newState;
        }
        RefreshPreviewPagesOnly();
    }

    // ==================== Refresh logic ====================

    private void RefreshPreview()
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
            var excelColumns = CollectExcelColumns(allRows);
            var pageNumber = 1;
            var sourceRowNumber = 0;

            foreach (var row in allRows)
            {
                sourceRowNumber++;
                var isSelected = true;
                var copies = 1;
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

            // Build preview rows
            _previewRows = BuildExpandedRowsFromTracking();

            _preflightResult = _printService.ValidateRows(_template, _previewRows);
            var previewPages = _printService.CreatePreviewPages(_template, _previewRows);
            foreach (var page in previewPages)
            {
                Pages.Add(new PrintPreviewPageViewModel
                {
                    PageNumber = page.PageNumber,
                    PreviewImage = RenderPreviewImage(page.Visual, page.WidthDip, page.HeightDip),
                    Width = page.WidthDip,
                    Height = page.HeightDip
                });
            }

            _currentPageIndex = Math.Min(_currentPageIndex, Math.Max(0, Pages.Count - 1));
            _pageInput = Pages.Count == 0 ? "0" : (_currentPageIndex + 1).ToString();
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
    private void RefreshPreviewPagesOnly()
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
            var previewPages = _printService.CreatePreviewPages(_template, _previewRows);
            foreach (var page in previewPages)
            {
                Pages.Add(new PrintPreviewPageViewModel
                {
                    PageNumber = page.PageNumber,
                    PreviewImage = RenderPreviewImage(page.Visual, page.WidthDip, page.HeightDip),
                    Width = page.WidthDip,
                    Height = page.HeightDip
                });
            }

            _currentPageIndex = Math.Min(_currentPageIndex, Math.Max(0, Pages.Count - 1));
            _pageInput = Pages.Count == 0 ? "0" : (_currentPageIndex + 1).ToString();
            OnPropertyChanged();
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    private IReadOnlyDictionary<string, string>?[] BuildExpandedRowsFromTracking()
    {
        var allRows = GetRows().ToArray();
        var expanded = new List<IReadOnlyDictionary<string, string>?>();
        foreach (var vm in _trackingRows)
        {
            if (!vm.IsSelected || vm.Copies <= 0) continue;
            var row = allRows.ElementAtOrDefault(vm.SourceRowNumber - 1);
            for (var i = 0; i < vm.Copies; i++)
            {
                expanded.Add(row);
            }
        }
        return expanded.ToArray();
    }

    // ==================== Data helpers ====================

    private IEnumerable<IReadOnlyDictionary<string, string>?> GetRows()
    {
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
        var allRows = GetRows().ToArray();
        foreach (var vm in _trackingRows)
        {
            if (!vm.IsSelected || vm.Copies <= 0) continue;
            var row = allRows.ElementAtOrDefault(vm.SourceRowNumber - 1);
            for (var i = 0; i < vm.Copies; i++)
            {
                yield return row;
            }
        }
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

    private void ApplyPrintSetup()
    {
        _template.PrinterProfile.LabelWidthMm = _template.WidthMm;
        _template.PrinterProfile.LabelHeightMm = _template.HeightMm;
        _template.Orientation = LabelGeometry.ResolveOrientation(_template.WidthMm, _template.HeightMm);
    }

    private void OnPropertyChanged()
    {
        DataContext = null;
        DataContext = this;
    }

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

    private static ImageSource RenderPreviewImage(Visual visual, double width, double height)
    {
        const double previewDpi = 300;
        var scale = previewDpi / 96.0;
        var pixelWidth = Math.Max(1, (int)Math.Ceiling(width * scale));
        var pixelHeight = Math.Max(1, (int)Math.Ceiling(height * scale));
        var target = new RenderTargetBitmap(pixelWidth, pixelHeight, previewDpi, previewDpi, PixelFormats.Pbgra32);
        target.Render(visual);
        target.Freeze();
        return target;
    }

    // ==================== Print history ====================

    private PrintLogEntry CreatePrintLogEntry(IReadOnlyDictionary<string, string>? row, int labelCount, int labelIndex)
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
            Notes = "Printed from Ctrl+P preview"
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
        var value = BindingResolver.ResolveObject(item, row);
        return string.IsNullOrWhiteSpace(value) ? string.Empty : $"{item.Name}={value}";
    }

    private void OpenPrintHistoryFile()
    {
        if (!File.Exists(_printLogService.LogFilePath))
        {
            return;
        }

        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = _printLogService.LogFilePath,
            UseShellExecute = true
        });
    }

    private async Task WritePrintHistoryAsync(IReadOnlyDictionary<string, string>?[] rows)
    {
        try
        {
            await _printLogService.AppendManyAsync(rows.Select((row, index) => CreatePrintLogEntry(row, rows.Length, index + 1)));
            OpenPrintHistoryFile();
        }
        catch (IOException ex)
        {
            MessageBox.Show(
                this,
                $"Print job was sent, but print history could not be saved because the Excel history file is open.\n\nClose print-history.xlsx, then print again if you need the log.\n\n{ex.Message}",
                "Print history is open",
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