using System.ComponentModel;
using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using ANLAbel.App.ViewModels;
using ANLAbel.Core.Models;

namespace ANLAbel.App;

/// <summary>
/// Central place to add, edit, test, relink, and remove shared Excel data sources
/// (database-manager-module-plan.md M2). Complements the per-template Import/Update/
/// Relink/Unlink Excel actions already on the ribbon and Database panel — this window
/// is for managing sources shared across templates, not for the current template's
/// own one-off link.
/// </summary>
public partial class DatabaseManagerWindow : Window
{
    private readonly MainViewModel _viewModel;

    public DatabaseManagerWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        _viewModel.PropertyChanged += ViewModelOnPropertyChanged;
        Closed += (_, _) => _viewModel.PropertyChanged -= ViewModelOnPropertyChanged;
    }

    private void ViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.HasLinkedExcelSource))
        {
            RefreshUsedByCurrentTemplateText();
        }
    }

    private DataSource? SelectedSource => SourceListBox.SelectedItem as DataSource;

    private void SourceListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var source = SelectedSource;
        NoSelectionMarker.IsChecked = source is null;
        DetailPanel.Visibility = source is null ? Visibility.Collapsed : Visibility.Visible;
        TestResultText.Text = string.Empty;
        PreviewDataGrid.Visibility = Visibility.Collapsed;
        PreviewDataGrid.ItemsSource = null;

        if (source is null)
        {
            return;
        }

        NameTextBox.Text = source.Name;
        FilePathTextBox.Text = source.FilePath;
        SheetComboBox.ItemsSource = null;
        SheetComboBox.Text = source.SheetName;
        HeaderRowTextBox.Text = source.HeaderRowIndex.ToString();
        RefreshUsedByCurrentTemplateText();
    }

    private void RefreshUsedByCurrentTemplateText()
    {
        var source = SelectedSource;
        if (source is null)
        {
            return;
        }

        UsedByCurrentTemplateText.Text = string.Equals(_viewModel.Template.DatabaseConfig.DataSourceId, source.Id, StringComparison.OrdinalIgnoreCase)
            ? "Yes — the template currently open in the designer uses this source."
            : "No — the template currently open in the designer does not reference this source.";
    }

    private void NameTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        var source = SelectedSource;
        if (source is null)
        {
            return;
        }

        source.Name = NameTextBox.Text;
        _viewModel.PersistDataSources();
    }

    private void HeaderRowTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        var source = SelectedSource;
        if (source is null)
        {
            return;
        }

        if (!int.TryParse(HeaderRowTextBox.Text, out var headerRow) || headerRow < 1)
        {
            headerRow = 1;
        }

        HeaderRowTextBox.Text = headerRow.ToString();
        source.HeaderRowIndex = headerRow;
        _viewModel.PersistDataSources();
    }

    private void SheetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        PersistSheetNameIfChanged();
    }

    private void SheetComboBox_LostFocus(object sender, RoutedEventArgs e)
    {
        PersistSheetNameIfChanged();
    }

    private void PersistSheetNameIfChanged()
    {
        var source = SelectedSource;
        var sheetName = SheetComboBox.Text;
        if (source is null || string.Equals(source.SheetName, sheetName, StringComparison.Ordinal))
        {
            return;
        }

        source.SheetName = sheetName;
        _viewModel.PersistDataSources();
    }

    private async void LoadSheets_Click(object sender, RoutedEventArgs e)
    {
        var source = SelectedSource;
        if (source is null)
        {
            return;
        }

        Cursor = System.Windows.Input.Cursors.Wait;
        try
        {
            var sheets = await _viewModel.ExcelDataService.GetSheetNamesAsync(source.FilePath);
            SheetComboBox.ItemsSource = sheets;
            SheetComboBox.Text = sheets.Contains(source.SheetName) ? source.SheetName : sheets.FirstOrDefault() ?? string.Empty;
            TestResultText.Foreground = System.Windows.Media.Brushes.DarkGreen;
            TestResultText.Text = $"Found {sheets.Count} sheet(s).";
        }
        catch (Exception ex)
        {
            TestResultText.Foreground = System.Windows.Media.Brushes.Firebrick;
            TestResultText.Text = $"Could not read sheets: {ex.Message}";
        }
        finally
        {
            Cursor = null;
        }
    }

    private async void TestConnection_Click(object sender, RoutedEventArgs e)
    {
        var source = SelectedSource;
        if (source is null)
        {
            return;
        }

        Cursor = System.Windows.Input.Cursors.Wait;
        try
        {
            var (ok, message) = await _viewModel.ExcelDataService.TestConnectionAsync(source.FilePath, SheetComboBox.Text, source.HeaderRowIndex);
            TestResultText.Foreground = ok ? System.Windows.Media.Brushes.DarkGreen : System.Windows.Media.Brushes.Firebrick;
            TestResultText.Text = (ok ? "✓ " : "✗ ") + message;
        }
        finally
        {
            Cursor = null;
        }
    }

    private async void PreviewData_Click(object sender, RoutedEventArgs e)
    {
        var source = SelectedSource;
        if (source is null)
        {
            return;
        }

        Cursor = System.Windows.Input.Cursors.Wait;
        try
        {
            DataTable table = await _viewModel.ExcelDataService.LoadSheetAsync(source.FilePath, SheetComboBox.Text, source.HeaderRowIndex);
            PreviewDataGrid.ItemsSource = table.DefaultView;
            PreviewDataGrid.Visibility = Visibility.Visible;
            TestResultText.Foreground = System.Windows.Media.Brushes.DarkGreen;
            TestResultText.Text = $"Preview loaded: {table.Rows.Count} row(s), {table.Columns.Count} column(s).";
        }
        catch (Exception ex)
        {
            PreviewDataGrid.Visibility = Visibility.Collapsed;
            TestResultText.Foreground = System.Windows.Media.Brushes.Firebrick;
            TestResultText.Text = $"Could not load preview: {ex.Message}";
        }
        finally
        {
            Cursor = null;
        }
    }

    private void Relink_Click(object sender, RoutedEventArgs e)
    {
        var source = SelectedSource;
        if (source is null)
        {
            return;
        }

        _viewModel.RelinkDataSourceCommand.Execute(source);
        FilePathTextBox.Text = source.FilePath;
        SheetComboBox.Text = source.SheetName;
        HeaderRowTextBox.Text = source.HeaderRowIndex.ToString();
    }

    private void UseForCurrentTemplate_Click(object sender, RoutedEventArgs e)
    {
        var source = SelectedSource;
        if (source is null)
        {
            return;
        }

        _viewModel.UseDataSourceCommand.Execute(source);
    }

    private void Remove_Click(object sender, RoutedEventArgs e)
    {
        var source = SelectedSource;
        if (source is null)
        {
            return;
        }

        var isUsedByCurrent = string.Equals(_viewModel.Template.DatabaseConfig.DataSourceId, source.Id, StringComparison.OrdinalIgnoreCase);
        var usageNote = isUsedByCurrent
            ? "\n\nThe template currently open in the designer uses this source — it will fall back to its own file path after removal."
            : "\n\nAny template referencing this source will fall back to its own file path after removal.";

        var confirmed = MessageBox.Show(
            this,
            $"Remove the shared data source \"{source.DisplayName}\"?{usageNote}",
            "Remove Data Source",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning) == MessageBoxResult.Yes;
        if (!confirmed)
        {
            return;
        }

        _viewModel.RemoveDataSourceCommand.Execute(source);
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private static readonly TimeSpan OrphanUnusedThreshold = TimeSpan.FromDays(30);

    private void CleanUp_Click(object sender, RoutedEventArgs e)
    {
        var thresholdUtc = DateTime.UtcNow - OrphanUnusedThreshold;
        var candidates = _viewModel.DataSources
            .Where(source => !File.Exists(source.FilePath) && (source.LastUsedUtc is null || source.LastUsedUtc < thresholdUtc))
            .ToArray();

        if (candidates.Length == 0)
        {
            MessageBox.Show(this, "No orphaned data sources found (missing file, unused for 30+ days).", "Clean Up", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var window = new DataSourceCleanupWindow(_viewModel, candidates) { Owner = this };
        window.ShowDialog();
    }
}
