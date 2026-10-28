using ANLAbel.Core.Enums;

namespace ANLAbel.Core.Barcode;

/// <summary>
/// GS1 point-of-sale EAN/UPC magnification: X from 80 % to 200 % of the
/// 0.330 mm nominal module. Outside it the symbol is out of specification for
/// retail scanning; it is reported as advice because in-house labels often
/// shrink EAN codes on purpose and many still scan. The reference engine
/// (NiceLabel 5 barcode.def, confirmed by decompiling IsBarcodeInStandard)
/// checks both symbol width and height against per-symbology windows; the
/// height windows below are those same 80 %-200 % ranges in millimetres.
/// </summary>
public static class RetailMagnificationContract
{
    public const double NominalXmm = 0.330;
    public const double MinimumXmm = 0.264;
    public const double MaximumXmm = 0.660;

    public static bool Applies(BarcodeSymbology symbology)
        => symbology is BarcodeSymbology.Ean13 or BarcodeSymbology.Ean8 or BarcodeSymbology.UpcA or BarcodeSymbology.UpcE;

    /// <summary>Advice when the printed X is outside 80 - 200 %, or empty.</summary>
    public static string Check(BarcodeSymbology symbology, double effectiveXmm, int dpi)
    {
        if (!Applies(symbology) || !double.IsFinite(effectiveXmm) || effectiveXmm <= 0)
        {
            return string.Empty;
        }

        var magnification = effectiveXmm / NominalXmm * 100;
        if (effectiveXmm + 1e-9 >= MinimumXmm && effectiveXmm - 1e-9 <= MaximumXmm)
        {
            return string.Empty;
        }

        return $"{symbology} prints at {magnification:0}% magnification (X {effectiveXmm:0.###} mm at {dpi} DPI), outside the GS1 retail range of 80% to 200% (X {MinimumXmm:0.###} to {MaximumXmm:0.###} mm). It may still scan in house, but retail point-of-sale scanning is not assured. Set X between {MinimumXmm:0.###} and {MaximumXmm:0.###} mm.";
    }

    /// <summary>
    /// Height window (mm) from the reference barcode.def table, or false when
    /// the symbology has no retail height window.
    /// </summary>
    public static bool TryGetHeightRangeMm(BarcodeSymbology symbology, out double minimumMm, out double maximumMm)
    {
        (minimumMm, maximumMm) = symbology switch
        {
            BarcodeSymbology.Ean13 or BarcodeSymbology.UpcA => (19.60, 49.00),
            BarcodeSymbology.Ean8 => (15.90, 39.76),
            BarcodeSymbology.UpcE => (11.20, 28.00),
            _ => (0, 0)
        };
        return maximumMm > 0;
    }

    /// <summary>Advice when the authored object height is outside the GS1 retail height window, or empty.</summary>
    public static string CheckHeight(BarcodeSymbology symbology, double objectHeightMm)
    {
        if (!TryGetHeightRangeMm(symbology, out var minimumMm, out var maximumMm)
            || !double.IsFinite(objectHeightMm)
            || objectHeightMm <= 0)
        {
            return string.Empty;
        }

        if (objectHeightMm + 1e-9 >= minimumMm && objectHeightMm - 1e-9 <= maximumMm)
        {
            return string.Empty;
        }

        var direction = objectHeightMm < minimumMm ? "shorter" : "taller";
        return $"{symbology} object is {objectHeightMm:0.##} mm high, {direction} than the GS1 retail height range of {minimumMm:0.##} to {maximumMm:0.##} mm. It may still scan in house, but retail point-of-sale scanning is not assured. Set the object height between {minimumMm:0.##} and {maximumMm:0.##} mm for retail use.";
    }
}
