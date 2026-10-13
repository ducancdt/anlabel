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
            StatusText.Text = "Hãy lưu mẫu tem trước; mẫu chưa có tên tệp thì chưa có lịch sử phiên bản.";
            RevisionGrid.ItemsSource = Array.Empty<ProjectRevisionEntry>();
            DiffTextBox.Text = "Chưa có bản hiện tại và bản sao lưu để so sánh.";
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
            DiffTextBox.Text = ANLAbel.App.Services.RevisionText.Details(diff)
                + Environment.NewLine
                + Environment.NewLine
                + $"Mã băm bản hiện tại: {FormatHash(diff.PrimaryHash)}"
                + Environment.NewLine
                + $"Mã băm bản sao lưu: {FormatHash(diff.BackupHash)}"
                + Environment.NewLine
                + Environment.NewLine
                + $"Số sự kiện lưu trữ đã ghi: {audit.Count}"
                + (latestAudit is null
                    ? string.Empty
                    : Environment.NewLine + $"Gần nhất: {latestAudit.Event} lúc {latestAudit.TimestampUtc.ToLocalTime():yyyy-MM-dd HH:mm:ss}");
            StatusText.Text = $"Đã kiểm tra {entries.Count} phiên bản đã lưu của {Path.GetFileName(_filePath)}.";
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            StatusText.Text = "Không kiểm tra được các phiên bản: " + ANLAbel.App.Services.OperationFailureExplainer.StatusLine(exception);
            RevisionGrid.ItemsSource = Array.Empty<ProjectRevisionEntry>();
            DiffTextBox.Text = "Không so sánh được các phiên bản: " + ANLAbel.App.Services.OperationFailureExplainer.StatusLine(exception);
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
            "Khôi phục bản sửa đổi đã kiểm chứng này làm mẫu tem hiện tại? Bản chính hiện tại sẽ được lưu trữ sao lưu và tài liệu đang mở sẽ được tải lại. Các chỉnh sửa chưa lưu sẽ bị hủy.",
            "Xác nhận khôi phục mẫu tem",
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
            StatusText.Text = $"Đã khôi phục {result.TemplateName} từ bản sửa đổi đã kiểm chứng. Bản chính trước đó được lưu trữ sao lưu.";
            await RefreshAsync();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            var restoreFailure = ANLAbel.App.Services.OperationFailureExplainer.WithTechnicalIfGeneric(ANLAbel.App.Services.TemplateFailureExplainer.Explain(exception));
            StatusText.Text = "Khôi phục thất bại: " + restoreFailure;
            MessageBox.Show(this, restoreFailure, "Khôi phục thất bại", MessageBoxButton.OK, MessageBoxImage.Error);
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
