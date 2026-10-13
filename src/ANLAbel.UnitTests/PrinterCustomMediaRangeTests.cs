using ANLAbel.Core.Enums;
using ANLAbel.Core.Printing;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class PrinterCustomMediaRangeTests
{
    [Fact]
    public void TenthsOfMillimeterRangeIncludesItsBoundaries()
    {
        var range = PrinterCustomMediaRange.FromTenthsOfMillimeter(200, 100, 1040, 1800);

        Assert.True(range.IsValid);
        Assert.True(range.Contains(20, 10));
        Assert.True(range.Contains(104, 180));
        Assert.False(range.Contains(104.2, 180));
    }

    [Fact]
    public void DegenerateDriverSentinelIsNotCustomMediaEvidence()
    {
        var range = PrinterCustomMediaRange.FromTenthsOfMillimeter(1, 1, 1, 1);

        Assert.False(range.IsValid);
        Assert.False(range.Contains(0.1, 0.1));
    }

    [Fact]
    public void CustomMediaRequiresUsableDriverLimits()
    {
        var request = new PrinterMediaRequest(PaperSizeSource.Manual, 50, 30, 50, 30);
        var missingLimits = Snapshot(null);
        var validLimits = Snapshot(new PrinterCustomMediaRange(20, 10, 104, 180));

        Assert.Equal(
            PrinterMediaResolutionStatus.Mismatch,
            PrinterMediaResolver.Resolve(missingLimits, request).Status);
        Assert.Equal(
            PrinterMediaResolutionStatus.CustomSupported,
            PrinterMediaResolver.Resolve(validLimits, request).Status);
    }

    [Fact]
    public void CustomMediaOutsideDriverLimitsFailsClosed()
    {
        var snapshot = Snapshot(new PrinterCustomMediaRange(20, 10, 40, 25));
        var request = new PrinterMediaRequest(PaperSizeSource.Manual, 50, 30, 50, 30);

        var result = PrinterMediaResolver.Resolve(snapshot, request);

        Assert.Equal(PrinterMediaResolutionStatus.Mismatch, result.Status);
        Assert.Contains("outside", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static PrinterCapabilitySnapshot Snapshot(PrinterCustomMediaRange? range) => new(
        "queue",
        "driver",
        DateTimeOffset.UnixEpoch,
        IsAvailable: true,
        IsComplete: true,
        SupportsCustomMedia: true,
        Array.Empty<PrinterMediaCandidate>(),
        CustomMediaRange: range);
}
