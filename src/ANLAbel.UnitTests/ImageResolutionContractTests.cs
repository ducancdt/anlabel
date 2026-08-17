using ANLAbel.Core.Printing;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class ImageResolutionContractTests
{
    [Fact]
    public void Observe_ComputesIndependentAxisPpiFromAOneInchFrame()
    {
        var observation = ImageResolutionContract.Observe(300, 150, 25.4, 25.4);

        Assert.Equal(300, observation.PixelWidth);
        Assert.Equal(150, observation.PixelHeight);
        Assert.Equal(300, observation.EffectivePpiX);
        Assert.Equal(150, observation.EffectivePpiY);
        Assert.True(observation.IsValid);
    }

    [Fact]
    public void Observe_HalvingTheFrameDoublesEffectivePpi()
    {
        var full = ImageResolutionContract.Observe(300, 150, 25.4, 25.4);
        var half = ImageResolutionContract.Observe(300, 150, 12.7, 12.7);

        Assert.Equal(full.EffectivePpiX * 2, half.EffectivePpiX);
        Assert.Equal(full.EffectivePpiY * 2, half.EffectivePpiY);
    }

    [Fact]
    public void MeetsDeviceGrid_RequiresBothAxesAndRejectsNonPositiveDpi()
    {
        var observation = ImageResolutionContract.Observe(300, 150, 25.4, 25.4);

        Assert.True(observation.MeetsDeviceGrid(203, 150));
        Assert.True(observation.MeetsDeviceGrid(300, 150));
        Assert.False(observation.MeetsDeviceGrid(301, 150));
        Assert.False(observation.MeetsDeviceGrid(300, 151));
        Assert.False(observation.MeetsDeviceGrid(300, 300));
        Assert.False(observation.MeetsDeviceGrid(0, 150));
        Assert.False(observation.MeetsDeviceGrid(300, 0));
        Assert.False(observation.MeetsDeviceGrid(-1, 150));
        Assert.False(observation.MeetsDeviceGrid(300, -1));
    }

    [Fact]
    public void MeetsDeviceGrid_IncludesTheAuthoredEpsilonOnEachAxis()
    {
        var justInside = new ImageResolutionObservation(10, 10, 203 - 0.0001, 203 - 0.0001);
        var justOutside = new ImageResolutionObservation(10, 10, 203 - 0.0002, 203);

        Assert.True(justInside.IsValid);
        Assert.True(justInside.MeetsDeviceGrid(203, 203));
        Assert.False(justOutside.MeetsDeviceGrid(203, 203));
    }

    [Theory]
    [InlineData(0, 10, 100, 100)]
    [InlineData(10, 0, 100, 100)]
    [InlineData(10, 10, 0, 100)]
    [InlineData(10, 10, double.NaN, 100)]
    [InlineData(10, 10, 100, 0)]
    [InlineData(10, 10, -1, 100)]
    [InlineData(10, 10, 100, double.PositiveInfinity)]
    public void IsValid_FailsClosedOnAnyNonPositiveOrNonFiniteField(
        int pixelWidth,
        int pixelHeight,
        double ppiX,
        double ppiY)
    {
        var observation = new ImageResolutionObservation(pixelWidth, pixelHeight, ppiX, ppiY);
        Assert.False(observation.IsValid);
        Assert.False(observation.MeetsDeviceGrid(203, 203));
    }

    [Theory]
    [InlineData(0, 10, 10, 10)]
    [InlineData(-1, 10, 10, 10)]
    [InlineData(10, 0, 10, 10)]
    [InlineData(10, -1, 10, 10)]
    [InlineData(10, 10, 0, 10)]
    [InlineData(10, 10, -1, 10)]
    [InlineData(10, 10, 10, 0)]
    [InlineData(10, 10, 10, -1)]
    [InlineData(10, 10, double.NaN, 10)]
    [InlineData(10, 10, 10, double.PositiveInfinity)]
    [InlineData(10, 10, double.NegativeInfinity, 10)]
    public void Observe_RejectsInvalidImageOrFrameDimensions(
        int pixelsX,
        int pixelsY,
        double widthMm,
        double heightMm)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            ImageResolutionContract.Observe(pixelsX, pixelsY, widthMm, heightMm));
        Assert.False(string.IsNullOrWhiteSpace(ex.Message));
        Assert.Contains("must be", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
