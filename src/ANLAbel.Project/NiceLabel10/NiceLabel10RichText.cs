using System.Globalization;
using System.Text;

namespace ANLAbel.Project.NiceLabel10;

/// <summary>
/// Plain-text recovery for NiceLabel 10 rich-text items
/// (<c>RichTextFormatting = True</c>). The corpus stores their contents in
/// three forms: raw RTF, base64-encoded RTF, or base64-encoded plain
/// text. Character formatting is dropped; paragraph, line, tab and table
/// cell breaks are kept so the operator's text survives the import.
/// </summary>
internal static class NiceLabel10RichText
{
    private static readonly HashSet<string> SkippedDestinations = new(StringComparer.Ordinal)
    {
        "fonttbl", "colortbl", "stylesheet", "listtable", "listoverridetable", "info", "pict",
        "header", "footer", "headerl", "headerr", "footerl", "footerr", "themedata",
        "colorschememapping", "latentstyles", "datastore", "rsidtbl", "generator",
        "xmlnstbl", "mmathPr", "object", "fldinst", "revtbl", "pntext", "filetbl"
    };

    static NiceLabel10RichText()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    /// <summary>
    /// Recovers a stored plain string value. NiceLabel 10 never writes a raw
    /// line break into its XML: a multi-line value (fixed contents, variable
    /// default or sample) is stored as base64 UTF-8 text (486 such values in
    /// the 207-file corpus, no raw line break anywhere). A value is decoded
    /// only when it is well-formed base64 whose strict UTF-8 decoding is
    /// printable text containing a line break; anything else is returned as
    /// stored.
    /// </summary>
    public static string DecodeStoredText(string? stored)
    {
        var text = stored ?? string.Empty;
        var decoded = TryDecodeBase64(text.Trim());
        if (decoded is null
            || decoded.IndexOfAny(new[] { '\r', '\n' }) < 0
            || decoded.Any(ch => char.IsControl(ch) && ch is not ('\r' or '\n' or '\t')))
        {
            return text;
        }

        return decoded;
    }

    /// <summary>Recovers the plain text of a stored rich-text contents value.</summary>
    public static string ToPlainText(string? stored)
    {
        var text = (stored ?? string.Empty).Trim();
        if (text.Length == 0)
        {
            return string.Empty;
        }

        if (!IsRtf(text))
        {
            var decoded = TryDecodeBase64(text);
            if (decoded is null)
            {
                return text;
            }

            text = decoded.Trim();
            if (!IsRtf(text))
            {
                return text;
            }
        }

        return ExtractRtfText(text);
    }

    private static bool IsRtf(string text) => text.StartsWith("{\\rtf", StringComparison.Ordinal);

    private static string? TryDecodeBase64(string text)
    {
        var compact = text.Replace("\r", string.Empty).Replace("\n", string.Empty);
        if (compact.Length < 8 || compact.Length % 4 != 0 || compact.Any(ch => !IsBase64Char(ch)))
        {
            return null;
        }

        try
        {
            var bytes = Convert.FromBase64String(compact);
            return new UTF8Encoding(false, throwOnInvalidBytes: true).GetString(bytes);
        }
        catch (FormatException)
        {
            return null;
        }
        catch (DecoderFallbackException)
        {
            return null;
        }

        static bool IsBase64Char(char ch) =>
            ch is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or '+' or '/' or '=';
    }

    /// <summary>
    /// Minimal RTF reader (RTF 1.9 syntax): groups, control words/symbols,
    /// hex escapes in the document code page, <c>\uN</c> with the
    /// <c>\ucN</c> fallback skip, and the destinations that never carry
    /// document text.
    /// </summary>
    private static string ExtractRtfText(string rtf)
    {
        var output = new StringBuilder();
        var skipDepth = new Stack<bool>();
        var ucStack = new Stack<int>();
        var skipping = false;
        var unicodeSkip = 1;
        var pendingUnicodeSkip = 0;
        var encoding = Encoding.Latin1;
        var groupJustOpened = false;

        for (var i = 0; i < rtf.Length; i++)
        {
            var ch = rtf[i];
            switch (ch)
            {
                case '{':
                    skipDepth.Push(skipping);
                    ucStack.Push(unicodeSkip);
                    groupJustOpened = true;
                    continue;
                case '}':
                    if (skipDepth.Count > 0)
                    {
                        skipping = skipDepth.Pop();
                        unicodeSkip = ucStack.Pop();
                    }

                    groupJustOpened = false;
                    continue;
                case '\\':
                {
                    if (i + 1 >= rtf.Length)
                    {
                        break;
                    }

                    var next = rtf[i + 1];
                    if (next == '*' && groupJustOpened)
                    {
                        // {\*\destination ...}: ignorable destination.
                        skipping = true;
                        i++;
                        groupJustOpened = false;
                        continue;
                    }

                    if (next == '\'')
                    {
                        // \'hh hex escape in the document code page.
                        if (i + 3 < rtf.Length
                            && int.TryParse(rtf.AsSpan(i + 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var code))
                        {
                            if (!skipping)
                            {
                                if (pendingUnicodeSkip > 0)
                                {
                                    pendingUnicodeSkip--;
                                }
                                else
                                {
                                    output.Append(encoding.GetString(new[] { (byte)code }));
                                }
                            }

                            i += 3;
                        }
                        else
                        {
                            i++;
                        }

                        groupJustOpened = false;
                        continue;
                    }

                    if (!char.IsLetter(next))
                    {
                        // Control symbol.
                        if (!skipping)
                        {
                            switch (next)
                            {
                                case '\\':
                                case '{':
                                case '}':
                                    output.Append(next);
                                    break;
                                case '~':
                                    output.Append(' ');
                                    break;
                                case '_':
                                    output.Append('-');
                                    break;
                                case '\n':
                                case '\r':
                                    output.Append('\n');
                                    break;
                            }
                        }

                        i++;
                        groupJustOpened = false;
                        continue;
                    }

                    // Control word: letters, optional signed number, optional space.
                    var start = i + 1;
                    var end = start;
                    while (end < rtf.Length && char.IsLetter(rtf[end]))
                    {
                        end++;
                    }

                    var word = rtf[start..end];
                    var numberStart = end;
                    if (end < rtf.Length && (rtf[end] == '-' || char.IsDigit(rtf[end])))
                    {
                        end++;
                        while (end < rtf.Length && char.IsDigit(rtf[end]))
                        {
                            end++;
                        }
                    }

                    var number = 0;
                    var hasNumber = end > numberStart
                        && int.TryParse(rtf.AsSpan(numberStart, end - numberStart), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out number);

                    if (end < rtf.Length && rtf[end] == ' ')
                    {
                        end++;
                    }

                    i = end - 1;
                    var opensDestination = groupJustOpened;
                    groupJustOpened = false;

                    if (opensDestination && SkippedDestinations.Contains(word))
                    {
                        skipping = true;
                        continue;
                    }

                    switch (word)
                    {
                        case "ansicpg" when hasNumber:
                            encoding = ResolveEncoding(number, encoding);
                            break;
                        case "uc" when hasNumber:
                            unicodeSkip = Math.Max(0, number);
                            break;
                        case "u" when hasNumber:
                            if (!skipping)
                            {
                                var codePoint = number < 0 ? number + 65536 : number;
                                output.Append((char)codePoint);
                                pendingUnicodeSkip = unicodeSkip;
                            }

                            break;
                        case "par":
                        case "line":
                        case "row":
                            AppendIfVisible('\n');
                            break;
                        case "tab":
                        case "cell":
                            AppendIfVisible('\t');
                            break;
                        case "emdash":
                            AppendIfVisible('—');
                            break;
                        case "endash":
                            AppendIfVisible('–');
                            break;
                        case "lquote":
                        case "rquote":
                            AppendIfVisible('\'');
                            break;
                        case "ldblquote":
                        case "rdblquote":
                            AppendIfVisible('"');
                            break;
                        case "bullet":
                            AppendIfVisible('•');
                            break;
                    }

                    continue;
                }
                case '\r':
                case '\n':
                    groupJustOpened = false;
                    continue;
            }

            groupJustOpened = false;
            if (skipping)
            {
                continue;
            }

            if (pendingUnicodeSkip > 0)
            {
                pendingUnicodeSkip--;
                continue;
            }

            output.Append(ch);
        }

        return output.ToString().Replace("\r\n", "\n").Trim();

        void AppendIfVisible(char value)
        {
            if (!skipping)
            {
                output.Append(value);
            }
        }
    }

    private static Encoding ResolveEncoding(int codePage, Encoding fallback)
    {
        try
        {
            return Encoding.GetEncoding(codePage);
        }
        catch (ArgumentException)
        {
            return fallback;
        }
        catch (NotSupportedException)
        {
            return fallback;
        }
    }
}
