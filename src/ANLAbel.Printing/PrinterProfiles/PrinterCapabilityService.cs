using System.Printing;
using ANLAbel.Core.Enums;
using ANLAbel.Core.Printing;

namespace ANLAbel.Printing.PrinterProfiles;

public interface IPrinterCapabilityService
{
    Task<PrinterCapabilitySnapshot> GetSnapshotAsync(
        string queueName,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Loads media capabilities only for the selected Windows queue. System.Printing
/// runs on a dedicated STA thread; Win32 PaperSizes is a conservative fallback
/// when the WPF print schema exposes no usable media rows.
/// </summary>
public sealed class PrinterCapabilityService : IPrinterCapabilityService
{
    private static readonly TimeSpan DefaultReadTimeout = TimeSpan.FromSeconds(8);
    private readonly TimeSpan _readTimeout;
    private readonly Func<string, CancellationToken, PrinterCapabilitySnapshot> _readSnapshot;

    public PrinterCapabilityService(TimeSpan? readTimeout = null)
        : this(readTimeout ?? DefaultReadTimeout, ReadSnapshot)
    {
    }

    internal PrinterCapabilityService(
        TimeSpan readTimeout,
        Func<string, CancellationToken, PrinterCapabilitySnapshot> readSnapshot)
    {
        ArgumentNullException.ThrowIfNull(readSnapshot);
        _readTimeout = readTimeout;
        _readSnapshot = readSnapshot;
        if (_readTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(readTimeout), "Capability timeout must be positive.");
        }
    }

    public async Task<PrinterCapabilitySnapshot> GetSnapshotAsync(
        string queueName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(queueName))
        {
            return PrinterCapabilitySnapshot.Unavailable(
                string.Empty,
                "Select a printer queue before loading media capabilities.");
        }

        var completion = new TaskCompletionSource<PrinterCapabilitySnapshot>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                completion.TrySetResult(_readSnapshot(queueName, cancellationToken));
            }
            catch (OperationCanceledException)
            {
                completion.TrySetCanceled(cancellationToken);
            }
            catch (Exception ex)
            {
                completion.TrySetResult(PrinterCapabilitySnapshot.Unavailable(
                    queueName,
                    $"Printer capability discovery failed: {ex.Message}"));
            }
        })
        {
            IsBackground = true,
            Name = "ANLAbel printer capability discovery"
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        try
        {
            return await completion.Task
                .WaitAsync(_readTimeout, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            // A third-party driver can block inside System.Printing or GDI. The
            // background STA is deliberately abandoned instead of holding the
            // UI/preview pipeline hostage. Its late result cannot win this task.
            return PrinterCapabilitySnapshot.Unavailable(
                queueName,
                $"Printer capability discovery timed out after {_readTimeout.TotalSeconds:0.#} seconds. Open driver properties or retry.");
        }
    }

    private static PrinterCapabilitySnapshot ReadSnapshot(
        string queueName,
        CancellationToken cancellationToken)
    {
        var diagnostics = new List<string>();
        var driverIdentity = string.Empty;
        var candidates = new List<PrinterMediaCandidate>();
        var resolutions = new List<PrinterResolutionCandidate>();
        int? effectiveDpiX = null;
        int? effectiveDpiY = null;
        PrinterPageObservation? pageObservation = null;
        PrinterQueueHealthObservation? queueHealth = null;
        PrinterCustomMediaRange? customMediaRange = null;
        var observedTicketHash = string.Empty;
        PrinterStandardFeatureObservation? standardFeatures = null;
        PrinterSchemaFeatureInventory? featureInventory = null;
        var wpfReadSucceeded = false;
        var win32ReadSucceeded = false;
        var queueAvailable = false;

        try
        {
            using var server = new LocalPrintServer();
            using var queue = server.GetPrintQueue(queueName);
            queueHealth = PrinterQueueHealthReader.Read(queue);
            queueAvailable = true;
            var driver = SafeRead(() => queue.QueueDriver);
            driverIdentity = SafeRead(() => driver?.Name)?.Trim() ?? string.Empty;
            var ticket = SafeRead(() => queue.UserPrintTicket)
                ?? SafeRead(() => queue.DefaultPrintTicket)
                ?? new PrintTicket();
            observedTicketHash = PrintTicketFingerprint.Compute(ticket);
            if (string.IsNullOrWhiteSpace(observedTicketHash))
            {
                diagnostics.Add("The current driver PrintTicket could not be serialized for fingerprinting.");
            }
            var capabilities = queue.GetPrintCapabilities(ticket);

            standardFeatures = new PrinterStandardFeatureObservation(
                ticket.PageMediaType?.ToString() ?? string.Empty,
                capabilities.PageMediaTypeCapability
                    .Select(value => value.ToString())
                    .ToArray(),
                ticket.OutputQuality?.ToString() ?? string.Empty,
                capabilities.OutputQualityCapability
                    .Select(value => value.ToString())
                    .ToArray());

            using (var capabilitiesXml = queue.GetPrintCapabilitiesAsXml(ticket))
            {
                featureInventory = PrinterSchemaFeatureInventoryParser.Parse(capabilitiesXml);
            }
            if (!string.IsNullOrWhiteSpace(featureInventory.Diagnostic))
            {
                diagnostics.Add(featureInventory.Diagnostic);
            }

            if (ticket.PageResolution is { } effectiveResolution)
            {
                effectiveDpiX = effectiveResolution.X;
                effectiveDpiY = effectiveResolution.Y;
            }

            var effectiveMedia = ticket.PageMediaSize;
            var imageableArea = capabilities.PageImageableArea;
            pageObservation = new PrinterPageObservation(
                effectiveMedia?.PageMediaSizeName?.ToString() ?? string.Empty,
                effectiveMedia?.Width,
                effectiveMedia?.Height,
                imageableArea?.OriginWidth,
                imageableArea?.OriginHeight,
                imageableArea?.ExtentWidth,
                imageableArea?.ExtentHeight);

            foreach (var media in capabilities.PageMediaSizeCapability
                         ?? Enumerable.Empty<PageMediaSize>().ToList().AsReadOnly())
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (media.Width is not double widthDip || media.Height is not double heightDip)
                {
                    continue;
                }

                var identity = media.PageMediaSizeName?.ToString() ?? string.Empty;
                candidates.Add(new PrinterMediaCandidate(
                    FriendlyName(identity, widthDip, heightDip),
                    DipToMm(widthDip),
                    DipToMm(heightDip),
                    PrinterMediaProvenance.DriverWpf,
                    identity));
            }

            foreach (var resolution in capabilities.PageResolutionCapability
                         ?? Enumerable.Empty<PageResolution>().ToList().AsReadOnly())
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (resolution.X is int dpiX && resolution.Y is int dpiY
                    && dpiX > 0 && dpiY > 0)
                {
                    resolutions.Add(new PrinterResolutionCandidate(dpiX, dpiY));
                }
            }

            wpfReadSucceeded = true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            diagnostics.Add($"WPF driver capability read failed: {ex.Message}");
        }

        // Always query the GDI/WinForms catalog. Industrial drivers sometimes
        // publish only a subset through Print Schema while exposing additional
        // installed forms through the Win32 path. MergeCandidates keeps WPF as
        // the preferred identity when both APIs report the same row.
        try
        {
            var settings = new System.Drawing.Printing.PrinterSettings
            {
                PrinterName = queueName
            };
            if (!settings.IsValid)
            {
                diagnostics.Add("Win32 reported that the selected printer queue is unavailable.");
            }
            else
            {
                queueAvailable = true;
                foreach (System.Drawing.Printing.PaperSize paper in settings.PaperSizes)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    candidates.Add(new PrinterMediaCandidate(
                        paper.PaperName,
                        HundredthInchToMm(paper.Width),
                        HundredthInchToMm(paper.Height),
                        PrinterMediaProvenance.DriverWin32,
                        paper.RawKind.ToString(System.Globalization.CultureInfo.InvariantCulture)));
                }

                customMediaRange = Win32PrinterCapabilityReader.ReadCustomMediaRange(
                    queueName,
                    out var customMediaDiagnostic);
                if (!string.IsNullOrWhiteSpace(customMediaDiagnostic))
                {
                    diagnostics.Add(customMediaDiagnostic);
                }

                win32ReadSucceeded = true;
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            diagnostics.Add($"Win32 driver media read failed: {ex.Message}");
        }

        var merged = PrinterMediaResolver.MergeCandidates(candidates);
        var mergedResolutions = resolutions
            .Where(resolution => resolution.IsValid)
            .Distinct()
            .OrderBy(resolution => resolution.DpiX)
            .ThenBy(resolution => resolution.DpiY)
            .ToArray();
        if (!queueAvailable)
        {
            return PrinterCapabilitySnapshot.Unavailable(
                queueName,
                diagnostics.Count == 0
                    ? "The selected Windows printer queue is unavailable."
                    : string.Join(" ", diagnostics));
        }

        return new PrinterCapabilitySnapshot(
            queueName,
            driverIdentity,
            DateTimeOffset.UtcNow,
            IsAvailable: true,
            // A one-source result remains useful for an exact match, but a
            // missing source must not turn a non-match into a hard mismatch.
            IsComplete: merged.Count > 0 && wpfReadSucceeded && win32ReadSucceeded,
            SupportsCustomMedia: customMediaRange?.IsValid == true,
            merged,
            string.Join(" ", diagnostics),
            mergedResolutions,
            effectiveDpiX,
            effectiveDpiY,
            pageObservation,
            queueHealth,
            customMediaRange,
            observedTicketHash,
            standardFeatures,
            featureInventory);
    }

    private static string FriendlyName(string identity, double widthDip, double heightDip) =>
        string.IsNullOrWhiteSpace(identity) || string.Equals(identity, "Unknown", StringComparison.OrdinalIgnoreCase)
            ? $"Driver media {DipToMm(widthDip):0.##} × {DipToMm(heightDip):0.##} mm"
            : identity;

    private static double DipToMm(double dip) => Math.Round(dip * 25.4 / 96.0, 2);

    private static double HundredthInchToMm(int value) => Math.Round(value * 25.4 / 100.0, 2);

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
