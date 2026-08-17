using ANLAbel.Core.Printing;

namespace ANLAbel.Printing.PrinterProfiles;

/// <summary>
/// Evidence-based outcome of a print submission. Windows accepting a document into
/// the spooler does not prove that a physical label was printed.
/// </summary>
public enum PrintJobOutcome
{
    Cancelled,
    SpoolAccepted,
    DeviceAcknowledged,
    Completed,
    Failed,
    Unknown
}

public sealed record PrintJobResult(
    PrintJobOutcome Outcome,
    string PrinterName,
    string Description,
    int LabelCount,
    string ErrorMessage = "",
    int DpiX = 0,
    int DpiY = 0,
    bool PrintableAreaVerified = false,
    int? SpoolJobId = null,
    string OutputContractHash = "",
    bool OutputContractTicketVerified = false,
    string DocumentHash = "",
    string SceneHash = "",
    bool SceneCompilationVerified = false,
    string TextResourceFingerprint = "",
    string ImageRasterFingerprint = "",
    string ManifestFingerprint = "",
    PrintJobManifest? Manifest = null)
{
    /// <summary>
    /// Optional pre-dispatch queue snapshot used to resolve a driver-published
    /// job identifier after <c>PrintDocument</c> returns. It is not part of the
    /// user-facing manifest and never authorizes physical completion.
    /// </summary>
    public SpoolJobSubmissionEvidence? SubmissionEvidence { get; init; }

    /// <summary>
    /// Thermal golden evidence carried by the prepared plan, when one was
    /// explicitly approved. This is metadata evidence only; physical output
    /// still requires scanner/verifier confirmation.
    /// </summary>
    public string ThermalRasterGoldenFingerprint { get; init; } = string.Empty;

    /// <summary>
    /// Redacted support-evidence fingerprint produced on the shipped print
    /// path.  Empty when preparation never reached a durable job identity.
    /// Never contains raw label field values.
    /// </summary>
    public string SupportEvidenceFingerprint { get; init; } = string.Empty;

    /// <summary>
    /// Canonical redacted support JSON for operator export.  Payloads and
    /// secrets are redacted by <see cref="PrintSupportEvidenceContract"/>.
    /// </summary>
    public string SupportEvidenceJson { get; init; } = string.Empty;

    public bool IsAccepted => Outcome is PrintJobOutcome.SpoolAccepted
        or PrintJobOutcome.DeviceAcknowledged
        or PrintJobOutcome.Completed;

    /// <summary>
    /// True only when the print queue exposed a positive job identifier that can
    /// be used for bounded status polling.  A successful PrintDocument return
    /// without this identity is still an accepted submission, but it cannot be
    /// correlated to a queue row safely.
    /// </summary>
    public bool HasSpoolIdentity => SpoolJobId is int jobId && jobId > 0;

    public bool IsPhysicalCompletionVerified => Outcome == PrintJobOutcome.Completed;

    public string UserFacingStatus => Outcome switch
    {
        PrintJobOutcome.Cancelled => "Print cancelled.",
        PrintJobOutcome.SpoolAccepted => AppendSpoolEvidence("Print job accepted by the Windows spooler; physical completion is not verified."),
        PrintJobOutcome.DeviceAcknowledged => AppendSpoolEvidence("Printer acknowledged the job; physical completion is not independently verified."),
        PrintJobOutcome.Completed => "Print completed with device confirmation.",
        PrintJobOutcome.Failed => string.IsNullOrWhiteSpace(ErrorMessage) ? "Print failed." : $"Print failed: {ErrorMessage}",
        _ => string.IsNullOrWhiteSpace(ErrorMessage) ? "Print outcome is unknown; do not retry automatically." : $"Print outcome is unknown: {ErrorMessage}"
    };

    private string AppendSpoolEvidence(string message)
    {
        return HasSpoolIdentity
            ? $"{message} Spool job #{SpoolJobId}."
            : $"{message} No spool job identity was captured; queue status cannot be correlated safely. Do not retry automatically until the queue/device is reconciled.";
    }
}
