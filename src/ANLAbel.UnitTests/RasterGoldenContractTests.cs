using ANLAbel.Core.Printing;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class RasterGoldenContractTests
{
    [Fact]
    public void SameFrameProducesStableFingerprint()
    {
        var pixels = CreatePixels(3, 2, 16);

        var first = RasterGoldenContract.Describe(3, 2, 203, 609, 16, "BGRA32", pixels);
        var second = RasterGoldenContract.Describe(3, 2, 203, 609, 16, "BGRA32", pixels);

        Assert.True(first.IsValid);
        Assert.Equal(first.Fingerprint, second.Fingerprint);
        Assert.True(RasterGoldenContract.Matches(second, 3, 2, 203, 609, 16, "BGRA32", pixels));
    }

    [Fact]
    public void AnyDeviceOrPixelChangeBreaksGoldenMatch()
    {
        var pixels = CreatePixels(3, 2, 16);
        var golden = RasterGoldenContract.Describe(3, 2, 203, 609, 16, "BGRA32", pixels);
        var changed = pixels.ToArray();
        changed[5] ^= 0xFF;

        Assert.False(RasterGoldenContract.Matches(golden, 3, 2, 300, 609, 16, "BGRA32", pixels));
        Assert.False(RasterGoldenContract.Matches(golden, 3, 2, 203, 609, 16, "BGRA32", changed));
    }

    [Fact]
    public void InvalidBufferAndFormatFailClosed()
    {
        var invalidStride = RasterGoldenContract.Describe(3, 2, 203, 203, 8, "BGRA32", new byte[16]);
        var invalidFormat = RasterGoldenContract.Describe(3, 2, 203, 203, 16, "", new byte[32]);
        var impossibleIdentity = new RasterGoldenIdentity(
            RasterGoldenContract.AlgorithmRevision,
            1,
            int.MaxValue,
            203,
            203,
            4,
            "BGRA32",
            "ABC");

        Assert.False(invalidStride.IsValid);
        Assert.False(invalidFormat.IsValid);
        Assert.False(impossibleIdentity.IsValid);
    }

    private static byte[] CreatePixels(int width, int height, int stride)
    {
        var pixels = new byte[stride * height];
        for (var index = 0; index < pixels.Length; index++)
        {
            pixels[index] = (byte)((index * 17 + width + height) % 251);
        }

        return pixels;
    }
}
