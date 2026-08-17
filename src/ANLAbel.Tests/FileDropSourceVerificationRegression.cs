using System.Text;
using ANLAbel.Core.Automation;
using ANLAbel.Data.Automation;

internal static class FileDropSourceVerificationRegression
{
    public static Task Run()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".jsonl");
        try
        {
            var bytes = Encoding.UTF8.GetBytes("original bytes");
            var identity = FileDropClaimContract.CreateIdentity("trigger", "config", FileDropClaimContract.ComputeContentFingerprint(bytes));
            var ledger = new FileDropClaimLedger(path);
            ledger.TryRecordDetection(identity, out _, out _); ledger.TryTransition(identity, FileDropEventState.Claimed, "review", out _, out _);
            var verifier = new FileDropSourceVerificationService(ledger);
            using var unchanged = new MemoryStream(bytes);
            Require(verifier.VerifyClaimed(identity, unchanged, out var stable) && stable.Contains("still matches", StringComparison.OrdinalIgnoreCase), "Matching bytes must remain claimed without dispatch.");
            using var changed = new MemoryStream(Encoding.UTF8.GetBytes("changed bytes"));
            Require(!verifier.VerifyClaimed(identity, changed, out var blocked) && blocked.Contains("terminally blocked", StringComparison.OrdinalIgnoreCase), "Changed bytes must become terminally blocked.");
            Require(ledger.ReadValid(out var diagnostics).Last().To == FileDropEventState.ChangedAfterClaim && diagnostics.Count == 0, "Changed-after-claim must be durable.");

            var preparedIdentity = FileDropClaimContract.CreateIdentity("prepared-trigger", "config", FileDropClaimContract.ComputeContentFingerprint(bytes));
            ledger.TryRecordDetection(preparedIdentity, out _, out _); ledger.TryTransition(preparedIdentity, FileDropEventState.Claimed, "review", out _, out _);
            ledger.TryTransition(preparedIdentity, FileDropEventState.Prepared, "parsed", out _, out _);
            using var changedAfterPreparation = new MemoryStream(Encoding.UTF8.GetBytes("changed after preparation"));
            Require(!verifier.VerifyClaimedOrPrepared(preparedIdentity, changedAfterPreparation, out var preparedBlocked) && preparedBlocked.Contains("terminally blocked", StringComparison.OrdinalIgnoreCase), "Prepared records must be invalidated when their source changes before any future dispatch.");
            Require(ledger.ReadValid(out diagnostics).Last(item => item.Identity.EventId == preparedIdentity.EventId).To == FileDropEventState.ChangedAfterClaim && diagnostics.Count == 0, "Changed prepared bytes must be durably terminal.");
        }
        finally { if (File.Exists(path)) File.Delete(path); }
        return Task.CompletedTask;
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
