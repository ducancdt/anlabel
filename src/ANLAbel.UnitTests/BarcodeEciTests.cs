using ANLAbel.Barcode.Options;
using ANLAbel.Barcode.Renderers;
using ANLAbel.Core.Barcode;
using Xunit;
using ZXing;
using ZXing.Common;

namespace ANLAbel.UnitTests;

/// <summary>
/// A 2D symbol carries an ECI header only when its data needs one. With UTF-8
/// as the character set for everything, every Data Matrix and every QR code in
/// byte mode began with ECI 26, which some scanners pass on as "\000026", and a
/// GS1 Data Matrix had no leading FNC1 (symbology identifier ]d4, not ]d2), so a
/// GS1 scanner read it as plain text.
/// </summary>
public sealed class BarcodeEciTests
{
    [Theory]
    [InlineData(BarcodeType.DataMatrix, "SP-0001 lot abc", "]d1")]
    [InlineData(BarcodeType.DataMatrix, "Lô hàng Hà Nội", "]d4")]
    [InlineData(BarcodeType.QRCode, "SP-0001 lot abc", "]Q1")]
    [InlineData(BarcodeType.QRCode, "Lô hàng Hà Nội", "]Q2")]
    public void OnlyDataBeyondAscii_GetsAnEciHeader(BarcodeType type, string data, string symbology)
    {
        var result = Decode(type, data, new BarcodeRenderOptions());

        Assert.Equal(data, result.Text);
        Assert.Equal(symbology, result.ResultMetadata[ResultMetadataType.SYMBOLOGY_IDENTIFIER]);
    }

    [Theory]
    [InlineData(BarcodeType.DataMatrix, "]d2")]
    [InlineData(BarcodeType.QRCode, "]Q3")]
    public void Gs1Symbols_StartWithFnc1(BarcodeType type, string symbology)
    {
        var result = Decode(type, "(01)09501101020917(10)LOT1(17)261231", new BarcodeRenderOptions { IsGs1 = true });

        Assert.Equal(symbology, result.ResultMetadata[ResultMetadataType.SYMBOLOGY_IDENTIFIER]);
        // ZXing returns the FNC1 that marks a GS1 Data Matrix as a leading GS; a scanner does not transmit it.
        Assert.Equal("0109501101020917" + "10LOT1" + "\u001D" + "17261231", result.Text.TrimStart('\u001D'));
    }

    [Theory]
    [InlineData(BarcodeType.DataMatrix, "]d1")]
    [InlineData(BarcodeType.QRCode, "]Q1")]
    public void Iso15434Envelope_KeepsItsControlCharacters(BarcodeType type, string symbology)
    {
        // [)> RS 06 GS ... RS EOT, as a function builds it from the control characters menu.
        const string envelope = "[)>\u001E06\u001DP4500123456\u001DQ1200\u001D1TLOT42\u001E\u0004";
        var result = Decode(type, envelope, new BarcodeRenderOptions());

        Assert.Equal(envelope, result.Text);
        Assert.Equal(symbology, result.ResultMetadata[ResultMetadataType.SYMBOLOGY_IDENTIFIER]);
    }

    private static Result Decode(BarcodeType type, string data, BarcodeRenderOptions options)
    {
        var image = new ZxingBarcodeRenderer().RenderBarcode(data, type, 30, 30, 300, options);
        var source = new RGBLuminanceSource(image.BgraPixels, image.WidthPixels, image.HeightPixels, RGBLuminanceSource.BitmapFormat.BGRA32);
        var reader = new BarcodeReaderGeneric
        {
            Options = new DecodingOptions
            {
                TryHarder = true,
                PossibleFormats = [type == BarcodeType.QRCode ? BarcodeFormat.QR_CODE : BarcodeFormat.DATA_MATRIX]
            }
        };
        var result = reader.Decode(source);
        Assert.NotNull(result);
        return result;
    }
}
