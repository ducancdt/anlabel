using ANLAbel.Barcode.Options;
using ANLAbel.Barcode.Renderers;
using ANLAbel.Core.Enums;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class QrFrameFillContractTests
{
    private readonly ZxingBarcodeRenderer _renderer = new();

    [Fact]
    public void QrBitmapUsesIntegerModulesInsideTheFrame()
    {
        const double widthMm = 40;
        const int dpi = 300;
        var frame = (int)Math.Round(widthMm / 25.4 * dpi, MidpointRounding.AwayFromZero);

        var pixels = _renderer.RenderBarcode(
            "PART-001",
            BarcodeType.QRCode,
            widthMm,
            widthMm,
            dpi,
            new BarcodeRenderOptions { QuietZoneModules = 2 });

        Assert.Equal(pixels.WidthPixels, pixels.HeightPixels);
        Assert.True(pixels.WidthPixels <= frame);
        var leftover = frame - pixels.WidthPixels;
        var modulePx = DarkRunAfterQuietZone(pixels) / 7.0;
        Assert.True(modulePx >= 1);
        Assert.True(leftover / 2.0 < modulePx + 0.01);
    }

    [Fact]
    public void EnlargingTheObjectGrowsModulesInsteadOfAddingPad()
    {
        var options = new BarcodeRenderOptions { QuietZoneModules = 2 };
        var compact = _renderer.RenderBarcode("QR Code", BarcodeType.QRCode, 10, 10, 300, options);
        var enlarged = _renderer.RenderBarcode("QR Code", BarcodeType.QRCode, 40, 40, 300, options);

        var compactInset = FirstDarkColumn(compact);
        var enlargedInset = FirstDarkColumn(enlarged);
        Assert.True(compactInset > 0, "Compact QR with a 2-module quiet zone must inset dark modules.");
        Assert.True(enlargedInset > 0, "Enlarged QR with a 2-module quiet zone must inset dark modules.");

        var compactBody = compact.WidthPixels - 2 * compactInset;
        var enlargedBody = enlarged.WidthPixels - 2 * enlargedInset;
        Assert.True(enlargedBody > compactBody * 2,
            $"Enlarging 10mm to 40mm must grow the dark symbol ({compactBody}px -> {enlargedBody}px), not add empty ring.");

        var enlargedInsetRatio = enlargedInset / (double)enlarged.WidthPixels;
        Assert.InRange(enlargedInsetRatio, 0.05, 0.12);
    }

    [Fact]
    public void ZeroQuietZoneReachesTheObjectEdge()
    {
        var pixels = _renderer.RenderBarcode(
            "EDGE",
            BarcodeType.QRCode,
            20,
            20,
            300,
            new BarcodeRenderOptions { QuietZoneModules = 0 });

        Assert.Equal(0, FirstDarkColumn(pixels));
        Assert.Equal(0, FirstDarkRow(pixels));
    }

    [Fact]
    public void NonSquareFrameKeepsSquareModulesAtSquareDpi()
    {
        var pixels = _renderer.RenderBarcode(
            "QR Code",
            BarcodeType.QRCode,
            40,
            28,
            300,
            new BarcodeRenderOptions { QuietZoneModules = 2 });

        var left = FirstDarkColumn(pixels);
        var top = FirstDarkRow(pixels);
        var right = LastDarkColumn(pixels);
        var bottom = LastDarkRow(pixels);
        Assert.True(left >= 0 && top >= 0);
        Assert.Equal(right - left, bottom - top);
        Assert.Equal(pixels.WidthPixels, pixels.HeightPixels);

        var frameW = (int)Math.Round(40 / 25.4 * 300, MidpointRounding.AwayFromZero);
        var frameH = (int)Math.Round(28 / 25.4 * 300, MidpointRounding.AwayFromZero);
        Assert.True(pixels.WidthPixels <= Math.Min(frameW, frameH));
        var modulePx = (right - left + 1) / 21.0;
        Assert.True((frameH - pixels.HeightPixels) / 2.0 < modulePx + 0.01);
    }

    [Fact]
    public void NewQrObjectDefaultsToTwoModuleQuietZone()
    {
        var item = new ANLAbel.Core.Models.LabelObject { Type = ObjectType.QRCode };
        Assert.Equal(2, item.QrQuietZoneModules);
    }

    private static int FirstDarkColumn(BarcodePixelImage image)
    {
        for (var x = 0; x < image.WidthPixels; x++)
        {
            for (var y = 0; y < image.HeightPixels; y++)
            {
                if (image.BgraPixels[(y * image.WidthPixels + x) * 4] < 128)
                {
                    return x;
                }
            }
        }

        return -1;
    }

    private static int LastDarkColumn(BarcodePixelImage image)
    {
        for (var x = image.WidthPixels - 1; x >= 0; x--)
        {
            for (var y = 0; y < image.HeightPixels; y++)
            {
                if (image.BgraPixels[(y * image.WidthPixels + x) * 4] < 128)
                {
                    return x;
                }
            }
        }

        return -1;
    }

    private static int LastDarkRow(BarcodePixelImage image)
    {
        for (var y = image.HeightPixels - 1; y >= 0; y--)
        {
            for (var x = 0; x < image.WidthPixels; x++)
            {
                if (image.BgraPixels[(y * image.WidthPixels + x) * 4] < 128)
                {
                    return y;
                }
            }
        }

        return -1;
    }

    private static int DarkRunAfterQuietZone(BarcodePixelImage image)
    {
        var startX = FirstDarkColumn(image);
        var startY = FirstDarkRow(image);
        if (startX < 0 || startY < 0)
        {
            return 0;
        }

        var run = 0;
        for (var x = startX; x < image.WidthPixels; x++)
        {
            if (image.BgraPixels[(startY * image.WidthPixels + x) * 4] >= 128)
            {
                break;
            }

            run++;
        }

        return run;
    }

    private static int FirstDarkRow(BarcodePixelImage image)
    {
        for (var y = 0; y < image.HeightPixels; y++)
        {
            for (var x = 0; x < image.WidthPixels; x++)
            {
                if (image.BgraPixels[(y * image.WidthPixels + x) * 4] < 128)
                {
                    return y;
                }
            }
        }

        return -1;
    }

}
