using ANLAbel.Core.Printing;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class MediaDimensionContractTests
{
    // 25.4 mm = 1 inch = 96 WPF DIP. These are domain identities, not a
    // copy of the contract's conversion helper.
    private const double OneInchMm = 25.4;
    private const double HalfInchMm = 12.7;
    private const double OneInchDip = 96.0;
    private const double HalfInchDip = 48.0;

    [Fact]
    public void ExactPhysicalInchMatchesNinetySixDip()
    {
        Assert.True(MediaDimensionContract.Matches(OneInchMm, HalfInchMm, OneInchDip, HalfInchDip));
        Assert.True(MediaDimensionContract.Matches(OneInchMm, HalfInchMm, OneInchDip + 0.9, HalfInchDip - 0.9));
        Assert.True(MediaDimensionContract.Matches(OneInchMm, HalfInchMm, OneInchDip + 1.0, HalfInchDip));
        Assert.True(MediaDimensionContract.Matches(OneInchMm, HalfInchMm, OneInchDip, HalfInchDip + 1.0));
    }

    [Fact]
    public void DriverCoercionIsRejectedOnEachAxis()
    {
        Assert.False(MediaDimensionContract.Matches(OneInchMm, HalfInchMm, OneInchDip + 1.1, HalfInchDip));
        Assert.False(MediaDimensionContract.Matches(OneInchMm, HalfInchMm, OneInchDip, HalfInchDip + 1.1));
        Assert.False(MediaDimensionContract.Matches(OneInchMm, HalfInchMm, OneInchDip - 1.1, HalfInchDip));
        Assert.False(MediaDimensionContract.Matches(OneInchMm, HalfInchMm, OneInchDip, HalfInchDip - 1.1));
    }

    [Fact]
    public void SwappedAxesDoNotMatch()
    {
        Assert.False(MediaDimensionContract.Matches(OneInchMm, HalfInchMm, HalfInchDip, OneInchDip));
    }

    [Fact]
    public void ZeroToleranceAcceptsOnlyExactDip()
    {
        Assert.True(MediaDimensionContract.Matches(OneInchMm, HalfInchMm, OneInchDip, HalfInchDip, 0));
        Assert.False(MediaDimensionContract.Matches(OneInchMm, HalfInchMm, OneInchDip + 0.1, HalfInchDip, 0));
    }

    [Fact]
    public void WiderToleranceAcceptsLargerDriverRounding()
    {
        Assert.True(MediaDimensionContract.Matches(OneInchMm, HalfInchMm, OneInchDip + 1.5, HalfInchDip, 2.0));
        Assert.False(MediaDimensionContract.Matches(OneInchMm, HalfInchMm, OneInchDip + 2.1, HalfInchDip, 2.0));
    }

    [Theory]
    [InlineData(0, HalfInchMm, OneInchDip, HalfInchDip, 1.0)]
    [InlineData(-1, HalfInchMm, OneInchDip, HalfInchDip, 1.0)]
    [InlineData(OneInchMm, 0, OneInchDip, HalfInchDip, 1.0)]
    [InlineData(OneInchMm, -1, OneInchDip, HalfInchDip, 1.0)]
    [InlineData(OneInchMm, HalfInchMm, 0, HalfInchDip, 1.0)]
    [InlineData(OneInchMm, HalfInchMm, -1, HalfInchDip, 1.0)]
    [InlineData(OneInchMm, HalfInchMm, OneInchDip, 0, 1.0)]
    [InlineData(OneInchMm, HalfInchMm, OneInchDip, -1, 1.0)]
    [InlineData(OneInchMm, HalfInchMm, OneInchDip, HalfInchDip, -0.01)]
    public void NonPositiveOrNegativeToleranceNeverMatches(
        double expectedWidthMm,
        double expectedHeightMm,
        double effectiveWidthDip,
        double effectiveHeightDip,
        double toleranceDip)
    {
        Assert.False(MediaDimensionContract.Matches(
            expectedWidthMm,
            expectedHeightMm,
            effectiveWidthDip,
            effectiveHeightDip,
            toleranceDip));
    }

    [Theory]
    [InlineData(double.NaN, HalfInchMm, OneInchDip, HalfInchDip, 1.0)]
    [InlineData(OneInchMm, double.NaN, OneInchDip, HalfInchDip, 1.0)]
    [InlineData(OneInchMm, HalfInchMm, double.NaN, HalfInchDip, 1.0)]
    [InlineData(OneInchMm, HalfInchMm, OneInchDip, double.NaN, 1.0)]
    [InlineData(OneInchMm, HalfInchMm, OneInchDip, HalfInchDip, double.NaN)]
    [InlineData(double.PositiveInfinity, HalfInchMm, OneInchDip, HalfInchDip, 1.0)]
    [InlineData(OneInchMm, double.NegativeInfinity, OneInchDip, HalfInchDip, 1.0)]
    [InlineData(OneInchMm, HalfInchMm, double.PositiveInfinity, HalfInchDip, 1.0)]
    public void NonFiniteValuesNeverMatch(
        double expectedWidthMm,
        double expectedHeightMm,
        double effectiveWidthDip,
        double effectiveHeightDip,
        double toleranceDip)
    {
        Assert.False(MediaDimensionContract.Matches(
            expectedWidthMm,
            expectedHeightMm,
            effectiveWidthDip,
            effectiveHeightDip,
            toleranceDip));
    }
}
