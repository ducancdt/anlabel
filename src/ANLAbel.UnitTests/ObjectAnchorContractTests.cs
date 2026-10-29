using ANLAbel.Core.Enums;
using ANLAbel.Core.Geometry;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class ObjectAnchorContractTests
{
    [Theory]
    [InlineData(ObjectAnchorPoint.TopLeft, 10, 20)]
    [InlineData(ObjectAnchorPoint.TopRight, -10, 20)]
    [InlineData(ObjectAnchorPoint.Center, 0, 15)]
    [InlineData(ObjectAnchorPoint.BottomRight, -10, 10)]
    [InlineData(ObjectAnchorPoint.BottomCenter, 0, 10)]
    public void GrowingKeepsTheAnchorFixed(ObjectAnchorPoint anchor, double x, double y)
    {
        // 20 x 10 mm at (10, 20) grows to 40 x 20 mm.
        Assert.Equal((x, y), ObjectAnchorContract.Resize(10, 20, 20, 10, 40, 20, anchor));
    }

    [Fact]
    public void AnchorValuesFollowNiceLabelOrder()
    {
        Assert.Equal((0.5, 0.0), ObjectAnchorContract.Fractions((ObjectAnchorPoint)1));
        Assert.Equal((0.0, 0.5), ObjectAnchorContract.Fractions((ObjectAnchorPoint)3));
        Assert.Equal((1.0, 1.0), ObjectAnchorContract.Fractions((ObjectAnchorPoint)8));
        Assert.Equal((0.0, 0.0), ObjectAnchorContract.Fractions((ObjectAnchorPoint)9));
    }
}
