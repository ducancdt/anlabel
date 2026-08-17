using ANLAbel.Core.Models;
using ANLAbel.Core.Scene;
using System.Security.Cryptography;

namespace ANLAbel.Project.SaveLoad;

public enum ProjectRevisionKind
{
    Primary,
    Backup,
    Archive
}

/// <summary>
/// A small, local-first revision surface for a template's committed primary
/// file and its last-known-good backup.  It deliberately exposes validation
/// state so the UI never treats file existence as proof that a rollback source
/// is safe to use.
/// </summary>
public sealed record ProjectRevisionEntry(
    string Path,
    ProjectRevisionKind Kind,
    bool Exists,
    bool IsValid,
    long SizeBytes,
    DateTime LastWriteTimeUtc,
    string? TemplateName,
    string? DocumentHash,
    string Status,
    string? Diagnostic)
{
    public string KindText => Kind == ProjectRevisionKind.Primary
        ? "Current primary"
        : Kind == ProjectRevisionKind.Backup
            ? "Recovery backup"
            : "Archived snapshot";

    public string StatusText => Status;

    public string LastWriteText => Exists
        ? LastWriteTimeUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")
        : "—";

    public string SizeText => !Exists
        ? "—"
        : SizeBytes < 1024
            ? $"{SizeBytes} B"
            : $"{SizeBytes / 1024d:0.0} KB";

    public string DocumentHashText => string.IsNullOrWhiteSpace(DocumentHash)
        ? "—"
        : DocumentHash.Length <= 12
            ? DocumentHash
            : DocumentHash[..12];

    public bool CanRestore => (Kind == ProjectRevisionKind.Backup || Kind == ProjectRevisionKind.Archive)
        && Exists
        && IsValid;
}

public sealed record ProjectRevisionRestoreResult(
    string PrimaryPath,
    string RestoredFromPath,
    string PreviousPrimaryArchivedPath,
    string TemplateName);

public sealed record ProjectRevisionDiff(
    bool IsComparable,
    bool HasChanges,
    string PrimaryHash,
    string BackupHash,
    IReadOnlyList<string> Differences,
    string Summary)
{
    public string DetailsText => Differences.Count == 0
        ? Summary
        : Summary + Environment.NewLine + string.Join(Environment.NewLine, Differences.Select(item => $"• {item}"));
}

public sealed class ProjectRevisionService
{
    private readonly IProjectFileService _projectFileService;

    public ProjectRevisionService(IProjectFileService? projectFileService = null)
    {
        _projectFileService = projectFileService ?? new ProjectFileService();
    }

    public async Task<IReadOnlyList<ProjectRevisionEntry>> ListAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path is required.", nameof(filePath));
        }

        var primaryPath = Path.GetFullPath(filePath);
        return new[]
        {
            await InspectAsync(primaryPath, ProjectRevisionKind.Primary, cancellationToken),
            await InspectAsync(ProjectFileService.GetBackupPath(primaryPath), ProjectRevisionKind.Backup, cancellationToken)
        };
    }

    /// <summary>
    /// Lists the current pair plus the bounded hash-addressed archive.  The
    /// original <see cref="ListAsync"/> contract intentionally remains a
    /// two-slot primary/.bak view for callers that only need fast recovery.
    /// </summary>
    public async Task<IReadOnlyList<ProjectRevisionEntry>> ListAllAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        var current = await ListAsync(filePath, cancellationToken);
        var archive = await ListArchiveAsync(filePath, cancellationToken);
        return current.Concat(archive).ToArray();
    }

    public async Task<IReadOnlyList<ProjectRevisionEntry>> ListArchiveAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path is required.", nameof(filePath));
        }

        var primaryPath = Path.GetFullPath(filePath);
        var archiveDirectory = ProjectRevisionArchive.GetDirectory(primaryPath);
        if (!Directory.Exists(archiveDirectory))
        {
            return Array.Empty<ProjectRevisionEntry>();
        }

        var audit = await ProjectRevisionArchive.ReadAuditAsync(primaryPath, cancellationToken);
        var reasonByPath = audit
            .Where(entry => !string.IsNullOrWhiteSpace(entry.ArchivePath))
            .GroupBy(entry => Path.GetFullPath(entry.ArchivePath!), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Reason, StringComparer.OrdinalIgnoreCase);
        var paths = Directory.EnumerateFiles(archiveDirectory, "*.anlabel", SearchOption.TopDirectoryOnly)
            .OrderByDescending(path => File.GetLastWriteTimeUtc(path))
            .ThenByDescending(path => path, StringComparer.Ordinal)
            .ToArray();
        var entries = new List<ProjectRevisionEntry>(paths.Length);
        foreach (var path in paths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var entry = await InspectAsync(path, ProjectRevisionKind.Archive, cancellationToken);
            if (reasonByPath.TryGetValue(path, out var reason)
                && !string.IsNullOrWhiteSpace(reason))
            {
                entry = entry with { Diagnostic = $"Reason: {reason}" };
            }

            entries.Add(entry);
        }

        return entries;
    }

    public Task<IReadOnlyList<ProjectRevisionAuditEntry>> ListAuditAsync(
        string filePath,
        CancellationToken cancellationToken = default)
        => ProjectRevisionArchive.ReadAuditAsync(filePath, cancellationToken);

    public async Task<ProjectRevisionDiff> CompareAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        var entries = await ListAsync(filePath, cancellationToken);
        var primaryEntry = entries.Single(entry => entry.Kind == ProjectRevisionKind.Primary);
        var backupEntry = entries.Single(entry => entry.Kind == ProjectRevisionKind.Backup);
        if (!primaryEntry.IsValid || !backupEntry.IsValid)
        {
            return new ProjectRevisionDiff(
                IsComparable: false,
                HasChanges: false,
                PrimaryHash: primaryEntry.DocumentHash ?? string.Empty,
                BackupHash: backupEntry.DocumentHash ?? string.Empty,
                Differences: Array.Empty<string>(),
                Summary: $"Diff unavailable: primary is {primaryEntry.Status.ToLowerInvariant()} and backup is {backupEntry.Status.ToLowerInvariant()}.");
        }

        var primary = DocumentSnapshot.Capture(await _projectFileService.LoadAsync(primaryEntry.Path, cancellationToken));
        var backup = DocumentSnapshot.Capture(await _projectFileService.LoadAsync(backupEntry.Path, cancellationToken));
        var differences = new List<string>();

        Compare(differences, "Template name", primary.Name, backup.Name);
        Compare(differences, "Label size", FormatSize(primary.WidthMm, primary.HeightMm), FormatSize(backup.WidthMm, backup.HeightMm));
        Compare(differences, "Design DPI", primary.Dpi.ToString(), backup.Dpi.ToString());
        Compare(differences, "Orientation", primary.Orientation.ToString(), backup.Orientation.ToString());
        Compare(differences, "Gap / margin", FormatPair(primary.GapMm, primary.MarginMm), FormatPair(backup.GapMm, backup.MarginMm));
        Compare(differences, "Printer queue", primary.PrinterProfile.PrinterName, backup.PrinterProfile.PrinterName);
        Compare(differences, "Paper", primary.PrinterProfile.PaperName, backup.PrinterProfile.PaperName);
        Compare(differences, "Media / feed", $"{primary.PrinterProfile.MediaType} / {primary.PrinterProfile.FeedDirection}", $"{backup.PrinterProfile.MediaType} / {backup.PrinterProfile.FeedDirection}");
        Compare(differences, "Printer DPI", primary.PrinterProfile.Dpi.ToString(), backup.PrinterProfile.Dpi.ToString());
        Compare(differences, "Printer offsets / scale", FormatPrinterTransform(primary), FormatPrinterTransform(backup));
        Compare(differences, "Objects", FormatObjectSummary(primary), FormatObjectSummary(backup));
        Compare(differences, "Guides", primary.Guides.Length.ToString(), backup.Guides.Length.ToString());
        Compare(differences, "Linked data", FormatDataSummary(primary), FormatDataSummary(backup));

        var hasChanges = !string.Equals(primary.DocumentHash, backup.DocumentHash, StringComparison.Ordinal);
        if (hasChanges && differences.Count == 0)
        {
            differences.Add("Document content/style/resource identity changed (see hashes).");
        }

        return new ProjectRevisionDiff(
            IsComparable: true,
            HasChanges: hasChanges,
            PrimaryHash: primary.DocumentHash,
            BackupHash: backup.DocumentHash,
            Differences: differences,
            Summary: hasChanges
                ? "Primary and backup differ; review the fields below before rollback."
                : "Primary and backup have the same document identity.");
    }

    /// <summary>
    /// Restores only the managed .bak of <paramref name="filePath"/>.  The
    /// backup is validated before any bytes are committed.  On Windows/NTFS,
    /// File.Replace atomically puts the selected backup at the primary path
    /// and moves the previous primary into the new backup slot.  The fallback
    /// preserves the same ordering on filesystems without Replace support.
    /// </summary>
    public Task<ProjectRevisionRestoreResult> RestoreBackupAsync(
        string filePath,
        CancellationToken cancellationToken = default)
        => RestoreRevisionAsync(filePath, ProjectFileService.GetBackupPath(filePath), cancellationToken);

    public async Task<ProjectRevisionRestoreResult> RestoreRevisionAsync(
        string filePath,
        string revisionPath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path is required.", nameof(filePath));
        }

        if (string.IsNullOrWhiteSpace(revisionPath))
        {
            throw new ArgumentException("Revision path is required.", nameof(revisionPath));
        }

        var primaryPath = Path.GetFullPath(filePath);
        var sourcePath = Path.GetFullPath(revisionPath);
        var backupPath = ProjectFileService.GetBackupPath(primaryPath);
        if (!IsAllowedRestoreSource(primaryPath, sourcePath))
        {
            throw new InvalidOperationException("Only the managed backup or a local archived revision can be restored.");
        }

        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException("The selected revision does not exist.", sourcePath);
        }

        // Validate the exact source before reading its bytes.  A malformed
        // backup must never become the new primary through a UI shortcut.
        var restoredTemplate = await _projectFileService.LoadAsync(sourcePath, cancellationToken);
        var restoredBytes = await File.ReadAllBytesAsync(sourcePath, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        var directory = Path.GetDirectoryName(primaryPath)
            ?? throw new InvalidOperationException("The template path has no parent directory.");
        Directory.CreateDirectory(directory);
        var temporaryPath = Path.Combine(directory, $".{Path.GetFileName(primaryPath)}.{Guid.NewGuid():N}.rollback.tmp");
        try
        {
            await WriteDurablyAsync(temporaryPath, restoredBytes, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            // Preserve the exact current primary before the rollback commit.
            // This is intentionally done even when the primary is malformed
            // or uses a future schema: such bytes are valuable forensic
            // evidence and must not disappear behind the .bak swap.
            await ProjectRevisionArchive.ArchiveAsync(
                primaryPath,
                reason: "Rollback previous primary",
                cancellationToken);

            var sourceArtifact = new ProjectRevisionArchiveArtifact(
                sourcePath,
                DateTime.UtcNow,
                restoredBytes.LongLength,
                Convert.ToHexString(SHA256.HashData(restoredBytes)),
                "Rollback source");
            await ProjectRevisionArchive.AppendAuditAsync(
                primaryPath,
                sourceArtifact,
                eventName: "Rollback prepared",
                cancellationToken);

            if (File.Exists(primaryPath))
            {
                try
                {
                    File.Replace(temporaryPath, primaryPath, backupPath, ignoreMetadataErrors: true);
                }
                catch (PlatformNotSupportedException)
                {
                    await CommitWithoutReplaceAsync(primaryPath, backupPath, temporaryPath);
                }
                catch (NotSupportedException)
                {
                    await CommitWithoutReplaceAsync(primaryPath, backupPath, temporaryPath);
                }
            }
            else
            {
                File.Move(temporaryPath, primaryPath, overwrite: true);
            }

            // Do not pass a cancel token after the commit boundary.  Once the
            // new primary is published, report its validation rather than
            // making a successful rollback look canceled.
            _ = await _projectFileService.LoadAsync(primaryPath, CancellationToken.None);
            return new ProjectRevisionRestoreResult(
                primaryPath,
                sourcePath,
                backupPath,
                restoredTemplate.Name);
        }
        finally
        {
            TryDelete(temporaryPath);
        }
    }

    private static bool IsAllowedRestoreSource(string primaryPath, string sourcePath)
    {
        var backupPath = ProjectFileService.GetBackupPath(primaryPath);
        if (string.Equals(backupPath, sourcePath, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var archiveDirectory = ProjectRevisionArchive.GetDirectory(primaryPath);
        var relative = Path.GetRelativePath(archiveDirectory, sourcePath);
        return !Path.IsPathRooted(relative)
            && !relative.Equals("..", StringComparison.Ordinal)
            && !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            && string.Equals(Path.GetExtension(sourcePath), ".anlabel", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<ProjectRevisionEntry> InspectAsync(
        string path,
        ProjectRevisionKind kind,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            return new ProjectRevisionEntry(
                path,
                kind,
                Exists: false,
                IsValid: false,
                SizeBytes: 0,
                LastWriteTimeUtc: DateTime.MinValue,
                TemplateName: null,
                DocumentHash: null,
                Status: "Missing",
                Diagnostic: "No committed revision is present at this path.");
        }

        var info = new FileInfo(path);
        try
        {
            var template = await _projectFileService.LoadAsync(path, cancellationToken);
            var documentHash = DocumentSnapshot.Capture(template).DocumentHash;
            return new ProjectRevisionEntry(
                path,
                kind,
                Exists: true,
                IsValid: true,
                SizeBytes: info.Length,
                LastWriteTimeUtc: info.LastWriteTimeUtc,
                TemplateName: template.Name,
                DocumentHash: documentHash,
                Status: "Valid",
                Diagnostic: null);
        }
        catch (InvalidDataException exception)
        {
            var unsupported = exception.InnerException is UnsupportedProjectSchemaException;
            return new ProjectRevisionEntry(
                path,
                kind,
                Exists: true,
                IsValid: false,
                SizeBytes: info.Length,
                LastWriteTimeUtc: info.LastWriteTimeUtc,
                TemplateName: null,
                DocumentHash: null,
                Status: unsupported ? "Unsupported schema" : "Invalid",
                Diagnostic: exception.Message);
        }
        catch (IOException exception)
        {
            return new ProjectRevisionEntry(
                path,
                kind,
                Exists: true,
                IsValid: false,
                SizeBytes: info.Length,
                LastWriteTimeUtc: info.LastWriteTimeUtc,
                TemplateName: null,
                DocumentHash: null,
                Status: "Unreadable",
                Diagnostic: exception.Message);
        }
    }

    private static void Compare(List<string> differences, string label, string primary, string backup)
    {
        if (!string.Equals(primary, backup, StringComparison.Ordinal))
        {
            differences.Add($"{label}: primary '{primary}' · backup '{backup}'");
        }
    }

    private static string FormatSize(double widthMm, double heightMm)
        => $"{widthMm:0.###} × {heightMm:0.###} mm";

    private static string FormatPair(double first, double second)
        => $"{first:0.###} / {second:0.###} mm";

    private static string FormatPrinterTransform(DocumentSnapshot snapshot)
        => $"offset {snapshot.PrinterProfile.OffsetXMm:0.###},{snapshot.PrinterProfile.OffsetYMm:0.###} · scale {snapshot.PrinterProfile.ScaleX:0.###},{snapshot.PrinterProfile.ScaleY:0.###}";

    private static string FormatObjectSummary(DocumentSnapshot snapshot)
    {
        var counts = snapshot.Objects
            .GroupBy(item => item.Type)
            .OrderBy(group => group.Key.ToString(), StringComparer.Ordinal)
            .Select(group => $"{group.Key}={group.Count()}");
        return string.Join(", ", counts.DefaultIfEmpty("none"));
    }

    private static string FormatDataSummary(DocumentSnapshot snapshot)
    {
        var config = snapshot.DatabaseConfig;
        if (string.IsNullOrWhiteSpace(config.FilePath)
            && string.IsNullOrWhiteSpace(config.DataSourceId)
            && string.IsNullOrWhiteSpace(config.SheetName))
        {
            return "none";
        }

        return $"{Path.GetFileName(config.FilePath)} / {config.SheetName} / fields {config.LabelFields.Length}";
    }

    private static async Task WriteDurablyAsync(
        string path,
        byte[] bytes,
        CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            path,
            new FileStreamOptions
            {
                Mode = FileMode.CreateNew,
                Access = FileAccess.Write,
                Share = FileShare.None,
                BufferSize = 64 * 1024,
                Options = FileOptions.Asynchronous | FileOptions.WriteThrough
            });
        await stream.WriteAsync(bytes, cancellationToken);
        await stream.FlushAsync(cancellationToken);
        stream.Flush(flushToDisk: true);
    }

    private static async Task CommitWithoutReplaceAsync(
        string primaryPath,
        string backupPath,
        string temporaryPath)
    {
        // The fallback deliberately runs to completion once it starts.  The
        // selected backup bytes are already validated and a cancellation after
        // this point must not leave the backup slot half-replaced.
        await CopyFileDurablyAsync(primaryPath, backupPath, CancellationToken.None);
        File.Move(temporaryPath, primaryPath, overwrite: true);
    }

    private static async Task CopyFileDurablyAsync(
        string sourcePath,
        string destinationPath,
        CancellationToken cancellationToken)
    {
        var temporaryPath = destinationPath + $".{Guid.NewGuid():N}.tmp";
        try
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
            await using (var destination = new FileStream(
                temporaryPath,
                new FileStreamOptions
                {
                    Mode = FileMode.CreateNew,
                    Access = FileAccess.Write,
                    Share = FileShare.None,
                    BufferSize = 64 * 1024,
                    Options = FileOptions.Asynchronous | FileOptions.WriteThrough
                }))
            {
                await source.CopyToAsync(destination, 64 * 1024, cancellationToken);
                await destination.FlushAsync(cancellationToken);
                destination.Flush(flushToDisk: true);
            }

            File.Move(temporaryPath, destinationPath, overwrite: true);
        }
        finally
        {
            TryDelete(temporaryPath);
        }
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
            // Preserve the original operation error.
        }
    }
}
