namespace ANLAbel.Data.DataLogs;

/// <summary>
/// One record of a data-source operation (import/refresh/relink/restore-on-open).
/// Used to trace back "which data produced this print run" after the fact —
/// see database-plan.md TC6.
/// </summary>
public sealed class DataOperationLogEntry
{
    public DateTime TimestampLocal { get; init; } = DateTime.Now;
    public string Operation { get; init; } = string.Empty;
    public string TemplateFilePath { get; init; } = string.Empty;
    public string ExcelFilePath { get; init; } = string.Empty;
    public string SheetName { get; init; } = string.Empty;
    public int RowCount { get; init; }
    public int ColumnCount { get; init; }
    public bool Success { get; init; } = true;
    public string ErrorMessage { get; init; } = string.Empty;
}
