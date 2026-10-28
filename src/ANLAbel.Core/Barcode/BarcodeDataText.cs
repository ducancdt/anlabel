using System.Globalization;

namespace ANLAbel.Core.Barcode;

/// <summary>
/// Barcode payload text rules shared by designer, preview, preflight and print.
/// Excel cells pasted from the web or an ERP often carry characters the operator
/// cannot see (no-break space, zero-width space, byte order mark) at either end
/// of the value. No linear symbology encodes them, so the row blocked with a
/// message that named no character. They are removed from the edges of every
/// barcode payload; ASCII whitespace and all interior characters are kept and
/// an unsupported one is named with its position.
/// </summary>
public static class BarcodeDataText
{
    /// <summary>
    /// True for a character that is invisible in Excel and on the label and that
    /// no symbology is expected to carry: non-ASCII whitespace (U+00A0 no-break
    /// space, U+3000 ideographic space, U+0085 next line, ...) and the zero-width
    /// formatters U+200B–U+200D, U+2060 and U+FEFF.
    /// </summary>
    public static bool IsInvisible(char character)
        => (character > '\u007F' && char.IsWhiteSpace(character))
           || character is '​' or '‌' or '‍' or '⁠' or '﻿';

    /// <summary>Removes invisible characters from both ends; ASCII whitespace stays.</summary>
    public static string TrimInvisible(string? data)
    {
        if (string.IsNullOrEmpty(data))
        {
            return string.Empty;
        }

        var start = 0;
        var end = data.Length;
        while (start < end && IsInvisible(data[start]))
        {
            start++;
        }

        while (end > start && IsInvisible(data[end - 1]))
        {
            end--;
        }

        return start == 0 && end == data.Length ? data : data[start..end];
    }

    /// <summary>True when the payload has an invisible character at either end.</summary>
    public static bool HasInvisibleEdge(string? data)
        => !string.IsNullOrEmpty(data) && (IsInvisible(data[0]) || IsInvisible(data[^1]));

    /// <summary>
    /// Describes the first character that <paramref name="isSupported"/> rejects
    /// as <c>U+2013 '–' at position 6</c>, or with a name instead of the glyph
    /// when the character is invisible or a control (<c>U+00A0 (no-break space)
    /// at position 1</c>). Positions are 1-based. Null when all are supported.
    /// </summary>
    public static string? DescribeFirstUnsupported(string? data, Func<char, bool> isSupported)
    {
        ArgumentNullException.ThrowIfNull(isSupported);
        if (string.IsNullOrEmpty(data))
        {
            return null;
        }

        for (var index = 0; index < data.Length; index++)
        {
            var character = data[index];
            if (isSupported(character))
            {
                continue;
            }

            return $"{Describe(character)} at position {(index + 1).ToString(CultureInfo.InvariantCulture)}";
        }

        return null;
    }

    /// <summary>
    /// <c>U+XXXX 'c'</c> for a visible character, <c>U+XXXX (name)</c> for one
    /// that would print as nothing.
    /// </summary>
    public static string Describe(char character)
    {
        var code = $"U+{(int)character:X4}";
        var name = InvisibleName(character);
        return name is null ? $"{code} '{character}'" : $"{code} ({name})";
    }

    private static string? InvisibleName(char character) => character switch
    {
        ' ' => "no-break space",
        ' ' => "narrow no-break space",
        ' ' => "figure space",
        '　' => "ideographic space",
        '​' => "zero-width space",
        '‌' => "zero-width non-joiner",
        '‍' => "zero-width joiner",
        '⁠' => "word joiner",
        '﻿' => "byte order mark",
        '\t' => "tab",
        '\n' => "line feed",
        '\r' => "carriage return",
        _ when char.IsWhiteSpace(character) && character > '\u007F' => "space",
        _ when char.IsControl(character) => "control character",
        _ => null,
    };
}
