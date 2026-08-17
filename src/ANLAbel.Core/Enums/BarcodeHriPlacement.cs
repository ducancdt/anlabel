namespace ANLAbel.Core.Enums;

/// <summary>
/// Vertical placement of human-readable interpretation (HRI) relative to a
/// linear barcode symbol. Shared by designer, preview, print and preflight.
/// </summary>
public enum BarcodeHriPlacement
{
    /// <summary>No HRI; the full object frame is available to bars.</summary>
    None = 0,

    /// <summary>HRI strip below the symbol (legacy default when text is shown).</summary>
    Below = 1,

    /// <summary>HRI strip above the symbol.</summary>
    Above = 2
}
