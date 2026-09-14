using ANLAbel.Core.Geometry;
using ANLAbel.Core.Models;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class TransformedBoundsContractTests
{
    [Theory]
    [InlineData(0, 10, 20, 40, 30)]
    [InlineData(180, 10, 20, 40, 30)]
    [InlineData(90, 20, 10, 30, 40)]
    [InlineData(270, 20, 10, 30, 40)]
    public void CardinalRotationUsesTheAuthoredFrameCenter(
        int rotation,
        double expectedLeft,
        double expectedTop,
        double expectedRight,
        double expectedBottom)
    {
        var bounds = TransformedBoundsContract.GetBounds(10, 20, 30, 10, rotation);

        Assert.Equal(expectedLeft, bounds.Left, 6);
        Assert.Equal(expectedTop, bounds.Top, 6);
        Assert.Equal(expectedRight, bounds.Right, 6);
        Assert.Equal(expectedBottom, bounds.Bottom, 6);
    }

    [Fact]
    public void ArrangeEngineUsesTheSameTransformedBounds()
    {
        var item = new LabelObject
        {
            XMm = 10,
            YMm = 20,
            WidthMm = 30,
            HeightMm = 10,
            Rotation = 90
        };

        var bounds = LabelArrangeEngine.GetBounds(item);
        var expected = TransformedBoundsContract.GetBounds(item);

        Assert.Equal(expected, bounds);
    }

    [Fact]
    public void UnsupportedRotationNormalizesFailClosedToZero()
    {
        Assert.Equal(0, TransformedBoundsContract.NormalizeRotation(45));
        Assert.Equal(270, TransformedBoundsContract.NormalizeRotation(-90));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(90, 90)]
    [InlineData(180, 180)]
    [InlineData(270, 270)]
    [InlineData(360, 0)]
    [InlineData(450, 90)]
    [InlineData(-270, 90)]
    [InlineData(720, 0)]
    [InlineData(91, 0)]
    [InlineData(1, 0)]
    public void NormalizeRotationSnapsToCardinalOrFailsClosed(int rotation, int expected)
    {
        Assert.Equal(expected, TransformedBoundsContract.NormalizeRotation(rotation));
    }

    [Theory]
    [InlineData(double.NaN, 20, 30, 10)]
    [InlineData(10, double.NaN, 30, 10)]
    [InlineData(10, 20, double.PositiveInfinity, 10)]
    [InlineData(10, 20, 30, double.NegativeInfinity)]
    public void NonFiniteInputFailsClosedToNaNBounds(
        double xMm,
        double yMm,
        double widthMm,
        double heightMm)
    {
        var bounds = TransformedBoundsContract.GetBounds(xMm, yMm, widthMm, heightMm, 0);

        Assert.True(double.IsNaN(bounds.Left));
        Assert.True(double.IsNaN(bounds.Top));
        Assert.True(double.IsNaN(bounds.Right));
        Assert.True(double.IsNaN(bounds.Bottom));
    }

    [Fact]
    public void NegativeExtentClampsToZeroAroundTheAuthoredOrigin()
    {
        // Width/height clamp to 0, so the transformed box collapses onto the
        // authored top-left corner (center == origin when extent is zero).
        var bounds = TransformedBoundsContract.GetBounds(15, 25, -30, -10, 0);

        Assert.Equal(15, bounds.Left, 6);
        Assert.Equal(25, bounds.Top, 6);
        Assert.Equal(15, bounds.Right, 6);
        Assert.Equal(25, bounds.Bottom, 6);
    }

    [Fact]
    public void QuarterTurnSwapsWidthAndHeightAroundTheSameCenter()
    {
        // 90deg: transformed extent uses height for width and width for height,
        // pinning the mutation that drops the 90-or-270 swap.
        var bounds = TransformedBoundsContract.GetBounds(0, 0, 40, 10, 90);

        Assert.Equal(15, bounds.Left, 6);
        Assert.Equal(-15, bounds.Top, 6);
        Assert.Equal(25, bounds.Right, 6);
        Assert.Equal(25, bounds.Bottom, 6);
    }
}
