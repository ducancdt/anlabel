using ANLAbel.Core.Geometry;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class DeviceBarcodeLayoutTests
{
    [Theory]
    [InlineData(203, 203)]
    [InlineData(300, 300)]
    [InlineData(600, 600)]
    [InlineData(305, 609)]
    public void LayoutUsesIndependentPrinterAxesAndStaysInsideFrame(int dpiX, int dpiY)
    {
        const double leftDip = 7.35;
        const double topDip = 3.2;
        const double widthDip = 42.7;
        const double heightDip = 11.4;
        var bits = new[]
        {
            false, true, true, false, true, false, true, true, true, false,
            true, false, false, true, true, false, true, true, false, true,
            false, false, true, true, true, false, true, false, true, true,
            false, true, false, true, true, false, false
        };

        var layout = DeviceBarcodeLayout.Create(
            leftDip,
            topDip,
            widthDip,
            heightDip,
            dpiX,
            dpiY,
            bits.Length,
            bits);

        var expectedLeft = DeviceDotQuantizer.DipToDots(leftDip, dpiX);
        var expectedTop = DeviceDotQuantizer.DipToDots(topDip, dpiY);
        var expectedRight = DeviceDotQuantizer.DipToDots(leftDip + widthDip, dpiX);
        var expectedBottom = DeviceDotQuantizer.DipToDots(topDip + heightDip, dpiY);
        Assert.Equal(expectedLeft, layout.LeftDot);
        Assert.Equal(expectedTop, layout.TopDot);
        Assert.Equal(expectedRight - expectedLeft, layout.WidthDots);
        Assert.Equal(expectedBottom - expectedTop, layout.HeightDots);
        Assert.NotEmpty(layout.DarkRuns);
        Assert.All(layout.DarkRuns, run =>
        {
            Assert.InRange(run.StartDot, 0, layout.WidthDots - 1);
            Assert.InRange(run.WidthDots, 1, layout.WidthDots - run.StartDot);
            Assert.InRange(run.EndDotExclusive, 1, layout.WidthDots);
        });

        if (dpiX != dpiY)
        {
            Assert.NotEqual(
                DeviceDotQuantizer.DotSizeDip(dpiX),
                DeviceDotQuantizer.DotSizeDip(dpiY));
        }
    }

    [Fact]
    public void DarkRunsFollowMonotonicModuleBoundaries()
    {
        var bits = new[] { false, true, true, false, true, true, false, true };
        var layout = DeviceBarcodeLayout.Create(
            0,
            0,
            DeviceDotQuantizer.DotsToDip(80, 300),
            DeviceDotQuantizer.DotsToDip(20, 600),
            300,
            600,
            bits.Length,
            bits);

        var previousEnd = -1;
        foreach (var run in layout.DarkRuns)
        {
            Assert.True(run.StartDot >= previousEnd, "Dark runs must preserve source order without crossing.");
            previousEnd = run.EndDotExclusive;
        }
    }

    [Fact]
    public void CollapsedSubDotGapsDoNotCreateOverlappingRuns()
    {
        var layout = DeviceBarcodeLayout.Create(
            0,
            0,
            DeviceDotQuantizer.DotsToDip(3, 203),
            DeviceDotQuantizer.DotsToDip(8, 203),
            203,
            203,
            12,
            new[] { true, false, true, false, true, false, true, false, true, false, true, false });

        for (var index = 1; index < layout.DarkRuns.Count; index++)
        {
            Assert.True(
                layout.DarkRuns[index].StartDot > layout.DarkRuns[index - 1].EndDotExclusive,
                "Dot-collapsed module gaps must be merged, never emitted as overlapping runs.");
        }
    }

    [Fact]
    public void InvalidBitDataAndFrameFailClosed()
    {
        Assert.Throws<ArgumentException>(() => DeviceBarcodeLayout.Create(0, 0, 10, 4, 203, 203, 3, new[] { true }));
        Assert.Throws<ArgumentOutOfRangeException>(() => DeviceBarcodeLayout.Create(0, 0, 0, 4, 203, 203, 1, new[] { true }));
        Assert.Throws<ArgumentOutOfRangeException>(() => DeviceBarcodeLayout.Create(0, 0, 10, 4, 203, 203, 0, Array.Empty<bool>()));
    }
}
