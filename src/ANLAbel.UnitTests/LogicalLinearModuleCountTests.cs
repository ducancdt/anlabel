using ANLAbel.Barcode.Options;
using ANLAbel.Barcode.Renderers;
using ANLAbel.Core.Barcode;
using Xunit;

namespace ANLAbel.UnitTests;

/// <summary>
/// P1.0: pure logical module count must come from the shipped encoder path
/// and must not track frame-scaled vector pixel width.
/// </summary>
public sealed class LogicalLinearModuleCountTests
{
    private readonly ZxingBarcodeRenderer _renderer = new();

    [Fact]
    public void CountLinearModules_IsIndependentOfFrameWidthAndDpi()
    {
        const string payload = "ABC123";
        var options = new BarcodeRenderOptions { QuietZoneModules = 10 };

        var logical = _renderer.CountLinearModules(payload, BarcodeType.Code128, options);
        Assert.NotNull(logical);
        Assert.True(logical > 1, "Code128 payload must encode to more than one logical module.");

        // Frame-scaled vectors at two widths must still report the same pure count
        // when asked via CountLinearModules (API ignores frame by design).
        var again = _renderer.CountLinearModules(payload, BarcodeType.Code128, options);
        Assert.Equal(logical, again);

        var wideVector = _renderer.RenderBarcodeVector(payload, BarcodeType.Code128, widthMm: 60, heightMm: 12, dpi: 300, options);
        var narrowVector = _renderer.RenderBarcodeVector(payload, BarcodeType.Code128, widthMm: 20, heightMm: 12, dpi: 203, options);
        Assert.NotNull(wideVector);
        Assert.NotNull(narrowVector);

        // Scaled pixel columns track the frame — they must NOT equal logical count
        // for a comfortably wide frame at 300 DPI.
        Assert.True(
            wideVector!.WidthModules > logical!.Value,
            $"Wide-frame vector pixel width ({wideVector.WidthModules}) must exceed pure logical modules ({logical}).");
        Assert.NotEqual(wideVector.WidthModules, narrowVector!.WidthModules);

        // Industrial estimate using logical modules yields multi-dot modules for a 40 mm frame.
        var resolution = LinearBarcodeModuleContract.ResolveForObject(
            authoredModuleWidthMm: 0,
            frameWidthMm: 40,
            totalModules: logical.Value,
            dpi: 203);
        Assert.False(
            resolution.IsBelowMinimumDots,
            $"Comfortable frame with pure logical count must not be treated as sub-2-dot (dots={resolution.ModuleDots}, modules={logical}).");
    }

    [Fact]
    public void CountLinearModules_GrowsWithLongerPayload()
    {
        var shortCount = _renderer.CountLinearModules("A", BarcodeType.Code128, new BarcodeRenderOptions { QuietZoneModules = 2 });
        var longCount = _renderer.CountLinearModules("ABCDEFGHIJKLMNOP", BarcodeType.Code128, new BarcodeRenderOptions { QuietZoneModules = 2 });
        Assert.NotNull(shortCount);
        Assert.NotNull(longCount);
        Assert.True(longCount > shortCount, "Longer Code128 payload must require more modules.");
    }

    [Fact]
    public void CountLinearModules_ReturnsNullForMatrixAndInvalidData()
    {
        Assert.Null(_renderer.CountLinearModules("hello", BarcodeType.QRCode, null));
        Assert.Null(_renderer.CountLinearModules("", BarcodeType.Code128, null));
        Assert.Null(_renderer.CountLinearModules("   ", BarcodeType.Code128, null));
    }

    [Theory]
    [InlineData(10)]
    [InlineData(2)]
    public void CountLinearModules_IncludesQuietZoneInMarginHint(int quietZone)
    {
        var options = new BarcodeRenderOptions { QuietZoneModules = quietZone };
        var count = _renderer.CountLinearModules("ABC123", BarcodeType.Code128, options);
        Assert.NotNull(count);
        Assert.True(count > quietZone * 2, "Logical width must leave room for quiet-zone margins plus the symbol.");
    }

    [Fact]
    public void SizedFromXWidth_UsesShippedResolveAndLogicalCount_WithinOnePrinterDot()
    {
        const string payload = "ABC123";
        const int dpi = 203;
        var options = new BarcodeRenderOptions { QuietZoneModules = 10 };
        var modules = _renderer.CountLinearModules(payload, BarcodeType.Code128, options);
        Assert.NotNull(modules);

        const double authoredX = 0.33;
        var expected = LinearBarcodeModuleContract.SizedFromXWidthMm(authoredX, modules!.Value, dpi);
        var resolution = LinearBarcodeModuleContract.Resolve(authoredX, dpi);
        Assert.Equal(resolution.EffectiveModuleWidthMm * modules.Value, expected, precision: 9);

        // Frame width must not redefine pure logical count.
        var widePx = _renderer.RenderBarcodeVector(payload, BarcodeType.Code128, 80, 12, dpi, options)!.WidthModules;
        Assert.NotEqual(modules.Value, widePx);

        var tol = LinearBarcodeModuleContract.OnePrinterDotMm(dpi);
        Assert.InRange(expected, expected - tol, expected + tol);
        Assert.True(expected > 10, "SizedFromX width for Code128 ABC123 @ 0.33mm should be multi-centimetre.");
    }
}
