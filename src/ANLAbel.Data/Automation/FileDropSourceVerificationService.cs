using System.Security.Cryptography;
using ANLAbel.Core.Automation;

namespace ANLAbel.Data.Automation;

/// <summary>Rechecks immutable source bytes before any future parser/dispatch stage.</summary>
public sealed class FileDropSourceVerificationService
{
    private readonly FileDropClaimLedger _ledger;
    public FileDropSourceVerificationService(FileDropClaimLedger ledger) => _ledger = ledger;

    /// <summary>
    /// Revalidates source bytes while the event is still eligible for a future
    /// dispatch. Preparation is not an authorization boundary: a source that
    /// changes after parsing remains terminally blocked before any later queue step.
    /// </summary>
    public bool VerifyClaimedOrPrepared(FileDropEventIdentity identity, Stream source, out string result)
    {
        var events = _ledger.ReadValid(out var diagnostics);
        if (diagnostics.Count != 0) { result = "Automation claim ledger requires repair before source verification."; return false; }
        var latest = events.LastOrDefault(item => item.Identity.EventId == identity.EventId);
        if (latest is null || (latest.To != FileDropEventState.Claimed && latest.To != FileDropEventState.Prepared))
        {
            result = "Only the latest claimed or prepared evidence can be source-verified.";
            return false;
        }
        var current = Convert.ToHexString(SHA256.HashData(source));
        if (string.Equals(current, identity.SourceFingerprint, StringComparison.Ordinal))
        {
            result = "Source fingerprint still matches the claimed evidence; parser and dispatch remain unavailable.";
            return true;
        }
        _ledger.TryTransition(identity, FileDropEventState.ChangedAfterClaim, "Source fingerprint changed after claim; no parser or dispatch is permitted.", out _, out var error);
        result = string.IsNullOrWhiteSpace(error) ? "Source changed after claim and is now terminally blocked." : error;
        return false;
    }

    // Retained for preparation-stage call sites.
    public bool VerifyClaimed(FileDropEventIdentity identity, Stream source, out string result) =>
        VerifyClaimedOrPrepared(identity, source, out result);
}
