using ANLAbel.Core.Geometry;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class GroupResizeGeometryContractTests
{
    [Fact]
    public void RightBottomResizeKeepsTopLeftAndMapsCorners()
    {
        var source = new GroupResizeFrame(10, 20, 40, 20);

        var ok = GroupResizeGeometryContract.TryResize(
            source,
            deltaXMm: 0,
            deltaYMm: 0,
            deltaWidthMm: 20,
            deltaHeightMm: 10,
            minimumWidthMm: 1,
            minimumHeightMm: 1,
            out var target);

        Assert.True(ok);
        Assert.Equal(10, target.XMm, 6);
        Assert.Equal(20, target.YMm, 6);
        Assert.Equal(60, target.WidthMm, 6);
        Assert.Equal(30, target.HeightMm, 6);

        var transform = new GroupResizeTransform(source, target);
        Assert.Equal(10, transform.MapX(10), 6);
        Assert.Equal(20, transform.MapY(20), 6);
        Assert.Equal(70, transform.MapX(50), 6);
        Assert.Equal(50, transform.MapY(40), 6);
    }

    [Fact]
    public void LeftTopResizeKeepsOppositeCornerFixed()
    {
        var source = new GroupResizeFrame(10, 20, 40, 20);

        var ok = GroupResizeGeometryContract.TryResize(
            source,
            deltaXMm: 5,
            deltaYMm: 4,
            deltaWidthMm: -5,
            deltaHeightMm: -4,
            minimumWidthMm: 1,
            minimumHeightMm: 1,
            out var target);

        Assert.True(ok);
        Assert.Equal(15, target.XMm, 6);
        Assert.Equal(24, target.YMm, 6);
        Assert.Equal(35, target.WidthMm, 6);
        Assert.Equal(16, target.HeightMm, 6);
        Assert.Equal(source.RightMm, target.RightMm, 6);
        Assert.Equal(source.BottomMm, target.BottomMm, 6);
    }

    [Fact]
    public void MinimumSizeKeepsMovingEdgeFromCrossingAnchor()
    {
        var source = new GroupResizeFrame(10, 20, 40, 20);

        var ok = GroupResizeGeometryContract.TryResize(
            source,
            deltaXMm: 100,
            deltaYMm: 0,
            deltaWidthMm: -100,
            deltaHeightMm: 0,
            minimumWidthMm: 5,
            minimumHeightMm: 1,
            out var target);

        Assert.True(ok);
        Assert.Equal(source.RightMm, target.RightMm, 6);
        Assert.Equal(5, target.WidthMm, 6);
        Assert.Equal(45, target.XMm, 6);
    }

    [Fact]
    public void CanvasClampPreservesFrameInsideArtboard()
    {
        var clamped = GroupResizeGeometryContract.ClampToCanvas(
            new GroupResizeFrame(-5, -4, 40, 30),
            canvasWidthMm: 50,
            canvasHeightMm: 40);

        Assert.Equal(0, clamped.XMm, 6);
        Assert.Equal(0, clamped.YMm, 6);
        Assert.Equal(40, clamped.WidthMm, 6);
        Assert.Equal(30, clamped.HeightMm, 6);
    }

    [Theory]
    [InlineData(double.NaN, 0, 10, 10, false)]
    [InlineData(0, double.PositiveInfinity, 10, 10, false)]
    [InlineData(0, 0, double.NaN, 10, false)]
    [InlineData(0, 0, 10, double.NegativeInfinity, false)]
    [InlineData(0, 0, 0, 10, false)]
    [InlineData(0, 0, 10, 0, false)]
    [InlineData(0, 0, 10, 10, true)]
    public void IsValidRejectsNonFiniteAndNonPositiveFrames(
        double xMm,
        double yMm,
        double widthMm,
        double heightMm,
        bool expected)
    {
        Assert.Equal(
            expected,
            GroupResizeGeometryContract.IsValid(new GroupResizeFrame(xMm, yMm, widthMm, heightMm)));
    }

    [Fact]
    public void ZeroSizedSourceYieldsIdentityScale()
    {
        var transform = new GroupResizeTransform(
            new GroupResizeFrame(0, 0, 0, 0),
            new GroupResizeFrame(0, 0, 50, 40));

        Assert.Equal(1, transform.ScaleX, 6);
        Assert.Equal(1, transform.ScaleY, 6);
    }

    [Fact]
    public void TryResizeFailsClosedOnInvalidSource()
    {
        var ok = GroupResizeGeometryContract.TryResize(
            new GroupResizeFrame(0, 0, 0, 10),
            1, 1, 1, 1, 1, 1,
            out var target);

        Assert.False(ok);
        Assert.Equal(default, target);
    }

    [Theory]
    [InlineData(double.NaN, 0, 0, 0)]
    [InlineData(0, double.PositiveInfinity, 0, 0)]
    [InlineData(0, 0, double.NaN, 0)]
    [InlineData(0, 0, 0, double.NegativeInfinity)]
    public void TryResizeFailsClosedOnNonFiniteDelta(
        double deltaXMm,
        double deltaYMm,
        double deltaWidthMm,
        double deltaHeightMm)
    {
        var ok = GroupResizeGeometryContract.TryResize(
            new GroupResizeFrame(10, 20, 40, 20),
            deltaXMm, deltaYMm, deltaWidthMm, deltaHeightMm,
            minimumWidthMm: 1,
            minimumHeightMm: 1,
            out var target);

        Assert.False(ok);
        Assert.Equal(default, target);
    }

    [Fact]
    public void NonFiniteMinimumSizeFallsBackToTinyFloor()
    {
        var ok = GroupResizeGeometryContract.TryResize(
            new GroupResizeFrame(10, 20, 40, 20),
            deltaXMm: 100,
            deltaYMm: 0,
            deltaWidthMm: -100,
            deltaHeightMm: 0,
            minimumWidthMm: double.NaN,
            minimumHeightMm: 1,
            out var target);

        Assert.True(ok);
        Assert.Equal(0.001, target.WidthMm, 6);
    }

    [Fact]
    public void NonFiniteMinimumHeightFallsBackToTinyFloor()
    {
        var ok = GroupResizeGeometryContract.TryResize(
            new GroupResizeFrame(10, 20, 40, 20),
            deltaXMm: 0,
            deltaYMm: 100,
            deltaWidthMm: 0,
            deltaHeightMm: -100,
            minimumWidthMm: 1,
            minimumHeightMm: double.NaN,
            out var target);

        Assert.True(ok);
        Assert.Equal(0.001, target.HeightMm, 6);
    }

    [Fact]
    public void HeightMinimumKeepsMovingEdgeFromCrossingAnchor()
    {
        var source = new GroupResizeFrame(10, 20, 40, 20);

        var ok = GroupResizeGeometryContract.TryResize(
            source,
            deltaXMm: 0,
            deltaYMm: 100,
            deltaWidthMm: 0,
            deltaHeightMm: -100,
            minimumWidthMm: 1,
            minimumHeightMm: 5,
            out var target);

        Assert.True(ok);
        Assert.Equal(source.BottomMm, target.BottomMm, 6);
        Assert.Equal(5, target.HeightMm, 6);
        Assert.Equal(35, target.YMm, 6);
    }

    [Fact]
    public void ShrinkFromRightKeepsLeftAnchorWhenClampedToMinimum()
    {
        var source = new GroupResizeFrame(10, 20, 40, 20);

        var ok = GroupResizeGeometryContract.TryResize(
            source,
            deltaXMm: 0,
            deltaYMm: 0,
            deltaWidthMm: -100,
            deltaHeightMm: 0,
            minimumWidthMm: 5,
            minimumHeightMm: 1,
            out var target);

        Assert.True(ok);
        Assert.Equal(source.XMm, target.XMm, 6);
        Assert.Equal(5, target.WidthMm, 6);
    }

    [Fact]
    public void ShrinkFromBottomKeepsTopAnchorWhenClampedToMinimum()
    {
        var source = new GroupResizeFrame(10, 20, 40, 20);

        var ok = GroupResizeGeometryContract.TryResize(
            source,
            deltaXMm: 0,
            deltaYMm: 0,
            deltaWidthMm: 0,
            deltaHeightMm: -100,
            minimumWidthMm: 1,
            minimumHeightMm: 5,
            out var target);

        Assert.True(ok);
        Assert.Equal(source.YMm, target.YMm, 6);
        Assert.Equal(5, target.HeightMm, 6);
    }

    [Fact]
    public void ClampReturnsFrameUnchangedForInvalidOrNonFiniteCanvas()
    {
        var frame = new GroupResizeFrame(5, 5, 20, 10);

        Assert.Equal(frame, GroupResizeGeometryContract.ClampToCanvas(frame, double.NaN, 40));
        Assert.Equal(
            new GroupResizeFrame(0, 0, 0, 10),
            GroupResizeGeometryContract.ClampToCanvas(new GroupResizeFrame(0, 0, 0, 10), 50, 40));
    }

    [Fact]
    public void ClampFillsCanvasWhenFrameIsLargerThanArtboard()
    {
        var clamped = GroupResizeGeometryContract.ClampToCanvas(
            new GroupResizeFrame(5, 5, 100, 80),
            canvasWidthMm: 50,
            canvasHeightMm: 40);

        Assert.Equal(0, clamped.XMm, 6);
        Assert.Equal(0, clamped.YMm, 6);
        Assert.Equal(50, clamped.WidthMm, 6);
        Assert.Equal(40, clamped.HeightMm, 6);
    }

    [Fact]
    public void ClampShiftsOverflowingRightAndBottomEdgesInside()
    {
        var clamped = GroupResizeGeometryContract.ClampToCanvas(
            new GroupResizeFrame(90, 95, 20, 10),
            canvasWidthMm: 100,
            canvasHeightMm: 100);

        Assert.Equal(80, clamped.XMm, 6);
        Assert.Equal(90, clamped.YMm, 6);
        Assert.Equal(20, clamped.WidthMm, 6);
        Assert.Equal(10, clamped.HeightMm, 6);
    }

    [Fact]
    public void ClampEnforcesMinimumSizeAndPullsFrameBackInside()
    {
        var clamped = GroupResizeGeometryContract.ClampToCanvas(
            new GroupResizeFrame(2, 2, 3, 3),
            canvasWidthMm: 5,
            canvasHeightMm: 5,
            minimumWidthMm: 4,
            minimumHeightMm: 4);

        Assert.Equal(4, clamped.WidthMm, 6);
        Assert.Equal(4, clamped.HeightMm, 6);
        Assert.Equal(1, clamped.XMm, 6);
        Assert.Equal(1, clamped.YMm, 6);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(90)]
    [InlineData(180)]
    [InlineData(270)]
    public void RotatedMemberReconstructsAuthoredFrameFromMappedDisplayBounds(int rotation)
    {
        var groupSource = new GroupResizeFrame(0, 0, 100, 80);
        var groupTarget = new GroupResizeFrame(0, 0, 200, 120);
        var transform = new GroupResizeTransform(groupSource, groupTarget);
        var authored = new ResizeFrame(20, 10, 12, 8);
        var display = TransformedBoundsContract.GetBounds(
            authored.XMm,
            authored.YMm,
            authored.WidthMm,
            authored.HeightMm,
            rotation);

        var mapped = GroupResizeGeometryContract.MapBounds(transform, display);
        var reconstructed = GroupResizeGeometryContract.ToAuthoredFrame(mapped, rotation);
        var expectedDisplayWidth = rotation is 90 or 270 ? authored.HeightMm * 2 : authored.WidthMm * 2;
        var expectedDisplayHeight = rotation is 90 or 270 ? authored.WidthMm * 1.5 : authored.HeightMm * 1.5;

        Assert.Equal(expectedDisplayWidth, mapped.Width, 6);
        Assert.Equal(expectedDisplayHeight, mapped.Height, 6);
        var expectedAuthoredWidth = rotation is 90 or 270
            ? authored.WidthMm * 1.5
            : authored.WidthMm * 2;
        var expectedAuthoredHeight = rotation is 90 or 270
            ? authored.HeightMm * 2
            : authored.HeightMm * 1.5;
        Assert.Equal(expectedAuthoredWidth, reconstructed.WidthMm, 6);
        Assert.Equal(expectedAuthoredHeight, reconstructed.HeightMm, 6);
        Assert.Equal(display.CenterX * 2, reconstructed.XMm + reconstructed.WidthMm / 2, 6);
        Assert.Equal(display.CenterY * 1.5, reconstructed.YMm + reconstructed.HeightMm / 2, 6);
    }
}
