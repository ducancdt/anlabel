using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using ANLAbel.Core.Models;

namespace ANLAbel.Project.SaveLoad;

public sealed class ProjectFileService : IProjectFileService
{
    public const string FileFormat = "anlabel";
    public const int CurrentSchemaVersion = 2;
    public const string BackupSuffix = ".bak";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task SaveAsync(LabelTemplate template, string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(template);
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path is required.", nameof(filePath));
        }

        var fullPath = Path.GetFullPath(filePath);
        var directory = Path.GetDirectoryName(fullPath)
            ?? throw new InvalidOperationException("The template path has no parent directory.");
        Directory.CreateDirectory(directory);

        // Never stream JSON directly into the live template.  A process stop,
        // power loss, or cancellation in the middle of serialization would
        // otherwise leave a zero-byte/partial file that cannot be recovered.
        var temporaryPath = Path.Combine(directory, $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            await using (var stream = new FileStream(
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
                var envelope = new ProjectFileEnvelope(
                    FileFormat,
                    CurrentSchemaVersion,
                    template);
                await JsonSerializer.SerializeAsync(stream, envelope, JsonOptions, cancellationToken);
                await stream.FlushAsync(cancellationToken);
                stream.Flush(flushToDisk: true);
            }

            // Preserve the last committed document before replacing it.  The
            // backup is prepared in the same directory and renamed into place
            // before the new primary is committed.  A crash at either boundary
            // therefore leaves at least one complete, parseable document.
            if (File.Exists(fullPath) && await ShouldRotateBackupAsync(fullPath, cancellationToken))
            {
                // Keep a bounded, hash-addressed snapshot before rotating the
                // fast .bak slot.  If archiving fails, the live document and
                // its recovery slot are left untouched.
                await ProjectRevisionArchive.ArchiveAsync(
                    fullPath,
                    reason: "Save previous primary",
                    cancellationToken);

                var backupPath = GetBackupPath(fullPath);
                var backupTemporaryPath = Path.Combine(directory, $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.bak.tmp");
                try
                {
                    await CopyFileDurablyAsync(fullPath, backupTemporaryPath, cancellationToken);
                    File.Move(backupTemporaryPath, backupPath, overwrite: true);
                }
                finally
                {
                    TryDeleteTemporaryFile(backupTemporaryPath);
                }
            }

            // Move within the same directory so the replacement is a single
            // filesystem rename from readers' perspective.
            File.Move(temporaryPath, fullPath, overwrite: true);
        }
        catch
        {
            TryDeleteTemporaryFile(temporaryPath);
            throw;
        }
    }

    private static void TryDeleteTemporaryFile(string path)
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
            // Preserve the original save/cancellation exception.
        }
    }

    private static async Task CopyFileDurablyAsync(
        string sourcePath,
        string destinationPath,
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
            destinationPath,
            new FileStreamOptions
            {
                Mode = FileMode.CreateNew,
                Access = FileAccess.Write,
                Share = FileShare.None,
                BufferSize = 64 * 1024,
                Options = FileOptions.Asynchronous | FileOptions.WriteThrough
            });

        await source.CopyToAsync(destination, 64 * 1024, cancellationToken);
        await destination.FlushAsync(cancellationToken);
        destination.Flush(flushToDisk: true);
    }

    private async Task<bool> ShouldRotateBackupAsync(
        string existingPath,
        CancellationToken cancellationToken)
    {
        try
        {
            _ = await LoadAsync(existingPath, cancellationToken);
            return true;
        }
        catch (InvalidDataException exception)
        {
            if (IsUnsupportedSchemaError(exception))
            {
                throw new UnsupportedProjectSchemaException(
                    $"The existing template cannot be overwritten because it uses an unsupported schema: {exception.Message}",
                    exception);
            }

            // Keep an existing .bak untouched when the primary is already
            // corrupt.  Otherwise a recovery save could destroy the only
            // known-good copy with another copy of the damaged bytes.
            return false;
        }
    }

    public static string GetBackupPath(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path is required.", nameof(filePath));
        }

        return Path.GetFullPath(filePath) + BackupSuffix;
    }

    public async Task<LabelTemplate> LoadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path is required.", nameof(filePath));
        }

        await using var stream = new FileStream(
            Path.GetFullPath(filePath),
            new FileStreamOptions
            {
                Mode = FileMode.Open,
                Access = FileAccess.Read,
                Share = FileShare.Read,
                BufferSize = 64 * 1024,
                Options = FileOptions.Asynchronous | FileOptions.SequentialScan
            });
        JsonDocument document;
        try
        {
            document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("The template file contains malformed JSON.", exception);
        }

        using (document)
        {
            try
            {
                return LoadDocument(document);
            }
            catch (UnsupportedProjectSchemaException exception)
            {
                // Keep the public LoadAsync contract as InvalidDataException
                // for callers that already handle malformed documents, while
                // retaining a typed marker for the recovery path below.
                throw new InvalidDataException(exception.Message, exception);
            }
        }
    }

    public async Task<ProjectLoadResult> LoadWithRecoveryAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path is required.", nameof(filePath));
        }

        var primaryPath = Path.GetFullPath(filePath);
        try
        {
            var template = await LoadAsync(primaryPath, cancellationToken);
            return new ProjectLoadResult(template, primaryPath, false, null, null);
        }
        catch (InvalidDataException primaryException)
        {
            if (IsUnsupportedSchemaError(primaryException))
            {
                // A newer format must not be silently downgraded to an older
                // backup.  The operator needs an updated application instead.
                throw new UnsupportedProjectSchemaException(primaryException.Message, primaryException);
            }

            var backupPath = GetBackupPath(primaryPath);
            if (!File.Exists(backupPath))
            {
                throw;
            }

            try
            {
                var backupTemplate = await LoadAsync(backupPath, cancellationToken);
                return new ProjectLoadResult(
                    backupTemplate,
                    backupPath,
                    true,
                    backupPath,
                    primaryException.Message);
            }
            catch (InvalidDataException backupException)
            {
                if (IsUnsupportedSchemaError(backupException))
                {
                    throw new UnsupportedProjectSchemaException(backupException.Message, backupException);
                }

                throw new InvalidDataException(
                    $"The template and its recovery backup are both invalid. Primary: {primaryException.Message} Backup: {backupException.Message}",
                    primaryException);
            }
        }
    }

    private static bool IsUnsupportedSchemaError(InvalidDataException exception)
    {
        return exception.InnerException is UnsupportedProjectSchemaException
            || (exception.InnerException is InvalidDataException nested && IsUnsupportedSchemaError(nested));
    }

    private static LabelTemplate LoadDocument(JsonDocument document)
    {
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException("The template file root must be a JSON object.");
        }

        var root = document.RootElement;
        if (root.TryGetProperty("schemaVersion", out var schemaVersionElement))
        {
            if (schemaVersionElement.ValueKind != JsonValueKind.Number
                || !schemaVersionElement.TryGetInt32(out var schemaVersion))
            {
                throw new InvalidDataException("The template schemaVersion is invalid.");
            }

            if (schemaVersion > CurrentSchemaVersion)
            {
                throw new UnsupportedProjectSchemaException(
                    $"The template uses schema version {schemaVersion}, but this build supports up to {CurrentSchemaVersion}. Update ANLAbel before opening it.");
            }

            if (schemaVersion < 1)
            {
                throw new UnsupportedProjectSchemaException($"The template schema version {schemaVersion} is not supported.");
            }

            if (!root.TryGetProperty("format", out var formatElement)
                || formatElement.ValueKind != JsonValueKind.String
                || !string.Equals(formatElement.GetString(), FileFormat, StringComparison.Ordinal))
            {
                throw new UnsupportedProjectSchemaException("The template file format marker is missing or unsupported.");
            }

            if (!root.TryGetProperty("template", out var templateElement)
                || templateElement.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidDataException("The versioned template envelope does not contain a template object.");
            }

            return DeserializeTemplate(templateElement, "The versioned template payload is invalid.");
        }

        // Files written before the envelope was introduced contained a raw
        // LabelTemplate.  Keep this migration path deliberately explicit and
        // one-way: missing newer fields use model defaults, while a future
        // envelope version fails closed instead of being silently downgraded.
        return DeserializeTemplate(root, "The legacy template payload is invalid.");
    }

    private static LabelTemplate DeserializeTemplate(JsonElement element, string message)
    {
        try
        {
            return element.Deserialize<LabelTemplate>(JsonOptions)
                ?? throw new InvalidDataException(message);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException(message, exception);
        }
    }

    private sealed record ProjectFileEnvelope(
        [property: JsonPropertyName("format")] string Format,
        [property: JsonPropertyName("schemaVersion")] int SchemaVersion,
        [property: JsonPropertyName("template")] LabelTemplate Template);
}
