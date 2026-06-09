using System.IO;
using System.Windows;
using ANLAbel.App.ViewModels;
using Microsoft.Win32;

namespace ANLAbel.App;

public partial class ExcelImportWindow : Window
{
    private readonly MainViewModel _viewModel;

    public ExcelImportWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
    }

    private async void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Import Excel workbook",
            Filter = "Excel Workbook (*.xlsx;*.xlsm)|*.xlsx;*.xlsm|All files (*.*)|*.*"
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            var sheets = _viewModel.GetExcelSheetNames(dialog.FileName);
            var sheetDialog = new ExcelSheetWindow(sheets) { Owner = this };
            if (sheetDialog.ShowDialog() != true || string.IsNullOrWhiteSpace(sheetDialog.SelectedSheetName))
            {
                return;
            }

            await _viewModel.ImportExcelAsync(dialog.FileName, sheetDialog.SelectedSheetName);
        }
        catch (IOException ex)
        {
            MessageBox.Show(this, $"Cannot read the Excel file. Close it in Excel and try again.\n\n{ex.Message}", "Import failed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Import failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}