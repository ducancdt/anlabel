using System.Security.Cryptography;
using System.Text;
using ANLAbel.Core.Enums;

namespace ANLAbel.Core.Printing;

/// <summary>
/// Versioned, platform-neutral identity for an embedded image's raster policy.
/// The encoded payload is already represented by its SHA-256 fingerprint; this
/// contract adds the transform mode and payload length so a preview/print pair
/// cannot silently switch between driver-managed colour and application-owned
/// monochrome output.
/// </summary>
public static class ImageRasterContract
{
    public const string ContractVersion = "image-raster/v1";
    public const string AlgorithmRevision = "luma709-white-alpha-threshold128-bayer4x4";

    public static bool IsSupported(ImageRasterMode mode)
        => mode is ImageRasterMode.DriverManaged
            or ImageRasterMode.MonochromeThreshold
            or ImageRasterMode.MonochromeOrderedDither;

    public static string ComputeFingerprint(
        string payloadFingerprint,
        int payloadLength,
        ImageRasterMode mode,
        int pixelWidth = 0,
        int pixelHeight = 0)
    {
        if (string.IsNullOrWhiteSpace(payloadFingerprint) || payloadLength <= 0)
        {
            return string.Empty;
        }

        if (!IsSupported(mode))
        {
            return string.Empty;
        }

        var canonical = string.Join('|',
            ContractVersion,
            AlgorithmRevision,
            payloadFingerprint.Trim().ToUpperInvariant(),
            payloadLength,
            mode,
            Math.Max(0, pixelWidth),
            Math.Max(0, pixelHeight));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    public static ImageRasterIdentity Describe(
        string payloadFingerprint,
        int payloadLength,
        ImageRasterMode mode,
        int pixelWidth = 0,
        int pixelHeight = 0)
        => new(
            ContractVersion,
            payloadFingerprint ?? string.Empty,
            Math.Max(0, payloadLength),
            mode,
            Math.Max(0, pixelWidth),
            Math.Max(0, pixelHeight),
            ComputeFingerprint(payloadFingerprint ?? string.Empty, payloadLength, mode, pixelWidth, pixelHeight));
}

public sealed record ImageRasterIdentity(
    string ContractVersion,
    string PayloadFingerprint,
    int PayloadLength,
    ImageRasterMode Mode,
    int PixelWidth,
    int PixelHeight,
    string Fingerprint)
{
    public bool IsValid => PayloadLength > 0
        && PixelWidth > 0
        && PixelHeight > 0
        && !string.IsNullOrWhiteSpace(PayloadFingerprint)
        && !string.IsNullOrWhiteSpace(Fingerprint)
        && ImageRasterContract.IsSupported(Mode);
}
