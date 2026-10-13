using System.Text;

namespace ANLAbel.Core.Automation.Job;

public sealed record JobParseDiagnostic(int LineNumber, string Message, bool IsWarning = false);

public sealed record JobParseResult(
    IReadOnlyList<IJobCommand> Commands,
    IReadOnlyList<JobParseDiagnostic> Diagnostics)
{
    public bool HasErrors => Diagnostics.Any(diagnostic => !diagnostic.IsWarning);
}

/// <summary>
/// High-fidelity parser for NiceLabel JOB command files (.job).
/// Conforms to NiceLabel 2019 User Guide Chapter 9.1.1 specifications.
/// </summary>
public static class NiceLabelJobParser
{
    public static JobParseResult Parse(string content)
    {
        ArgumentNullException.ThrowIfNull(content);
        using var reader = new StringReader(content);
        return Parse(reader);
    }

    public static JobParseResult Parse(TextReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var commands = new List<IJobCommand>();
        var diagnostics = new List<JobParseDiagnostic>();
        var textQualifier = '"';
        var lineNumber = 0;

        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            lineNumber++;
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                continue;
            }

            if (trimmed.StartsWith(';'))
            {
                commands.Add(new CommentCommand(lineNumber, line, trimmed[1..].Trim()));
                continue;
            }

            // Extract command verb
            var spaceIndex = trimmed.IndexOf(' ');
            var verb = spaceIndex < 0 ? trimmed : trimmed[..spaceIndex];
            var remainder = spaceIndex < 0 ? string.Empty : trimmed[spaceIndex..].Trim();

            try
            {
                switch (verb.ToUpperInvariant())
                {
                    case "LABEL":
                        ParseLabelCommand(lineNumber, line, remainder, textQualifier, commands, diagnostics);
                        break;

                    case "PRINTER":
                        ParsePrinterCommand(lineNumber, line, remainder, textQualifier, commands, diagnostics);
                        break;

                    case "SET":
                        ParseSetCommand(lineNumber, line, remainder, textQualifier, commands, diagnostics);
                        break;

                    case "CLEARVARIABLEVALUES":
                    case "CLEARVARIABLE":
                        commands.Add(new ClearVariablesCommand(lineNumber, line));
                        break;

                    case "PRINT":
                        ParsePrintCommand(lineNumber, line, remainder, textQualifier, commands, diagnostics);
                        break;

                    case "PRINTJOBNAME":
                    case "JOBNAME":
                        ParsePrintJobNameCommand(lineNumber, line, remainder, textQualifier, commands, diagnostics);
                        break;

                    case "SETPRINTPARAM":
                        ParseSetPrintParamCommand(lineNumber, line, remainder, textQualifier, commands, diagnostics);
                        break;

                    case "SESSIONSTART":
                        commands.Add(new SessionControlCommand(lineNumber, line, SessionControlAction.Start));
                        break;

                    case "SESSIONPRINT":
                        ParseSessionPrintCommand(lineNumber, line, remainder, textQualifier, commands, diagnostics);
                        break;

                    case "SESSIONEND":
                        commands.Add(new SessionControlCommand(lineNumber, line, SessionControlAction.End));
                        break;

                    case "PORT":
                        ParsePortCommand(lineNumber, line, remainder, textQualifier, commands, diagnostics);
                        break;

                    case "IGNOREERROR":
                        ParseIgnoreErrorCommand(lineNumber, line, remainder, commands, diagnostics);
                        break;

                    case "CREATEFILE":
                        ParseCreateFileCommand(lineNumber, line, remainder, textQualifier, commands, diagnostics);
                        break;

                    case "DELETEFILE":
                        ParseDeleteFileCommand(lineNumber, line, remainder, textQualifier, commands, diagnostics);
                        break;

                    case "TEXTQUALIFIER":
                        if (remainder.Length > 0)
                        {
                            textQualifier = remainder[0];
                            commands.Add(new TextQualifierCommand(lineNumber, line, textQualifier));
                        }
                        else
                        {
                            diagnostics.Add(new JobParseDiagnostic(lineNumber, "TEXTQUALIFIER requires a character argument."));
                        }
                        break;

                    default:
                        diagnostics.Add(new JobParseDiagnostic(lineNumber, $"Unrecognized NiceLabel JOB command '{verb}'."));
                        break;
                }
            }
            catch (Exception ex)
            {
                diagnostics.Add(new JobParseDiagnostic(lineNumber, $"Error parsing command '{verb}': {ex.Message}"));
            }
        }

        return new JobParseResult(commands, diagnostics);
    }

    private static void ParseLabelCommand(
        int lineNumber, string line, string remainder, char qualifier,
        List<IJobCommand> commands, List<JobParseDiagnostic> diagnostics)
    {
        var args = SplitArguments(remainder, qualifier);
        if (args.Count == 0 || string.IsNullOrWhiteSpace(args[0]))
        {
            diagnostics.Add(new JobParseDiagnostic(lineNumber, "LABEL command requires a label template file path."));
            return;
        }

        var templatePath = args[0];
        var overridePrinter = args.Count > 1 && !string.IsNullOrWhiteSpace(args[1]) ? args[1] : null;
        commands.Add(new LabelOpenCommand(lineNumber, line, templatePath, overridePrinter));
    }

    private static void ParsePrinterCommand(
        int lineNumber, string line, string remainder, char qualifier,
        List<IJobCommand> commands, List<JobParseDiagnostic> diagnostics)
    {
        var args = SplitArguments(remainder, qualifier);
        if (args.Count == 0 || string.IsNullOrWhiteSpace(args[0]))
        {
            diagnostics.Add(new JobParseDiagnostic(lineNumber, "PRINTER command requires a printer name."));
            return;
        }

        commands.Add(new PrinterOverrideCommand(lineNumber, line, args[0]));
    }

    private static void ParseSetCommand(
        int lineNumber, string line, string remainder, char qualifier,
        List<IJobCommand> commands, List<JobParseDiagnostic> diagnostics)
    {
        // SET name = value [, step [, repetitions]]
        var eqIndex = remainder.IndexOf('=');
        if (eqIndex < 0)
        {
            diagnostics.Add(new JobParseDiagnostic(lineNumber, "SET command requires '=' assignment syntax (SET <name> = <value>)."));
            return;
        }

        var varName = remainder[..eqIndex].Trim();
        if (string.IsNullOrWhiteSpace(varName))
        {
            diagnostics.Add(new JobParseDiagnostic(lineNumber, "SET command variable name cannot be empty."));
            return;
        }

        var rest = remainder[(eqIndex + 1)..].Trim();
        var args = SplitArguments(rest, qualifier);
        if (args.Count == 0)
        {
            commands.Add(new SetVariableCommand(lineNumber, line, varName, string.Empty));
            return;
        }

        var val = args[0];
        var step = 0;
        var reps = 0;

        if (args.Count > 1 && int.TryParse(args[1], out var parsedStep))
        {
            step = parsedStep;
        }

        if (args.Count > 2 && int.TryParse(args[2], out var parsedReps))
        {
            reps = parsedReps;
        }

        commands.Add(new SetVariableCommand(lineNumber, line, varName, val, step, reps));
    }

    private static void ParsePrintCommand(
        int lineNumber, string line, string remainder, char qualifier,
        List<IJobCommand> commands, List<JobParseDiagnostic> diagnostics)
    {
        var args = SplitArguments(remainder, qualifier);
        if (args.Count == 0 || string.IsNullOrWhiteSpace(args[0]))
        {
            diagnostics.Add(new JobParseDiagnostic(lineNumber, "PRINT command requires a quantity argument."));
            return;
        }

        if (!int.TryParse(args[0], out var qty) || qty < 1)
        {
            diagnostics.Add(new JobParseDiagnostic(lineNumber, $"Invalid PRINT quantity '{args[0]}'. Must be a positive integer."));
            return;
        }

        var skip = args.Count > 1 && int.TryParse(args[1], out var s) ? Math.Max(0, s) : 0;
        var copies = args.Count > 2 && int.TryParse(args[2], out var c) ? Math.Max(1, c) : 1;
        var sets = args.Count > 3 && int.TryParse(args[3], out var st) ? Math.Max(1, st) : 1;

        commands.Add(new PrintCommand(lineNumber, line, qty, skip, copies, sets));
    }

    private static void ParsePrintJobNameCommand(
        int lineNumber, string line, string remainder, char qualifier,
        List<IJobCommand> commands, List<JobParseDiagnostic> diagnostics)
    {
        var args = SplitArguments(remainder, qualifier);
        if (args.Count == 0 || string.IsNullOrWhiteSpace(args[0]))
        {
            diagnostics.Add(new JobParseDiagnostic(lineNumber, "PRINTJOBNAME command requires a name."));
            return;
        }

        commands.Add(new PrintJobNameCommand(lineNumber, line, args[0]));
    }

    private static void ParseSetPrintParamCommand(
        int lineNumber, string line, string remainder, char qualifier,
        List<IJobCommand> commands, List<JobParseDiagnostic> diagnostics)
    {
        var eqIndex = remainder.IndexOf('=');
        if (eqIndex < 0)
        {
            diagnostics.Add(new JobParseDiagnostic(lineNumber, "SETPRINTPARAM command requires '=' assignment syntax."));
            return;
        }

        var paramName = remainder[..eqIndex].Trim();
        var rest = remainder[(eqIndex + 1)..].Trim();
        var args = SplitArguments(rest, qualifier);
        var paramVal = args.Count > 0 ? args[0] : string.Empty;

        commands.Add(new SetPrintParamCommand(lineNumber, line, paramName, paramVal));
    }

    private static void ParseSessionPrintCommand(
        int lineNumber, string line, string remainder, char qualifier,
        List<IJobCommand> commands, List<JobParseDiagnostic> diagnostics)
    {
        var args = SplitArguments(remainder, qualifier);
        var qty = 1;
        var skip = 0;

        if (args.Count > 0 && int.TryParse(args[0], out var q))
        {
            qty = Math.Max(1, q);
        }

        if (args.Count > 1 && int.TryParse(args[1], out var s))
        {
            skip = Math.Max(0, s);
        }

        commands.Add(new SessionControlCommand(lineNumber, line, SessionControlAction.Print, qty, skip));
    }

    private static void ParsePortCommand(
        int lineNumber, string line, string remainder, char qualifier,
        List<IJobCommand> commands, List<JobParseDiagnostic> diagnostics)
    {
        var args = SplitArguments(remainder, qualifier);
        if (args.Count == 0 || string.IsNullOrWhiteSpace(args[0]))
        {
            diagnostics.Add(new JobParseDiagnostic(lineNumber, "PORT command requires an output path."));
            return;
        }

        var append = args.Count > 1 && string.Equals(args[1], "APPEND", StringComparison.OrdinalIgnoreCase);
        commands.Add(new PortRedirectCommand(lineNumber, line, args[0], append));
    }

    private static void ParseIgnoreErrorCommand(
        int lineNumber, string line, string remainder,
        List<IJobCommand> commands, List<JobParseDiagnostic> diagnostics)
    {
        var flag = remainder.Trim();
        if (string.Equals(flag, "ON", StringComparison.OrdinalIgnoreCase))
        {
            commands.Add(new IgnoreErrorCommand(lineNumber, line, true));
        }
        else if (string.Equals(flag, "OFF", StringComparison.OrdinalIgnoreCase))
        {
            commands.Add(new IgnoreErrorCommand(lineNumber, line, false));
        }
        else
        {
            diagnostics.Add(new JobParseDiagnostic(lineNumber, $"IGNOREERROR requires 'on' or 'off', got '{remainder}'."));
        }
    }

    private static void ParseCreateFileCommand(
        int lineNumber, string line, string remainder, char qualifier,
        List<IJobCommand> commands, List<JobParseDiagnostic> diagnostics)
    {
        var args = SplitArguments(remainder, qualifier);
        if (args.Count == 0 || string.IsNullOrWhiteSpace(args[0]))
        {
            diagnostics.Add(new JobParseDiagnostic(lineNumber, "CREATEFILE requires a file path."));
            return;
        }

        var content = args.Count > 1 ? args[1] : null;
        commands.Add(new FileOperationCommand(lineNumber, line, FileOperationAction.Create, args[0], content));
    }

    private static void ParseDeleteFileCommand(
        int lineNumber, string line, string remainder, char qualifier,
        List<IJobCommand> commands, List<JobParseDiagnostic> diagnostics)
    {
        var args = SplitArguments(remainder, qualifier);
        if (args.Count == 0 || string.IsNullOrWhiteSpace(args[0]))
        {
            diagnostics.Add(new JobParseDiagnostic(lineNumber, "DELETEFILE requires a file path."));
            return;
        }

        commands.Add(new FileOperationCommand(lineNumber, line, FileOperationAction.Delete, args[0]));
    }

    /// <summary>
    /// Parses comma-separated arguments while respecting quotes and escape sequences.
    /// Handles \r, \n, \t, \\, and escaped quotes.
    /// </summary>
    public static IReadOnlyList<string> SplitArguments(string input, char qualifier = '"')
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return Array.Empty<string>();
        }

        var result = new List<string>();
        var sb = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < input.Length; i++)
        {
            var c = input[i];

            if (c == '\\' && i + 1 < input.Length)
            {
                var next = input[i + 1];
                if (next == 'r') { sb.Append('\r'); i++; continue; }
                if (next == 'n') { sb.Append('\n'); i++; continue; }
                if (next == 't') { sb.Append('\t'); i++; continue; }
                if (next == '\\') { sb.Append('\\'); i++; continue; }
                if (next == qualifier) { sb.Append(qualifier); i++; continue; }
            }

            if (c == qualifier)
            {
                if (!inQuotes)
                {
                    // Opening quote: discard any whitespace accumulated before the quote
                    if (sb.ToString().Trim().Length == 0)
                    {
                        sb.Clear();
                    }
                    inQuotes = true;
                }
                else
                {
                    inQuotes = false;
                }
                continue;
            }

            if (c == ',' && !inQuotes)
            {
                result.Add(sb.ToString().Trim());
                sb.Clear();
                continue;
            }

            sb.Append(c);
        }

        result.Add(sb.ToString().Trim());
        return result;
    }
}
