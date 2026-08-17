using ANLAbel.Core.Geometry;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class DeviceDotQuantizerTests
{
    [Fact]
    public void RoundTripPreservesIntegerDotsAcrossIndustrialResolutions()
    {
        foreach (var dpi in new[] { 203, 300, 305, 600, 609 })
        {
            foreach (var dots in new[] { -11, 0, 1, 2, 17, 203 })
            {
                var dip = DeviceDotQuantizer.DotsToDip(dots, dpi);
                Assert.Equal(dots, DeviceDotQuantizer.DipToDots(dip, dpi));
            }
        }
    }

    [Fact]
    public void NonSquareDpiUsesIndependentAxes()
    {
        var x = DeviceDotQuantizer.DipToDots(96, 203);
        var y = DeviceDotQuantizer.DipToDots(96, 609);

        Assert.Equal(203, x);
        Assert.Equal(609, y);
        Assert.NotEqual(DeviceDotQuantizer.DotSizeDip(203), DeviceDotQuantizer.DotSizeDip(609));
    }

    [Fact]
    public void ModuleBoundariesAreMonotonicAndCoverTargetWidth()
    {
        const int totalModules = 37;
        const int totalWidthDots = 101;
        var previous = 0;

        for (var index = 0; index <= totalModules; index++)
        {
            var boundary = DeviceDotQuantizer.QuantizeModuleBoundary(index, totalModules, totalWidthDots);
            Assert.InRange(boundary, previous, totalWidthDots);
            previous = boundary;
        }

        Assert.Equal(0, DeviceDotQuantizer.QuantizeModuleBoundary(0, totalModules, totalWidthDots));
        Assert.Equal(totalWidthDots, DeviceDotQuantizer.QuantizeModuleBoundary(totalModules, totalModules, totalWidthDots));
    }

    [Fact]
    public void InvalidDpiAndModuleInputsFailClosed()
    {
        AssertNamedOutOfRange("dpi", "DPI must be positive", () => DeviceDotQuantizer.DotSizeDip(0));
        AssertNamedOutOfRange("dpi", "DPI must be positive", () => DeviceDotQuantizer.DotSizeDip(-203));
        AssertNamedOutOfRange("dpi", "DPI must be positive", () => DeviceDotQuantizer.DipToDots(1, 0));
        AssertNamedOutOfRange("dpi", "DPI must be positive", () => DeviceDotQuantizer.DipToDots(1, -300));
        AssertNamedOutOfRange("dpi", "DPI must be positive", () => DeviceDotQuantizer.DotsToDip(1, 0));
        AssertNamedOutOfRange("dpi", "DPI must be positive", () => DeviceDotQuantizer.SnapDip(1, 0));
        AssertNamedOutOfRange("dip", "Coordinate must be finite", () => DeviceDotQuantizer.DipToDots(double.NaN, 203));
        AssertNamedOutOfRange("dip", "Coordinate must be finite", () => DeviceDotQuantizer.DipToDots(double.PositiveInfinity, 203));
        AssertNamedOutOfRange("dip", "Coordinate must be finite", () => DeviceDotQuantizer.DipToDots(double.NegativeInfinity, 300));
        AssertNamedOutOfRange("totalModules", "Module count must be positive", () => DeviceDotQuantizer.QuantizeModuleBoundary(1, 0, 10));
        AssertNamedOutOfRange("totalModules", "Module count must be positive", () => DeviceDotQuantizer.QuantizeModuleBoundary(1, -4, 10));
        AssertNamedOutOfRange("moduleIndex", "Boundary must be within the module range", () => DeviceDotQuantizer.QuantizeModuleBoundary(-1, 10, 10));
        AssertNamedOutOfRange("moduleIndex", "Boundary must be within the module range", () => DeviceDotQuantizer.QuantizeModuleBoundary(11, 10, 10));
        AssertNamedOutOfRange("totalWidthDots", "Width in dots cannot be negative", () => DeviceDotQuantizer.QuantizeModuleBoundary(1, 10, -1));
    }

    [Fact]
    public void DipToDotsRejectsValuesOutsideTheIntegerDotRange()
    {
        var overflow = AssertNamedOutOfRange("dip", "outside the supported dot range", () => DeviceDotQuantizer.DipToDots(1e20, 203));
        AssertNamedOutOfRange("dip", "outside the supported dot range", () => DeviceDotQuantizer.DipToDots(-1e20, 300));

        const int dpi = 203;
        var maxInclusiveDip = int.MaxValue * DeviceDotQuantizer.DotSizeDip(dpi);
        Assert.Equal(int.MaxValue, DeviceDotQuantizer.DipToDots(maxInclusiveDip, dpi));
        var minInclusiveDip = int.MinValue * DeviceDotQuantizer.DotSizeDip(dpi);
        Assert.Equal(int.MinValue, DeviceDotQuantizer.DipToDots(minInclusiveDip, dpi));
    }

    private static ArgumentOutOfRangeException AssertNamedOutOfRange(string paramName, string messageFragment, Action action)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(action);
        Assert.Equal(paramName, ex.ParamName);
        Assert.Contains(messageFragment, ex.Message, StringComparison.Ordinal);
        return ex;
    }

    [Fact]
    public void SnapDipIsIdempotentAndUsesAwayFromZeroMidpoints()
    {
        const int dpi = 203;
        var halfDot = DeviceDotQuantizer.DotSizeDip(dpi) / 2;

        Assert.Equal(DeviceDotQuantizer.DotsToDip(1, dpi), DeviceDotQuantizer.SnapDip(halfDot, dpi), 9);
        Assert.Equal(DeviceDotQuantizer.DotsToDip(-1, dpi), DeviceDotQuantizer.SnapDip(-halfDot, dpi), 9);

        var snapped = DeviceDotQuantizer.SnapDip(12.345, dpi);
        Assert.Equal(snapped, DeviceDotQuantizer.SnapDip(snapped, dpi), 9);
        Assert.Equal(DeviceDotQuantizer.DipToDots(snapped, dpi), DeviceDotQuantizer.DipToDots(12.345, dpi));
    }

    [Fact]
    public void QuantizeModuleBoundaryRejectsNegativeWidthAndPreservesExactMultiples()
    {
        Assert.Equal(0, DeviceDotQuantizer.QuantizeModuleBoundary(0, 8, 0));
        Assert.Equal(0, DeviceDotQuantizer.QuantizeModuleBoundary(8, 8, 0));
        Assert.Equal(40, DeviceDotQuantizer.QuantizeModuleBoundary(4, 8, 80));
        Assert.Equal(15, DeviceDotQuantizer.QuantizeModuleBoundary(3, 8, 40));
    }
}
