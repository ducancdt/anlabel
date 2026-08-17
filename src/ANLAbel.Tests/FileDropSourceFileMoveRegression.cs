using ANLAbel.Core.Automation;
using ANLAbel.Data.Automation;

internal static class FileDropSourceFileMoveRegression
{
    public static Task Run()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            var watch = Path.Combine(root, "watch"); var archive = Path.Combine(root, "archive"); Directory.CreateDirectory(watch);
            var source = Path.Combine(watch, "batch.csv"); File.WriteAllText(source, "SKU\nA\n");
            var configuration = new FileDropTriggerConfiguration("move-trigger", "Move", watch, "*.csv", false, true);
            var identity = FileDropClaimContract.CreateIdentity(configuration.TriggerId, configuration.ConfigurationFingerprint, FileDropClaimContract.ComputeContentFingerprint(File.ReadAllBytes(source)));
            var ledger = new FileDropClaimLedger(Path.Combine(root, "ledger.jsonl"));
            ledger.TryRecordDetection(identity, out _, out _); ledger.TryTransition(identity, FileDropEventState.Claimed, "review", out _, out _); ledger.TryTransition(identity, FileDropEventState.Prepared, "prepared", out _, out _); ledger.TryTransition(identity, FileDropEventState.Dispatching, "dispatch", out _, out _); ledger.TryTransition(identity, FileDropEventState.Dispatched, "submitted", out _, out _);
            var mover = new FileDropSourceFileMoveService(ledger);
            Require(mover.TryMove(identity, configuration, source, archive, FileDropSourceDisposition.Archive, out var target, out _), "Dispatched source must move atomically to a validated local archive.");
            Require(!File.Exists(source) && File.Exists(target), "Archive move must rename the source without a duplicate at the watch path.");
            Require(ledger.ReadValid(out var diagnostics).Last().To == FileDropEventState.Archived && diagnostics.Count == 0, "Archive completion must be durable and terminal.");
            var outside = Path.Combine(root, "outside.csv"); File.WriteAllText(outside, "SKU\nB\n");
            Require(!mover.TryMove(identity, configuration, outside, archive, FileDropSourceDisposition.Archive, out _, out var rejected) && rejected.Contains("watch root", StringComparison.OrdinalIgnoreCase), "Move must reject a source outside its configured watch root.");
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        return Task.CompletedTask;
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
