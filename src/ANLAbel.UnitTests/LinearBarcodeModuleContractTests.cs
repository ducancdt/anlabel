using ANLAbel.Core.Barcode;
using ANLAbel.Core.Enums;
using Xunit;

namespace ANLAbel.UnitTests;

/// <summary>
/// Industrial 1D X-dimension policy: authored module mm quantized to whole
/// printer dots at the print-plan DPI (not design-only DPI alone).
/// </summary>
public sealed class LinearBarcodeModuleContractTests
{
    [Theory]
    // 0.33 mm @ 203 DPI => round(0.33/25.4*203) = round(2.637) = 3 dots
    [InlineData(0.33, 203, 3)]
    // 0.33 mm @ 300 DPI => round(0.33/25.4*300) = round(3.898) = 4 dots
    [InlineData(0.33, 300, 4)]
    // 0.25 mm @ 203 DPI => round(0.25/25.4*203) = round(1.998) = 2 dots
    [InlineData(0.25, 203, 2)]
    // Sub-dot raw: 0.08 mm @ 203 => round(0.639) = 1 (floor to at least 1)
    [InlineData(0.08, 203, 1)]
    // 0.19 mm industrial floor @ 203 => round(1.518) = 2 dots
    [InlineData(0.19, 203, 2)]
    public void Resolve_QuantizesAuthoredXDimToWholePrinterDots(double moduleMm, int dpi, int expectedDots)
    {
        var resolution = LinearBarcodeModuleContract.Resolve(moduleMm, dpi);

        Assert.Equal(expectedDots, resolution.ModuleDots);
        Assert.Equal(moduleMm, resolution.AuthoredModuleWidthMm, precision: 6);
        Assert.Equal(dpi, resolution.Dpi);

        // Effective mm reconstructs exactly from integer dots (within one-dot metric).
        var expectedEffectiveMm = expectedDots * LinearBarcodeModuleContract.MillimetersPerInch / dpi;
        Assert.Equal(expectedEffectiveMm, resolution.EffectiveModuleWidthMm, precision: 9);

        // Changing DPI must change dots predictably for the same authored X.
        var otherDpi = dpi == 203 ? 300 : 203;
        var other = LinearBarcodeModuleContract.Resolve(moduleMm, otherDpi);
        var expectedOtherDots = Math.Max(1, (int)Math.Round(
            moduleMm / LinearBarcodeModuleContract.MillimetersPerInch * otherDpi,
            MidpointRounding.AwayFromZero));
        Assert.Equal(expectedOtherDots, other.ModuleDots);
        if (expectedOtherDots != expectedDots)
        {
            Assert.NotEqual(resolution.ModuleDots, other.ModuleDots);
        }
    }

    [Fact]
    public void Resolve_ReportsSubMinimumDotsAndIndustrialFloor()
    {
        // 1 dot at 203 DPI ≈ 0.125 mm — below both 2-dot floor and 0.19 mm floor.
        var thin = LinearBarcodeModuleContract.Resolve(0.08, 203);
        Assert.True(thin.IsBelowMinimumDots);
        Assert.True(thin.IsBelowIndustrialFloorMm);
        Assert.True(thin.HasIndustrialRisk);
        Assert.Equal(1, thin.ModuleDots);

        var message = LinearBarcodeModuleContract.FormatIndustrialRiskMessage(thin);
        Assert.Contains("dot", message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("1", message, StringComparison.Ordinal);
        Assert.Contains("203", message, StringComparison.Ordinal);

        // Comfortable industrial module: 0.33 mm → 3 dots @ 203, above 0.19 mm floor.
        var ok = LinearBarcodeModuleContract.Resolve(0.33, 203);
        Assert.False(ok.IsBelowMinimumDots);
        Assert.False(ok.IsBelowIndustrialFloorMm);
        Assert.False(ok.HasIndustrialRisk);
        Assert.Equal(string.Empty, LinearBarcodeModuleContract.FormatIndustrialRiskMessage(ok));
    }

    [Fact]
    public void ResolveForObject_PrefersAuthoredXDimOverFrameEstimate()
    {
        // Frame would imply a much larger module; authored X must win.
        var fromAuthored = LinearBarcodeModuleContract.ResolveForObject(
            authoredModuleWidthMm: 0.25,
            frameWidthMm: 50,
            totalModules: 50,
            dpi: 203);

        Assert.Equal(0.25, fromAuthored.AuthoredModuleWidthMm, precision: 6);
        Assert.Equal(2, fromAuthored.ModuleDots);

        var fromFrame = LinearBarcodeModuleContract.ResolveForObject(
            authoredModuleWidthMm: 0,
            frameWidthMm: 50,
            totalModules: 50,
            dpi: 203);

        // 50 mm / 50 modules = 1 mm → many dots at 203.
        Assert.Equal(1.0, fromFrame.AuthoredModuleWidthMm, precision: 6);
        Assert.True(fromFrame.ModuleDots > fromAuthored.ModuleDots);
        Assert.False(fromFrame.HasIndustrialRisk);
    }

    [Fact]
    public void EstimateModuleWidthMmFromFrame_IsFrameOverModuleCount()
    {
        Assert.Equal(0.4, LinearBarcodeModuleContract.EstimateModuleWidthMmFromFrame(40, 100), precision: 9);
    }

    [Fact]
    public void SizedFromXWidthMm_IsEffectiveModuleTimesLogicalCount()
    {
        const int dpi = 203;
        const int logicalModules = 100;
        var resolution = LinearBarcodeModuleContract.Resolve(0.33, dpi);
        var width = LinearBarcodeModuleContract.SizedFromXWidthMm(0.33, logicalModules, dpi);
        Assert.Equal(resolution.EffectiveModuleWidthMm * logicalModules, width, precision: 9);
        Assert.True(LinearBarcodeModuleContract.UsesSizedFromX(BarcodeWidthMode.SizedFromX, 0.33));
        Assert.False(LinearBarcodeModuleContract.UsesSizedFromX(BarcodeWidthMode.FrameOwned, 0.33));
        Assert.False(LinearBarcodeModuleContract.UsesSizedFromX(BarcodeWidthMode.SizedFromX, 0));
        Assert.Equal(25.4 / dpi, LinearBarcodeModuleContract.OnePrinterDotMm(dpi), precision: 9);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Resolve_RejectsNonPositiveModuleWidth(double bad)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => LinearBarcodeModuleContract.Resolve(bad, 203));
        Assert.Contains("must be", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-300)]
    public void Resolve_RejectsNonPositiveDpi(int badDpi)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => LinearBarcodeModuleContract.Resolve(0.33, badDpi));
        Assert.Contains("must be", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PublicFloorsAreTheAuthoredIndustrialConstants()
    {
        Assert.Equal(2, LinearBarcodeModuleContract.MinimumModuleDots);
        Assert.Equal(0.19, LinearBarcodeModuleContract.MinimumIndustrialXDimensionMm);
        Assert.Equal(0.33, LinearBarcodeModuleContract.RecommendedDefaultXDimensionMm);
        Assert.Equal(25.4, LinearBarcodeModuleContract.MillimetersPerInch);
    }

    [Fact]
    public void Resolve_TwoDotsAt300DpiIsBelowIndustrialFloorButMeetsDotFloor()
    {
        // 2 dots @ 300 DPI = 2 * 25.4 / 300 ≈ 0.169 mm, under the 0.19 mm floor.
        var resolution = LinearBarcodeModuleContract.Resolve(0.19, 300);

        Assert.Equal(2, resolution.ModuleDots);
        Assert.False(resolution.IsBelowMinimumDots);
        Assert.True(resolution.IsBelowIndustrialFloorMm);
        Assert.True(resolution.HasIndustrialRisk);

        var message = LinearBarcodeModuleContract.FormatIndustrialRiskMessage(resolution);
        Assert.Contains("industrial floor", message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains($"{resolution.ModuleDots} dots at {resolution.Dpi}", message, StringComparison.Ordinal);
        Assert.Contains("7.5", message, StringComparison.Ordinal);
        Assert.Contains("300", message, StringComparison.Ordinal);
        Assert.DoesNotContain("only", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Resolve_ExactIndustrialFloorIsNotBelowFloor()
    {
        // 19 dots at 2540 DPI reconstructs 0.19 mm exactly, so the 1e-9
        // epsilon must not flip the floor comparison.
        var resolution = LinearBarcodeModuleContract.Resolve(0.19, 2540);
        Assert.Equal(19, resolution.ModuleDots);
        Assert.Equal(
            LinearBarcodeModuleContract.MinimumIndustrialXDimensionMm,
            resolution.EffectiveModuleWidthMm,
            precision: 12);
        Assert.False(resolution.IsBelowIndustrialFloorMm);
        Assert.False(resolution.HasIndustrialRisk);
    }

    [Fact]
    public void HasIndustrialRisk_IsTrueWhenEitherFlagIsSet()
    {
        var dotsOnly = new LinearBarcodeModuleResolution(0.1, 203, 1, 0.125, true, false);
        var floorOnly = new LinearBarcodeModuleResolution(0.19, 300, 2, 0.169, false, true);
        var neither = new LinearBarcodeModuleResolution(0.33, 203, 3, 0.375, false, false);

        Assert.True(dotsOnly.HasIndustrialRisk);
        Assert.True(floorOnly.HasIndustrialRisk);
        Assert.False(neither.HasIndustrialRisk);
    }

    [Theory]
    [InlineData(0, 50)]
    [InlineData(-1, 50)]
    [InlineData(double.NaN, 50)]
    [InlineData(40, 0)]
    [InlineData(40, -1)]
    public void EstimateModuleWidthMmFromFrame_RejectsInvalidInputs(double frameWidthMm, int totalModules)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            LinearBarcodeModuleContract.EstimateModuleWidthMmFromFrame(frameWidthMm, totalModules));
        Assert.Contains("must be", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveForObject_NonPositiveAuthoredXUsesFrameEstimate()
    {
        var fromZero = LinearBarcodeModuleContract.ResolveForObject(0, 50, 50, 203);
        var fromNegative = LinearBarcodeModuleContract.ResolveForObject(-0.1, 50, 50, 203);
        var estimated = LinearBarcodeModuleContract.EstimateModuleWidthMmFromFrame(50, 50);

        Assert.Equal(estimated, fromZero.AuthoredModuleWidthMm);
        Assert.Equal(estimated, fromNegative.AuthoredModuleWidthMm);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void SizedFromXWidthMm_RejectsNonPositiveLogicalModules(int logicalModules)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            LinearBarcodeModuleContract.SizedFromXWidthMm(0.33, logicalModules, 203));
        Assert.Contains("must be", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-203)]
    public void OnePrinterDotMm_RejectsNonPositiveDpi(int dpi)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => LinearBarcodeModuleContract.OnePrinterDotMm(dpi));
        Assert.Contains("must be", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UsesSizedFromX_RequiresBothModeAndPositiveAuthoredX()
    {
        Assert.False(LinearBarcodeModuleContract.UsesSizedFromX(BarcodeWidthMode.SizedFromX, -0.1));
        Assert.False(LinearBarcodeModuleContract.UsesSizedFromX(BarcodeWidthMode.FrameOwned, 0.33));
        Assert.True(LinearBarcodeModuleContract.UsesSizedFromX(BarcodeWidthMode.SizedFromX, 0.33));
    }
}
