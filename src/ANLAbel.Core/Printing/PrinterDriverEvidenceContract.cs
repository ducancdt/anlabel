namespace ANLAbel.Core.Printing;

public enum PrinterDriverEvidenceState
{
    Unavailable,
    QueueBlocked,
    QueueHealthUnknown,
    ObservedTicketUnverified,
    EffectivePageMissing,
    ImageableAreaMissing,
    ImageableAreaInvalid,
    CapabilityCatalogPartial,
    FullyObserved
}

public sealed record PrinterDriverEvidence(
    PrinterDriverEvidenceState State,
    string Diagnostic)
{
    /// <summary>
    /// Means the read-only capability observation is internally complete. It
    /// does not authorize dispatch and is not evidence of physical output.
    /// </summary>
    public bool IsFullyObserved => State == PrinterDriverEvidenceState.FullyObserved;
}

/// <summary>
/// Summarizes read-only Windows driver evidence without replacing the
/// template-specific PrintTicket validation performed immediately before
/// dispatch.
/// </summary>
public static class PrinterDriverEvidenceContract
{
    public static PrinterDriverEvidence Evaluate(PrinterCapabilitySnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (!snapshot.IsAvailable)
        {
            return Evidence(
                PrinterDriverEvidenceState.Unavailable,
                string.IsNullOrWhiteSpace(snapshot.Diagnostic)
                    ? "The selected printer queue is unavailable."
                    : snapshot.Diagnostic);
        }

        if (snapshot.QueueHealth is null)
        {
            return Evidence(
                PrinterDriverEvidenceState.QueueHealthUnknown,
                "The queue exists, but its current health was not observed.");
        }

        if (!snapshot.QueueHealth.CanSubmit)
        {
            return Evidence(
                PrinterDriverEvidenceState.QueueBlocked,
                snapshot.QueueHealth.Diagnostic);
        }

        if (!IsSha256Fingerprint(snapshot.ObservedTicketHash))
        {
            return Evidence(
                PrinterDriverEvidenceState.ObservedTicketUnverified,
                "The current driver PrintTicket does not have a valid SHA-256 fingerprint.");
        }

        var page = snapshot.PageObservation;
        if (page is null || !page.HasEffectiveMedia)
        {
            return Evidence(
                PrinterDriverEvidenceState.EffectivePageMissing,
                "The driver did not expose a usable effective page size.");
        }

        if (!page.HasAnyImageableAreaValue)
        {
            return Evidence(
                PrinterDriverEvidenceState.ImageableAreaMissing,
                "The driver did not expose an imageable area for the effective page.");
        }

        if (!page.IsImageableAreaVerified)
        {
            return Evidence(
                PrinterDriverEvidenceState.ImageableAreaInvalid,
                "The driver exposed imageable-area values that are invalid or outside the effective page.");
        }

        if (!snapshot.IsComplete || !string.IsNullOrWhiteSpace(snapshot.Diagnostic))
        {
            return Evidence(
                PrinterDriverEvidenceState.CapabilityCatalogPartial,
                string.IsNullOrWhiteSpace(snapshot.Diagnostic)
                    ? "The effective page is usable, but the combined WPF/Win32 capability catalog is partial."
                    : $"The driver observation contains a capability warning: {snapshot.Diagnostic}");
        }

        return Evidence(
            PrinterDriverEvidenceState.FullyObserved,
            "Queue, effective page, imageable area and capability catalog were observed. This is not physical-output verification.");
    }

    private static PrinterDriverEvidence Evidence(
        PrinterDriverEvidenceState state,
        string diagnostic) => new(state, diagnostic);

    private static bool IsSha256Fingerprint(string? value) =>
        value is { Length: 64 } && value.All(character =>
            character is >= '0' and <= '9' or
            >= 'a' and <= 'f' or
            >= 'A' and <= 'F');
}
