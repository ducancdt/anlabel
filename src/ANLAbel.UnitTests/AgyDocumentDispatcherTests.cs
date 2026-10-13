using System.Diagnostics;
using AgyDocumentDispatcher;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class AgyDocumentDispatcherTests
{
    private const string FixtureProgram = """
        # 13 — Fixture program

        ## 4. Lộ trình

        - [ ] Roadmap prose is not a work item.

        ## 9. Checklist thực thi (nguồn việc duy nhất)

        - [x] 0.1 Already done.
        - [ ] 0.4 Một nguồn đường dẫn dữ liệu app
        - [ ] F01 Trạng thái "chưa lưu"
        - [ ] F02 "Lưu thành…" và phím tắt

        ## 10. After the checklist

        - [ ] Not part of section 9.
        """;

    [Fact]
    public void ListSourceDocuments_OnRealRepo_ReturnsOnlyTheLiveProgram()
    {
        var repo = FindRepoRoot();

        var source = Assert.Single(DocumentWorkQueue.ListSourceDocuments(repo));

        Assert.EndsWith("13-ke-hoach-cai-to-loi.md", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Load_OnRealRepo_NeverQueuesHistoryOrHandoffDocuments()
    {
        var repo = FindRepoRoot();
        var program = Path.Combine(repo, DocumentWorkQueue.LiveProgramRelativePath.Replace('/', Path.DirectorySeparatorChar));
        var lines = File.ReadAllLines(program);
        var checklistLine = Array.FindIndex(lines, line => line.StartsWith(DocumentWorkQueue.LiveChecklistHeadingPrefix, StringComparison.Ordinal)) + 1;
        Assert.True(checklistLine > 0, "The live program lost its section 9 checklist.");

        Assert.All(DocumentWorkQueue.Load(repo), item =>
        {
            Assert.Equal(Path.GetFullPath(program), item.SourcePath, StringComparer.OrdinalIgnoreCase);
            Assert.True(item.LineNumber > checklistLine, $"Item at line {item.LineNumber} is outside section 9.");
            Assert.StartsWith("- [ ]", item.VerbatimPhrase, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void ExtractFromFile_WithSection_ReturnsOnlyOpenItemsOfThatSection()
    {
        using var fixture = new FixtureRepo(FixtureProgram);

        var items = DocumentWorkQueue.ExtractFromFile(fixture.ProgramPath, fixture.Root, DocumentWorkQueue.LiveChecklistHeadingPrefix);

        Assert.Equal(
            new[] { "- [ ] 0.4 Một nguồn đường dẫn dữ liệu app", "- [ ] F01 Trạng thái \"chưa lưu\"", "- [ ] F02 \"Lưu thành…\" và phím tắt" },
            items.Select(item => item.VerbatimPhrase).ToArray());
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
    public void SelectNext_OnFixtureRepo_AssignmentQuotesItsSourceFile()
    {
        using var fixture = new FixtureRepo(FixtureProgram);

        var next = DocumentWorkQueue.SelectNext(fixture.Root);

        Assert.NotNull(next);
        Assert.Equal("- [ ] 0.4 Một nguồn đường dẫn dữ liệu app", next!.VerbatimPhrase);
        var sourceText = File.ReadAllText(next.SourcePath);
        Assert.Contains(next.VerbatimPhrase, sourceText, StringComparison.Ordinal);
        Assert.Contains(next.VerbatimPhrase, next.AssignmentText, StringComparison.Ordinal);
        Assert.Contains(DocumentWorkQueue.LiveProgramRelativePath, next.AssignmentText, StringComparison.Ordinal);
    }

    [Fact]
    public void Assemble_UsesAgyInSameGitToplevel_WithoutWorktreeSwitch()
    {
        using var fixture = new FixtureRepo(FixtureProgram);
        var next = DocumentWorkQueue.SelectNext(fixture.Root);
        Assert.NotNull(next);

        var invocation = AgyWorkerAssembler.Assemble(fixture.Root, next!);
        var toplevel = GitToplevel.Resolve(fixture.Root);

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
        using var fixture = new FixtureRepo(FixtureProgram);

        var cycles = AgyDispatchLoop.Run(new DispatchOptions(fixture.Root, MaxCycles: 2, DryRun: true));

        Assert.Equal(2, cycles.Count);
        Assert.Equal(1, cycles[0].CycleNumber);
        Assert.Equal(2, cycles[1].CycleNumber);
        Assert.NotEqual(cycles[0].Invocation.Assignment.Id, cycles[1].Invocation.Assignment.Id);
        Assert.All(cycles, cycle =>
        {
            Assert.False(cycle.ExecutedLive);
            Assert.Contains("agy", Path.GetFileName(cycle.Invocation.FileName), StringComparison.OrdinalIgnoreCase);
            Assert.Equal(GitToplevel.Resolve(fixture.Root), cycle.Invocation.WorkingDirectory, StringComparer.OrdinalIgnoreCase);
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
        using var fixture = new FixtureRepo(FixtureProgram);
        var all = DocumentWorkQueue.Load(fixture.Root);
        Assert.Equal(3, all.Count);
        var target = all[^1];

        var cycles = AgyDispatchLoop.Run(new DispatchOptions(
            fixture.Root, MaxCycles: 1, DryRun: true, StartAt: target.VerbatimPhrase));

        var cycle = Assert.Single(cycles);
        Assert.Equal(target.Id, cycle.Invocation.Assignment.Id);
    }

    [Fact]
    public void DispatchLoop_StartAt_NoMatch_Throws()
    {
        using var fixture = new FixtureRepo(FixtureProgram);
        Assert.Throws<InvalidOperationException>(() =>
            AgyDispatchLoop.Run(new DispatchOptions(
                fixture.Root, MaxCycles: 1, DryRun: true, StartAt: "no-such-work-item-xyzzy")));
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
            var marker = Path.Combine(directory.FullName, "AGENTS.md");
            var git = Path.Combine(directory.FullName, ".git");
            if (File.Exists(marker) && (Directory.Exists(git) || File.Exists(git)))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate ANLABEL repo root from the test directory.");
    }

    /// <summary>A throwaway git repository holding only a live-program document.</summary>
    private sealed class FixtureRepo : IDisposable
    {
        public FixtureRepo(string programText)
        {
            Root = Path.Combine(Path.GetTempPath(), "agy-fixture-" + Guid.NewGuid().ToString("N"));
            ProgramPath = Path.Combine(Root, DocumentWorkQueue.LiveProgramRelativePath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(ProgramPath)!);
            File.WriteAllText(ProgramPath, programText);
            var init = Process.Start(new ProcessStartInfo("git", "init -q")
            {
                WorkingDirectory = Root,
                UseShellExecute = false,
                CreateNoWindow = true,
            }) ?? throw new InvalidOperationException("git init could not start.");
            init.WaitForExit();
            if (init.ExitCode != 0)
            {
                throw new InvalidOperationException("git init failed for the dispatcher fixture.");
            }
        }

        public string Root { get; }

        public string ProgramPath { get; }

        public void Dispose()
        {
            try
            {
                foreach (var file in Directory.EnumerateFiles(Root, "*", SearchOption.AllDirectories))
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                }

                Directory.Delete(Root, recursive: true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Temp cleanup is best effort.
            }
        }
    }
}
