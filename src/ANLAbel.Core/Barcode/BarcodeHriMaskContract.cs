using System.Text;

namespace ANLAbel.Core.Barcode;

/// <summary>
/// Human-readable text mask: each mask character takes the next character of
/// the HRI text; every other mask character is printed as formatting. A mask
/// must have exactly one position per HRI character, so it can neither drop
/// nor invent data; otherwise the plain HRI text is shown and preflight blocks.
/// </summary>
public static class BarcodeHriMaskContract
{
    public const string DefaultMaskCharacter = "*";
    public const int MaximumMaskLength = 128;

    public static bool IsUsed(string? mask) => !string.IsNullOrEmpty(mask);

    public static int PositionCount(string mask, string? maskCharacter)
    {
        var slot = Slot(maskCharacter);
        return mask.Count(ch => ch == slot);
    }

    public static bool TryApply(string? mask, string? maskCharacter, string text, out string masked)
    {
        masked = text ?? string.Empty;
        if (!IsUsed(mask))
        {
            return true;
        }

        var slot = Slot(maskCharacter);
        if (mask!.Length > MaximumMaskLength || PositionCount(mask, maskCharacter) != masked.Length)
        {
            return false;
        }

        var builder = new StringBuilder(mask.Length);
        var next = 0;
        foreach (var ch in mask)
        {
            builder.Append(ch == slot ? masked[next++] : ch);
        }

        masked = builder.ToString();
        return true;
    }

    /// <summary>Plain-language problem with applying the mask to this HRI text, or empty.</summary>
    public static string Validate(string? mask, string? maskCharacter, string text)
    {
        if (!IsUsed(mask))
        {
            return string.Empty;
        }

        if (maskCharacter is { Length: > 1 })
        {
            return "HRI mask character must be a single character.";
        }

        if (mask!.Length > MaximumMaskLength)
        {
            return $"HRI mask is longer than {MaximumMaskLength} characters.";
        }

        var positions = PositionCount(mask, maskCharacter);
        var length = (text ?? string.Empty).Length;
        return positions == length
            ? string.Empty
            : $"HRI mask has {positions} position(s) for '{Slot(maskCharacter)}' but the text under the barcode has {length} character(s), so the mask would drop or invent characters. Make the mask match the data length or clear it.";
    }

    private static char Slot(string? maskCharacter)
        => string.IsNullOrEmpty(maskCharacter) ? DefaultMaskCharacter[0] : maskCharacter[0];
}
