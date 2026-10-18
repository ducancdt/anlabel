using ANLAbel.Barcode.Options;
using ANLAbel.Barcode.Renderers;
using ANLAbel.Core.Barcode;
using ANLAbel.Core.Enums;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class Code39RatioContractTests
{
    [Fact]
    public void PublicUss39BoundsAreTheAuthoredConstants()
    {
        Assert.Equal(0.508, Code39RatioContract.Ratio2MinimumXmm);
        Assert.Equal(2.54, Code39RatioContract.MinimumQuietZoneMmPerSide);
    }

    [Theory]
    [InlineData(Code39WideNarrowRatio.LegacyEngineDefault)]
    [InlineData(Code39WideNarrowRatio.Ratio2_0)]
    [InlineData(Code39WideNarrowRatio.Ratio2_2)]
    [InlineData(Code39WideNarrowRatio.Ratio2_5)]
    [InlineData(Code39WideNarrowRatio.Ratio3_0)]
    public void IsSupported_AcceptsEveryAuthoredRatio(Code39WideNarrowRatio ratio)
    {
        Assert.True(Code39RatioContract.IsSupported(ratio));
    }

    [Fact]
    public void IsSupported_RejectsUnknownRatio()
    {
        Assert.False(Code39RatioContract.IsSupported((Code39WideNarrowRatio)99));
    }

    [Fact]
    public void ToValue_MapsExplicitRatiosAndLeavesLegacyUnresolved()
    {
        Assert.Equal(2.0, Code39RatioContract.ToValue(Code39WideNarrowRatio.Ratio2_0));
        Assert.Equal(2.2, Code39RatioContract.ToValue(Code39WideNarrowRatio.Ratio2_2));
        Assert.Equal(2.5, Code39RatioContract.ToValue(Code39WideNarrowRatio.Ratio2_5));
        Assert.Equal(3.0, Code39RatioContract.ToValue(Code39WideNarrowRatio.Ratio3_0));
        Assert.Null(Code39RatioContract.ToValue(Code39WideNarrowRatio.LegacyEngineDefault));
        Assert.Null(Code39RatioContract.ToValue((Code39WideNarrowRatio)99));
    }

    [Fact]
    public void Ratio2RequiresAtLeastTheUss39MinimumEffectiveX()
    {
        Assert.False(Code39RatioContract.IsLegal(Code39WideNarrowRatio.Ratio2_0, 0.507));
        Assert.True(Code39RatioContract.IsLegal(
            Code39WideNarrowRatio.Ratio2_0,
            Code39RatioContract.Ratio2MinimumXmm - 1e-9));
        Assert.True(Code39RatioContract.IsLegal(Code39WideNarrowRatio.Ratio2_0, Code39RatioContract.Ratio2MinimumXmm));
        Assert.True(Code39RatioContract.IsLegal(Code39WideNarrowRatio.Ratio2_2, 0.19));
        Assert.True(Code39RatioContract.IsLegal(Code39WideNarrowRatio.Ratio2_5, 0.19));
        Assert.True(Code39RatioContract.IsLegal(Code39WideNarrowRatio.Ratio3_0, 0.19));
        Assert.True(Code39RatioContract.IsLegal(Code39WideNarrowRatio.LegacyEngineDefault, 0.19));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void IsLegal_RejectsNonPositiveOrNonFiniteX(double effectiveXmm)
    {
        Assert.False(Code39RatioContract.IsLegal(Code39WideNarrowRatio.Ratio3_0, effectiveXmm));
        Assert.False(Code39RatioContract.IsLegal((Code39WideNarrowRatio)99, 0.6));
    }

    [Fact]
    public void RequiredQuietZoneUsesTenModulesOrTheUss39Floor()
    {
        var x = LinearBarcodeModuleContract.Resolve(0.33, 300);
        Assert.True(x.EffectiveModuleWidthMm * 10 > Code39RatioContract.MinimumQuietZoneMmPerSide);
        Assert.Equal(
            x.EffectiveModuleWidthMm * 10,
            Code39RatioContract.RequiredQuietZoneMmPerSide(x.EffectiveModuleWidthMm),
            precision: 9);

        Assert.Equal(
            Code39RatioContract.MinimumQuietZoneMmPerSide,
            Code39RatioContract.RequiredQuietZoneMmPerSide(0.2));
        Assert.Equal(
            Code39RatioContract.MinimumQuietZoneMmPerSide,
            Code39RatioContract.RequiredQuietZoneMmPerSide(0.254));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void RequiredQuietZone_RejectsInvalidX(double effectiveXmm)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Code39RatioContract.RequiredQuietZoneMmPerSide(effectiveXmm));
    }

    [Fact]
    public void ObservedQuietZoneUsesNonNegativeModulesTimesResolvedX()
    {
        var x = LinearBarcodeModuleContract.Resolve(0.33, 300);
        Assert.Equal(0, Code39RatioContract.ObservedQuietZoneMmPerSide(0, x));
        Assert.Equal(0, Code39RatioContract.ObservedQuietZoneMmPerSide(-4, x));
        Assert.Equal(10 * x.EffectiveModuleWidthMm, Code39RatioContract.ObservedQuietZoneMmPerSide(10, x), precision: 9);
    }

    [Fact]
    public void ExplicitRatioChangesCode39VectorPattern()
    {
        var renderer = new ZxingBarcodeRenderer();
        var legacy = renderer.RenderBarcodeVector("ABC", BarcodeType.Code39, 40, 10, 300,
            new BarcodeRenderOptions { QuietZoneModules = 10 });
        var ratio = renderer.RenderBarcodeVector("ABC", BarcodeType.Code39, 40, 10, 300,
            new BarcodeRenderOptions { QuietZoneModules = 10, Code39WideNarrowRatio = Code39WideNarrowRatio.Ratio3_0 });

        Assert.NotNull(legacy);
        Assert.NotNull(ratio);
        Assert.False(legacy!.RowBits.SequenceEqual(ratio!.RowBits));
    }

    [Theory]
    [InlineData(1, 2.2, 3)]
    [InlineData(2, 2.2, 5)]
    [InlineData(3, 2.2, 7)]
    [InlineData(2, 2.5, 5)]
    [InlineData(3, 2.5, 8)]
    [InlineData(1, 2.0, 2)]
    [InlineData(2, 3.0, 6)]
    public void ResolveWideDots_StaysInsideTheLegalRealizedBand(int narrowDots, double ratio, int expectedWideDots)
    {
        var wide = Code39RatioContract.ResolveWideDots(narrowDots, ratio);

        Assert.Equal(expectedWideDots, wide);
        Assert.InRange(
            (double)wide / narrowDots,
            Code39RatioContract.MinimumRealizedRatio(ratio) - 1e-9,
            Code39RatioContract.MaximumRealizedRatio + 1e-9);
    }

    [Theory]
    [InlineData(Code39WideNarrowRatio.Ratio2_2, 203)]
    [InlineData(Code39WideNarrowRatio.Ratio2_2, 300)]
    [InlineData(Code39WideNarrowRatio.Ratio2_5, 203)]
    [InlineData(Code39WideNarrowRatio.Ratio3_0, 300)]
    [InlineData(Code39WideNarrowRatio.Ratio2_0, 600)]
    public void ExplicitRatioPrintsUniformWholeDotElements(Code39WideNarrowRatio ratio, int dpi)
    {
        var renderer = new ZxingBarcodeRenderer();
        var options = new BarcodeRenderOptions { QuietZoneModules = 10, Code39WideNarrowRatio = ratio };

        var vector = renderer.RenderBarcodeVector("ABC123", BarcodeType.Code39, 37, 10, dpi, options);

        Assert.NotNull(vector);
        var widths = InteriorRunLengths(vector!.RowBits).Distinct().Order().ToArray();
        Assert.Equal(2, widths.Length);
        Assert.InRange(
            (double)widths[1] / widths[0],
            Code39RatioContract.MinimumRealizedRatio(Code39RatioContract.ToValue(ratio)!.Value) - 1e-9,
            Code39RatioContract.MaximumRealizedRatio + 1e-9);
    }

    [Fact]
    public void ExplicitRatioWithoutQuietZoneKeepsTheOuterBars()
    {
        var renderer = new ZxingBarcodeRenderer();
        var options = new BarcodeRenderOptions { QuietZoneModules = 0, Code39WideNarrowRatio = Code39WideNarrowRatio.Ratio2_5 };

        var vector = renderer.RenderBarcodeVector("ABC", BarcodeType.Code39, 30, 10, 300, options);

        Assert.NotNull(vector);
        var bits = vector!.RowBits;
        var firstBar = Array.IndexOf(bits, true);
        var lastBar = Array.LastIndexOf(bits, true);
        Assert.True(firstBar >= 0);
        Assert.Equal(bits.Length - 1 - lastBar, firstBar, tolerance: 1);
        Assert.Equal(2, InteriorRunLengths(bits).Distinct().Count());
    }

    [Theory]
    [InlineData(Code39WideNarrowRatio.Ratio2_2)]
    [InlineData(Code39WideNarrowRatio.Ratio2_5)]
    [InlineData(Code39WideNarrowRatio.Ratio3_0)]
    public void ExplicitRatioCountsModulesInXUnits(Code39WideNarrowRatio ratio)
    {
        var renderer = new ZxingBarcodeRenderer();
        var legacy = renderer.CountLinearModules("ABC123", BarcodeType.Code39, new BarcodeRenderOptions { QuietZoneModules = 10 });
        var explicitRatio = renderer.CountLinearModules(
            "ABC123",
            BarcodeType.Code39,
            new BarcodeRenderOptions { QuietZoneModules = 10, Code39WideNarrowRatio = ratio });

        Assert.NotNull(legacy);
        Assert.NotNull(explicitRatio);
        // ZXing's legacy count uses wide = 2X; an explicit ratio widens every wide element.
        Assert.True(explicitRatio > legacy, $"explicit={explicitRatio} legacy={legacy}");
    }

    private static List<int> InteriorRunLengths(bool[] bits)
    {
        var first = Array.IndexOf(bits, true);
        var last = Array.LastIndexOf(bits, true);
        var runs = new List<int>();
        var length = 0;
        for (var x = first; x <= last; x++)
        {
            length++;
            if (x == last || bits[x + 1] != bits[x])
            {
                runs.Add(length);
                length = 0;
            }
        }

        return runs;
    }

    [Fact]
    public void ClonerAndSnapshotPreserveCode39WideNarrowRatio()
    {
        var original = new Core.Models.LabelObject
        {
            Type = ObjectType.BarcodeCode128,
            BarcodeSymbology = BarcodeSymbology.Code39,
            Code39WideNarrowRatio = Code39WideNarrowRatio.Ratio2_5,
            BarcodeModuleWidthMm = 0.33,
            QrQuietZoneModules = 10
        };

        var clone = Core.Models.LabelObjectCloner.Clone(original);
        Assert.Equal(Code39WideNarrowRatio.Ratio2_5, clone.Code39WideNarrowRatio);

        var snapshot = Core.Scene.SceneObjectSnapshot.Capture(original);
        Assert.Equal(Code39WideNarrowRatio.Ratio2_5, snapshot.Code39WideNarrowRatio);
    }
}
