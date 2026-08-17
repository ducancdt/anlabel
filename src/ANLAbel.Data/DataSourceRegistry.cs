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
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public DataSourceRegistry()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ANLAbel",
            "data-sources.json"))
    {
    }

    public DataSourceRegistry(string filePath)
    {
        _filePath = filePath;
    }

    public IReadOnlyList<DataSource> Sources => _sources;

    public void Load()
    {
        if (!File.Exists(_filePath))
        {
            _sources = new List<DataSource>();
            return;
        }

        var json = File.ReadAllText(_filePath);
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind == JsonValueKind.Array)
        {
            // Migrate the original bare-array format in memory.  The next save
            // writes the versioned document below, without breaking existing
            // installations or hand-edited legacy registries.
            _sources = JsonSerializer.Deserialize<List<DataSource>>(document.RootElement.GetRawText(), JsonOptions)
                ?? new List<DataSource>();
            return;
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

        _sources = registry.Sources ?? new List<DataSource>();
    }

    public void Save()
    {
        var dir = Path.GetDirectoryName(_filePath);
        if (dir is not null && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var json = JsonSerializer.Serialize(new RegistryDocument
        {
            SchemaVersion = CurrentSchemaVersion,
            Sources = _sources
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
    }
}
