using ANLAbel.Core.Automation;

namespace ANLAbel.Data.Automation;

/// <summary>
/// Explicit local review action for fingerprint-ledger claims. It does not
/// mutate or reread the source and therefore cannot parse, queue or print it.
/// </summary>
public sealed class FileDropClaimReviewService
{
    private readonly FileDropClaimLedger _ledger;
    public FileDropClaimReviewService(FileDropClaimLedger ledger) => _ledger = ledger;

    public int ClaimDetected(out string result)
    {
        var events = _ledger.ReadValid(out var diagnostics);
        if (diagnostics.Count != 0) { result = "Automation claim ledger requires repair before review."; return 0; }
        var pending = events.GroupBy(item => item.Identity.EventId, StringComparer.Ordinal).Select(group => group.Last()).Where(item => item.To == FileDropEventState.Detected).ToArray();
        var claimed = 0;
        foreach (var item in pending)
            if (_ledger.TryTransition(item.Identity, FileDropEventState.Claimed, "Explicit local review; source bytes were not moved or parsed.", out _, out _)) claimed++;
        result = claimed == 0 ? "No detected evidence is pending review." : $"Claimed {claimed} detected evidence item(s); no source was moved, parsed, queued or printed.";
        return claimed;
    }
}
