using System.Diagnostics;

namespace ANLAbel.Core.Printing;

/// <summary>
/// A queue-level state observed for one submitted print job. These values describe
/// what the spooler/driver reports; none of them, including <see cref="Completed"/>,
/// proves that a physical label came out of the printer.
/// </summary>
public enum SpoolJobState
{
    Unknown,
    NotFound,
    Pending,
    Spooling,
    Printing,
    Paused,
    Blocked,
    Offline,
    PaperOut,
    UserIntervention,
    Error,
    Completed,
    Retained,
    Deleted
}

/// <summary>
/// One immutable observation returned by a spool-status reader.
/// </summary>
public sealed record SpoolJobObservation(
    string PrinterName,
    int JobId,
    SpoolJobState State,
    string Message = "",
    int? PagesPrinted = null,
    int? TotalPages = null,
    bool IsTerminal = false,
    DateTimeOffset? ObservedAtUtc = null)
{
    /// <summary>
    /// Deliberately always false. A queue observation is not a sensor or verifier
    /// attached to the media path, so the application must not claim physical output.
    /// </summary>
    public bool PhysicalOutputVerified => false;

    public string UserFacingStatus
    {
        get
        {
            var stateText = State switch
            {
                SpoolJobState.NotFound => "not visible in the queue",
                SpoolJobState.Pending => "pending in the queue",
                SpoolJobState.Spooling => "spooling",
                SpoolJobState.Printing => "printing",
                SpoolJobState.Paused => "paused",
                SpoolJobState.Blocked => "blocked",
                SpoolJobState.Offline => "printer offline",
                SpoolJobState.PaperOut => "paper/media unavailable",
                SpoolJobState.UserIntervention => "waiting for operator intervention",
                SpoolJobState.Error => "queue error",
                SpoolJobState.Completed => "queue reports completed",
                SpoolJobState.Retained => "retained after queue processing",
                SpoolJobState.Deleted => "removed from the queue",
                _ => "unknown"
            };

            var detail = string.IsNullOrWhiteSpace(Message) ? string.Empty : $" {Message}";
            return $"Spool job #{JobId}: {stateText}.{detail} Physical output is not verified.";
        }
    }
}

/// <summary>
/// Platform-specific adapter used by <see cref="SpoolJobMonitor"/>. Implementations
/// should return an observation instead of throwing for an unavailable queue; only
/// cancellation is expected to escape as an exception.
/// </summary>
public interface ISpoolJobStatusReader
{
    ValueTask<SpoolJobObservation> ReadAsync(
        string printerName,
        int spoolJobId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of polling a queue. A timeout is intentionally represented as Unknown so
/// callers cannot mistake a lost/slow queue for success or failure.
/// </summary>
public sealed record SpoolJobMonitorResult(
    SpoolJobObservation FinalObservation,
    int PollCount,
    TimeSpan Elapsed,
    bool TimedOut)
{
    public bool IsTerminal => FinalObservation.IsTerminal;
    public bool PhysicalOutputVerified => false;

    public string UserFacingStatus => TimedOut
        ? $"Spool status polling timed out after {Elapsed.TotalSeconds:0.#} s; do not retry automatically. {FinalObservation.UserFacingStatus}"
        : FinalObservation.UserFacingStatus;
}

/// <summary>
/// Polls one spool job with bounded latency and cooperative cancellation. It has no
/// retry side effect and never mutates print-row state.
/// </summary>
public sealed class SpoolJobMonitor
{
    private readonly ISpoolJobStatusReader _reader;

    public SpoolJobMonitor(ISpoolJobStatusReader reader)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
    }

    public async Task<SpoolJobMonitorResult> MonitorAsync(
        string printerName,
        int spoolJobId,
        TimeSpan timeout,
        TimeSpan pollInterval,
        CancellationToken cancellationToken = default,
        IProgress<SpoolJobObservation>? progress = null)
    {
        if (string.IsNullOrWhiteSpace(printerName))
        {
            throw new ArgumentException("A printer name is required to monitor a spool job.", nameof(printerName));
        }

        if (spoolJobId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(spoolJobId), "A spool job identifier must be positive.");
        }

        if (timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), "The spool monitor timeout must be positive.");
        }

        if (pollInterval < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(pollInterval), "The spool monitor poll interval cannot be negative.");
        }

        var started = Stopwatch.GetTimestamp();
        var pollCount = 0;
        SpoolJobObservation? lastObservation = null;
        using var readCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (pollCount > 0 && GetElapsed(started) >= timeout)
            {
                return CreateTimeoutResult(printerName, spoolJobId, pollCount, started, lastObservation);
            }

            var remainingBeforeRead = timeout - GetElapsed(started);
            if (remainingBeforeRead <= TimeSpan.Zero)
            {
                return CreateTimeoutResult(printerName, spoolJobId, pollCount, started, lastObservation);
            }

            (bool TimedOut, SpoolJobObservation? Observation) read;
            try
            {
                read = await ReadWithDeadlineAsync(
                    printerName,
                    spoolJobId,
                    remainingBeforeRead,
                    cancellationToken,
                    readCancellation.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // Queue providers can throw during spooler restart, hot unplug or
                // a permissions transition. Fail closed as terminal Unknown;
                // callers may reconcile later, but this monitor never retries.
                read = (false, CreateReaderFaultObservation(
                    printerName,
                    spoolJobId,
                    $"The spool-status reader failed; queue state is unknown ({ex.Message})."));
            }
            if (read.TimedOut)
            {
                readCancellation.Cancel();
                return CreateTimeoutResult(printerName, spoolJobId, pollCount, started, lastObservation);
            }

            var observation = read.Observation;
            observation ??= new SpoolJobObservation(
                printerName,
                spoolJobId,
                SpoolJobState.Unknown,
                "The status reader returned no observation.",
                IsTerminal: true);

            observation = NormalizeObservation(printerName, spoolJobId, observation);
            lastObservation = observation;
            pollCount++;
            progress?.Report(observation);

            if (observation.IsTerminal)
            {
                return new SpoolJobMonitorResult(
                    observation,
                    pollCount,
                    GetElapsed(started),
                    TimedOut: false);
            }

            var elapsed = GetElapsed(started);
            var remaining = timeout - elapsed;
            if (remaining <= TimeSpan.Zero)
            {
                return CreateTimeoutResult(printerName, spoolJobId, pollCount, started, observation);
            }

            var delay = pollInterval <= remaining ? pollInterval : remaining;
            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task<(bool TimedOut, SpoolJobObservation? Observation)> ReadWithDeadlineAsync(
        string printerName,
        int spoolJobId,
        TimeSpan remaining,
        CancellationToken cancellationToken,
        CancellationToken readCancellationToken)
    {
        var readTask = _reader.ReadAsync(printerName, spoolJobId, readCancellationToken).AsTask();
        // A synchronous or already-completed provider must win immediately.
        // Going through Task.WhenAny with a near-expired deadline can otherwise
        // race a completed ValueTask against Task.Delay and drop the final
        // terminal observation under scheduler pressure.
        if (readTask.IsCompleted)
        {
            return (false, await readTask.ConfigureAwait(false));
        }

        var deadlineTask = Task.Delay(remaining, cancellationToken);
        var completed = await Task.WhenAny(readTask, deadlineTask).ConfigureAwait(false);
        if (completed == readTask)
        {
            return (false, await readTask.ConfigureAwait(false));
        }

        // Distinguish an operator cancellation from a monitor deadline. The linked
        // token is cancelled by the caller after this method returns; a reader that
        // ignores cancellation cannot hold the monitor/UI hostage past the deadline.
        cancellationToken.ThrowIfCancellationRequested();
        _ = readTask.ContinueWith(
            task => _ = task.Exception,
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
        return (true, null);
    }

    private static SpoolJobObservation NormalizeObservation(string printerName, int spoolJobId, SpoolJobObservation observation)
    {
        if (observation.JobId == spoolJobId
            && string.Equals(observation.PrinterName, printerName, StringComparison.OrdinalIgnoreCase))
        {
            return observation with { ObservedAtUtc = observation.ObservedAtUtc ?? DateTimeOffset.UtcNow };
        }

        return new SpoolJobObservation(
            printerName,
            spoolJobId,
            SpoolJobState.Unknown,
            "The status reader returned a different printer/job identity; monitoring stopped fail-closed.",
            IsTerminal: true,
            ObservedAtUtc: DateTimeOffset.UtcNow);
    }

    private static SpoolJobObservation CreateReaderFaultObservation(
        string printerName,
        int spoolJobId,
        string message)
    {
        return new SpoolJobObservation(
            printerName,
            spoolJobId,
            SpoolJobState.Unknown,
            message,
            IsTerminal: true,
            ObservedAtUtc: DateTimeOffset.UtcNow);
    }

    private static SpoolJobMonitorResult CreateTimeoutResult(
        string printerName,
        int spoolJobId,
        int pollCount,
        long started,
        SpoolJobObservation? lastObservation)
    {
        var lastState = lastObservation?.State.ToString() ?? "no observation";
        var observation = new SpoolJobObservation(
            printerName,
            spoolJobId,
            SpoolJobState.Unknown,
            $"No terminal queue status was observed (last state: {lastState}).",
            IsTerminal: false,
            ObservedAtUtc: DateTimeOffset.UtcNow);
        return new SpoolJobMonitorResult(observation, pollCount, GetElapsed(started), TimedOut: true);
    }

    private static TimeSpan GetElapsed(long started)
    {
        return Stopwatch.GetElapsedTime(started);
    }
}
