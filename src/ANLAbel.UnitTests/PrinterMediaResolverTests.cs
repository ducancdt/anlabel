using ANLAbel.Core.Enums;
using ANLAbel.Core.Printing;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class PrinterMediaResolverTests
{
    [Fact]
    public void DriverResolutionChoicesExcludeNonSquareModesAndKeepAuthoredDpi()
    {
        var choices = PrinterResolutionCatalog.BuildSquareDpiChoices(
            new[]
            {
                new PrinterResolutionCandidate(203, 203),
                new PrinterResolutionCandidate(300, 600),
                new PrinterResolutionCandidate(600, 600),
                new PrinterResolutionCandidate(0, 0)
            },
            authoredDpi: 305);

        Assert.Equal(new[] { 203, 305, 600 }, choices);
    }

    [Fact]
    public void PageObservationUsesSharedPrintableAreaValidation()
    {
        var valid = new PrinterPageObservation(
            "USER",
            MediaWidthDip: 400,
            MediaHeightDip: 200,
            ImageableOriginXDip: 2,
            ImageableOriginYDip: 3,
            ImageableWidthDip: 396,
            ImageableHeightDip: 194);
        var outsideMedia = valid with { ImageableWidthDip = 405 };
        var missing = valid with { ImageableHeightDip = null };

        Assert.True(valid.HasEffectiveMedia);
        Assert.True(valid.HasImageableArea);
        Assert.True(valid.IsImageableAreaVerified);
        Assert.False(outsideMedia.IsImageableAreaVerified);
        Assert.False(missing.HasImageableArea);
        Assert.False(missing.IsImageableAreaVerified);
        Assert.Equal(105.83, valid.MediaWidthMm);
    }

    [Fact]
    public void ManualMediaRequiresSameOrientationDriverMatch()
    {
        var snapshot = Snapshot(new PrinterMediaCandidate(
            "Driver 100 x 50", 100, 50, PrinterMediaProvenance.DriverWpf));

        var result = PrinterMediaResolver.Resolve(
            snapshot,
            new PrinterMediaRequest(PaperSizeSource.Manual, 50, 100, 50, 100));

        Assert.Equal(PrinterMediaResolutionStatus.Mismatch, result.Status);
        Assert.False(result.CanDispatch);
    }

    [Fact]
    public void ManualMediaMatchesWithinDriverRoundingTolerance()
    {
        var candidate = new PrinterMediaCandidate(
            "Driver 100 x 50", 100.3, 49.7, PrinterMediaProvenance.DriverWpf, "driver-id");

        var result = PrinterMediaResolver.Resolve(
            Snapshot(candidate),
            new PrinterMediaRequest(PaperSizeSource.Manual, 100, 50, 100, 50));

        Assert.Equal(PrinterMediaResolutionStatus.Compatible, result.Status);
        Assert.Same(candidate, result.MatchedMedia);
        Assert.True(result.CanDispatch);
    }

    [Fact]
    public void DriverMediaCannotBeCompatibleWhenItDoesNotMatchAuthoredLabel()
    {
        var candidate = new PrinterMediaCandidate(
            "Driver 50 x 30", 50, 30, PrinterMediaProvenance.DriverWpf, "driver-small");

        var result = PrinterMediaResolver.Resolve(
            Snapshot(candidate),
            new PrinterMediaRequest(PaperSizeSource.Manual, 100, 50, 50, 30, "driver-small"));

        Assert.Equal(PrinterMediaResolutionStatus.Mismatch, result.Status);
        Assert.Null(result.MatchedMedia);
        Assert.False(result.CanDispatch);
        Assert.Contains("authored label", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("does not contain", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ManualMediaMayBeLargerThanTheAuthoredLabel()
    {
        var candidate = new PrinterMediaCandidate(
            "Driver 100 x 50", 100, 50, PrinterMediaProvenance.DriverWpf, "driver-sheet");

        var result = PrinterMediaResolver.Resolve(
            Snapshot(candidate),
            new PrinterMediaRequest(PaperSizeSource.Manual, 50, 30, 100, 50, "driver-sheet"));

        Assert.Equal(PrinterMediaResolutionStatus.Compatible, result.Status);
        Assert.True(result.CanDispatch);
    }

    [Fact]
    public void ManualMediaSmallerThanAuthoredLabelStaysMismatch()
    {
        var candidate = new PrinterMediaCandidate(
            "75X100", 77.47, 100.08, PrinterMediaProvenance.DriverWpf, "75x100");

        var result = PrinterMediaResolver.Resolve(
            Snapshot(candidate),
            new PrinterMediaRequest(PaperSizeSource.Manual, 100, 50, 77.47, 100.08, "75x100"));

        Assert.Equal(PrinterMediaResolutionStatus.Mismatch, result.Status);
        Assert.False(result.CanDispatch);
        Assert.Contains("does not contain", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("driver-automatic", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("large enough", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ExplicitOrientationSwapStillMatchesAuthoredLabel()
    {
        var candidate = new PrinterMediaCandidate(
            "Driver portrait form", 50, 100, PrinterMediaProvenance.DriverWpf, "driver-portrait");

        var result = PrinterMediaResolver.Resolve(
            Snapshot(candidate),
            new PrinterMediaRequest(PaperSizeSource.Manual, 100, 50, 50, 100, "driver-portrait"));

        Assert.Equal(PrinterMediaResolutionStatus.Compatible, result.Status);
        Assert.Same(candidate, result.MatchedMedia);
    }

    [Fact]
    public void DriverAutomaticDoesNotRequireSoftwareStockMatch()
    {
        var result = PrinterMediaResolver.Resolve(
            Snapshot(),
            new PrinterMediaRequest(PaperSizeSource.DriverAutomatic, 73, 29));

        Assert.Equal(PrinterMediaResolutionStatus.DriverAutomatic, result.Status);
        Assert.True(result.CanDispatch);
    }

    [Fact]
    public void DriverAutomaticCannotBypassKnownQueueFault()
    {
        var snapshot = Snapshot() with
        {
            QueueHealth = PrinterQueueHealthContract.Evaluate(
                readSucceeded: true,
                isPaused: false,
                isOffline: true,
                isOutOfPaper: false,
                isPaperJammed: false,
                needsUserIntervention: false,
                isInError: false)
        };

        var result = PrinterMediaResolver.Resolve(
            snapshot,
            new PrinterMediaRequest(PaperSizeSource.DriverAutomatic, 73, 29));

        Assert.Equal(PrinterMediaResolutionStatus.QueueBlocked, result.Status);
        Assert.False(result.CanDispatch);
        Assert.Contains("offline", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void IncompleteCapabilitiesRemainUnknownInsteadOfAssumingCompatibility()
    {
        var snapshot = Snapshot(isComplete: false);

        var result = PrinterMediaResolver.Resolve(
            snapshot,
            new PrinterMediaRequest(PaperSizeSource.Manual, 100, 50, 100, 50));

        Assert.Equal(PrinterMediaResolutionStatus.Unknown, result.Status);
        Assert.False(result.CanDispatch);
    }

    [Fact]
    public void CustomMediaRequiresExplicitDriverSupport()
    {
        var result = PrinterMediaResolver.Resolve(
            Snapshot(supportsCustom: true),
            new PrinterMediaRequest(PaperSizeSource.Manual, 87, 31, 87, 31));

        Assert.Equal(PrinterMediaResolutionStatus.CustomSupported, result.Status);
        Assert.True(result.CanDispatch);
    }

    [Fact]
    public void UnavailableQueueFailsClosed()
    {
        var result = PrinterMediaResolver.Resolve(
            PrinterCapabilitySnapshot.Unavailable("missing", "Queue disappeared."),
            new PrinterMediaRequest(PaperSizeSource.Manual, 100, 50));

        Assert.Equal(PrinterMediaResolutionStatus.Unavailable, result.Status);
        Assert.False(result.CanDispatch);
    }

    [Fact]
    public void MergePreservesDriverProvenanceAndDropsInvalidRows()
    {
        var merged = PrinterMediaResolver.MergeCandidates(new[]
        {
            new PrinterMediaCandidate("Stock", 100, 50, PrinterMediaProvenance.DriverWin32),
            new PrinterMediaCandidate("Stock", 100.02, 50.02, PrinterMediaProvenance.DriverWpf),
            new PrinterMediaCandidate("Invalid", 0, 50, PrinterMediaProvenance.DriverWpf)
        });

        var row = Assert.Single(merged);
        Assert.Equal(PrinterMediaProvenance.DriverWpf, row.Provenance);
    }

    [Fact]
    public void MergeAddsWin32RowsMissingFromWpfCatalog()
    {
        var merged = PrinterMediaResolver.MergeCandidates(new[]
        {
            new PrinterMediaCandidate("100 x 50", 100, 50, PrinterMediaProvenance.DriverWpf, "NorthAmerica100x50"),
            new PrinterMediaCandidate("100 x 50", 100, 50, PrinterMediaProvenance.DriverWin32, "257"),
            new PrinterMediaCandidate("75 x 25", 75, 25, PrinterMediaProvenance.DriverWin32, "258")
        });

        Assert.Equal(2, merged.Count);
        Assert.Contains(merged, row =>
            row.Name == "100 x 50" &&
            row.Provenance == PrinterMediaProvenance.DriverWpf &&
            row.RawIdentity == "NorthAmerica100x50");
        Assert.Contains(merged, row =>
            row.Name == "75 x 25" &&
            row.Provenance == PrinterMediaProvenance.DriverWin32 &&
            row.RawIdentity == "258");
    }

    [Theory]
    [InlineData(PrinterMediaProvenance.VendorStock)]
    [InlineData(PrinterMediaProvenance.BuiltInStock)]
    [InlineData(PrinterMediaProvenance.UserCustom)]
    [InlineData(PrinterMediaProvenance.Unknown)]
    public void SoftwareStockMatchDoesNotAuthorizeDispatch(PrinterMediaProvenance provenance)
    {
        var candidate = new PrinterMediaCandidate(
            "Stock 100 x 50", 100, 50, provenance, "stock-id");

        var result = PrinterMediaResolver.Resolve(
            Snapshot(candidate),
            new PrinterMediaRequest(PaperSizeSource.Manual, 100, 50, 100, 50, "stock-id"));

        Assert.Equal(PrinterMediaResolutionStatus.SoftwareStockOnly, result.Status);
        Assert.False(result.CanDispatch);
        Assert.Same(candidate, result.MatchedMedia);
    }

    [Theory]
    [InlineData(PrinterMediaProvenance.DriverWpf)]
    [InlineData(PrinterMediaProvenance.DriverWin32)]
    public void DriverAdvertisedMatchAuthorizesDispatch(PrinterMediaProvenance provenance)
    {
        var candidate = new PrinterMediaCandidate(
            "Driver 100 x 50", 100, 50, provenance, "driver-id");

        var result = PrinterMediaResolver.Resolve(
            Snapshot(candidate),
            new PrinterMediaRequest(PaperSizeSource.Manual, 100, 50, 100, 50, "driver-id"));

        Assert.Equal(PrinterMediaResolutionStatus.Compatible, result.Status);
        Assert.True(result.CanDispatch);
        Assert.Same(candidate, result.MatchedMedia);
    }

    [Fact]
    public void DiagnosticMessagesUseCleanMultiplicationSymbolWithoutMojibake()
    {
        var candidate = new PrinterMediaCandidate("Driver 50 x 30", 50, 30, PrinterMediaProvenance.DriverWpf);
        var mismatchResult = PrinterMediaResolver.Resolve(
            Snapshot(candidate),
            new PrinterMediaRequest(PaperSizeSource.Manual, 100, 50, 50, 30));

        Assert.Equal(PrinterMediaResolutionStatus.Mismatch, mismatchResult.Status);
        Assert.Contains("50 × 30 mm", mismatchResult.Message);
        Assert.Contains("100 × 50 mm", mismatchResult.Message);
        Assert.DoesNotContain("Ã", mismatchResult.Message);

        var rangeSnapshot = Snapshot(candidate, supportsCustom: true);
        var outOfRangeResult = PrinterMediaResolver.Resolve(
            rangeSnapshot,
            new PrinterMediaRequest(PaperSizeSource.Manual, 200, 300, 200, 300));
        Assert.Equal(PrinterMediaResolutionStatus.Mismatch, outOfRangeResult.Status);
        Assert.Contains("10 × 10 to 110 × 190 mm", outOfRangeResult.Message);
        Assert.DoesNotContain("Ã", outOfRangeResult.Message);
    }

    [Fact]
    public void DriverSettingsSourceTreatsPageSizeAsDriverOwnedEvenWhenManualRequested()
    {
        var candidate = new PrinterMediaCandidate("Driver 100 x 50", 100, 50, PrinterMediaProvenance.DriverWpf);
        var snapshot = Snapshot(candidate);

        // When Driver owns settings, it defaults to DriverAutomatic without claiming manual media override
        var result = PrinterMediaResolver.Resolve(
            snapshot,
            new PrinterMediaRequest(
                PaperSizeSource.Manual,
                100,
                50,
                100,
                50,
                "Driver 100 x 50",
                PrinterSettingsSource.Driver));

        Assert.Equal(PrinterMediaResolutionStatus.DriverAutomatic, result.Status);
        Assert.True(result.CanDispatch);
        Assert.Contains("Current driver preferences own the print ticket", result.Message);
        Assert.Contains("Manual media selection is inactive", result.Message);
    }

    [Fact]
    public void DriverSettingsSourceFailsClosedWhenDriverPageObservationMismatchesAuthoredLabel()
    {
        var candidate = new PrinterMediaCandidate("A4", 210, 297, PrinterMediaProvenance.DriverWpf);
        // Driver DEVMODE has A4 configured (210 x 297 mm in DIP)
        var pageObs = new PrinterPageObservation(
            "A4",
            MediaWidthDip: 793.7, // ~210 mm
            MediaHeightDip: 1122.5, // ~297 mm
            ImageableOriginXDip: 0,
            ImageableOriginYDip: 0,
            ImageableWidthDip: 793.7,
            ImageableHeightDip: 1122.5);

        var snapshot = new PrinterCapabilitySnapshot(
            "Queue",
            "Driver",
            DateTimeOffset.UtcNow,
            IsAvailable: true,
            IsComplete: true,
            SupportsCustomMedia: false,
            new[] { candidate },
            string.Empty,
            PageObservation: pageObs);

        // Authored label is 100 x 50 mm, but driver ticket has A4
        var result = PrinterMediaResolver.Resolve(
            snapshot,
            new PrinterMediaRequest(
                PaperSizeSource.Manual,
                100,
                50,
                100,
                50,
                "A4",
                PrinterSettingsSource.Driver));

        Assert.Equal(PrinterMediaResolutionStatus.Mismatch, result.Status);
        Assert.False(result.CanDispatch);
        Assert.Contains("Current driver preferences own the print ticket", result.Message);
        Assert.Contains("does not match authored label 100 × 50 mm", result.Message);
        Assert.DoesNotContain("Ã", result.Message);
    }

    [Fact]
    public void DriverSettingsSourceAcceptsMatchingEffectivePageObservation()
    {
        var candidate = new PrinterMediaCandidate("100 x 50 mm", 100, 50, PrinterMediaProvenance.DriverWpf);
        // Driver DEVMODE has matching 100 x 50 mm configured
        var pageObs = new PrinterPageObservation(
            "100 x 50 mm",
            MediaWidthDip: 377.95, // 100 mm
            MediaHeightDip: 188.98, // 50 mm
            ImageableOriginXDip: 0,
            ImageableOriginYDip: 0,
            ImageableWidthDip: 377.95,
            ImageableHeightDip: 188.98);

        var snapshot = new PrinterCapabilitySnapshot(
            "Queue",
            "Driver",
            DateTimeOffset.UtcNow,
            IsAvailable: true,
            IsComplete: true,
            SupportsCustomMedia: false,
            new[] { candidate },
            string.Empty,
            PageObservation: pageObs);

        var result = PrinterMediaResolver.Resolve(
            snapshot,
            new PrinterMediaRequest(
                PaperSizeSource.DriverAutomatic,
                100,
                50,
                SettingsSource: PrinterSettingsSource.Driver));

        Assert.Equal(PrinterMediaResolutionStatus.DriverAutomatic, result.Status);
        Assert.True(result.CanDispatch);
        Assert.Contains("matches authored label", result.Message);
        Assert.Contains("Manual media override is inactive", result.Message);
        Assert.DoesNotContain("Ã", result.Message);
    }

    [Fact]
    public void PrinterProfileNormalizesPaperSizeSourceToAutomaticWhenDriverSettingsSelected()
    {
        var profile = new ANLAbel.Core.Models.PrinterProfile
        {
            SettingsSource = PrinterSettingsSource.Label,
            PaperSizeSource = PaperSizeSource.Manual
        };

        Assert.Equal(PaperSizeSource.Manual, profile.PaperSizeSource);

        // Switching to Driver must normalize PaperSizeSource to DriverAutomatic
        profile.SettingsSource = PrinterSettingsSource.Driver;
        Assert.Equal(PaperSizeSource.DriverAutomatic, profile.PaperSizeSource);

        // Attempting to set Manual while in Driver mode must remain DriverAutomatic
        profile.PaperSizeSource = PaperSizeSource.Manual;
        Assert.Equal(PaperSizeSource.DriverAutomatic, profile.PaperSizeSource);

        // Switching back to Label allows Manual again
        profile.SettingsSource = PrinterSettingsSource.Label;
        profile.PaperSizeSource = PaperSizeSource.Manual;
        Assert.Equal(PaperSizeSource.Manual, profile.PaperSizeSource);
    }

    [Fact]
    public void CommitPrintConfigurationNormalizesDriverSettingsToAutomatic()
    {
        var session = new ANLAbel.Core.Models.LabelTemplate();
        session.PrinterProfile.SettingsSource = PrinterSettingsSource.Driver;
        session.PrinterProfile.PaperSizeSource = PaperSizeSource.Manual; // Normalized to Automatic

        var authored = new ANLAbel.Core.Models.LabelTemplate();
        authored.PrinterProfile.SettingsSource = PrinterSettingsSource.Label;
        authored.PrinterProfile.PaperSizeSource = PaperSizeSource.Manual;

        PrintSessionTemplateContract.CommitPrintConfiguration(session, authored);

        Assert.Equal(PrinterSettingsSource.Driver, authored.PrinterProfile.SettingsSource);
        Assert.Equal(PaperSizeSource.DriverAutomatic, authored.PrinterProfile.PaperSizeSource);
    }

    private static PrinterCapabilitySnapshot Snapshot(
        PrinterMediaCandidate? media = null,
        bool isComplete = true,
        bool supportsCustom = false) =>
        new(
            "Queue",
            "Driver",
            DateTimeOffset.UtcNow,
            IsAvailable: true,
            IsComplete: isComplete,
            SupportsCustomMedia: supportsCustom,
            media is null ? Array.Empty<PrinterMediaCandidate>() : new[] { media },
            string.Empty,
            CustomMediaRange: supportsCustom
                ? new PrinterCustomMediaRange(10, 10, 110, 190)
                : null);
}
