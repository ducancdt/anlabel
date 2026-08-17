namespace ANLAbel.Core.Printing;

/// <summary>
/// Compares the immutable output contract captured during preparation with the
/// contract observed immediately before dispatch.  An empty expectation keeps
/// legacy/direct print callers compatible; once a preview has captured a hash,
/// a missing or changed effective hash fails closed.
/// </summary>
public static class PrintContractGuard
{
    public static bool Matches(string? expectedHash, string? actualHash)
    {
        if (string.IsNullOrWhiteSpace(expectedHash))
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(actualHash)
            && string.Equals(expectedHash.Trim(), actualHash.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Prepared/preview dispatches must carry proof that the driver returned a
    /// serializable effective ticket.  A hash calculated from an empty ticket
    /// is not evidence and must not authorize a job; direct legacy callers can
    /// continue using the two-argument overload.
    /// </summary>
    public static bool Matches(
        string? expectedHash,
        string? actualHash,
        bool actualTicketVerified)
    {
        if (string.IsNullOrWhiteSpace(expectedHash))
        {
            return true;
        }

        return actualTicketVerified && Matches(expectedHash, actualHash);
    }

    /// <summary>
    /// Compares the immutable plan prepared for preflight with the plan
    /// re-read immediately before <c>PrintDocument</c>.  The document and
    /// effective-output fingerprints must remain identical; a change in the
    /// ticket-evidence bit is also a contract change, even when a driver emits
    /// the same aggregate hash.  Empty fingerprints or unverified tickets never
    /// authorize dispatch.
    /// </summary>
    public static bool MatchesDispatchSnapshot(
        string? preparedDocumentHash,
        string? preparedOutputContractHash,
        bool preparedTicketVerified,
        string? finalDocumentHash,
        string? finalOutputContractHash,
        bool finalTicketVerified)
    {
        return !string.IsNullOrWhiteSpace(preparedDocumentHash)
            && !string.IsNullOrWhiteSpace(preparedOutputContractHash)
            && !string.IsNullOrWhiteSpace(finalDocumentHash)
            && !string.IsNullOrWhiteSpace(finalOutputContractHash)
            && string.Equals(preparedDocumentHash.Trim(), finalDocumentHash.Trim(), StringComparison.Ordinal)
            && string.Equals(preparedOutputContractHash.Trim(), finalOutputContractHash.Trim(), StringComparison.OrdinalIgnoreCase)
            && preparedTicketVerified
            && finalTicketVerified;
    }
}
