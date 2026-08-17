using ANLAbel.Core.Geometry;
using ANLAbel.Core.Printing;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class DeviceRenderGeometryTests
{
    [Fact]
    public void NonSquareDpiProducesIndependentLabelAndPrintableDots()
    {
        var geometry = DeviceRenderGeometry.Create(
            labelWidthMm: 40,
            labelHeightMm: 12,
            dpiX: 305,
            dpiY: 609,
            printableOriginXDip: 3.2,
            printableOriginYDip: 4.8,
            printableWidthDip: 145.5,
            printableHeightDip: 65.25,
            printableAreaVerified: true);

        Assert.True(geometry.IsValid);
        Assert.Equal(MmConverter.MmToPrinterDots(40, 305), geometry.LabelWidthDots);
        Assert.Equal(MmConverter.MmToPrinterDots(12, 609), geometry.LabelHeightDots);
        Assert.Equal(DeviceDotQuantizer.DipToDots(3.2, 305), geometry.PrintableOriginXDots);
        Assert.Equal(DeviceDotQuantizer.DipToDots(4.8, 609), geometry.PrintableOriginYDots);
        Assert.Equal(DeviceDotQuantizer.DipToDots(145.5, 305), geometry.PrintableWidthDots);
        Assert.Equal(DeviceDotQuantizer.DipToDots(65.25, 609), geometry.PrintableHeightDots);
        Assert.Contains(DeviceRenderGeometry.ContractVersion, geometry.CanonicalForm(), StringComparison.Ordinal);
    }

    [Fact]
    public void MissingPrintableAreaRemainsExplicitlyUnverified()
    {
        var geometry = DeviceRenderGeometry.Create(80, 30, 203, 203);

        Assert.True(geometry.IsValid);
        Assert.False(geometry.PrintableAreaVerified);
        Assert.Equal("printable-area-unverified", geometry.Diagnostic);
        Assert.Equal(0, geometry.PrintableWidthDots);
        Assert.Equal(0, geometry.PrintableHeightDots);
    }

    [Fact]
    public void VerifiedInvalidPrintableAreaFailsClosed()
    {
        var geometry = DeviceRenderGeometry.Create(
            80,
            30,
            203,
            203,
            printableWidthDip: 0,
            printableHeightDip: 100,
            printableAreaVerified: true);

        Assert.False(geometry.IsValid);
        Assert.False(geometry.PrintableAreaVerified);
        Assert.Equal("printable-area-invalid", geometry.Diagnostic);
    }
}
