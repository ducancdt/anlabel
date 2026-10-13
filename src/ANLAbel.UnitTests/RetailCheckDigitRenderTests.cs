using ANLAbel.Barcode.Renderers;
using ANLAbel.Core.Barcode;
using ANLAbel.Core.Enums;
using Xunit;
using ZXing;
using ZXing.Common;

namespace ANLAbel.UnitTests;

public sealed class RetailCheckDigitRenderTests
{
    [Fact]
    public void WrongEan13CheckDigitProducesNoSymbolAndValidPayloadRoundTrips()
    {
        var renderer = new ZxingBarcodeRenderer();
        const string wrongCheck = "4006381333932";
        const string withCheck = "4006381333931";
        const string withoutCheck = "400638133393";

        Assert.NotEmpty(BarcodeApplicationContract.ValidateData(
            BarcodeApplicationProfile.General,
            BarcodeSymbology.Ean13,
            wrongCheck));
        Assert.False(renderer.ValidateData(wrongCheck, BarcodeType.Ean13));
        var refused = Assert.Throws<ArgumentException>(() =>
            renderer.RenderBarcode(wrongCheck, BarcodeType.Ean13, 40, 20, 203));
        Assert.Contains("invalid", refused.Message, StringComparison.OrdinalIgnoreCase);

        Assert.True(renderer.ValidateData(withCheck, BarcodeType.Ean13));
        Assert.Equal(withCheck, Decode(renderer.RenderBarcode(withCheck, BarcodeType.Ean13, 60, 40, 203)));

        Assert.Empty(BarcodeApplicationContract.ValidateData(
            BarcodeApplicationProfile.General,
            BarcodeSymbology.Ean13,
            withoutCheck));
        Assert.True(renderer.ValidateData(withoutCheck, BarcodeType.Ean13));
        var expanded = Decode(renderer.RenderBarcode(withoutCheck, BarcodeType.Ean13, 60, 40, 203));
        Assert.StartsWith(withoutCheck, expanded, StringComparison.Ordinal);
        Assert.Equal(withoutCheck.Length + 1, expanded.Length);
        Assert.Empty(BarcodeApplicationContract.ValidateData(
            BarcodeApplicationProfile.General,
            BarcodeSymbology.Ean13,
            expanded));
    }

    private static string Decode(BarcodePixelImage image)
    {
        var source = new RGBLuminanceSource(
            image.BgraPixels,
            image.WidthPixels,
            image.HeightPixels,
            RGBLuminanceSource.BitmapFormat.BGRA32);
        var hints = new Dictionary<DecodeHintType, object>
        {
            [DecodeHintType.POSSIBLE_FORMATS] = new[] { BarcodeFormat.EAN_13 },
            [DecodeHintType.TRY_HARDER] = true
        };
        var result = new MultiFormatReader().decode(new BinaryBitmap(new HybridBinarizer(source)), hints);
        Assert.NotNull(result);
        return result.Text;
    }
}
