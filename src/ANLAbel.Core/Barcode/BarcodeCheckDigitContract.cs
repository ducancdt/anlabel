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
    /// Returns preflight messages when <paramref name="policy"/> is Verify and
    /// the payload fails the symbology check-digit rule.
    /// </summary>
    public static IReadOnlyList<string> Validate(
        BarcodeSymbology symbology,
        string payload,
        BarcodeCheckDigitPolicy policy)
    {
        if (policy != BarcodeCheckDigitPolicy.Verify || !SupportsOptionalCheckDigit(symbology))
        {
            return Array.Empty<string>();
        }

        var data = payload ?? string.Empty;
        if (string.IsNullOrWhiteSpace(data))
        {
            return new[] { $"{symbology} check-digit policy is Verify but the barcode data is empty." };
        }

        return HasValidTrailingCheckDigit(symbology, data)
            ? Array.Empty<string>()
            : new[]
            {
                $"{symbology} check-digit policy is Verify: payload does not end with a valid check digit. Correct the data or set policy to Auto/None."
            };
    }

    /// <summary>
    /// Human-readable string for HRI. When <paramref name="showCheckDigit"/> is
    /// false and the trailing character validates as a check digit under the
    /// active policy, it is omitted from the display string only.
    /// </summary>
    public static string FormatHriText(
        BarcodeSymbology symbology,
        string payload,
        BarcodeCheckDigitPolicy policy,
        bool showCheckDigit)
    {
        var data = payload ?? string.Empty;
        if (showCheckDigit || !SupportsOptionalCheckDigit(symbology) || data.Length < 2)
        {
            return data;
        }

        // Only strip when Auto/Verify can treat the last char as check digit.
        if (policy == BarcodeCheckDigitPolicy.None)
        {
            return data;
        }

        return HasValidTrailingCheckDigit(symbology, data)
            ? data[..^1]
            : data;
    }

    public static bool HasValidTrailingCheckDigit(BarcodeSymbology symbology, string payload)
    {
        if (string.IsNullOrEmpty(payload) || payload.Length < 2)
        {
            return false;
        }

        return symbology switch
        {
            BarcodeSymbology.Code39 => HasValidCode39CheckDigit(payload),
            BarcodeSymbology.ITF => HasValidItfCheckDigit(payload),
            _ => false
        };
    }

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
