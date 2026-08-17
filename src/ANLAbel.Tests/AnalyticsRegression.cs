using ANLAbel.App.ViewModels;
using ANLAbel.Core.Printing;
using ANLAbel.Data.PrintLogs;

internal static class AnalyticsRegression
{
    public static async Task Run()
    {
        var timestamp = DateTimeOffset.UtcNow;
        var failed = new PrintJobStateEvent("job-failed", 1, PrintJobLifecycleState.Dispatching, PrintJobLifecycleState.Failed, timestamp, "driver error", "Queue-A", null, "Error", "", "", "", false, "", "");
        var healthy = new PrintJobStateEvent("job-ok", 1, PrintJobLifecycleState.Dispatching, PrintJobLifecycleState.QueueObserved, timestamp, "", "Queue-A", 5, "Printing", "", "", "", false, "", "");
        var vm = new AnalyticsViewModel(_ => Task.FromResult(new HistorySnapshot(new PrintJobRecoverySnapshot([failed, healthy], ["state diagnostic"]), [new PrintOperationLogEntry { JobId = "job-ok", Success = false }], [], [new PrintLogSummary(1, timestamp.LocalDateTime, "T", "Q", "", "1"), new PrintLogSummary(2, timestamp.LocalDateTime, "T", "Q", "", "1")], [])));
        await vm.RefreshAsync();
        Require(vm.RecordedLabelRows == 2, "CSV rows must remain a separate label-row unit.");
        Require(vm.RecordedJobs == 2, "Durable JobId values must remain a separate job unit.");
        Require(vm.ErrorsOrUncertain == 3, "Failed lifecycle, failed operation trace and state diagnostic must remain explicit error/uncertainty evidence.");
        Require(vm.SourceHealth.Contains("Partial", StringComparison.Ordinal), "Diagnostics must prevent a healthy-coverage claim.");
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "ANLAbel.App", "AnalyticsWindow.xaml"));
        var xaml = File.ReadAllText(path);
        foreach (var required in new[] { "CC.P6.Analytics.Root", "CC.P6.Analytics.SourceHealth", "CC.P6.Analytics.PhysicalDisclaimer", "CC.P6.Analytics.OpenHistory" }) Require(xaml.Contains(required, StringComparison.Ordinal), $"Analytics must expose '{required}'.");
        foreach (var forbidden in new[] { "Reprint", "Pause", "Dispatch", "License" }) Require(!xaml.Contains(forbidden, StringComparison.OrdinalIgnoreCase), $"Analytics must not expose '{forbidden}'.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
