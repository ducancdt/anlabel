using ANLAbel.Barcode.Options;
using ANLAbel.Barcode.Renderers;
using ANLAbel.Core.Barcode;
using ANLAbel.Core.Enums;
using ANLAbel.Core.Models;
using Xunit;
using ZXing;
using ZXing.Common;

namespace ANLAbel.UnitTests;

public sealed class AdvancedLinearBarcodeTests
{
    private static readonly ZxingBarcodeRenderer Renderer = new();

    /// <summary>Decodes a rendered vector row the way a scanner would read it.</summary>
    private static string? Decode(BarcodeVectorData vector, BarcodeFormat format)
    {
        const int height = 20;
        var width = vector.RowBits.Length;
        var gray = new byte[width * height];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                gray[(y * width) + x] = vector.RowBits[x] ? (byte)0 : (byte)255;
            }
        }

        var source = new RGBLuminanceSource(gray, width, height, RGBLuminanceSource.BitmapFormat.Gray8);
        var hints = new Dictionary<DecodeHintType, object> { [DecodeHintType.PURE_BARCODE] = true };
        Reader reader = format == BarcodeFormat.CODE_39 ? new ZXing.OneD.Code39Reader() : new ZXing.OneD.Code128Reader();
        return reader.decode(new BinaryBitmap(new HybridBinarizer(source)), hints)?.Text;
    }

    [Fact]
    public void ManualCode128MatchesZxingWhenItUsesOneCodeSet()
    {
        var manual = Renderer.RenderBarcodeVector("<MODEB>Abc-12", BarcodeType.Code128, 40, 10, 300,
            new BarcodeRenderOptions { QuietZoneModules = 10, Code128CodeSet = Code128CodeSet.Manual });
        var forced = Renderer.RenderBarcodeVector("Abc-12", BarcodeType.Code128, 40, 10, 300,
            new BarcodeRenderOptions { QuietZoneModules = 10, Code128CodeSet = Code128CodeSet.B });

        Assert.NotNull(manual);
        Assert.NotNull(forced);
        Assert.Equal(forced!.RowBits, manual!.RowBits);
    }

    [Theory]
    [InlineData("<MODEC>123456<MODEB>Lot-A<MODEC>0099", "123456Lot-A0099")]
    [InlineData("<MODEA>ABC<MODEB>abc", "ABCabc")]
    public void ManualCode128SwitchesSetsAndScansBackToTheData(string authored, string expected)
    {
        var vector = Renderer.RenderBarcodeVector(authored, BarcodeType.Code128, 60, 10, 300,
            new BarcodeRenderOptions { QuietZoneModules = 10, Code128CodeSet = Code128CodeSet.Manual });

        Assert.NotNull(vector);
        Assert.Equal(expected, Decode(vector!, BarcodeFormat.CODE_128));
    }

    [Theory]
    [InlineData("ABC", "must start with")]
    [InlineData("<MODEC>123", "odd number of digits")]
    [InlineData("<MODEA>abc", "cannot encode 'a'")]
    [InlineData("<MODEB>", "no characters")]
    public void InvalidManualCode128IsExplained(string authored, string expected)
    {
        Assert.False(Code128ManualEncoding.TryParse(authored, out _, out var error));
        Assert.Contains(expected, error);
    }

    [Theory]
    [InlineData(Code128CodeSet.A, "ABC-12")]
    [InlineData(Code128CodeSet.C, "123456")]
    public void ForcedCode128SetsStillScan(Code128CodeSet set, string data)
    {
        var vector = Renderer.RenderBarcodeVector(data, BarcodeType.Code128, 40, 10, 300,
            new BarcodeRenderOptions { QuietZoneModules = 10, Code128CodeSet = set });

        Assert.NotNull(vector);
        Assert.Equal(data, Decode(vector!, BarcodeFormat.CODE_128));
    }

    [Fact]
    public void ManualMarkersAreNotPrintedUnderTheBarcode()
    {
        var item = new LabelObject
        {
            Type = ObjectType.BarcodeCode128,
            BarcodeSymbology = BarcodeSymbology.Code128,
            Code128CodeSet = Code128CodeSet.Manual
        };

        Assert.Equal("1234AB", BarcodeCheckDigitContract.FormatHriText(item, BarcodeSymbology.Code128, "<MODEC>1234<modeb>AB"));
        Assert.Equal("1234AB", BarcodeCheckDigitContract.HriMeasureText(item, "<MODEC>1234<MODEB>AB"));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public void Code39GapWidensOnlyTheSpaceBetweenCharactersAndStillScans(int gap)
    {
        var options = new BarcodeRenderOptions { QuietZoneModules = 10, Code39IntercharacterGapModules = gap };
        var vector = Renderer.RenderBarcodeVector("AB12", BarcodeType.Code39, 50, 10, 300, options);
        var legacyModules = Renderer.CountLinearModules("AB12", BarcodeType.Code39, new BarcodeRenderOptions { QuietZoneModules = 10 });
        var modules = Renderer.CountLinearModules("AB12", BarcodeType.Code39, options);

        Assert.NotNull(vector);
        Assert.Equal("AB12", Decode(vector!, BarcodeFormat.CODE_39));
        // *AB12* has 5 gaps between its 6 characters.
        Assert.Equal(legacyModules + (5 * (gap - 1)), modules);
    }

    [Fact]
    public void BearerThicknessFollowsTheNarrowBarWhenSetInModules()
    {
        var item = new LabelObject { BearerBarThicknessMm = 1.5, BearerBarThicknessModules = 4 };

        Assert.Equal(4 * 0.254, BearerBarContract.ResolveThicknessMm(item, 0.254), 9);
        Assert.Equal(1.5, BearerBarContract.ResolveThicknessMm(item, null));
        item.BearerBarThicknessModules = 0;
        Assert.Equal(1.5, BearerBarContract.ResolveThicknessMm(item, 0.254));
        item.BearerBarThicknessModules = 99;
        Assert.Equal(10, item.BearerBarThicknessModules);
    }

    [Fact]
    public void SharedOptionsCarryEveryLinearSetting()
    {
        var item = new LabelObject
        {
            QrQuietZoneModules = 7,
            Code39WideNarrowRatio = Code39WideNarrowRatio.Ratio2_5,
            Code39IntercharacterGapModules = 2,
            Code128CodeSet = Code128CodeSet.C,
            BearerBarStyle = BearerBarStyle.Frame,
            BearerBarThicknessMm = 2
        };

        var options = BarcodeRenderOptions.FromObject(item);

        Assert.Equal(
            (7, Code39WideNarrowRatio.Ratio2_5, 2, Code128CodeSet.C, BearerBarStyle.Frame, 2.0),
            (options.QuietZoneModules, options.Code39WideNarrowRatio, options.Code39IntercharacterGapModules,
             options.Code128CodeSet, options.BearerBarStyle, options.BearerBarThicknessMm));
        var clone = LabelObjectCloner.Clone(item);
        var snapshot = Core.Scene.SceneObjectSnapshot.Capture(item);
        Assert.Equal((2, Code128CodeSet.C), (clone.Code39IntercharacterGapModules, clone.Code128CodeSet));
        Assert.Equal((2, Code128CodeSet.C), (snapshot.Code39IntercharacterGapModules, snapshot.Code128CodeSet));
    }
}
