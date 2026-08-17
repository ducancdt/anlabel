using ANLAbel.Core.Geometry;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class SnapToleranceContractTests
{
    [Fact]
    public void PublicBudgetsAndZoomBoundsAreTheAuthoredConstants()
    {
        Assert.Equal(6.0, SnapToleranceContract.DefaultAcquireToleranceDip);
        Assert.Equal(10.0, SnapToleranceContract.DefaultReleaseToleranceDip);
        Assert.Equal(0.25, SnapToleranceContract.MinimumZoom);
        Assert.Equal(4.0, SnapToleranceContract.MaximumZoom);
    }

    [Theory]
    [InlineData(double.NaN, 1.0)]
    [InlineData(double.PositiveInfinity, 1.0)]
    [InlineData(double.NegativeInfinity, 1.0)]
    [InlineData(0, 0.25)]
    [InlineData(-5, 0.25)]
    [InlineData(0.1, 0.25)]
    [InlineData(0.25, 0.25)]
    [InlineData(1, 1.0)]
    [InlineData(4, 4.0)]
    [InlineData(8, 4.0)]
    [InlineData(10, 4.0)]
    public void NormalizeZoomFallsBackAndClamps(double zoom, double expected)
    {
        Assert.Equal(expected, SnapToleranceContract.NormalizeZoom(zoom));
    }

    [Fact]
    public void AcquireAndReleaseShareToDocumentMmAndStayInRatio()
    {
        var acquire = SnapToleranceContract.AcquireToleranceMm(1);
        var release = SnapToleranceContract.ReleaseToleranceMm(1);

        Assert.Equal(SnapToleranceContract.ToDocumentMm(SnapToleranceContract.DefaultAcquireToleranceDip, 1), acquire, precision: 9);
        Assert.Equal(SnapToleranceContract.ToDocumentMm(SnapToleranceContract.DefaultReleaseToleranceDip, 1), release, precision: 9);
        Assert.True(release > acquire);
        Assert.Equal(
            acquire * SnapToleranceContract.DefaultReleaseToleranceDip / SnapToleranceContract.DefaultAcquireToleranceDip,
            release,
            precision: 9);
    }

    [Fact]
    public void DocumentToleranceScalesInverselyWithZoomAndClamps()
    {
        var atOne = SnapToleranceContract.AcquireToleranceMm(1);
        Assert.True(atOne > 0);
        Assert.Equal(atOne / 2.0, SnapToleranceContract.AcquireToleranceMm(2), precision: 9);
        Assert.Equal(
            SnapToleranceContract.AcquireToleranceMm(SnapToleranceContract.MinimumZoom),
            SnapToleranceContract.AcquireToleranceMm(0.1),
            precision: 9);
        Assert.Equal(
            SnapToleranceContract.AcquireToleranceMm(SnapToleranceContract.MaximumZoom),
            SnapToleranceContract.AcquireToleranceMm(8),
            precision: 9);
        Assert.Equal(atOne, SnapToleranceContract.AcquireToleranceMm(double.NaN), precision: 9);
        Assert.Equal(atOne, SnapToleranceContract.ReleaseToleranceMm(1) * SnapToleranceContract.DefaultAcquireToleranceDip / SnapToleranceContract.DefaultReleaseToleranceDip, precision: 9);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(-1)]
    [InlineData(-0.01)]
    public void InvalidScreenBudgetIsZero(double screenToleranceDip)
    {
        Assert.Equal(0, SnapToleranceContract.ToDocumentMm(screenToleranceDip, 1));
        Assert.Equal(0, SnapToleranceContract.ToDocumentMm(screenToleranceDip, 2));
    }

    [Fact]
    public void ZeroScreenBudgetIsZeroRegardlessOfZoom()
    {
        Assert.Equal(0, SnapToleranceContract.ToDocumentMm(0, 1));
        Assert.Equal(0, SnapToleranceContract.ToDocumentMm(0, 0.25));
        Assert.Equal(0, SnapToleranceContract.ToDocumentMm(0, double.NaN));
    }
}
