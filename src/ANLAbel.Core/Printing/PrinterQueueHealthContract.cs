namespace ANLAbel.Core.Printing;

public enum PrinterQueueHealthState
{
    Ready,
    Paused,
    Offline,
    PaperOut,
    PaperJam,
    UserIntervention,
    Error,
    Unknown
}

public sealed record PrinterQueueHealthObservation(
    PrinterQueueHealthState State,
    DateTimeOffset ObservedAtUtc,
    string Diagnostic)
{
    public bool CanSubmit => State == PrinterQueueHealthState.Ready;
}

/// <summary>
/// Pure priority mapping for read-only Windows queue signals. These signals
/// authorize blocking only; a Ready observation is not physical-output proof.
/// </summary>
public static class PrinterQueueHealthContract
{
    /// <summary>PRINTER_ATTRIBUTE_WORK_OFFLINE; System.Printing has no name for it.</summary>
    public const int WorkOfflineAttribute = 0x400;

    /// <summary>
    /// True when Windows holds the queue's jobs instead of sending them, which it
    /// sets when a USB printer is unplugged or "Use Printer Offline" is chosen. The
    /// queue status then still reads ready, so a job would print later, unseen.
    /// </summary>
    public static bool WorksOffline(int queueAttributes) => (queueAttributes & WorkOfflineAttribute) != 0;

    public static PrinterQueueHealthObservation Evaluate(
        bool readSucceeded,
        bool isPaused,
        bool isOffline,
        bool isOutOfPaper,
        bool isPaperJammed,
        bool needsUserIntervention,
        bool isInError,
        DateTimeOffset? observedAtUtc = null,
        string? readDiagnostic = null)
    {
        var observedAt = observedAtUtc ?? DateTimeOffset.UtcNow;
        if (!readSucceeded)
        {
            return new(
                PrinterQueueHealthState.Unknown,
                observedAt,
                string.IsNullOrWhiteSpace(readDiagnostic)
                    ? "Printer queue health could not be read; no label was submitted."
                    : $"Printer queue health could not be read: {readDiagnostic} No label was submitted.");
        }

        if (isPaperJammed)
            return Block(PrinterQueueHealthState.PaperJam, observedAt, "The printer reports a paper/media jam.");
        if (isOutOfPaper)
            return Block(PrinterQueueHealthState.PaperOut, observedAt, "The printer reports that paper/media is unavailable.");
        if (isOffline)
            return Block(PrinterQueueHealthState.Offline, observedAt, "The printer queue reports offline.");
        if (needsUserIntervention)
            return Block(PrinterQueueHealthState.UserIntervention, observedAt, "The printer requires operator intervention.");
        if (isPaused)
            return Block(PrinterQueueHealthState.Paused, observedAt, "The printer queue is paused.");
        if (isInError)
            return Block(PrinterQueueHealthState.Error, observedAt, "The printer queue reports an error.");

        return new(
            PrinterQueueHealthState.Ready,
            observedAt,
            "Windows reports no blocking queue condition. This is not physical-output verification.");
    }

    private static PrinterQueueHealthObservation Block(
        PrinterQueueHealthState state,
        DateTimeOffset observedAt,
        string reason) =>
        new(state, observedAt, $"{reason} Resolve the queue/device condition and prepare again; no label was submitted.");
}
