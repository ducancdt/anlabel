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
    /// Accept payloads with or without a trailing check character. When a
    /// trailing character validates as the check digit it is treated as such
    /// for HRI formatting; otherwise the whole string is data.
    /// </summary>
    Auto = 1,

    /// <summary>
    /// Fail closed unless the payload ends with a correct check digit for the
    /// symbology. Preflight blocks dispatch when verification fails.
    /// </summary>
    Verify = 2
}
