namespace ANLAbel.Core.Printing;

/// <summary>
/// Queue metadata captured before or after a dispatch.  It is intentionally
/// value-only so identity attribution can be tested without a Windows spooler.
/// </summary>
public sealed record SpoolJobIdentityCandidate(
    int JobId,
    string JobName = "");

/// <summary>
/// Resolves a newly visible queue job without guessing.  A print submission is
/// correlated only when exactly one post-dispatch job is absent from the
/// pre-dispatch snapshot.  A matching job name can disambiguate a concurrent
/// queue submission; duplicate matches remain unknown by design.
/// </summary>
public static class SpoolJobIdentityResolver
{
    public static int? TryResolve(
        IReadOnlyCollection<SpoolJobIdentityCandidate>? before,
        IReadOnlyCollection<SpoolJobIdentityCandidate>? after,
        string? submittedJobName = null)
    {
        if (before is null || after is null)
        {
            return null;
        }

        var knownIds = before
            .Where(candidate => candidate is not null && candidate.JobId > 0)
            .Select(candidate => candidate.JobId)
            .ToHashSet();
        var candidates = after
            .Where(candidate => candidate is not null
                && candidate.JobId > 0
                && !knownIds.Contains(candidate.JobId))
            .ToArray();
        if (candidates.Length == 0)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(submittedJobName))
        {
            var named = candidates
                .Where(candidate => string.Equals(
                    candidate.JobName,
                    submittedJobName.Trim(),
                    StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (named.Length > 0)
            {
                candidates = named;
            }
        }

        return candidates.Length == 1 ? candidates[0].JobId : null;
    }
}
