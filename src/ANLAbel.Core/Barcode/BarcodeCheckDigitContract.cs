using ANLAbel.Core.Enums;

namespace ANLAbel.Core.Barcode;

/// <summary>
/// Pure check-digit policy and HRI display helpers. Encoding of the symbol always
/// uses the full authored payload; HRI-only hide never mutates module geometry.
/// </summary>
public static class BarcodeCheckDigitContract
{
    private const string Code39Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ-. $/+%";

    public static bool SupportsOptionalCheckDigit(BarcodeSymbology symbology)
        => symbology is BarcodeSymbology.Code39 or BarcodeSymbology.ITF;

    /// <summary>
    /// Symbologies that take a check digit under the given rule: the standard
    /// Code 39 / ITF rules, or a custom numeric rule on Code 39, ITF or Code 128.
    /// </summary>
    public static bool SupportsCheckDigit(BarcodeSymbology symbology, CustomCheckDigitAlgorithm? custom)
        => custom is null
            ? SupportsOptionalCheckDigit(symbology)
            : symbology is BarcodeSymbology.Code39 or BarcodeSymbology.ITF or BarcodeSymbology.Code128;

    /// <summary>
    /// Returns preflight messages when <paramref name="policy"/> is Verify and
    /// the payload fails the check-digit rule, or Generate and no check digit
    /// can be generated for the data.
    /// </summary>
    public static IReadOnlyList<string> Validate(
        BarcodeSymbology symbology,
        string payload,
        BarcodeCheckDigitPolicy policy,
        CustomCheckDigitAlgorithm? custom = null)
    {
        if (!SupportsCheckDigit(symbology, custom))
        {
            return Array.Empty<string>();
        }

        if (policy == BarcodeCheckDigitPolicy.Generate)
        {
            return ValidateGenerateBody(symbology, payload ?? string.Empty, custom);
        }

        if (policy != BarcodeCheckDigitPolicy.Verify)
        {
            return Array.Empty<string>();
        }

        var data = payload ?? string.Empty;
        if (string.IsNullOrWhiteSpace(data))
        {
            return new[] { $"{symbology} check-digit policy is Verify but the barcode data is empty." };
        }

        if (HasValidTrailingCheckDigit(symbology, data, custom))
        {
            return Array.Empty<string>();
        }

        return custom is null
            ? new[] { $"{symbology} check-digit policy is Verify: payload does not end with a valid check digit. Correct the data or set policy to Auto/None." }
            : new[] { $"{symbology} check-digit policy is Verify: payload does not end with a valid custom check digit. Correct the data, the custom rule, or set policy to Auto/None." };
    }

    /// <summary>
    /// Check digit problems for a label object, including an unusable custom
    /// definition when the policy relies on it.
    /// </summary>
    public static IReadOnlyList<string> Validate(Models.LabelObject item, BarcodeSymbology symbology, string payload)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (item.BarcodeCheckDigitPolicy is BarcodeCheckDigitPolicy.Verify or BarcodeCheckDigitPolicy.Generate)
        {
            var definitionErrors = CustomCheckDigitAlgorithm.ValidateDefinition(item);
            if (definitionErrors.Count > 0)
            {
                return definitionErrors;
            }
        }

        return Validate(symbology, payload, item.BarcodeCheckDigitPolicy, CustomCheckDigitAlgorithm.FromObject(item));
    }

    /// <summary>
    /// The payload the symbol encodes: the authored data, plus the computed
    /// check digit under Generate. Data that cannot take a check digit is
    /// returned unchanged so encode validation and preflight report it.
    /// </summary>
    public static string ToEncodedPayload(
        BarcodeSymbology symbology,
        string payload,
        BarcodeCheckDigitPolicy policy,
        CustomCheckDigitAlgorithm? custom = null)
    {
        var data = payload ?? string.Empty;
        if (policy != BarcodeCheckDigitPolicy.Generate
            || !SupportsCheckDigit(symbology, custom)
            || ValidateGenerateBody(symbology, data, custom).Count > 0)
        {
            return data;
        }

        return data + ComputeCheck(symbology, data, custom);
    }

    /// <summary>
    /// Encoded payload for a label object: Generate applies only to linear
    /// barcode objects, never to QR or Data Matrix object types.
    /// </summary>
    public static string ToEncodedPayload(Models.LabelObject item, string payload)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (CustomCheckDigitAlgorithm.ValidateDefinition(item).Count > 0)
        {
            // Never fall back to the standard rule for a broken custom one; preflight blocks it.
            return payload ?? string.Empty;
        }

        return item.Type == ObjectType.BarcodeCode128
            ? ToEncodedPayload(
                item.BarcodeSymbology,
                payload,
                item.BarcodeCheckDigitPolicy,
                CustomCheckDigitAlgorithm.FromObject(item))
            : payload ?? string.Empty;
    }

    /// <summary>
    /// Human-readable string for HRI. When <paramref name="showCheckDigit"/> is
    /// false and the policy is Verify or Generate (so the trailing characters of
    /// the encoded payload are known to be the check digit), they are omitted
    /// from the display string only. Auto accepts payloads without a check
    /// digit, where a last data character can match by coincidence (1 in 10
    /// for ITF), so Auto never hides a character.
    /// </summary>
    public static string FormatHriText(
        BarcodeSymbology symbology,
        string payload,
        BarcodeCheckDigitPolicy policy,
        bool showCheckDigit,
        CustomCheckDigitAlgorithm? custom = null)
    {
        var data = payload ?? string.Empty;
        if (showCheckDigit || !SupportsCheckDigit(symbology, custom) || data.Length < 2)
        {
            return data;
        }

        // Only Verify and Generate guarantee the trailing characters are a check digit.
        if (policy is not (BarcodeCheckDigitPolicy.Verify or BarcodeCheckDigitPolicy.Generate))
        {
            return data;
        }

        return TryGetCheckLength(symbology, data, custom, out var checkLength)
            ? data[..^checkLength]
            : data;
    }

    /// <summary>
    /// HRI text for a label object: the check digit display rule, then the
    /// object's HRI mask when it fits the text exactly (otherwise the plain
    /// text is shown and preflight blocks the mask).
    /// </summary>
    public static string FormatHriText(Models.LabelObject item, BarcodeSymbology symbology, string payload)
    {
        ArgumentNullException.ThrowIfNull(item);
        var text = FormatHriText(
            symbology,
            payload,
            item.BarcodeCheckDigitPolicy,
            item.BarcodeHriShowCheckDigit,
            CustomCheckDigitAlgorithm.FromObject(item));
        if (symbology == BarcodeSymbology.Code128 && item.Code128CodeSet == Code128CodeSet.Manual)
        {
            // Code-set markers steer the encoder; they are not label text.
            text = StripCode128Markers(text);
        }

        return BarcodeHriMaskContract.TryApply(item.BarcodeHriMask, item.BarcodeHriMaskCharacter, text, out var masked)
            ? masked
            : text;
    }

    /// <summary>
    /// Text used to size the HRI strip: the raw data, or the displayed text
    /// when a mask adds formatting characters.
    /// </summary>
    public static string HriMeasureText(Models.LabelObject item, string payload)
    {
        ArgumentNullException.ThrowIfNull(item);
        return BarcodeHriMaskContract.IsUsed(item.BarcodeHriMask) || item.Code128CodeSet == Code128CodeSet.Manual
            ? FormatHriText(item, item.BarcodeSymbology, payload)
            : payload ?? string.Empty;
    }

    private static string StripCode128Markers(string text)
    {
        foreach (var marker in new[] { "<MODEA>", "<MODEB>", "<MODEC>" })
        {
            text = text.Replace(marker, string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        return text;
    }

    public static bool HasValidTrailingCheckDigit(
        BarcodeSymbology symbology,
        string payload,
        CustomCheckDigitAlgorithm? custom = null)
        => TryGetCheckLength(symbology, payload, custom, out _);

    private static bool TryGetCheckLength(
        BarcodeSymbology symbology,
        string payload,
        CustomCheckDigitAlgorithm? custom,
        out int checkLength)
    {
        checkLength = 0;
        if (string.IsNullOrEmpty(payload) || payload.Length < 2)
        {
            return false;
        }

        if (custom is not null)
        {
            // A value kept in full can be two characters; try the longer form first.
            for (var length = custom.KeepAllDigits ? 2 : 1; length >= 1; length--)
            {
                if (payload.Length <= length)
                {
                    continue;
                }

                var expected = custom.Compute(payload[..^length]);
                if (expected is not null
                    && expected.Length == length
                    && payload.EndsWith(expected, StringComparison.Ordinal))
                {
                    checkLength = length;
                    return true;
                }
            }

            return false;
        }

        var valid = symbology switch
        {
            BarcodeSymbology.Code39 => HasValidCode39CheckDigit(payload),
            BarcodeSymbology.ITF => HasValidItfCheckDigit(payload),
            _ => false
        };
        checkLength = valid ? 1 : 0;
        return valid;
    }

    private static string ComputeCheck(BarcodeSymbology symbology, string body, CustomCheckDigitAlgorithm? custom)
        => custom is not null
            ? custom.Compute(body) ?? string.Empty
            : symbology == BarcodeSymbology.Code39
                ? ComputeCode39CheckDigit(body).ToString()
                : ComputeItfCheckDigit(body).ToString();

    public static char ComputeCode39CheckDigit(string bodyWithoutCheck)
    {
        var sum = 0;
        foreach (var ch in bodyWithoutCheck.ToUpperInvariant())
        {
            var idx = Code39Alphabet.IndexOf(ch);
            if (idx < 0)
            {
                throw new ArgumentException($"Invalid Code 39 character '{ch}' for check-digit calculation.");
            }

            sum += idx;
        }

        return Code39Alphabet[sum % 43];
    }

    public static char ComputeItfCheckDigit(string bodyWithoutCheck)
    {
        if (string.IsNullOrEmpty(bodyWithoutCheck) || !bodyWithoutCheck.All(char.IsDigit))
        {
            throw new ArgumentException("ITF check digit requires a non-empty digit body.");
        }

        // Standard Interleaved 2 of 5 optional check: weighted sum from the right.
        var sum = 0;
        var weight = 3;
        for (var i = bodyWithoutCheck.Length - 1; i >= 0; i--)
        {
            sum += (bodyWithoutCheck[i] - '0') * weight;
            weight = weight == 3 ? 1 : 3;
        }

        var mod = sum % 10;
        var check = mod == 0 ? 0 : 10 - mod;
        return (char)('0' + check);
    }

    private static IReadOnlyList<string> ValidateGenerateBody(
        BarcodeSymbology symbology,
        string body,
        CustomCheckDigitAlgorithm? custom)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return new[] { $"{symbology} check-digit policy is Generate but the barcode data is empty." };
        }

        if (custom is not null && !body.All(char.IsAsciiDigit))
        {
            return new[] { "A custom check digit can only be generated for data made of digits." };
        }

        if (custom is null && symbology == BarcodeSymbology.Code39)
        {
            return body.ToUpperInvariant().All(ch => Code39Alphabet.Contains(ch))
                ? Array.Empty<string>()
                : new[] { "Code 39 check digit cannot be generated: the data contains characters outside the Code 39 set." };
        }

        if (symbology != BarcodeSymbology.ITF)
        {
            return Array.Empty<string>();
        }

        if (!body.All(char.IsAsciiDigit))
        {
            return new[] { "ITF check digit cannot be generated: the data must contain digits only." };
        }

        var checkLength = ComputeCheck(symbology, body, custom).Length;
        return (body.Length + checkLength) % 2 == 0
            ? Array.Empty<string>()
            : new[] { $"ITF needs an even number of digits including the check digit; {body.Length} data digits plus {checkLength} check digit(s) is odd. Change the data length by one digit." };
    }

    private static bool HasValidCode39CheckDigit(string payload)
    {
        var upper = payload.ToUpperInvariant();
        try
        {
            var expected = ComputeCode39CheckDigit(upper[..^1]);
            return upper[^1] == expected;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static bool HasValidItfCheckDigit(string payload)
    {
        if (!payload.All(char.IsDigit) || payload.Length < 2)
        {
            return false;
        }

        try
        {
            var expected = ComputeItfCheckDigit(payload[..^1]);
            return payload[^1] == expected;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
