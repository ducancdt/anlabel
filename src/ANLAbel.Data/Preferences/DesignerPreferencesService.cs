using System.Text.Json;

namespace ANLAbel.Data.Preferences;

/// <summary>
/// Persists lightweight designer interaction choices independently from label
/// templates, so opening a template never changes the user's workspace behavior.
/// </summary>
public sealed class DesignerPreferencesService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _filePath;

    public DesignerPreferencesService()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ANLAbel",
            "designer-preferences.json"))
    {
    }

    public DesignerPreferencesService(string filePath)
    {
        _filePath = filePath;
    }

    public DesignerPreferences Load()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                return new DesignerPreferences();
            }

            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<DesignerPreferences>(json, JsonOptions)
                ?? new DesignerPreferences();
        }
        catch (IOException)
        {
            return new DesignerPreferences();
        }
        catch (UnauthorizedAccessException)
        {
            return new DesignerPreferences();
        }
        catch (JsonException)
        {
            return new DesignerPreferences();
        }
    }

    public void Save(DesignerPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);

        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var temporaryPath = _filePath + ".tmp";
        var json = JsonSerializer.Serialize(preferences, JsonOptions);
        File.WriteAllText(temporaryPath, json);
        File.Move(temporaryPath, _filePath, overwrite: true);
    }
}
