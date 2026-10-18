using ANLAbel.Core.Enums;

namespace ANLAbel.Core.Barcode;

/// <summary>
/// GS1 point-of-sale EAN/UPC magnification: X from 80 % to 200 % of the
/// 0.330 mm nominal module. Outside it the symbol is out of specification for
/// retail scanning; it is reported as advice because in-house labels often
/// shrink EAN codes on purpose and many still scan.
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
}
