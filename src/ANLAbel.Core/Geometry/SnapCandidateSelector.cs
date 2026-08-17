namespace ANLAbel.Core.Geometry;

/// <summary>
/// Candidate ranking for smart alignment.  Semantic priority is evaluated
/// before physical distance so a same-edge/same-center match is not stolen by
/// a less meaningful cross-anchor that happens to be a fraction closer.
/// </summary>
public readonly record struct SnapCandidate(
    double SourcePosition,
    double TargetPosition,
    double Distance,
    int Priority,
    string StableKey,
    string? Label = null)
{
    public double Delta => TargetPosition - SourcePosition;
}

public static class SnapCandidateSelector
{
    public static SnapCandidate? Choose(IEnumerable<SnapCandidate> candidates, double acquireTolerance)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        if (!double.IsFinite(acquireTolerance) || acquireTolerance <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(acquireTolerance));
        }

        var eligible = candidates
            .Where(candidate => candidate.Distance >= 0 && candidate.Distance <= acquireTolerance)
            .OrderByDescending(candidate => candidate.Priority)
            .ThenBy(candidate => candidate.Distance)
            .ThenBy(candidate => candidate.StableKey, StringComparer.Ordinal)
            .ToArray();
        return eligible.Length == 0 ? null : eligible[0];
    }
}
