namespace ANLAbel.Core.Enums;

/// <summary>
/// Optional check-digit policy for symbologies that allow a trailing check
/// character (Code 39 mod-43, ITF mod-10). Code 128 remains engine-managed and
/// ignores this policy.
/// </summary>
public enum BarcodeCheckDigitPolicy
{
    /// <summary>No check-digit requirement; payload is encoded as authored.</summary>
    None = 0,

    /// <summary>
    /// Accept payloads with or without a trailing check character. The whole
    /// string is shown in HRI: a data character that happens to validate as a
    /// check digit must not be hidden.
    /// </summary>
    Auto = 1,

    /// <summary>
    /// Fail closed unless the payload ends with a correct check digit for the
    /// symbology. Preflight blocks dispatch when verification fails.
    /// </summary>
    Verify = 2,

    /// <summary>
    /// The authored data is the body; the symbology check digit is computed
    /// and appended to the encoded payload (and to HRI unless hidden).
    /// </summary>
    Generate = 3
}
