using ANLAbel.Core.Enums;
using ANLAbel.Core.Printing;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class ImageRasterContractTests
{
    [Fact]
    public void FingerprintIncludesModeAndObservedDimensions()
    {
        var first = ImageRasterContract.ComputeFingerprint("payload", 128, ImageRasterMode.DriverManaged, 100, 50);
        var threshold = ImageRasterContract.ComputeFingerprint("payload", 128, ImageRasterMode.MonochromeThreshold, 100, 50);
        var dimensions = ImageRasterContract.ComputeFingerprint("payload", 128, ImageRasterMode.DriverManaged, 101, 50);

        Assert.NotEqual(first, threshold);
        Assert.NotEqual(first, dimensions);
        Assert.NotEqual(threshold, dimensions);
    }

    [Fact]
    public void DescribeRequiresCompleteDecodedIdentity()
    {
        var identity = ImageRasterContract.Describe(
            "AABB",
            4,
            ImageRasterMode.MonochromeOrderedDither,
            24,
            12);

        Assert.True(identity.IsValid);
        Assert.Equal(ImageRasterMode.MonochromeOrderedDither, identity.Mode);
        Assert.Equal(24, identity.PixelWidth);
        Assert.Equal(12, identity.PixelHeight);
        Assert.Equal(ImageRasterContract.ContractVersion, identity.ContractVersion);
    }

    [Fact]
    public void MissingPayloadOrUnsupportedModeHasNoIdentity()
    {
        Assert.Equal(string.Empty, ImageRasterContract.ComputeFingerprint("", 4, ImageRasterMode.DriverManaged, 1, 1));
        Assert.Equal(string.Empty, ImageRasterContract.ComputeFingerprint("payload", 0, ImageRasterMode.DriverManaged, 1, 1));
        Assert.Equal(string.Empty, ImageRasterContract.ComputeFingerprint("payload", 4, (ImageRasterMode)99, 1, 1));
        Assert.False(ImageRasterContract.IsSupported((ImageRasterMode)99));
    }
}
