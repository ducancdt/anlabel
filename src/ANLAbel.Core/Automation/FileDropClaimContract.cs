using System.Security.Cryptography;
using System.Text;

namespace ANLAbel.Core.Automation;

/// <summary>
/// The owner-neutral state and identity rules for a local file-drop trigger.
/// This contract deliberately has no watcher, file move, queue, or print API.
/// </summary>
public enum FileDropEventState
{
    Unknown,
    Detected,
    Claimed,
    Prepared,
    Dispatching,
    MovingToArchive,
    MovingToQuarantine,
    Blocked,
    Dispatched,
    Archived,
    Quarantined,
    ChangedAfterClaim
}

public sealed record FileDropEventIdentity(
    string EventId,
    string TriggerId,
    string ConfigurationFingerprint,
    string SourceFingerprint);

public static class FileDropClaimContract
{
    public static FileDropEventIdentity CreateIdentity(
        string triggerId,
        string configurationFingerprint,
        string sourceFingerprint)
    {
        if (string.IsNullOrWhiteSpace(triggerId))
            throw new ArgumentException("A trigger ID is required.", nameof(triggerId));
        if (string.IsNullOrWhiteSpace(configurationFingerprint))
            throw new ArgumentException("A configuration fingerprint is required.", nameof(configurationFingerprint));
        if (string.IsNullOrWhiteSpace(sourceFingerprint))
            throw new ArgumentException("A source fingerprint is required.", nameof(sourceFingerprint));

        var canonical = string.Join("|", triggerId.Trim(), configurationFingerprint.Trim(), sourceFingerprint.Trim());
        var eventId = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
        return new FileDropEventIdentity(eventId, triggerId.Trim(), configurationFingerprint.Trim(), sourceFingerprint.Trim());
    }

    public static string ComputeContentFingerprint(ReadOnlySpan<byte> sourceBytes) =>
        Convert.ToHexString(SHA256.HashData(sourceBytes));

    public static bool TryTransition(FileDropEventState from, FileDropEventState to, out string error)
    {
        var allowed = (from, to) switch
        {
            (FileDropEventState.Unknown, FileDropEventState.Detected) => true,
            (FileDropEventState.Detected, FileDropEventState.Claimed) => true,
            (FileDropEventState.Detected, FileDropEventState.Blocked) => true,
            (FileDropEventState.Detected, FileDropEventState.Quarantined) => true,
            (FileDropEventState.Claimed, FileDropEventState.Blocked) => true,
            (FileDropEventState.Claimed, FileDropEventState.Prepared) => true,
            (FileDropEventState.Claimed, FileDropEventState.Quarantined) => true,
            (FileDropEventState.Claimed, FileDropEventState.ChangedAfterClaim) => true,
            (FileDropEventState.Prepared, FileDropEventState.Dispatching) => true,
            (FileDropEventState.Prepared, FileDropEventState.Blocked) => true,
            (FileDropEventState.Prepared, FileDropEventState.ChangedAfterClaim) => true,
            (FileDropEventState.Dispatching, FileDropEventState.Dispatched) => true,
            (FileDropEventState.Dispatching, FileDropEventState.Blocked) => true,
            (FileDropEventState.Dispatching, FileDropEventState.ChangedAfterClaim) => true,
            (FileDropEventState.Dispatched, FileDropEventState.MovingToArchive) => true,
            (FileDropEventState.Claimed, FileDropEventState.MovingToQuarantine) => true,
            (FileDropEventState.Prepared, FileDropEventState.MovingToQuarantine) => true,
            (FileDropEventState.Blocked, FileDropEventState.MovingToQuarantine) => true,
            (FileDropEventState.ChangedAfterClaim, FileDropEventState.MovingToQuarantine) => true,
            (FileDropEventState.MovingToArchive, FileDropEventState.Archived) => true,
            (FileDropEventState.MovingToArchive, FileDropEventState.Blocked) => true,
            (FileDropEventState.MovingToQuarantine, FileDropEventState.Quarantined) => true,
            (FileDropEventState.MovingToQuarantine, FileDropEventState.Blocked) => true,
            _ => false
        };

        error = allowed
            ? string.Empty
            : $"Automation file-drop transition {from} -> {to} is not permitted.";
        return allowed;
    }

    public static bool IsTerminal(FileDropEventState state) => state is
        FileDropEventState.Blocked or
        FileDropEventState.Archived or
        FileDropEventState.Quarantined or
        FileDropEventState.ChangedAfterClaim;
}
