using ANLAbel.Data.Preferences;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class DesignerPreferencesServiceTests
{
    [Fact]
    public void SaveLoad_PreservesSnapPreference()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"anlabel-designer-prefs-{Guid.NewGuid():N}");
        var filePath = Path.Combine(directory, "designer-preferences.json");

        try
        {
            var service = new DesignerPreferencesService(filePath);
            service.Save(new DesignerPreferences { SnapToObjects = false });

            Assert.False(new DesignerPreferencesService(filePath).Load().SnapToObjects);
        }
        finally
        {
            try { Directory.Delete(directory, recursive: true); } catch { }
        }
    }

    [Fact]
    public void Load_CorruptJson_ReturnsSafeDefaults()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"anlabel-designer-prefs-{Guid.NewGuid():N}");
        var filePath = Path.Combine(directory, "designer-preferences.json");
        Directory.CreateDirectory(directory);

        try
        {
            File.WriteAllText(filePath, "{not-json");

            Assert.True(new DesignerPreferencesService(filePath).Load().SnapToObjects);
        }
        finally
        {
            try { Directory.Delete(directory, recursive: true); } catch { }
        }
    }
}
