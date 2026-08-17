namespace ANLAbel.Core.Printing;

/// <summary>
/// Compares a driver-reported PageMediaSize (WPF DIP) with the physical stock
/// dimensions authored in millimetres.  The comparison is deliberately a
/// value-only seam so driver-coercion policy can be tested without a printer.
/// </summary>
public static class MediaDimensionContract
{
    private const double DipPerMillimetre = 96.0 / 25.4;

    public static bool Matches(
        double expectedWidthMm,
        double expectedHeightMm,
        double effectiveWidthDip,
        double effectiveHeightDip,
        double toleranceDip = 1.0)
    {
        if (!double.IsFinite(expectedWidthMm)
            || !double.IsFinite(expectedHeightMm)
            || !double.IsFinite(effectiveWidthDip)
            || !double.IsFinite(effectiveHeightDip)
            || !double.IsFinite(toleranceDip)
            || expectedWidthMm <= 0
            || expectedHeightMm <= 0
            || effectiveWidthDip <= 0
            || effectiveHeightDip <= 0
            || toleranceDip < 0)
        {
            return false;
        }

        var expectedWidthDip = expectedWidthMm * DipPerMillimetre;
        var expectedHeightDip = expectedHeightMm * DipPerMillimetre;
        return Math.Abs(effectiveWidthDip - expectedWidthDip) <= toleranceDip
            && Math.Abs(effectiveHeightDip - expectedHeightDip) <= toleranceDip;
    }
}
