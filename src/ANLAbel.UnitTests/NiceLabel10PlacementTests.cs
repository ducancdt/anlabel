using ANLAbel.Project.NiceLabel10;
using Xunit;

namespace ANLAbel.UnitTests;

/// <summary>
/// The NiceLabel 10 anchor/rotation placement contract derived from the
/// vendor runtime geometry (docs/NICELABEL_10_REVERSE_ENGINEERING.md §8).
/// </summary>
public sealed class NiceLabel10PlacementTests
{
    [Theory]
    [InlineData(0, 0.0, 0.0)] // LeftTop
    [InlineData(1, 0.5, 0.0)] // CenterTop
    [InlineData(2, 1.0, 0.0)] // RightTop
    [InlineData(3, 0.0, 0.5)] // LeftMiddle
    [InlineData(4, 0.5, 0.5)] // CenterMiddle
    [InlineData(5, 1.0, 0.5)] // RightMiddle
    [InlineData(6, 0.0, 1.0)] // LeftBottom
    [InlineData(7, 0.5, 1.0)] // CenterBottom
    [InlineData(8, 1.0, 1.0)] // RightBottom
    [InlineData(9, 0.0, 0.0)] // None
    [InlineData(-1, 0.0, 0.0)]
    public void AnchorMultipliers_FollowVendorEnumOrder(int anchor, double width, double height)
    {
        Assert.Equal((width, height), NiceLabel10Placement.AnchorMultipliers(anchor));
    }

    [Fact]
    public void LeftTopUnrotated_KeepsStoredPoint()
    {
        var frame = NiceLabel10Placement.ResolveFrame(10, 20, 40, 8, anchoringPoint: 0, rotationDegrees: 0);
        Assert.Equal((10.0, 20.0, 0), frame);
    }

    [Fact]
    public void RightTopUnrotated_StoredPointIsRightEdge()
    {
        var frame = NiceLabel10Placement.ResolveFrame(100, 20, 40, 8, anchoringPoint: 2, rotationDegrees: 0);
        Assert.Equal((60.0, 20.0, 0), frame);
    }

    [Fact]
    public void CenterBottomUnrotated_StoredPointIsBottomCentre()
    {
        var frame = NiceLabel10Placement.ResolveFrame(50, 60, 40, 8, anchoringPoint: 7, rotationDegrees: 0);
        Assert.Equal((30.0, 52.0, 0), frame);
    }

    [Fact]
    public void LeftTopRotated270_ObjectRisesAboveStoredPoint()
    {
        // A 40 x 8 text rotated 270° clockwise occupies an 8-wide, 40-tall
        // box whose bottom-left corner is the stored point. ANLAbel keeps
        // the unrotated 40 x 8 frame centred on that box.
        var (left, top, rotation) = NiceLabel10Placement.ResolveFrame(10, 100, 40, 8, anchoringPoint: 0, rotationDegrees: 270);
        Assert.Equal(270, rotation);
        Assert.Equal(10 + 8 / 2.0 - 40 / 2.0, left, 6);
        Assert.Equal(100 - 40 + 40 / 2.0 - 8 / 2.0, top, 6);
    }

    [Fact]
    public void RightTopRotated270_StoredPointIsTopLeftOfBox()
    {
        // RightTop (wm 1, hm 0) at 270°: anchor offset (H·hm, W·(1−wm)) = (0, 0).
        var (left, top, rotation) = NiceLabel10Placement.ResolveFrame(13.7, 162.8, 40, 8, anchoringPoint: 2, rotationDegrees: 270);
        Assert.Equal(270, rotation);
        Assert.Equal(13.7 + 4 - 20, left, 6);
        Assert.Equal(162.8 + 20 - 4, top, 6);
    }

    [Fact]
    public void CenterMiddleRotated90_BoxCentredOnStoredPoint()
    {
        var (left, top, rotation) = NiceLabel10Placement.ResolveFrame(50, 50, 40, 8, anchoringPoint: 4, rotationDegrees: 90);
        Assert.Equal(90, rotation);
        Assert.Equal(50 - 20, left, 6);
        Assert.Equal(50 - 4, top, 6);
    }

    [Fact]
    public void LeftTopRotated180_StoredPointIsBottomRight()
    {
        var (left, top, rotation) = NiceLabel10Placement.ResolveFrame(100, 50, 40, 8, anchoringPoint: 0, rotationDegrees: 180);
        Assert.Equal(180, rotation);
        Assert.Equal(60, left, 6);
        Assert.Equal(42, top, 6);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(90, 90)]
    [InlineData(180, 180)]
    [InlineData(270, 270)]
    [InlineData(-90, 270)]
    [InlineData(450, 90)]
    [InlineData(45, 0)]
    [InlineData(double.NaN, 0)]
    public void NormalizeRotation_MapsOntoQuarterTurns(double stored, int expected)
    {
        Assert.Equal(expected, NiceLabel10Placement.NormalizeRotation(stored));
    }
}
