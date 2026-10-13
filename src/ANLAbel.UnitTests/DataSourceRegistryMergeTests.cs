using ANLAbel.Core.Models;
using ANLAbel.Data;
using Xunit;

namespace ANLAbel.UnitTests;

/// <summary>
/// Audit 2026-10-13: saving the shared data-source list writes this session's
/// changes on top of the file, so another window's entries survive and a list
/// that could not be read at startup is never replaced by a partial one.
/// </summary>
public sealed class DataSourceRegistryMergeTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "ANLAbel.RegistryMerge." + Guid.NewGuid().ToString("N"));

    public DataSourceRegistryMergeTests() => Directory.CreateDirectory(_directory);

    public void Dispose()
    {
        try { Directory.Delete(_directory, recursive: true); }
        catch (IOException) { }
    }

    [Fact]
    public void Two_windows_each_keep_the_source_they_added()
    {
        var path = Path.Combine(_directory, "data-sources.json");
        var first = new DataSourceRegistry(path);
        var second = new DataSourceRegistry(path);
        first.Load();
        second.Load();

        first.Upsert(Source("a", "A.xlsx"));
        first.Save();
        second.Upsert(Source("b", "B.xlsx"));
        second.Save();

        var reread = new DataSourceRegistry(path);
        reread.Load();
        Assert.Equal(new[] { "a", "b" }, reread.Sources.Select(source => source.Id).Order().ToArray());
    }

    [Fact]
    public void A_removal_in_one_window_is_not_undone_by_another_windows_save()
    {
        var path = Path.Combine(_directory, "data-sources.json");
        var seed = new DataSourceRegistry(path);
        seed.Load();
        seed.Upsert(Source("x", "X.xlsx"));
        seed.Upsert(Source("y", "Y.xlsx"));
        seed.Save();

        var first = new DataSourceRegistry(path);
        var second = new DataSourceRegistry(path);
        first.Load();
        second.Load();
        first.Remove("x");
        first.Save();
        second.GetById("y")!.Name = "Y renamed";
        second.Save();

        var reread = new DataSourceRegistry(path);
        reread.Load();
        Assert.Null(reread.GetById("x"));
        Assert.Equal("Y renamed", reread.GetById("y")!.Name);
    }

    [Fact]
    public void A_list_locked_at_startup_is_merged_later_never_replaced()
    {
        var path = Path.Combine(_directory, "data-sources.json");
        var seed = new DataSourceRegistry(path);
        seed.Load();
        seed.Upsert(Source("kept", "Kept.xlsx"));
        seed.Save();

        var registry = new DataSourceRegistry(path);
        using (new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            Assert.False(registry.LoadOrQuarantine().IsLoaded);
            registry.Upsert(Source("new", "New.xlsx"));
            Assert.ThrowsAny<IOException>(() => registry.Save());
        }

        Assert.Single(ReadIds(path), "kept");
        registry.Save();
        Assert.Equal(new[] { "kept", "new" }, ReadIds(path).Order().ToArray());
    }

    [Fact]
    public void A_list_written_by_a_newer_version_is_not_overwritten()
    {
        var path = Path.Combine(_directory, "data-sources.json");
        var registry = new DataSourceRegistry(path);
        registry.Load();
        registry.Upsert(Source("mine", "Mine.xlsx"));
        File.WriteAllText(path, "{\"SchemaVersion\": 99, \"Sources\": []}");

        Assert.ThrowsAny<IOException>(() => registry.Save());
        Assert.Contains("99", File.ReadAllText(path), StringComparison.Ordinal);
    }

    private string[] ReadIds(string path)
    {
        var registry = new DataSourceRegistry(path);
        registry.Load();
        return registry.Sources.Select(source => source.Id).ToArray();
    }

    [Fact]
    public void Members_from_a_newer_version_survive_a_save_by_this_version()
    {
        var path = Path.Combine(_directory, "data-sources.json");
        var seed = new DataSourceRegistry(path);
        seed.Load();
        seed.Upsert(Source("x", "X.xlsx"));
        seed.Save();

        // A newer ANLAbel added a member to the entry without a schema bump.
        var document = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(path))!;
        document["Sources"]![0]!["FutureSourceMember"] = "kept";
        File.WriteAllText(path, document.ToJsonString());

        var registry = new DataSourceRegistry(path);
        registry.Load();
        registry.GetById("x")!.Name = "X renamed";
        registry.Save();

        var text = File.ReadAllText(path);
        Assert.Contains("FutureSourceMember", text, StringComparison.Ordinal);
        Assert.Contains("X renamed", text, StringComparison.Ordinal);
    }

    private static DataSource Source(string id, string file) => new()
    {
        Id = id,
        Name = id,
        FilePath = Path.Combine("C:", "data", file),
        SheetName = "Data",
        HeaderRowIndex = 1
    };
}
