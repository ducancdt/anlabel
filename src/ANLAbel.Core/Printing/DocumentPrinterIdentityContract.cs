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

    /// <summary>
    /// A blank saved identity is a legacy/unverified document and remains
    /// loadable. Once a driver identity has been explicitly captured, failure
    /// to read it or a changed driver behind the same queue must fail closed.
    /// </summary>
    public static DriverIdentityDecision EvaluateDriverBinding(
        string? savedDriverIdentity,
        string? currentDriverIdentity)
    {
        var saved = savedDriverIdentity?.Trim() ?? string.Empty;
        var current = currentDriverIdentity?.Trim() ?? string.Empty;
        if (saved.Length == 0)
        {
            return DriverIdentityDecision.LegacyUnverified;
        }

        if (current.Length == 0)
        {
            return DriverIdentityDecision.Blocked(
                "Printing stopped because the current printer driver identity could not be verified. Reopen Printer Setup and review the queue; no label was submitted.");
        }

        if (!string.Equals(saved, current, StringComparison.OrdinalIgnoreCase))
        {
            return DriverIdentityDecision.Blocked(
                $"Printing stopped because the saved driver '{saved}' changed to '{current}'. Reopen Printer Setup and review media/DPI; no label was submitted.");
        }

        return DriverIdentityDecision.Verified;
    }
}

public readonly record struct DriverIdentityDecision(
    bool IsAllowed,
    bool IsVerified,
    string Diagnostic)
{
    public static DriverIdentityDecision LegacyUnverified { get; } = new(true, false, string.Empty);
    public static DriverIdentityDecision Verified { get; } = new(true, true, string.Empty);

    public static DriverIdentityDecision Blocked(string diagnostic) => new(false, false, diagnostic);
}
