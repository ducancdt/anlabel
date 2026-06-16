using ANLAbel.Core.Enums;
using ANLAbel.Core.Geometry;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class ConverterTests
{
    [Fact]
    public void MmToDip_25_4mm_Equals96Dip()
    {
        Assert.Equal(96.0, MmConverter.MmToDip(25.4), precision: 3);
    }

    [Fact]
    public void DipToMm_96Dip_Equals25_4mm()
    {
        Assert.Equal(25.4, MmConverter.DipToMm(96), precision: 3);
    }

    [Theory]
    [InlineData(25.4, 203, 203)]
    [InlineData(25.4, 300, 300)]
    public void MmToPrinterDots_25_4mm_EqualsOneDpiWorth(double mm, int dpi, int expected)
    {
        Assert.Equal(expected, MmConverter.MmToPrinterDots(mm, dpi));
    }

    [Fact]
    public void OrientSize_Landscape_PutsLongSideHorizontal()
    {
        var size = LabelGeometry.OrientSize(50, 100, LabelOrientation.Landscape);
        Assert.Equal(100d, size.WidthMm);
        Assert.Equal(50d, size.HeightMm);
    }

    [Fact]
    public void OrientSize_Portrait_PutsLongSideVertical()
    {
        var size = LabelGeometry.OrientSize(100, 50, LabelOrientation.Portrait);
        Assert.Equal(50d, size.WidthMm);
        Assert.Equal(100d, size.HeightMm);
    }
}
