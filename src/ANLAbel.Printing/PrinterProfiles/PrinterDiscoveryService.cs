using System.Printing;

namespace ANLAbel.Printing.PrinterProfiles;

/// <summary>
/// Read-only Windows queue enumeration result. An empty row set and an
/// enumeration failure are intentionally different operator states.
/// </summary>
public sealed record PrinterDiscoveryResult(
    IReadOnlyList<PrinterInfo> Printers,
    string ErrorMessage = "")
{
    public bool IsSuccess => string.IsNullOrWhiteSpace(ErrorMessage);
}

/// <summary>
/// Lists Windows printers without attempting to read paper sizes from drivers.
/// Paper sizes come from <see cref="StandardLabelSizes"/> catalog instead.
/// </summary>
public sealed class PrinterDiscoveryService
{
    public IReadOnlyList<PrinterInfo> GetInstalledPrinters()
    {
        return DiscoverInstalledPrinters().Printers;
    }

    public PrinterDiscoveryResult DiscoverInstalledPrinters()
    {
        try
        {
            using var server = new LocalPrintServer();
            var defaultName = LocalPrintServer.GetDefaultPrintQueue()?.FullName ?? string.Empty;
            var queues = server.GetPrintQueues(new[]
            {
                EnumeratedPrintQueueTypes.Local,
                EnumeratedPrintQueueTypes.Connections
            });

            var printers = queues
                .Select(queue => CreatePrinterInfo(queue, defaultName))
                .OrderByDescending(printer => printer.IsDefault)
                .ThenBy(printer => printer.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToArray();
            return new PrinterDiscoveryResult(printers);
        }
        catch (Exception ex)
        {
            return new PrinterDiscoveryResult(
                Array.Empty<PrinterInfo>(),
                $"Windows printer enumeration failed: {ex.Message}");
        }
    }

    private static PrinterInfo CreatePrinterInfo(PrintQueue queue, string defaultName)
    {
        var driverName = SafeRead(() => queue.QueueDriver?.Name) ?? string.Empty;
        var name = queue.FullName;

        return new PrinterInfo
        {
            Name = name,
            DriverName = driverName,
            IsDefault = string.Equals(name, defaultName, StringComparison.OrdinalIgnoreCase),
            PaperSizes = StandardLabelSizes.All
        };
    }

    private static T? SafeRead<T>(Func<T?> read)
    {
        try
        {
            return read();
        }
        catch
        {
            return default;
        }
    }
}
