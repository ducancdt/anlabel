using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using ANLAbel.Core.Enums;
using ANLAbel.Core.Models;

namespace ANLAbel.Core.Text;

/// <summary>
/// Canonical identity for the text resources and presentation policy that a
/// renderer must use. It deliberately does not claim that a font is installed
/// or that a platform has produced identical glyph metrics; those are runtime
/// observations. The fingerprint makes a requested family/style/fallback
/// policy explicit so preview, print and recovery can prove they used the same
/// text resource contract.
/// </summary>
public static class TextResourceContract
{
    public const string ContractVersion = "text-resource-v1";
    public const string BaselineFallbackFamily = "Arial";

    public static TextResourceDescriptor Describe(ObjectStyle style)
    {
        ArgumentNullException.ThrowIfNull(style);
        return Describe(
            style.FontFamily,
            style.FontSizePt,
            style.Bold,
            style.Italic,
            style.Underline,
            style.TextDirection,
            style.LineHeightPt);
    }

    public static TextResourceDescriptor Describe(
        string? requestedFamily,
        double fontSizePt,
        bool bold,
        bool italic,
        bool underline,
        TextDirectionMode direction,
        double lineHeightPt)
    {
        var requested = string.IsNullOrWhiteSpace(requestedFamily)
            ? BaselineFallbackFamily
            : requestedFamily.Trim();
        var canonicalFamily = NormalizeFamily(requested);
        var canonicalFallback = NormalizeFamily(BaselineFallbackFamily);
        var canonical = string.Join(
            "|",
            ContractVersion,
            $"family={canonicalFamily}",
            $"size-pt={FormatDouble(fontSizePt)}",
            $"weight={(bold ? "700" : "400")}",
            $"italic={(italic ? "1" : "0")}",
            $"underline={(underline ? "1" : "0")}",
            $"direction={direction}",
            $"line-height-pt={FormatDouble(lineHeightPt)}",
            $"fallback={canonicalFallback}");
        var fingerprint = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));

        return new TextResourceDescriptor(
            requested,
            canonicalFamily,
            fontSizePt,
            bold,
            italic,
            underline,
            direction,
            lineHeightPt,
            BaselineFallbackFamily,
            ContractVersion,
            fingerprint);
    }

    public static string NormalizeFamily(string? family)
    {
        var value = string.IsNullOrWhiteSpace(family)
            ? BaselineFallbackFamily
            : family.Trim().Normalize(NormalizationForm.FormC);
        var parts = value.Split(
            new[] { ' ', '\t', '\r', '\n' },
            StringSplitOptions.RemoveEmptyEntries);
        return string.Join(' ', parts).ToUpperInvariant();
    }

    private static string FormatDouble(double value)
        => value.ToString("R", CultureInfo.InvariantCulture);
}

public sealed record TextResourceDescriptor(
    string RequestedFamily,
    string CanonicalFamily,
    double FontSizePt,
    bool Bold,
    bool Italic,
    bool Underline,
    TextDirectionMode Direction,
    double LineHeightPt,
    string BaselineFallbackFamily,
    string ContractVersion,
    string Fingerprint);
