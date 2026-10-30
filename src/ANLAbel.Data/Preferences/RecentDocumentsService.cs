using System.Text.Json;

namespace ANLAbel.Data.Preferences;

/// <summary>
/// The labels listed under "Tệp gần đây" on the File page (NiceLabel 10 Start and
/// Open → Recent Documents): most recent first, each path once, at most
/// <see cref="Capacity"/>. Kept apart from the templates, like the designer
/// preferences; a missing or damaged list reads as empty.
/// </summary>
public sealed class RecentDocumentsService
{
    public const int Capacity = 12;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _filePath;

    public RecentDocumentsService()
        : this(AppDataPaths.Local("recent-documents.json"))
    {
    }

    public RecentDocumentsService(string filePath)
    {
        _filePath = filePath;
    }

    public IReadOnlyList<string> Load()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                return [];
            }

            var paths = JsonSerializer.Deserialize<List<string>>(File.ReadAllText(_filePath), JsonOptions) ?? [];
            return Normalize(paths);
        }
        catch (IOException)
        {
            return [];
        }
        catch (UnauthorizedAccessException)
        {
            return [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    /// <summary>Puts <paramref name="path"/> first and returns the saved list.</summary>
    public IReadOnlyList<string> Add(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var full = Path.GetFullPath(path);
        var list = Normalize(new[] { full }.Concat(Load()));
        Save(list);
        return list;
    }

    /// <summary>Drops <paramref name="path"/> (e.g. a file that no longer exists) and returns the saved list.</summary>
    public IReadOnlyList<string> Remove(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var list = Load().Where(item => !string.Equals(item, path, StringComparison.OrdinalIgnoreCase)).ToArray();
        Save(list);
        return list;
    }

    private static IReadOnlyList<string> Normalize(IEnumerable<string> paths) =>
        paths.Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(Capacity)
            .ToArray();

    private void Save(IReadOnlyList<string> paths)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        AtomicFile.WriteAllText(_filePath, JsonSerializer.Serialize(paths, JsonOptions));
    }
}
