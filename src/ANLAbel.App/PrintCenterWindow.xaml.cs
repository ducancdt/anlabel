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
            MessageBox.Show(this, ex.Message, "Print Center", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            ? $"{filtered.Count} job(s)"
            : $"{filtered.Count}/{_allCandidates.Count} match";

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
            ShowActionMessage("This job has no safe queue identity for reconciliation. Review or choose an explicit operator action.", MessageBoxImage.Warning);
            return;
        }

        try
        {
            SetBusy(true);
            var result = await _viewModel.ReconcilePrintJobAsync(candidate.JobId);
            ShowActionMessage(result.Summary, result.Outcome == PrintJobReconciliationOutcome.QueueObserved ? MessageBoxImage.Information : MessageBoxImage.Warning);
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            ShowActionMessage(ex.Message, MessageBoxImage.Warning);
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
            ShowActionMessage(result.Summary, MessageBoxImage.Information);
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            ShowActionMessage(ex.Message, MessageBoxImage.Warning);
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
            $"Void {candidate.JobId} in the durable history? No printer command is sent and the event chain is retained.",
            "Void print job",
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
            ShowActionMessage(result.Summary, MessageBoxImage.Information);
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            ShowActionMessage(ex.Message, MessageBoxImage.Warning);
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
            ShowActionMessage(result.Summary, MessageBoxImage.Information);
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            ShowActionMessage(ex.Message, MessageBoxImage.Warning);
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
            ShowActionMessage("Select a linked reprint request with a valid manifest before approving it.", MessageBoxImage.Warning);
            return;
        }

        try
        {
            SetBusy(true);
            var result = await _viewModel.ApprovePrintJobReprintAsync(candidate.JobId, candidate.Manifest);
            ShowActionMessage(result.Summary, MessageBoxImage.Information);
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            ShowActionMessage(ex.Message, MessageBoxImage.Warning);
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
            ShowActionMessage("Only an approved linked child with a valid manifest can open the guarded preview.", MessageBoxImage.Warning);
            return;
        }

        if (_openApprovedPreview is null)
        {
            ShowActionMessage("The host window did not provide an approved-preview action.", MessageBoxImage.Warning);
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
            Title = "Export redacted support evidence",
            Filter = "Support evidence (*.json)|*.json",
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
                $"Redacted support evidence written to:\n{path}\nFingerprint: {bundle.EvidenceFingerprint}\nPhysical completion is not claimed.",
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            ShowActionMessage($"Could not export support evidence: {ex.Message}", MessageBoxImage.Warning);
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
            ShowActionMessage("Select a print job first.", MessageBoxImage.Information);
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
            DetailsText.Text = _viewModel.PrintRecoveryReport.RequiresRepair
                ? $"Event log diagnostics:\n{string.Join(Environment.NewLine, _viewModel.PrintRecoveryReport.StoreDiagnostics)}"
                : "Select a job to inspect its durable evidence.";
        }
        else
        {
            var manifest = candidate.Manifest;
            DetailsText.Text =
                $"{candidate.JobId} · {candidate.State} · {candidate.Action}\n" +
                $"Printer: {candidate.PrinterName} · Spool: {candidate.SpoolJobId?.ToString() ?? "none"} · Queue: {candidate.QueueState}\n" +
                $"Manifest: {candidate.ManifestFingerprint} · Metadata valid: {manifest?.IsFingerprintValid == true}\n" +
                candidate.Reason;
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
        MessageBox.Show(this, message, "Print Center", MessageBoxButton.OK, image);
    }
}
