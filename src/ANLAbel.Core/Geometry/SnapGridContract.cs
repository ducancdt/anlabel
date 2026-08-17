namespace ANLAbel.Core.Geometry;

/// <summary>
/// Canonical grid policy for designer interactions.  Display lines and pointer
/// snapping use the same physical millimetre step; the screen zoom only changes
/// how that step is projected, never the committed document coordinate.
/// </summary>
public static class SnapGridContract
{
    public const double DefaultStepMm = 1.0;
    public const double MinimumStepMm = 0.25;
    public const double MaximumStepMm = 20.0;

    public static double NormalizeStep(double stepMm)
    {
        return !double.IsFinite(stepMm) || stepMm <= 0
            ? DefaultStepMm
            : Math.Clamp(stepMm, MinimumStepMm, MaximumStepMm);
    }

    public static double Snap(double positionMm, double stepMm)
    {
        if (!double.IsFinite(positionMm))
        {
            return 0;
        }

        var step = NormalizeStep(stepMm);
        return Math.Round(positionMm / step, MidpointRounding.AwayFromZero) * step;
    }

    public static bool TrySnap(double positionMm, double stepMm, double toleranceMm, out double targetMm)
    {
        targetMm = Snap(positionMm, stepMm);
        return double.IsFinite(toleranceMm)
            && toleranceMm >= 0
            && Math.Abs(targetMm - positionMm) <= toleranceMm;
    }
}
