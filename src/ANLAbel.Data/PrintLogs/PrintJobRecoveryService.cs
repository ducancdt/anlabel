using ANLAbel.Core.Printing;

namespace ANLAbel.Data.PrintLogs;

/// <summary>
/// A read-only replay result used by the app's recovery banner and the future
/// Print Center. It intentionally contains only the last valid event per job;
/// the event store remains the source of truth for the full lineage.
/// </summary>
public sealed record PrintJobRecoverySnapshot(
    IReadOnlyList<PrintJobStateEvent> LatestEvents,
    IReadOnlyList<string> StoreDiagnostics)
{
    public static PrintJobRecoverySnapshot Empty { get; } = new(
        Array.Empty<PrintJobStateEvent>(),
        Array.Empty<string>());
}

public enum PrintJobRecoveryAction
{
    None,
    ReconcileQueue,
    OperatorDecision,
    RepairEventLog
}

public enum PrintJobReconciliationOutcome
{
    QueueObserved,
    TimedOut,
    OperatorDecisionRequired,
    RepairRequired,
    InvalidCandidate
}

/// <summary>
/// Result of one bounded, read-only queue re-query. It never authorizes a
/// reprint and never sets physical-output verification.
/// </summary>
public sealed record PrintJobReconciliationResult(
    string JobId,
    PrintJobLifecycleState PriorState,
    PrintJobRecoveryAction RequestedAction,
    PrintJobReconciliationOutcome Outcome,
    SpoolJobMonitorResult? QueueResult,
    string Summary)
{
    public bool AutomaticRetryAllowed => false;

    public bool PhysicalOutputVerified => false;

    public bool RequiresOperatorDecision => Outcome is not PrintJobReconciliationOutcome.RepairRequired
        and not PrintJobReconciliationOutcome.InvalidCandidate;
}

/// <summary>
/// One non-terminal job that must be reconciled after a restart or uncertain
/// dispatch. Automatic retry is deliberately never allowed by this contract.
/// </summary>
public sealed record PrintJobRecoveryCandidate(
    string JobId,
    PrintJobLifecycleState State,
    PrintJobRecoveryAction Action,
    DateTimeOffset LastEventUtc,
    string PrinterName,
    int? SpoolJobId,
    string QueueState,
    string DocumentHash,
    string SceneHash,
    string OutputContractHash,
    string Reason,
    PrintJobOperatorAction OperatorAction = PrintJobOperatorAction.None,
    string RelatedJobId = "",
    string Actor = "",
    string ManifestFingerprint = "",
    PrintJobManifest? Manifest = null)
{
    public bool AutomaticRetryAllowed => false;
}

public sealed record PrintJobRecoveryReport(
    IReadOnlyList<PrintJobRecoveryCandidate> Candidates,
    IReadOnlyList<string> StoreDiagnostics)
{
    public static PrintJobRecoveryReport Empty { get; } = new(
        Array.Empty<PrintJobRecoveryCandidate>(),
        Array.Empty<string>());

    public bool HasPendingJobs => Candidates.Count > 0;

    public bool RequiresRepair => StoreDiagnostics.Count > 0;

    public bool AutomaticRetryAllowed => false;

    public string UserFacingSummary
    {
        get
        {
            if (RequiresRepair)
            {
                return "Print history needs repair before an uncertain job can be retried.";
            }

            return HasPendingJobs
                ? $"{Candidates.Count} print job(s) need reconciliation before retry."
                : "No print jobs need reconciliation.";
        }
    }
}

/// <summary>
/// Classifies durable job tails after process/spooler restart. This service has
/// no dispatch API on purpose: the safe result is a queue re-query or an explicit
/// operator decision, never an implicit duplicate print.
/// </summary>
public static class PrintJobRecoveryService
{
    public static PrintJobReconciliationResult CreateInvalidResult(
        string jobId,
        string reason)
    {
        return new PrintJobReconciliationResult(
            jobId ?? string.Empty,
            PrintJobLifecycleState.Unknown,
            PrintJobRecoveryAction.OperatorDecision,
            PrintJobReconciliationOutcome.InvalidCandidate,
            null,
            reason);
    }

    public static async Task<PrintJobRecoveryReport> LoadAsync(
        PrintJobStateStore store,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(store);
        var snapshot = await store.ReadRecoverySnapshotAsync(cancellationToken).ConfigureAwait(false);
        return Analyze(snapshot);
    }

    public static PrintJobRecoveryReport Analyze(PrintJobRecoverySnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var candidates = new List<PrintJobRecoveryCandidate>();
        foreach (var lastEvent in snapshot.LatestEvents)
        {
            if (PrintJobStateMachine.IsTerminal(lastEvent.To))
            {
                continue;
            }

            var (action, reason) = Classify(lastEvent);
            candidates.Add(new PrintJobRecoveryCandidate(
                lastEvent.JobId,
                lastEvent.To,
                action,
                lastEvent.TimestampUtc,
                lastEvent.PrinterName,
                lastEvent.SpoolJobId,
                lastEvent.QueueState,
                lastEvent.DocumentHash,
                lastEvent.SceneHash,
                lastEvent.OutputContractHash,
                reason,
                lastEvent.OperatorAction,
                lastEvent.RelatedJobId,
                lastEvent.Actor,
                lastEvent.ManifestFingerprint,
                lastEvent.Manifest));
        }

        if (snapshot.StoreDiagnostics.Count > 0)
        {
            // The valid prefix is useful for support, but no candidate may be
            // treated as safe to retry while the append log itself is damaged.
            candidates = candidates
                .Select(candidate => candidate with
                {
                    Action = PrintJobRecoveryAction.RepairEventLog,
                    Reason = "The event log has a corrupt or incomplete tail; repair/archive it before any retry."
                })
                .ToList();
        }

        return new PrintJobRecoveryReport(
            candidates
                .OrderByDescending(candidate => candidate.LastEventUtc)
                .ThenBy(candidate => candidate.JobId, StringComparer.Ordinal)
                .ToArray(),
            snapshot.StoreDiagnostics.ToArray());
    }

    /// <summary>
    /// Queries one candidate's captured printer/job identity. The reader is
    /// injected so tests can model pending, terminal, timeout and identity-fault
    /// behavior without touching a real Windows queue.
    /// </summary>
    public static async Task<PrintJobReconciliationResult> ReconcileQueueAsync(
        PrintJobRecoveryCandidate candidate,
        ISpoolJobStatusReader statusReader,
        TimeSpan? timeout = null,
        TimeSpan? pollInterval = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(statusReader);

        if (candidate.Action != PrintJobRecoveryAction.ReconcileQueue)
        {
            return new PrintJobReconciliationResult(
                candidate.JobId,
                candidate.State,
                candidate.Action,
                candidate.Action == PrintJobRecoveryAction.RepairEventLog
                    ? PrintJobReconciliationOutcome.RepairRequired
                    : PrintJobReconciliationOutcome.InvalidCandidate,
                null,
                candidate.Reason);
        }

        if (candidate.SpoolJobId is not int spoolJobId
            || spoolJobId <= 0
            || string.IsNullOrWhiteSpace(candidate.PrinterName))
        {
            return new PrintJobReconciliationResult(
                candidate.JobId,
                candidate.State,
                candidate.Action,
                PrintJobReconciliationOutcome.InvalidCandidate,
                null,
                "The recovery candidate has no safe printer/job identity; an operator must decide and automatic retry remains disabled.");
        }

        var monitor = new SpoolJobMonitor(statusReader);
        var queueResult = await monitor.MonitorAsync(
            candidate.PrinterName,
            spoolJobId,
            timeout ?? TimeSpan.FromSeconds(3),
            pollInterval ?? TimeSpan.FromMilliseconds(250),
            cancellationToken).ConfigureAwait(false);

        if (queueResult.TimedOut)
        {
            return new PrintJobReconciliationResult(
                candidate.JobId,
                candidate.State,
                candidate.Action,
                PrintJobReconciliationOutcome.TimedOut,
                queueResult,
                $"{queueResult.UserFacingStatus} Operator decision required; automatic retry is disabled.");
        }

        // A queue fault is terminal for this reconciliation attempt even when
        // Windows has not removed the job yet.  Keeping these states as a
        // generic QueueObserved result makes Print Center look like it is safe
        // to keep polling, while the operator actually needs to clear media,
        // resume the device or inspect the driver before deciding what to do.
        var terminalQueueState = IsTerminalQueueState(queueResult.FinalObservation.State);
        var outcome = terminalQueueState || queueResult.FinalObservation.State == SpoolJobState.Unknown
            ? PrintJobReconciliationOutcome.OperatorDecisionRequired
            : PrintJobReconciliationOutcome.QueueObserved;
        var summary = terminalQueueState
            ? $"{queueResult.UserFacingStatus} Physical output is not verified; operator decision required."
            : $"{queueResult.UserFacingStatus} No automatic retry is permitted.";

        return new PrintJobReconciliationResult(
            candidate.JobId,
            candidate.State,
            candidate.Action,
            outcome,
            queueResult,
            summary);
    }

    private static (PrintJobRecoveryAction Action, string Reason) Classify(PrintJobStateEvent lastEvent)
    {
        if (lastEvent.OperatorAction == PrintJobOperatorAction.Acknowledge)
        {
            return (
                PrintJobRecoveryAction.OperatorDecision,
                "An operator acknowledged this uncertain job; review the queue evidence or void it before any reprint.");
        }

        if (lastEvent.OperatorAction == PrintJobOperatorAction.ReprintRequested)
        {
            return (
                PrintJobRecoveryAction.OperatorDecision,
                $"A linked reprint request ({lastEvent.RelatedJobId}) is recorded; dispatch remains a separate explicit action.");
        }

        if (lastEvent.OperatorAction == PrintJobOperatorAction.ReprintApproved)
        {
            return (
                PrintJobRecoveryAction.OperatorDecision,
                $"A linked reprint was approved against manifest {lastEvent.ManifestFingerprint}; dispatch still requires an explicit current-template/data match.");
        }

        if (lastEvent.To is PrintJobLifecycleState.SpoolAccepted
            or PrintJobLifecycleState.QueueObserved
            or PrintJobLifecycleState.Unknown)
        {
            if (IsTerminalQueueState(lastEvent.QueueState))
            {
                return (
                    PrintJobRecoveryAction.OperatorDecision,
                    "The queue reported a terminal state, but physical output is not verified; an operator must decide.");
            }

            if (lastEvent.SpoolJobId is int spoolJobId
                && spoolJobId > 0
                && !string.IsNullOrWhiteSpace(lastEvent.PrinterName))
            {
                return (
                    PrintJobRecoveryAction.ReconcileQueue,
                    "A printer and spool identity are available; query the queue before deciding whether to reprint.");
            }

            return (
                PrintJobRecoveryAction.OperatorDecision,
                "Dispatch reached an uncertain state without a safely reusable queue identity; an operator must decide.");
        }

        return (
            PrintJobRecoveryAction.OperatorDecision,
            "The process stopped before a terminal print outcome was recorded; do not retry automatically.");
    }

    private static bool IsTerminalQueueState(SpoolJobState state)
    {
        return state is SpoolJobState.Completed
            or SpoolJobState.Deleted
            or SpoolJobState.NotFound
            or SpoolJobState.Error
            or SpoolJobState.Offline
            or SpoolJobState.PaperOut
            or SpoolJobState.UserIntervention
            or SpoolJobState.Blocked
            or SpoolJobState.Paused
            or SpoolJobState.Retained;
    }

    private static bool IsTerminalQueueState(string queueState)
    {
        return Enum.TryParse<SpoolJobState>(queueState, ignoreCase: true, out var state)
            && IsTerminalQueueState(state);
    }
}
