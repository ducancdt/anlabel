using ANLAbel.Core.Barcode;
using ANLAbel.Core.Enums;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class RetailMagnificationContractTests
{
    [Theory]
    [InlineData(BarcodeSymbology.Ean13, 0.330)]
    [InlineData(BarcodeSymbology.UpcA, 0.264)]
    [InlineData(BarcodeSymbology.Ean8, 0.660)]
    [InlineData(BarcodeSymbology.Code128, 0.10)]
    public void InsideTheGs1RangeOrNotRetailGivesNoAdvice(BarcodeSymbology symbology, double x)
    {
        Assert.Equal(string.Empty, RetailMagnificationContract.Check(symbology, x, 300));
    }

    [Theory]
    [InlineData(BarcodeSymbology.Ean13, 0.254, "77%")]
    [InlineData(BarcodeSymbology.UpcE, 0.762, "231%")]
    public void OutsideTheRangeNamesTheMagnification(BarcodeSymbology symbology, double x, string expected)
    {
        var advice = RetailMagnificationContract.Check(symbology, x, 300);

        Assert.Contains(expected + " magnification", advice);
        Assert.Contains("80% to 200%", advice);
    }

    [Theory]
    [InlineData(BarcodeSymbology.Ean13, 19.60)]
    [InlineData(BarcodeSymbology.Ean13, 49.00)]
    [InlineData(BarcodeSymbology.UpcA, 24.50)]
    [InlineData(BarcodeSymbology.Ean8, 15.90)]
    [InlineData(BarcodeSymbology.Ean8, 39.76)]
    [InlineData(BarcodeSymbology.UpcE, 11.20)]
    [InlineData(BarcodeSymbology.UpcE, 28.00)]
    [InlineData(BarcodeSymbology.Code128, 5.0)]
    [InlineData(BarcodeSymbology.Ean13, 0)]
    [InlineData(BarcodeSymbology.Ean13, -3)]
    public void HeightInsideTheWindowOrNotRetailGivesNoAdvice(BarcodeSymbology symbology, double heightMm)
    {
        Assert.Equal(string.Empty, RetailMagnificationContract.CheckHeight(symbology, heightMm));
    }

    [Theory]
    [InlineData(BarcodeSymbology.Ean13, 12.0, "shorter")]
    [InlineData(BarcodeSymbology.Ean13, 55.0, "taller")]
    [InlineData(BarcodeSymbology.Ean8, 10.0, "shorter")]
    [InlineData(BarcodeSymbology.UpcE, 30.0, "taller")]
    public void HeightOutsideTheWindowNamesTheRange(BarcodeSymbology symbology, double heightMm, string direction)
    {
        var advice = RetailMagnificationContract.CheckHeight(symbology, heightMm);

        Assert.Contains(direction, advice);
        Assert.Contains("GS1 retail height range", advice);
        Assert.Contains("retail point-of-sale scanning is not assured", advice);
    }

    [Theory]
    [InlineData(BarcodeSymbology.Ean13, 19.60, 49.00)]
    [InlineData(BarcodeSymbology.UpcA, 19.60, 49.00)]
    [InlineData(BarcodeSymbology.Ean8, 15.90, 39.76)]
    [InlineData(BarcodeSymbology.UpcE, 11.20, 28.00)]
    public void HeightRangesMatchTheReferenceBarcodeDefTable(BarcodeSymbology symbology, double minimumMm, double maximumMm)
    {
        Assert.True(RetailMagnificationContract.TryGetHeightRangeMm(symbology, out var actualMinimum, out var actualMaximum));
        Assert.Equal(minimumMm, actualMinimum);
        Assert.Equal(maximumMm, actualMaximum);
    }
}
