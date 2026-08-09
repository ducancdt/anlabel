using System.Windows;
using ANLAbel.Data.Excel;

namespace ANLAbel.App;

/// <summary>
/// Lets the user point at the row that actually contains column names before importing
/// (database-plan.md Giai đoạn 3 item 8) — instead of the app always assuming row 1, which
/// breaks on workbooks with a title/report-date row (or a few blank rows) above the real
/// header. Shown from <see cref="ExcelImportWindow"/>'s Browse flow, after sheet selection.
/// </summary>
public partial class ExcelHeaderRowWindow : Window
{
    public ExcelHeaderRowWindow(IReadOnlyList<ExcelPreviewRow> previewRows, int initialHeaderRow)
    {
        InitializeComponent();
        var items = previewRows
            .Select(row => new PreviewRowItem(row.RowNumber, string.Join(" | ", row.Cells)))
            .ToArray();
        PreviewGrid.ItemsSource = items;
        PreviewGrid.SelectedItem = items.FirstOrDefault(item => item.RowNumber == initialHeaderRow) ?? items.FirstOrDefault();
    }

    public int SelectedHeaderRow { get; private set; }

    private void UseRow_Click(object sender, RoutedEventArgs e)
    {
        if (PreviewGrid.SelectedItem is not PreviewRowItem item)
        {
            MessageBox.Show(this, "Please select the row that contains column headers.", "Excel import", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        SelectedHeaderRow = item.RowNumber;
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private sealed record PreviewRowItem(int RowNumber, string PreviewText);
}
