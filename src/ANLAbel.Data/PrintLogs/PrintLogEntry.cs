namespace ANLAbel.Data.PrintLogs;

public sealed class PrintLogEntry
{
    public DateTime PrintedAt { get; init; } = DateTime.Now;
    public string TemplateName { get; init; } = string.Empty;
    public string TemplateFilePath { get; init; } = string.Empty;
    public string PrinterName { get; init; } = string.Empty;
    public double LabelWidthMm { get; init; }
    public double LabelHeightMm { get; init; }
    public int Dpi { get; init; }
    public string PrintMode { get; init; } = string.Empty;
    public int RowCount { get; init; }
    public int LabelCount { get; init; }
    public int LabelIndex { get; init; }
    public string ExcelFilePath { get; init; } = string.Empty;
    public string ExcelSheetName { get; init; } = string.Empty;
    public string PartNo { get; init; } = string.Empty;
    public string ItemName { get; init; } = string.Empty;
    public string Lot { get; init; } = string.Empty;
    public string Quantity { get; init; } = string.Empty;
    public string LabelContent { get; init; } = string.Empty;
    public string RowData { get; init; } = string.Empty;
    public string Notes { get; init; } = string.Empty;
}
