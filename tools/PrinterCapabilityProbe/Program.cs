using System.Text.Json;
using System.Text.Json.Serialization;
using ANLAbel.Printing.PrinterProfiles;

var discovery = new PrinterDiscoveryService().DiscoverInstalledPrinters();
if (!discovery.IsSuccess)
{
    Console.Error.WriteLine(discovery.ErrorMessage);
    return 2;
}

var requestedNames = args
    .Where(argument => !string.Equals(argument, "--all", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(argument, "--require-fully-observed", StringComparison.OrdinalIgnoreCase))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();
var probeAll = args.Any(argument => string.Equals(argument, "--all", StringComparison.OrdinalIgnoreCase));
var requireFullyObserved = args.Any(argument =>
    string.Equals(argument, "--require-fully-observed", StringComparison.OrdinalIgnoreCase));

if (!probeAll && requestedNames.Length == 0)
{
    Console.WriteLine("Installed Windows printer queues:");
    foreach (var printer in discovery.Printers)
    {
        Console.WriteLine($"- {printer.Name} | driver={printer.DriverName} | default={printer.IsDefault}");
    }

    Console.WriteLine();
    Console.WriteLine("Read-only capability probe:");
    Console.WriteLine("  dotnet run --project tools/PrinterCapabilityProbe -- \"Queue name\"");
    Console.WriteLine("  dotnet run --project tools/PrinterCapabilityProbe -- --all");
    Console.WriteLine("  dotnet run --project tools/PrinterCapabilityProbe -- --all --require-fully-observed");
    Console.WriteLine("Strict mode returns exit code 3 when a queue is available but driver evidence is not fully observed.");
    return 0;
}

var queueNames = probeAll
    ? discovery.Printers.Select(printer => printer.Name).ToArray()
    : requestedNames;
var installedNames = discovery.Printers
    .Select(printer => printer.Name)
    .ToHashSet(StringComparer.OrdinalIgnoreCase);
var service = new PrinterCapabilityService();
var results = new List<object>();
var hasUnavailableQueue = false;
var hasUnverifiedEvidence = false;

foreach (var queueName in queueNames)
{
    if (!installedNames.Contains(queueName))
    {
        hasUnavailableQueue = true;
        results.Add(new
        {
            QueueName = queueName,
            IsAvailable = false,
            Diagnostic = "The requested queue is not installed."
        });
        continue;
    }

    var snapshot = await service.GetSnapshotAsync(queueName);
    var evidence = ANLAbel.Core.Printing.PrinterDriverEvidenceContract.Evaluate(snapshot);
    hasUnavailableQueue |= !snapshot.IsAvailable;
    hasUnverifiedEvidence |= !evidence.IsFullyObserved;
    results.Add(new
    {
        snapshot.QueueName,
        snapshot.DriverIdentity,
        snapshot.CapturedAt,
        snapshot.IsAvailable,
        snapshot.IsComplete,
        snapshot.SupportsCustomMedia,
        snapshot.CustomMediaRange,
        snapshot.ObservedTicketHash,
        snapshot.StandardFeatures,
        snapshot.FeatureInventory,
        snapshot.Diagnostic,
        snapshot.EffectiveDpiX,
        snapshot.EffectiveDpiY,
        snapshot.PageObservation,
        snapshot.QueueHealth,
        DriverEvidence = evidence,
        Resolutions = snapshot.AvailableResolutions,
        snapshot.Media
    });
}

Console.WriteLine(JsonSerializer.Serialize(results, new JsonSerializerOptions
{
    WriteIndented = true,
    Converters = { new JsonStringEnumConverter() }
}));
return hasUnavailableQueue
    ? 2
    : requireFullyObserved && hasUnverifiedEvidence
        ? 3
        : 0;
