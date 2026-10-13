using System.Diagnostics;
using System.Text;

namespace AgyDocumentDispatcher;

public sealed record DispatchOptions(
    string RepoRoot,
    int MaxCycles,
    bool DryRun,
    string? AgyPath = null,
    bool SkipPermissions = true,
    string? StartAt = null,
    string? PrintTimeout = null);

public sealed record DispatchCycle(
    int CycleNumber,
    AgyWorkerInvocation Invocation,
    bool ExecutedLive,
    string? LiveOutput);

public static class AgyDispatchLoop
{
    public static IReadOnlyList<DispatchCycle> Run(DispatchOptions options)
    {
        if (options.MaxCycles < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "MaxCycles must be at least 1.");
        }

        var toplevel = GitToplevel.Resolve(options.RepoRoot);
        var remaining = DocumentWorkQueue.Load(toplevel).ToList();
        if (!string.IsNullOrWhiteSpace(options.StartAt))
        {
            var startIndex = remaining.FindIndex(item =>
                item.VerbatimPhrase.Contains(options.StartAt, StringComparison.OrdinalIgnoreCase));
            if (startIndex < 0)
            {
                throw new InvalidOperationException(
                    "No open work item matched --start-at \"" + options.StartAt + "\".");
            }

            remaining = remaining.GetRange(startIndex, remaining.Count - startIndex);
        }

        var dispatched = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var cycles = new List<DispatchCycle>();

        for (var cycle = 1; cycle <= options.MaxCycles; cycle++)
        {
            var next = remaining.FirstOrDefault(item => !dispatched.Contains(item.Id));
            if (next is null)
            {
                break;
            }

            var invocation = AgyWorkerAssembler.Assemble(toplevel, next, options.AgyPath);
            string? liveOutput = null;
            var executed = false;
            if (!options.DryRun)
            {
                liveOutput = ExecuteAgy(invocation, options.SkipPermissions, options.PrintTimeout);
                executed = true;
            }

            dispatched.Add(next.Id);
            cycles.Add(new DispatchCycle(cycle, invocation, executed, liveOutput));
        }

        return cycles;
    }

    public static string FormatCycles(IReadOnlyList<DispatchCycle> cycles)
    {
        var sb = new StringBuilder();
        sb.AppendLine("dispatcher=AgyDocumentDispatcher");
        sb.AppendLine("worker=agy");
        sb.AppendLine("cycle_count=" + cycles.Count.ToString(System.Globalization.CultureInfo.InvariantCulture));
        foreach (var cycle in cycles)
        {
            var inv = cycle.Invocation;
            sb.AppendLine();
            sb.AppendLine("=== cycle " + cycle.CycleNumber.ToString(System.Globalization.CultureInfo.InvariantCulture) + " ===");
            sb.AppendLine("git_toplevel=" + inv.WorkingDirectory);
            sb.AppendLine("worker_command=" + inv.FileName);
            sb.AppendLine("source_document=" + inv.Assignment.SourcePath);
            sb.AppendLine("verbatim_phrase=" + inv.Assignment.VerbatimPhrase);
            sb.AppendLine("assembled_argv=" + inv.DisplayCommand);
            sb.AppendLine("dry_run=" + (!cycle.ExecutedLive).ToString().ToLowerInvariant());
            sb.AppendLine("assignment_text_begin");
            sb.AppendLine(inv.Assignment.AssignmentText);
            sb.AppendLine("assignment_text_end");
            if (cycle.ExecutedLive)
            {
                sb.AppendLine("worker_output_begin");
                sb.AppendLine(cycle.LiveOutput ?? string.Empty);
                sb.AppendLine("worker_output_end");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Pure assembly of the agy argv so the flag order/content can be tested
    /// without launching the worker. <paramref name="printTimeout"/> is forwarded
    /// as agy's <c>--print-timeout</c> (e.g. "20m"); agy's own default is 5m.
    /// </summary>
    public static IReadOnlyList<string> BuildAgyArguments(
        AgyWorkerInvocation invocation,
        bool skipPermissions,
        string? printTimeout)
    {
        var args = new List<string>();
        if (skipPermissions)
        {
            args.Add("--dangerously-skip-permissions");
        }

        if (!string.IsNullOrWhiteSpace(printTimeout))
        {
            args.Add("--print-timeout");
            args.Add(printTimeout);
        }

        args.AddRange(invocation.Arguments);
        return args;
    }

    private static string ExecuteAgy(AgyWorkerInvocation invocation, bool skipPermissions, string? printTimeout)
    {
        var args = BuildAgyArguments(invocation, skipPermissions, printTimeout);
        var psi = new ProcessStartInfo
        {
            FileName = invocation.FileName,
            WorkingDirectory = invocation.WorkingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var argument in args)
        {
            psi.ArgumentList.Add(argument);
        }

        using var process = Process.Start(psi)
                            ?? throw new InvalidOperationException("Failed to start agy worker.");
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();
        return stdout + stderr;
    }
}
