using System.IO;
using System.Text.Json;

namespace ANLAbel.App.Services;

/// <summary>
/// Persists user's last-used printer settings (printer name, paper, DPI, orientation)
/// so the next time they print or open the setup dialog, the previous selections are restored.
/// </summary>
public sealed class PrinterPreferencesService
{
    private static readonly string PreferencesDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ANLAbel");

    private static readonly string PreferencesPath = Path.Combine(PreferencesDir, "printer-preferences.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public PrinterPreferences Load()
    {
        try
        {
            if (!File.Exists(PreferencesPath))
            {
                return new PrinterPreferences();
            }

            var json = File.ReadAllText(PreferencesPath);
            return JsonSerializer.Deserialize<PrinterPreferences>(json, JsonOptions) ?? new PrinterPreferences();
        }
        catch
        {
            return new PrinterPreferences();
        }
    }

    public void Save(PrinterPreferences preferences)
    {
        try
        {
            Directory.CreateDirectory(PreferencesDir);
            var json = JsonSerializer.Serialize(preferences, JsonOptions);
            File.WriteAllText(PreferencesPath, json);
        }
        catch
        {
            // Silently fail - preferences are non-critical
        }
    }
}

public sealed class PrinterPreferences
{
    public string PrinterName { get; set; } = string.Empty;
    public string PaperName { get; set; } = string.Empty;
    public string PaperCategory { get; set; } = string.Empty;
    public int Dpi { get; set; } = 203;
    public string Orientation { get; set; } = "Portrait";
}