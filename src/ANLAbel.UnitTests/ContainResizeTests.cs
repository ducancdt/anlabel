using ANLAbel.Core.Geometry;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class ContainResizeTests
{
    [Fact]
    public void Calculate_WiderContainer_FitsByHeightAndCentersHorizontally()
    {
        var result = ContainResize.Calculate(
            containerWidth: 1000,
            containerHeight: 500,
            contentWidth: 400,
            contentHeight: 400);

        Assert.Equal(1.25, result.Scale, precision: 6);
        Assert.Equal(500, result.RenderWidth, precision: 6);
        Assert.Equal(500, result.RenderHeight, precision: 6);
        Assert.Equal(250, result.OffsetX, precision: 6);
        Assert.Equal(0, result.OffsetY, precision: 6);
    }

    [Fact]
    public void Calculate_TallerContainer_FitsByWidthAndCentersVertically()
    {
        var result = ContainResize.Calculate(
            containerWidth: 500,
            containerHeight: 1000,
            contentWidth: 400,
            contentHeight: 400);

        Assert.Equal(1.25, result.Scale, precision: 6);
        Assert.Equal(500, result.RenderWidth, precision: 6);
        Assert.Equal(500, result.RenderHeight, precision: 6);
        Assert.Equal(0, result.OffsetX, precision: 6);
        Assert.Equal(250, result.OffsetY, precision: 6);
    }

    [Theory]
    [InlineData(0, 100, 100, 100)]
    [InlineData(100, 0, 100, 100)]
    [InlineData(100, 100, 0, 100)]
    [InlineData(100, 100, 100, 0)]
    [InlineData(-1, 100, 100, 100)]
    public void Calculate_InvalidInput_ThrowsArgumentOutOfRangeException(
        double containerWidth,
        double containerHeight,
        double contentWidth,
        double contentHeight)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ContainResize.Calculate(
            containerWidth,
            containerHeight,
            contentWidth,
            contentHeight));
    }
}