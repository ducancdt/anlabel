namespace ANLAbel.Core.Geometry;

public readonly record struct SpacingInterval(double Leading, double Trailing, string StableKey)
{
    public bool IsFinite => double.IsFinite(Leading) && double.IsFinite(Trailing);
}

public readonly record struct SmartSpacingGap(
    double BeforeTrailing,
    double AfterLeading,
    double Gap,
    string BeforeKey,
    string AfterKey)
{
    /// <summary>Trailing edge of the interval after the measured gap.</summary>
    public double AfterTrailing { get; init; }
}

/// <summary>
/// Pure smart-spacing geometry.  Only adjacent, non-overlapping intervals are
/// considered so pointer evaluation stays O(n log n), even on large labels.
/// </summary>
public static class SmartSpacingContract
{
    private const double EpsilonMm = 0.0001;

    public static IReadOnlyList<SmartSpacingGap> GetAdjacentGaps(IEnumerable<SpacingInterval> intervals)
    {
        ArgumentNullException.ThrowIfNull(intervals);

        var sorted = intervals
            .Where(interval => interval.IsFinite && interval.Trailing >= interval.Leading)
            .OrderBy(interval => interval.Leading)
            .ThenBy(interval => interval.Trailing)
            .ThenBy(interval => interval.StableKey, StringComparer.Ordinal)
            .ToArray();
        if (sorted.Length < 2)
        {
            return Array.Empty<SmartSpacingGap>();
        }

        var result = new List<SmartSpacingGap>(sorted.Length - 1);
        var before = sorted[0];
        for (var index = 1; index < sorted.Length; index++)
        {
            var after = sorted[index];
            var gap = after.Leading - before.Trailing;
            if (gap < -EpsilonMm)
            {
                // Overlapping/nested objects form one occupied run. Keep the
                // interval with the furthest trailing edge so an object inside
                // the run cannot create a false equal-spacing target later.
                if (after.Trailing > before.Trailing)
                {
                    before = after;
                }
                continue;
            }

            result.Add(new SmartSpacingGap(
                before.Trailing,
                after.Leading,
                Math.Max(0, gap),
                before.StableKey,
                after.StableKey)
            {
                AfterTrailing = after.Trailing
            });
            before = after;
        }

        return result;
    }

    public static IReadOnlyList<double> CandidateLeadingPositions(double objectSizeMm, SmartSpacingGap gap)
    {
        if (!double.IsFinite(objectSizeMm) || objectSizeMm <= 0 || !double.IsFinite(gap.Gap)
            || !double.IsFinite(gap.AfterTrailing))
        {
            return Array.Empty<double>();
        }

        // Place the moving object after the right interval or before the left
        // interval while preserving the measured adjacent gap.
        return new[]
        {
            gap.AfterTrailing + gap.Gap,
            gap.BeforeTrailing - gap.Gap - objectSizeMm
        };
    }
}
