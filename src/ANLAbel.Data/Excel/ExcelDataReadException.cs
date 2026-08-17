namespace ANLAbel.Data.Excel;

public enum ExcelDataReadError
{
    MissingFile,
    InvalidWorkbook,
    InvalidData,
    MissingSheet,
    InvalidHeaderRow
}

/// <summary>
/// A user-actionable Excel read failure. The UI can display <see cref="Exception.Message"/>
/// while tests and future workflows can branch on <see cref="Error"/>.
/// </summary>
public sealed class ExcelDataReadException : Exception
{
    public ExcelDataReadException(
        ExcelDataReadError error,
        string message,
        string filePath,
        string? sheetName = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Error = error;
        FilePath = filePath;
        SheetName = sheetName;
    }

    public ExcelDataReadError Error { get; }

    public string FilePath { get; }

    public string? SheetName { get; }
}
