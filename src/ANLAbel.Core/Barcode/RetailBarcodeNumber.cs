using ANLAbel.Core.Enums;

namespace ANLAbel.Core.Barcode;

/// <summary>
/// The number an EAN-13, EAN-8, UPC-A or UPC-E symbol carries, as a scanner
/// reads it. Data may be given without its GS1 check digit, and a UPC-E as its
/// six digits alone (number system 0, as GS1 and NiceLabel 10 take it). The
/// encoder and the human-readable line both use this number, so the digits
/// printed under the bars are the digits the scanner returns.
/// </summary>
public static class RetailBarcodeNumber
{
    public static bool Applies(BarcodeSymbology symbology)
        => symbology is BarcodeSymbology.Ean13 or BarcodeSymbology.Ean8 or BarcodeSymbology.UpcA or BarcodeSymbology.UpcE;

    /// <summary>
    /// The full number with number system and check digit; data that is not a
    /// short or full retail form comes back unchanged, for validation to report.
    /// </summary>
    public static string Complete(BarcodeSymbology symbology, string data)
    {
        if (!Applies(symbology) || string.IsNullOrEmpty(data) || !data.All(char.IsAsciiDigit))
        {
            return data;
        }

        return (symbology, data.Length) switch
        {
            (BarcodeSymbology.Ean13, 12) or (BarcodeSymbology.Ean8, 7) or (BarcodeSymbology.UpcA, 11) => data + CheckDigit(data),
            (BarcodeSymbology.UpcE, 6) => Complete(symbology, "0" + data),
            (BarcodeSymbology.UpcE, 7) when IsUpcENumberSystem(data[0]) => data + CheckDigit(ExpandUpcE(data)),
            _ => data
        };
    }

    /// <summary>What the encoder is given: a six-digit UPC-E gains its number system 0.</summary>
    public static string ToEncoderInput(BarcodeSymbology symbology, string data)
        => symbology == BarcodeSymbology.UpcE && data.Length == 6 && data.All(char.IsAsciiDigit)
            ? "0" + data
            : data;

    /// <summary>UPC-E carries number system 0 or 1 only.</summary>
    public static bool IsUpcENumberSystem(char digit) => digit is '0' or '1';

    /// <summary>
    /// True when an eight-digit UPC-E ends with the check digit of its UPC-A
    /// expansion (the number system must be 0 or 1).
    /// </summary>
    public static bool HasValidUpcECheckDigit(string eightDigits)
        => eightDigits.Length == 8
            && eightDigits.All(char.IsAsciiDigit)
            && IsUpcENumberSystem(eightDigits[0])
            && CheckDigit(ExpandUpcE(eightDigits[..7])) == eightDigits[7];

    /// <summary>GS1 mod-10 check digit (weights 3 and 1 from the right), the rule ITF also uses.</summary>
    private static char CheckDigit(string body) => BarcodeCheckDigitContract.ComputeItfCheckDigit(body);

    /// <summary>
    /// The UPC-A body (11 digits, no check digit) a seven-digit UPC-E stands for:
    /// its last digit says where the zeros of the manufacturer and item numbers go.
    /// </summary>
    private static string ExpandUpcE(string seven)
    {
        var system = seven[0];
        var d = seven[1..];
        return d[5] switch
        {
            '0' or '1' or '2' => $"{system}{d[0]}{d[1]}{d[5]}0000{d[2]}{d[3]}{d[4]}",
            '3' => $"{system}{d[0]}{d[1]}{d[2]}00000{d[3]}{d[4]}",
            '4' => $"{system}{d[0]}{d[1]}{d[2]}{d[3]}00000{d[4]}",
            _ => $"{system}{d[0]}{d[1]}{d[2]}{d[3]}{d[4]}0000{d[5]}"
        };
    }
}
