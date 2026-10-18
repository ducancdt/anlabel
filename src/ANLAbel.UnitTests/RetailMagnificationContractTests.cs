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
}
