using ANLAbel.Core.Data;

namespace ANLAbel.Core.Automation.Job;

/// <summary>
/// Immutable specification for one concrete print action derived from a JOB command script.
/// Binds template, printer, variable data record, quantity, copies, and session identity.
/// </summary>
public sealed record JobPrintAction(
    int ActionIndex,
    string TemplatePath,
    string? TargetPrinterName,
    string JobName,
    DataRecord Variables,
    int Quantity,
    int SkipFirstSlots,
    int IdenticalCopies,
    int NumberOfSets,
    IReadOnlyDictionary<string, string> PrintParameters,
    string? PortRedirectPath,
    bool PortAppend,
    string? SessionId)
{
    /// <summary>
    /// Total physical labels this action requests. Computed with saturation to
    /// <see cref="long.MaxValue"/> so a hostile Quantity/IdenticalCopies/NumberOfSets
    /// triple can never silently WRAP (unchecked overflow) into a small positive value
    /// that would sneak past the per-action ceiling guard. On overflow the value clamps
    /// to <see cref="long.MaxValue"/>, guaranteeing the ceiling guard rejects it and the
    /// run fails closed. Normal valid values are unaffected.
    /// </summary>
    public long TotalLabelCount => SaturatingMath.Multiply(
        SaturatingMath.Multiply(Quantity, Math.Max(1, IdenticalCopies)),
        Math.Max(1, NumberOfSets));
}

/// <summary>
/// Overflow-proof integer arithmetic for label-count aggregation. All operations
/// clamp to [<see cref="long.MinValue"/>, <see cref="long.MaxValue"/>] instead of
/// wrapping (unchecked) or throwing (checked/Enumerable.Sum), so untrusted JOB input
/// can never crash the host or wrap into a spuriously valid range.
/// </summary>
internal static class SaturatingMath
{
    public static long Multiply(long a, long b)
    {
        if (a == 0 || b == 0)
        {
            return 0;
        }

        var result = unchecked(a * b);
        // Detect overflow: if dividing back does not reproduce the operand, it wrapped.
        // (Guard the long.MinValue / -1 corner which itself overflows.)
        if ((a == long.MinValue && b == -1) || (b == long.MinValue && a == -1) || result / b != a)
        {
            return ((a > 0) == (b > 0)) ? long.MaxValue : long.MinValue;
        }

        return result;
    }

    public static long Add(long a, long b)
    {
        var result = unchecked(a + b);
        // Overflow occurred iff operands share a sign that differs from the result's sign.
        if (((a ^ result) & (b ^ result)) < 0)
        {
            return a > 0 ? long.MaxValue : long.MinValue;
        }

        return result;
    }
}

public sealed record JobFileAction(
    FileOperationAction Action,
    string TargetPath,
    string? Content);

/// <summary>
/// Compiled execution plan representing all actions, files, and diagnostics of a JOB script.
/// </summary>
public sealed record JobExecutionPlan(
    IReadOnlyList<JobPrintAction> Actions,
    IReadOnlyList<JobFileAction> FileActions,
    IReadOnlyList<string> Diagnostics,
    bool IgnoreErrors)
{
    /// <summary>
    /// Sum of all per-action label counts, computed with saturation instead of
    /// <see cref="Enumerable.Sum(IEnumerable{long})"/> (which is CHECKED and throws
    /// <see cref="OverflowException"/>). A crafted multi-action JOB whose total exceeds
    /// <see cref="long.MaxValue"/> therefore clamps to <see cref="long.MaxValue"/> rather
    /// than crashing the host, letting the per-job ceiling guard fail closed gracefully.
    /// </summary>
    public long TotalLabelsToPrint
    {
        get
        {
            var total = 0L;
            foreach (var action in Actions)
            {
                total = SaturatingMath.Add(total, action.TotalLabelCount);
            }

            return total;
        }
    }

    public bool HasErrors => Diagnostics.Count > 0 && !IgnoreErrors;
}

/// <summary>
/// Compiles an AST of parsed IJobCommands into a validated, deterministic JobExecutionPlan.
/// Simulates variable states, counter stepping, session aggregation, and printer overrides.
/// </summary>
public static class JobExecutionPlanner
{
    public static JobExecutionPlan Build(IReadOnlyList<IJobCommand> commands)
    {
        ArgumentNullException.ThrowIfNull(commands);

        var actions = new List<JobPrintAction>();
        var fileActions = new List<JobFileAction>();
        var diagnostics = new List<string>();

        var currentTemplate = string.Empty;
        string? currentPrinter = null;
        var currentJobName = "ANLAbel Automation Job";
        string? portRedirect = null;
        var portAppend = false;
        var ignoreErrors = false;

        var variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var printParams = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var counters = new Dictionary<string, CounterState>(StringComparer.OrdinalIgnoreCase);

        var inSession = false;
        string? currentSessionId = null;
        var actionIndex = 0;

        foreach (var cmd in commands)
        {
            switch (cmd)
            {
                case CommentCommand:
                    // Comments are non-executable
                    break;

                case LabelOpenCommand labelCmd:
                    currentTemplate = labelCmd.TemplatePath;
                    if (!string.IsNullOrWhiteSpace(labelCmd.OverridePrinterName))
                    {
                        currentPrinter = labelCmd.OverridePrinterName;
                    }
                    break;

                case PrinterOverrideCommand printerCmd:
                    currentPrinter = printerCmd.PrinterName;
                    break;

                case SetVariableCommand setCmd:
                    variables[setCmd.VariableName] = setCmd.Value;
                    if (setCmd.Step != 0)
                    {
                        if (long.TryParse(setCmd.Value, out var initialVal))
                        {
                            counters[setCmd.VariableName] = new CounterState(
                                initialVal,
                                setCmd.Step,
                                Math.Max(1, setCmd.Repetitions),
                                CurrentRepetition: 0);
                        }
                    }
                    else
                    {
                        counters.Remove(setCmd.VariableName);
                    }
                    break;

                case ClearVariablesCommand:
                    variables.Clear();
                    counters.Clear();
                    break;

                case PrintJobNameCommand jobNameCmd:
                    currentJobName = jobNameCmd.JobName;
                    break;

                case SetPrintParamCommand paramCmd:
                    printParams[paramCmd.ParamName] = paramCmd.ParamValue;
                    break;

                case PortRedirectCommand portCmd:
                    portRedirect = portCmd.OutputPath;
                    portAppend = portCmd.Append;
                    break;

                case IgnoreErrorCommand ignoreCmd:
                    ignoreErrors = ignoreCmd.IgnoreErrors;
                    break;

                case FileOperationCommand fileCmd:
                    fileActions.Add(new JobFileAction(fileCmd.Action, fileCmd.TargetPath, fileCmd.Content));
                    break;

                case SessionControlCommand sessionCmd:
                    switch (sessionCmd.Action)
                    {
                        case SessionControlAction.Start:
                            inSession = true;
                            currentSessionId = Guid.NewGuid().ToString("N");
                            break;

                        case SessionControlAction.Print:
                            if (!inSession)
                            {
                                diagnostics.Add($"Line {sessionCmd.LineNumber}: SESSIONPRINT command without active SESSIONSTART.");
                                break;
                            }
                            if (string.IsNullOrWhiteSpace(currentTemplate))
                            {
                                diagnostics.Add($"Line {sessionCmd.LineNumber}: SESSIONPRINT command without prior LABEL command.");
                                break;
                            }

                            actionIndex++;
                            actions.Add(CreateAction(
                                actionIndex,
                                currentTemplate,
                                currentPrinter,
                                currentJobName,
                                variables,
                                counters,
                                sessionCmd.Quantity,
                                sessionCmd.SkipFirstSlots,
                                identicalCopies: 1,
                                numberOfSets: 1,
                                printParams,
                                portRedirect,
                                portAppend,
                                currentSessionId));
                            break;

                        case SessionControlAction.End:
                            inSession = false;
                            currentSessionId = null;
                            break;
                    }
                    break;

                case PrintCommand printCmd:
                    if (string.IsNullOrWhiteSpace(currentTemplate))
                    {
                        diagnostics.Add($"Line {printCmd.LineNumber}: PRINT command without prior LABEL command.");
                        break;
                    }

                    actionIndex++;
                    actions.Add(CreateAction(
                        actionIndex,
                        currentTemplate,
                        currentPrinter,
                        currentJobName,
                        variables,
                        counters,
                        printCmd.Quantity,
                        printCmd.SkipFirstSlots,
                        printCmd.IdenticalCopies,
                        printCmd.NumberOfSets,
                        printParams,
                        portRedirect,
                        portAppend,
                        sessionId: null));
                    break;

                case TextQualifierCommand:
                    // Qualifier handled at parser level
                    break;
            }
        }

        if (inSession)
        {
            diagnostics.Add("JOB command stream ended without closing SESSIONEND.");
        }

        return new JobExecutionPlan(actions, fileActions, diagnostics, ignoreErrors);
    }

    private static JobPrintAction CreateAction(
        int actionIndex,
        string templatePath,
        string? printerName,
        string jobName,
        Dictionary<string, string> variables,
        Dictionary<string, CounterState> counters,
        int quantity,
        int skipFirstSlots,
        int identicalCopies,
        int numberOfSets,
        Dictionary<string, string> printParams,
        string? portRedirect,
        bool portAppend,
        string? sessionId)
    {
        // Snapshot current variable states into a DataRecord
        var dataRecord = DataRecord.Create(variables.Select(p => new KeyValuePair<string, string?>(p.Key, p.Value)));

        // Advance counters for the next action
        var counterKeys = counters.Keys.ToArray();
        foreach (var key in counterKeys)
        {
            var counter = counters[key];
            var newReps = counter.CurrentRepetition + quantity;
            var stepsToAdd = newReps / counter.Repetitions;
            var remainingReps = newReps % counter.Repetitions;
            var newCurrentValue = counter.CurrentValue + (stepsToAdd * counter.Step);

            counters[key] = counter with
            {
                CurrentValue = newCurrentValue,
                CurrentRepetition = remainingReps
            };
            variables[key] = newCurrentValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        return new JobPrintAction(
            actionIndex,
            templatePath,
            printerName,
            jobName,
            dataRecord,
            quantity,
            skipFirstSlots,
            identicalCopies,
            numberOfSets,
            new Dictionary<string, string>(printParams, StringComparer.OrdinalIgnoreCase),
            portRedirect,
            portAppend,
            sessionId);
    }

    private sealed record CounterState(
        long CurrentValue,
        int Step,
        int Repetitions,
        int CurrentRepetition);
}
