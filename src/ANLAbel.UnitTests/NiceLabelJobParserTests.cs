using ANLAbel.Core.Automation.Job;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class NiceLabelJobParserTests
{
    [Fact]
    public void Parse_OfficialNiceLabelExample_Succeeds()
    {
        // Example directly from NiceLabel 2019 User Guide section 9.1.1.2
        const string script = """
            ; NiceLabel 2019 User Guide sample
            LABEL "label2.nlbl"
            SET code="12345"
            SET article="FUSILLI"
            SET ean="383860026501"
            SET weight="1,0 kg"
            PRINT 1
            """;

        var result = NiceLabelJobParser.Parse(script);

        Assert.False(result.HasErrors);
        Assert.Equal(7, result.Commands.Count);

        Assert.IsType<CommentCommand>(result.Commands[0]);

        var labelCmd = Assert.IsType<LabelOpenCommand>(result.Commands[1]);
        Assert.Equal("label2.nlbl", labelCmd.TemplatePath);
        Assert.Null(labelCmd.OverridePrinterName);

        var setCode = Assert.IsType<SetVariableCommand>(result.Commands[2]);
        Assert.Equal("code", setCode.VariableName);
        Assert.Equal("12345", setCode.Value);

        var setWeight = Assert.IsType<SetVariableCommand>(result.Commands[5]);
        Assert.Equal("weight", setWeight.VariableName);
        Assert.Equal("1,0 kg", setWeight.Value);

        var printCmd = Assert.IsType<PrintCommand>(result.Commands[6]);
        Assert.Equal(1, printCmd.Quantity);

        // Plan execution
        var plan = JobExecutionPlanner.Build(result.Commands);
        Assert.False(plan.HasErrors);
        Assert.Single(plan.Actions);

        var action = plan.Actions[0];
        Assert.Equal("label2.nlbl", action.TemplatePath);
        Assert.Equal(1, action.Quantity);
        Assert.Equal("12345", action.Variables.Values["code"]);
        Assert.Equal("FUSILLI", action.Variables.Values["article"]);
        Assert.Equal("383860026501", action.Variables.Values["ean"]);
        Assert.Equal("1,0 kg", action.Variables.Values["weight"]);
    }

    [Fact]
    public void Parse_AllCommandsAndEscapes_Succeeds()
    {
        const string script = """
            ; Setup
            LABEL "C:\Templates\shipping.anlabel", "Zebra ZD420"
            PRINTER "Datamax E-4203"
            PRINTJOBNAME "Batch-408"
            SETPRINTPARAM PRINTSPEED = 4
            SETPRINTPARAM PRINTDARKNESS = 15
            PORT "C:\Spool\output.prn", APPEND
            IGNOREERROR on
            CREATEFILE "C:\Flags\start.txt", "STARTED"

            ; Variables
            SET customer = "ACME Corp, Inc."
            SET address = "Line 1\r\nLine 2"
            SET counter = 100, 5, 2

            PRINT 10, 1, 2, 1

            CLEARVARIABLEVALUES
            DELETEFILE "C:\Flags\start.txt"
            """;

        var result = NiceLabelJobParser.Parse(script);
        Assert.False(result.HasErrors);

        var plan = JobExecutionPlanner.Build(result.Commands);
        Assert.False(plan.HasErrors);
        Assert.Single(plan.Actions);

        var action = plan.Actions[0];
        Assert.Equal(@"C:\Templates\shipping.anlabel", action.TemplatePath);
        Assert.Equal("Datamax E-4203", action.TargetPrinterName);
        Assert.Equal("Batch-408", action.JobName);
        Assert.Equal(@"C:\Spool\output.prn", action.PortRedirectPath);
        Assert.True(action.PortAppend);

        Assert.Equal("ACME Corp, Inc.", action.Variables.Values["customer"]);
        Assert.Equal("Line 1\r\nLine 2", action.Variables.Values["address"]);
        Assert.Equal("100", action.Variables.Values["counter"]);

        Assert.Equal(10, action.Quantity);
        Assert.Equal(1, action.SkipFirstSlots);
        Assert.Equal(2, action.IdenticalCopies);
        Assert.Equal(20, action.TotalLabelCount);

        Assert.Equal("4", action.PrintParameters["PRINTSPEED"]);
        Assert.Equal("15", action.PrintParameters["PRINTDARKNESS"]);

        Assert.Equal(2, plan.FileActions.Count);
        Assert.Equal(FileOperationAction.Create, plan.FileActions[0].Action);
        Assert.Equal("STARTED", plan.FileActions[0].Content);
        Assert.Equal(FileOperationAction.Delete, plan.FileActions[1].Action);
    }

    [Fact]
    public void SessionPrint_AggregatesUnderSingleSession()
    {
        const string script = """
            LABEL "template_a.anlabel"
            PRINTER "Zebra ZD420"
            SET part = "BOLT-M8"

            SESSIONSTART
            SESSIONPRINT 2
            SET part = "NUT-M8"
            SESSIONPRINT 3
            SESSIONEND
            """;

        var result = NiceLabelJobParser.Parse(script);
        Assert.False(result.HasErrors);

        var plan = JobExecutionPlanner.Build(result.Commands);
        Assert.False(plan.HasErrors);
        Assert.Equal(2, plan.Actions.Count);

        // Both actions must share the exact same non-empty SessionId
        Assert.NotNull(plan.Actions[0].SessionId);
        Assert.Equal(plan.Actions[0].SessionId, plan.Actions[1].SessionId);

        Assert.Equal("BOLT-M8", plan.Actions[0].Variables.Values["part"]);
        Assert.Equal(2, plan.Actions[0].Quantity);

        Assert.Equal("NUT-M8", plan.Actions[1].Variables.Values["part"]);
        Assert.Equal(3, plan.Actions[1].Quantity);
        Assert.Equal(5, plan.TotalLabelsToPrint);
    }

    [Fact]
    public void Counter_StepsAcrossPrints_Deterministically()
    {
        const string script = """
            LABEL "box.anlabel"
            SET serial = 1000, 10, 1
            PRINT 1
            PRINT 1
            PRINT 1
            """;

        var result = NiceLabelJobParser.Parse(script);
        var plan = JobExecutionPlanner.Build(result.Commands);

        Assert.Equal(3, plan.Actions.Count);
        Assert.Equal("1000", plan.Actions[0].Variables.Values["serial"]);
        Assert.Equal("1010", plan.Actions[1].Variables.Values["serial"]);
        Assert.Equal("1020", plan.Actions[2].Variables.Values["serial"]);
    }

    [Fact]
    public void PrintWithoutLabel_ReportsDiagnostic()
    {
        const string script = """
            SET title = "No Template"
            PRINT 1
            """;

        var result = NiceLabelJobParser.Parse(script);
        var plan = JobExecutionPlanner.Build(result.Commands);

        Assert.True(plan.HasErrors);
        Assert.Contains(plan.Diagnostics, d => d.Contains("PRINT command without prior LABEL"));
    }

    [Fact]
    public void UnclosedSession_ReportsDiagnostic()
    {
        const string script = """
            LABEL "test.anlabel"
            SESSIONSTART
            SESSIONPRINT 1
            """;

        var result = NiceLabelJobParser.Parse(script);
        var plan = JobExecutionPlanner.Build(result.Commands);

        Assert.True(plan.HasErrors);
        Assert.Contains(plan.Diagnostics, d => d.Contains("SESSIONEND"));
    }

    [Fact]
    public void SplitArguments_HandlesQuotesAndCommasCorrectly()
    {
        var args = NiceLabelJobParser.SplitArguments(@"item1, ""item 2, with comma"", ""item \""escaped\""""");
        Assert.Equal(3, args.Count);
        Assert.Equal("item1", args[0]);
        Assert.Equal("item 2, with comma", args[1]);
        Assert.Equal(@"item ""escaped""", args[2]);
    }

    [Fact]
    public void Counter_WithRepetitions_StepsAccordingToNiceLabelSpec()
    {
        // 100 with step +5 every 2 repetitions
        const string script = """
            LABEL "box.anlabel"
            SET counter = 100, 5, 2
            PRINT 1
            PRINT 1
            PRINT 1
            PRINT 1
            """;

        var result = NiceLabelJobParser.Parse(script);
        var plan = JobExecutionPlanner.Build(result.Commands);

        Assert.Equal(4, plan.Actions.Count);
        // Print 1: counter starts at 100 (rep 1 of 2)
        Assert.Equal("100", plan.Actions[0].Variables.Values["counter"]);
        // Print 2: still 100 (rep 2 of 2)
        Assert.Equal("100", plan.Actions[1].Variables.Values["counter"]);
        // Print 3: steps by 5 -> 105 (rep 1 of 2)
        Assert.Equal("105", plan.Actions[2].Variables.Values["counter"]);
        // Print 4: still 105 (rep 2 of 2)
        Assert.Equal("105", plan.Actions[3].Variables.Values["counter"]);
    }

    [Fact]
    public void ClearVariable_ClearsPriorVariablesAndCounters()
    {
        const string script = """
            LABEL "test.anlabel"
            SET var1 = "Alpha"
            SET var2 = "Beta"
            PRINT 1
            CLEARVARIABLE
            SET var3 = "Gamma"
            PRINT 1
            """;

        var result = NiceLabelJobParser.Parse(script);
        var plan = JobExecutionPlanner.Build(result.Commands);

        Assert.Equal(2, plan.Actions.Count);
        Assert.Equal("Alpha", plan.Actions[0].Variables.Values["var1"]);
        Assert.Equal("Beta", plan.Actions[0].Variables.Values["var2"]);

        // Second print should NOT contain var1 or var2
        Assert.False(plan.Actions[1].Variables.Values.ContainsKey("var1"));
        Assert.False(plan.Actions[1].Variables.Values.ContainsKey("var2"));
        Assert.Equal("Gamma", plan.Actions[1].Variables.Values["var3"]);
    }

    [Fact]
    public void JobName_And_PrinterOverride_CapturedInAction()
    {
        const string script = """
            LABEL "test.anlabel"
            PRINTER "Zebra Industrial 300"
            JOBNAME "Batch-PO-9921"
            PRINT 5
            """;

        var result = NiceLabelJobParser.Parse(script);
        var plan = JobExecutionPlanner.Build(result.Commands);

        Assert.Single(plan.Actions);
        var action = plan.Actions[0];
        Assert.Equal("Zebra Industrial 300", action.TargetPrinterName);
        Assert.Equal("Batch-PO-9921", action.JobName);
        Assert.Equal(5, action.Quantity);
    }

    [Fact]
    public void PortRedirection_And_FileOperations_PlannedCorrectly()
    {
        const string script = """
            LABEL "test.anlabel"
            PORT "C:\\temp\\spool.prn", APPEND
            CREATEFILE "flag.ready", "PRINT_INITIATED"
            PRINT 2
            DELETEFILE "flag.ready"
            """;

        var result = NiceLabelJobParser.Parse(script);
        var plan = JobExecutionPlanner.Build(result.Commands);

        Assert.Single(plan.Actions);
        Assert.Equal(@"C:\temp\spool.prn", plan.Actions[0].PortRedirectPath);
        Assert.True(plan.Actions[0].PortAppend);

        Assert.Equal(2, plan.FileActions.Count);
        Assert.Equal(FileOperationAction.Create, plan.FileActions[0].Action);
        Assert.Equal("flag.ready", plan.FileActions[0].TargetPath);
        Assert.Equal("PRINT_INITIATED", plan.FileActions[0].Content);

        Assert.Equal(FileOperationAction.Delete, plan.FileActions[1].Action);
        Assert.Equal("flag.ready", plan.FileActions[1].TargetPath);
    }

    [Fact]
    public void UnknownDirective_IsHardParseError()
    {
        // FIX H1: an unrecognized directive must surface as a hard (non-warning) parse error
        // so the runner halts instead of silently dropping the command.
        const string script = """
            LABEL "test.anlabel"
            FOOBAR something
            PRINT 1
            """;

        var result = NiceLabelJobParser.Parse(script);

        Assert.True(result.HasErrors);
        Assert.Contains(result.Diagnostics, d => !d.IsWarning && d.Message.Contains("Unrecognized"));
    }

    [Fact]
    public void IgnoreErrors_SetsFlag()
    {
        const string script = """
            IGNOREERROR on
            LABEL "missing.anlabel"
            PRINT 1
            """;

        var result = NiceLabelJobParser.Parse(script);
        var plan = JobExecutionPlanner.Build(result.Commands);

        Assert.True(plan.IgnoreErrors);
    }
}

