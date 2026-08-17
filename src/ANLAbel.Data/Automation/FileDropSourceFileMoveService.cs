using ANLAbel.Core.Automation;

namespace ANLAbel.Data.Automation;

public enum FileDropSourceDisposition { Archive, Quarantine }

/// <summary>Moves a claimed source only after durable intent, on one local volume.</summary>
public sealed class FileDropSourceFileMoveService
{
    private readonly FileDropClaimLedger _ledger;
    public FileDropSourceFileMoveService(FileDropClaimLedger ledger) => _ledger = ledger;

    public bool TryMove(FileDropEventIdentity identity, FileDropTriggerConfiguration configuration, string sourcePath, string destinationRoot, FileDropSourceDisposition disposition, out string destinationPath, out string result)
    {
        destinationPath = string.Empty;
        if (!FileDropTriggerConfigurationContract.TryValidate(configuration, out var configurationError)) { result = configurationError; return false; }
        var expected = FileDropClaimContract.CreateIdentity(identity.TriggerId, identity.ConfigurationFingerprint, identity.SourceFingerprint);
        if (!string.Equals(identity.EventId, expected.EventId, StringComparison.Ordinal) || !string.Equals(identity.TriggerId, configuration.TriggerId, StringComparison.Ordinal) || !string.Equals(identity.ConfigurationFingerprint, configuration.ConfigurationFingerprint, StringComparison.Ordinal)) { result = "Source move requires the exact detected event and trigger configuration."; return false; }
        if (!TryResolvePaths(configuration.WatchRoot, sourcePath, destinationRoot, identity.EventId, out var source, out var destination, out result)) return false;
        var moving = disposition == FileDropSourceDisposition.Archive ? FileDropEventState.MovingToArchive : FileDropEventState.MovingToQuarantine;
        var completed = disposition == FileDropSourceDisposition.Archive ? FileDropEventState.Archived : FileDropEventState.Quarantined;
        if (!_ledger.TryTransition(identity, moving, $"Validated local {disposition.ToString().ToLowerInvariant()} move started; source and destination share one volume.", out _, out var transitionError)) { result = transitionError; return false; }
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Move(source, destination, overwrite: false);
            if (!_ledger.TryTransition(identity, completed, $"Source moved to local {disposition.ToString().ToLowerInvariant()} destination; no payload was copied into the ledger.", out _, out var completedError)) { destinationPath = destination; result = $"Source moved but lifecycle finalization requires repair: {completedError}"; return false; }
            destinationPath = destination; result = $"Source moved atomically to local {disposition.ToString().ToLowerInvariant()}."; return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            _ledger.TryTransition(identity, FileDropEventState.Blocked, $"Local {disposition.ToString().ToLowerInvariant()} move failed; no automatic retry is permitted: {ex.Message}", out _, out _);
            result = $"Local {disposition.ToString().ToLowerInvariant()} move failed: {ex.Message}"; return false;
        }
    }

    private static bool TryResolvePaths(string watchRoot, string sourcePath, string destinationRoot, string eventId, out string source, out string destination, out string error)
    {
        source = destination = string.Empty;
        if (!Path.IsPathFullyQualified(sourcePath) || !Path.IsPathFullyQualified(destinationRoot)) { error = "Source and destination must be absolute local paths."; return false; }
        var watch = Path.GetFullPath(watchRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        source = Path.GetFullPath(sourcePath); var root = Path.GetFullPath(destinationRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (!IsChildOf(source, watch) || !File.Exists(source) || (File.GetAttributes(source) & FileAttributes.ReparsePoint) != 0) { error = "Source must be a readable non-link file inside the configured watch root."; return false; }
        if (IsChildOf(root, watch) || string.Equals(root, watch, StringComparison.OrdinalIgnoreCase)) { error = "Archive or quarantine root must be outside the configured watch root."; return false; }
        if (!string.Equals(Path.GetPathRoot(source), Path.GetPathRoot(root), StringComparison.OrdinalIgnoreCase)) { error = "Archive or quarantine root must be on the same volume for an atomic move."; return false; }
        destination = Path.Combine(root, eventId, Path.GetFileName(source));
        if (!IsChildOf(destination, root)) { error = "Resolved destination escaped its configured local root."; return false; }
        error = string.Empty; return true;
    }

    private static bool IsChildOf(string path, string root) => path.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
}
