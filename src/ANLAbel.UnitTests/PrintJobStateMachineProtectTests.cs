using ANLAbel.Core.Printing;
using Xunit;

namespace ANLAbel.UnitTests;

/// <summary>
/// L3 recovery: spool accept and queue observation are not physical completion.
/// Drives <see cref="PrintJobStateMachine.CanTransition"/> — the same gate
/// Print Preview, MainViewModel, and ValidateTransition use — not a copy of
/// the switch table. Distinct from 0.252–0.256 file-drop, Excel identity,
/// ticket-snapshot, envelope, and SCN001 slices.
/// </summary>
public sealed class PrintJobStateMachineProtectTests
{
    [Fact]
    public void CompletedRequiresPhysicalVerificationAndTerminalStatesStayClosed()
    {
        Assert.False(PrintJobStateMachine.CanTransition(
            PrintJobLifecycleState.SpoolAccepted,
            PrintJobLifecycleState.Completed,
            physicalOutputVerified: false));
        Assert.False(PrintJobStateMachine.CanTransition(
            PrintJobLifecycleState.QueueObserved,
            PrintJobLifecycleState.Completed,
            physicalOutputVerified: false));
        Assert.False(PrintJobStateMachine.CanTransition(
            PrintJobLifecycleState.Dispatching,
            PrintJobLifecycleState.Completed,
            physicalOutputVerified: false));

        Assert.True(PrintJobStateMachine.CanTransition(
            PrintJobLifecycleState.SpoolAccepted,
            PrintJobLifecycleState.Completed,
            physicalOutputVerified: true));
        Assert.True(PrintJobStateMachine.CanTransition(
            PrintJobLifecycleState.SpoolAccepted,
            PrintJobLifecycleState.QueueObserved,
            physicalOutputVerified: false));

        Assert.False(PrintJobStateMachine.CanTransition(
            PrintJobLifecycleState.Completed,
            PrintJobLifecycleState.Failed,
            physicalOutputVerified: true));
        Assert.False(PrintJobStateMachine.CanTransition(
            PrintJobLifecycleState.Failed,
            PrintJobLifecycleState.Completed,
            physicalOutputVerified: true));
        Assert.False(PrintJobStateMachine.CanTransition(
            PrintJobLifecycleState.Cancelled,
            PrintJobLifecycleState.Preparing,
            physicalOutputVerified: false));
    }

    [Fact]
    public void ValidateTransitionRejectsCompletedWithoutPhysicalVerification()
    {
        var transition = new PrintJobStateTransition(
            "job-spool-only",
            PrintJobLifecycleState.SpoolAccepted,
            PrintJobLifecycleState.Completed,
            DateTimeOffset.UtcNow,
            "spool accepted is not a printed label",
            PrinterName: "Zebra-203",
            PhysicalOutputVerified: false);

        var ex = Assert.Throws<InvalidOperationException>(
            () => PrintJobStateMachine.ValidateTransition(transition, PrintJobLifecycleState.SpoolAccepted));
        Assert.Contains("not allowed", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("device-evidence", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
