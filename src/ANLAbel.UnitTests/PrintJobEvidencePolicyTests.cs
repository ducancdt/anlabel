using ANLAbel.Core.Printing;
using ANLAbel.Data.PrintLogs;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class PrintJobEvidencePolicyTests
{
    [Theory]
    [InlineData(PrintJobLifecycleState.Preparing, true)]
    [InlineData(PrintJobLifecycleState.PreflightPassed, true)]
    [InlineData(PrintJobLifecycleState.Dispatching, true)]
    [InlineData(PrintJobLifecycleState.SpoolAccepted, false)]
    [InlineData(PrintJobLifecycleState.QueueObserved, false)]
    [InlineData(PrintJobLifecycleState.Failed, false)]
    [InlineData(PrintJobLifecycleState.Cancelled, false)]
    [InlineData(PrintJobLifecycleState.Unknown, false)]
    [InlineData(PrintJobLifecycleState.Completed, false)]
    public void RequiredBeforeDispatchMatchesPreDispatchStates(PrintJobLifecycleState to, bool required)
    {
        Assert.Equal(required, PrintJobEvidencePolicy.IsRequiredBeforeDispatch(to));
    }

    [Fact]
    public void ResolveManifestCountsKeepsExpandedLabelsSeparateFromSourceRows()
    {
        // A×2 + B×1 → 3 labels from 2 source rows. Swapping these breaks reprint checks.
        var (labelCount, sourceRowCount) = PrintJobEvidencePolicy.ResolveManifestCounts(
            expandedLabelCount: 3,
            distinctSourceRowCount: 2);

        Assert.Equal(3, labelCount);
        Assert.Equal(2, sourceRowCount);

        var manifest = PrintJobManifest.Create(
            "Label",
            "label.anlabel",
            "Quick Print",
            "Zebra-01",
            100,
            50,
            203,
            203,
            labelCount,
            sourceRowCount,
            new IReadOnlyDictionary<string, string>?[]
            {
                new Dictionary<string, string> { ["Part"] = "A" },
                new Dictionary<string, string> { ["Part"] = "A" },
                new Dictionary<string, string> { ["Part"] = "B" }
            });

        Assert.Equal(3, manifest.LabelCount);
        Assert.Equal(2, manifest.SourceRowCount);
    }

    [Fact]
    public async Task RequiredPreDispatchAppendFailureBlocksSend()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"anlabel-evidence-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "print-job-events.jsonl");
        try
        {
            // Corrupt tail blocks AppendAsync inside the store (shipped fail-closed path).
            await File.WriteAllTextAsync(path, "{not-json\n");
            var store = new PrintJobStateStore(path);
            var transition = new PrintJobStateTransition(
                Guid.NewGuid().ToString("N"),
                PrintJobLifecycleState.Created,
                PrintJobLifecycleState.Preparing,
                DateTimeOffset.UtcNow,
                "prepare");

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                PrintJobEvidenceAppend.AppendAsync(store, transition));

            Assert.Contains("required job evidence", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            try { Directory.Delete(dir, recursive: true); } catch { /* best-effort */ }
        }
    }

    [Fact]
    public async Task PostDispatchAppendFailureIsBestEffort()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"anlabel-evidence-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "print-job-events.jsonl");
        try
        {
            await File.WriteAllTextAsync(path, "{not-json\n");
            var store = new PrintJobStateStore(path);
            var transition = new PrintJobStateTransition(
                Guid.NewGuid().ToString("N"),
                PrintJobLifecycleState.Dispatching,
                PrintJobLifecycleState.SpoolAccepted,
                DateTimeOffset.UtcNow,
                "spool accepted");

            Assert.False(await PrintJobEvidenceAppend.AppendAsync(store, transition),
                "A dropped post-dispatch event must be reported so the operator can be told.");
        }
        finally
        {
            try { Directory.Delete(dir, recursive: true); } catch { /* best-effort */ }
        }
    }
}
