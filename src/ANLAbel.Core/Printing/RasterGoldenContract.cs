using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace ANLAbel.Core.Printing;

/// <summary>
/// Deterministic identity for a rasterized label frame. It captures device axes,
/// pixel geometry, stride and bytes so preview/driver golden fixtures cannot
/// silently compare images rendered at different DPI or row layouts.
/// </summary>
public static class RasterGoldenContract
{
    public const string AlgorithmRevision = "raster-golden-v1";

    public static RasterGoldenIdentity Describe(
        int widthPixels,
        int heightPixels,
        int dpiX,
        int dpiY,
        int stride,
        string pixelFormat,
        ReadOnlySpan<byte> pixels)
    {
        if (widthPixels <= 0 || heightPixels <= 0 || dpiX <= 0 || dpiY <= 0)
        {
            return RasterGoldenIdentity.Invalid("dimensions-or-dpi-invalid");
        }

        if (widthPixels > int.MaxValue / 4)
        {
            return RasterGoldenIdentity.Invalid("width-overflow");
        }

        var minimumStride = widthPixels * 4;
        if (stride < minimumStride || stride <= 0 || heightPixels > int.MaxValue / stride)
        {
            return RasterGoldenIdentity.Invalid("pixel-buffer-invalid");
        }

        var payloadLength = stride * heightPixels;
        if (pixels.Length < payloadLength)
        {
            return RasterGoldenIdentity.Invalid("pixel-buffer-invalid");
        }

        if (string.IsNullOrWhiteSpace(pixelFormat))
        {
            return RasterGoldenIdentity.Invalid("pixel-format-missing");
        }

        var canonicalHeader = string.Join('|', new[]
        {
            AlgorithmRevision,
            widthPixels.ToString(CultureInfo.InvariantCulture),
            heightPixels.ToString(CultureInfo.InvariantCulture),
            dpiX.ToString(CultureInfo.InvariantCulture),
            dpiY.ToString(CultureInfo.InvariantCulture),
            stride.ToString(CultureInfo.InvariantCulture),
            pixelFormat.Trim().ToUpperInvariant()
        });
        var headerBytes = Encoding.UTF8.GetBytes(canonicalHeader);
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        hash.AppendData(headerBytes);
        hash.AppendData(new byte[] { 0 });
        hash.AppendData(pixels[..payloadLength]);
        return new RasterGoldenIdentity(
            AlgorithmRevision,
            widthPixels,
            heightPixels,
            dpiX,
            dpiY,
            stride,
            pixelFormat.Trim().ToUpperInvariant(),
            Convert.ToHexString(hash.GetHashAndReset()));
    }

    public static bool Matches(
        RasterGoldenIdentity expected,
        int widthPixels,
        int heightPixels,
        int dpiX,
        int dpiY,
        int stride,
        string pixelFormat,
        ReadOnlySpan<byte> pixels)
    {
        if (!expected.IsValid)
        {
            return false;
        }

        var actual = Describe(widthPixels, heightPixels, dpiX, dpiY, stride, pixelFormat, pixels);
        return actual.IsValid
            && string.Equals(actual.Fingerprint, expected.Fingerprint, StringComparison.Ordinal);
    }
}

public sealed record RasterGoldenIdentity(
    string AlgorithmRevision,
    int WidthPixels,
    int HeightPixels,
    int DpiX,
    int DpiY,
    int Stride,
    string PixelFormat,
    string Fingerprint)
{
    public bool IsValid => string.Equals(AlgorithmRevision, RasterGoldenContract.AlgorithmRevision, StringComparison.Ordinal)
        && WidthPixels > 0
        && HeightPixels > 0
        && DpiX > 0
        && DpiY > 0
        && WidthPixels <= int.MaxValue / 4
        && Stride > 0
        && HeightPixels <= int.MaxValue / Stride
        && Stride >= WidthPixels * 4
        && !string.IsNullOrWhiteSpace(PixelFormat)
        && !string.IsNullOrWhiteSpace(Fingerprint);

    public static RasterGoldenIdentity Invalid(string reason)
        => new(
            RasterGoldenContract.AlgorithmRevision,
            0,
            0,
            0,
            0,
            0,
            reason,
            string.Empty);
}
