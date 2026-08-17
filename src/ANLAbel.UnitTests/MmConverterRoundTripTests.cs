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

    [Fact]
    public void PrinterDotsTreatZeroAsZeroAndKeepSignForSmallPositiveAndNegativeMoves()
    {
        Assert.Equal(0, MmConverter.MmToPrinterDots(0, 203));
        Assert.True(MmConverter.MmToPrinterDots(0.2, 203) >= 1);
        Assert.True(MmConverter.MmToPrinterDots(-0.2, 203) <= -1);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-203)]
    public void PrinterConversion_InvalidDpi_FailsFast(int dpi)
    {
        var dotsEx = Assert.Throws<ArgumentOutOfRangeException>(() => MmConverter.MmToPrinterDots(10, dpi));
        Assert.Equal("dpi", dotsEx.ParamName);
        Assert.Contains("Printer DPI must be greater than zero", dotsEx.Message, StringComparison.Ordinal);
        var mmEx = Assert.Throws<ArgumentOutOfRangeException>(() => MmConverter.PrinterDotsToMm(100, dpi));
        Assert.Equal("dpi", mmEx.ParamName);
        Assert.Contains("Printer DPI must be greater than zero", mmEx.Message, StringComparison.Ordinal);
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
