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

    [Fact]
    public void SaveLoad_PreservesGridVisibility_AndDefaultsToShown()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"anlabel-designer-prefs-{Guid.NewGuid():N}");
        var filePath = Path.Combine(directory, "designer-preferences.json");
        Directory.CreateDirectory(directory);

        try
        {
            // Files written before Xem → Lưới existed have no ShowGrid: the grid stays visible.
            File.WriteAllText(filePath, "{ \"SnapToObjects\": true, \"SnapToGrid\": false, \"GridStepMm\": 2 }");
            Assert.True(new DesignerPreferencesService(filePath).Load().ShowGrid);

            new DesignerPreferencesService(filePath).Save(new DesignerPreferences { ShowGrid = false });
            Assert.False(new DesignerPreferencesService(filePath).Load().ShowGrid);
        }
        finally
        {
            try { Directory.Delete(directory, recursive: true); } catch { }
        }
    }

    [Fact]
    public void RecentDocuments_PutsLatestFirst_OncePerPath_AndCaps()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"anlabel-recent-{Guid.NewGuid():N}");
        var filePath = Path.Combine(directory, "recent-documents.json");

        try
        {
            var service = new RecentDocumentsService(filePath);
            var first = Path.Combine(directory, "a.anlabel");
            var second = Path.Combine(directory, "b.anlabel");
            service.Add(first);
            service.Add(second);
            var list = service.Add(first.ToUpperInvariant());

            Assert.Equal(2, list.Count);
            Assert.Equal(first.ToUpperInvariant(), list[0], ignoreCase: true);
            Assert.Equal(second, new RecentDocumentsService(filePath).Load()[1], ignoreCase: true);

            for (var index = 0; index < RecentDocumentsService.Capacity + 5; index++)
            {
                service.Add(Path.Combine(directory, $"label-{index}.anlabel"));
            }

            Assert.Equal(RecentDocumentsService.Capacity, service.Load().Count);
            Assert.EndsWith($"label-{RecentDocumentsService.Capacity + 4}.anlabel", service.Load()[0]);

            var removed = service.Remove(service.Load()[0]);
            Assert.Equal(RecentDocumentsService.Capacity - 1, removed.Count);
        }
        finally
        {
            try { Directory.Delete(directory, recursive: true); } catch { }
        }
    }

    [Fact]
    public void RecentDocuments_MissingOrCorruptList_ReadsEmpty()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"anlabel-recent-{Guid.NewGuid():N}");
        var filePath = Path.Combine(directory, "recent-documents.json");

        try
        {
            Assert.Empty(new RecentDocumentsService(filePath).Load());
            Directory.CreateDirectory(directory);
            File.WriteAllText(filePath, "[not-json");
            Assert.Empty(new RecentDocumentsService(filePath).Load());
        }
        finally
        {
            try { Directory.Delete(directory, recursive: true); } catch { }
        }
    }
}
