using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using ANLAbel.Core.Enums;

namespace ANLAbel.Core.Text;

/// <summary>
/// Creates a stable, value-only identity for a rendered text layout.  Glyph
/// shaping remains a renderer responsibility, but designer, preview, print and
/// preflight can prove that they used the same normalized value, resource
/// policy, frame, direction and measured metrics.
/// </summary>
public static class TextLayoutIdentityContract
{
    public const string ContractVersion = "text-layout-identity-v1";

    public static string ComputeFingerprint(TextLayoutIdentityInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var canonical = string.Join(
            "|",
            ContractVersion,
            $"text={input.TextHash}",
            $"resource={input.TextResourceFingerprint}",
            $"direction={input.ResolvedDirection}",
            $"constrain={(input.ConstrainToBox ? 1 : 0)}",
            $"dip={Format(input.PixelsPerDip)}",
            $"frame-w={Format(input.FrameWidthDip)}",
            $"frame-h={Format(input.FrameHeightDip)}",
            $"width={Format(input.WidthDip)}",
            $"height={Format(input.HeightDip)}",
            $"ink={Format(input.InkExtentDip)}",
            $"baseline={Format(input.BaselineDip)}",
            $"line-height={Format(input.LineHeightDip)}",
            $"lines={input.LineCount.ToString(CultureInfo.InvariantCulture)}",
            $"content-width={Format(input.ContentWidthDip)}",
            $"offset={Format(input.VerticalOffsetDip)}",
            $"effective-font-pt={Format(input.EffectiveFontSizePt)}",
            $"horizontal-scale={Format(input.HorizontalScale)}",
            $"vertical-scale={Format(input.VerticalScale)}",
            $"horizontal-anchor={Format(input.HorizontalScaleAnchorFraction)}",
            $"overflow={(input.IsOverflowing ? 1 : 0)}");
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    private static string Format(double value) =>
        value.ToString("R", CultureInfo.InvariantCulture);
}

public sealed record TextLayoutIdentityInput(
    string TextHash,
    string TextResourceFingerprint,
    TextDirectionMode ResolvedDirection,
    bool ConstrainToBox,
    double PixelsPerDip,
    double FrameWidthDip,
    double FrameHeightDip,
    double WidthDip,
    double HeightDip,
    double InkExtentDip,
    double BaselineDip,
    double LineHeightDip,
    int LineCount,
    double ContentWidthDip,
    double VerticalOffsetDip,
    bool IsOverflowing)
{
    public double EffectiveFontSizePt { get; init; } = double.NaN;
    public double HorizontalScale { get; init; } = 1.0;
    public double VerticalScale { get; init; } = 1.0;
    public double HorizontalScaleAnchorFraction { get; init; }
}
