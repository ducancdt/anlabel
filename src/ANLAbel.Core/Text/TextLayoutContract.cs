using System.Collections.Immutable;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using ANLAbel.Core.Enums;

namespace ANLAbel.Core.Text;

/// <summary>
/// Platform-neutral text policy shared by the WPF presenter and future
/// renderers. It deliberately owns only decisions that do not require a font
/// engine: grapheme boundaries, newline normalization, paragraph direction,
/// line-height policy and vertical alignment. A caller supplies glyph widths
/// when it needs physical wrapping.
/// </summary>
public static class TextLayoutContract
{
    public const double DipPerPoint = 96.0 / 72.0;

    public static TextLayoutTextSnapshot Capture(string? value, TextDirectionMode mode = TextDirectionMode.Auto)
    {
        var normalized = NormalizeLineEndings(value);
        var clusters = SegmentGraphemes(normalized);
        var resolvedDirection = ResolveDirection(mode, normalized);
        return new TextLayoutTextSnapshot(
            normalized,
            resolvedDirection,
            clusters,
            ComputeContentHash(normalized, resolvedDirection, clusters));
    }

    public static string NormalizeLineEndings(string? value)
    {
        return (value ?? string.Empty)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');
    }

    public static ImmutableArray<string> SegmentGraphemes(string? value)
    {
        var normalized = value ?? string.Empty;
        if (normalized.Length == 0)
        {
            return ImmutableArray<string>.Empty;
        }

        var builder = ImmutableArray.CreateBuilder<string>();
        var enumerator = StringInfo.GetTextElementEnumerator(normalized);
        while (enumerator.MoveNext())
        {
            builder.Add(enumerator.GetTextElement());
        }

        return builder.ToImmutable();
    }

    /// <summary>
    /// Wraps by grapheme cluster. The measurement callback is called only with
    /// complete strings, so a font engine can shape the candidate without the
    /// policy ever splitting a surrogate pair, combining sequence or emoji ZWJ
    /// cluster.
    /// </summary>
    public static string WrapGraphemes(
        string? value,
        double width,
        Func<string, double> measureLine)
    {
        ArgumentNullException.ThrowIfNull(measureLine);
        var snapshot = Capture(value);
        if (snapshot.Text.Length == 0)
        {
            return snapshot.Text;
        }

        width = Math.Max(1, width);
        var output = new StringBuilder(snapshot.Text.Length + 16);
        var line = new StringBuilder();
        foreach (var element in snapshot.GraphemeClusters)
        {
            if (element == "\n")
            {
                AppendLine(output, line);
                continue;
            }

            var lineLengthBefore = line.Length;
            line.Append(element);
            if (measureLine(line.ToString()) <= width)
            {
                continue;
            }

            line.Length = lineLengthBefore;
            if (line.Length > 0)
            {
                TrimTrailingSpaces(line);
                AppendLine(output, line);
            }

            if (!IsWhitespaceCluster(element))
            {
                line.Append(element);
            }
        }

        output.Append(line);
        return output.ToString();
    }

    public static TextDirectionMode ResolveDirection(TextDirectionMode mode, string? value)
    {
        if (mode is TextDirectionMode.LeftToRight or TextDirectionMode.RightToLeft)
        {
            return mode;
        }

        foreach (var rune in (value ?? string.Empty).EnumerateRunes())
        {
            if (!IsLetter(rune))
            {
                continue;
            }

            return IsRtlLetter(rune.Value)
                ? TextDirectionMode.RightToLeft
                : TextDirectionMode.LeftToRight;
        }

        // Numbers, punctuation and symbols are weak/neutral bidi classes;
        // preserve the historical LTR default when no strong letter exists.
        return TextDirectionMode.LeftToRight;
    }

    public static double ResolveLineHeightDip(double naturalLineHeightDip, double requestedLineHeightPt)
    {
        var natural = Math.Max(1, naturalLineHeightDip);
        var requested = requestedLineHeightPt > 0
            ? requestedLineHeightPt * DipPerPoint
            : 0;
        return Math.Max(natural, requested);
    }

    public static double ResolveVerticalOffset(
        TextVerticalAlignmentMode? alignment,
        double textHeightDip,
        double frameHeightDip,
        bool constrainToBox)
    {
        var resolved = alignment ?? (constrainToBox ? TextVerticalAlignmentMode.Top : TextVerticalAlignmentMode.Center);
        var remaining = Math.Max(0, frameHeightDip - textHeightDip);
        return resolved switch
        {
            TextVerticalAlignmentMode.Bottom => remaining,
            TextVerticalAlignmentMode.Center => remaining / 2,
            _ => 0
        };
    }

    private static string ComputeContentHash(
        string text,
        TextDirectionMode direction,
        ImmutableArray<string> clusters)
    {
        var canonical = new StringBuilder();
        Append(canonical, direction.ToString());
        Append(canonical, text);
        foreach (var cluster in clusters)
        {
            Append(canonical, cluster);
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString())));
    }

    private static bool IsWhitespaceCluster(string cluster)
    {
        return cluster.All(char.IsWhiteSpace);
    }

    private static bool IsLetter(Rune rune)
    {
        return Rune.GetUnicodeCategory(rune) is
            UnicodeCategory.UppercaseLetter or
            UnicodeCategory.LowercaseLetter or
            UnicodeCategory.TitlecaseLetter or
            UnicodeCategory.ModifierLetter or
            UnicodeCategory.OtherLetter;
    }

    private static bool IsRtlLetter(int codePoint)
    {
        return codePoint is
            >= 0x0590 and <= 0x08FF or
            >= 0xFB1D and <= 0xFDFF or
            >= 0xFE70 and <= 0xFEFF or
            >= 0x10800 and <= 0x10FFF or
            >= 0x1E800 and <= 0x1EEFF;
    }

    private static void AppendLine(StringBuilder output, StringBuilder line)
    {
        output.Append(line);
        // Keep the contract platform-neutral. WPF accepts LF and callers do
        // not inherit Environment.NewLine differences between Windows and
        // non-Windows render/test hosts.
        output.Append('\n');
        line.Clear();
    }

    private static void TrimTrailingSpaces(StringBuilder value)
    {
        while (value.Length > 0 && char.IsWhiteSpace(value[^1]))
        {
            value.Length--;
        }
    }

    private static void Append(StringBuilder builder, string value)
    {
        builder.Append(value.Length.ToString(CultureInfo.InvariantCulture))
            .Append(':')
            .Append(value);
    }
}

public sealed record TextLayoutTextSnapshot(
    string Text,
    TextDirectionMode ResolvedDirection,
    ImmutableArray<string> GraphemeClusters,
    string ContentHash);
