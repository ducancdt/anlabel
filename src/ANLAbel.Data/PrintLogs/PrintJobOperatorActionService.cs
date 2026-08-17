using ANLAbel.Core.Printing;

namespace ANLAbel.Data.PrintLogs;

/// <summary>
/// Result of an explicit operator decision. This service records intent and
/// lineage only; it has no printer or dispatch dependency and can never retry a
/// job implicitly.
/// </summary>
public sealed record PrintJobOperatorActionResult(
    string JobId,
    PrintJobOperatorAction Action,
    bool Succeeded,
    PrintJobStateEvent? Event,
    string RelatedJobId,
    PrintJobStateEvent? RelatedEvent,
    string Summary)
{
    public bool AutomaticRetryAllowed => false;

    public bool PhysicalOutputVerified => false;
}

/// <summary>
/// Appends acknowledge/void/reprint-request decisions to the same hash-chained
/// event store used by dispatch. A reprint request creates a linked child in
/// <c>Created</c> state, but deliberately does not prepare or submit it.
/// </summary>
public static class PrintJobOperatorActionService
{
    public static Task<PrintJobOperatorActionResult> AcknowledgeAsync(
        PrintJobStateStore store,
        string jobId,
        string actor = "operator",
        string reason = "Operator acknowledged the uncertain print job.",
        CancellationToken cancellationToken = default)
    {
        return AppendDecisionAsync(
            store,
            jobId,
            PrintJobOperatorAction.Acknowledge,
            actor,
            reason,
            cancellationToken);
    }

    public static Task<PrintJobOperatorActionResult> VoidAsync(
        PrintJobStateStore store,
        string jobId,
        string actor = "operator",
        string reason = "Operator voided the uncertain print job.",
        CancellationToken cancellationToken = default)
    {
        return AppendDecisionAsync(
            store,
            jobId,
            PrintJobOperatorAction.Void,
            actor,
            reason,
            cancellationToken);
    }

    public static async Task<PrintJobOperatorActionResult> RequestReprintAsync(
        PrintJobStateStore store,
        string jobId,
        string actor = "operator",
        string reason = "Operator requested an explicitly linked reprint.",
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(store);
        var current = await ReadCurrentAsync(store, jobId, cancellationToken).ConfigureAwait(false);
        EnsureActionable(current, jobId);
        if (current.OperatorAction == PrintJobOperatorAction.ReprintRequested)
        {
            throw new InvalidOperationException(
                $"Job '{jobId}' already has a linked reprint request; review that child before requesting another.");
        }

        var childJobId = $"{jobId}:reprint:{Guid.NewGuid():N}";
        var parentEvent = await store.AppendAsync(
            BuildTransition(
                current,
                current.To,
                PrintJobOperatorAction.ReprintRequested,
                actor,
                reason,
                relatedJobId: childJobId),
            cancellationToken).ConfigureAwait(false);

        // The child is only a durable lineage marker. It remains Created and
        // must pass the normal preparation/preflight/dispatch path separately.
        var childEvent = await store.AppendAsync(
            new PrintJobStateTransition(
                childJobId,
                PrintJobLifecycleState.Created,
                PrintJobLifecycleState.Created,
                DateTimeOffset.UtcNow,
                "Linked reprint requested; no dispatch has been started.",
                PrinterName: current.PrinterName,
                SpoolJobId: null,
                QueueState: string.Empty,
                DocumentHash: current.DocumentHash,
                SceneHash: current.SceneHash,
                OutputContractHash: current.OutputContractHash,
                OperatorAction: PrintJobOperatorAction.ReprintRequested,
                RelatedJobId: current.JobId,
                Actor: NormalizeActor(actor),
                ManifestFingerprint: current.ManifestFingerprint,
                Manifest: current.Manifest),
            cancellationToken).ConfigureAwait(false);

        return new PrintJobOperatorActionResult(
            jobId,
            PrintJobOperatorAction.ReprintRequested,
            true,
            parentEvent,
            childJobId,
            childEvent,
            $"Linked reprint job {childJobId} was created but not dispatched. Review and run preparation explicitly; automatic retry remains disabled.");
    }

    /// <summary>
    /// Records explicit approval for a linked child. Approval is valid only when
    /// the caller presents the exact immutable manifest that was captured on the
    /// child; this event still does not dispatch or claim physical output.
    /// </summary>
    public static async Task<PrintJobOperatorActionResult> ApproveReprintAsync(
        PrintJobStateStore store,
        string childJobId,
        PrintJobManifest expectedManifest,
        string actor = "operator",
        string reason = "Operator approved the linked reprint after manifest review.",
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(expectedManifest);
        var current = await ReadCurrentAsync(store, childJobId, cancellationToken).ConfigureAwait(false);
        EnsureActionable(current, childJobId);

        if (current.To != PrintJobLifecycleState.Created
            || current.OperatorAction != PrintJobOperatorAction.ReprintRequested
            || string.IsNullOrWhiteSpace(current.RelatedJobId)
            || current.Manifest is null
            || string.IsNullOrWhiteSpace(current.ManifestFingerprint)
            || !current.Manifest.IsFingerprintValid)
        {
            throw new InvalidOperationException(
                $"Reprint child '{childJobId}' is not awaiting approval or has no immutable manifest metadata.");
        }

        if (!expectedManifest.IsFingerprintValid
            || !string.Equals(expectedManifest.Fingerprint, current.ManifestFingerprint, StringComparison.Ordinal)
            || !string.Equals(current.Manifest.Fingerprint, current.ManifestFingerprint, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Reprint approval was blocked because the presented manifest does not match the captured print inputs.");
        }

        var approvedEvent = await store.AppendAsync(
            new PrintJobStateTransition(
                current.JobId,
                PrintJobLifecycleState.Created,
                PrintJobLifecycleState.Created,
                DateTimeOffset.UtcNow,
                string.IsNullOrWhiteSpace(reason) ? "Reprint approved" : reason.Trim(),
                PrinterName: current.PrinterName,
                SpoolJobId: null,
                QueueState: string.Empty,
                DocumentHash: current.DocumentHash,
                SceneHash: current.SceneHash,
                OutputContractHash: current.OutputContractHash,
                OperatorAction: PrintJobOperatorAction.ReprintApproved,
                RelatedJobId: current.RelatedJobId,
                Actor: NormalizeActor(actor),
                TextResourceFingerprint: current.TextResourceFingerprint,
                ManifestFingerprint: current.ManifestFingerprint,
                Manifest: current.Manifest),
            cancellationToken).ConfigureAwait(false);

        return new PrintJobOperatorActionResult(
            childJobId,
            PrintJobOperatorAction.ReprintApproved,
            true,
            approvedEvent,
            current.RelatedJobId,
            null,
            $"Reprint child {childJobId} was approved against its immutable manifest. Dispatch remains a separate explicit action.");
    }

    /// <summary>
    /// Reads the latest valid event for an explicit dispatch/approval caller.
    /// The returned event is immutable and contains no raw row payload.
    /// </summary>
    public static Task<PrintJobStateEvent> ReadCurrentEventAsync(
        PrintJobStateStore store,
        string jobId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(store);
        return ReadCurrentAsync(store, jobId, cancellationToken);
    }

    private static async Task<PrintJobOperatorActionResult> AppendDecisionAsync(
        PrintJobStateStore store,
        string jobId,
        PrintJobOperatorAction action,
        string actor,
        string reason,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(store);
        var current = await ReadCurrentAsync(store, jobId, cancellationToken).ConfigureAwait(false);
        EnsureActionable(current, jobId);

        var target = action == PrintJobOperatorAction.Void
            ? PrintJobLifecycleState.Cancelled
            : current.To;
        var stateEvent = await store.AppendAsync(
            BuildTransition(current, target, action, actor, reason, relatedJobId: string.Empty),
            cancellationToken).ConfigureAwait(false);

        var summary = action == PrintJobOperatorAction.Void
            ? $"Print job {jobId} was voided in the durable lineage. No cancellation was sent to the printer and no physical output claim was made."
            : $"Print job {jobId} was acknowledged in the durable lineage. Physical output remains unverified and automatic retry is disabled.";
        return new PrintJobOperatorActionResult(
            jobId,
            action,
            true,
            stateEvent,
            string.Empty,
            null,
            summary);
    }

    private static async Task<PrintJobStateEvent> ReadCurrentAsync(
        PrintJobStateStore store,
        string jobId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(jobId))
        {
            throw new ArgumentException("A print job identifier is required.", nameof(jobId));
        }

        var snapshot = await store.ReadRecoverySnapshotAsync(cancellationToken).ConfigureAwait(false);
        if (snapshot.StoreDiagnostics.Count > 0)
        {
            throw new InvalidOperationException(
                "The print-job event log needs repair before an operator action can be recorded.");
        }

        var current = snapshot.LatestEvents.FirstOrDefault(
            item => string.Equals(item.JobId, jobId, StringComparison.Ordinal));
        return current ?? throw new InvalidOperationException($"Print job '{jobId}' was not found in the durable event log.");
    }

    private static void EnsureActionable(PrintJobStateEvent current, string jobId)
    {
        if (PrintJobStateMachine.IsTerminal(current.To))
        {
            throw new InvalidOperationException($"Print job '{jobId}' is already terminal and cannot be changed.");
        }
    }

    private static PrintJobStateTransition BuildTransition(
        PrintJobStateEvent current,
        PrintJobLifecycleState target,
        PrintJobOperatorAction action,
        string actor,
        string reason,
        string relatedJobId)
    {
        return new PrintJobStateTransition(
            current.JobId,
            current.To,
            target,
            DateTimeOffset.UtcNow,
            string.IsNullOrWhiteSpace(reason) ? action.ToString() : reason.Trim(),
            current.PrinterName,
            current.SpoolJobId,
            current.QueueState,
            current.DocumentHash,
            current.SceneHash,
            current.OutputContractHash,
            false,
            action,
            relatedJobId,
            NormalizeActor(actor),
            current.TextResourceFingerprint,
            current.ManifestFingerprint,
            current.Manifest);
    }

    private static string NormalizeActor(string actor)
    {
        return string.IsNullOrWhiteSpace(actor) ? "operator" : actor.Trim();
    }
}
