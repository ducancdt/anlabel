using System.IO;
using System.Windows;
using System.Windows.Input;
using ANLAbel.App.ViewModels;
using ANLAbel.Data.Excel;
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
            Title = "Nhập dữ liệu Excel hoặc CSV",
            Filter = "Tệp dữ liệu (*.xlsx;*.xlsm;*.csv)|*.xlsx;*.xlsm;*.csv|Sổ làm việc Excel (*.xlsx;*.xlsm)|*.xlsx;*.xlsm|Tệp CSV (*.csv)|*.csv|Tất cả tệp (*.*)|*.*"
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
            // Reading sheet names + header-row preview touches the file on disk (can be slow
            // for large workbooks, network/OneDrive paths, or a file locked open in Excel), so
            // it must run off the UI thread or the window appears frozen. Both are read from a
            // single file open (bug fix 2026-07-03: opening an .xlsx parses the whole workbook
            // regardless of how little of it you read, so reading sheets and preview rows in
            // two separate opens silently doubled the wait — invisible to the user because no
            // wait cursor/Cancel button was shown for the second open, which on a slow machine
            // looked exactly like the app freezing).
            var token = _cts.Token;
            IReadOnlyList<ExcelSheetPreview> sheetPreviews;
            try
            {
                sheetPreviews = await _viewModel.ExcelDataService.GetSheetsWithPreviewAsync(dialog.FileName, cancellationToken: token);
            }
            catch (ExcelDataReadException)
            {
                // Preview failed outright (corrupt file, etc.) — fall back to the sheet-names-only
                // path so the user still gets the real error from ImportExcelAsync below instead
                // of a preview-specific message that hides the actual problem.
                var sheets = await _viewModel.GetExcelSheetNamesAsync(dialog.FileName, token);
                sheetPreviews = sheets.Select(name => new ExcelSheetPreview(name, Array.Empty<ExcelPreviewRow>())).ToArray();
            }

            Mouse.OverrideCursor = null;
            CancelButton.Visibility = Visibility.Collapsed;
            var sheetDialog = new ExcelSheetWindow(sheetPreviews.Select(s => s.SheetName)) { Owner = this };
            if (sheetDialog.ShowDialog() != true || string.IsNullOrWhiteSpace(sheetDialog.SelectedSheetName))
            {
                return;
            }

            // Let the user point at the real header row instead of always assuming row 1
            // (database-plan.md Giai đoạn 3 item 8) — already-read preview rows, no extra I/O.
            var headerRow = 1;
            var previewRows = sheetPreviews.FirstOrDefault(s => s.SheetName == sheetDialog.SelectedSheetName)?.Rows ?? Array.Empty<ExcelPreviewRow>();
            if (previewRows.Count > 1)
            {
                var headerRowDialog = new ExcelHeaderRowWindow(previewRows, previewRows[0].RowNumber) { Owner = this };
                if (headerRowDialog.ShowDialog() != true)
                {
                    return;
                }

                headerRow = headerRowDialog.SelectedHeaderRow;
            }

            Mouse.OverrideCursor = Cursors.Wait;
            CancelButton.Visibility = Visibility.Visible;
            _viewModel.Template.DatabaseConfig.HeaderRowIndex = headerRow;
            await _viewModel.ImportExcelAsync(dialog.FileName, sheetDialog.SelectedSheetName, token);
        }
        catch (OperationCanceledException)
        {
            // User cancelled – silently return
        }
        catch (IOException ex)
        {
            MessageBox.Show(this, $"Không thể đọc tệp Excel. Vui lòng đóng tệp trong Excel rồi thử lại.\n\n{ex.Message}", "Nhập dữ liệu thất bại", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (TimeoutException)
        {
            // Safety net (bug fix 2026-07-03, second round): guarantees the UI always recovers
            // instead of staying frozen indefinitely if something inside the Excel read gets
            // stuck — reported on a weak machine even after switching to ExcelDataReader.
            MessageBox.Show(
                this,
                "Đọc tệp Excel quá lâu và đã tự động dừng. Hãy thử lại, đóng các chương trình khác hoặc thử với tệp nhỏ hơn.",
                "Hết thời gian nhập dữ liệu",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Nhập dữ liệu thất bại", MessageBoxButton.OK, MessageBoxImage.Error);
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
