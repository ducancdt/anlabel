using ANLAbel.App;
using ANLAbel.Core.Automation;
using ANLAbel.Core.Data;
using ANLAbel.Core.Workflow;
using ANLAbel.Data.Automation;

internal static class AutomationPreparedBatchDispatchRegression
{
    public static async Task Run()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".jsonl");
        var historyPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".jsonl");
        try
        {
            var configuration = new FileDropTriggerConfiguration("dispatch-trigger", "Dispatch", Path.GetTempPath(), "*.csv", false, true, Path.Combine(Path.GetTempPath(), "template.anlabel"), "Named queue", DocumentWorkflowPrintPolicyMode.Off);
            var source = FileDropClaimContract.ComputeContentFingerprint("SKU,LOT\nA,L\n"u8);
            var eventIdentity = FileDropClaimContract.CreateIdentity(configuration.TriggerId, configuration.ConfigurationFingerprint, source);
            var records = new[] { DataRecord.Create([new("SKU", "A"), new("LOT", "L")]) };
            var batch = FileDropPreparedBatchContract.Create(eventIdentity, "template-hash", records);
            var ledger = new FileDropClaimLedger(path);
            ledger.TryRecordDetection(eventIdentity, out _, out _);
            ledger.TryTransition(eventIdentity, FileDropEventState.Claimed, "review", out _, out _);
            ledger.TryTransition(eventIdentity, FileDropEventState.Prepared, "prepared", out _, out _);
            var history = new AutomationJobHistoryStore(historyPath);
            var service = new AutomationPreparedBatchDispatchService(ledger, history);
            var preflight = new AutomationPreparedBatchPreflightResult(true, "Named queue", [], "passed");
            var calls = 0;
            var dispatched = await service.DispatchAsync(batch, configuration, records, preflight, (_, _) => { calls++; return Task.FromResult(new AutomationPreparedBatchDispatchReceipt(true, "job-001", "manifest-001")); });
            Require(dispatched.Dispatched && calls == 1, "One exact prepared event may dispatch once after preflight passes.");
            Require(ledger.ReadValid(out var valid).Last().To == FileDropEventState.Dispatched && valid.Count == 0, "Accepted dispatch must be durably terminal without claiming physical output.");
            var links = history.ReadValid(out var historyDiagnostics);
            Require(links.Count == 1 && historyDiagnostics.Count == 0 && links[0].EventId == batch.EventId && links[0].JobId == "job-001", "Accepted dispatch must write an integrity-protected event-to-job link without record payloads.");

            var duplicate = await service.DispatchAsync(batch, configuration, records, preflight, (_, _) => { calls++; return Task.FromResult(new AutomationPreparedBatchDispatchReceipt(true, "job-002")); });
            Require(!duplicate.Dispatched && calls == 1, "Duplicate event IDs must be blocked before a second dispatcher invocation.");

            var secondSource = FileDropClaimContract.ComputeContentFingerprint("SKU,LOT\nB,L\n"u8);
            var secondEvent = FileDropClaimContract.CreateIdentity(configuration.TriggerId, configuration.ConfigurationFingerprint, secondSource);
            var secondBatch = FileDropPreparedBatchContract.Create(secondEvent, "template-hash", records);
            ledger.TryRecordDetection(secondEvent, out _, out _);
            ledger.TryTransition(secondEvent, FileDropEventState.Claimed, "review", out _, out _);
            ledger.TryTransition(secondEvent, FileDropEventState.Prepared, "prepared", out _, out _);
            var interrupted = await service.DispatchAsync(secondBatch, configuration, records, preflight, (_, _) => throw new OperationCanceledException("simulated stop"));
            Require(!interrupted.Dispatched && ledger.ReadValid(out valid).Last(item => item.Identity.EventId == secondEvent.EventId).To == FileDropEventState.Blocked,
                "A failed or interrupted dispatcher invocation must become Blocked with no retry.");
        }
        finally { if (File.Exists(path)) File.Delete(path); if (File.Exists(historyPath)) File.Delete(historyPath); }
    }

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
