using System.Printing;

namespace ANLAbel.Printing.PrinterProfiles;

/// <summary>
/// Lists Windows printers without attempting to read paper sizes from drivers.
/// Paper sizes come from <see cref="StandardLabelSizes"/> catalog instead.
/// </summary>
public sealed class PrinterDiscoveryService
{
    public IReadOnlyList<PrinterInfo> GetInstalledPrinters()
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

            return queues
                .Select(queue => CreatePrinterInfo(queue, defaultName))
                .OrderByDescending(printer => printer.IsDefault)
                .ThenBy(printer => printer.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToArray();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PrinterDiscovery] Failed to enumerate printers: {ex.Message}");
            return Array.Empty<PrinterInfo>();
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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PrinterDiscovery] SafeRead failed: {ex.Message}");
            return default;
        }
    }
}