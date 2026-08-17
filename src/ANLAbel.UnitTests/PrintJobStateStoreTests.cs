using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ANLAbel.Core.Printing;
using ANLAbel.Data.PrintLogs;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class PrintJobStateStoreTests
{
    [Fact]
    public async Task AppendsSequencedHashChainedEventsAndRecoversAfterReopen()
    {
        var directory = CreateDirectory();
        var path = Path.Combine(directory, "events.jsonl");
        const string jobId = "job-001";

        try
        {
            var store = new PrintJobStateStore(path);
            var first = await store.AppendAsync(Transition(jobId, PrintJobLifecycleState.Created, PrintJobLifecycleState.Preparing, "open"));
            var second = await store.AppendAsync(Transition(jobId, PrintJobLifecycleState.Preparing, PrintJobLifecycleState.PreflightPassed, "preflight ok"));
            var third = await store.AppendAsync(Transition(jobId, PrintJobLifecycleState.PreflightPassed, PrintJobLifecycleState.Dispatching, "dispatch"));

            Assert.Equal(1, first.Sequence);
            Assert.Equal(2, second.Sequence);
            Assert.Equal(3, third.Sequence);
            Assert.NotEqual(first.IntegrityHash, second.IntegrityHash);
            Assert.Equal(first.IntegrityHash, second.PreviousHash);
            Assert.Equal(second.IntegrityHash, third.PreviousHash);
            Assert.Equal(PrintJobLifecycleState.Dispatching, store.GetCurrentState(jobId));

            var reopened = new PrintJobStateStore(path);
            Assert.Equal(PrintJobLifecycleState.Dispatching, reopened.GetCurrentState(jobId));
            var events = await reopened.ReadEventsAsync(jobId);
            Assert.Equal(3, events.Count);
            Assert.Empty(reopened.RecoveryDiagnostics);
        }
        finally
        {
            TryDelete(directory);
        }
    }

    [Fact]
    public async Task RejectsInvalidTransitionAndDoesNotWriteAnEvent()
    {
        var directory = CreateDirectory();
        var path = Path.Combine(directory, "events.jsonl");

        try
        {
            var store = new PrintJobStateStore(path);
            await Assert.ThrowsAsync<InvalidOperationException>(() => store.AppendAsync(
                Transition("job-002", PrintJobLifecycleState.Dispatching, PrintJobLifecycleState.SpoolAccepted, "missing prefix")));
            Assert.False(File.Exists(path));

            await store.AppendAsync(Transition("job-002", PrintJobLifecycleState.Created, PrintJobLifecycleState.Preparing, "open"));
            await Assert.ThrowsAsync<InvalidOperationException>(() => store.AppendAsync(
                Transition("job-002", PrintJobLifecycleState.Created, PrintJobLifecycleState.Dispatching, "stale writer")));
            Assert.Single(await store.ReadEventsAsync("job-002"));
        }
        finally
        {
            TryDelete(directory);
        }
    }

    [Fact]
    public async Task CompletedRequiresExplicitPhysicalEvidence()
    {
        var directory = CreateDirectory();
        var path = Path.Combine(directory, "events.jsonl");

        try
        {
            var store = new PrintJobStateStore(path);
            var manifest = CreateManifest("job-003");
            var verification = PhysicalOutputVerificationEvidence.Create(
                "job-003",
                manifest.Fingerprint,
                PhysicalVerificationMethod.Scanner,
                PhysicalVerificationOutcome.Pass,
                "payload-003",
                "payload-003",
                "scanner-test");
            await store.AppendAsync(Transition("job-003", PrintJobLifecycleState.Created, PrintJobLifecycleState.Preparing, "open"));
            await store.AppendAsync(Transition("job-003", PrintJobLifecycleState.Preparing, PrintJobLifecycleState.PreflightPassed, "preflight ok"));
            await store.AppendAsync(Transition("job-003", PrintJobLifecycleState.PreflightPassed, PrintJobLifecycleState.Dispatching, "dispatch"));
            await store.AppendAsync(Transition("job-003", PrintJobLifecycleState.Dispatching, PrintJobLifecycleState.SpoolAccepted, "spool accepted"));
            await store.AppendAsync(Transition("job-003", PrintJobLifecycleState.SpoolAccepted, PrintJobLifecycleState.QueueObserved, "queue completed", queueState: "Completed"));

            await Assert.ThrowsAsync<InvalidOperationException>(() => store.AppendAsync(
                Transition("job-003", PrintJobLifecycleState.QueueObserved, PrintJobLifecycleState.Completed, "queue completed", queueState: "Completed")));

            var completed = await store.AppendAsync(Transition(
                "job-003",
                PrintJobLifecycleState.QueueObserved,
                PrintJobLifecycleState.Completed,
                "verified by device adapter",
                queueState: "Completed",
                physicalOutputVerified: true,
                manifest: manifest,
                verificationEvidence: verification));
            Assert.True(completed.PhysicalOutputVerified);
            Assert.Equal(verification.Fingerprint, completed.VerificationEvidence!.Fingerprint);
            Assert.Equal(PrintJobLifecycleState.Completed, store.GetCurrentState("job-003"));
        }
        finally
        {
            TryDelete(directory);
        }
    }

    [Fact]
    public async Task CorruptTailStopsReplayAndBlocksFurtherAppend()
    {
        var directory = CreateDirectory();
        var path = Path.Combine(directory, "events.jsonl");

        try
        {
            var store = new PrintJobStateStore(path);
            await store.AppendAsync(Transition("job-004", PrintJobLifecycleState.Created, PrintJobLifecycleState.Preparing, "open"));
            await File.AppendAllTextAsync(path, "{not-json}\n");

            var reopened = new PrintJobStateStore(path);
            Assert.Equal(PrintJobLifecycleState.Preparing, reopened.GetCurrentState("job-004"));
            Assert.NotEmpty(reopened.RecoveryDiagnostics);
            await Assert.ThrowsAsync<InvalidOperationException>(() => reopened.AppendAsync(
                Transition("job-004", PrintJobLifecycleState.Preparing, PrintJobLifecycleState.PreflightPassed, "must stop on corruption")));
        }
        finally
        {
            TryDelete(directory);
        }
    }

    [Fact]
    public async Task SeparateStoreInstancesRefreshSequenceBeforeAppending()
    {
        var directory = CreateDirectory();
        var path = Path.Combine(directory, "events.jsonl");

        try
        {
            var firstStore = new PrintJobStateStore(path);
            var secondStore = new PrintJobStateStore(path);
            await firstStore.AppendAsync(Transition("job-005", PrintJobLifecycleState.Created, PrintJobLifecycleState.Preparing, "first writer"));
            var secondEvent = await secondStore.AppendAsync(Transition("job-005", PrintJobLifecycleState.Preparing, PrintJobLifecycleState.PreflightPassed, "second writer"));

            Assert.Equal(2, secondEvent.Sequence);
            Assert.Equal(PrintJobLifecycleState.PreflightPassed, secondStore.GetCurrentState("job-005"));
            Assert.Equal(2, (await secondStore.ReadEventsAsync("job-005")).Count);
        }
        finally
        {
            TryDelete(directory);
        }
    }

    [Fact]
    public async Task PersistsTextResourceFingerprintAsPartOfDurableEvidence()
    {
        var directory = CreateDirectory();
        var path = Path.Combine(directory, "events.jsonl");
        const string fingerprint = "FONT-RESOURCE-ABC";

        try
        {
            var store = new PrintJobStateStore(path);
            var stateEvent = await store.AppendAsync(Transition(
                "job-text-resource",
                PrintJobLifecycleState.Created,
                PrintJobLifecycleState.Preparing,
                "prepare",
                textResourceFingerprint: fingerprint));

            Assert.Equal(fingerprint, stateEvent.TextResourceFingerprint);
            var reopened = new PrintJobStateStore(path);
            var recovered = Assert.Single(await reopened.ReadEventsAsync("job-text-resource"));
            Assert.Equal(fingerprint, recovered.TextResourceFingerprint);
            Assert.Empty(reopened.RecoveryDiagnostics);
        }
        finally
        {
            TryDelete(directory);
        }
    }

    [Fact]
    public async Task PersistsManifestFingerprintAsPartOfDurableEvidence()
    {
        var directory = CreateDirectory();
        var path = Path.Combine(directory, "events.jsonl");
        const string fingerprint = "MANIFEST-ABC";

        try
        {
            var store = new PrintJobStateStore(path);
            var stateEvent = await store.AppendAsync(Transition(
                "job-manifest",
                PrintJobLifecycleState.Created,
                PrintJobLifecycleState.Preparing,
                "prepare",
                manifestFingerprint: fingerprint));

            Assert.Equal(fingerprint, stateEvent.ManifestFingerprint);
            var reopened = new PrintJobStateStore(path);
            var recovered = Assert.Single(await reopened.ReadEventsAsync("job-manifest"));
            Assert.Equal(fingerprint, recovered.ManifestFingerprint);
            Assert.Empty(reopened.RecoveryDiagnostics);
        }
        finally
        {
            TryDelete(directory);
        }
    }

    [Fact]
    public async Task ReplaysLegacyEventHashAfterOperatorLineageSchemaExtension()
    {
        var directory = CreateDirectory();
        var path = Path.Combine(directory, "events.jsonl");

        try
        {
            var timestamp = DateTimeOffset.UtcNow;
            var legacy = new PrintJobStateEvent(
                "job-legacy",
                1,
                PrintJobLifecycleState.Created,
                PrintJobLifecycleState.Preparing,
                timestamp,
                "open",
                "Test Queue",
                17,
                string.Empty,
                "doc-hash",
                "scene-hash",
                "contract-hash",
                false,
                string.Empty,
                string.Empty);
            legacy = legacy with { IntegrityHash = LegacyHash(legacy) };
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(legacy) + Environment.NewLine);

            var reopened = new PrintJobStateStore(path);
            Assert.Equal(PrintJobLifecycleState.Preparing, reopened.GetCurrentState("job-legacy"));
            Assert.Empty(reopened.RecoveryDiagnostics);

            var next = await reopened.AppendAsync(Transition(
                "job-legacy",
                PrintJobLifecycleState.Preparing,
                PrintJobLifecycleState.PreflightPassed,
                "preflight"));
            Assert.Equal(2, next.Sequence);
            Assert.Equal(legacy.IntegrityHash, next.PreviousHash);
        }
        finally
        {
            TryDelete(directory);
        }
    }

    private static string LegacyHash(PrintJobStateEvent stateEvent)
    {
        var canonical = string.Join("|", new[]
        {
            stateEvent.JobId,
            stateEvent.Sequence.ToString(System.Globalization.CultureInfo.InvariantCulture),
            stateEvent.From.ToString(),
            stateEvent.To.ToString(),
            stateEvent.TimestampUtc.ToUniversalTime().ToString("O", System.Globalization.CultureInfo.InvariantCulture),
            stateEvent.Reason,
            stateEvent.PrinterName,
            stateEvent.SpoolJobId?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
            stateEvent.QueueState,
            stateEvent.DocumentHash,
            stateEvent.SceneHash,
            stateEvent.OutputContractHash,
            stateEvent.PhysicalOutputVerified ? "1" : "0",
            stateEvent.PreviousHash
        });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }

    private static PrintJobStateTransition Transition(
        string jobId,
        PrintJobLifecycleState from,
        PrintJobLifecycleState to,
        string reason,
        string queueState = "",
        bool physicalOutputVerified = false,
        string textResourceFingerprint = "",
        string manifestFingerprint = "",
        PrintJobManifest? manifest = null,
        PhysicalOutputVerificationEvidence? verificationEvidence = null)
    {
        return new PrintJobStateTransition(
            jobId,
            from,
            to,
            DateTimeOffset.UtcNow,
            reason,
            PrinterName: "Test Queue",
            SpoolJobId: 17,
            QueueState: queueState,
            DocumentHash: "doc-hash",
            SceneHash: "scene-hash",
            OutputContractHash: "contract-hash",
            PhysicalOutputVerified: physicalOutputVerified,
            TextResourceFingerprint: textResourceFingerprint,
            ManifestFingerprint: manifest?.Fingerprint ?? manifestFingerprint,
            Manifest: manifest,
            VerificationEvidence: verificationEvidence);
    }

    private static PrintJobManifest CreateManifest(string jobId)
    {
        return PrintJobManifest.Create(
            "State label",
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
