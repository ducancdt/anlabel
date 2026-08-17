namespace ANLAbel.Core.Printing;

/// <summary>
/// Durable lifecycle states for one print operation. Queue observations are kept
/// separate from <see cref="Completed"/> because a Windows queue signal is not a
/// physical-media verification.
/// </summary>
public enum PrintJobLifecycleState
{
    Created,
    Preparing,
    PreflightPassed,
    Dispatching,
    SpoolAccepted,
    QueueObserved,
    Unknown,
    Completed,
    Failed,
    Cancelled
}

/// <summary>
/// Explicit operator decisions attached to the durable job lineage. These are
/// audit events, not dispatch commands: an acknowledgement never proves output,
/// a void never deletes history, and a reprint request never submits a new job.
/// </summary>
public enum PrintJobOperatorAction
{
    None,
    Acknowledge,
    Void,
    ReprintRequested,
    ReprintApproved
}

/// <summary>
/// Value-only command to append one lifecycle transition. The data layer assigns
/// sequence and integrity fields when it durably writes the event.
/// </summary>
public sealed record PrintJobStateTransition(
    string JobId,
    PrintJobLifecycleState From,
    PrintJobLifecycleState To,
    DateTimeOffset TimestampUtc,
    string Reason = "",
    string PrinterName = "",
    int? SpoolJobId = null,
    string QueueState = "",
    string DocumentHash = "",
    string SceneHash = "",
    string OutputContractHash = "",
    bool PhysicalOutputVerified = false,
    PrintJobOperatorAction OperatorAction = PrintJobOperatorAction.None,
    string RelatedJobId = "",
    string Actor = "",
    string TextResourceFingerprint = "",
    string ManifestFingerprint = "",
    PrintJobManifest? Manifest = null,
    PhysicalOutputVerificationEvidence? VerificationEvidence = null);

/// <summary>
/// Central transition policy. Keeping it in Core makes UI, headless dispatch and
/// recovery use the same fail-closed rules.
/// </summary>
public static class PrintJobStateMachine
{
    public static bool IsTerminal(PrintJobLifecycleState state)
    {
        return state is PrintJobLifecycleState.Completed
            or PrintJobLifecycleState.Failed
            or PrintJobLifecycleState.Cancelled;
    }

    public static bool CanTransition(
        PrintJobLifecycleState from,
        PrintJobLifecycleState to,
        bool physicalOutputVerified = false)
    {
        if (from == to)
        {
            return from == PrintJobLifecycleState.QueueObserved;
        }

        if (IsTerminal(from))
        {
            return false;
        }

        if (to == PrintJobLifecycleState.Completed && !physicalOutputVerified)
        {
            return false;
        }

        return from switch
        {
            PrintJobLifecycleState.Created => to is PrintJobLifecycleState.Preparing
                or PrintJobLifecycleState.Failed
                or PrintJobLifecycleState.Cancelled,
            PrintJobLifecycleState.Preparing => to is PrintJobLifecycleState.PreflightPassed
                or PrintJobLifecycleState.Failed
                or PrintJobLifecycleState.Cancelled,
            PrintJobLifecycleState.PreflightPassed => to is PrintJobLifecycleState.Dispatching
                or PrintJobLifecycleState.Failed
                or PrintJobLifecycleState.Cancelled,
            PrintJobLifecycleState.Dispatching => to is PrintJobLifecycleState.SpoolAccepted
                or PrintJobLifecycleState.Unknown
                or PrintJobLifecycleState.Failed
                or PrintJobLifecycleState.Cancelled
                or PrintJobLifecycleState.Completed,
            PrintJobLifecycleState.SpoolAccepted => to is PrintJobLifecycleState.QueueObserved
                or PrintJobLifecycleState.Unknown
                or PrintJobLifecycleState.Failed
                or PrintJobLifecycleState.Cancelled
                or PrintJobLifecycleState.Completed,
            PrintJobLifecycleState.QueueObserved => to is PrintJobLifecycleState.Unknown
                or PrintJobLifecycleState.Failed
                or PrintJobLifecycleState.Cancelled
                or PrintJobLifecycleState.Completed,
            PrintJobLifecycleState.Unknown => to is PrintJobLifecycleState.QueueObserved
                or PrintJobLifecycleState.Failed
                or PrintJobLifecycleState.Cancelled
                or PrintJobLifecycleState.Completed,
            _ => false
        };
    }

    public static void ValidateTransition(PrintJobStateTransition transition, PrintJobLifecycleState? currentState)
    {
        ArgumentNullException.ThrowIfNull(transition);
        if (string.IsNullOrWhiteSpace(transition.JobId))
        {
            throw new ArgumentException("A print job transition requires a stable job identifier.", nameof(transition));
        }

        if (currentState is PrintJobLifecycleState current && current != transition.From)
        {
            throw new InvalidOperationException(
                $"Job '{transition.JobId}' is currently '{current}', but the transition starts at '{transition.From}'.");
        }

        if (currentState is null && transition.From != PrintJobLifecycleState.Created)
        {
            throw new InvalidOperationException(
                $"Job '{transition.JobId}' has no durable prefix, so its first transition must start at 'Created'.");
        }

        if (!string.IsNullOrWhiteSpace(transition.Actor)
            && transition.Actor.Length > 256)
        {
            throw new ArgumentException("The print-job operator identity is too long.", nameof(transition));
        }

        if (transition.OperatorAction != PrintJobOperatorAction.None)
        {
            ValidateOperatorAction(transition);
            return;
        }

        if (transition.VerificationEvidence is not null)
        {
            if (transition.Manifest is null || !transition.Manifest.IsFingerprintValid)
            {
                throw new InvalidOperationException("Physical verification evidence requires a valid print manifest.");
            }

            var evidenceValidation = PhysicalOutputVerificationEvidence.Validate(
                transition.Manifest,
                transition.JobId,
                transition.VerificationEvidence);
            if (!evidenceValidation.IsAccepted)
            {
                throw new InvalidOperationException(evidenceValidation.Message);
            }
        }

        if (transition.PhysicalOutputVerified
            && transition.To != PrintJobLifecycleState.Completed)
        {
            throw new InvalidOperationException("Physical-output verification can only be asserted by a Completed transition.");
        }

        if (!CanTransition(transition.From, transition.To, transition.PhysicalOutputVerified))
        {
            throw new InvalidOperationException(
                $"Transition '{transition.From} -> {transition.To}' is not allowed without a valid device-evidence contract.");
        }

        if (transition.To == PrintJobLifecycleState.Completed && !transition.PhysicalOutputVerified)
        {
            throw new InvalidOperationException("Completed requires explicit physical-output verification.");
        }

        if (transition.To == PrintJobLifecycleState.Completed
            && transition.VerificationEvidence is not { IsEligibleForCompletion: true })
        {
            throw new InvalidOperationException("Completed requires accepted scanner/verifier evidence bound to the print manifest.");
        }
    }

    private static void ValidateOperatorAction(PrintJobStateTransition transition)
    {
        if (transition.PhysicalOutputVerified)
        {
            throw new InvalidOperationException("An operator action cannot be used as physical-output verification.");
        }

        switch (transition.OperatorAction)
        {
            case PrintJobOperatorAction.Acknowledge:
                if (transition.From != transition.To
                    || transition.From is not (PrintJobLifecycleState.SpoolAccepted
                        or PrintJobLifecycleState.QueueObserved
                        or PrintJobLifecycleState.Unknown))
                {
                    throw new InvalidOperationException(
                        "Acknowledge is only valid as a same-state decision for an uncertain dispatched job.");
                }
                break;

            case PrintJobOperatorAction.Void:
                if (transition.To != PrintJobLifecycleState.Cancelled
                    || IsTerminal(transition.From))
                {
                    throw new InvalidOperationException(
                        "Void must move a non-terminal job to Cancelled and cannot alter terminal history.");
                }
                break;

            case PrintJobOperatorAction.ReprintRequested:
                if (transition.From != transition.To
                    || IsTerminal(transition.From)
                    || string.IsNullOrWhiteSpace(transition.RelatedJobId))
                {
                    throw new InvalidOperationException(
                        "A reprint request is a same-state audit event and must identify its related job.");
                }
                break;

            case PrintJobOperatorAction.ReprintApproved:
                if (transition.From != PrintJobLifecycleState.Created
                    || transition.To != PrintJobLifecycleState.Created
                    || string.IsNullOrWhiteSpace(transition.RelatedJobId)
                    || string.IsNullOrWhiteSpace(transition.ManifestFingerprint)
                    || transition.Manifest is null
                    || !string.Equals(transition.Manifest.Fingerprint, transition.ManifestFingerprint, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "A reprint approval must be a same-state Created event with a matching immutable manifest and parent job.");
                }
                break;

            default:
                throw new InvalidOperationException("Unknown operator action.");
        }
    }
}
