using System.Text;
using ANLAbel.Core.Printing;
using ANLAbel.Data.PrintLogs;
using Xunit;

namespace ANLAbel.UnitTests;

/// <summary>Plan 13 H18: a damaged job log must not stop printing for good.</summary>
public sealed class PrintJobLogRepairTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "ANLAbel.JobLogRepair." + Guid.NewGuid().ToString("N"));

    public PrintJobLogRepairTests() => Directory.CreateDirectory(_directory);

    public void Dispose()
    {
        try { Directory.Delete(_directory, recursive: true); }
        catch (IOException) { }
    }

    [Fact]
    public async Task A_write_cut_off_by_power_loss_is_archived_and_printing_continues()
    {
        var path = Path.Combine(_directory, "print-job-events.jsonl");
        var store = new PrintJobStateStore(path);
        await store.AppendAsync(Transition("job-a", PrintJobLifecycleState.Created, PrintJobLifecycleState.Preparing));
        var validBytes = File.ReadAllBytes(path);
        await File.AppendAllTextAsync(path, "{\"JobId\":\"job-a\",\"Sequence\":2,\"Fr");   // torn: no newline

        var reopened = new PrintJobStateStore(path);
        var next = await reopened.AppendAsync(Transition("job-a", PrintJobLifecycleState.Preparing, PrintJobLifecycleState.PreflightPassed));

        Assert.Equal(2, next.Sequence);
        Assert.Empty(reopened.RecoveryDiagnostics);
        var notice = Assert.Single(reopened.RepairNotices);
        Assert.Contains("ghi dở", notice, StringComparison.Ordinal);
        var archive = Assert.Single(Directory.GetFiles(_directory, "print-job-events.hong-*.jsonl"));
        Assert.EndsWith("\"Fr", File.ReadAllText(archive), StringComparison.Ordinal);
        Assert.StartsWith(Encoding.UTF8.GetString(validBytes), File.ReadAllText(path), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Damage_inside_the_log_blocks_until_the_operator_archives_it()
    {
        var path = Path.Combine(_directory, "print-job-events.jsonl");
        var store = new PrintJobStateStore(path);
        await store.AppendAsync(Transition("job-b", PrintJobLifecycleState.Created, PrintJobLifecycleState.Preparing));
        await File.AppendAllTextAsync(path, "{not-json}\n");

        var reopened = new PrintJobStateStore(path);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            reopened.AppendAsync(Transition("job-b", PrintJobLifecycleState.Preparing, PrintJobLifecycleState.PreflightPassed)));

        var archive = await reopened.ArchiveDamagedLogAndStartNewAsync();

        Assert.NotNull(archive);
        Assert.Contains("{not-json}", File.ReadAllText(archive!), StringComparison.Ordinal);
        Assert.DoesNotContain("{not-json}", File.ReadAllText(path), StringComparison.Ordinal);
        Assert.Equal(PrintJobLifecycleState.Preparing, new PrintJobStateStore(path).GetCurrentState("job-b"));
        var next = await reopened.AppendAsync(Transition("job-b", PrintJobLifecycleState.Preparing, PrintJobLifecycleState.PreflightPassed));
        Assert.Equal(2, next.Sequence);
        Assert.Contains(reopened.RepairNotices, notice => notice.Contains(Path.GetFileName(archive!), StringComparison.Ordinal));
    }

    [Fact]
    public async Task Archiving_a_healthy_log_does_nothing()
    {
        var path = Path.Combine(_directory, "print-job-events.jsonl");
        var store = new PrintJobStateStore(path);
        await store.AppendAsync(Transition("job-c", PrintJobLifecycleState.Created, PrintJobLifecycleState.Preparing));

        Assert.Null(await store.ArchiveDamagedLogAndStartNewAsync());
        Assert.Empty(Directory.GetFiles(_directory, "*.hong-*"));
    }

    [Fact]
    public async Task Recovery_snapshot_reports_the_automatic_repair()
    {
        var path = Path.Combine(_directory, "print-job-events.jsonl");
        await new PrintJobStateStore(path).AppendAsync(Transition("job-d", PrintJobLifecycleState.Created, PrintJobLifecycleState.Preparing));
        await File.AppendAllTextAsync(path, "{\"JobId\":\"job-d\"");

        var snapshot = await new PrintJobStateStore(path).ReadRecoverySnapshotAsync();

        Assert.Empty(snapshot.StoreDiagnostics);
        Assert.NotNull(snapshot.RepairNotices);
        Assert.Single(snapshot.RepairNotices!);
        Assert.Single(snapshot.LatestEvents);
    }

    [Fact]
    public async Task A_large_settled_log_is_archived_whole_and_a_new_log_starts()
    {
        var path = Path.Combine(_directory, "print-job-events.jsonl");
        var writer = new PrintJobStateStore(path);
        await AppendPath(writer, "job-e", PrintJobLifecycleState.Created, PrintJobLifecycleState.Preparing, PrintJobLifecycleState.Failed);
        await AppendPath(writer, "job-f", PrintJobLifecycleState.Created, PrintJobLifecycleState.Preparing, PrintJobLifecycleState.PreflightPassed,
            PrintJobLifecycleState.Dispatching, PrintJobLifecycleState.SpoolAccepted);
        var original = File.ReadAllBytes(path);

        var store = new PrintJobStateStore(path, settledArchiveThresholdBytes: 1);
        var snapshot = await store.ReadRecoverySnapshotAsync();

        Assert.Empty(snapshot.LatestEvents);
        Assert.Empty(snapshot.RepairNotices ?? Array.Empty<string>());
        var archive = Assert.Single(Directory.GetFiles(_directory, "print-job-events.luu-*.jsonl"));
        Assert.Equal(original, File.ReadAllBytes(archive));
        var next = await store.AppendAsync(Transition("job-g", PrintJobLifecycleState.Created, PrintJobLifecycleState.Preparing));
        Assert.Equal(1, next.Sequence);
        Assert.Single((await store.ReadRecoverySnapshotAsync()).LatestEvents);
        Assert.Single(Directory.GetFiles(_directory, "print-job-events.luu-*.jsonl"));
    }

    [Theory]
    [InlineData(PrintJobLifecycleState.Unknown)]
    [InlineData(PrintJobLifecycleState.Dispatching)]
    [InlineData(PrintJobLifecycleState.Preparing)]
    public async Task A_log_with_an_open_job_is_never_archived(PrintJobLifecycleState lastState)
    {
        var path = Path.Combine(_directory, "print-job-events.jsonl");
        var writer = new PrintJobStateStore(path);
        await AppendPath(writer, "job-h", PrintJobLifecycleState.Created, PrintJobLifecycleState.Preparing, PrintJobLifecycleState.Failed);
        var states = new[]
        {
            PrintJobLifecycleState.Created, PrintJobLifecycleState.Preparing, PrintJobLifecycleState.PreflightPassed,
            PrintJobLifecycleState.Dispatching, PrintJobLifecycleState.Unknown
        };
        await AppendPath(writer, "job-i", states[..(Array.IndexOf(states, lastState) + 1)]);

        var snapshot = await new PrintJobStateStore(path, settledArchiveThresholdBytes: 1).ReadRecoverySnapshotAsync();

        Assert.Equal(2, snapshot.LatestEvents.Count);
        Assert.True(File.Exists(path));
        Assert.Empty(Directory.GetFiles(_directory, "*.luu-*"));
    }

    [Fact]
    public async Task A_preparation_abandoned_days_ago_does_not_keep_the_log_active()
    {
        var path = Path.Combine(_directory, "print-job-events.jsonl");
        var writer = new PrintJobStateStore(path);
        await writer.AppendAsync(Transition("job-k", PrintJobLifecycleState.Created, PrintJobLifecycleState.Preparing)
            with { TimestampUtc = DateTimeOffset.UtcNow.AddDays(-2) });

        var snapshot = await new PrintJobStateStore(path, settledArchiveThresholdBytes: 1).ReadRecoverySnapshotAsync();

        Assert.Empty(snapshot.LatestEvents);
        Assert.Single(Directory.GetFiles(_directory, "print-job-events.luu-*.jsonl"));
    }

    [Fact]
    public async Task A_small_log_is_not_archived()
    {
        var path = Path.Combine(_directory, "print-job-events.jsonl");
        await AppendPath(new PrintJobStateStore(path), "job-j", PrintJobLifecycleState.Created, PrintJobLifecycleState.Preparing, PrintJobLifecycleState.Failed);

        var snapshot = await new PrintJobStateStore(path).ReadRecoverySnapshotAsync();

        Assert.Single(snapshot.LatestEvents);
        Assert.Empty(Directory.GetFiles(_directory, "*.luu-*"));
    }

    private static async Task AppendPath(PrintJobStateStore store, string jobId, params PrintJobLifecycleState[] states)
    {
        for (var index = 1; index < states.Length; index++)
        {
            await store.AppendAsync(Transition(jobId, states[index - 1], states[index]));
        }
    }

    private static PrintJobStateTransition Transition(string jobId, PrintJobLifecycleState from, PrintJobLifecycleState to)
    {
        return new PrintJobStateTransition(
            jobId,
            from,
            to,
            DateTimeOffset.UtcNow,
            "repair test",
            "Test Queue",
            null,
            string.Empty,
            "doc",
            "scene",
            "contract",
            false);
    }
}
