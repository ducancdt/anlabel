using ANLAbel.Core.Automation;
using ANLAbel.Data.Automation;

internal static class FileDropDetectionServiceRegression
{
    public static Task Run()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(directory);
        var source = Path.Combine(directory, "input.csv");
        var ledgerPath = Path.Combine(directory, "claims.jsonl");
        try
        {
            File.WriteAllText(source, "Code,Name\nA,One\n");
            var configuration = new FileDropTriggerConfiguration("test-drop", "Test drop", directory, "*.csv", false, true);
            var ledger = new FileDropClaimLedger(ledgerPath);
            using var service = new FileDropDetectionService(configuration, ledger);
            Require(service.TryDetect(source, out var first) && first.Contains("no source was claimed", StringComparison.OrdinalIgnoreCase), "Detection must only create evidence, never a claim.");
            Require(!service.TryDetect(source, out var duplicate) && duplicate.Contains("Duplicate", StringComparison.OrdinalIgnoreCase), "Repeated detection must collapse to the same durable identity.");
            Require(ledger.ReadValid(out var diagnostics).Single().To == FileDropEventState.Detected && diagnostics.Count == 0, "Detect-only runner must write just Detected state.");
            Require(!service.TryStart(out _) || service.IsRunning, "A configured local root may start only its independent detect-only watcher.");
            service.Stop();
            Require(!service.IsRunning, "Stopping must release the watcher.");
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
        return Task.CompletedTask;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
