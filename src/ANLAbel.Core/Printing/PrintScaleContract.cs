namespace ANLAbel.Core.Printing;

/// <summary>
/// Authored print scale. Industrial labels print 1:1. A small calibration
/// band is allowed; fit-to-page and incomplete axes fail closed.
/// </summary>
public static class PrintScaleContract
{
    public const double Identity = 1;
    public const double MinimumScale = 0.5;
    public const double MaximumScale = 2.0;

    public static PrintScaleDecision Evaluate(double scaleX, double scaleY)
    {
        var unsetX = scaleX == 0;
        var unsetY = scaleY == 0;
        if (unsetX && unsetY)
        {
            return PrintScaleDecision.Allowed;
        }

        if (unsetX || unsetY)
        {
            return PrintScaleDecision.Blocked(
                "Print scale is incomplete (only one axis is set). Use 1.0 on both axes, or enter both calibration factors.");
        }

        if (!double.IsFinite(scaleX) || !double.IsFinite(scaleY) || scaleX < 0 || scaleY < 0)
        {
            return PrintScaleDecision.Blocked(
                "Print scale must be finite and not negative.");
        }

        if (!IsCalibrationBand(scaleX) || !IsCalibrationBand(scaleY))
        {
            return PrintScaleDecision.Blocked(
                $"Print scale {scaleX:0.###} × {scaleY:0.###} is outside {MinimumScale:0.#}–{MaximumScale:0.#}. Industrial labels print 1:1; use a small calibration factor, not fit-to-page.");
        }

        return PrintScaleDecision.Allowed;
    }

    public static bool IsCalibrationBand(double scale)
    {
        return double.IsFinite(scale)
            && scale >= MinimumScale
            && scale <= MaximumScale;
    }
}

public readonly record struct PrintScaleDecision(bool IsAllowed, string Diagnostic)
{
    public static PrintScaleDecision Allowed { get; } = new(true, string.Empty);

    public static PrintScaleDecision Blocked(string diagnostic)
        => new(false, diagnostic);
}
