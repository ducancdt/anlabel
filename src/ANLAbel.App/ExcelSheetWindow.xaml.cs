using System.Windows;

namespace ANLAbel.App;

public partial class ExcelSheetWindow : Window
{
    public ExcelSheetWindow(IEnumerable<string> sheetNames)
    {
        InitializeComponent();
        SheetsList.ItemsSource = sheetNames.ToArray();
        SheetsList.SelectedIndex = SheetsList.Items.Count > 0 ? 0 : -1;
    }

    public string? SelectedSheetName { get; private set; }

    private void Import_Click(object sender, RoutedEventArgs e)
    {
        SelectedSheetName = SheetsList.SelectedItem?.ToString();
        if (string.IsNullOrWhiteSpace(SelectedSheetName))
        {
            MessageBox.Show(this, "Please select a sheet.", "Excel import", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
