using AgyDocumentDispatcher;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class AgyDocumentDispatcherTests
{
    [Fact]
    public void SelectNext_FromRealMasterPlan_ContainsVerbatimOpenCheckbox()
    {
        var repo = FindRepoRoot();
        var documentPath = Path.Combine(repo, "MASTER_PLAN.md");
        Assert.True(File.Exists(documentPath), "MASTER_PLAN.md must exist in the checkout.");

        var documentText = File.ReadAllText(documentPath);
        var distinctiveLine = documentText
            .Split(["\r\n", "\n"], StringSplitOptions.None)
            .Select(line => line.Trim())
            .FirstOrDefault(line => line.Contains("- [ ]", StringComparison.Ordinal)
                                    && line.Contains("M4b", StringComparison.Ordinal));
        Assert.False(string.IsNullOrWhiteSpace(distinctiveLine), "MASTER_PLAN.md lost its open M4b checkbox.");

        var extracted = DocumentWorkQueue.ExtractFromFile(documentPath, repo);
        Assert.NotEmpty(extracted);

        var assignment = Assert.Single(extracted, item =>
            item.VerbatimPhrase.Contains("M4b", StringComparison.Ordinal));
        Assert.False(string.IsNullOrWhiteSpace(assignment.AssignmentText));
        Assert.Contains(distinctiveLine, assignment.VerbatimPhrase, StringComparison.Ordinal);
        Assert.Contains(assignment.VerbatimPhrase, documentText, StringComparison.Ordinal);
        Assert.Contains(assignment.VerbatimPhrase, assignment.AssignmentText, StringComparison.Ordinal);
        Assert.EndsWith("MASTER_PLAN.md", assignment.SourcePath, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ExtractFromFile_SkipsDocumentationOnlyStatusTitle()
    {
        var path = Path.Combine(Path.GetTempPath(), "agy-doconly-" + Guid.NewGuid().ToString("N") + ".md");
        File.WriteAllText(
            path,
            "# ANLAbel — CC-P3 handoff — **Status:** roadmap/design review; documentation-only checkpoint (2026-08-13)\n\nBody text only.\n");
        try
        {
            var items = DocumentWorkQueue.ExtractFromFile(path);
            Assert.Empty(items);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ExtractFromFile_SkipsStandingGovernanceCheckboxes()
    {
        var path = Path.Combine(Path.GetTempPath(), "agy-gov-" + Guid.NewGuid().ToString("N") + ".md");
        File.WriteAllText(
            path,
            "# Handoff\n"
            + "- [ ] Any future command contract is documented with regression coverage before a UI button is enabled.\n"
            + "- [ ] Any UI change has a named Figma node and regression coverage.\n"
            + "- [ ] A clean implementation checkpoint exists; this note is not evidence that the worktree is releasable.\n"
            + "- [ ] Add HRI check-digit toggle to the barcode properties panel.\n");
        try
        {
            var items = DocumentWorkQueue.ExtractFromFile(path);
            var item = Assert.Single(items);
            Assert.Contains("HRI check-digit toggle", item.VerbatimPhrase, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void SelectNext_OnRepoDocuments_AssignmentQuotesItsSourceFile()
    {
        var repo = FindRepoRoot();
        var next = DocumentWorkQueue.SelectNext(repo);
        Assert.NotNull(next);
        Assert.False(string.IsNullOrWhiteSpace(next!.AssignmentText));
        Assert.True(File.Exists(next.SourcePath));

        var sourceText = File.ReadAllText(next.SourcePath);
        Assert.Contains(next.VerbatimPhrase, sourceText, StringComparison.Ordinal);
        Assert.Contains(next.VerbatimPhrase, next.AssignmentText, StringComparison.Ordinal);
    }

    [Fact]
    public void Assemble_UsesAgyInSameGitToplevel_WithoutWorktreeSwitch()
    {
        var repo = FindRepoRoot();
        var next = DocumentWorkQueue.SelectNext(repo);
        Assert.NotNull(next);

        var invocation = AgyWorkerAssembler.Assemble(repo, next!);
        var toplevel = GitToplevel.Resolve(repo);

        Assert.Equal(toplevel, invocation.WorkingDirectory, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("agy", Path.GetFileName(invocation.FileName), StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith("--print=", invocation.Arguments[0], StringComparison.Ordinal);
        Assert.Contains(next!.VerbatimPhrase, invocation.Arguments[0], StringComparison.Ordinal);
        Assert.Contains(next.VerbatimPhrase, invocation.Prompt, StringComparison.Ordinal);
        Assert.DoesNotContain("worktree add", invocation.DisplayCommand, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("isolation_worktree", invocation.Prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DispatchLoop_DryRunTwoCycles_AdvancesRemainingDocumentTasks()
    {
        var repo = FindRepoRoot();
        var cycles = AgyDispatchLoop.Run(new DispatchOptions(repo, MaxCycles: 2, DryRun: true));
        Assert.Equal(2, cycles.Count);
        Assert.Equal(1, cycles[0].CycleNumber);
        Assert.Equal(2, cycles[1].CycleNumber);
        Assert.NotEqual(cycles[0].Invocation.Assignment.Id, cycles[1].Invocation.Assignment.Id);
        Assert.All(cycles, cycle =>
        {
            Assert.False(cycle.ExecutedLive);
            Assert.Contains("agy", Path.GetFileName(cycle.Invocation.FileName), StringComparison.OrdinalIgnoreCase);
            Assert.Equal(GitToplevel.Resolve(repo), cycle.Invocation.WorkingDirectory, StringComparer.OrdinalIgnoreCase);
            var source = File.ReadAllText(cycle.Invocation.Assignment.SourcePath);
            Assert.Contains(cycle.Invocation.Assignment.VerbatimPhrase, source, StringComparison.Ordinal);
        });

        var formatted = AgyDispatchLoop.FormatCycles(cycles);
        Assert.Contains("=== cycle 1 ===", formatted, StringComparison.Ordinal);
        Assert.Contains("=== cycle 2 ===", formatted, StringComparison.Ordinal);
        Assert.Contains("worker_command=", formatted, StringComparison.Ordinal);
        Assert.DoesNotContain("git worktree add", formatted, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DispatchLoop_StartAt_SkipsEarlierItemsAndBeginsAtMatch()
    {
        var repo = FindRepoRoot();
        var all = DocumentWorkQueue.Load(repo);
        Assert.True(all.Count > 1, "Expected more than one open work item in the repo.");
        var target = all[all.Count - 1];

        var cycles = AgyDispatchLoop.Run(new DispatchOptions(
            repo, MaxCycles: 1, DryRun: true, StartAt: target.VerbatimPhrase));

        var cycle = Assert.Single(cycles);
        Assert.Equal(target.Id, cycle.Invocation.Assignment.Id);
    }

    [Fact]
    public void FormatCycles_LiveCycle_IncludesWorkerOutput()
    {
        var item = new DocumentWorkItem("doc.md:1", "doc.md", 1, "- [ ] do the thing", "assignment");
        var invocation = new AgyWorkerInvocation("agy", ["--print=x"], "/repo", "prompt", item);
        var cycle = new DispatchCycle(1, invocation, ExecutedLive: true, LiveOutput: "AGY-SAID-HELLO");

        var formatted = AgyDispatchLoop.FormatCycles([cycle]);

        Assert.Contains("worker_output_begin", formatted, StringComparison.Ordinal);
        Assert.Contains("AGY-SAID-HELLO", formatted, StringComparison.Ordinal);
        Assert.Contains("worker_output_end", formatted, StringComparison.Ordinal);
    }

    [Fact]
    public void FormatCycles_DryRunCycle_OmitsWorkerOutput()
    {
        var item = new DocumentWorkItem("doc.md:1", "doc.md", 1, "- [ ] do the thing", "assignment");
        var invocation = new AgyWorkerInvocation("agy", ["--print=x"], "/repo", "prompt", item);
        var cycle = new DispatchCycle(1, invocation, ExecutedLive: false, LiveOutput: null);

        var formatted = AgyDispatchLoop.FormatCycles([cycle]);

        Assert.DoesNotContain("worker_output_begin", formatted, StringComparison.Ordinal);
    }

    [Fact]
    public void DispatchLoop_StartAt_NoMatch_Throws()
    {
        var repo = FindRepoRoot();
        Assert.Throws<InvalidOperationException>(() =>
            AgyDispatchLoop.Run(new DispatchOptions(
                repo, MaxCycles: 1, DryRun: true, StartAt: "no-such-work-item-xyzzy")));
    }

    [Fact]
    public void BuildAgyArguments_WithPrintTimeout_ForwardsFlagBeforePrompt()
    {
        var item = new DocumentWorkItem("doc.md:1", "doc.md", 1, "- [ ] task", "assignment");
        var invocation = new AgyWorkerInvocation("agy", ["--print=go"], "/repo", "prompt", item);

        var args = AgyDispatchLoop.BuildAgyArguments(invocation, skipPermissions: true, printTimeout: "20m").ToList();

        Assert.Equal("--dangerously-skip-permissions", args[0]);
        var timeoutIndex = args.IndexOf("--print-timeout");
        Assert.True(timeoutIndex >= 0, "Expected --print-timeout to be forwarded.");
        Assert.Equal("20m", args[timeoutIndex + 1]);
        var promptIndex = args.IndexOf("--print=go");
        Assert.True(promptIndex > timeoutIndex, "The prompt must follow the timeout flag.");
    }

    [Fact]
    public void BuildAgyArguments_WithoutPrintTimeout_OmitsFlag()
    {
        var item = new DocumentWorkItem("doc.md:1", "doc.md", 1, "- [ ] task", "assignment");
        var invocation = new AgyWorkerInvocation("agy", ["--print=go"], "/repo", "prompt", item);

        var args = AgyDispatchLoop.BuildAgyArguments(invocation, skipPermissions: false, printTimeout: null).ToList();

        Assert.DoesNotContain("--print-timeout", args);
        Assert.DoesNotContain("--dangerously-skip-permissions", args);
        Assert.Equal("--print=go", args[0]);
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var marker = Path.Combine(directory.FullName, "MASTER_PLAN.md");
            var git = Path.Combine(directory.FullName, ".git");
            if (File.Exists(marker) && (Directory.Exists(git) || File.Exists(git)))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate ANLABEL repo root from the test directory.");
    }
}
