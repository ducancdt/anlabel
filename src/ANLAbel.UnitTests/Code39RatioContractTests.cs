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
