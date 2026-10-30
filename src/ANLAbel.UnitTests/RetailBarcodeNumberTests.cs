using ANLAbel.Barcode.Options;
using ANLAbel.Barcode.Renderers;
using ANLAbel.Core.Barcode;
using ANLAbel.Core.Enums;
using ANLAbel.Core.Models;
using Xunit;
using ZXing;
using ZXing.Common;

namespace ANLAbel.UnitTests;

public sealed class RetailBarcodeNumberTests
{
    [Theory]
    [InlineData(BarcodeSymbology.Ean13, "590123412345", "5901234123457")]
    [InlineData(BarcodeSymbology.Ean13, "5901234123457", "5901234123457")]
    [InlineData(BarcodeSymbology.Ean8, "9638507", "96385074")]
    [InlineData(BarcodeSymbology.UpcA, "03600029145", "036000291452")]
    [InlineData(BarcodeSymbology.UpcE, "123456", "01234565")]
    [InlineData(BarcodeSymbology.UpcE, "0123456", "01234565")]
    [InlineData(BarcodeSymbology.UpcE, "1123456", "11234562")]
    [InlineData(BarcodeSymbology.UpcE, "0654321", "06543217")]
    [InlineData(BarcodeSymbology.UpcE, "01234565", "01234565")]
    public void Complete_GivesTheNumberTheScannerReads(BarcodeSymbology symbology, string data, string expected)
    {
        Assert.Equal(expected, RetailBarcodeNumber.Complete(symbology, data));
    }

    [Theory]
    [InlineData(BarcodeSymbology.Ean13, "12345")]
    [InlineData(BarcodeSymbology.Ean13, "59012341234A")]
    [InlineData(BarcodeSymbology.UpcE, "2123456")]
    [InlineData(BarcodeSymbology.UpcE, "")]
    [InlineData(BarcodeSymbology.Code128, "12345678901")]
    public void Complete_LeavesOtherDataForValidation(BarcodeSymbology symbology, string data)
    {
        Assert.Equal(data, RetailBarcodeNumber.Complete(symbology, data));
    }

    [Theory]
    [InlineData(BarcodeSymbology.Ean13, "590123412345")]
    [InlineData(BarcodeSymbology.Ean8, "9638507")]
    [InlineData(BarcodeSymbology.UpcA, "03600029145")]
    [InlineData(BarcodeSymbology.UpcE, "123456")]
    [InlineData(BarcodeSymbology.UpcE, "1123456")]
    [InlineData(BarcodeSymbology.UpcE, "0654321")]
    public void TheHumanReadableLineMatchesWhatIsEncoded(BarcodeSymbology symbology, string data)
    {
        var item = new LabelObject { Type = ObjectType.BarcodeCode128, BarcodeSymbology = symbology, BarcodeHriShowCheckDigit = false };
        var (type, format) = symbology switch
        {
            BarcodeSymbology.Ean13 => (BarcodeType.Ean13, BarcodeFormat.EAN_13),
            BarcodeSymbology.Ean8 => (BarcodeType.Ean8, BarcodeFormat.EAN_8),
            BarcodeSymbology.UpcA => (BarcodeType.UpcA, BarcodeFormat.UPC_A),
            _ => (BarcodeType.UpcE, BarcodeFormat.UPC_E)
        };

        var vector = new ZxingBarcodeRenderer().RenderBarcodeVector(data, type, 40, 10, 300, new BarcodeRenderOptions { QuietZoneModules = 10 });

        Assert.NotNull(vector);
        var shown = BarcodeCheckDigitContract.FormatHriText(item, symbology, data);
        Assert.Equal(Decode(vector!, format), shown);
        Assert.Equal(shown, BarcodeCheckDigitContract.HriMeasureText(item, data));
    }

    [Theory]
    [InlineData("012345", true, null)]
    [InlineData("0123456", true, null)]
    [InlineData("1123456", true, null)]
    [InlineData("01234565", true, null)]
    [InlineData("01234567", false, "invalid check digit")]
    [InlineData("2123456", false, "must be 0 or 1")]
    [InlineData("21234565", false, "must be 0 or 1")]
    public void UpcE_NeedsNumberSystemZeroOrOneAndItsCheckDigit(string payload, bool expectedValid, string? expectedPart)
    {
        var errors = BarcodeApplicationContract.ValidateData(BarcodeApplicationProfile.General, BarcodeSymbology.UpcE, payload);

        Assert.Equal(expectedValid, errors.Count == 0);
        if (!expectedValid)
        {
            Assert.Contains(errors, error => error.Contains(expectedPart!, StringComparison.Ordinal));
        }

        // What the contract accepts, the encoder encodes.
        Assert.Equal(expectedValid, new ZxingBarcodeRenderer().ValidateData(payload, BarcodeType.UpcE));
        if (expectedValid)
        {
            Assert.NotNull(new ZxingBarcodeRenderer().RenderBarcodeVector(payload, BarcodeType.UpcE, 30, 10, 300));
        }
    }

    private static string? Decode(BarcodeVectorData vector, BarcodeFormat format)
    {
        const int height = 20;
        var width = vector.RowBits.Length;
        var gray = new byte[width * height];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                gray[(y * width) + x] = vector.RowBits[x] ? (byte)0 : (byte)255;
            }
        }

        var source = new RGBLuminanceSource(gray, width, height, RGBLuminanceSource.BitmapFormat.Gray8);
        var hints = new Dictionary<DecodeHintType, object>
        {
            [DecodeHintType.PURE_BARCODE] = true,
            [DecodeHintType.POSSIBLE_FORMATS] = new[] { format }
        };
        return new MultiFormatReader().decode(new BinaryBitmap(new HybridBinarizer(source)), hints)?.Text;
    }
}
