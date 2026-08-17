using ANLAbel.Core.Enums;
using ANLAbel.Core.Geometry;
using ANLAbel.Core.Models;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class LabelArrangeEngineTests
{
    [Fact]
    public void Align_SelectionFrameLeft_UsesDeterministicMinimumEdge()
    {
        var left = Text(20, 2, 10, 4, "left");
        var right = Text(55, 8, 15, 4, "right");

        var result = LabelArrangeEngine.Align(
            new[] { left, right },
            keyObject: right,
            LabelAlignmentMode.Left,
            LabelArrangeReferenceMode.SelectionBounds);

        Assert.True(result.Succeeded);
        Assert.True(result.Changed);
        Assert.Equal(20, left.XMm, precision: 2);
        Assert.Equal(20, right.XMm, precision: 2);
    }

    [Fact]
    public void Align_KeyObjectCenter_PreservesKeyAndMovesOnlyPeers()
    {
        var key = Text(42, 12, 20, 6, "key");
        var peer = Text(5, 2, 10, 4, "peer");

        var result = LabelArrangeEngine.Align(
            new[] { key, peer },
            key,
            LabelAlignmentMode.HorizontalCenter,
            LabelArrangeReferenceMode.KeyObject);

        Assert.True(result.Succeeded);
        Assert.Equal(42, key.XMm, precision: 2);
        Assert.Equal(47, peer.XMm, precision: 2);
    }

    [Fact]
    public void Distribute_HorizontalGaps_UsesExactEqualGapForMixedWidths()
    {
        var first = Text(0, 0, 10, 5, "a");
        var middle = Text(17, 0, 4, 5, "b");
        var last = Text(40, 0, 20, 5, "c");

        var result = LabelArrangeEngine.Distribute(
            new[] { last, middle, first },
            LabelDistributionMode.HorizontalGaps);

        Assert.True(result.Succeeded);
        var firstBounds = LabelArrangeEngine.GetBounds(first);
        var middleBounds = LabelArrangeEngine.GetBounds(middle);
        var lastBounds = LabelArrangeEngine.GetBounds(last);
        Assert.Equal(middleBounds.Left - firstBounds.Right, lastBounds.Left - middleBounds.Right, precision: 2);
        Assert.Equal(23, middle.XMm, precision: 2);
    }

    [Fact]
    public void Distribute_LockedSelection_FailsWithoutMutatingGeometry()
    {
        var first = Text(0, 0, 10, 5, "a");
        var middle = Text(20, 0, 10, 5, "b");
        var last = Text(40, 0, 10, 5, "c");
        middle.IsLocked = true;

        var result = LabelArrangeEngine.Distribute(
            new[] { first, middle, last },
            LabelDistributionMode.HorizontalCenters);

        Assert.False(result.Succeeded);
        Assert.Equal(20, middle.XMm, precision: 2);
        Assert.Contains("Unlock", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    private static LabelObject Text(double x, double y, double width, double height, string id)
    {
        return new LabelObject
        {
            Id = id,
            Type = ObjectType.Text,
            XMm = x,
            YMm = y,
            WidthMm = width,
            HeightMm = height
        };
    }
}
