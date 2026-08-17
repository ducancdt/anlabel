using ANLAbel.Core.Automation;
using ANLAbel.Core.Data;
using ANLAbel.Data.Automation;
using System.IO;

namespace ANLAbel.App;

/// <summary>
/// Explicit local dispatch coordinator. It atomically records Dispatching
/// before invoking a caller-provided dispatcher, so one event identity cannot
/// be submitted twice. It never retries an ambiguous invocation.
/// </summary>
public sealed class AutomationPreparedBatchDispatchService
{
    private readonly FileDropClaimLedger _ledger;
    private readonly AutomationJobHistoryStore _history;

    public AutomationPreparedBatchDispatchService(FileDropClaimLedger ledger, AutomationJobHistoryStore? history = null)
    {
        _ledger = ledger;
        _history = history ?? new AutomationJobHistoryStore(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ANLAbel", "automation-job-history.jsonl"));
    }

    public async Task<AutomationPreparedBatchDispatchResult> DispatchAsync(
        FileDropPreparedBatchIdentity batch,
        FileDropTriggerConfiguration configuration,
        IReadOnlyList<DataRecord> records,
        AutomationPreparedBatchPreflightResult preflight,
        Func<AutomationPreparedBatchDispatchRequest, CancellationToken, Task<AutomationPreparedBatchDispatchReceipt>> dispatcher,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(batch);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(records);
        ArgumentNullException.ThrowIfNull(preflight);
        ArgumentNullException.ThrowIfNull(dispatcher);
        if (!preflight.Passed || string.IsNullOrWhiteSpace(preflight.CanonicalQueueName))
            return new(false, "Automation dispatch requires a passed exact-template/named-queue preflight.");

        var identity = FileDropClaimContract.CreateIdentity(batch.TriggerId, batch.ConfigurationFingerprint, batch.SourceFingerprint);
        if (!string.Equals(identity.EventId, batch.EventId, StringComparison.Ordinal)
            || !string.Equals(batch.TriggerId, configuration.TriggerId, StringComparison.Ordinal)
            || !string.Equals(batch.ConfigurationFingerprint, configuration.ConfigurationFingerprint, StringComparison.Ordinal)
            || batch.RecordCount != records.Count
            || !string.Equals(batch.DataFingerprint, FileDropPreparedBatchContract.ComputeDataFingerprint(records), StringComparison.Ordinal))
            return new(false, "Automation dispatch rejected a batch that no longer matches its event, configuration or records.");

        if (!_ledger.TryTransition(identity, FileDropEventState.Dispatching,
                $"Explicit local dispatch started for prepared batch {batch.PreparedBatchId}; no retry is permitted if the process ends.",
                out _, out var claimError))
            return new(false, claimError);

        try
        {
            var receipt = await dispatcher(new(batch, configuration, records, preflight.CanonicalQueueName), cancellationToken);
            if (!receipt.Accepted)
            {
                var detail = string.IsNullOrWhiteSpace(receipt.Detail) ? "Local dispatcher rejected the prepared batch." : receipt.Detail;
                _ledger.TryTransition(identity, FileDropEventState.Blocked, detail, out _, out _);
                return new(false, detail);
            }

            if (string.IsNullOrWhiteSpace(receipt.JobId))
            {
                const string missingJob = "Dispatcher accepted a request without a durable job ID; event is blocked without retry.";
                _ledger.TryTransition(identity, FileDropEventState.Blocked, missingJob, out _, out _);
                return new(false, missingJob);
            }
            if (!_history.TryAppend(batch.EventId, batch.PreparedBatchId, receipt.JobId, receipt.ManifestFingerprint, out _, out var historyError))
            {
                var detail = $"Dispatch was not finalized because durable automation-to-job history could not be recorded: {historyError}";
                _ledger.TryTransition(identity, FileDropEventState.Blocked, detail, out _, out _);
                return new(false, detail);
            }

            var acceptedDetail = string.IsNullOrWhiteSpace(receipt.Detail)
                ? "Local dispatcher accepted the request; physical output remains unverified."
                : receipt.Detail;
            if (!_ledger.TryTransition(identity, FileDropEventState.Dispatched, acceptedDetail, out _, out var terminalError))
                return new(false, terminalError);
            return new(true, acceptedDetail);
        }
        catch (Exception ex) when (ex is OperationCanceledException or IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            var detail = $"Dispatch outcome is ambiguous or failed; event is terminally blocked without retry: {ex.Message}";
            _ledger.TryTransition(identity, FileDropEventState.Blocked, detail, out _, out _);
            return new(false, detail);
        }
    }

    /// <summary>Startup recovery blocks in-flight work instead of replaying it.</summary>
    public int BlockInterruptedDispatches()
    {
        var events = _ledger.ReadValid(out var diagnostics);
        if (diagnostics.Count != 0) return 0;
        var interrupted = events
            .GroupBy(item => item.Identity.EventId, StringComparer.Ordinal)
            .Select(group => group.Last())
            .Where(item => item.To == FileDropEventState.Dispatching)
            .ToArray();
        var blocked = 0;
        foreach (var item in interrupted)
            if (_ledger.TryTransition(item.Identity, FileDropEventState.Blocked,
                "Application restarted while dispatch was in progress; outcome is ambiguous and will not be retried automatically.", out _, out _))
                blocked++;
        return blocked;
    }
}

public sealed record AutomationPreparedBatchDispatchRequest(
    FileDropPreparedBatchIdentity Batch,
    FileDropTriggerConfiguration Configuration,
    IReadOnlyList<DataRecord> Records,
    string CanonicalQueueName);

public sealed record AutomationPreparedBatchDispatchReceipt(bool Accepted, string JobId = "", string ManifestFingerprint = "", string Detail = "");

public sealed record AutomationPreparedBatchDispatchResult(bool Dispatched, string Diagnostic);
