using System.IO;
using System.Windows;
using ANLAbel.App.ViewModels;
using ANLAbel.Project.SaveLoad;

namespace ANLAbel.App;

public partial class TemplateRevisionWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly ProjectRevisionService _revisionService;
    private readonly string _filePath;
    private bool _busy;

    public TemplateRevisionWindow(MainViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        _viewModel = viewModel;
        _revisionService = new ProjectRevisionService();
        _filePath = string.IsNullOrWhiteSpace(viewModel.CurrentFilePath)
            ? string.Empty
            : Path.GetFullPath(viewModel.CurrentFilePath);
        InitializeComponent();
        Loaded += async (_, _) => await RefreshAsync();
    }

    private ProjectRevisionEntry? SelectedEntry => RevisionGrid.SelectedItem as ProjectRevisionEntry;

    private async Task RefreshAsync()
    {
        if (string.IsNullOrWhiteSpace(_filePath))
        {
            StatusText.Text = "Save the current template first; an untitled document has no revision history.";
            RevisionGrid.ItemsSource = Array.Empty<ProjectRevisionEntry>();
            DiffTextBox.Text = "No committed primary/backup pair is available.";
            RestoreButton.IsEnabled = false;
            return;
        }

        SetBusy(true);
        try
        {
            var entries = await _revisionService.ListAllAsync(_filePath);
            RevisionGrid.ItemsSource = entries;
            var diff = await _revisionService.CompareAsync(_filePath);
            var audit = await _revisionService.ListAuditAsync(_filePath);
            var latestAudit = audit.FirstOrDefault();
            DiffTextBox.Text = diff.DetailsText
                + Environment.NewLine
                + Environment.NewLine
                + $"Primary hash: {FormatHash(diff.PrimaryHash)}"
                + Environment.NewLine
                + $"Backup hash: {FormatHash(diff.BackupHash)}"
                + Environment.NewLine
                + Environment.NewLine
                + $"Archive audit events: {audit.Count}"
                + (latestAudit is null
                    ? string.Empty
                    : Environment.NewLine + $"Latest: {latestAudit.Event} at {latestAudit.TimestampUtc.ToLocalTime():yyyy-MM-dd HH:mm:ss}");
            StatusText.Text = $"{entries.Count} committed revisions inspected for {Path.GetFileName(_filePath)}.";
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            StatusText.Text = $"Revision inspection failed: {exception.Message}";
            RevisionGrid.ItemsSource = Array.Empty<ProjectRevisionEntry>();
            DiffTextBox.Text = $"Diff inspection failed: {exception.Message}";
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetBusy(bool busy)
    {
        _busy = busy;
        RefreshButton.IsEnabled = !busy;
        RestoreButton.IsEnabled = !busy && SelectedEntry?.CanRestore == true;
    }

    private void RevisionGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        RestoreButton.IsEnabled = !_busy && SelectedEntry?.CanRestore == true;
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        await RefreshAsync();
    }

    private async void Restore_Click(object sender, RoutedEventArgs e)
    {
        var selected = SelectedEntry;
        if (_busy || selected is null || !selected.CanRestore)
        {
            return;
        }

        var confirmation = MessageBox.Show(
            this,
            "Restore the validated revision as the current template? The current primary will be moved into the backup slot and archived, and the open document will be reloaded. Unsaved edits in the current document will be discarded.",
            "Confirm template rollback",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (confirmation != MessageBoxResult.Yes)
        {
            return;
        }

        SetBusy(true);
        try
        {
            var result = await _revisionService.RestoreRevisionAsync(_filePath, selected.Path);
            await _viewModel.OpenAsync(_filePath);
            await _viewModel.RefreshPrinterQueueStatusAsync();
            StatusText.Text = $"Restored {result.TemplateName} from the validated revision. The previous primary is retained as backup and archive.";
            await RefreshAsync();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            StatusText.Text = $"Rollback failed: {exception.Message}";
            MessageBox.Show(this, exception.Message, "Rollback failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private static string FormatHash(string hash)
        => string.IsNullOrWhiteSpace(hash) ? "unavailable" : hash;
}
