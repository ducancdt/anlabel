using ANLAbel.Core.Enums;

namespace ANLAbel.Core.Barcode;

/// <summary>
/// Single source of truth for barcode HRI (human-readable interpretation) text layout.
/// Both the designer canvas and the print renderer must use these methods so that
/// WYSIWYG is guaranteed — no local textHeight or padding constants elsewhere.
///
/// Layout model: SHRINK.
/// The object's WidthMm × HeightMm is the total bounding box.
/// When ShowBarcodeText is true, the bars occupy the top portion and the text
/// occupies the bottom portion — both fit within the declared object height.
///
/// HRI text applies only to 1D barcodes (Code128, EAN, UPC, etc.).
/// 2D matrix codes (QR, DataMatrix, Aztec, PDF417) never receive HRI text.
/// </summary>
public static class BarcodeTextLayout
{
    /// <summary>Line-height multiplier applied to the em size to get the text row height.</summary>
    public const double TextHeightFactor = 1.8;

    /// <summary>Gap (DIP) between the bottom of the bars and the top of the text baseline.</summary>
    public const double TextBottomPaddingDip = 2.0;

    /// <summary>Convert font size in points to WPF device-independent pixels.</summary>
    public static double FontSizeDip(double fontSizePt) => fontSizePt * 96.0 / 72.0;

    /// <summary>
    /// Total vertical space (DIP) consumed by the HRI text row, including padding.
    /// This is the amount subtracted from the object height to obtain the bar area height.
    /// </summary>
    public static double TextRowHeightDip(double fontSizePt)
        => FontSizeDip(fontSizePt) * TextHeightFactor + TextBottomPaddingDip;

    /// <summary>
    /// Height (DIP) available for the barcode bars given the total object height.
    /// Returns <paramref name="totalHeightDip"/> unchanged when <paramref name="showText"/> is false.
    /// </summary>
    public static double BarcodeHeightDip(double totalHeightDip, double fontSizePt, bool showText)
    {
        if (!showText)
            return totalHeightDip;
        return Math.Max(1, totalHeightDip - TextRowHeightDip(fontSizePt));
    }

    /// <summary>
    /// Y offset (DIP) from the top of the object to the text drawing origin,
    /// relative to <paramref name="barcodeHeightDip"/> (the bar area height).
    /// </summary>
    public static double TextOriginY(double barcodeHeightDip)
        => barcodeHeightDip + TextBottomPaddingDip;

    /// <summary>
    /// Returns true when <paramref name="type"/> / <paramref name="symbology"/> is a 1D barcode
    /// that should display HRI text below its bars.
    /// 2D matrix codes (QR, DataMatrix, Aztec, PDF417) always return false.
    /// </summary>
    public static bool Is1DBarcode(ObjectType type, BarcodeSymbology symbology)
    {
        if (type is ObjectType.QRCode or ObjectType.DataMatrix)
            return false;

        return symbology is not (BarcodeSymbology.QRCode
            or BarcodeSymbology.DataMatrix
            or BarcodeSymbology.Aztec
            or BarcodeSymbology.Pdf417);
    }
}
