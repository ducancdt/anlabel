namespace ANLAbel.Data.PrintLogs;

/// <summary>
/// One job-level record of a print operation (as opposed to <see cref="PrintLogEntry"/>,
/// which logs one row per printed label to the human-facing print-history.xlsx). This is
/// a machine-parseable trace for support/audit — see print-preview-reliability-plan.md
/// item 3 ("chuẩn hoá print log").
/// </summary>
public sealed class PrintOperationLogEntry
{
    public DateTime TimestampLocal { get; init; } = DateTime.Now;
    public string TemplateName { get; init; } = string.Empty;
    public string TemplateFilePath { get; init; } = string.Empty;
    public string PrinterName { get; init; } = string.Empty;
    public double LabelWidthMm { get; init; }
    public double LabelHeightMm { get; init; }
    public int Dpi { get; init; }
    public string PrintMode { get; init; } = string.Empty;
    public int RowsSelected { get; init; }
    public int LabelsPrinted { get; init; }
    public bool Success { get; init; } = true;
    public string ErrorMessage { get; init; } = string.Empty;
}
