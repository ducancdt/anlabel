using ANLAbel.Core.Enums;

namespace ANLAbel.Core.Geometry;

/// <summary>
/// Keeps keyboard movement deterministic and independent of monitor DPI/zoom.
/// The WPF shell maps modifiers to a mode; all geometry is then applied in mm.
/// </summary>
public static class NudgeStepContract
{
    public const double FineStepMm = 0.01;
    public const double StandardStepMm = 0.1;
    public const double CoarseStepMm = 1.0;

    public static double ResolveStepMm(NudgeStepMode mode)
    {
        return mode switch
        {
            NudgeStepMode.Fine => FineStepMm,
            NudgeStepMode.Coarse => CoarseStepMm,
            _ => StandardStepMm
        };
    }

    public static (double DeltaX, double DeltaY) ResolveDelta(
        NudgeDirection direction,
        NudgeStepMode mode)
    {
        var step = ResolveStepMm(mode);
        return direction switch
        {
            NudgeDirection.Left => (-step, 0),
            NudgeDirection.Up => (0, -step),
            NudgeDirection.Right => (step, 0),
            NudgeDirection.Down => (0, step),
            _ => (0, 0)
        };
    }
}
