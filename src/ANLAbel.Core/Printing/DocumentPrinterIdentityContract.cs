namespace ANLAbel.Core.Printing;

/// <summary>
/// Document printer identity. Last-used preferences are not a queue and must
/// not be written onto a template that has no named printer.
/// </summary>
public static class DocumentPrinterIdentityContract
{
    public static string? QueueNameFromDocument(string? documentPrinterName)
    {
        return string.IsNullOrWhiteSpace(documentPrinterName)
            ? null
            : documentPrinterName.Trim();
    }

    public static string? QueueNameFromDocument(
        string? documentPrinterName,
        string? preferencePrinterName)
    {
        _ = preferencePrinterName;
        return QueueNameFromDocument(documentPrinterName);
    }

    public static string? PaperNameFromDocumentOrHint(string? documentPaperName, string? hintPaperName)
    {
        if (!string.IsNullOrWhiteSpace(documentPaperName))
        {
            return documentPaperName.Trim();
        }

        return string.IsNullOrWhiteSpace(hintPaperName) ? null : hintPaperName.Trim();
    }
}
