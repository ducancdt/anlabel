namespace ANLAbel.Data.PrintLogs;

/// <summary>
/// Pure, order-preserving search contract for the operator Print Center.
/// Scanner input normally contains the durable job id, so an exact id match
/// wins over broad text matches without mutating the recovery report.
/// </summary>
public static class PrintRecoveryCandidateFilter
{
    public static IReadOnlyList<PrintJobRecoveryCandidate> Apply(
        IEnumerable<PrintJobRecoveryCandidate> candidates,
        string? query)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        var source = candidates.ToArray();
        var normalized = query?.Trim() ?? string.Empty;
        if (normalized.Length == 0)
        {
            return source;
        }

        var exact = source
            .Where(candidate => string.Equals(candidate.JobId, normalized, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (exact.Length > 0)
        {
            return exact;
        }

        return source
            .Where(candidate => Matches(candidate, normalized))
            .ToArray();
    }

    public static bool Matches(PrintJobRecoveryCandidate candidate, string query)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        var normalized = query.Trim();
        return Contains(candidate.JobId)
            || Contains(candidate.PrinterName)
            || Contains(candidate.SpoolJobId?.ToString() ?? string.Empty)
            || Contains(candidate.QueueState)
            || Contains(candidate.State.ToString())
            || Contains(candidate.OperatorAction.ToString())
            || Contains(candidate.ManifestFingerprint)
            || Contains(candidate.RelatedJobId)
            || Contains(candidate.Reason);

        bool Contains(string value) => value.Contains(normalized, StringComparison.OrdinalIgnoreCase);
    }
}
