using ANLAbel.Core.Automation;
using ANLAbel.Data.Automation;

internal static class FileDropClaimLedgerRegression
{
    public static Task Run()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".jsonl");
        try
        {
            var identity = FileDropClaimContract.CreateIdentity("incoming-csv", "config-v1", "source-v1");
            var ledger = new FileDropClaimLedger(path);
            Require(ledger.TryRecordDetection(identity, out var detected, out _), "A new source must be recorded as detected.");
            Require(detected?.To == FileDropEventState.Detected, "Detection must retain its explicit state.");
            Require(!ledger.TryRecordDetection(identity, out _, out var duplicate) && duplicate.Contains("Duplicate", StringComparison.OrdinalIgnoreCase), "Repeated notifications must not create a second durable claim event.");
            Require(ledger.TryTransition(identity, FileDropEventState.Claimed, "Stable after debounce", out _, out _), "A detected source must be claimable.");
            Require(ledger.TryTransition(identity, FileDropEventState.ChangedAfterClaim, "Content hash changed", out _, out _), "Changed bytes must be durably terminal.");
            Require(!ledger.TryTransition(identity, FileDropEventState.Dispatched, "", out _, out _), "A terminal event must not dispatch later.");
            var events = ledger.ReadValid(out var diagnostics);
            Require(events.Count == 3 && diagnostics.Count == 0, "The valid claim chain must retain all transitions.");
            File.AppendAllText(path, "invalid\n");
            Require(ledger.ReadValid(out diagnostics).Count == 3 && diagnostics.Count == 1, "A corrupt tail must preserve the valid claim prefix.");
            Require(!ledger.TryRecordDetection(FileDropClaimContract.CreateIdentity("incoming-csv", "config-v1", "source-v2"), out _, out var repair) && repair.Contains("repair", StringComparison.OrdinalIgnoreCase), "A corrupt audit tail must block new claims.");
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
        return Task.CompletedTask;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
