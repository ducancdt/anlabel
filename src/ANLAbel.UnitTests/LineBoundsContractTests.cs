using ANLAbel.Core.Enums;
using ANLAbel.Core.Geometry;
using ANLAbel.Core.Models;
using ANLAbel.Core.Scene;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class LineBoundsContractTests
{
    [Fact]
    public void StrokeExtendsTheSafetyHullByHalfItsPhysicalWidth()
    {
        var line = new LabelObject
        {
            Type = ObjectType.Line,
            XMm = 1,
            YMm = 5,
            LineEndXMm = 29.9,
            LineEndYMm = 5,
            Style = { OutlineStyle = OutlineStyle.Solid, BorderThicknessMm = 0.4 }
        };

        var bounds = LineBoundsContract.GetBounds(line);

        Assert.Equal(0.8, bounds.Left, 6);
        Assert.Equal(30.1, bounds.Right, 6);
        Assert.Equal(4.8, bounds.Top, 6);
        Assert.Equal(5.2, bounds.Bottom, 6);
    }

    [Fact]
    public void NoOutlineDoesNotInflateTheEndpointBounds()
    {
        var bounds = LineBoundsContract.GetBounds(1, 5, 29.9, 5, OutlineStyle.None, 10);

        Assert.Equal(1, bounds.Left, 6);
        Assert.Equal(29.9, bounds.Right, 6);
        Assert.Equal(5, bounds.Top, 6);
        Assert.Equal(5, bounds.Bottom, 6);
    }

    [Fact]
    public void ArrangeEngineUsesTheSameLineSafetyHull()
    {
        var line = new LabelObject
        {
            Type = ObjectType.Line,
            XMm = 2,
            YMm = 3,
            LineEndXMm = 12,
            LineEndYMm = 8,
            Style = { BorderThicknessMm = 0.6 }
        };

        Assert.Equal(LineBoundsContract.GetBounds(line), LabelArrangeEngine.GetBounds(line));
    }

    [Fact]
    public void GetBounds_ThrowsOnNullInputs()
    {
        Assert.Throws<ArgumentNullException>(() => LineBoundsContract.GetBounds((LabelObject)null!));
        Assert.Throws<ArgumentNullException>(() => LineBoundsContract.GetBounds((SceneObjectSnapshot)null!));
    }

    [Fact]
    public void ZeroZeroEndpoints_FallBackToWidthAndHeight()
    {
        var line = new LabelObject
        {
            Type = ObjectType.Line,
            XMm = 2,
            YMm = 3,
            WidthMm = 10,
            HeightMm = 5,
            LineEndXMm = 0,
            LineEndYMm = 0,
            Style = { OutlineStyle = OutlineStyle.None, BorderThicknessMm = 2 }
        };

        var bounds = LineBoundsContract.GetBounds(line);
        Assert.Equal(2, bounds.Left, 6);
        Assert.Equal(3, bounds.Top, 6);
        Assert.Equal(12, bounds.Right, 6);
        Assert.Equal(8, bounds.Bottom, 6);
    }

    [Fact]
    public void SingleZeroCoordinate_IsARealEndpointNotAFallback()
    {
        var onlyXZero = new LabelObject
        {
            Type = ObjectType.Line,
            XMm = 2,
            YMm = 3,
            WidthMm = 10,
            HeightMm = 5,
            LineEndXMm = 0,
            LineEndYMm = 5,
            Style = { OutlineStyle = OutlineStyle.None }
        };
        var onlyYZero = new LabelObject
        {
            Type = ObjectType.Line,
            XMm = 2,
            YMm = 3,
            WidthMm = 10,
            HeightMm = 5,
            LineEndXMm = 10,
            LineEndYMm = 0,
            Style = { OutlineStyle = OutlineStyle.None }
        };

        var xZero = LineBoundsContract.GetBounds(onlyXZero);
        Assert.Equal(0, xZero.Left, 6);
        Assert.Equal(2, xZero.Right, 6);
        Assert.Equal(3, xZero.Top, 6);
        Assert.Equal(5, xZero.Bottom, 6);

        var yZero = LineBoundsContract.GetBounds(onlyYZero);
        Assert.Equal(2, yZero.Left, 6);
        Assert.Equal(10, yZero.Right, 6);
        Assert.Equal(0, yZero.Top, 6);
        Assert.Equal(3, yZero.Bottom, 6);

        var snapFallback = LineBoundsContract.GetBounds(SceneObjectSnapshot.Capture(new LabelObject
        {
            Type = ObjectType.Line,
            XMm = 2,
            YMm = 3,
            WidthMm = 10,
            HeightMm = 5,
            LineEndXMm = 0,
            LineEndYMm = 0,
            Style = { OutlineStyle = OutlineStyle.None, BorderThicknessMm = 2 }
        }));
        Assert.Equal(2, snapFallback.Left, 6);
        Assert.Equal(3, snapFallback.Top, 6);
        Assert.Equal(12, snapFallback.Right, 6);
        Assert.Equal(8, snapFallback.Bottom, 6);

        var snapXZero = LineBoundsContract.GetBounds(SceneObjectSnapshot.Capture(onlyXZero));
        Assert.Equal(0, snapXZero.Left, 6);
        Assert.Equal(2, snapXZero.Right, 6);
        Assert.Equal(3, snapXZero.Top, 6);
        Assert.Equal(5, snapXZero.Bottom, 6);

        var snapYZero = LineBoundsContract.GetBounds(SceneObjectSnapshot.Capture(onlyYZero));
        Assert.Equal(2, snapYZero.Left, 6);
        Assert.Equal(10, snapYZero.Right, 6);
        Assert.Equal(0, snapYZero.Top, 6);
        Assert.Equal(3, snapYZero.Bottom, 6);
    }

    [Fact]
    public void SnapshotOverload_MatchesCapturedObject()
    {
        var line = new LabelObject
        {
            Type = ObjectType.Line,
            XMm = 1,
            YMm = 5,
            LineEndXMm = 29.9,
            LineEndYMm = 5,
            Style = { OutlineStyle = OutlineStyle.Solid, BorderThicknessMm = 0.4 }
        };
        var fromObject = LineBoundsContract.GetBounds(line);
        var fromSnapshot = LineBoundsContract.GetBounds(SceneObjectSnapshot.Capture(line));

        Assert.Equal(fromObject, fromSnapshot);
        Assert.Equal(0.8, fromSnapshot.Left, 6);
        Assert.Equal(30.1, fromSnapshot.Right, 6);
    }

    [Fact]
    public void ReversedAndVerticalEndpoints_UseMinMaxHull()
    {
        var reversed = LineBoundsContract.GetBounds(29.9, 5, 1, 5, OutlineStyle.Solid, 0.4);
        Assert.Equal(0.8, reversed.Left, 6);
        Assert.Equal(30.1, reversed.Right, 6);
        Assert.Equal(4.8, reversed.Top, 6);
        Assert.Equal(5.2, reversed.Bottom, 6);

        var vertical = LineBoundsContract.GetBounds(5, 1, 5, 10, OutlineStyle.Solid, 0.4);
        Assert.Equal(4.8, vertical.Left, 6);
        Assert.Equal(5.2, vertical.Right, 6);
        Assert.Equal(0.8, vertical.Top, 6);
        Assert.Equal(10.2, vertical.Bottom, 6);
    }

    [Theory]
    [InlineData(OutlineStyle.Solid)]
    [InlineData(OutlineStyle.Dash)]
    [InlineData(OutlineStyle.Dot)]
    public void VisibleOutlineStyles_InflateByHalfStroke(OutlineStyle outline)
    {
        var bounds = LineBoundsContract.GetBounds(1, 5, 29.9, 5, outline, 0.4);
        Assert.Equal(0.8, bounds.Left, 6);
        Assert.Equal(30.1, bounds.Right, 6);
        Assert.Equal(4.8, bounds.Top, 6);
        Assert.Equal(5.2, bounds.Bottom, 6);
    }

    [Theory]
    [InlineData(-0.4)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void NonPositiveOrNonFiniteStroke_DoesNotInflate(double strokeMm)
    {
        var bounds = LineBoundsContract.GetBounds(1, 5, 29.9, 5, OutlineStyle.Solid, strokeMm);
        Assert.Equal(1, bounds.Left, 6);
        Assert.Equal(29.9, bounds.Right, 6);
        Assert.Equal(5, bounds.Top, 6);
        Assert.Equal(5, bounds.Bottom, 6);
    }

    [Theory]
    [InlineData(double.NaN, 5, 10, 5)]
    [InlineData(1, double.PositiveInfinity, 10, 5)]
    [InlineData(1, 5, double.NegativeInfinity, 5)]
    [InlineData(1, 5, 10, double.NaN)]
    public void NonFiniteEndpoint_ReturnsNaNHull(double startX, double startY, double endX, double endY)
    {
        var bounds = LineBoundsContract.GetBounds(startX, startY, endX, endY, OutlineStyle.Solid, 0.4);
        Assert.True(double.IsNaN(bounds.Left));
        Assert.True(double.IsNaN(bounds.Top));
        Assert.True(double.IsNaN(bounds.Right));
        Assert.True(double.IsNaN(bounds.Bottom));
    }
}
