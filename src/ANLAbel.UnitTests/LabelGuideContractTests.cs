using ANLAbel.Core.Enums;
using ANLAbel.Core.Geometry;
using ANLAbel.Core.Models;
using ANLAbel.Core.Scene;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class LabelGuideContractTests
{
    [Fact]
    public void ClampAndHitTestUseThePhysicalDocumentSpace()
    {
        var guide = new LabelGuide
        {
            Id = "x-guide",
            Orientation = LabelGuideOrientation.Vertical,
            PositionMm = 24
        };

        Assert.Equal(20, LabelGuideContract.ClampPosition(24, LabelGuideOrientation.Vertical, 20, 10), precision: 6);
        Assert.Equal(0, LabelGuideContract.ClampPosition(double.NaN, LabelGuideOrientation.Horizontal, 20, 10), precision: 6);

        var nearest = LabelGuideContract.FindNearest(
            new[] { guide },
            LabelGuideOrientation.Vertical,
            positionMm: 24.1,
            zoom: 1,
            widthMm: 30,
            heightMm: 10,
            includeLocked: false);

        Assert.Same(guide, nearest);
    }

    [Fact]
    public void LockedAndHiddenGuidesAreNotDragTargets()
    {
        var locked = new LabelGuide
        {
            Id = "locked",
            Orientation = LabelGuideOrientation.Vertical,
            PositionMm = 10,
            IsLocked = true
        };
        var hidden = new LabelGuide
        {
            Id = "hidden",
            Orientation = LabelGuideOrientation.Vertical,
            PositionMm = 10,
            IsVisible = false
        };

        var nearest = LabelGuideContract.FindNearest(
            new[] { locked, hidden },
            LabelGuideOrientation.Vertical,
            positionMm: 10,
            zoom: 1,
            widthMm: 30,
            heightMm: 10,
            includeLocked: false);

        Assert.Null(nearest);
    }

    [Fact]
    public void AuthoringGuidesChangeDocumentHashButNotCompiledSceneHash()
    {
        var template = new LabelTemplate
        {
            Id = "guides",
            WidthMm = 80,
            HeightMm = 40
        };
        template.Objects.Add(new LabelObject
        {
            Id = "text",
            Type = ObjectType.Text,
            Text = "Hello",
            WidthMm = 20,
            HeightMm = 8
        });

        var before = SceneCompiler.Compile(DocumentSnapshot.Capture(template));
        template.Guides.Add(new LabelGuide
        {
            Id = "vertical-1",
            Orientation = LabelGuideOrientation.Vertical,
            PositionMm = 12.5
        });
        var after = SceneCompiler.Compile(DocumentSnapshot.Capture(template));

        Assert.NotEqual(before.DocumentHash, after.DocumentHash);
        Assert.Equal(before.SceneHash, after.SceneHash);
        Assert.Single(after.Snapshot.Guides);
        Assert.Equal(12.5, after.Snapshot.Guides[0].PositionMm, precision: 6);
    }

    [Fact]
    public void StableOrderDoesNotDependOnCollectionOrder()
    {
        var guides = new[]
        {
            new LabelGuide { Id = "b", Orientation = LabelGuideOrientation.Vertical, PositionMm = 5 },
            new LabelGuide { Id = "a", Orientation = LabelGuideOrientation.Vertical, PositionMm = 5 },
            new LabelGuide { Id = "h", Orientation = LabelGuideOrientation.Horizontal, PositionMm = 1 }
        };

        var order = LabelGuideContract.StableOrder(guides).Select(guide => guide.Id).ToArray();
        Assert.Equal(new[] { "a", "b", "h" }, order);

        var byPosition = LabelGuideContract.StableOrder(new[]
        {
            new LabelGuide { Id = "z", Orientation = LabelGuideOrientation.Vertical, PositionMm = 8 },
            new LabelGuide { Id = "m", Orientation = LabelGuideOrientation.Vertical, PositionMm = 2 }
        }).Select(guide => guide.Id).ToArray();
        Assert.Equal(new[] { "m", "z" }, byPosition);
    }

    [Fact]
    public void PublicConstantsAreTheAuthoredValues()
    {
        Assert.Equal(0, LabelGuideContract.MinimumPositionMm);
        Assert.Equal(8, LabelGuideContract.HitToleranceDip);
    }

    [Fact]
    public void Clamp_UsesWidthForVerticalAndHeightForHorizontal()
    {
        Assert.Equal(20, LabelGuideContract.ClampPosition(50, LabelGuideOrientation.Vertical, 20, 8), precision: 6);
        Assert.Equal(8, LabelGuideContract.ClampPosition(50, LabelGuideOrientation.Horizontal, 20, 8), precision: 6);
        Assert.Equal(0, LabelGuideContract.ClampPosition(-4, LabelGuideOrientation.Vertical, 20, 8), precision: 6);
        Assert.Equal(12, LabelGuideContract.ClampPosition(12, LabelGuideOrientation.Vertical, 20, 8), precision: 6);
    }

    [Theory]
    [InlineData(double.NaN, 20)]
    [InlineData(double.PositiveInfinity, 20)]
    [InlineData(10, 0)]
    [InlineData(10, -5)]
    [InlineData(10, double.NaN)]
    [InlineData(10, double.NegativeInfinity)]
    public void Clamp_ReturnsZero_WhenPositionOrLengthIsNotUsable(double positionMm, double lengthMm)
    {
        Assert.Equal(0, LabelGuideContract.ClampPosition(positionMm, LabelGuideOrientation.Vertical, lengthMm, 40));
        Assert.Equal(0, LabelGuideContract.ClampPosition(positionMm, LabelGuideOrientation.Horizontal, 40, lengthMm));
    }

    [Fact]
    public void Clamp_RoundsToThreeDecimalsAwayFromZero()
    {
        Assert.Equal(1.235, LabelGuideContract.ClampPosition(1.2345, LabelGuideOrientation.Vertical, 20, 10), precision: 6);
        Assert.Equal(1.234, LabelGuideContract.ClampPosition(1.2344, LabelGuideOrientation.Vertical, 20, 10), precision: 6);
    }

    [Fact]
    public void IsValid_RequiresIdOrientationAndInRangePosition()
    {
        var vertical = new LabelGuide
        {
            Id = "v",
            Orientation = LabelGuideOrientation.Vertical,
            PositionMm = 20
        };
        Assert.True(LabelGuideContract.IsValid(vertical, 20, 10));
        Assert.False(LabelGuideContract.IsValid(vertical, 19.999, 10));

        var horizontal = new LabelGuide
        {
            Id = "h",
            Orientation = LabelGuideOrientation.Horizontal,
            PositionMm = 10
        };
        Assert.True(LabelGuideContract.IsValid(horizontal, 20, 10));
        Assert.False(LabelGuideContract.IsValid(horizontal, 20, 9.999));

        vertical.Orientation = (LabelGuideOrientation)99;
        Assert.False(LabelGuideContract.IsValid(vertical, 40, 40));
        Assert.Throws<ArgumentNullException>(() => LabelGuideContract.IsValid(null!, 20, 10));
    }

    [Fact]
    public void FindNearest_UsesDipToleranceAndOrdinalTieBreak()
    {
        var closer = new LabelGuide { Id = "z", Orientation = LabelGuideOrientation.Vertical, PositionMm = 10 };
        var farther = new LabelGuide { Id = "a", Orientation = LabelGuideOrientation.Vertical, PositionMm = 10.3 };
        var sameB = new LabelGuide { Id = "b", Orientation = LabelGuideOrientation.Vertical, PositionMm = 12 };
        var sameA = new LabelGuide { Id = "a2", Orientation = LabelGuideOrientation.Vertical, PositionMm = 12 };
        sameA.Id = "a";

        Assert.Same(closer, LabelGuideContract.FindNearest(
            new[] { farther, closer },
            LabelGuideOrientation.Vertical,
            positionMm: 10,
            zoom: 1,
            widthMm: 40,
            heightMm: 10));

        var tied = LabelGuideContract.FindNearest(
            new[] { sameB, sameA },
            LabelGuideOrientation.Vertical,
            positionMm: 12,
            zoom: 1,
            widthMm: 40,
            heightMm: 10);
        Assert.Same(sameA, tied);

        var origin = new LabelGuide { Id = "origin", Orientation = LabelGuideOrientation.Vertical, PositionMm = 0 };
        var atEdge = LabelGuideContract.FindNearest(
            new[] { origin },
            LabelGuideOrientation.Vertical,
            positionMm: MmConverter.DipToMm(LabelGuideContract.HitToleranceDip),
            zoom: 1,
            widthMm: 40,
            heightMm: 10);
        var beyond = LabelGuideContract.FindNearest(
            new[] { origin },
            LabelGuideOrientation.Vertical,
            positionMm: MmConverter.DipToMm(LabelGuideContract.HitToleranceDip) + 0.01,
            zoom: 1,
            widthMm: 40,
            heightMm: 10);
        Assert.Same(origin, atEdge);
        Assert.Null(beyond);
    }

    [Fact]
    public void FindNearest_FiltersOrientationLockedHiddenAndNonFiniteProbe()
    {
        var locked = new LabelGuide
        {
            Id = "locked",
            Orientation = LabelGuideOrientation.Vertical,
            PositionMm = 10,
            IsLocked = true
        };
        var horizontal = new LabelGuide
        {
            Id = "horiz",
            Orientation = LabelGuideOrientation.Horizontal,
            PositionMm = 10
        };

        Assert.Same(locked, LabelGuideContract.FindNearest(
            new[] { locked },
            LabelGuideOrientation.Vertical,
            10,
            zoom: 1,
            widthMm: 30,
            heightMm: 20,
            includeLocked: true));
        Assert.Null(LabelGuideContract.FindNearest(
            new[] { locked },
            LabelGuideOrientation.Vertical,
            10,
            zoom: 1,
            widthMm: 30,
            heightMm: 20,
            includeLocked: false));
        Assert.Null(LabelGuideContract.FindNearest(
            new[] { horizontal },
            LabelGuideOrientation.Vertical,
            10,
            zoom: 1,
            widthMm: 30,
            heightMm: 20));
        Assert.Null(LabelGuideContract.FindNearest(
            new[] { locked },
            LabelGuideOrientation.Vertical,
            double.NaN,
            zoom: 1,
            widthMm: 30,
            heightMm: 20,
            includeLocked: true));
        Assert.Throws<ArgumentNullException>(() => LabelGuideContract.FindNearest(
            null!,
            LabelGuideOrientation.Vertical,
            10,
            1,
            30,
            20));
    }

    [Fact]
    public void FindNearest_NegativeZoomStillUsesMinimumScale()
    {
        var guide = new LabelGuide
        {
            Id = "near",
            Orientation = LabelGuideOrientation.Horizontal,
            PositionMm = 5
        };

        var found = LabelGuideContract.FindNearest(
            new[] { guide },
            LabelGuideOrientation.Horizontal,
            positionMm: 5.2,
            zoom: -1,
            widthMm: 30,
            heightMm: 20);
        Assert.Same(guide, found);
    }

    [Fact]
    public void StableOrder_NullThrows()
    {
        Assert.Throws<ArgumentNullException>(() => LabelGuideContract.StableOrder(null!));
    }
}
