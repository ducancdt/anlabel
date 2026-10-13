using System.Globalization;
using System.Text.Json;

namespace ANLAbel.Data.Preferences;

public sealed class UiPreferencesService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _filePath;

    public UiPreferencesService()
        : this(AppDataPaths.Local("ui-preferences.json"))
    {
    }

    public UiPreferencesService(string filePath)
    {
        _filePath = filePath;
    }

    public static UiLanguage DetectDefault()
    {
        return UiLanguage.Vietnamese;
    }

    public static UiLanguage Parse(string? value)
    {
        if (string.Equals(value, "vi", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, nameof(UiLanguage.Vietnamese), StringComparison.OrdinalIgnoreCase))
        {
            return UiLanguage.Vietnamese;
        }

        if (string.Equals(value, "en", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, nameof(UiLanguage.English), StringComparison.OrdinalIgnoreCase))
        {
            return UiLanguage.English;
        }

        return DetectDefault();
    }

    public static string ToStorage(UiLanguage language) => language == UiLanguage.Vietnamese ? "vi" : "en";

    public UiLanguage Load()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                return DetectDefault();
            }

            var json = File.ReadAllText(_filePath);
            var preferences = JsonSerializer.Deserialize<UiPreferences>(json, JsonOptions);
            return Parse(preferences?.Language);
        }
        catch (IOException)
        {
            return DetectDefault();
        }
        catch (UnauthorizedAccessException)
        {
            return DetectDefault();
        }
        catch (JsonException)
        {
            return DetectDefault();
        }
    }

    public void Save(UiLanguage language)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        AtomicFile.WriteAllText(_filePath, JsonSerializer.Serialize(new UiPreferences { Language = ToStorage(language) }, JsonOptions));
    }
}
