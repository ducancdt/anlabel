using System.Windows;
using System.Windows.Controls;
using ANLAbel.App.Services;
using ANLAbel.Core.Enums;
using ANLAbel.Printing.PrinterProfiles;

namespace ANLAbel.App;

public partial class PrinterSetupWindow : Window
{
    private readonly IReadOnlyList<PrinterInfo> _printers;
    private readonly PrinterPreferencesService _prefsService = new();
    private bool _syncing;

    public PrinterSetupWindow(IReadOnlyList<PrinterInfo> printers, string? initialPrinterName = null, string? initialPaperName = null, LabelOrientation initialOrientation = LabelOrientation.Portrait, int initialDpi = 203)
    {
        InitializeComponent();
        _printers = printers;

        // Load saved preferences
        var prefs = _prefsService.Load();

        // Use saved prefs as defaults if caller didn't provide specific values
        var effectivePrinterName = !string.IsNullOrWhiteSpace(initialPrinterName)
            ? initialPrinterName
            : prefs.PrinterName;
        var effectivePaperName = !string.IsNullOrWhiteSpace(initialPaperName)
            ? initialPaperName
            : prefs.PaperName;
        var effectiveDpi = initialDpi != 203 ? initialDpi : prefs.Dpi;
        var effectiveOrientation = initialOrientation != LabelOrientation.Portrait
            ? initialOrientation
            : (prefs.Orientation == "Landscape" ? LabelOrientation.Landscape : LabelOrientation.Portrait);

        CategoryBox.ItemsSource = StandardLabelSizes.Categories;

        // If we have a saved category, select it; otherwise first
        if (!string.IsNullOrWhiteSpace(prefs.PaperCategory))
        {
            CategoryBox.SelectedItem = StandardLabelSizes.Categories
                .FirstOrDefault(c => string.Equals(c, prefs.PaperCategory, StringComparison.OrdinalIgnoreCase));
        }
        if (CategoryBox.SelectedIndex < 0)
        {
            CategoryBox.SelectedIndex = 0;
        }

        PrinterBox.ItemsSource = printers;
        PrinterBox.SelectedItem = printers.FirstOrDefault(p => string.Equals(p.Name, effectivePrinterName, StringComparison.OrdinalIgnoreCase));
        if (PrinterBox.SelectedItem is null && printers.Count > 0)
        {
            PrinterBox.SelectedIndex = 0;
        }

        DpiBox.SelectedIndex = GetDpiIndex(effectiveDpi);
        PortraitRadio.IsChecked = effectiveOrientation == LabelOrientation.Portrait;
        LandscapeRadio.IsChecked = effectiveOrientation == LabelOrientation.Landscape;

        if (!string.IsNullOrWhiteSpace(effectivePaperName))
        {
            var match = StandardLabelSizes.All.FirstOrDefault(s => string.Equals(s.Name, effectivePaperName, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
            {
                SetSizeFromPaper(match);
                SelectCategoryForPaper(match);
            }
        }

        UpdateSizeSummary();
    }

    public PrinterInfo? SelectedPrinter { get; private set; }
    public PrinterPaperInfo? SelectedPaper { get; private set; }
    public int SelectedDpi { get; private set; } = 203;
    public LabelOrientation SelectedOrientation { get; private set; } = LabelOrientation.Portrait;

    private void CategoryBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CategoryBox.SelectedItem is not string category)
        {
            return;
        }

        PaperSizesList.ItemsSource = StandardLabelSizes.GetByCategory(category);
        PaperSizesList.SelectedIndex = 0;
    }

    private void PaperSizesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncing || PaperSizesList.SelectedItem is not PrinterPaperInfo paper)
        {
            return;
        }

        SetSizeFromPaper(paper);
        UpdateSizeSummary();
    }

    private void SetSizeFromPaper(PrinterPaperInfo paper)
    {
        _syncing = true;
        WidthBox.Text = paper.WidthMm.ToString("0.##");
        HeightBox.Text = paper.HeightMm.ToString("0.##");
        _syncing = false;
    }

    private void SelectCategoryForPaper(PrinterPaperInfo paper)
    {
        if (!string.IsNullOrWhiteSpace(paper.Category))
        {
            CategoryBox.SelectedItem = StandardLabelSizes.Categories
                .FirstOrDefault(c => string.Equals(c, paper.Category, StringComparison.OrdinalIgnoreCase));
        }
    }

    private void UpdateSizeSummary()
    {
        if (double.TryParse(WidthBox.Text, out var w) && double.TryParse(HeightBox.Text, out var h))
        {
            SizeSummaryText.Text = $"{w:0.##} × {h:0.##} mm";
        }
    }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        if (!double.TryParse(WidthBox.Text, out var w) || w <= 0 || !double.TryParse(HeightBox.Text, out var h) || h <= 0)
        {
            MessageBox.Show(this, "Please enter valid label width and height in mm.", "Printer setup", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        SelectedPrinter = PrinterBox.SelectedItem as PrinterInfo;
        SelectedPaper = PaperSizesList.SelectedItem as PrinterPaperInfo
            ?? new PrinterPaperInfo { Name = "Custom", WidthMm = w, HeightMm = h, Source = PaperSizeSourceKind.UserCustom };
        SelectedDpi = ReadDpi();
        SelectedOrientation = LandscapeRadio.IsChecked == true ? LabelOrientation.Landscape : LabelOrientation.Portrait;

        // Save preferences for next time
        _prefsService.Save(new PrinterPreferences
        {
            PrinterName = SelectedPrinter?.Name ?? string.Empty,
            PaperName = SelectedPaper?.Name ?? string.Empty,
            PaperCategory = CategoryBox.SelectedItem as string ?? string.Empty,
            Dpi = SelectedDpi,
            Orientation = SelectedOrientation == LabelOrientation.Landscape ? "Landscape" : "Portrait"
        });

        DialogResult = true;
    }

    private void Skip_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private int ReadDpi()
    {
        if (DpiBox.SelectedItem is ComboBoxItem item && int.TryParse(item.Content?.ToString(), out var dpi))
        {
            return dpi;
        }

        return 203;
    }

    private static int GetDpiIndex(int dpi)
    {
        return dpi switch
        {
            300 => 1,
            600 => 2,
            _ => 0
        };
    }
}