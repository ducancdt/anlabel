using System.ComponentModel;
using System.Runtime.CompilerServices;
using ANLAbel.Data.PrintLogs;

namespace ANLAbel.App.ViewModels;

public sealed record OperationsQueueEvidence(
    string RequestedName,
    bool IsAvailable,
    string CanonicalName,
    string ErrorMessage,
    DateTimeOffset ObservedAtUtc);

/// <summary>
/// Immutable-at-apply projection for the local Operations Overview. Source
/// owners remain MainViewModel and the durable print event log; this type only
/// coordinates a bounded read and rejects stale refresh results.
/// </summary>
public sealed class OperationsOverviewViewModel : INotifyPropertyChanged
{
    private readonly Func<CancellationToken, Task<OperationsQueueEvidence>> _loadQueue;
    private readonly Func<CancellationToken, Task<PrintJobRecoveryReport>> _loadRecovery;
    private readonly Func<DateTimeOffset> _clock;
    private int _requestedEpoch;
    private bool _isRefreshing;
    private DateTimeOffset? _refreshedAtUtc;
    private string _queueName = "Not selected";
    private string _queueStateText = "Not checked";
    private string _queueDetailText = "Refresh to verify the queue saved in the current template.";
    private string _queueObservedText = "No observation yet";
    private string _recoveryStateText = "Not checked";
    private string _recoveryDetailText = "Refresh to inspect durable print-job evidence.";
    private string _diagnosticsText = "No diagnostics loaded.";
    private string _sourceStatusText = "Ready to refresh local evidence.";
    private IReadOnlyList<PrintJobRecoveryCandidate> _candidates = Array.Empty<PrintJobRecoveryCandidate>();

    public OperationsOverviewViewModel(
        Func<CancellationToken, Task<OperationsQueueEvidence>> loadQueue,
        Func<CancellationToken, Task<PrintJobRecoveryReport>> loadRecovery,
        Func<DateTimeOffset>? clock = null)
    {
        _loadQueue = loadQueue ?? throw new ArgumentNullException(nameof(loadQueue));
        _loadRecovery = loadRecovery ?? throw new ArgumentNullException(nameof(loadRecovery));
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsRefreshing
    {
        get => _isRefreshing;
        private set
        {
            if (SetProperty(ref _isRefreshing, value))
            {
                OnPropertyChanged(nameof(CanRefresh));
                OnPropertyChanged(nameof(RefreshButtonText));
            }
        }
    }

    public bool CanRefresh => !IsRefreshing;

    public string RefreshButtonText => IsRefreshing ? "Refreshing…" : "Refresh";

    public DateTimeOffset? RefreshedAtUtc
    {
        get => _refreshedAtUtc;
        private set
        {
            if (SetProperty(ref _refreshedAtUtc, value))
            {
                OnPropertyChanged(nameof(RefreshedAtText));
            }
        }
    }

    public string RefreshedAtText => RefreshedAtUtc is null
        ? "Not refreshed"
        : $"Refreshed {RefreshedAtUtc.Value.ToLocalTime():yyyy-MM-dd HH:mm:ss}";

    public string QueueName { get => _queueName; private set => SetProperty(ref _queueName, value); }
    public string QueueStateText { get => _queueStateText; private set => SetProperty(ref _queueStateText, value); }
    public string QueueDetailText { get => _queueDetailText; private set => SetProperty(ref _queueDetailText, value); }
    public string QueueObservedText { get => _queueObservedText; private set => SetProperty(ref _queueObservedText, value); }
    public string RecoveryStateText { get => _recoveryStateText; private set => SetProperty(ref _recoveryStateText, value); }
    public string RecoveryDetailText { get => _recoveryDetailText; private set => SetProperty(ref _recoveryDetailText, value); }
    public string DiagnosticsText { get => _diagnosticsText; private set => SetProperty(ref _diagnosticsText, value); }
    public string SourceStatusText { get => _sourceStatusText; private set => SetProperty(ref _sourceStatusText, value); }
    public IReadOnlyList<PrintJobRecoveryCandidate> Candidates { get => _candidates; private set => SetProperty(ref _candidates, value); }

    public async Task<bool> RefreshAsync(CancellationToken cancellationToken = default)
    {
        var epoch = Interlocked.Increment(ref _requestedEpoch);
        IsRefreshing = true;
        SourceStatusText = "Reading queue and recovery evidence…";

        try
        {
            var queueTask = ObserveAsync(_loadQueue, cancellationToken);
            var recoveryTask = ObserveAsync(_loadRecovery, cancellationToken);
            await Task.WhenAll(queueTask, recoveryTask).ConfigureAwait(true);

            if (epoch != Volatile.Read(ref _requestedEpoch))
            {
                return false;
            }

            var queue = await queueTask.ConfigureAwait(true);
            var recovery = await recoveryTask.ConfigureAwait(true);
            ApplyQueue(queue.Value, queue.Error);
            ApplyRecovery(recovery.Value, recovery.Error);
            RefreshedAtUtc = _clock();
            SourceStatusText = BuildSourceStatus(queue.Error, recovery.Error);
            return true;
        }
        finally
        {
            if (epoch == Volatile.Read(ref _requestedEpoch))
            {
                IsRefreshing = false;
            }
        }
    }

    private void ApplyQueue(OperationsQueueEvidence? evidence, Exception? error)
    {
        if (error is not null || evidence is null)
        {
            QueueName = "Queue source unavailable";
            QueueStateText = "Read failed";
            QueueDetailText = error?.Message ?? "No queue evidence was returned.";
            QueueObservedText = "Observation unavailable";
            return;
        }

        QueueName = !string.IsNullOrWhiteSpace(evidence.CanonicalName)
            ? evidence.CanonicalName
            : !string.IsNullOrWhiteSpace(evidence.RequestedName)
                ? evidence.RequestedName
                : "Not selected";
        QueueStateText = evidence.IsAvailable ? "Available" : "Action required";
        QueueDetailText = evidence.IsAvailable
            ? "The saved queue resolved exactly; no Windows default fallback was used."
            : string.IsNullOrWhiteSpace(evidence.ErrorMessage)
                ? "The saved queue is unavailable. Open Printer Setup to choose a verified queue."
                : evidence.ErrorMessage;
        QueueObservedText = $"Observed {evidence.ObservedAtUtc.ToLocalTime():yyyy-MM-dd HH:mm:ss}";
    }

    private void ApplyRecovery(PrintJobRecoveryReport? report, Exception? error)
    {
        if (error is not null || report is null)
        {
            RecoveryStateText = "Read failed";
            RecoveryDetailText = error?.Message ?? "No recovery report was returned.";
            Candidates = Array.Empty<PrintJobRecoveryCandidate>();
            DiagnosticsText = "Recovery diagnostics unavailable.";
            return;
        }

        Candidates = report.Candidates;
        RecoveryStateText = report.RequiresRepair
            ? "Event log repair required"
            : report.HasPendingJobs
                ? $"{report.Candidates.Count} job(s) need review"
                : "Clear";
        RecoveryDetailText = report.UserFacingSummary;
        DiagnosticsText = report.StoreDiagnostics.Count == 0
            ? "No durable-store diagnostics. Automatic retry remains disabled."
            : string.Join(Environment.NewLine, report.StoreDiagnostics.Select(item => $"• {item}"));
    }

    private static string BuildSourceStatus(Exception? queueError, Exception? recoveryError)
    {
        var failed = (queueError is not null ? 1 : 0) + (recoveryError is not null ? 1 : 0);
        return failed switch
        {
            0 => "Local evidence is current.",
            1 => "Partial result: one local source could not be read.",
            _ => "Local evidence could not be refreshed."
        };
    }

    private static async Task<(T? Value, Exception? Error)> ObserveAsync<T>(
        Func<CancellationToken, Task<T>> loader,
        CancellationToken cancellationToken)
        where T : class
    {
        try
        {
            return (await loader(cancellationToken).ConfigureAwait(true), null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return (null, ex);
        }
    }

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
