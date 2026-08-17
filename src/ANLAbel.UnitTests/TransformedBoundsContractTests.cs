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
}
