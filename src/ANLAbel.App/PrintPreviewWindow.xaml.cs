using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ANLAbel.App.ViewModels;
using ANLAbel.Core.Enums;
using ANLAbel.Core.Expressions;
using ANLAbel.Core.Expressions.Formulas;
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
    private bool _printAllRows = true;
    private double _previewZoom = 1.0;
    private int _currentPageIndex;
    private string _pageInput = "1";
    private PrintPreflightResult _preflightResult = new(Array.Empty<PrintPreflightIssue>());
    private DataRowView? _selectedTrackingRow;
    private DataTable _trackingTable = new();
    private Dictionary<int, int> _perRowCopies = new(); // sourceRowNumber -> copies

    public PrintPreviewWindow(LabelTemplate template, IReadOnlyDictionary<string, string>? currentRow, DataView? excelDataView, PrintService printService, PrintLogService printLogService, string templateFilePath)
    {
        InitializeComponent();
        _template = template;
        _currentRow = currentRow;
        _excelDataView = excelDataView;
        _printService = printService;
        _printLogService = printLogService;
        _templateFilePath = templateFilePath;
        _selectedPrinterName = template.PrinterProfile.PrinterName;
        DataContext = this;
        RefreshPreview();
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
    public string PreviewDataModeText => _printAllRows
        ? $"Tracking all Excel rows ({_excelDataView?.Count ?? 0} row(s))"
        : "Tracking current Excel row";
    public bool HasPreflightIssues => !_preflightResult.IsSuccess;
    public IReadOnlyList<PrintPreflightIssue> PreflightIssues => _preflightResult.Issues.Take(8).ToArray();
    public string PreflightStatusText => _preflightResult.IsSuccess
        ? "Preflight passed. Content is ready to print."
        : _preflightResult.ToUserMessage(3);
    public string PreflightIssuesSummary => _preflightResult.Issues.Count <= 8
        ? $"{_preflightResult.Issues.Count} issue(s) found."
        : $"Showing first 8 of {_preflightResult.Issues.Count} issue(s).";
    public DataView TrackingDataView => _trackingTable.DefaultView;
    public DataRowView? SelectedTrackingRow
    {
        get => _selectedTrackingRow;
        set
        {
            _selectedTrackingRow = value;
            if (value is not null)
            {
                var pageCol = _trackingTable.Columns.IndexOf("#");
                if (pageCol >= 0 && int.TryParse(value[pageCol]?.ToString(), out var pageNumber) && _currentPageIndex != pageNumber - 1)
                {
                    _currentPageIndex = Math.Max(0, Math.Min(Pages.Count - 1, pageNumber - 1));
                    _pageInput = (_currentPageIndex + 1).ToString();
                }
            }

            OnPropertyChanged();
        }
    }
    public double PreviewZoom
    {
        get => _previewZoom;
        private set
        {
            _previewZoom = Math.Max(0.25, Math.Min(4, Math.Round(value, 2)));
            OnPropertyChanged();
        }
    }

    public bool PrintCurrentOnly
    {
        get => !_printAllRows;
        set
        {
            if (value)
            {
                _printAllRows = false;
                RefreshPreview();
            }
        }
    }

    public bool PrintAllRows
    {
        get => _printAllRows;
        set
        {
            if (value)
            {
                _printAllRows = true;
                RefreshPreview();
            }
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
                // OrientSize swaps dimensions for Landscape so the design canvas shows
                // the label in landscape view (like NiceLabel).
                var (widthMm, heightMm) = LabelGeometry.OrientSize(dialog.SelectedPaper.WidthMm, dialog.SelectedPaper.HeightMm, dialog.SelectedOrientation);
                _template.WidthMm = widthMm;
                _template.HeightMm = heightMm;
                _template.PrinterProfile.LabelWidthMm = widthMm;
                _template.PrinterProfile.LabelHeightMm = heightMm;
                // Store original physical dimensions for printer driver PageMediaSize
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
            // _previewRows is already expanded with per-row copies in BuildExpandedRowsWithTracking()
            var rows = _previewRows;
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
            SyncSelectedTrackingRow();
            OnPropertyChanged();
        }
    }

    private void NextPage_Click(object sender, RoutedEventArgs e)
    {
        if (_currentPageIndex < Pages.Count - 1)
        {
            _currentPageIndex++;
            _pageInput = (_currentPageIndex + 1).ToString();
            SyncSelectedTrackingRow();
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
        SyncSelectedTrackingRow();
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
            SyncSelectedTrackingRow();
            OnPropertyChanged();
        }
    }

    private void RefreshPreview()
    {
        ApplyPrintSetup();
        Pages.Clear();
        _trackingTable = new DataTable();
        _previewRows = BuildExpandedRowsWithTracking();
        BuildTrackingColumns();
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
        SyncSelectedTrackingRow();
        OnPropertyChanged();
    }

    private IEnumerable<IReadOnlyDictionary<string, string>?> GetRows()
    {
        if (!_printAllRows)
        {
            yield return _currentRow;
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

    private void BuildTrackingColumns()
    {
        TrackingDataGrid.Columns.Clear();

        // Fixed columns
        TrackingDataGrid.Columns.Add(new DataGridTextColumn
        {
            Header = "#",
            Binding = new System.Windows.Data.Binding("[#]") { Mode = System.Windows.Data.BindingMode.OneWay },
            IsReadOnly = true,
            Width = 36
        });
        TrackingDataGrid.Columns.Add(new DataGridTextColumn
        {
            Header = "Row",
            Binding = new System.Windows.Data.Binding("[Row]") { Mode = System.Windows.Data.BindingMode.OneWay },
            IsReadOnly = true,
            Width = 40
        });

        // Copies column with up/down buttons
        var copiesTemplate = new DataGridTemplateColumn
        {
            Header = "Copies",
            Width = 90,
            IsReadOnly = false
        };
        var cellTemplate = new DataTemplate();
        var stackPanel = new FrameworkElementFactory(typeof(StackPanel));
        stackPanel.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
        stackPanel.SetValue(StackPanel.VerticalAlignmentProperty, VerticalAlignment.Center);

        var textBlock = new FrameworkElementFactory(typeof(TextBlock));
        textBlock.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("[Copies]") { Mode = System.Windows.Data.BindingMode.OneWay });
        textBlock.SetValue(TextBlock.WidthProperty, 28.0);
        textBlock.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Center);
        textBlock.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
        textBlock.SetValue(TextBlock.FontWeightProperty, FontWeights.Bold);
        textBlock.SetValue(TextBlock.FontSizeProperty, 12.0);
        stackPanel.AppendChild(textBlock);

        var upBtn = new FrameworkElementFactory(typeof(Button));
        upBtn.SetValue(Button.ContentProperty, "▲");
        upBtn.SetValue(Button.WidthProperty, 22.0);
        upBtn.SetValue(Button.HeightProperty, 18.0);
        upBtn.SetValue(Button.FontSizeProperty, 8.0);
        upBtn.SetValue(Button.PaddingProperty, new Thickness(0));
        upBtn.SetValue(Button.MarginProperty, new Thickness(2, 0, 0, 0));
        upBtn.SetValue(Button.TagProperty, "UP");
        upBtn.AddHandler(Button.ClickEvent, new RoutedEventHandler(CopiesButton_Click));
        stackPanel.AppendChild(upBtn);

        var downBtn = new FrameworkElementFactory(typeof(Button));
        downBtn.SetValue(Button.ContentProperty, "▼");
        downBtn.SetValue(Button.WidthProperty, 22.0);
        downBtn.SetValue(Button.HeightProperty, 18.0);
        downBtn.SetValue(Button.FontSizeProperty, 8.0);
        downBtn.SetValue(Button.PaddingProperty, new Thickness(0));
        downBtn.SetValue(Button.MarginProperty, new Thickness(1, 0, 0, 0));
        downBtn.SetValue(Button.TagProperty, "DOWN");
        downBtn.AddHandler(Button.ClickEvent, new RoutedEventHandler(CopiesButton_Click));
        stackPanel.AppendChild(downBtn);

        cellTemplate.VisualTree = stackPanel;
        copiesTemplate.CellTemplate = cellTemplate;
        TrackingDataGrid.Columns.Add(copiesTemplate);

        // Excel columns
        foreach (DataColumn col in _trackingTable.Columns)
        {
            if (col.ColumnName is "#" or "Row" or "Copies")
            {
                continue;
            }

            TrackingDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = col.ColumnName,
                Binding = new System.Windows.Data.Binding($"[{col.ColumnName}]") { Mode = System.Windows.Data.BindingMode.OneWay },
                IsReadOnly = true,
                Width = DataGridLength.Auto
            });
        }
    }

    private void CopiesButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn)
        {
            return;
        }

        // Find the DataRowView from the visual tree
        var cell = btn.Parent as StackPanel;
        if (cell is null)
        {
            return;
        }

        // Get the row index from the DataGrid context
        var rowView = TrackingDataGrid.CurrentItem as DataRowView;
        if (rowView is null)
        {
            return;
        }

        if (!int.TryParse(rowView["Row"]?.ToString(), out var sourceRowNumber))
        {
            return;
        }

        var delta = btn.Tag?.ToString() == "UP" ? 1 : -1;
        var currentCopies = _perRowCopies.ContainsKey(sourceRowNumber) ? _perRowCopies[sourceRowNumber] : 1;
        var newCopies = Math.Max(1, Math.Min(999, currentCopies + delta));
        _perRowCopies[sourceRowNumber] = newCopies;

        RefreshPreview();
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

    private IReadOnlyDictionary<string, string>?[] BuildExpandedRowsWithTracking()
    {
        // Build DataTable with fixed columns + all Excel columns
        _trackingTable = new DataTable();
        _trackingTable.Columns.Add("#", typeof(int));
        _trackingTable.Columns.Add("Row", typeof(int));
        _trackingTable.Columns.Add("Copies", typeof(int));

        // Collect all unique Excel column names
        var allRows = GetRows().ToArray();
        var excelColumns = new List<string>();
        foreach (var row in allRows)
        {
            if (row is not null)
            {
                foreach (var key in row.Keys)
                {
                    if (!excelColumns.Contains(key, StringComparer.OrdinalIgnoreCase))
                    {
                        excelColumns.Add(key);
                    }
                }
            }
        }

        foreach (var col in excelColumns)
        {
            _trackingTable.Columns.Add(col, typeof(string));
        }

        var expandedRows = new List<IReadOnlyDictionary<string, string>?>();
        var sourceRowNumber = 0;
        var pageNumber = 1;
        foreach (var row in allRows)
        {
            sourceRowNumber++;
            var copies = _perRowCopies.ContainsKey(sourceRowNumber) ? _perRowCopies[sourceRowNumber] : 1;
            copies = Math.Max(1, Math.Min(999, copies));

            // Add one DataTable row per source row (not per copy) for the tracking view
            expandedRows.Add(row);
            var dataRow = _trackingTable.NewRow();
            dataRow["#"] = pageNumber;
            dataRow["Row"] = sourceRowNumber;
            dataRow["Copies"] = copies;
            foreach (var col in excelColumns)
            {
                dataRow[col] = row is not null && row.TryGetValue(col, out var val) ? val : string.Empty;
            }
            _trackingTable.Rows.Add(dataRow);

            // Add remaining copies
            for (var copyIndex = 2; copyIndex <= copies; copyIndex++)
            {
                expandedRows.Add(row);
            }
            pageNumber += copies;
        }

        return expandedRows.ToArray();
    }

    private void SyncSelectedTrackingRow()
    {
        _selectedTrackingRow = null;
        foreach (DataRowView rowView in _trackingTable.DefaultView)
        {
            var pageCol = _trackingTable.Columns.IndexOf("#");
            if (pageCol >= 0 && rowView[pageCol] is int pageNumber)
            {
                var copies = _trackingTable.Columns.IndexOf("Copies") is var copiesCol && copiesCol >= 0
                    ? (int)rowView[copiesCol]
                    : 1;
                // This row represents page range [pageNumber .. pageNumber+copies-1]
                if (_currentPageIndex + 1 >= pageNumber && _currentPageIndex + 1 < pageNumber + copies)
                {
                    _selectedTrackingRow = rowView;
                    break;
                }
            }
        }
    }

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
            PrintMode = _printAllRows ? "Preview all rows" : "Preview current row",
            RowCount = _printAllRows ? Math.Max(1, _excelDataView?.Count ?? 0) : (_currentRow is null ? 0 : 1),
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