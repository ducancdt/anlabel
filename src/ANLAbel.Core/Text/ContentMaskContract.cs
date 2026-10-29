using System.Text;

namespace ANLAbel.Core.Text;

/// <summary>
/// NiceLabel 10 content mask (object Source &gt; Mask; vendor
/// <c>ContentsMask</c>/<c>MaskValue</c>, formatter in Utils.dll): each mask
/// character takes the next character of the value, every other character
/// is written as is, and each line of the value starts the mask again.
/// Writing stops at the first mask character with no value left; value
/// characters beyond the mask are kept so no data is lost.
/// </summary>
public static class ContentMaskContract
{
    public const string DefaultMaskCharacter = "*";

    public static string Apply(string? mask, string? maskCharacter, string? value)
    {
        if (string.IsNullOrEmpty(mask) || string.IsNullOrEmpty(value))
        {
            return value ?? string.Empty;
        }

        var placeholder = string.IsNullOrEmpty(maskCharacter) ? DefaultMaskCharacter : maskCharacter;
        var lines = TextLayoutContract.NormalizeLineEndings(value).Split('\n');
        return string.Join("\n", lines.Select(line => ApplyLine(mask, placeholder, line)));
    }

    /// <summary>The object's mask applied to its resolved value.</summary>
    public static string ApplyTo(ANLAbel.Core.Models.LabelObject item, string? value)
    {
        ArgumentNullException.ThrowIfNull(item);
        return Apply(item.ContentMask, item.ContentMaskCharacter, value);
    }

    private static string ApplyLine(string mask, string placeholder, string line)
    {
        if (line.Length == 0)
        {
            return line;
        }

        var result = new StringBuilder(mask.Length + line.Length);
        var next = 0;
        var position = 0;
        while (position < mask.Length)
        {
            if (string.CompareOrdinal(mask, position, placeholder, 0, placeholder.Length) == 0)
            {
                if (next >= line.Length)
                {
                    return result.ToString();
                }

                result.Append(line[next++]);
                position += placeholder.Length;
                continue;
            }

            result.Append(mask[position++]);
        }

        return result.Append(line, next, line.Length - next).ToString();
    }
}
