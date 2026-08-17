using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ANLAbel.Project.SaveLoad;

/// <summary>
/// Durable, local-first archive for committed template bytes.  The archive is
/// intentionally separate from the managed <c>.bak</c> slot: the latter is
/// the fast recovery path, while this bounded history is the audit trail for
/// repeated saves and rollbacks.
/// </summary>
public static class ProjectRevisionArchive
{
    public const int DefaultRetentionCount = 8;
    public const string DirectorySuffix = ".revisions";
    public const string AuditFileName = "audit.jsonl";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    private static readonly SemaphoreSlim AuditGate = new(1, 1);

    public static string GetDirectory(string filePath)
    {
        var fullPath = Path.GetFullPath(filePath);
        var directory = Path.GetDirectoryName(fullPath)
            ?? throw new InvalidOperationException("The template path has no parent directory.");
        return Path.Combine(directory, $".{Path.GetFileName(fullPath)}{DirectorySuffix}");
    }

    public static string GetAuditPath(string filePath)
        => Path.Combine(GetDirectory(filePath), AuditFileName);

    public static async Task<ProjectRevisionArchiveArtifact?> ArchiveAsync(
        string filePath,
        string reason,
        CancellationToken cancellationToken = default,
        int retentionCount = DefaultRetentionCount)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path is required.", nameof(filePath));
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Archive reason is required.", nameof(reason));
        }

        if (retentionCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(retentionCount), "Retention must keep at least one archive.");
        }

        var sourcePath = Path.GetFullPath(filePath);
        if (!File.Exists(sourcePath))
        {
            return null;
        }

        var archiveDirectory = GetDirectory(sourcePath);
        Directory.CreateDirectory(archiveDirectory);
        var temporaryPath = Path.Combine(archiveDirectory, $".{Guid.NewGuid():N}.archive.tmp");
        ProjectRevisionArchiveArtifact? artifact = null;
        try
        {
            artifact = await CopyAndHashDurablyAsync(sourcePath, temporaryPath, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            var timestampUtc = DateTime.UtcNow;
            var finalName = $"{timestampUtc:yyyyMMddTHHmmssfffZ}-{artifact.ContentHash[..16]}-{Guid.NewGuid():N}.anlabel";
            var finalPath = Path.Combine(archiveDirectory, finalName);
            File.Move(temporaryPath, finalPath, overwrite: false);

            var committed = artifact with
            {
                Path = finalPath,
                CreatedUtc = timestampUtc,
                Reason = reason
            };
            await AppendAuditAsync(
                sourcePath,
                committed,
                eventName: "Archived",
                cancellationToken);
            await TrimAsync(sourcePath, retentionCount, cancellationToken);
            return committed;
        }
        finally
        {
            TryDelete(temporaryPath);
        }
    }

    public static async Task AppendAuditAsync(
        string filePath,
        ProjectRevisionArchiveArtifact artifact,
        string eventName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(eventName))
        {
            throw new ArgumentException("Audit event is required.", nameof(eventName));
        }

        var sourcePath = Path.GetFullPath(filePath);
        var auditPath = GetAuditPath(sourcePath);
        Directory.CreateDirectory(Path.GetDirectoryName(auditPath)!);
        var entry = new ProjectRevisionAuditEntry(
            DateTime.UtcNow,
            eventName,
            sourcePath,
            artifact.Path,
            artifact.ContentHash,
            artifact.SizeBytes,
            artifact.Reason);
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(entry, JsonOptions) + Environment.NewLine);

        await AuditGate.WaitAsync(cancellationToken);
        try
        {
            await using var stream = new FileStream(
                auditPath,
                new FileStreamOptions
                {
                    Mode = FileMode.Append,
                    Access = FileAccess.Write,
                    Share = FileShare.Read,
                    BufferSize = 16 * 1024,
                    Options = FileOptions.Asynchronous | FileOptions.WriteThrough
                });
            await stream.WriteAsync(bytes, cancellationToken);
            await stream.FlushAsync(cancellationToken);
            stream.Flush(flushToDisk: true);
        }
        finally
        {
            AuditGate.Release();
        }
    }

    public static async Task<IReadOnlyList<ProjectRevisionAuditEntry>> ReadAuditAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        var auditPath = GetAuditPath(filePath);
        if (!File.Exists(auditPath))
        {
            return Array.Empty<ProjectRevisionAuditEntry>();
        }

        var entries = new List<ProjectRevisionAuditEntry>();
        await using var stream = new FileStream(
            auditPath,
            new FileStreamOptions
            {
                Mode = FileMode.Open,
                Access = FileAccess.Read,
                Share = FileShare.ReadWrite,
                BufferSize = 16 * 1024,
                Options = FileOptions.Asynchronous | FileOptions.SequentialScan
            });
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            try
            {
                var entry = JsonSerializer.Deserialize<ProjectRevisionAuditEntry>(line, JsonOptions);
                if (entry is not null)
                {
                    entries.Add(entry);
                }
            }
            catch (JsonException)
            {
                // A torn final audit line must not hide valid earlier events.
                // The corresponding archived bytes remain independently
                // inspectable and hash-verifiable.
            }
        }

        return entries
            .OrderByDescending(entry => entry.TimestampUtc)
            .ToArray();
    }

    public static async Task TrimAsync(
        string filePath,
        int retentionCount = DefaultRetentionCount,
        CancellationToken cancellationToken = default)
    {
        if (retentionCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(retentionCount), "Retention must keep at least one archive.");
        }

        var archiveDirectory = GetDirectory(filePath);
        if (!Directory.Exists(archiveDirectory))
        {
            return;
        }

        var files = new DirectoryInfo(archiveDirectory)
            .EnumerateFiles("*.anlabel", SearchOption.TopDirectoryOnly)
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .ThenByDescending(file => file.Name, StringComparer.Ordinal)
            .ToArray();
        foreach (var stale in files.Skip(retentionCount))
        {
            cancellationToken.ThrowIfCancellationRequested();
            // The directory is derived from the explicit primary path and the
            // glob is restricted to archive documents; no broad cleanup is
            // attempted here.
            stale.Delete();
        }
    }

    private static async Task<ProjectRevisionArchiveArtifact> CopyAndHashDurablyAsync(
        string sourcePath,
        string temporaryPath,
        CancellationToken cancellationToken)
    {
        await using var source = new FileStream(
            sourcePath,
            new FileStreamOptions
            {
                Mode = FileMode.Open,
                Access = FileAccess.Read,
                Share = FileShare.Read,
                BufferSize = 64 * 1024,
                Options = FileOptions.Asynchronous | FileOptions.SequentialScan
            });
        await using var destination = new FileStream(
            temporaryPath,
            new FileStreamOptions
            {
                Mode = FileMode.CreateNew,
                Access = FileAccess.Write,
                Share = FileShare.None,
                BufferSize = 64 * 1024,
                Options = FileOptions.Asynchronous | FileOptions.WriteThrough
            });
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = new byte[64 * 1024];
        long size = 0;
        int read;
        while ((read = await source.ReadAsync(buffer.AsMemory(), cancellationToken)) > 0)
        {
            await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            hash.AppendData(buffer, 0, read);
            size += read;
        }

        await destination.FlushAsync(cancellationToken);
        destination.Flush(flushToDisk: true);
        return new ProjectRevisionArchiveArtifact(
            Path: string.Empty,
            CreatedUtc: DateTime.MinValue,
            SizeBytes: size,
            ContentHash: Convert.ToHexString(hash.GetHashAndReset()),
            Reason: string.Empty);
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
        catch
        {
            // Preserve the original save/rollback error.
        }
    }
}

public sealed record ProjectRevisionArchiveArtifact(
    string Path,
    DateTime CreatedUtc,
    long SizeBytes,
    string ContentHash,
    string Reason);

public sealed record ProjectRevisionAuditEntry(
    DateTime TimestampUtc,
    string Event,
    string PrimaryPath,
    string? ArchivePath,
    string ContentHash,
    long SizeBytes,
    string Reason);
