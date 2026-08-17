namespace ANLAbel.Core.Enums;

/// <summary>
/// Bearer (guard) bar style for packaging and shipping container linear barcodes (e.g. ITF-14).
/// Prevents short scans and equalizes print plate impression.
/// </summary>
public enum BearerBarStyle
{
    /// <summary>
    /// No bearer bars (default).
    /// </summary>
    None = 0,

    /// <summary>
    /// Top and bottom horizontal guard bars (standard for label printing onto shipping containers).
    /// </summary>
    TopBottom = 1,

    /// <summary>
    /// Complete rectangular frame enclosing the barcode and quiet zones (standard for direct corrugated printing).
    /// </summary>
    Frame = 2
}
