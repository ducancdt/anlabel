using ANLAbel.App.ViewModels;
using ANLAbel.Data.PrintLogs;

internal static class OperationsOverviewRegression
{
    public static async Task Run()
    {
        await RejectsStaleRefreshAsync();
        await PreservesPartialEvidenceAsync();
        AssertUiContractExcludesLicensing();
    }

    private static async Task RejectsStaleRefreshAsync()
    {
        var firstEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirst = new TaskCompletionSource<OperationsQueueEvidence>(TaskCreationOptions.RunContinuationsAsynchronously);
        var queueCall = 0;
        var firstObservedAt = new DateTimeOffset(2026, 8, 13, 1, 0, 0, TimeSpan.Zero);
        var secondObservedAt = firstObservedAt.AddMinutes(1);

        var overview = new OperationsOverviewViewModel(
            cancellationToken =>
            {
                var call = Interlocked.Increment(ref queueCall);
                if (call == 1)
                {
                    firstEntered.SetResult();
                    return releaseFirst.Task.WaitAsync(cancellationToken);
                }

                return Task.FromResult(new OperationsQueueEvidence(
                    "Queue-New",
                    true,
                    "Queue-New",
                    string.Empty,
                    secondObservedAt));
            },
            _ => Task.FromResult(PrintJobRecoveryReport.Empty),
            () => secondObservedAt);

        var firstRefresh = overview.RefreshAsync();
        await firstEntered.Task.WaitAsync(TimeSpan.FromSeconds(2));
        var secondApplied = await overview.RefreshAsync();

        releaseFirst.SetResult(new OperationsQueueEvidence(
            "Queue-Old",
            false,
            string.Empty,
            "stale missing queue",
            firstObservedAt));
        var firstApplied = await firstRefresh;

        Require(secondApplied, "Newest operations refresh must apply.");
        Require(!firstApplied, "Older operations refresh must be rejected after a newer epoch applies.");
        Require(overview.QueueName == "Queue-New", "Stale queue evidence must not overwrite the latest snapshot.");
        Require(overview.QueueStateText == "Available", "Latest queue state must remain visible.");
        Require(!overview.IsRefreshing, "Refresh busy state must clear after the current epoch completes.");
    }

    private static async Task PreservesPartialEvidenceAsync()
    {
        var overview = new OperationsOverviewViewModel(
            _ => throw new InvalidOperationException("queue probe failed"),
            _ => Task.FromResult(PrintJobRecoveryReport.Empty));

        var applied = await overview.RefreshAsync();

        Require(applied, "A partial source result is still a valid overview snapshot.");
        Require(overview.QueueStateText == "Read failed", "Queue source failure must remain explicit.");
        Require(overview.RecoveryStateText == "Clear", "Healthy recovery evidence must survive a queue source failure.");
        Require(overview.SourceStatusText.Contains("Partial result", StringComparison.Ordinal),
            "Partial refresh must be named in the overview status.");
    }

    private static void AssertUiContractExcludesLicensing()
    {
        var xamlPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "ANLAbel.App",
            "OperationsOverviewWindow.xaml"));
        Require(File.Exists(xamlPath), $"Operations overview XAML must exist: {xamlPath}");

        var xaml = File.ReadAllText(xamlPath);
        foreach (var automationId in new[]
                 {
                     "CC.P1.Overview.Root",
                     "CC.P1.Overview.Refresh",
                     "CC.P1.Overview.Queue",
                     "CC.P1.Overview.Recovery",
                     "CC.P1.Overview.Diagnostics",
                     "CC.P1.Overview.OpenPrinterSetup",
                     "CC.P1.Overview.OpenPrintCenter",
                     "CC.P1.Overview.OpenHistory"
                 })
        {
            Require(xaml.Contains(automationId, StringComparison.Ordinal),
                $"Operations overview must expose AutomationId '{automationId}'.");
        }

        foreach (var excluded in new[] { "license", "activation", "entitlement", "printer seat" })
        {
            Require(!xaml.Contains(excluded, StringComparison.OrdinalIgnoreCase),
                $"Operations overview must exclude the out-of-scope concept '{excluded}'.");
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
