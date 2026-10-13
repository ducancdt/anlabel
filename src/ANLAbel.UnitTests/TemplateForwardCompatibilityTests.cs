using System.Text.Json.Nodes;
using ANLAbel.Core.Models;
using ANLAbel.Project.SaveLoad;
using Xunit;

namespace ANLAbel.UnitTests;

/// <summary>
/// Audit 2026-10-13: a template written by a newer ANLAbel keeps the members
/// this version does not know, at every level, and a primary this version
/// cannot read is archived before a save replaces it.
/// </summary>
public sealed class TemplateForwardCompatibilityTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "ANLAbel.ForwardCompat." + Guid.NewGuid().ToString("N"));

    public TemplateForwardCompatibilityTests() => Directory.CreateDirectory(_directory);

    public void Dispose()
    {
        try { Directory.Delete(_directory, recursive: true); }
        catch (IOException) { }
    }

    [Fact]
    public async Task Unknown_members_survive_load_and_save_at_every_level()
    {
        var service = new ProjectFileService();
        var path = Path.Combine(_directory, "future.anlabel");
        var template = new LabelTemplate { Name = "Future", WidthMm = 50, HeightMm = 30 };
        template.Objects.Add(new LabelObject { Id = "o1", Name = "Text", Text = "A", XMm = 1, YMm = 1, WidthMm = 10, HeightMm = 5 });
        template.Guides.Add(new LabelGuide());
        template.DatabaseConfig.AvailableFields.Add(new DatabaseField { Name = "Part" });
        await service.SaveAsync(template, path);

        // Simulate a newer build adding members below the template level.
        var document = JsonNode.Parse(await File.ReadAllTextAsync(path))!;
        var saved = document["template"]!;
        saved["Objects"]![0]!["FutureObjectMember"] = "o";
        saved["Objects"]![0]!["Style"]!["FutureStyleMember"] = 2;
        saved["PrinterProfile"]!["FuturePrinterMember"] = true;
        saved["DatabaseConfig"]!["FutureDatabaseMember"] = "d";
        saved["DatabaseConfig"]!["AvailableFields"]![0]!["FutureFieldMember"] = "f";
        saved["Guides"]![0]!["FutureGuideMember"] = "g";
        await File.WriteAllTextAsync(path, document.ToJsonString());

        var loaded = await service.LoadAsync(path);
        loaded.Objects[0].Text = "B";
        await service.SaveAsync(loaded, path);

        var text = await File.ReadAllTextAsync(path);
        foreach (var member in new[] { "FutureObjectMember", "FutureStyleMember", "FuturePrinterMember", "FutureDatabaseMember", "FutureFieldMember", "FutureGuideMember" })
        {
            Assert.Contains(member, text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task A_primary_this_version_cannot_read_is_archived_before_a_save_replaces_it()
    {
        var service = new ProjectFileService();
        var path = Path.Combine(_directory, "damaged.anlabel");
        await File.WriteAllTextAsync(path, "{\"format\":\"anlabel\",\"schemaVersion\":2,\"template\":{\"Objects\":[{\"Type\":\"ShapeFromTheFuture\"}]}}");
        var damaged = await File.ReadAllBytesAsync(path);

        await service.SaveAsync(new LabelTemplate { Name = "Replacement", WidthMm = 50, HeightMm = 30 }, path);

        var archived = Directory.GetFiles(Path.GetDirectoryName(path)!, "*.anlabel", SearchOption.AllDirectories)
            .Where(file => !string.Equals(file, path, StringComparison.OrdinalIgnoreCase))
            .Select(File.ReadAllBytes)
            .ToArray();
        Assert.Contains(archived, bytes => bytes.SequenceEqual(damaged));
    }
}
