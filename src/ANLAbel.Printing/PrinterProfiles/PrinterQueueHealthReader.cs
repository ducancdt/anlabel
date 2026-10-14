using System.Printing;
using ANLAbel.Core.Printing;

namespace ANLAbel.Printing.PrinterProfiles;

internal static class PrinterQueueHealthReader
{
    public static PrinterQueueHealthObservation Read(PrintQueue queue)
    {
        ArgumentNullException.ThrowIfNull(queue);
        try
        {
            queue.Refresh();
            // An unplugged USB printer is marked "work offline" while its status
            // stays clear: a job sent then waits in the spooler and prints later.
            return PrinterQueueHealthContract.Evaluate(
                readSucceeded: true,
                queue.IsPaused,
                queue.IsOffline || PrinterQueueHealthContract.WorksOffline((int)queue.QueueAttributes),
                queue.IsOutOfPaper,
                queue.IsPaperJammed,
                queue.NeedUserIntervention,
                queue.IsInError);
        }
        catch (Exception ex)
        {
            return PrinterQueueHealthContract.Evaluate(
                readSucceeded: false,
                isPaused: false,
                isOffline: false,
                isOutOfPaper: false,
                isPaperJammed: false,
                needsUserIntervention: false,
                isInError: false,
                readDiagnostic: ex.Message);
        }
    }
}
