using ANLAbel.Barcode.Options;
using ANLAbel.Barcode.Renderers;
using ANLAbel.Core.Enums;
using ANLAbel.Core.Models;
using ANLAbel.Core.Scene;
using ANLAbel.Project.SaveLoad;
using Xunit;
using ZXing;
using ZXing.Common;

namespace ANLAbel.UnitTests;

/// <summary>
/// PDF417 error-correction level and truncated-symbol contract, adopted from
/// the NiceLabel 5 "Security" page (nine security levels and a truncated
/// option; docs/NICELABEL_5_DEEP_REVERSE_ENGINEERING.md §10.7). Auto (-1)
/// must keep the pre-field symbol, explicit levels must change the symbol
/// and still decode, and data that no longer fits must fail instead of
/// silently printing a weaker symbol.
/// </summary>
public sealed class Pdf417ContractTests
{
    private static readonly ZxingBarcodeRenderer Renderer = new();
    private const string Sample = "ANLABEL-PDF417-TEST-123456";

    /// <summary>Renders into a frame too small for scaling, so the image is the native module matrix (1 dot per module).</summary>
    private static BarcodePixelImage RenderNative(string data, BarcodeRenderOptions options)
        => Renderer.RenderBarcode(data, BarcodeType.Pdf417, 5, 2, 96, options);

    private static string? Decode(BarcodePixelImage image)
    {
        var source = new RGBLuminanceSource(
            image.BgraPixels,
            image.WidthPixels,
            image.HeightPixels,
            RGBLuminanceSource.BitmapFormat.BGRA32);
        var hints = new Dictionary<DecodeHintType, object>
        {
            [DecodeHintType.POSSIBLE_FORMATS] = new[] { BarcodeFormat.PDF_417 },
            [DecodeHintType.TRY_HARDER] = true
        };
        return new MultiFormatReader().decode(new BinaryBitmap(new HybridBinarizer(source)), hints)?.Text;
    }

    [Fact]
    public void AutoLevelRendersTheLegacySymbol()
    {
        var legacy = RenderNative(Sample, new BarcodeRenderOptions());
        var auto = RenderNative(Sample, new BarcodeRenderOptions { Pdf417ErrorCorrectionLevel = -1 });

        Assert.Equal(legacy.BgraPixels, auto.BgraPixels);
        // ZXing's encoder default is level 2: probing the shipped encoder
        // gave the same native matrix size for "no hint" and EC2.
        var ec2 = RenderNative(Sample, new BarcodeRenderOptions { Pdf417ErrorCorrectionLevel = 2 });
        Assert.Equal(
            (legacy.WidthPixels, legacy.HeightPixels),
            (ec2.WidthPixels, ec2.HeightPixels));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(8)]
    public void ExplicitLevelsStillDecodeToTheData(int level)
    {
        var image = Renderer.RenderBarcode(
            Sample, BarcodeType.Pdf417, 100, 40, 300,
            new BarcodeRenderOptions { Pdf417ErrorCorrectionLevel = level });

        Assert.Equal(Sample, Decode(image));
    }

    [Fact]
    public void HigherLevelsMakeABiggerSymbol()
    {
        var ec0 = RenderNative(Sample, new BarcodeRenderOptions { Pdf417ErrorCorrectionLevel = 0 });
        var ec8 = RenderNative(Sample, new BarcodeRenderOptions { Pdf417ErrorCorrectionLevel = 8 });

        Assert.True(ec8.WidthPixels > ec0.WidthPixels);
        Assert.True(ec8.HeightPixels > ec0.HeightPixels);
    }

    [Fact]
    public void TruncatedSymbolIsNarrowerAndStillDecodes()
    {
        var full = RenderNative(Sample, new BarcodeRenderOptions());
        var truncated = RenderNative(Sample, new BarcodeRenderOptions { Pdf417Truncated = true });

        Assert.True(truncated.WidthPixels < full.WidthPixels);

        var printed = Renderer.RenderBarcode(
            Sample, BarcodeType.Pdf417, 100, 40, 300,
            new BarcodeRenderOptions { Pdf417Truncated = true });
        Assert.Equal(Sample, Decode(printed));
    }

    [Fact]
    public void DataThatOnlyFitsAtALowLevelFailsAtLevel8()
    {
        var data = new string('A', 1200);

        var low = RenderNative(data, new BarcodeRenderOptions { Pdf417ErrorCorrectionLevel = 0 });
        Assert.True(low.WidthPixels > 0);
        Assert.ThrowsAny<Exception>(() =>
            RenderNative(data, new BarcodeRenderOptions { Pdf417ErrorCorrectionLevel = 8 }));
    }

    [Fact]
    public void SettingsRoundTripThroughClonerSnapshotAndOptions()
    {
        var item = new LabelObject
        {
            Type = ObjectType.BarcodeCode128,
            BarcodeSymbology = BarcodeSymbology.Pdf417,
            Pdf417ErrorCorrectionLevel = 5,
            Pdf417Truncated = true
        };

        var options = BarcodeRenderOptions.FromObject(item);
        Assert.Equal((5, true), (options.Pdf417ErrorCorrectionLevel, options.Pdf417Truncated));

        var clone = LabelObjectCloner.Clone(item);
        Assert.Equal((5, true), (clone.Pdf417ErrorCorrectionLevel, clone.Pdf417Truncated));

        var snapshot = SceneObjectSnapshot.Capture(item);
        Assert.Equal((5, true), (snapshot.Pdf417ErrorCorrectionLevel, snapshot.Pdf417Truncated));

        // Out-of-range authored values clamp to the ISO range, never throw.
        item.Pdf417ErrorCorrectionLevel = 99;
        Assert.Equal(8, item.Pdf417ErrorCorrectionLevel);
        item.Pdf417ErrorCorrectionLevel = -7;
        Assert.Equal(-1, item.Pdf417ErrorCorrectionLevel);
    }

    [Fact]
    public void DefaultsKeepTheDocumentHashAndExplicitValuesChangeIt()
    {
        static LabelTemplate Template(int level, bool truncated) => new()
        {
            WidthMm = 100,
            HeightMm = 60,
            Objects =
            [
                new LabelObject
                {
                    Id = "pdf417-object",
                    Type = ObjectType.BarcodeCode128,
                    BarcodeSymbology = BarcodeSymbology.Pdf417,
                    Text = Sample,
                    Pdf417ErrorCorrectionLevel = level,
                    Pdf417Truncated = truncated
                }
            ]
        };

        var legacyHash = DocumentSnapshot.Capture(Template(-1, false)).DocumentHash;
        var explicitHash = DocumentSnapshot.Capture(Template(5, false)).DocumentHash;
        var truncatedHash = DocumentSnapshot.Capture(Template(-1, true)).DocumentHash;

        Assert.NotEqual(legacyHash, explicitHash);
        Assert.NotEqual(legacyHash, truncatedHash);
    }

    [Fact]
    public async Task SettingsSurviveSaveAndLoad()
    {
        var service = new ProjectFileService();
        var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.anlabel");
        try
        {
            var template = new LabelTemplate
            {
                WidthMm = 100,
                HeightMm = 60,
                Objects =
                [
                    new LabelObject
                    {
                        Type = ObjectType.BarcodeCode128,
                        BarcodeSymbology = BarcodeSymbology.Pdf417,
                        Text = Sample,
                        Pdf417ErrorCorrectionLevel = 6,
                        Pdf417Truncated = true
                    }
                ]
            };

            await service.SaveAsync(template, tempFile);
            var loaded = await service.LoadAsync(tempFile);

            var item = Assert.Single(loaded.Objects);
            Assert.Equal((6, true), (item.Pdf417ErrorCorrectionLevel, item.Pdf417Truncated));
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
}
