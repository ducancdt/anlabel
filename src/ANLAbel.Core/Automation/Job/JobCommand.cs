namespace ANLAbel.Core.Automation.Job;

/// <summary>
/// Common contract for all parsed NiceLabel JOB command file directives.
/// Commands are immutable, line-indexed records representing top-to-bottom execution intent.
/// </summary>
public interface IJobCommand
{
    int LineNumber { get; }
    string RawLine { get; }
}

public enum SessionControlAction
{
    Start,
    Print,
    End
}

public enum FileOperationAction
{
    Create,
    Delete
}

/// <summary>Comment line starting with semicolon (;).</summary>
public sealed record CommentCommand(int LineNumber, string RawLine, string Comment) : IJobCommand;

/// <summary>
/// LABEL &lt;label name&gt; [, &lt;printer_name&gt;]
/// Opens a label template. An optional printer name overrides the template's saved printer.
/// </summary>
public sealed record LabelOpenCommand(
    int LineNumber,
    string RawLine,
    string TemplatePath,
    string? OverridePrinterName = null) : IJobCommand;

/// <summary>
/// PRINTER &lt;printer name&gt;
/// Overrides the target printer queue for subsequent label prints.
/// </summary>
public sealed record PrinterOverrideCommand(
    int LineNumber,
    string RawLine,
    string PrinterName) : IJobCommand;

/// <summary>
/// SET &lt;name&gt; = &lt;value&gt; [, &lt;step&gt; [, &lt;number of repetitions&gt;]]
/// Assigns a value to a label variable. Supports counter step and repetitions.
/// Multi-line values use \r\n escape sequences.
/// </summary>
public sealed record SetVariableCommand(
    int LineNumber,
    string RawLine,
    string VariableName,
    string Value,
    int Step = 0,
    int Repetitions = 0) : IJobCommand;

/// <summary>
/// CLEARVARIABLEVALUES
/// Resets all variable values to their default states.
/// </summary>
public sealed record ClearVariablesCommand(int LineNumber, string RawLine) : IJobCommand;

/// <summary>
/// PRINT &lt;quantity&gt; [, &lt;skip&gt; [, &lt;identical label copies&gt; [, &lt;number of label sets&gt;]]]
/// Triggers print generation for the currently active label and variable context.
/// </summary>
public sealed record PrintCommand(
    int LineNumber,
    string RawLine,
    int Quantity,
    int SkipFirstSlots = 0,
    int IdenticalCopies = 1,
    int NumberOfSets = 1) : IJobCommand;

/// <summary>
/// PRINTJOBNAME &lt;name&gt;
/// Specifies the display name of the print job in Windows Spooler.
/// </summary>
public sealed record PrintJobNameCommand(
    int LineNumber,
    string RawLine,
    string JobName) : IJobCommand;

/// <summary>
/// SETPRINTPARAM &lt;paramname&gt; = &lt;value&gt;
/// Fine-tunes target printer parameters: PRINTSPEED, PRINTDARKNESS, PRINTOFFSETX, PRINTOFFSETY, PAPERBIN, PRINTERSETTINGS.
/// </summary>
public sealed record SetPrintParamCommand(
    int LineNumber,
    string RawLine,
    string ParamName,
    string ParamValue) : IJobCommand;

/// <summary>
/// SESSIONSTART / SESSIONPRINT &lt;quantity&gt; [, &lt;skip&gt;] / SESSIONEND
/// Controls multi-label session printing stream, joining multiple labels into a single spool job.
/// </summary>
public sealed record SessionControlCommand(
    int LineNumber,
    string RawLine,
    SessionControlAction Action,
    int Quantity = 1,
    int SkipFirstSlots = 0) : IJobCommand;

/// <summary>
/// PORT &lt;file name&gt; [, APPEND]
/// Redirects printer output stream to a designated file.
/// </summary>
public sealed record PortRedirectCommand(
    int LineNumber,
    string RawLine,
    string OutputPath,
    bool Append = false) : IJobCommand;

/// <summary>
/// IGNOREERROR &lt;on | off&gt;
/// Controls whether non-fatal JOB execution errors terminate processing.
/// </summary>
public sealed record IgnoreErrorCommand(
    int LineNumber,
    string RawLine,
    bool IgnoreErrors) : IJobCommand;

/// <summary>
/// CREATEFILE &lt;file name&gt; [, &lt;contents&gt;] / DELETEFILE &lt;file name&gt;
/// Inter-process handshake signaling files for MES/WMS coordination.
/// </summary>
public sealed record FileOperationCommand(
    int LineNumber,
    string RawLine,
    FileOperationAction Action,
    string TargetPath,
    string? Content = null) : IJobCommand;

/// <summary>
/// TEXTQUALIFIER &lt;character&gt;
/// Specifies the text qualifier character embedding variable values (default is double quote ").
/// </summary>
public sealed record TextQualifierCommand(
    int LineNumber,
    string RawLine,
    char Qualifier) : IJobCommand;
