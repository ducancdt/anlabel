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

    /// <summary>Plain Vietnamese status for the operator (plan 13 H25).</summary>
    public string UserFacingStatus => Outcome switch
    {
        PrintJobOutcome.Cancelled => "Đã hủy in.",
        PrintJobOutcome.SpoolAccepted => AppendSpoolEvidence("Hàng đợi Windows đã nhận lệnh in; chưa xác minh tem đã ra."),
        PrintJobOutcome.DeviceAcknowledged => AppendSpoolEvidence("Máy in đã xác nhận nhận lệnh; chưa xác minh độc lập tem đã ra."),
        PrintJobOutcome.Completed => "Đã in xong, có xác nhận từ thiết bị.",
        PrintJobOutcome.Failed => string.IsNullOrWhiteSpace(ErrorMessage) ? "In thất bại." : $"In thất bại: {ErrorMessage}",
        _ => string.IsNullOrWhiteSpace(ErrorMessage) ? "Chưa rõ kết quả in; không tự in lại." : $"Chưa rõ kết quả in: {ErrorMessage}"
    };

    private string AppendSpoolEvidence(string message)
    {
        return HasSpoolIdentity
            ? $"{message} Mã lệnh hàng đợi #{SpoolJobId}."
            : $"{message} Không ghi nhận được mã lệnh hàng đợi nên không đối chiếu được trạng thái. Không tự in lại cho tới khi đã đối soát hàng đợi và máy in.";
    }
}
