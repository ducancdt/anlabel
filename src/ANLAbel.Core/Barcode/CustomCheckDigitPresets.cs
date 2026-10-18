using ANLAbel.Core.Models;

namespace ANLAbel.Core.Barcode;

/// <summary>
/// Published check digit schemes the operator can load into an object's
/// custom rule instead of typing weights by hand. Loading a preset only fills
/// the fields; the policy (Verify / Generate) stays the operator's choice.
/// </summary>
public static class CustomCheckDigitPresets
{
    public const string None = "";
    public const string Gs1Mod10 = "GS1 mod 10 (3,1)";
    public const string Luhn = "Luhn mod 10";
    public const string Mod11X = "Mod 11, 10 = X (ISBN-10)";
    public const string UpuS10 = "UPU S10 mod 11 (bưu gửi)";

    public static IReadOnlyList<string> Names { get; } = [Gs1Mod10, Luhn, Mod11X, UpuS10];

    public static bool Apply(LabelObject item, string? preset)
    {
        ArgumentNullException.ThrowIfNull(item);
        (int Modulus, string Weights, bool FromRight, bool SumDigits, bool Complement, bool KeepAll, bool ZeroAsModulus, string Map)? values = preset switch
        {
            Gs1Mod10 => (10, "3,1", true, false, true, false, false, ""),
            Luhn => (10, "2,1", true, true, true, false, false, ""),
            Mod11X => (11, "2,3,4,5,6,7,8,9,10", true, false, true, false, false, "10=X"),
            UpuS10 => (11, "8,6,4,2,3,5,9,7", false, false, true, false, true, "10=0;11=5"),
            _ => null
        };
        if (values is not { } v)
        {
            return false;
        }

        item.CheckDigitModulus = v.Modulus;
        item.CheckDigitWeights = v.Weights;
        item.CheckDigitWeightsFromRight = v.FromRight;
        item.CheckDigitSumDigitsOfProducts = v.SumDigits;
        item.CheckDigitComplement = v.Complement;
        item.CheckDigitKeepAllDigits = v.KeepAll;
        item.CheckDigitZeroRemainderAsModulus = v.ZeroAsModulus;
        item.CheckDigitValueMap = v.Map;
        return true;
    }
}
