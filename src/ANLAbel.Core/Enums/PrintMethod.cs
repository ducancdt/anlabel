namespace ANLAbel.Core.Enums;

/// <summary>
/// Controls whether barcode elements are rendered via the application-owned
/// exact vector/raster graphics pipeline (default, ensuring 100% parity with the designer)
/// or via printer-native command language (e.g. ZPL/EPL) where supported.
/// </summary>
public enum PrintMethod
{
    /// <summary>
    /// Application-owned exact graphic rendering (parity with designer, preview, and exported images).
    /// </summary>
    ApplicationGraphic = 0,

    /// <summary>
    /// Printer-native command stream (thermal vendor commands). Fails closed in preflight
    /// if the target printer does not support native direct commands.
    /// </summary>
    PrinterNative = 1
}
