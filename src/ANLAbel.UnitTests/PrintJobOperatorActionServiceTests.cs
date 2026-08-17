using ANLAbel.Core.Printing;
using ANLAbel.Data.PrintLogs;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class PrintJobOperatorActionServiceTests
{
    [Fact]
    public async Task AcknowledgeIsSameStateAuditAndSurvivesRestart()
    {
        var directory = CreateDirectory();
        var path = Path.Combine(directory, "events.jsonl");

        try
        {
            var store = new PrintJobStateStore(path);
            await AppendDispatchPrefixAsync(store, "job-ack", spoolJobId: 501, printerName: "Zebra Test");

            var result = await PrintJobOperatorActionService.AcknowledgeAsync(store, "job-ack", actor: "alice");

            Assert.True(result.Succeeded);
            Assert.False(result.AutomaticRetryAllowed);
            Assert.False(result.PhysicalOutputVerified);
            Assert.Equal(PrintJobOperatorAction.Acknowledge, result.Event!.OperatorAction);
            Assert.Equal(PrintJobLifecycleState.SpoolAccepted, result.Event.From);
            Assert.Equal(PrintJobLifecycleState.SpoolAccepted, result.Event.To);
            Assert.Equal("alice", result.Event.Actor);
            Assert.Equal(PrintJobLifecycleState.SpoolAccepted, store.GetCurrentState("job-ack"));

            var reopened = new PrintJobStateStore(path);
            var report = await PrintJobRecoveryService.LoadAsync(reopened);
            var candidate = Assert.Single(report.Candidates);
            Assert.Equal(PrintJobOperatorAction.Acknowledge, candidate.OperatorAction);
            Assert.Equal(PrintJobRecoveryAction.OperatorDecision, candidate.Action);
            Assert.Contains("acknowledged", candidate.Reason, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            TryDelete(directory);
        }
    }

    [Fact]
    public async Task VoidTerminalizesLineageWithoutPhysicalCompletion()
    {
        var directory = CreateDirectory();
        var path = Path.Combine(directory, "events.jsonl");

        try
        {
            var store = new PrintJobStateStore(path);
            await AppendDispatchPrefixAsync(store, "job-void", spoolJobId: 502, printerName: "Zebra Test");

            var result = await PrintJobOperatorActionService.VoidAsync(store, "job-void", actor: "bob", reason: "Operator confirmed duplicate risk.");

            Assert.True(result.Succeeded);
            Assert.Equal(PrintJobOperatorAction.Void, result.Event!.OperatorAction);
            Assert.Equal(PrintJobLifecycleState.Cancelled, result.Event.To);
            Assert.False(result.Event.PhysicalOutputVerified);
            Assert.Equal(PrintJobLifecycleState.Cancelled, store.GetCurrentState("job-void"));
            Assert.Empty((await PrintJobRecoveryService.LoadAsync(store)).Candidates);
        }
        finally
        {
            TryDelete(directory);
        }
    }

    [Fact]
    public async Task ReprintCreatesLinkedCreatedChildWithoutDispatch()
    {
        var directory = CreateDirectory();
        var path = Path.Combine(directory, "events.jsonl");

        try
        {
            var store = new PrintJobStateStore(path);
            var manifest = CreateManifest("P-503");
            await AppendDispatchPrefixAsync(store, "job-parent", spoolJobId: 503, printerName: "Zebra Test", manifest: manifest);

            var result = await PrintJobOperatorActionService.RequestReprintAsync(store, "job-parent", actor: "carol");

            Assert.True(result.Succeeded);
            Assert.False(result.AutomaticRetryAllowed);
            Assert.False(result.PhysicalOutputVerified);
            Assert.NotEmpty(result.RelatedJobId);
            Assert.Equal(result.RelatedJobId, result.Event!.RelatedJobId);
            Assert.Equal(PrintJobOperatorAction.ReprintRequested, result.Event.OperatorAction);
            Assert.Equal(PrintJobLifecycleState.SpoolAccepted, result.Event.To);
            Assert.Equal(result.RelatedJobId, result.RelatedEvent!.JobId);
            Assert.Equal(PrintJobLifecycleState.Created, result.RelatedEvent.From);
            Assert.Equal(PrintJobLifecycleState.Created, result.RelatedEvent.To);
            Assert.Equal("job-parent", result.RelatedEvent.RelatedJobId);
            Assert.Null(result.RelatedEvent.SpoolJobId);
            Assert.Equal(manifest.Fingerprint, result.Event.ManifestFingerprint);
            Assert.Equal(manifest.Fingerprint, result.RelatedEvent.ManifestFingerprint);
            Assert.Equal(manifest, result.RelatedEvent.Manifest);

            var reopened = new PrintJobStateStore(path);
            Assert.Equal(PrintJobLifecycleState.SpoolAccepted, reopened.GetCurrentState("job-parent"));
            Assert.Equal(PrintJobLifecycleState.Created, reopened.GetCurrentState(result.RelatedJobId));
            var events = await reopened.ReadEventsAsync(result.RelatedJobId);
            Assert.Single(events);
            Assert.Equal(PrintJobOperatorAction.ReprintRequested, events[0].OperatorAction);
            Assert.Contains((await PrintJobRecoveryService.LoadAsync(reopened)).Candidates,
                item => item.JobId == result.RelatedJobId && item.OperatorAction == PrintJobOperatorAction.ReprintRequested);
        }
        finally
        {
            TryDelete(directory);
        }
    }

    [Fact]
    public async Task ReprintApprovalRequiresExactManifestAndIsDurable()
    {
        var directory = CreateDirectory();
        var path = Path.Combine(directory, "events.jsonl");

        try
        {
            var store = new PrintJobStateStore(path);
            var captured = CreateManifest("P-approval");
            await AppendDispatchPrefixAsync(store, "job-approval", spoolJobId: 505, printerName: "Zebra Test", manifest: captured);
            var request = await PrintJobOperatorActionService.RequestReprintAsync(store, "job-approval", actor: "reviewer");

            var mismatch = CreateManifest("P-changed");
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                PrintJobOperatorActionService.ApproveReprintAsync(store, request.RelatedJobId, mismatch, actor: "reviewer"));
            Assert.Single(await store.ReadEventsAsync(request.RelatedJobId));

            var approval = await PrintJobOperatorActionService.ApproveReprintAsync(
                store,
                request.RelatedJobId,
                captured,
                actor: "reviewer");
            Assert.True(approval.Succeeded);
            Assert.Equal(PrintJobOperatorAction.ReprintApproved, approval.Event!.OperatorAction);
            Assert.Equal(PrintJobLifecycleState.Created, approval.Event.From);
            Assert.Equal(PrintJobLifecycleState.Created, approval.Event.To);
            Assert.Equal(captured.Fingerprint, approval.Event.ManifestFingerprint);
            Assert.Equal(captured, approval.Event.Manifest);

            var reopened = new PrintJobStateStore(path);
            var events = await reopened.ReadEventsAsync(request.RelatedJobId);
            Assert.Equal(2, events.Count);
            Assert.Equal(PrintJobOperatorAction.ReprintApproved, events[^1].OperatorAction);
            var candidate = Assert.Single((await PrintJobRecoveryService.LoadAsync(reopened)).Candidates,
                item => item.JobId == request.RelatedJobId);
            Assert.Equal(PrintJobOperatorAction.ReprintApproved, candidate.OperatorAction);
            Assert.Contains("approved", candidate.Reason, StringComparison.OrdinalIgnoreCase);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                PrintJobOperatorActionService.ApproveReprintAsync(store, request.RelatedJobId, captured));
        }
        finally
        {
            TryDelete(directory);
        }
    }

    [Fact]
    public async Task TerminalHistoryRejectsFurtherOperatorActions()
    {
        var directory = CreateDirectory();
        var path = Path.Combine(directory, "events.jsonl");

        try
        {
            var store = new PrintJobStateStore(path);
            await AppendDispatchPrefixAsync(store, "job-terminal", spoolJobId: 504, printerName: "Zebra Test");
            await PrintJobOperatorActionService.VoidAsync(store, "job-terminal");

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                PrintJobOperatorActionService.RequestReprintAsync(store, "job-terminal"));
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                PrintJobOperatorActionService.AcknowledgeAsync(store, "job-terminal"));
        }
        finally
        {
            TryDelete(directory);
        }
    }

    private static async Task AppendDispatchPrefixAsync(
        PrintJobStateStore store,
        string jobId,
        int spoolJobId,
        string printerName,
        PrintJobManifest? manifest = null)
    {
        await store.AppendAsync(Transition(jobId, PrintJobLifecycleState.Created, PrintJobLifecycleState.Preparing, "open", printerName, spoolJobId, manifest));
        await store.AppendAsync(Transition(jobId, PrintJobLifecycleState.Preparing, PrintJobLifecycleState.PreflightPassed, "preflight", printerName, spoolJobId, manifest));
        await store.AppendAsync(Transition(jobId, PrintJobLifecycleState.PreflightPassed, PrintJobLifecycleState.Dispatching, "dispatch", printerName, spoolJobId, manifest));
        await store.AppendAsync(Transition(jobId, PrintJobLifecycleState.Dispatching, PrintJobLifecycleState.SpoolAccepted, "spool accepted", printerName, spoolJobId, manifest));
    }

    private static PrintJobStateTransition Transition(
        string jobId,
        PrintJobLifecycleState from,
        PrintJobLifecycleState to,
        string reason,
        string printerName,
        int spoolJobId,
        PrintJobManifest? manifest = null)
    {
        return new PrintJobStateTransition(
            jobId,
            from,
            to,
            DateTimeOffset.UtcNow,
            reason,
            PrinterName: printerName,
            SpoolJobId: spoolJobId,
            DocumentHash: "doc",
            SceneHash: "scene",
            OutputContractHash: "contract",
            ManifestFingerprint: manifest?.Fingerprint ?? string.Empty,
            Manifest: manifest);
    }

    private static PrintJobManifest CreateManifest(string partNumber)
    {
        return PrintJobManifest.Create(
            "Approval label",
            "approval.alabel",
            "Approval reprint",
            "Zebra Test",
            100,
            50,
            203,
            203,
            1,
            1,
            new IReadOnlyDictionary<string, string>?[]
            {
                new Dictionary<string, string> { ["PartNo"] = partNumber }
            },
            documentHash: "doc",
            textResourceFingerprint: "text",
            sceneHash: "scene",
            outputContractHash: "contract");
    }

    private static string CreateDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "ANLAbel-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static void TryDelete(string directory)
    {
        try
        {
            Directory.Delete(directory, recursive: true);
        }
        catch
        {
            // Test cleanup must not hide the assertion that already ran.
        }
    }
}
