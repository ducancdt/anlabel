using ANLAbel.Barcode.Options;
using ANLAbel.Barcode.Renderers;
using Xunit;
using ZXing;
using ZXing.Common;

namespace ANLAbel.UnitTests;

/// <summary>
/// DEFECT 1 — proves that a GS1-128 (Code 128) barcode encodes the inter-AI
/// separator as a real FNC1 symbol character rather than a literal ASCII GS
/// (0x1D) data character. ZXing.Net's Code128Writer only maps the escape char
/// U+00F1 onto an FNC1 symbol; a literal 0x1D becomes ordinary Code-Set data.
/// This is a genuine encode → decode round trip through the real renderer and
/// the ZXing reader; it asserts on the decoded bits, not on the pre-encoder
/// string.
/// </summary>
public sealed class Gs1Code128Fnc1RoundTripTests
{
    [Theory]
    [InlineData(203)]
    [InlineData(300)]
    [InlineData(600)]
    public void Gs1Code128SeparatorRoundTripsAsFnc1NotLiteralGs(int dpi)
    {
        // AI 21 (serial) is variable-length -> a separator must follow it before
        // AI 10 (batch/lot). AI 21 is the last variable field before another AI.
        const string source = "(21)ABC(10)XYZ";
        var renderer = new ZxingBarcodeRenderer();
        var options = new BarcodeRenderOptions { IsGs1 = true, QuietZoneModules = 10 };

        var image = renderer.RenderBarcode(source, BarcodeType.Code128, 80, 25, dpi, options);

        var luminance = new RGBLuminanceSource(
            image.BgraPixels,
            image.WidthPixels,
            image.HeightPixels,
            RGBLuminanceSource.BitmapFormat.BGRA32);
        var hints = new Dictionary<DecodeHintType, object>
        {
            [DecodeHintType.POSSIBLE_FORMATS] = new[] { BarcodeFormat.CODE_128 },
            [DecodeHintType.TRY_HARDER] = true,
            // ASSUME_GS1 makes the reader surface FNC1: leading FNC1 -> "]C1"
            // AIM identifier, subsequent FNC1 -> the ASCII GS (0x1D) separator.
            [DecodeHintType.ASSUME_GS1] = true
        };

        var decoded = new MultiFormatReader().decode(new BinaryBitmap(new HybridBinarizer(luminance)), hints);

        Assert.NotNull(decoded);
        Assert.Equal(BarcodeFormat.CODE_128, decoded.BarcodeFormat);

        const char gs = '\u001d';
        // Leading FNC1 is reported by the reader as the "]C1" symbology id.
        Assert.StartsWith("]C1", decoded.Text, StringComparison.Ordinal);
        // Exactly one inter-AI separator, positioned between the two element
        // strings — proving the 0x1D reached ZXing as an FNC1 symbol.
        Assert.Equal($"]C121ABC{gs}10XYZ", decoded.Text);
        Assert.Equal(1, decoded.Text.Count(c => c == gs));

        var separatorIndex = decoded.Text.IndexOf(gs);
        Assert.Equal("]C121ABC", decoded.Text[..separatorIndex]);
        Assert.Equal("10XYZ", decoded.Text[(separatorIndex + 1)..]);
    }
}
