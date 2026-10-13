using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.Json;
using ANLAbel.Core.Models;
using ANLAbel.Data;
using ANLAbel.Project.SaveLoad;

namespace ANLAbel.App.Services;

/// <summary>A recovery copy left behind by an ANLAbel session that is no longer running.</summary>
public sealed record RecoveryEntry(
    string DocumentPath,
    string MetadataPath,
    string? OriginalFilePath,
    string DocumentName,
    DateTime SavedAtLocal);

/// <summary>
/// Crash-recovery copies of the open label (plan 13 F03/F04).
/// <para>
/// Each running session owns one copy in <c>%LOCALAPPDATA%\ANLAbel\Recovered</c>,
/// named after its process id and start time. Copies are written only while the
/// document has unsaved changes, never replace an authored file, and are deleted
/// when the session ends normally. At startup, copies whose session is no longer
/// running are offered back to the operator.
/// </para>
/// </summary>
public sealed class DocumentRecoveryService
{
    private const string FilePrefix = "session-";
    private const int MetadataSchemaVersion = 1;

    private static readonly JsonSerializerOptions MetadataJsonOptions = new() { WriteIndented = true };

    private readonly string _directory;
    private readonly string _sessionKey;
    private readonly Func<int, long?> _liveSessionStartTicks;

    public DocumentRecoveryService()
        : this(AppDataPaths.Local("Recovered"), Environment.ProcessId, Process.GetCurrentProcess().StartTime.ToUniversalTime().Ticks, null)
    {
    }

    /// <param name="liveSessionStartTicks">
    /// Returns the UTC start ticks of a running ANLAbel process with the given id,
    /// or null when no such ANLAbel process is running. Tests inject this.
    /// </param>
    public DocumentRecoveryService(string directory, int processId, long processStartUtcTicks, Func<int, long?>? liveSessionStartTicks)
    {
        _directory = directory;
        _sessionKey = string.Create(CultureInfo.InvariantCulture, $"{processId}-{processStartUtcTicks}");
        _liveSessionStartTicks = liveSessionStartTicks ?? FindRunningAnlabelStartTicks;
    }

    public string RecoveryDirectory => _directory;

    public string SessionDocumentPath => Path.Combine(_directory, FilePrefix + _sessionKey + ".anlabel");

    public string SessionMetadataPath => Path.ChangeExtension(SessionDocumentPath, ".json");

    /// <summary>Writes or replaces this session's recovery copy.</summary>
    public void Write(LabelTemplate template, string? originalFilePath, string documentName)
    {
        ArgumentNullException.ThrowIfNull(template);
        ProjectFileService.WriteRecoveryCopy(template, SessionDocumentPath);
        var metadata = new RecoveryMetadata(
            MetadataSchemaVersion,
            string.IsNullOrWhiteSpace(originalFilePath) ? null : originalFilePath,
            documentName,
            DateTime.Now);
        WriteAtomically(SessionMetadataPath, JsonSerializer.Serialize(metadata, MetadataJsonOptions));
    }

    /// <summary>Removes this session's copy after a normal close or a successful save.</summary>
    public void DeleteSession()
    {
        TryDelete(SessionDocumentPath);
        TryDelete(SessionMetadataPath);
    }

    /// <summary>Copies left by sessions that are no longer running, newest first.</summary>
    public IReadOnlyList<RecoveryEntry> FindOrphans()
    {
        if (!Directory.Exists(_directory))
        {
            return Array.Empty<RecoveryEntry>();
        }

        var entries = new List<RecoveryEntry>();
        foreach (var documentPath in Directory.EnumerateFiles(_directory, FilePrefix + "*.anlabel"))
        {
            var key = Path.GetFileNameWithoutExtension(documentPath)[FilePrefix.Length..];
            if (string.Equals(key, _sessionKey, StringComparison.Ordinal) || IsLiveSession(key))
            {
                continue;
            }

            var metadataPath = Path.ChangeExtension(documentPath, ".json");
            var metadata = TryReadMetadata(metadataPath);
            entries.Add(new RecoveryEntry(
                documentPath,
                metadataPath,
                metadata?.OriginalFilePath,
                string.IsNullOrWhiteSpace(metadata?.DocumentName) ? "Mẫu chưa đặt tên" : metadata.DocumentName,
                metadata?.SavedAtLocal ?? File.GetLastWriteTime(documentPath)));
        }

        return entries.OrderByDescending(entry => entry.SavedAtLocal).ToArray();
    }

    public static void Delete(RecoveryEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        TryDelete(entry.DocumentPath);
        TryDelete(entry.MetadataPath);
    }

    private bool IsLiveSession(string key)
    {
        var parts = key.Split('-', 2);
        if (parts.Length != 2
            || !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var processId)
            || !long.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var startTicks))
        {
            return false;
        }

        return _liveSessionStartTicks(processId) == startTicks;
    }

    private static long? FindRunningAnlabelStartTicks(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            using var current = Process.GetCurrentProcess();
            if (!string.Equals(process.ProcessName, current.ProcessName, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return process.StartTime.ToUniversalTime().Ticks;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception or NotSupportedException)
        {
            // Not running, already exited, or another user's process: this
            // user's recovery folder is not owned by it.
            return null;
        }
    }

    private static RecoveryMetadata? TryReadMetadata(string path)
    {
        try
        {
            return File.Exists(path)
                ? JsonSerializer.Deserialize<RecoveryMetadata>(File.ReadAllText(path), MetadataJsonOptions)
                : null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            return null;
        }
    }

    private static void WriteAtomically(string path, string content)
    {
        var directory = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(directory);
        var temporaryPath = Path.Combine(directory, $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");
        File.WriteAllText(temporaryPath, content);
        File.Move(temporaryPath, path, overwrite: true);
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // A locked copy is offered again at the next start; it is never lost.
        }
    }

    private sealed record RecoveryMetadata(int SchemaVersion, string? OriginalFilePath, string DocumentName, DateTime SavedAtLocal);
}
