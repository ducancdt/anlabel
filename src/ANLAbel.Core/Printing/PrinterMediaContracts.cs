using ANLAbel.Core.Enums;

namespace ANLAbel.Core.Printing;

public sealed record PrinterMediaCandidate(
    string Name,
    double WidthMm,
    double HeightMm,
    PrinterMediaProvenance Provenance,
    string RawIdentity = "")
{
    public bool HasValidDimensions =>
        double.IsFinite(WidthMm) && WidthMm > 0 &&
        double.IsFinite(HeightMm) && HeightMm > 0;

    /// <summary>
    /// True only when the media originates from a driver-advertised source
    /// (WPF/Win32). A software stock preset (VendorStock/BuiltInStock/UserCustom)
    /// is never proof that the queue can accept the physical page size, so it must
    /// never authorize dispatch on its own.
    /// </summary>
    public bool IsDriverAdvertised =>
        Provenance is PrinterMediaProvenance.DriverWpf or PrinterMediaProvenance.DriverWin32;
}

public sealed record PrinterCapabilitySnapshot(
    string QueueName,
    string DriverIdentity,
    DateTimeOffset CapturedAt,
    bool IsAvailable,
    bool IsComplete,
    bool SupportsCustomMedia,
    IReadOnlyList<PrinterMediaCandidate> Media,
    string Diagnostic = "",
    IReadOnlyList<PrinterResolutionCandidate>? Resolutions = null,
    int? EffectiveDpiX = null,
    int? EffectiveDpiY = null,
    PrinterPageObservation? PageObservation = null,
    PrinterQueueHealthObservation? QueueHealth = null,
    PrinterCustomMediaRange? CustomMediaRange = null,
    string ObservedTicketHash = "",
    PrinterStandardFeatureObservation? StandardFeatures = null,
    PrinterSchemaFeatureInventory? FeatureInventory = null)
{
    public IReadOnlyList<PrinterResolutionCandidate> AvailableResolutions =>
        Resolutions ?? Array.Empty<PrinterResolutionCandidate>();

    public static PrinterCapabilitySnapshot Unavailable(string queueName, string diagnostic) =>
        new(
            queueName,
            string.Empty,
            DateTimeOffset.UtcNow,
            IsAvailable: false,
            IsComplete: false,
            SupportsCustomMedia: false,
            Array.Empty<PrinterMediaCandidate>(),
            diagnostic);
}

public sealed record PrinterStandardFeatureObservation(
    string EffectiveMediaType,
    IReadOnlyList<string> MediaTypes,
    string EffectiveOutputQuality,
    IReadOnlyList<string> OutputQualities)
{
    public IReadOnlyList<string> AvailableMediaTypes { get; } = Normalize(MediaTypes);
    public IReadOnlyList<string> AvailableOutputQualities { get; } = Normalize(OutputQualities);
    public bool IsEffectiveMediaTypeAdvertised =>
        IsBlankOrAdvertised(EffectiveMediaType, AvailableMediaTypes);
    public bool IsEffectiveOutputQualityAdvertised =>
        IsBlankOrAdvertised(EffectiveOutputQuality, AvailableOutputQualities);

    private static IReadOnlyList<string> Normalize(IEnumerable<string>? values) =>
        (values ?? Array.Empty<string>())
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .Select(value => value.Trim())
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
        .ToArray();

    private static bool IsBlankOrAdvertised(string value, IReadOnlyList<string> advertised) =>
        string.IsNullOrWhiteSpace(value) || advertised.Contains(value.Trim(), StringComparer.OrdinalIgnoreCase);
}

public sealed record PrinterPageObservation(
    string EffectiveMediaName,
    double? MediaWidthDip,
    double? MediaHeightDip,
    double? ImageableOriginXDip,
    double? ImageableOriginYDip,
    double? ImageableWidthDip,
    double? ImageableHeightDip)
{
    private const double MmPerDip = 25.4 / 96.0;

    public double? MediaWidthMm => ToMm(MediaWidthDip);
    public double? MediaHeightMm => ToMm(MediaHeightDip);
    public double? ImageableOriginXMm => ToMm(ImageableOriginXDip);
    public double? ImageableOriginYMm => ToMm(ImageableOriginYDip);
    public double? ImageableWidthMm => ToMm(ImageableWidthDip);
    public double? ImageableHeightMm => ToMm(ImageableHeightDip);

    public bool HasEffectiveMedia => IsPositive(MediaWidthDip) && IsPositive(MediaHeightDip);

    public bool HasImageableArea =>
        IsFiniteNonNegative(ImageableOriginXDip) &&
        IsFiniteNonNegative(ImageableOriginYDip) &&
        IsPositive(ImageableWidthDip) &&
        IsPositive(ImageableHeightDip);

    public bool HasAnyImageableAreaValue =>
        ImageableOriginXDip.HasValue ||
        ImageableOriginYDip.HasValue ||
        ImageableWidthDip.HasValue ||
        ImageableHeightDip.HasValue;

    public bool IsImageableAreaVerified =>
        HasImageableArea &&
        PrintableAreaContract.Validate(
            ImageableOriginXDip!.Value,
            ImageableOriginYDip!.Value,
            ImageableWidthDip!.Value,
            ImageableHeightDip!.Value,
            MediaWidthDip,
            MediaHeightDip).HasUsableGeometry;

    private static double? ToMm(double? dip) =>
        dip is double value && double.IsFinite(value)
            ? Math.Round(value * MmPerDip, 2)
            : null;

    private static bool IsPositive(double? value) =>
        value is double number && double.IsFinite(number) && number > 0;

    private static bool IsFiniteNonNegative(double? value) =>
        value is double number && double.IsFinite(number) && number >= 0;
}

public sealed record PrinterResolutionCandidate(int DpiX, int DpiY)
{
    public bool IsValid => DpiX > 0 && DpiY > 0;
    public bool IsSquare => IsValid && DpiX == DpiY;
}

public sealed record PrinterCustomMediaRange(
    double MinimumWidthMm,
    double MinimumHeightMm,
    double MaximumWidthMm,
    double MaximumHeightMm)
{
    private const double BoundaryToleranceMm = 0.1;

    public bool IsValid =>
        IsPositive(MinimumWidthMm) &&
        IsPositive(MinimumHeightMm) &&
        IsPositive(MaximumWidthMm) &&
        IsPositive(MaximumHeightMm) &&
        MaximumWidthMm >= MinimumWidthMm &&
        MaximumHeightMm >= MinimumHeightMm &&
        (MaximumWidthMm - MinimumWidthMm >= BoundaryToleranceMm ||
         MaximumHeightMm - MinimumHeightMm >= BoundaryToleranceMm);

    public bool Contains(double widthMm, double heightMm) =>
        IsValid &&
        double.IsFinite(widthMm) &&
        double.IsFinite(heightMm) &&
        widthMm >= MinimumWidthMm - BoundaryToleranceMm &&
        heightMm >= MinimumHeightMm - BoundaryToleranceMm &&
        widthMm <= MaximumWidthMm + BoundaryToleranceMm &&
        heightMm <= MaximumHeightMm + BoundaryToleranceMm;

    public static PrinterCustomMediaRange FromTenthsOfMillimeter(
        int minimumWidth,
        int minimumHeight,
        int maximumWidth,
        int maximumHeight) =>
        new(
            minimumWidth / 10.0,
            minimumHeight / 10.0,
            maximumWidth / 10.0,
            maximumHeight / 10.0);

    private static bool IsPositive(double value) => double.IsFinite(value) && value > 0;
}

public static class PrinterResolutionCatalog
{
    /// <summary>
    /// Produces scalar choices for the current UI without claiming that a
    /// non-square driver mode is square. The authored DPI remains selectable so
    /// changing queues cannot silently rewrite a saved document value.
    /// </summary>
    public static IReadOnlyList<int> BuildSquareDpiChoices(
        IEnumerable<PrinterResolutionCandidate> candidates,
        int authoredDpi)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        return candidates
            .Where(candidate => candidate.IsSquare)
            .Select(candidate => candidate.DpiX)
            .Append(authoredDpi)
            .Where(dpi => dpi > 0)
            .Distinct()
            .OrderBy(dpi => dpi)
            .ToArray();
    }
}

public sealed record PrinterMediaRequest(
    PaperSizeSource PageSizeSource,
    double LabelWidthMm,
    double LabelHeightMm,
    double RequestedMediaWidthMm = 0,
    double RequestedMediaHeightMm = 0,
    string RequestedMediaName = "",
    PrinterSettingsSource SettingsSource = PrinterSettingsSource.Label);

public enum PrinterMediaResolutionStatus
{
    Compatible,
    CustomSupported,
    DriverAutomatic,
    SoftwareStockOnly,
    QueueBlocked,
    Mismatch,
    Unknown,
    Unavailable
}

public sealed record PrinterMediaResolution(
    PrinterMediaResolutionStatus Status,
    PrinterMediaCandidate? MatchedMedia,
    string Message)
{
    public bool CanDispatch => Status is
        PrinterMediaResolutionStatus.Compatible or
        PrinterMediaResolutionStatus.CustomSupported or
        PrinterMediaResolutionStatus.DriverAutomatic;
}

/// <summary>
/// Pure policy for comparing an authored label/media request with a fresh
/// printer capability snapshot. It never changes document geometry and never
/// treats an orientation-swapped size as an exact match.
/// </summary>
public static class PrinterMediaResolver
{
    public const double DimensionToleranceMm = 0.5;

    public static PrinterMediaResolution Resolve(
        PrinterCapabilitySnapshot snapshot,
        PrinterMediaRequest request)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(request);

        if (!snapshot.IsAvailable)
        {
            return new PrinterMediaResolution(
                PrinterMediaResolutionStatus.Unavailable,
                null,
                string.IsNullOrWhiteSpace(snapshot.Diagnostic)
                    ? "The selected printer queue is unavailable."
                    : snapshot.Diagnostic);
        }

        if (snapshot.QueueHealth is { CanSubmit: false } health)
        {
            return new PrinterMediaResolution(
                PrinterMediaResolutionStatus.QueueBlocked,
                null,
                health.Diagnostic);
        }

        var candidates = snapshot.Media
            .Where(candidate => candidate.HasValidDimensions)
            .ToArray();

        if (request.SettingsSource == PrinterSettingsSource.Driver)
        {
            if (snapshot.PageObservation is { HasEffectiveMedia: true } page)
            {
                var pageW = page.MediaWidthMm!.Value;
                var pageH = page.MediaHeightMm!.Value;
                if (!LabelStockContract.MatchesAuthoredLabel(pageW, pageH, request.LabelWidthMm, request.LabelHeightMm))
                {
                    return new PrinterMediaResolution(
                        PrinterMediaResolutionStatus.Mismatch,
                        null,
                        $"Current driver preferences own the print ticket, but driver configured page {pageW:0.##} × {pageH:0.##} mm does not match authored label {request.LabelWidthMm:0.##} × {request.LabelHeightMm:0.##} mm. Select 'Saved with label' to override media or configure driver preferences.");
                }

                return new PrinterMediaResolution(
                    PrinterMediaResolutionStatus.DriverAutomatic,
                    FindNamedCandidate(candidates, page.EffectiveMediaName) ?? candidates.FirstOrDefault(c => DimensionsMatch(c, pageW, pageH)),
                    $"Current driver preferences own the print ticket (effective page {pageW:0.##} × {pageH:0.##} mm matches authored label). Manual media override is inactive.");
            }

            return new PrinterMediaResolution(
                PrinterMediaResolutionStatus.DriverAutomatic,
                null,
                "Current driver preferences own the print ticket. Physical page sizing will be resolved by the printer driver at dispatch time. Manual media selection is inactive.");
        }

        if (request.PageSizeSource == PaperSizeSource.DriverAutomatic)
        {
            return new PrinterMediaResolution(
                PrinterMediaResolutionStatus.DriverAutomatic,
                FindNamedCandidate(candidates, request.RequestedMediaName),
                "Physical page sizing will be resolved by the printer driver at dispatch time.");
        }

        var width = request.RequestedMediaWidthMm > 0
            ? request.RequestedMediaWidthMm
            : request.LabelWidthMm;
        var height = request.RequestedMediaHeightMm > 0
            ? request.RequestedMediaHeightMm
            : request.LabelHeightMm;

        if (!IsValidDimension(width) || !IsValidDimension(height))
        {
            return new PrinterMediaResolution(
                PrinterMediaResolutionStatus.Unknown,
                null,
                "The requested physical media dimensions are invalid.");
        }

        if (!LabelStockContract.ContainsAuthoredLabel(
                width,
                height,
                request.LabelWidthMm,
                request.LabelHeightMm))
        {
            return new PrinterMediaResolution(
                PrinterMediaResolutionStatus.Mismatch,
                null,
                $"Physical media {width:0.##} × {height:0.##} mm does not contain the authored label "
                + $"{request.LabelWidthMm:0.##} × {request.LabelHeightMm:0.##} mm. "
                + "Use driver-automatic page size for labels on a roll, or choose media large enough to hold the label.");
        }

        var named = FindNamedCandidate(candidates, request.RequestedMediaName);
        if (named is not null && DimensionsMatch(named, width, height))
        {
            return Classify(named);
        }

        var exact = candidates.FirstOrDefault(candidate => DimensionsMatch(candidate, width, height));
        if (exact is not null)
        {
            return Classify(exact);
        }

        if (snapshot.SupportsCustomMedia && snapshot.CustomMediaRange?.Contains(width, height) == true)
        {
            return new PrinterMediaResolution(
                PrinterMediaResolutionStatus.CustomSupported,
                null,
                "The requested physical size is within the custom-media range reported by the driver.");
        }

        if (snapshot.SupportsCustomMedia)
        {
            return new PrinterMediaResolution(
                PrinterMediaResolutionStatus.Mismatch,
                null,
                snapshot.CustomMediaRange is null
                    ? "The driver claims custom-media support without usable size limits."
                    : $"The requested physical size is outside the driver custom-media range "
                      + $"{snapshot.CustomMediaRange.MinimumWidthMm:0.##} × {snapshot.CustomMediaRange.MinimumHeightMm:0.##} to "
                      + $"{snapshot.CustomMediaRange.MaximumWidthMm:0.##} × {snapshot.CustomMediaRange.MaximumHeightMm:0.##} mm.");
        }

        if (!snapshot.IsComplete || candidates.Length == 0)
        {
            return new PrinterMediaResolution(
                PrinterMediaResolutionStatus.Unknown,
                null,
                "Printer media capabilities are incomplete; compatibility must be revalidated before dispatch.");
        }

        return new PrinterMediaResolution(
            PrinterMediaResolutionStatus.Mismatch,
            null,
            "No same-orientation driver media matches the requested physical size.");
    }

    public static IReadOnlyList<PrinterMediaCandidate> MergeCandidates(
        IEnumerable<PrinterMediaCandidate> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        return candidates
            .Where(candidate => candidate.HasValidDimensions)
            .GroupBy(candidate => new
            {
                Name = candidate.Name.Trim().ToUpperInvariant(),
                Width = Math.Round(candidate.WidthMm, 1),
                Height = Math.Round(candidate.HeightMm, 1)
            })
            .Select(group => group
                .OrderBy(candidate => candidate.Provenance == PrinterMediaProvenance.DriverWpf ? 0 : 1)
                .First())
            .OrderBy(candidate => candidate.WidthMm)
            .ThenBy(candidate => candidate.HeightMm)
            .ThenBy(candidate => candidate.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    private static PrinterMediaResolution Classify(PrinterMediaCandidate candidate) =>
        candidate.IsDriverAdvertised
            ? Compatible(candidate)
            : SoftwareStockOnly(candidate);

    private static PrinterMediaResolution Compatible(PrinterMediaCandidate candidate) =>
        new(
            PrinterMediaResolutionStatus.Compatible,
            candidate,
            $"Driver media '{candidate.Name}' matches the requested physical size.");

    private static PrinterMediaResolution SoftwareStockOnly(PrinterMediaCandidate candidate) =>
        new(
            PrinterMediaResolutionStatus.SoftwareStockOnly,
            candidate,
            $"Media '{candidate.Name}' matches the requested physical size but is a software stock "
            + "preset, not driver-advertised physical media; it cannot authorize dispatch.");

    private static PrinterMediaCandidate? FindNamedCandidate(
        IReadOnlyList<PrinterMediaCandidate> candidates,
        string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return candidates.FirstOrDefault(candidate =>
            string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase) ||
            (!string.IsNullOrWhiteSpace(candidate.RawIdentity) &&
             string.Equals(candidate.RawIdentity, name, StringComparison.OrdinalIgnoreCase)));
    }

    private static bool DimensionsMatch(PrinterMediaCandidate candidate, double width, double height) =>
        Math.Abs(candidate.WidthMm - width) <= DimensionToleranceMm &&
        Math.Abs(candidate.HeightMm - height) <= DimensionToleranceMm;

    private static bool IsValidDimension(double value) => double.IsFinite(value) && value > 0;
}
