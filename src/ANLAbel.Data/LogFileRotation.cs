namespace ANLAbel.Data;

/// <summary>
/// Size-based rotation for best-effort, append-only trace logs
/// (<c>data-operations.jsonl</c>, <c>print-operations.jsonl</c>).
/// <para>
/// The current file is renamed to <c>name.yyyyMMdd-HHmmss.ext</c> once it reaches
/// the size limit, and only the newest archives are kept. Rotation never deletes
/// the live file's content without archiving it first. A rotation failure is
/// swallowed so that logging, which is auxiliary, never blocks the operation that
/// is being logged.
/// </para>
/// <para>
/// Do not use this for <c>print-job-events.jsonl</c>: that file is a hash-chained
/// recovery record and has its own lifecycle.
/// </para>
/// </summary>
public static class LogFileRotation
{
    public const long DefaultMaxBytes = 5L * 1024 * 1024;
    public const int DefaultKeepArchives = 5;

    /// <summary>
    /// Archives <paramref name="logFilePath"/> when it is at least <paramref name="maxBytes"/>
    /// long. Callers must hold the same lock they use for appending.
    /// </summary>
    /// <returns>The archive path, or <c>null</c> when nothing was rotated.</returns>
    public static string? RotateIfNeeded(
        string logFilePath,
        long maxBytes = DefaultMaxBytes,
        int keepArchives = DefaultKeepArchives,
        DateTime? now = null)
    {
        try
        {
            var file = new FileInfo(logFilePath);
            if (!file.Exists || file.Length < maxBytes)
            {
                return null;
            }

            var directory = file.DirectoryName ?? ".";
            var baseName = Path.GetFileNameWithoutExtension(file.Name);
            var extension = file.Extension;
            var stamp = (now ?? DateTime.Now).ToString("yyyyMMdd-HHmmss");
            var archivePath = Path.Combine(directory, $"{baseName}.{stamp}{extension}");
            for (var counter = 1; File.Exists(archivePath); counter++)
            {
                archivePath = Path.Combine(directory, $"{baseName}.{stamp}-{counter}{extension}");
            }

            File.Move(file.FullName, archivePath);
            PruneArchives(directory, baseName, extension, Math.Max(1, keepArchives));
            return archivePath;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static void PruneArchives(string directory, string baseName, string extension, int keepArchives)
    {
        var archives = Directory
            .EnumerateFiles(directory, $"{baseName}.*{extension}")
            .Where(path => !string.Equals(Path.GetFileName(path), baseName + extension, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(path => Path.GetFileName(path), StringComparer.Ordinal)
            .Skip(keepArchives)
            .ToArray();

        foreach (var archive in archives)
        {
            try
            {
                File.Delete(archive);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // A locked old archive is retried at the next rotation.
            }
        }
    }
}
