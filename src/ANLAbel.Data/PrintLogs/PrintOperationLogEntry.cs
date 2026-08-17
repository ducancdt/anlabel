using ANLAbel.Core.Printing;

namespace ANLAbel.Data.PrintLogs;

/// <summary>
/// One job-level record of a print operation (as opposed to <see cref="PrintLogEntry"/>,
/// which logs one row per printed label to the human-facing print-history.xlsx). This is
/// a machine-parseable trace for support/audit — see print-preview-reliability-plan.md
/// item 3 ("chuẩn hoá print log").
/// </summary>
public sealed class PrintOperationLogEntry
{
    public string JobId { get; init; } = string.Empty;
    public DateTime TimestampLocal { get; init; } = DateTime.Now;
    public string TemplateName { get; init; } = string.Empty;
    public string TemplateFilePath { get; init; } = string.Empty;
    public string PrinterName { get; init; } = string.Empty;
    public double LabelWidthMm { get; init; }
    public double LabelHeightMm { get; init; }
    public int Dpi { get; init; }
    public int DpiX { get; init; }
    public int DpiY { get; init; }
    public string PrintMode { get; init; } = string.Empty;
    public string PrintMethod { get; init; } = "ApplicationGraphic";
    public bool NativeCommandsUsed { get; init; }
    public string Outcome { get; init; } = string.Empty;
    public string OutcomeEvidence { get; init; } = string.Empty;
    public int? SpoolJobId { get; init; }
    public string SpoolState { get; init; } = string.Empty;
    public string SpoolStatusMessage { get; init; } = string.Empty;
    public int SpoolStatusPollCount { get; init; }
    public bool SpoolStatusTimedOut { get; init; }
    public DateTimeOffset? SpoolStatusObservedAtUtc { get; init; }
    public string OutputContractHash { get; init; } = string.Empty;
    public bool OutputContractTicketVerified { get; init; }
    public string DocumentHash { get; init; } = string.Empty;
    public string TextResourceFingerprint { get; init; } = string.Empty;
    public string ImageRasterFingerprint { get; init; } = string.Empty;
    public string ThermalRasterGoldenFingerprint { get; init; } = string.Empty;
    public string ManifestFingerprint { get; init; } = string.Empty;
    public PrintJobManifest? Manifest { get; init; }
    /// <summary>
    /// SHA-256 of the redacted support-evidence bundle produced on the shipped
    /// print path. Empty when preparation never reached support attachment.
    /// Never stores raw label payloads.
    /// </summary>
    public string SupportEvidenceFingerprint { get; init; } = string.Empty;
    public string SceneHash { get; init; } = string.Empty;
    public bool SceneCompilationVerified { get; init; }
    public string OperatorAction { get; init; } = string.Empty;
    public string RelatedJobId { get; init; } = string.Empty;
    public string OperatorActor { get; init; } = string.Empty;
    public int RowsSelected { get; init; }
    public int LabelsPrinted { get; init; }
    public bool Success { get; init; } = true;
    public string ErrorMessage { get; init; } = string.Empty;
}
