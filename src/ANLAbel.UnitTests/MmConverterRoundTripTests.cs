using ANLAbel.Core.Geometry;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class MmConverterRoundTripTests
{
    [Fact]
    public void DipRoundTrip_PreservesIndustrialLabelDimensions()
    {
        foreach (var mm in RepresentativeDimensionsMm)
        {
            var restoredMm = MmConverter.DipToMm(MmConverter.MmToDip(mm));

            Assert.InRange(Math.Abs(restoredMm - mm), 0, 0.000001);
        }
    }

    [Theory]
    [InlineData(203)]
    [InlineData(300)]
    [InlineData(600)]
    public void PrinterDotRoundTrip_StaysWithinPrintPlanTolerance(int dpi)
    {
        foreach (var mm in RepresentativeDimensionsMm)
        {
            var dots = MmConverter.MmToPrinterDots(mm, dpi);
            var restoredMm = MmConverter.PrinterDotsToMm(dots, dpi);

            Assert.True(
                Math.Abs(restoredMm - mm) <= 0.05,
                $"{mm:0.###} mm at {dpi} DPI restored as {restoredMm:0.######} mm ({dots} dots), exceeding 0.05 mm tolerance.");
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-203)]
    public void PrinterConversion_InvalidDpi_FailsFast(int dpi)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => MmConverter.MmToPrinterDots(10, dpi));
        Assert.Throws<ArgumentOutOfRangeException>(() => MmConverter.PrinterDotsToMm(100, dpi));
    }

    private static readonly double[] RepresentativeDimensionsMm =
    {
        0.5,
        1,
        10,
        25.4,
        50,
        100,
        150
    };
}
