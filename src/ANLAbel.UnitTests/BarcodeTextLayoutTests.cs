using ANLAbel.Core.Barcode;
using ANLAbel.Core.Enums;
using ANLAbel.Core.Geometry;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class BarcodeTextLayoutTests
{
    [Fact]
    public void BarcodeHeightDip_WithText_LessThanTotalHeight()
    {
        Assert.True(BarcodeTextLayout.BarcodeHeightDip(100, 7, showText: true) < 100);
    }

    [Fact]
    public void TextOriginY_PlacedBelowBarArea()
    {
        var barDip = BarcodeTextLayout.BarcodeHeightDip(100, 7, showText: true);
        Assert.True(BarcodeTextLayout.TextOriginY(barDip) > barDip);
    }

    [Fact]
    public void FontSizeDip_MatchesPtToDipConversion()
    {
        Assert.Equal(7.0 * 96.0 / 72.0, BarcodeTextLayout.FontSizeDip(7), precision: 3);
    }

    [Fact]
    public void BarcodeHeightDip_IsDeterministic()
    {
        var first = BarcodeTextLayout.BarcodeHeightDip(100, 7, showText: true);
        var second = BarcodeTextLayout.BarcodeHeightDip(100, 7, showText: true);
        Assert.Equal(first, second, precision: 3);
    }

    [Theory]
    [InlineData(4.0, 8.0)]
    [InlineData(4.0, 12.0)]
    [InlineData(4.0, 20.0)]
    [InlineData(4.0, 30.0)]
    [InlineData(7.0, 8.0)]
    [InlineData(7.0, 12.0)]
    [InlineData(10.0, 20.0)]
    [InlineData(14.0, 30.0)]
    [InlineData(20.0, 30.0)]
    public void BarPlusTextRow_FitsWithinObjectHeight(double fontSizePt, double totalMm)
    {
        var totalDip = MmConverter.MmToDip(totalMm);
        var barDip = BarcodeTextLayout.BarcodeHeightDip(totalDip, fontSizePt, showText: true);
        var textRowDip = BarcodeTextLayout.TextRowHeightDip(fontSizePt);
        Assert.True(barDip + textRowDip <= totalDip + 0.001, $"bar+text exceeds height ({totalMm}mm, {fontSizePt}pt)");
        Assert.True(barDip >= 1, $"bar area must be at least 1 DIP ({totalMm}mm, {fontSizePt}pt)");
    }

    [Theory]
    [InlineData(ObjectType.QRCode, BarcodeSymbology.QRCode, false)]
    [InlineData(ObjectType.DataMatrix, BarcodeSymbology.DataMatrix, false)]
    [InlineData(ObjectType.BarcodeCode128, BarcodeSymbology.Aztec, false)]
    [InlineData(ObjectType.BarcodeCode128, BarcodeSymbology.Pdf417, false)]
    [InlineData(ObjectType.BarcodeCode128, BarcodeSymbology.Code128, true)]
    [InlineData(ObjectType.BarcodeCode128, BarcodeSymbology.Ean13, true)]
    [InlineData(ObjectType.BarcodeCode128, BarcodeSymbology.UpcA, true)]
    [InlineData(ObjectType.BarcodeCode128, BarcodeSymbology.ITF, true)]
    [InlineData(ObjectType.BarcodeCode128, BarcodeSymbology.Code39, true)]
    public void Is1DBarcode_MatchesExpected(ObjectType type, BarcodeSymbology sym, bool expected)
    {
        Assert.Equal(expected, BarcodeTextLayout.Is1DBarcode(type, sym));
    }
}
