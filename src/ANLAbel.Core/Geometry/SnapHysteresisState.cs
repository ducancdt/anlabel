namespace ANLAbel.Core.Geometry;

/// <summary>
/// Keeps a snap target stable while the pointer moves inside a slightly larger
/// release window than the acquire window.  The document itself remains in mm;
/// callers decide the acquire/release tolerances in that same unit.
/// </summary>
public sealed class SnapHysteresisState
{
    public double? LockedTarget { get; private set; }

    public double? Resolve(double proposedPosition, double? candidateTarget, double releaseTolerance)
    {
        if (releaseTolerance < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(releaseTolerance));
        }

        if (LockedTarget is double locked)
        {
            if (Math.Abs(proposedPosition - locked) <= releaseTolerance)
            {
                return locked;
            }

            LockedTarget = null;
        }

        if (candidateTarget is double target)
        {
            LockedTarget = target;
            return target;
        }

        return null;
    }

    public void Reset()
    {
        LockedTarget = null;
    }
}
