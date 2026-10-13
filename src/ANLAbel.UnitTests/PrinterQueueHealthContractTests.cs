using ANLAbel.Core.Printing;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class PrinterQueueHealthContractTests
{
    [Fact]
    public void ReadyMeansNoKnownBlockingSignalNotPhysicalProof()
    {
        var result = Evaluate();

        Assert.Equal(PrinterQueueHealthState.Ready, result.State);
        Assert.True(result.CanSubmit);
        Assert.Contains("not physical-output verification", result.Diagnostic, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(false, false, false, true, false, false, PrinterQueueHealthState.PaperJam)]
    [InlineData(false, false, true, false, false, false, PrinterQueueHealthState.PaperOut)]
    [InlineData(false, true, false, false, false, false, PrinterQueueHealthState.Offline)]
    [InlineData(false, false, false, false, true, false, PrinterQueueHealthState.UserIntervention)]
    [InlineData(true, false, false, false, false, false, PrinterQueueHealthState.Paused)]
    [InlineData(false, false, false, false, false, true, PrinterQueueHealthState.Error)]
    public void BlockingSignalsFailClosed(
        bool paused,
        bool offline,
        bool paperOut,
        bool paperJam,
        bool intervention,
        bool error,
        PrinterQueueHealthState expected)
    {
        var result = Evaluate(paused, offline, paperOut, paperJam, intervention, error);

        Assert.Equal(expected, result.State);
        Assert.False(result.CanSubmit);
        Assert.Contains("no label was submitted", result.Diagnostic, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UnreadableHealthIsUnknownAndCannotSubmit()
    {
        var result = PrinterQueueHealthContract.Evaluate(
            readSucceeded: false,
            false, false, false, false, false, false,
            readDiagnostic: "spooler restarting");

        Assert.Equal(PrinterQueueHealthState.Unknown, result.State);
        Assert.False(result.CanSubmit);
        Assert.Contains("spooler restarting", result.Diagnostic, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SpecificMediaFaultWinsOverGenericErrorFlag()
    {
        var result = Evaluate(paperOut: true, error: true);

        Assert.Equal(PrinterQueueHealthState.PaperOut, result.State);
    }

    private static PrinterQueueHealthObservation Evaluate(
        bool paused = false,
        bool offline = false,
        bool paperOut = false,
        bool paperJam = false,
        bool intervention = false,
        bool error = false) =>
        PrinterQueueHealthContract.Evaluate(
            readSucceeded: true,
            paused,
            offline,
            paperOut,
            paperJam,
            intervention,
            error,
            observedAtUtc: DateTimeOffset.UnixEpoch);
}
