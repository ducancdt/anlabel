namespace ANLAbel.Core.Printing;

/// <summary>
/// Immutable context retained after dispatch so a delayed spooler publication
/// can be correlated off the UI thread.  The pre-dispatch snapshot is evidence,
/// not a printer handle; an unavailable snapshot deliberately disables lookup.
/// </summary>
public sealed record SpoolJobSubmissionEvidence(
    string PrinterName,
    string Description,
    IReadOnlyList<SpoolJobIdentityCandidate> PreDispatchJobs,
    DateTimeOffset CapturedAtUtc)
{
    public bool IsValid => !string.IsNullOrWhiteSpace(PrinterName)
        && PreDispatchJobs is not null
        && PreDispatchJobs.All(candidate => candidate is not null && candidate.JobId > 0);
}
