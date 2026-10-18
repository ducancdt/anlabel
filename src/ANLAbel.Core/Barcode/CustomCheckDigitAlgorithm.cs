using ANLAbel.Core.Models;

namespace ANLAbel.Core.Barcode;

/// <summary>
/// Operator-defined weighted-modulus check digit for numeric data, for
/// customer codes whose check digit is not the symbology's own rule.
/// The weights repeat cyclically starting at the rightmost data digit
/// (or the leftmost when <see cref="WeightsFromRight"/> is false).
/// </summary>
public sealed record CustomCheckDigitAlgorithm(
    int Modulus,
    IReadOnlyList<int> Weights,
    bool WeightsFromRight,
    bool SumDigitsOfProducts,
    bool Complement,
    bool KeepAllDigits,
    bool ZeroRemainderAsModulus = false,
    IReadOnlyDictionary<int, string>? ValueMap = null)
{
    public const int MinimumModulus = 2;
    public const int MaximumModulus = 99;

    /// <summary>
    /// The object's custom algorithm, or null when it uses the symbology's
    /// standard check digit (modulus 0) or the definition is invalid.
    /// </summary>
    public static CustomCheckDigitAlgorithm? FromObject(LabelObject item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (item.CheckDigitModulus is < MinimumModulus or > MaximumModulus)
        {
            return null;
        }

        return TryParseWeights(item.CheckDigitWeights, out var weights)
               && TryParseValueMap(item.CheckDigitValueMap, out var valueMap)
            ? new CustomCheckDigitAlgorithm(
                item.CheckDigitModulus,
                weights,
                item.CheckDigitWeightsFromRight,
                item.CheckDigitSumDigitsOfProducts,
                item.CheckDigitComplement,
                item.CheckDigitKeepAllDigits,
                item.CheckDigitZeroRemainderAsModulus,
                valueMap)
            : null;
    }

    /// <summary>
    /// Plain-language problems with an object's custom definition; empty when
    /// the object uses the standard rule or the definition is usable.
    /// </summary>
    public static IReadOnlyList<string> ValidateDefinition(LabelObject item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (item.CheckDigitModulus <= 0)
        {
            return Array.Empty<string>();
        }

        var errors = new List<string>();
        if (item.CheckDigitModulus is < MinimumModulus or > MaximumModulus)
        {
            errors.Add($"Custom check digit modulus must be {MinimumModulus} to {MaximumModulus}; it is {item.CheckDigitModulus}.");
        }

        if (!TryParseWeights(item.CheckDigitWeights, out _))
        {
            errors.Add($"Custom check digit weights \"{item.CheckDigitWeights}\" must be a comma-separated list of 1 to 32 whole numbers from 0 to 99, not all zero (for example \"3,1\").");
        }

        if (!TryParseValueMap(item.CheckDigitValueMap, out _))
        {
            errors.Add($"Custom check digit value map \"{item.CheckDigitValueMap}\" must look like 10=X;11=0: a value from 0 to 99, '=', then one or two letters or digits.");
        }

        return errors;
    }

    /// <summary>
    /// Parses "10=X;11=0": check values that print as other characters (mod 11
    /// schemes print 10 as X or 0). Empty means no mapping.
    /// </summary>
    public static bool TryParseValueMap(string? text, out IReadOnlyDictionary<int, string> map)
    {
        var parsed = new Dictionary<int, string>();
        map = parsed;
        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        foreach (var entry in text.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = entry.Split('=', 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2
                || !int.TryParse(parts[0], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var value)
                || value > 99
                || parts[1].Length is 0 or > 2
                || !parts[1].All(char.IsAsciiLetterOrDigit)
                || !parsed.TryAdd(value, parts[1].ToUpperInvariant()))
            {
                return false;
            }
        }

        return true;
    }

    public static bool TryParseWeights(string? text, out IReadOnlyList<int> weights)
    {
        weights = Array.Empty<int>();
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var parts = text.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length is 0 or > 32)
        {
            return false;
        }

        var parsed = new int[parts.Length];
        for (var i = 0; i < parts.Length; i++)
        {
            if (!int.TryParse(parts[i], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var weight)
                || weight > 99)
            {
                return false;
            }

            parsed[i] = weight;
        }

        if (parsed.All(weight => weight == 0))
        {
            return false;
        }

        weights = parsed;
        return true;
    }

    /// <summary>Check characters for <paramref name="body"/>, or null when the body is not all digits.</summary>
    public string? Compute(string body)
    {
        if (string.IsNullOrEmpty(body) || !body.All(char.IsAsciiDigit))
        {
            return null;
        }

        var sum = 0L;
        for (var i = 0; i < body.Length; i++)
        {
            var digitIndex = WeightsFromRight ? body.Length - 1 - i : i;
            var product = (body[digitIndex] - '0') * Weights[i % Weights.Count];
            sum += SumDigitsOfProducts ? SumOfDigits(product) : product;
        }

        var remainder = (int)(sum % Modulus);
        var value = !Complement
            ? remainder
            : remainder != 0 ? Modulus - remainder : ZeroRemainderAsModulus ? Modulus : 0;
        if (ValueMap is not null && ValueMap.TryGetValue(value, out var mapped))
        {
            return mapped;
        }

        return KeepAllDigits
            ? value.ToString(System.Globalization.CultureInfo.InvariantCulture)
            : ((char)('0' + (value % 10))).ToString();
    }

    private static int SumOfDigits(int value)
    {
        var sum = 0;
        for (var rest = value; rest > 0; rest /= 10)
        {
            sum += rest % 10;
        }

        return sum;
    }
}
