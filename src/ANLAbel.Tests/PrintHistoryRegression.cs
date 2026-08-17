using ANLAbel.App.ViewModels;
using ANLAbel.Core.Printing;
using ANLAbel.Data.PrintLogs;

internal static class PrintHistoryRegression
{
    public static async Task Run()
    {
        await StateStoreWinsLifecycleAndCsvStaysSeparateAsync();
        AssertHistoryUiExcludesMutation();
    }

    private static async Task StateStoreWinsLifecycleAndCsvStaysSeparateAsync()
    {
        var timestamp = new DateTimeOffset(2026, 8, 13, 12, 0, 0, TimeSpan.Zero);
        var state = new PrintJobStateEvent("job-1", 3, PrintJobLifecycleState.Dispatching, PrintJobLifecycleState.QueueObserved, timestamp, "queue observed", "Queue-A", 4, "Printing", "doc", "scene", "output", false, "prev", "hash");
        var operation = new PrintOperationLogEntry { JobId = "job-1", TemplateName = "Template-A", PrinterName = "Queue-A", Outcome = "Success", SpoolState = "Completed", TimestampLocal = timestamp.LocalDateTime };
        var vm = new PrintHistoryViewModel(_ => Task.FromResult(new HistorySnapshot(new PrintJobRecoverySnapshot([state], ["valid prefix diagnostic"]), [operation], [], [new PrintLogSummary(1, timestamp.LocalDateTime, "Template-A", "Queue-A", "Current", "1")], [])));
        Require(await vm.RefreshAsync(), "History snapshot must load.");
        var job = vm.Rows.Single(row => row.RecordId == "job-1");
        Require(job.Lifecycle == "QueueObserved", "Durable state must win lifecycle over operation outcome.");
        Require(job.Source == "State store + operation trace", "Merged job must retain source provenance.");
        Require(job.Evidence.Contains("physical output unverified", StringComparison.OrdinalIgnoreCase), "Spool evidence must not claim physical output.");
        var csv = vm.Rows.Single(row => row.RecordType == "CsvLabelRecord");
        Require(csv.RecordId == "csv:1", "CSV record must not fabricate a JobId.");
        Require(csv.Detail.Contains("no JobId was fabricated", StringComparison.OrdinalIgnoreCase), "CSV detail must name its identity boundary.");
        Require(vm.StatusText.Contains("diagnostic", StringComparison.OrdinalIgnoreCase), "Store diagnostics must stay visible in history status.");
    }

    private static void AssertHistoryUiExcludesMutation()
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "ANLAbel.App", "PrintHistoryWindow.xaml"));
        var xaml = File.ReadAllText(path);
        foreach (var id in new[] { "CC.P5.History.Root", "CC.P5.History.ActivityTable", "CC.P5.History.Detail", "CC.P5.History.OpenPrintCenter" }) Require(xaml.Contains(id, StringComparison.Ordinal), $"History UI must expose '{id}'.");
        foreach (var excluded in new[] { "RequestReprint", "ApproveReprint", "Dispatch", "Activation", "License" }) Require(!xaml.Contains(excluded, StringComparison.OrdinalIgnoreCase), $"History must not introduce '{excluded}'.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
