using System.Printing;
using ANLAbel.Core.Printing;

namespace ANLAbel.Printing.PrinterProfiles;

/// <summary>
/// Reads the Windows queue status for one known spool identifier. The adapter is
/// intentionally read-only and reports queue evidence; it cannot verify the label
/// path, media sensor, or the printed mark on the physical stock.
/// </summary>
public sealed class WindowsSpoolJobStatusReader : ISpoolJobStatusReader
{
    public async ValueTask<SpoolJobObservation> ReadAsync(
        string printerName,
        int spoolJobId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(printerName);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(spoolJobId);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            return await Task.Run(
                () => ReadOnWorker(printerName, spoolJobId, cancellationToken),
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Queue discovery can fail when the spooler is restarting, a network
            // queue is disconnected, or permissions change. Surface that uncertainty
            // as a terminal observation for this polling call; callers must not retry
            // the print automatically from this signal.
            return new SpoolJobObservation(
                printerName,
                spoolJobId,
                SpoolJobState.Unknown,
                $"Windows could not read the queue: {ex.Message}",
                IsTerminal: true,
                ObservedAtUtc: DateTimeOffset.UtcNow);
        }
    }

    private static SpoolJobObservation ReadOnWorker(
        string printerName,
        int spoolJobId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var server = new LocalPrintServer();
        using var queue = server.GetPrintQueue(printerName);
        queue.Refresh();
        using var jobs = queue.GetPrintJobInfoCollection();
        var job = jobs
            .Cast<PrintSystemJobInfo>()
            .FirstOrDefault(candidate => candidate.JobIdentifier == spoolJobId);

        if (job is null)
        {
            return new SpoolJobObservation(
                printerName,
                spoolJobId,
                SpoolJobState.NotFound,
                "The job is no longer exposed by the queue; physical output is ambiguous.",
                IsTerminal: true,
                ObservedAtUtc: DateTimeOffset.UtcNow);
        }

        cancellationToken.ThrowIfCancellationRequested();
        var (state, isTerminal, message) = MapStatus(job.JobStatus);
        return new SpoolJobObservation(
            printerName,
            spoolJobId,
            state,
            message,
            PagesPrinted: TryReadPageCount(() => job.NumberOfPagesPrinted),
            TotalPages: TryReadPageCount(() => job.NumberOfPages),
            IsTerminal: isTerminal,
            ObservedAtUtc: DateTimeOffset.UtcNow);
    }

    private static (SpoolJobState State, bool IsTerminal, string Message) MapStatus(PrintJobStatus status)
    {
        if (status.HasFlag(PrintJobStatus.Error))
        {
            return (SpoolJobState.Error, true, "The Windows spooler reports an error; operator review is required.");
        }

        if (status.HasFlag(PrintJobStatus.PaperOut))
        {
            return (SpoolJobState.PaperOut, true, "The queue reports paper/media unavailable; resolve the stock condition before retrying.");
        }

        if (status.HasFlag(PrintJobStatus.Offline))
        {
            return (SpoolJobState.Offline, true, "The queue reports the printer offline; verify the connection before retrying.");
        }

        if (status.HasFlag(PrintJobStatus.UserIntervention))
        {
            return (SpoolJobState.UserIntervention, true, "The printer requires operator intervention.");
        }

        if (status.HasFlag(PrintJobStatus.Blocked))
        {
            return (SpoolJobState.Blocked, true, "The job is blocked by the queue or a preceding job.");
        }

        if (status.HasFlag(PrintJobStatus.Paused))
        {
            return (SpoolJobState.Paused, true, "The job is paused in the Windows queue.");
        }

        if (status.HasFlag(PrintJobStatus.Deleting) || status.HasFlag(PrintJobStatus.Deleted))
        {
            return (SpoolJobState.Deleted, true, "The job is being removed from the queue; physical output is not verified.");
        }

        if (status.HasFlag(PrintJobStatus.Completed) || status.HasFlag(PrintJobStatus.Printed))
        {
            return (SpoolJobState.Completed, true, "The queue reports completion; this is not a physical output verification.");
        }

        if (status.HasFlag(PrintJobStatus.Retained))
        {
            return (SpoolJobState.Retained, true, "The job is retained after queue processing; physical output is not verified.");
        }

        if (status.HasFlag(PrintJobStatus.Printing))
        {
            return (SpoolJobState.Printing, false, "The queue reports that the job is printing.");
        }

        if (status.HasFlag(PrintJobStatus.Spooling))
        {
            return (SpoolJobState.Spooling, false, "The queue reports that the job is spooling.");
        }

        if (status.HasFlag(PrintJobStatus.Restarted))
        {
            return (SpoolJobState.Pending, false, "The queue reports that the job restarted and is pending.");
        }

        return (SpoolJobState.Pending, false, "The job remains in the queue without a more specific state.");
    }

    private static int? TryReadPageCount(Func<int> read)
    {
        try
        {
            var value = read();
            return value >= 0 ? value : null;
        }
        catch
        {
            return null;
        }
    }
}
