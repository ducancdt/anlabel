using ANLAbel.Core.Enums;
using ANLAbel.Core.Printing;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class ThermalRasterGoldenContractTests
{
    [Fact]
    public void ProfileFingerprintBindsDriverFirmwareMaterialAndCalibration()
    {
        var baseline = CreateProfile();
        var firmwareChanged = baseline with { FirmwareVersion = "FW-2" };
        var mediaChanged = baseline with { MediaIdentity = "STOCK-B" };

        Assert.True(baseline.IsValid);
        Assert.NotEqual(baseline.Fingerprint, ThermalRasterGoldenContract.ComputeProfileFingerprint(firmwareChanged));
        Assert.NotEqual(baseline.Fingerprint, ThermalRasterGoldenContract.ComputeProfileFingerprint(mediaChanged));
    }

    [Fact]
    public void MissingPhysicalContextCannotCreateAValidProfile()
    {
        var created = ThermalRasterGoldenContract.TryCreateProfile(
            "Zebra-01",
            "ZDesigner",
            "",
            "FW-1",
            "STOCK-A",
            "RIBBON-NONE",
            "CAL-2026-08",
            LabelMediaType.Gap,
            FeedDirection.TopToBottom,
            false,
            203,
            203,
            100,
            50,
            2,
            0,
            0,
            1,
            1,
            out var profile,
            out var error);

        Assert.False(created);
        Assert.Equal("thermal-profile-identifiers-missing", error);
        Assert.False(profile.IsValid);
    }

    [Fact]
    public void BindingMatchesOnlyTheReviewedProfileAndRaster()
    {
        var profile = CreateProfile();
        var raster = RasterGoldenContract.Describe(4, 2, 203, 305, 16, "Pbgra32", new byte[32]);
        var binding = ThermalRasterGoldenContract.CreateBinding("zebra-stock-a-v1", profile, raster);

        var accepted = ThermalRasterGoldenContract.Validate(binding, profile, raster);
        var changedProfile = CreateProfile("CAL-2026-09");
        var changedRaster = RasterGoldenContract.Describe(4, 2, 203, 305, 16, "Pbgra32", new byte[31].Append((byte)1).ToArray());

        Assert.True(binding.IsValid);
        Assert.True(accepted.IsAccepted);
        Assert.Equal("profile-mismatch", ThermalRasterGoldenContract.Validate(binding, changedProfile, raster).Code);
        Assert.Equal("raster-mismatch", ThermalRasterGoldenContract.Validate(binding, profile, changedRaster).Code);
    }

    [Fact]
    public void InvalidRasterCannotProduceAnAcceptedBinding()
    {
        var profile = CreateProfile();
        var invalidRaster = RasterGoldenIdentity.Invalid("bad-frame");

        var created = ThermalRasterGoldenContract.TryCreateBinding(
            "golden",
            profile,
            invalidRaster,
            out var binding,
            out var error);

        Assert.False(created);
        Assert.Equal("raster-golden-invalid", error);
        Assert.False(binding.IsValid);
        Assert.False(ThermalRasterGoldenContract.Validate(binding, profile, invalidRaster).IsAccepted);
    }

    [Fact]
    public void BindingTamperingFailsSelfValidation()
    {
        var profile = CreateProfile();
        var raster = RasterGoldenContract.Describe(2, 2, 203, 203, 8, "Pbgra32", new byte[16]);
        var binding = ThermalRasterGoldenContract.CreateBinding("golden", profile, raster);

        Assert.False((binding with { GoldenId = "other" }).IsValid);
        Assert.False((binding with { RasterFingerprint = new string('A', 64) }).IsValid);
    }

    [Fact]
    public void ProfileNormalizationKeepsEquivalentWhitespaceStable()
    {
        var first = CreateProfile();
        var second = ThermalRasterGoldenContract.CreateProfile(
            "  Zebra-01  ",
            " ZDesigner ",
            "DRV-1",
            "FW-1",
            "STOCK-A",
            "RIBBON-NONE",
            "CAL-2026-08",
            LabelMediaType.Gap,
            FeedDirection.TopToBottom,
            false,
            203,
            203,
            100,
            50,
            2,
            0,
            0,
            1,
            1);

        Assert.Equal(first.Fingerprint, second.Fingerprint);
    }

    private static ThermalRasterProfile CreateProfile(string calibrationIdentity = "CAL-2026-08")
        => ThermalRasterGoldenContract.CreateProfile(
            "Zebra-01",
            "ZDesigner",
            "DRV-1",
            "FW-1",
            "STOCK-A",
            "RIBBON-NONE",
            calibrationIdentity,
            LabelMediaType.Gap,
            FeedDirection.TopToBottom,
            false,
            203,
            203,
            100,
            50,
            2,
            0,
            0,
            1,
            1);
}
