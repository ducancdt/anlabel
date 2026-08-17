using System.Diagnostics;
using ANLAbel.Core.Printing;
using ANLAbel.Data.PrintLogs;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class PrintJobRecoveryServiceTests
{
    [Fact]
    public async Task PendingSpoolIdentityRequiresQueueReconciliation()
    {
        var directory = CreateDirectory();
        var path = Path.Combine(directory, "events.jsonl");

        try
        {
            var store = new PrintJobStateStore(path);
            await AppendDispatchPrefixAsync(store, "job-reconcile", spoolJobId: 77, printerName: "Zebra Test");

            var report = await PrintJobRecoveryService.LoadAsync(store);

            var candidate = Assert.Single(report.Candidates);
            Assert.Equal(PrintJobLifecycleState.SpoolAccepted, candidate.State);
            Assert.Equal(PrintJobRecoveryAction.ReconcileQueue, candidate.Action);
            Assert.False(candidate.AutomaticRetryAllowed);
            Assert.False(report.AutomaticRetryAllowed);
            Assert.Contains("reconciliation", report.UserFacingSummary, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            TryDelete(directory);
        }
    }

    [Fact]
    public async Task TerminalQueueEvidenceStillRequiresOperatorDecision()
    {
        var directory = CreateDirectory();
        var path = Path.Combine(directory, "events.jsonl");

        try
        {
            var store = new PrintJobStateStore(path);
            await AppendDispatchPrefixAsync(store, "job-terminal-queue", spoolJobId: 78, printerName: "Zebra Test");
            await store.AppendAsync(Transition(
                "job-terminal-queue",
                PrintJobLifecycleState.SpoolAccepted,
                PrintJobLifecycleState.QueueObserved,
                "queue removed the job",
                printerName: "Zebra Test",
                spoolJobId: 78,
                queueState: SpoolJobState.Completed.ToString()));

            var report = await PrintJobRecoveryService.LoadAsync(store);

            var candidate = Assert.Single(report.Candidates);
            Assert.Equal(PrintJobRecoveryAction.OperatorDecision, candidate.Action);
            Assert.Contains("physical", candidate.Reason, StringComparison.OrdinalIgnoreCase);
            Assert.False(candidate.AutomaticRetryAllowed);
        }
        finally
        {
            TryDelete(directory);
        }
    }

    [Fact]
    public async Task MissingSpoolIdentityRequiresOperatorDecision()
    {
        var directory = CreateDirectory();
        var path = Path.Combine(directory, "events.jsonl");

        try
        {
            var store = new PrintJobStateStore(path);
            await AppendDispatchPrefixAsync(store, "job-no-identity", spoolJobId: null, printerName: "");

            var report = await PrintJobRecoveryService.LoadAsync(store);

            var candidate = Assert.Single(report.Candidates);
            Assert.Equal(PrintJobRecoveryAction.OperatorDecision, candidate.Action);
            Assert.Contains("identity", candidate.Reason, StringComparison.OrdinalIgnoreCase);
            Assert.False(report.AutomaticRetryAllowed);
        }
        finally
        {
            TryDelete(directory);
        }
    }

    [Fact]
    public void CorruptStoreDiagnosticsOverrideAnyRetryClassification()
    {
        var snapshot = new PrintJobRecoverySnapshot(
            new[]
            {
                new PrintJobStateEvent(
                    "job-corrupt",
                    3,
                    PrintJobLifecycleState.SpoolAccepted,
                    PrintJobLifecycleState.QueueObserved,
                    DateTimeOffset.UtcNow,
                    "queue observation",
                    "Zebra Test",
                    79,
                    SpoolJobState.Printing.ToString(),
                    "doc",
                    "scene",
                    "contract",
                    false,
                    "previous",
                    "hash")
            },
            new[] { "Ignored malformed print-job event tail" });

        var report = PrintJobRecoveryService.Analyze(snapshot);

        var candidate = Assert.Single(report.Candidates);
        Assert.Equal(PrintJobRecoveryAction.RepairEventLog, candidate.Action);
        Assert.True(report.RequiresRepair);
        Assert.False(report.AutomaticRetryAllowed);
        Assert.Contains("repair", report.UserFacingSummary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TerminalJobsAreNotRecoveryCandidates()
    {
        var directory = CreateDirectory();
        var path = Path.Combine(directory, "events.jsonl");

        try
        {
            var store = new PrintJobStateStore(path);
            var manifest = CreateManifest("job-complete");
            var verification = PhysicalOutputVerificationEvidence.Create(
                "job-complete",
                manifest.Fingerprint,
                PhysicalVerificationMethod.BarcodeVerifier,
                PhysicalVerificationOutcome.Pass,
                "payload-complete",
                "payload-complete",
                "verifier-test",
                grade: "A");
            await AppendDispatchPrefixAsync(store, "job-complete", spoolJobId: 80, printerName: "Zebra Test", manifest: manifest);
            await store.AppendAsync(Transition(
                "job-complete",
                PrintJobLifecycleState.SpoolAccepted,
                PrintJobLifecycleState.QueueObserved,
                "verified by device",
                printerName: "Zebra Test",
                spoolJobId: 80,
                queueState: SpoolJobState.Completed.ToString(),
                manifest: manifest));
            await store.AppendAsync(Transition(
                "job-complete",
                PrintJobLifecycleState.QueueObserved,
                PrintJobLifecycleState.Completed,
                "verified by device",
                printerName: "Zebra Test",
                spoolJobId: 80,
                queueState: SpoolJobState.Completed.ToString(),
                physicalOutputVerified: true,
                manifest: manifest,
                verificationEvidence: verification));

            var report = await PrintJobRecoveryService.LoadAsync(store);

            Assert.Empty(report.Candidates);
            Assert.False(report.HasPendingJobs);
            Assert.Equal("No print jobs need reconciliation.", report.UserFacingSummary);
        }
        finally
        {
            TryDelete(directory);
        }
    }

    [Fact]
    public async Task QueueRequeryReturnsEvidenceWithoutAuthorizingRetry()
    {
        var candidate = Candidate(PrintJobRecoveryAction.ReconcileQueue, spoolJobId: 81, queueState: "");
        var reader = new SequenceReader(new SpoolJobObservation(
            "Zebra Test",
            81,
            SpoolJobState.Printing,
            "driver reports printing",
            IsTerminal: true,
            ObservedAtUtc: DateTimeOffset.UtcNow));

        var result = await PrintJobRecoveryService.ReconcileQueueAsync(
            candidate,
            reader,
            timeout: TimeSpan.FromSeconds(1),
            pollInterval: TimeSpan.Zero);

        Assert.Equal(PrintJobReconciliationOutcome.QueueObserved, result.Outcome);
        Assert.Equal(SpoolJobState.Printing, result.QueueResult!.FinalObservation.State);
        Assert.True(result.RequiresOperatorDecision);
        Assert.False(result.AutomaticRetryAllowed);
        Assert.False(result.PhysicalOutputVerified);
    }

    [Fact]
    public async Task QueueTerminalStateRequiresOperatorDecision()
    {
        var candidate = Candidate(PrintJobRecoveryAction.ReconcileQueue, spoolJobId: 82, queueState: "");
        var reader = new SequenceReader(new SpoolJobObservation(
            "Zebra Test",
            82,
            SpoolJobState.Completed,
            "queue completed",
            IsTerminal: true,
            ObservedAtUtc: DateTimeOffset.UtcNow));

        var result = await PrintJobRecoveryService.ReconcileQueueAsync(candidate, reader);

        Assert.Equal(PrintJobReconciliationOutcome.OperatorDecisionRequired, result.Outcome);
        Assert.Contains("Physical output is not verified", result.Summary, StringComparison.Ordinal);
        Assert.False(result.AutomaticRetryAllowed);
    }

    [Theory]
    [InlineData(SpoolJobState.Error)]
    [InlineData(SpoolJobState.Offline)]
    [InlineData(SpoolJobState.PaperOut)]
    [InlineData(SpoolJobState.UserIntervention)]
    [InlineData(SpoolJobState.Blocked)]
    [InlineData(SpoolJobState.Paused)]
    [InlineData(SpoolJobState.Retained)]
    public async Task QueueFaultStatesRequireOperatorDecision(
        SpoolJobState faultState)
    {
        var candidate = Candidate(PrintJobRecoveryAction.ReconcileQueue, spoolJobId: 87, queueState: "");
        var reader = new SequenceReader(new SpoolJobObservation(
            "Zebra Test",
            87,
            faultState,
            "driver fault requires operator action",
            IsTerminal: true,
            ObservedAtUtc: DateTimeOffset.UtcNow));

        var result = await PrintJobRecoveryService.ReconcileQueueAsync(
            candidate,
            reader,
            timeout: TimeSpan.FromSeconds(1),
            pollInterval: TimeSpan.Zero);

        Assert.Equal(PrintJobReconciliationOutcome.OperatorDecisionRequired, result.Outcome);
        Assert.Contains("Physical output is not verified", result.Summary, StringComparison.Ordinal);
        Assert.False(result.AutomaticRetryAllowed);
        Assert.False(result.PhysicalOutputVerified);
    }

    [Theory]
    [InlineData(SpoolJobState.Error)]
    [InlineData(SpoolJobState.Offline)]
    [InlineData(SpoolJobState.PaperOut)]
    [InlineData(SpoolJobState.UserIntervention)]
    [InlineData(SpoolJobState.Blocked)]
    [InlineData(SpoolJobState.Paused)]
    [InlineData(SpoolJobState.Retained)]
    public void PersistedQueueFaultStateRequiresOperatorDecision(SpoolJobState faultState)
    {
        var snapshot = new PrintJobRecoverySnapshot(
            new[]
            {
                new PrintJobStateEvent(
                    "job-fault-state",
                    4,
                    PrintJobLifecycleState.SpoolAccepted,
                    PrintJobLifecycleState.QueueObserved,
                    DateTimeOffset.UtcNow,
                    "queue fault",
                    "Zebra Test",
                    88,
                    faultState.ToString(),
                    "doc",
                    "scene",
                    "contract",
                    false,
                    "previous",
                    "hash")
            },
            Array.Empty<string>());

        var report = PrintJobRecoveryService.Analyze(snapshot);

        var candidate = Assert.Single(report.Candidates);
        Assert.Equal(PrintJobRecoveryAction.OperatorDecision, candidate.Action);
        Assert.Contains("terminal", candidate.Reason, StringComparison.OrdinalIgnoreCase);
        Assert.False(candidate.AutomaticRetryAllowed);
    }

    [Fact]
    public async Task QueueRequeryTimeoutIsBoundedAndFailClosed()
    {
        var candidate = Candidate(PrintJobRecoveryAction.ReconcileQueue, spoolJobId: 83, queueState: "");
        var started = Stopwatch.GetTimestamp();

        var result = await PrintJobRecoveryService.ReconcileQueueAsync(
            candidate,
            new HangingReader(),
            timeout: TimeSpan.FromMilliseconds(40),
            pollInterval: TimeSpan.Zero);

        var elapsed = Stopwatch.GetElapsedTime(started);
        Assert.Equal(PrintJobReconciliationOutcome.TimedOut, result.Outcome);
        Assert.True(result.QueueResult!.TimedOut);
        Assert.False(result.AutomaticRetryAllowed);
        Assert.True(elapsed < TimeSpan.FromSeconds(1), $"Queue re-query exceeded bound: {elapsed}");
    }

    [Fact]
    public async Task QueueIdentityMismatchCannotBeUsedForReconciliation()
    {
        var candidate = Candidate(PrintJobRecoveryAction.ReconcileQueue, spoolJobId: 84, queueState: "");
        var reader = new SequenceReader(new SpoolJobObservation(
            "Other Queue",
            999,
            SpoolJobState.Printing,
            "wrong identity",
            IsTerminal: true,
            ObservedAtUtc: DateTimeOffset.UtcNow));

        var result = await PrintJobRecoveryService.ReconcileQueueAsync(candidate, reader);

        Assert.Equal(PrintJobReconciliationOutcome.OperatorDecisionRequired, result.Outcome);
        Assert.Equal(SpoolJobState.Unknown, result.QueueResult!.FinalObservation.State);
        Assert.Contains("identity", result.QueueResult.FinalObservation.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(result.AutomaticRetryAllowed);
    }

    [Fact]
    public async Task SpoolerRestartFaultProducesOperatorDecisionWithoutRetry()
    {
        var candidate = Candidate(PrintJobRecoveryAction.ReconcileQueue, spoolJobId: 86, queueState: "");

        var result = await PrintJobRecoveryService.ReconcileQueueAsync(
            candidate,
            new SpoolerRestartReader());

        Assert.Equal(PrintJobReconciliationOutcome.OperatorDecisionRequired, result.Outcome);
        Assert.Equal(SpoolJobState.Unknown, result.QueueResult!.FinalObservation.State);
        Assert.True(result.QueueResult.FinalObservation.IsTerminal);
        Assert.Contains("spool-status reader failed", result.QueueResult.FinalObservation.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(result.AutomaticRetryAllowed);
        Assert.False(result.PhysicalOutputVerified);
    }

    [Fact]
    public async Task NonQueueCandidateDoesNotInvokeReader()
    {
        var candidate = Candidate(PrintJobRecoveryAction.OperatorDecision, spoolJobId: 85, queueState: "");
        var reader = new CountingReader();

        var result = await PrintJobRecoveryService.ReconcileQueueAsync(candidate, reader);

        Assert.Equal(PrintJobReconciliationOutcome.InvalidCandidate, result.Outcome);
        Assert.Equal(0, reader.ReadCount);
        Assert.False(result.AutomaticRetryAllowed);
    }

    private static async Task AppendDispatchPrefixAsync(
        PrintJobStateStore store,
        string jobId,
        int? spoolJobId,
        string printerName,
        PrintJobManifest? manifest = null)
    {
        await store.AppendAsync(Transition(jobId, PrintJobLifecycleState.Created, PrintJobLifecycleState.Preparing, "open", printerName, spoolJobId, manifest: manifest));
        await store.AppendAsync(Transition(jobId, PrintJobLifecycleState.Preparing, PrintJobLifecycleState.PreflightPassed, "preflight", printerName, spoolJobId, manifest: manifest));
        await store.AppendAsync(Transition(jobId, PrintJobLifecycleState.PreflightPassed, PrintJobLifecycleState.Dispatching, "dispatch", printerName, spoolJobId, manifest: manifest));
        await store.AppendAsync(Transition(jobId, PrintJobLifecycleState.Dispatching, PrintJobLifecycleState.SpoolAccepted, "spool accepted", printerName, spoolJobId, manifest: manifest));
    }

    private static PrintJobRecoveryCandidate Candidate(
        PrintJobRecoveryAction action,
        int? spoolJobId,
        string queueState)
    {
        return new PrintJobRecoveryCandidate(
            "job-query",
            PrintJobLifecycleState.SpoolAccepted,
            action,
            DateTimeOffset.UtcNow,
            "Zebra Test",
            spoolJobId,
            queueState,
            "doc",
            "scene",
            "contract",
            "candidate reason");
    }

    private sealed class SequenceReader : ISpoolJobStatusReader
    {
        private readonly SpoolJobObservation _observation;

        public SequenceReader(SpoolJobObservation observation)
        {
            _observation = observation;
        }

        public ValueTask<SpoolJobObservation> ReadAsync(
            string printerName,
            int spoolJobId,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(_observation);
        }
    }

    private sealed class CountingReader : ISpoolJobStatusReader
    {
        public int ReadCount { get; private set; }

        public ValueTask<SpoolJobObservation> ReadAsync(
            string printerName,
            int spoolJobId,
            CancellationToken cancellationToken = default)
        {
            ReadCount++;
            return ValueTask.FromResult(new SpoolJobObservation(printerName, spoolJobId, SpoolJobState.Unknown, IsTerminal: true));
        }
    }

    private sealed class HangingReader : ISpoolJobStatusReader
    {
        public async ValueTask<SpoolJobObservation> ReadAsync(
            string printerName,
            int spoolJobId,
            CancellationToken cancellationToken = default)
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            return new SpoolJobObservation(printerName, spoolJobId, SpoolJobState.Printing, IsTerminal: false);
        }
    }

    private sealed class SpoolerRestartReader : ISpoolJobStatusReader
    {
        public ValueTask<SpoolJobObservation> ReadAsync(
            string printerName,
            int spoolJobId,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("The spooler is restarting.");
        }
    }

    private static PrintJobStateTransition Transition(
        string jobId,
        PrintJobLifecycleState from,
        PrintJobLifecycleState to,
        string reason,
        string printerName = "Test Queue",
        int? spoolJobId = 17,
        string queueState = "",
        bool physicalOutputVerified = false,
        PrintJobManifest? manifest = null,
        PhysicalOutputVerificationEvidence? verificationEvidence = null)
    {
        return new PrintJobStateTransition(
            jobId,
            from,
            to,
            DateTimeOffset.UtcNow,
            reason,
            PrinterName: printerName,
            SpoolJobId: spoolJobId,
            QueueState: queueState,
            DocumentHash: "doc-hash",
            SceneHash: "scene-hash",
            OutputContractHash: "contract-hash",
            PhysicalOutputVerified: physicalOutputVerified,
            ManifestFingerprint: manifest?.Fingerprint ?? string.Empty,
            Manifest: manifest,
            VerificationEvidence: verificationEvidence);
    }

    private static PrintJobManifest CreateManifest(string jobId)
    {
        return PrintJobManifest.Create(
            "Recovery label",
            $"{jobId}.anlabel",
            "Print",
            "Zebra Test",
            100,
            50,
            203,
            203,
            1,
            1,
            new IReadOnlyDictionary<string, string>?[]
            {
                new Dictionary<string, string> { ["PartNo"] = jobId }
            },
            documentHash: "doc-hash",
            textResourceFingerprint: "text-hash",
            sceneHash: "scene-hash",
            outputContractHash: "contract-hash");
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
