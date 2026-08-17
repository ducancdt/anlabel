using ANLAbel.Core.Enums;
using ANLAbel.Core.Geometry;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class ResizeGeometryContractTests
{
    [Theory]
    [InlineData(0, ResizeEdge.Left, TransformedBoundsEdge.Left)]
    [InlineData(90, ResizeEdge.Left, TransformedBoundsEdge.Top)]
    [InlineData(180, ResizeEdge.Left, TransformedBoundsEdge.Right)]
    [InlineData(270, ResizeEdge.Left, TransformedBoundsEdge.Bottom)]
    [InlineData(90, ResizeEdge.Top, TransformedBoundsEdge.Right)]
    public void MapsAuthoredEdgesToTheDisplayedWorldEdge(
        int rotation,
        ResizeEdge localEdge,
        TransformedBoundsEdge expected)
    {
        Assert.Equal(expected, ResizeGeometryContract.MapToWorldEdge(rotation, localEdge));
    }

    [Fact]
    public void LeftResizePreservesOppositeEdgeAndSnapsRotatedTop()
    {
        var frame = new ResizeFrame(10, 20, 30, 10);
        var before = TransformedBoundsContract.GetBounds(frame.XMm, frame.YMm, frame.WidthMm, frame.HeightMm, 90);
        var targetTop = before.Top - 2;

        var adjusted = ResizeGeometryContract.ApplyWorldEdgeSnap(frame, 90, ResizeEdge.Left, targetTop);
        var after = TransformedBoundsContract.GetBounds(adjusted.XMm, adjusted.YMm, adjusted.WidthMm, adjusted.HeightMm, 90);

        Assert.Equal(frame.XMm + frame.WidthMm, adjusted.XMm + adjusted.WidthMm, 6);
        Assert.Equal(targetTop, after.Top, 5);
    }

    [Fact]
    public void BottomResizeKeepsTopFrameAnchorForZeroRotation()
    {
        var frame = new ResizeFrame(10, 20, 30, 10);
        var adjusted = ResizeGeometryContract.ApplyWorldEdgeSnap(frame, 0, ResizeEdge.Bottom, 35);

        Assert.Equal(frame.YMm, adjusted.YMm, 6);
        Assert.Equal(15, adjusted.HeightMm, 6);
        Assert.Equal(35, ResizeGeometryContract.GetWorldEdgePosition(adjusted, 0, ResizeEdge.Bottom), 6);
    }

    [Fact]
    public void RightResizePreservesOppositeEdgeAndSnapsRotatedTopAt270Degrees()
    {
        var frame = new ResizeFrame(10, 20, 30, 10);
        var before = TransformedBoundsContract.GetBounds(frame.XMm, frame.YMm, frame.WidthMm, frame.HeightMm, 270);
        var targetTop = before.Top + 3;

        var adjusted = ResizeGeometryContract.ApplyWorldEdgeSnap(frame, 270, ResizeEdge.Right, targetTop);
        var after = TransformedBoundsContract.GetBounds(adjusted.XMm, adjusted.YMm, adjusted.WidthMm, adjusted.HeightMm, 270);

        Assert.Equal(frame.XMm, adjusted.XMm, 6);
        Assert.Equal(targetTop, after.Top, 5);
    }

    [Fact]
    public void ShiftCornerResizePreservesAuthoredAspectRatioAndOppositeEdges()
    {
        var source = new ResizeFrame(10, 20, 20, 10);
        var proposed = new ResizeFrame(8, 16, 22, 14);

        var adjusted = ResizeModifierContract.Apply(
            source,
            proposed,
            ResizeHandle.TopLeft,
            ResizeModifierFlags.PreserveAspectRatio);

        Assert.Equal(2, adjusted.XMm, 6);
        Assert.Equal(16, adjusted.YMm, 6);
        Assert.Equal(28, adjusted.WidthMm, 6);
        Assert.Equal(14, adjusted.HeightMm, 6);
        Assert.Equal(2, adjusted.WidthMm / adjusted.HeightMm, 6);
        Assert.Equal(source.RightMm, adjusted.RightMm, 6);
    }

    [Fact]
    public void ControlResizeExpandsAroundTheOriginalCentre()
    {
        var source = new ResizeFrame(10, 20, 20, 10);
        var proposed = new ResizeFrame(10, 20, 30, 10);

        var adjusted = ResizeModifierContract.Apply(
            source,
            proposed,
            ResizeHandle.Right,
            ResizeModifierFlags.ResizeFromCenter);

        Assert.Equal(0, adjusted.XMm, 6);
        Assert.Equal(40, adjusted.WidthMm, 6);
        Assert.Equal(source.XMm + source.WidthMm / 2, adjusted.XMm + adjusted.WidthMm / 2, 6);
        Assert.Equal(source.YMm, adjusted.YMm, 6);
    }

    [Fact]
    public void GroupShiftResizeUsesTheSameAspectContract()
    {
        var source = new GroupResizeFrame(5, 5, 30, 10);
        var proposed = new GroupResizeFrame(5, 5, 36, 12);

        var adjusted = ResizeModifierContract.Apply(
            source,
            proposed,
            ResizeHandle.BottomRight,
            ResizeModifierFlags.PreserveAspectRatio);

        Assert.Equal(36, adjusted.WidthMm, 6);
        Assert.Equal(12, adjusted.HeightMm, 6);
        Assert.Equal(source.XMm, adjusted.XMm, 6);
        Assert.Equal(source.YMm, adjusted.YMm, 6);
        Assert.Equal(3, adjusted.WidthMm / adjusted.HeightMm, 6);
    }
}
