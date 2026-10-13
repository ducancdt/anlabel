using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ANLAbel.App.ViewModels;
using ANLAbel.Core.Printing;
using ANLAbel.Data.PrintLogs;
using Microsoft.Win32;

namespace ANLAbel.App;

/// <summary>
/// Operator-facing recovery surface. It only calls read-only reconciliation or
/// explicit lineage actions; no button in this window retries a print implicitly.
/// </summary>
public partial class PrintCenterWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly Action<PrintJobRecoveryCandidate>? _openApprovedPreview;
    private IReadOnlyList<PrintJobRecoveryCandidate> _allCandidates = Array.Empty<PrintJobRecoveryCandidate>();
    private bool _isBusy;

    public PrintCenterWindow(
        MainViewModel viewModel,
        Action<PrintJobRecoveryCandidate>? openApprovedPreview = null)
    {
        InitializeComponent();
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _openApprovedPreview = openApprovedPreview;
        DataContext = viewModel;
        UpdateActionState();
    }

    private PrintJobRecoveryCandidate? SelectedCandidate => JobsGrid.SelectedItem as PrintJobRecoveryCandidate;

    private async void PrintCenterWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await RefreshAsync();
        FilterTextBox.Focus();
        Keyboard.Focus(FilterTextBox);
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        var selectedJobId = SelectedCandidate?.JobId;
        try
        {
            SetBusy(true);
            await _viewModel.RefreshPrintRecoveryAsync();
            _allCandidates = _viewModel.PrintRecoveryReport.Candidates;
            ApplyFilter(selectedJobId);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ANLAbel.App.Services.PrintCenterText.Failure(ex), "Trung tâm in ấn", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void FilterTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyFilter(SelectedCandidate?.JobId);
    }

    private void ApplyFilter(string? selectedJobId)
    {
        var filtered = PrintRecoveryCandidateFilter.Apply(_allCandidates, FilterTextBox.Text);
        JobsGrid.ItemsSource = filtered;
        FilterSummaryText.Text = _allCandidates.Count == filtered.Count
            ? $"{filtered.Count} lệnh"
            : $"{filtered.Count}/{_allCandidates.Count} lệnh khớp";

        if (!string.IsNullOrWhiteSpace(selectedJobId))
        {
            var selected = filtered.FirstOrDefault(candidate =>
                string.Equals(candidate.JobId, selectedJobId, StringComparison.Ordinal));
            if (selected is not null)
            {
                JobsGrid.SelectedItem = selected;
            }
        }

        if (JobsGrid.SelectedIndex < 0 && JobsGrid.Items.Count > 0)
        {
            JobsGrid.SelectedIndex = 0;
        }

        UpdateDetails();
    }

    private async void PrintCenterWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F5 && Keyboard.Modifiers == ModifierKeys.None)
        {
            e.Handled = true;
            if (!_isBusy)
            {
                await RefreshAsync();
            }

            return;
        }

        if (e.Key == Key.Escape && Keyboard.Modifiers == ModifierKeys.None)
        {
            if (FilterTextBox.Text.Length > 0)
            {
                FilterTextBox.Clear();
            }
            else
            {
                JobsGrid.Focus();
            }

            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter
            && Keyboard.Modifiers == ModifierKeys.None
            && FilterTextBox.IsKeyboardFocusWithin)
        {
            var scannedId = FilterTextBox.Text.Trim();
            var exact = _allCandidates.FirstOrDefault(candidate =>
                string.Equals(candidate.JobId, scannedId, StringComparison.OrdinalIgnoreCase));
            if (exact is not null)
            {
                ApplyFilter(exact.JobId);
                JobsGrid.SelectedItem = exact;
                JobsGrid.ScrollIntoView(exact);
                JobsGrid.Focus();
            }

            e.Handled = true;
            return;
        }

        // Ctrl+Enter is deliberately the only keyboard path that opens a
        // preview. It remains a non-dispatching, manifest-guarded action.
        if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
        {
            var candidate = SelectedCandidate;
            if (candidate?.OperatorAction == ANLAbel.Core.Printing.PrintJobOperatorAction.ReprintApproved
                && candidate.Manifest?.IsFingerprintValid == true
                && PreviewButton.IsEnabled)
            {
                e.Handled = true;
                _openApprovedPreview?.Invoke(candidate);
            }
        }
    }

    private async void ReconcileButton_Click(object sender, RoutedEventArgs e)
    {
        var candidate = RequireCandidate();
        if (candidate is null)
        {
            return;
        }

        if (candidate.Action != PrintJobRecoveryAction.ReconcileQueue)
        {
            ShowActionMessage("Lệnh này không có mã hàng đợi an toàn để đối soát. Hãy xem lại và chọn một thao tác: ghi nhận, hủy hoặc yêu cầu in lại.", MessageBoxImage.Warning);
            return;
        }

        try
        {
            SetBusy(true);
            var result = await _viewModel.ReconcilePrintJobAsync(candidate.JobId);
            ShowActionMessage(ANLAbel.App.Services.PrintCenterText.Reconcile(result), result.Outcome == PrintJobReconciliationOutcome.QueueObserved ? MessageBoxImage.Information : MessageBoxImage.Warning);
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            ShowActionMessage(ANLAbel.App.Services.PrintCenterText.Failure(ex), MessageBoxImage.Warning);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void AcknowledgeButton_Click(object sender, RoutedEventArgs e)
    {
        var candidate = RequireCandidate();
        if (candidate is null)
        {
            return;
        }

        try
        {
            SetBusy(true);
            var result = await _viewModel.AcknowledgePrintJobAsync(candidate.JobId);
            ShowActionMessage(ANLAbel.App.Services.PrintCenterText.ActionResult(result), MessageBoxImage.Information);
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            ShowActionMessage(ANLAbel.App.Services.PrintCenterText.Failure(ex), MessageBoxImage.Warning);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void VoidButton_Click(object sender, RoutedEventArgs e)
    {
        var candidate = RequireCandidate();
        if (candidate is null)
        {
            return;
        }

        var choice = MessageBox.Show(
            this,
            $"Hủy lệnh {candidate.JobId} trong lịch sử bền vững? Không có lệnh in nào được gửi và chuỗi sự kiện vẫn được giữ lại.",
            "Hủy lệnh in",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (choice != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            SetBusy(true);
            var result = await _viewModel.VoidPrintJobAsync(candidate.JobId);
            ShowActionMessage(ANLAbel.App.Services.PrintCenterText.ActionResult(result), MessageBoxImage.Information);
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            ShowActionMessage(ANLAbel.App.Services.PrintCenterText.Failure(ex), MessageBoxImage.Warning);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void RequestReprintButton_Click(object sender, RoutedEventArgs e)
    {
        var candidate = RequireCandidate();
        if (candidate is null)
        {
            return;
        }

        try
        {
            SetBusy(true);
            var result = await _viewModel.RequestPrintJobReprintAsync(candidate.JobId);
            ShowActionMessage(ANLAbel.App.Services.PrintCenterText.ActionResult(result), MessageBoxImage.Information);
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            ShowActionMessage(ANLAbel.App.Services.PrintCenterText.Failure(ex), MessageBoxImage.Warning);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void ApproveButton_Click(object sender, RoutedEventArgs e)
    {
        var candidate = RequireCandidate();
        if (candidate is null)
        {
            return;
        }

        if (candidate.OperatorAction != ANLAbel.Core.Printing.PrintJobOperatorAction.ReprintRequested
            || candidate.Manifest is null)
        {
            ShowActionMessage("Hãy chọn một yêu cầu in lại có bản kê hợp lệ trước khi duyệt.", MessageBoxImage.Warning);
            return;
        }

        try
        {
            SetBusy(true);
            var result = await _viewModel.ApprovePrintJobReprintAsync(candidate.JobId, candidate.Manifest);
            ShowActionMessage(ANLAbel.App.Services.PrintCenterText.ActionResult(result), MessageBoxImage.Information);
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            ShowActionMessage(ANLAbel.App.Services.PrintCenterText.Failure(ex), MessageBoxImage.Warning);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void PreviewButton_Click(object sender, RoutedEventArgs e)
    {
        var candidate = RequireCandidate();
        if (candidate is null)
        {
            return;
        }

        if (candidate.OperatorAction != ANLAbel.Core.Printing.PrintJobOperatorAction.ReprintApproved
            || candidate.Manifest is null)
        {
            ShowActionMessage("Chỉ yêu cầu in lại đã duyệt và có bản kê hợp lệ mới mở được Xem trước bảo vệ.", MessageBoxImage.Warning);
            return;
        }

        if (_openApprovedPreview is null)
        {
            ShowActionMessage("Cửa sổ chính chưa cung cấp thao tác mở Xem trước cho lệnh đã duyệt.", MessageBoxImage.Warning);
            return;
        }

        _openApprovedPreview(candidate);
    }

    /// <summary>
    /// Exports a redacted support-evidence JSON for the selected durable job.
    /// File I/O runs off the UI thread; the export never includes raw label
    /// payloads and never claims physical completion.
    /// </summary>
    private async void ExportSupportButton_Click(object sender, RoutedEventArgs e)
    {
        var candidate = RequireCandidate();
        if (candidate is null)
        {
            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = "Xuất dữ liệu chứng cứ hỗ trợ",
            Filter = "Dữ liệu chứng cứ (*.json)|*.json",
            FileName = $"anlabel-support-{SanitizeFileToken(candidate.JobId)}.json",
            AddExtension = true,
            DefaultExt = ".json",
            OverwritePrompt = true
        };
        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        var path = dialog.FileName;
        try
        {
            SetBusy(true);
            var bundle = BuildSupportEvidence(candidate);
            await Task.Run(() => PrintSupportEvidenceContract.WriteJsonAsync(bundle, path))
                .ConfigureAwait(true);
            ShowActionMessage(
                $"Đã xuất dữ liệu hỗ trợ (đã ẩn nội dung tem) tới:\n{path}\nDấu nhận dạng: {bundle.EvidenceFingerprint}\nTệp này không xác nhận tem đã in ra.",
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            ShowActionMessage("Không xuất được dữ liệu hỗ trợ: " + ANLAbel.App.Services.OperationFailureExplainer.StatusLine(ex), MessageBoxImage.Warning);
        }
        finally
        {
            SetBusy(false);
        }
    }

    /// <summary>
    /// Shared seam used by the Export button and tests: recovery candidates
    /// become redacted support evidence without raw label values.
    /// </summary>
    internal static PrintSupportEvidenceBundle BuildSupportEvidence(PrintJobRecoveryCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        return PrintSupportEvidenceContract.BuildFromDurableJob(
            jobId: candidate.JobId,
            printerName: candidate.PrinterName,
            spoolJobId: candidate.SpoolJobId,
            queueState: candidate.QueueState,
            documentHash: candidate.DocumentHash,
            sceneHash: candidate.SceneHash,
            outputContractHash: candidate.OutputContractHash,
            manifestFingerprint: candidate.ManifestFingerprint,
            lifecycleState: candidate.State.ToString(),
            operatorAction: candidate.OperatorAction.ToString(),
            relatedJobId: candidate.RelatedJobId,
            reason: candidate.Reason);
    }

    private static string SanitizeFileToken(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "job";
        }

        var invalid = Path.GetInvalidFileNameChars();
        var chars = value.Trim().Select(ch => invalid.Contains(ch) ? '-' : ch).ToArray();
        var token = new string(chars);
        return token.Length <= 48 ? token : token[..48];
    }

    private PrintJobRecoveryCandidate? RequireCandidate()
    {
        var candidate = SelectedCandidate;
        if (candidate is null)
        {
            ShowActionMessage("Hãy chọn một lệnh in trước.", MessageBoxImage.Information);
        }

        return candidate;
    }

    private void JobsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateDetails();
    }

    private void UpdateDetails()
    {
        var candidate = SelectedCandidate;
        if (candidate is null)
        {
            var report = _viewModel.PrintRecoveryReport;
            var notices = report.RepairNotices ?? Array.Empty<string>();
            DetailsText.Text = report.RequiresRepair
                ? "Nhật ký lệnh in bị hỏng (thường do mất điện hoặc tắt máy khi đang ghi). ANLAbel tạm dừng in để không mất bằng chứng. "
                  + "Bấm “Lưu trữ nhật ký hỏng và bắt đầu nhật ký mới” để tiếp tục.\nChi tiết kỹ thuật: "
                  + string.Join(" · ", report.StoreDiagnostics)
                : notices.Count > 0
                    ? string.Join(Environment.NewLine, notices)
                    : "Chọn một lệnh để xem bằng chứng của lệnh đó.";
        }
        else
        {
            var manifest = candidate.Manifest;
            DetailsText.Text =
                $"{candidate.JobId} · {ANLAbel.App.Services.PrintCenterText.State(candidate.State)} · {ANLAbel.App.Services.PrintCenterText.RecoveryAction(candidate.Action)}\n" +
                $"Máy in: {candidate.PrinterName} · Mã lệnh hàng đợi: {candidate.SpoolJobId?.ToString() ?? "không có"} · Hàng đợi: {ANLAbel.App.Services.PrintCenterText.Queue(candidate.QueueState)}\n" +
                $"Bản kê: {candidate.ManifestFingerprint} · Bản kê hợp lệ: {(manifest?.IsFingerprintValid == true ? "có" : "không")}\n" +
                ANLAbel.App.Services.PrintCenterText.Reason(candidate.Reason);
        }

        UpdateActionState();
    }

    private void UpdateActionState()
    {
        var candidate = SelectedCandidate;
        var hasCandidate = candidate is not null;
        var nonTerminal = hasCandidate && candidate!.State is not ANLAbel.Core.Printing.PrintJobLifecycleState.Completed
            and not ANLAbel.Core.Printing.PrintJobLifecycleState.Failed
            and not ANLAbel.Core.Printing.PrintJobLifecycleState.Cancelled;
        ReconcileButton.IsEnabled = nonTerminal && candidate!.Action == PrintJobRecoveryAction.ReconcileQueue;
        AcknowledgeButton.IsEnabled = nonTerminal;
        VoidButton.IsEnabled = nonTerminal;
        RequestReprintButton.IsEnabled = nonTerminal
            && candidate!.OperatorAction is not ANLAbel.Core.Printing.PrintJobOperatorAction.ReprintRequested
            and not ANLAbel.Core.Printing.PrintJobOperatorAction.ReprintApproved;
        ApproveButton.IsEnabled = nonTerminal
            && candidate!.OperatorAction == ANLAbel.Core.Printing.PrintJobOperatorAction.ReprintRequested
            && candidate.Manifest?.IsFingerprintValid == true;
        PreviewButton.IsEnabled = nonTerminal
            && candidate!.OperatorAction == ANLAbel.Core.Printing.PrintJobOperatorAction.ReprintApproved
            && candidate.Manifest?.IsFingerprintValid == true;
        // Export is available for any selected durable job, including terminal
        // states, so support can reconstruct completed/failed outcomes.
        ExportSupportButton.IsEnabled = hasCandidate;
        RepairLogButton.Visibility = _viewModel.PrintRecoveryReport.RequiresRepair ? Visibility.Visible : Visibility.Collapsed;
        RepairLogButton.IsEnabled = !_isBusy;
    }

    private async void RepairLogButton_Click(object sender, RoutedEventArgs e)
    {
        var choice = OperatorDialog.Ask(
            this,
            "Nhật ký lệnh in bị hỏng ở dòng cuối",
            [
                "Thường do mất điện hoặc tắt máy khi đang ghi. ANLAbel tạm dừng in để không mất bằng chứng.",
                "Lưu trữ nhật ký hỏng và tiếp tục với phần còn hợp lệ? Tệp cũ được giữ nguyên để tra cứu."
            ],
            null,
            OperatorDialogTone.Error,
            new OperatorDialogChoice("archive", "Lưu trữ nhật ký hỏng và bắt đầu nhật ký mới", IsDefault: true),
            new OperatorDialogChoice("cancel", "Hủy", IsCancel: true));
        if (choice != "archive")
        {
            return;
        }

        try
        {
            SetBusy(true);
            var archive = await _viewModel.ArchiveDamagedPrintLogAsync();
            _allCandidates = _viewModel.PrintRecoveryReport.Candidates;
            ApplyFilter(null);
            ShowActionMessage(
                archive is null
                    ? "Nhật ký lệnh in không còn lỗi; không cần lưu trữ."
                    : $"Đã lưu trữ nhật ký hỏng thành {System.IO.Path.GetFileName(archive)}. Có thể in tiếp.",
                MessageBoxImage.Information);
        }
        catch (Exception ex) when (ex is System.IO.IOException or UnauthorizedAccessException)
        {
            ShowActionMessage("Chưa lưu trữ được nhật ký: " + ANLAbel.App.Services.OperationFailureExplainer.StatusLine(ex), MessageBoxImage.Warning);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetBusy(bool busy)
    {
        _isBusy = busy;
        RefreshButton.IsEnabled = !busy;
        JobsGrid.IsEnabled = !busy;
        if (!busy)
        {
            UpdateActionState();
        }
        else
        {
            ReconcileButton.IsEnabled = false;
            AcknowledgeButton.IsEnabled = false;
            VoidButton.IsEnabled = false;
            RequestReprintButton.IsEnabled = false;
            ApproveButton.IsEnabled = false;
            PreviewButton.IsEnabled = false;
            ExportSupportButton.IsEnabled = false;
        }
    }

    private void ShowActionMessage(string message, MessageBoxImage image)
    {
        MessageBox.Show(this, message, "Trung tâm in ấn", MessageBoxButton.OK, image);
    }
}
