using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Encodings.Web;
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
        _sources = JsonSerializer.Deserialize<List<DataSource>>(json, JsonOptions) ?? new List<DataSource>();
    }

    public void Save()
    {
        var dir = Path.GetDirectoryName(_filePath);
        if (dir is not null && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var json = JsonSerializer.Serialize(_sources, JsonOptions);
        File.WriteAllText(_filePath, json);
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