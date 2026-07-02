using System.IO;
using System.Windows;
using System.Windows.Input;
using ANLAbel.App.ViewModels;
using Microsoft.Win32;

namespace ANLAbel.App;

public partial class ExcelImportWindow : Window
{
    private readonly MainViewModel _viewModel;
    private CancellationTokenSource? _cts;

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

        var browseButton = sender as UIElement;
        if (browseButton is not null)
        {
            browseButton.IsEnabled = false;
        }

        _cts?.Dispose();
        _cts = new CancellationTokenSource();
        CancelButton.Visibility = Visibility.Visible;
        Mouse.OverrideCursor = Cursors.Wait;

        try
        {
            // Reading sheet names touches the file on disk (can be slow for large
            // workbooks, network/OneDrive paths, or a file locked open in Excel),
            // so it must run off the UI thread or the window appears frozen.
            var token = _cts.Token;
            var sheets = await _viewModel.GetExcelSheetNamesAsync(dialog.FileName, token);

            Mouse.OverrideCursor = null;
            CancelButton.Visibility = Visibility.Collapsed;
            var sheetDialog = new ExcelSheetWindow(sheets) { Owner = this };
            if (sheetDialog.ShowDialog() != true || string.IsNullOrWhiteSpace(sheetDialog.SelectedSheetName))
            {
                return;
            }

            Mouse.OverrideCursor = Cursors.Wait;
            CancelButton.Visibility = Visibility.Visible;
            await _viewModel.ImportExcelAsync(dialog.FileName, sheetDialog.SelectedSheetName, token);
        }
        catch (OperationCanceledException)
        {
            // User cancelled – silently return
        }
        catch (IOException ex)
        {
            MessageBox.Show(this, $"Cannot read the Excel file. Close it in Excel and try again.\n\n{ex.Message}", "Import failed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Import failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _cts?.Dispose();
            _cts = null;
            Mouse.OverrideCursor = null;
            CancelButton.Visibility = Visibility.Collapsed;
            if (browseButton is not null)
            {
                browseButton.IsEnabled = true;
            }
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        _cts?.Cancel();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}