namespace ANLAbel.Core.Enums;

/// <summary>
/// How a linear barcode object's horizontal size is determined.
/// </summary>
public enum BarcodeWidthMode
{
    /// <summary>
    /// Default / legacy: the operator owns <c>WidthMm</c>; modules stretch to fill the frame.
    /// </summary>
    FrameOwned = 0,

    /// <summary>
    /// Production width = quantized effective X (mm) × pure logical module count at plan DPI.
    /// Requires a positive authored <c>BarcodeModuleWidthMm</c>.
    /// </summary>
    SizedFromX = 1
}
