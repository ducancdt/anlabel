using ANLAbel.Core.Enums;
using ANLAbel.Core.Models;
using ANLAbel.Data;
using ANLAbel.Project.SaveLoad;
using Xunit;

namespace ANLAbel.UnitTests;

/// <summary>Plan 13 F03/F04/F05 building blocks below the UI.</summary>
public sealed class DocumentLifecycleFoundationTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "ANLAbel.Lifecycle." + Guid.NewGuid().ToString("N"));

    public DocumentLifecycleFoundationTests() => Directory.CreateDirectory(_root);

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); }
        catch (IOException) { }
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Non_finite_geometry_is_ignored_instead_of_poisoning_save_and_undo(double value)
    {
        var item = new LabelObject { Type = ObjectType.Rectangle, XMm = 3, YMm = 4, WidthMm = 10, HeightMm = 6, LineEndXMm = 8, LineEndYMm = 9 };

        item.XMm = value;
        item.YMm = value;
        item.WidthMm = value;
        item.HeightMm = value;
        item.LineEndXMm = value;
        item.LineEndYMm = value;

        Assert.Equal((3d, 4d, 10d, 6d, 8d, 9d), (item.XMm, item.YMm, item.WidthMm, item.HeightMm, item.LineEndXMm, item.LineEndYMm));
    }

    [Fact]
    public async Task Recovery_copy_is_a_loadable_document_without_backup_or_archive()
    {
        var path = Path.Combine(_root, "Recovered", "session-1-2.anlabel");
        var template = new LabelTemplate { Name = "Tem kho" };
        template.Objects.Add(new LabelObject { Id = "a", Type = ObjectType.Rectangle, XMm = 1, YMm = 2, WidthMm = 3, HeightMm = 4 });

        ProjectFileService.WriteRecoveryCopy(template, path);
        template.Name = "Lần hai";
        ProjectFileService.WriteRecoveryCopy(template, path);

        var loaded = await new ProjectFileService().LoadAsync(path);
        Assert.Equal("Lần hai", loaded.Name);
        Assert.Single(loaded.Objects);
        Assert.False(File.Exists(path + ProjectFileService.BackupSuffix));
        Assert.Single(Directory.GetFileSystemEntries(Path.GetDirectoryName(path)!));
    }

    [Fact]
    public void Malformed_registry_is_quarantined_and_the_session_starts_empty()
    {
        var path = Path.Combine(_root, "data-sources.json");
        File.WriteAllText(path, "{ not json");
        var registry = new DataSourceRegistry(path);

        var outcome = registry.LoadOrQuarantine(new DateTime(2026, 10, 13, 14, 32, 0));

        Assert.False(outcome.IsLoaded);
        Assert.Equal(path + ".hong-20261013-143200", outcome.QuarantinePath);
        Assert.True(File.Exists(outcome.QuarantinePath));
        Assert.False(File.Exists(path));
        Assert.Empty(registry.Sources);
    }

    [Fact]
    public void Registry_from_a_newer_version_is_quarantined_not_overwritten()
    {
        var path = Path.Combine(_root, "data-sources.json");
        File.WriteAllText(path, "{\"SchemaVersion\": 99, \"Sources\": []}");
        var registry = new DataSourceRegistry(path);

        var outcome = registry.LoadOrQuarantine();

        Assert.False(outcome.IsLoaded);
        Assert.NotNull(outcome.QuarantinePath);
        Assert.Contains("99", File.ReadAllText(outcome.QuarantinePath!));
    }

    [Fact]
    public void Locked_registry_is_left_in_place()
    {
        var path = Path.Combine(_root, "data-sources.json");
        File.WriteAllText(path, "{\"SchemaVersion\": 1, \"Sources\": []}");
        using var lockHandle = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        var registry = new DataSourceRegistry(path);

        var outcome = registry.LoadOrQuarantine();

        Assert.False(outcome.IsLoaded);
        Assert.Null(outcome.QuarantinePath);
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void Healthy_registry_loads()
    {
        var path = Path.Combine(_root, "data-sources.json");
        File.WriteAllText(path, "{\"SchemaVersion\": 1, \"Sources\": []}");

        Assert.True(new DataSourceRegistry(path).LoadOrQuarantine().IsLoaded);
    }
}
