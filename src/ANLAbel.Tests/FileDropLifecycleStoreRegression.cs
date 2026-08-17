using ANLAbel.Data.Automation;

internal static class FileDropLifecycleStoreRegression
{
    public static Task Run()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".jsonl");
        try
        {
            var store = new FileDropLifecycleStore(path);
            Require(store.TryAppend("Starting", "Validated local configuration.", out _, out _), "Lifecycle must record starting.");
            Require(store.TryAppend("Running", "Detect-only watcher active.", out var running, out _), "Lifecycle must record running.");
            var events = store.ReadValid(out var diagnostics);
            Require(events.Count == 2 && diagnostics.Count == 0 && running?.State == "Running", "Lifecycle journal must retain valid events.");
            File.AppendAllText(path, "invalid\n");
            Require(store.ReadValid(out diagnostics).Count == 2 && diagnostics.Count == 1, "Lifecycle journal must retain a valid prefix on corrupt tail.");
            Require(!store.TryAppend("Stopped", "", out _, out var error) && error.Contains("repair", StringComparison.OrdinalIgnoreCase), "A corrupt lifecycle tail must block further lifecycle claims.");
        }
        finally { if (File.Exists(path)) File.Delete(path); }
        return Task.CompletedTask;
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
