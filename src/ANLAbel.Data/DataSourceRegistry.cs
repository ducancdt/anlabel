using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Encodings.Web;
using System.Text;
using ANLAbel.Core.Models;

namespace ANLAbel.Data;

/// <summary>
/// Machine-wide registry of <see cref="DataSource"/> entries stored at
/// <c>%AppData%\ANLAbel\data-sources.json</c>. Templates reference data sources
/// by <see cref="DataSource.Id"/> so that moving or renaming the Excel file only
/// requires one re-link in the registry instead of editing every template.
/// </summary>
public sealed class DataSourceRegistry
{
    private const int CurrentSchemaVersion = 1;
    private readonly string _filePath;
    private List<DataSource> _sources = new();
    // What each entry looked like when it was read or last written, so a save
    // can tell this session's changes from entries another window changed.
    private Dictionary<string, string> _persisted = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _removedIds = new(StringComparer.OrdinalIgnoreCase);
    private bool _unreadableAtLoad;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public DataSourceRegistry()
        : this(AppDataPaths.Roaming("data-sources.json"))
    {
    }

    public DataSourceRegistry(string filePath)
    {
        _filePath = filePath;
    }

    public IReadOnlyList<DataSource> Sources => _sources;

    public void Load()
    {
        _sources = ReadSources();
        _persisted = Snapshot(_sources);
        _removedIds.Clear();
        _unreadableAtLoad = false;
    }

    private List<DataSource> ReadSources()
    {
        if (!File.Exists(_filePath))
        {
            return new List<DataSource>();
        }

        var json = File.ReadAllText(_filePath);
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind == JsonValueKind.Array)
        {
            // Migrate the original bare-array format in memory.  The next save
            // writes the versioned document below, without breaking existing
            // installations or hand-edited legacy registries.
            return JsonSerializer.Deserialize<List<DataSource>>(document.RootElement.GetRawText(), JsonOptions)
                ?? new List<DataSource>();
        }

        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException("The data-source registry root must be an object or an array.");
        }

        var registry = JsonSerializer.Deserialize<RegistryDocument>(document.RootElement.GetRawText(), JsonOptions)
            ?? throw new InvalidDataException("The data-source registry is empty or invalid.");
        if (registry.SchemaVersion > CurrentSchemaVersion)
        {
            throw new InvalidDataException($"The data-source registry requires a newer ANLAbel version (schema {registry.SchemaVersion}).");
        }

        return registry.Sources ?? new List<DataSource>();
    }

    /// <summary>
    /// Startup-safe load (plan 13 F05). A malformed registry, or one written by
    /// a newer ANLAbel, is moved aside as <c>data-sources.json.hong-&lt;time&gt;</c>
    /// and the registry starts empty, so the application still opens. A
    /// registry that cannot be read right now (locked, no access) is left in
    /// place and the registry starts empty for this session.
    /// </summary>
    public DataSourceRegistryLoadOutcome LoadOrQuarantine(DateTime? now = null)
    {
        try
        {
            Load();
            return DataSourceRegistryLoadOutcome.Loaded;
        }
        catch (Exception exception) when (exception is JsonException or InvalidDataException or NotSupportedException)
        {
            _sources = new List<DataSource>();
            var stamp = (now ?? DateTime.Now).ToString("yyyyMMdd-HHmmss");
            var quarantinePath = $"{_filePath}.hong-{stamp}";
            for (var counter = 1; File.Exists(quarantinePath); counter++)
            {
                quarantinePath = $"{_filePath}.hong-{stamp}-{counter}";
            }

            try
            {
                File.Move(_filePath, quarantinePath);
                return new DataSourceRegistryLoadOutcome(false, quarantinePath, exception.Message);
            }
            catch (Exception moveException) when (moveException is IOException or UnauthorizedAccessException)
            {
                return new DataSourceRegistryLoadOutcome(false, null, exception.Message);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            _sources = new List<DataSource>();
            _persisted = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _unreadableAtLoad = true;
            return new DataSourceRegistryLoadOutcome(false, null, exception.Message);
        }
    }

    /// <summary>
    /// Writes this session's changes on top of what the file holds now. Entries
    /// added or edited by another ANLAbel window stay, and a registry that could
    /// not be read at startup is never replaced by the partial in-memory list:
    /// if it still cannot be read, nothing is written and the error is raised.
    /// </summary>
    public void Save()
    {
        var changed = _sources
            .Where(source => !_persisted.TryGetValue(source.Id, out var before) || before != Serialize(source))
            .ToList();
        var removed = _removedIds.ToList();
        List<DataSource> merged;
        if (File.Exists(_filePath))
        {
            try
            {
                merged = ReadSources();
            }
            catch (Exception exception) when (exception is InvalidDataException or NotSupportedException)
            {
                // Written by a newer ANLAbel, or not a registry at all: never overwrite it.
                throw new IOException("The shared data-source list was not saved because the file on disk cannot be read by this version.", exception);
            }
            catch (JsonException exception)
            {
                if (_unreadableAtLoad)
                {
                    throw new IOException("The shared data-source list is damaged and was not overwritten.", exception);
                }

                // Damaged since this session read it: keep the damaged bytes aside
                // and write this session's full list, the best copy left.
                File.Copy(_filePath, $"{_filePath}.hong-{DateTime.Now:yyyyMMdd-HHmmss}", overwrite: true);
                merged = _sources.ToList();
            }
        }
        else
        {
            merged = _unreadableAtLoad ? new List<DataSource>() : _sources.ToList();
        }

        merged.RemoveAll(source => removed.Contains(source.Id, StringComparer.OrdinalIgnoreCase));
        foreach (var source in changed)
        {
            merged.RemoveAll(existing => string.Equals(existing.Id, source.Id, StringComparison.OrdinalIgnoreCase));
            merged.Add(source);
        }

        Write(merged);
        _sources = merged;
        _persisted = Snapshot(merged);
        _removedIds.Clear();
        _unreadableAtLoad = false;
    }

    private static string Serialize(DataSource source) => JsonSerializer.Serialize(source, JsonOptions);

    private static Dictionary<string, string> Snapshot(IEnumerable<DataSource> sources)
    {
        var snapshot = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var source in sources)
        {
            snapshot[source.Id] = Serialize(source);
        }

        return snapshot;
    }

    private void Write(List<DataSource> sources)
    {
        var dir = Path.GetDirectoryName(_filePath);
        if (dir is not null && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var json = JsonSerializer.Serialize(new RegistryDocument
        {
            SchemaVersion = CurrentSchemaVersion,
            Sources = sources
        }, JsonOptions);
        var temporaryPath = Path.Combine(
            dir ?? Environment.CurrentDirectory,
            $".{Path.GetFileName(_filePath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            using (var stream = new FileStream(
                temporaryPath,
                new FileStreamOptions
                {
                    Mode = FileMode.CreateNew,
                    Access = FileAccess.Write,
                    Share = FileShare.None,
                    BufferSize = 16 * 1024,
                    Options = FileOptions.WriteThrough
                }))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)))
            {
                writer.Write(json);
                writer.Flush();
                stream.Flush(flushToDisk: true);
            }

            File.Move(temporaryPath, _filePath, overwrite: true);
        }
        catch
        {
            try
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
            catch
            {
                // Preserve the original save exception.
            }

            throw;
        }
    }

    private sealed class RegistryDocument
    {
        public int SchemaVersion { get; set; } = CurrentSchemaVersion;
        public List<DataSource> Sources { get; set; } = new();
    }

    public DataSource? GetById(string id)
    {
        return _sources.FirstOrDefault(s => string.Equals(s.Id, id, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Adds or updates a data source. If an entry with the same <see cref="DataSource.Id"/>
    /// exists, its properties are updated; otherwise a new entry is appended.
    /// </summary>
    public void Upsert(DataSource source)
    {
        var existing = _sources.FirstOrDefault(s => string.Equals(s.Id, source.Id, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            existing.Name = source.Name;
            existing.FilePath = source.FilePath;
            existing.SheetName = source.SheetName;
            existing.HeaderRowIndex = source.HeaderRowIndex;
        }
        else
        {
            _sources.Add(source);
        }
    }

    public void Remove(string id)
    {
        _sources.RemoveAll(s => string.Equals(s.Id, id, StringComparison.OrdinalIgnoreCase));
        _removedIds.Add(id);
    }
}

/// <summary>Result of <see cref="DataSourceRegistry.LoadOrQuarantine"/>.</summary>
/// <param name="IsLoaded">True when the saved registry was read.</param>
/// <param name="QuarantinePath">Where an unreadable registry was moved, if it was moved.</param>
/// <param name="Problem">Technical reason when the registry was not read.</param>
public sealed record DataSourceRegistryLoadOutcome(bool IsLoaded, string? QuarantinePath, string? Problem)
{
    public static DataSourceRegistryLoadOutcome Loaded { get; } = new(true, null, null);
}
