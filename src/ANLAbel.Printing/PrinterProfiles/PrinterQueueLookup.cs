using System.Printing;

namespace ANLAbel.Printing.PrinterProfiles;

/// <summary>
/// Evidence returned while resolving a named Windows printer queue. Keeping the
/// lookup contract separate from <see cref="PrintService"/> lets tests model a
/// queue disappearing without touching a real spooler, and gives the UI a stable
/// reason instead of silently falling back to another queue.
/// </summary>
public sealed record PrinterQueueLookupResult(
    string RequestedName,
    bool IsAvailable,
    string CanonicalName,
    string ErrorMessage)
{
    public static PrinterQueueLookupResult Available(string requestedName, string canonicalName)
    {
        return new PrinterQueueLookupResult(
            requestedName,
            true,
            canonicalName,
            string.Empty);
    }

    public static PrinterQueueLookupResult Missing(string requestedName, string errorMessage)
    {
        return new PrinterQueueLookupResult(
            requestedName,
            false,
            string.Empty,
            string.IsNullOrWhiteSpace(errorMessage)
                ? "The requested Windows printer queue is unavailable."
                : errorMessage);
    }
}

public interface IPrinterQueueLookup
{
    PrinterQueueLookupResult Resolve(string printerName);
}

/// <summary>
/// Read-only Windows queue lookup used by production print paths. It does not
/// select the default queue when a non-empty requested name is missing.
/// </summary>
public sealed class WindowsPrinterQueueLookup : IPrinterQueueLookup
{
    public PrinterQueueLookupResult Resolve(string printerName)
    {
        if (string.IsNullOrWhiteSpace(printerName))
        {
            return PrinterQueueLookupResult.Missing(
                printerName ?? string.Empty,
                "No printer queue was selected.");
        }

        try
        {
            using var server = new LocalPrintServer();
            using var queue = server.GetPrintQueue(printerName);
            var canonicalName = queue.FullName;
            if (string.IsNullOrWhiteSpace(canonicalName)
                || !string.Equals(canonicalName, printerName, StringComparison.OrdinalIgnoreCase))
            {
                return PrinterQueueLookupResult.Missing(
                    printerName,
                    $"Windows resolved the requested queue to '{canonicalName}', which does not match the saved queue.");
            }

            return PrinterQueueLookupResult.Available(printerName, canonicalName);
        }
        catch (Exception ex)
        {
            return PrinterQueueLookupResult.Missing(
                printerName,
                $"Windows could not open the saved queue: {ex.Message}");
        }
    }
}
