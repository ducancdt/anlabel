using System.Text;
using ANLAbel.Core.Automation;
using ANLAbel.Data.Automation;

internal static class FileDropCsvPreparationRegression
{
    public static Task Run()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".jsonl");
        try
        {
            var good = Encoding.UTF8.GetBytes("Code,Name\nA,One\n");
            var identity = FileDropClaimContract.CreateIdentity("trigger", "config", FileDropClaimContract.ComputeContentFingerprint(good));
            var ledger = new FileDropClaimLedger(path);
            ledger.TryRecordDetection(identity, out _, out _); ledger.TryTransition(identity, FileDropEventState.Claimed, "review", out _, out _);
            var preparation = new FileDropCsvPreparationService(ledger);
            using var goodSource = new MemoryStream(good);
            Require(preparation.TryPrepare(identity, goodSource, out var records, out var prepared) && records.Count == 1 && prepared.Contains("remain unavailable", StringComparison.OrdinalIgnoreCase), "Valid CSV preparation must remain in-memory and non-dispatching.");
            Require(ledger.ReadValid(out var goodDiagnostics).Last(item => item.Identity.EventId == identity.EventId).To == FileDropEventState.Prepared && goodDiagnostics.Count == 0, "Valid CSV preparation must create a durable Prepared state.");

            var malformed = Encoding.UTF8.GetBytes("Code,Name\nA\n");
            var brokenIdentity = FileDropClaimContract.CreateIdentity("trigger", "config", FileDropClaimContract.ComputeContentFingerprint(malformed));
            ledger.TryRecordDetection(brokenIdentity, out _, out _); ledger.TryTransition(brokenIdentity, FileDropEventState.Claimed, "review", out _, out _);
            using var malformedSource = new MemoryStream(malformed);
            Require(!preparation.TryPrepare(brokenIdentity, malformedSource, out _, out var blocked) && blocked.Contains("blocked", StringComparison.OrdinalIgnoreCase), "Malformed CSV must become a durable blocked state.");
            Require(ledger.ReadValid(out var diagnostics).Last(item => item.Identity.EventId == brokenIdentity.EventId).To == FileDropEventState.Blocked && diagnostics.Count == 0, "Parser failure must be auditable as Blocked.");
        }
        finally { if (File.Exists(path)) File.Delete(path); }
        return Task.CompletedTask;
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
