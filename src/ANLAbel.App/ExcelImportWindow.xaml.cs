using System.IO;
using System.Windows;
using System.Windows.Input;
using ANLAbel.App.ViewModels;
using ANLAbel.Data.Excel;
using Microsoft.Win32;

namespace ANLAbel.App;

/// <summary>
/// How the database window opens (NiceLabel 10 Data tab): Excel and Text File ask for
/// the file at once, Edit Database shows the linked file and its fields.
/// </summary>
public enum ExcelImportStart
{
    Review,
    BrowseWorkbook,
    BrowseTextFile
}

public partial class ExcelImportWindow : Window
{
    private const int AllDataFilesFilter = 1;
    private const int WorkbookFilter = 2;
    private const int TextFileFilter = 3;

    private readonly MainViewModel _viewModel;
    private CancellationTokenSource? _cts;

    public ExcelImportWindow(MainViewModel viewModel, ExcelImportStart start = ExcelImportStart.Review)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        if (start != ExcelImportStart.Review)
        {
            // Cancelling the file picker cancels the wizard.
            Loaded += async (_, _) => await BrowseAsync(null, start == ExcelImportStart.BrowseTextFile ? TextFileFilter : WorkbookFilter, closeIfCancelled: true);
        }
    }

    /// <summary>The sheet to read when the file has exactly one; null when the operator must choose.</summary>
    private static string? OnlySheet(IReadOnlyList<ExcelSheetPreview> previews)
        => previews.Count == 1 && !string.IsNullOrWhiteSpace(previews[0].SheetName) ? previews[0].SheetName : null;

    private async void Browse_Click(object sender, RoutedEventArgs e)
    {
        await BrowseAsync(sender as UIElement, AllDataFilesFilter, closeIfCancelled: false);
    }

    private async Task BrowseAsync(UIElement? browseButton, int filterIndex, bool closeIfCancelled)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Nhập dữ liệu Excel hoặc CSV",
            Filter = "Tệp dữ liệu (*.xlsx;*.xlsm;*.csv)|*.xlsx;*.xlsm;*.csv|Sổ làm việc Excel (*.xlsx;*.xlsm)|*.xlsx;*.xlsm|Tệp CSV (*.csv)|*.csv|Tất cả tệp (*.*)|*.*",
            FilterIndex = filterIndex
        };

        if (dialog.ShowDialog(this) != true)
        {
            if (closeIfCancelled)
            {
                Close();
            }

            return;
        }

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
            // A CSV or a one-sheet workbook leaves nothing to choose.
            var sheetName = OnlySheet(sheetPreviews);
            if (sheetName is null)
            {
                var sheetDialog = new ExcelSheetWindow(sheetPreviews.Select(s => s.SheetName)) { Owner = this };
                if (sheetDialog.ShowDialog() != true || string.IsNullOrWhiteSpace(sheetDialog.SelectedSheetName))
                {
                    return;
                }

                sheetName = sheetDialog.SelectedSheetName;
            }

            // Let the user point at the real header row instead of always assuming row 1
            // (database-plan.md Giai đoạn 3 item 8) — already-read preview rows, no extra I/O.
            var headerRow = 1;
            var previewRows = sheetPreviews.FirstOrDefault(s => s.SheetName == sheetName)?.Rows ?? Array.Empty<ExcelPreviewRow>();
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
            // The header row is stored in the label only after the import succeeds (H16).
            await _viewModel.ImportExcelAsync(dialog.FileName, sheetName, headerRow, token);
        }
        catch (OperationCanceledException)
        {
            // User cancelled – silently return
        }
        catch (Exception ex)
        {
            // Locked file, timeout (the read stops instead of freezing the window),
            // missing sheet, Excel error cell, formula without a value, CSV shape:
            // one plain explanation with the file, row and column (plan 13 GĐ4).
            var explanation = ANLAbel.App.Services.OperationFailureExplainer.Explain(ex);
            OperatorDialog.Ask(
                this,
                "Nhập dữ liệu thất bại",
                [explanation.Problem, explanation.NextAction],
                $"Chi tiết kỹ thuật: {explanation.Technical}",
                OperatorDialogTone.Warning,
                new OperatorDialogChoice("ok", "Đóng", IsDefault: true, IsCancel: true));
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
