using ANLAbel.Data.Preferences;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class UiPreferencesServiceTests
{
    [Fact]
    public void SaveLoad_PreservesVietnamese()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"anlabel-ui-prefs-{Guid.NewGuid():N}");
        var filePath = Path.Combine(directory, "ui-preferences.json");

        try
        {
            var service = new UiPreferencesService(filePath);
            service.Save(UiLanguage.Vietnamese);

            Assert.Equal(UiLanguage.Vietnamese, new UiPreferencesService(filePath).Load());
        }
        finally
        {
            try { Directory.Delete(directory, recursive: true); } catch { }
        }
    }

    [Fact]
    public void Load_CorruptJson_ReturnsDetectedDefault()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"anlabel-ui-prefs-{Guid.NewGuid():N}");
        var filePath = Path.Combine(directory, "ui-preferences.json");
        Directory.CreateDirectory(directory);

        try
        {
            File.WriteAllText(filePath, "{not-json");

            Assert.Equal(UiPreferencesService.DetectDefault(), new UiPreferencesService(filePath).Load());
        }
        finally
        {
            try { Directory.Delete(directory, recursive: true); } catch { }
        }
    }

    [Theory]
    [InlineData("vi", UiLanguage.Vietnamese)]
    [InlineData("en", UiLanguage.English)]
    [InlineData("Vietnamese", UiLanguage.Vietnamese)]
    public void Parse_AcceptsStoredLanguageCodes(string stored, UiLanguage expected)
    {
        Assert.Equal(expected, UiPreferencesService.Parse(stored));
    }
}
