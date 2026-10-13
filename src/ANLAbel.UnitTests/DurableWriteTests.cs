using ANLAbel.Data;
using ANLAbel.Data.PrintLogs;
using ANLAbel.Project.SaveLoad;
using Xunit;

namespace ANLAbel.UnitTests;

/// <summary>
/// Audit 2026-10-13: settings, print history and template revisions survive a
/// crash mid-write, a file that cannot be deleted and a clock set back.
/// </summary>
public sealed class DurableWriteTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "ANLAbel.Durable." + Guid.NewGuid().ToString("N"));

    public DurableWriteTests() => Directory.CreateDirectory(_directory);

    public void Dispose()
    {
        try
        {
            foreach (var file in Directory.GetFiles(_directory, "*", SearchOption.AllDirectories))
            {
                File.SetAttributes(file, FileAttributes.Normal);
            }

            Directory.Delete(_directory, recursive: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }

    [Fact]
    public void An_atomic_write_replaces_the_file_and_leaves_no_temporary_file()
    {
        var path = Path.Combine(_directory, "settings.json");
        File.WriteAllText(path, "old");

        AtomicFile.WriteAllText(path, "{\"new\":true}");

        Assert.Equal("{\"new\":true}", File.ReadAllText(path));
        Assert.Equal(new[] { path }, Directory.GetFiles(_directory));
    }

    [Fact]
    public void A_failed_atomic_write_keeps_the_old_content_and_cleans_up()
    {
        var path = Path.Combine(_directory, "settings.json");
        File.WriteAllText(path, "old");
        File.SetAttributes(path, FileAttributes.ReadOnly);

        Assert.ThrowsAny<UnauthorizedAccessException>(() => AtomicFile.WriteAllText(path, "new"));

        Assert.Equal("old", File.ReadAllText(path));
        Assert.Equal(new[] { path }, Directory.GetFiles(_directory));
    }

    [Fact]
    public async Task A_history_line_cut_off_by_a_crash_does_not_swallow_the_next_record()
    {
        var path = Path.Combine(_directory, "print-history.csv");
        var service = new PrintLogService(path);
        await service.AppendAsync(new PrintLogEntry { TemplateName = "Mau-1", PrinterName = "P1", Quantity = "1" });
        // Power lost in the middle of the next row: no line ending.
        File.AppendAllText(path, "2026-10-13 08:00:00,PART-CUT");

        await service.AppendAsync(new PrintLogEntry { TemplateName = "Mau-2", PrinterName = "P1", Quantity = "2" });

        var (entries, diagnostics) = await service.ReadSummariesAsync();
        Assert.Empty(diagnostics);
        Assert.Equal(new[] { "Mau-1", string.Empty, "Mau-2" }, entries.Select(entry => entry.TemplateName));
        Assert.Equal("2", entries[^1].Quantity);
    }

    [Fact]
    public async Task A_history_file_held_for_a_moment_by_another_window_still_gets_the_record()
    {
        var path = Path.Combine(_directory, "print-history.csv");
        var service = new PrintLogService(path);
        await service.AppendAsync(new PrintLogEntry { TemplateName = "Mau-1", Quantity = "1" });

        Task append;
        using (new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            append = service.AppendAsync(new PrintLogEntry { TemplateName = "Mau-2", Quantity = "1" });
            await Task.Delay(150);
        }

        await append;
        var (entries, _) = await service.ReadSummariesAsync();
        Assert.Equal(new[] { "Mau-1", "Mau-2" }, entries.Select(entry => entry.TemplateName));
    }

    [Fact]
    public async Task The_revision_just_archived_survives_a_clock_set_back()
    {
        var path = Path.Combine(_directory, "label.anlabel");
        File.WriteAllText(path, "v1");
        for (var i = 0; i < 3; i++)
        {
            await ProjectRevisionArchive.ArchiveAsync(path, "test", retentionCount: 3);
        }

        // The older snapshots now look newer than "now", as after the PC clock
        // was set back.
        var archiveDirectory = ProjectRevisionArchive.GetDirectory(path);
        foreach (var file in Directory.GetFiles(archiveDirectory, "*.anlabel"))
        {
            File.SetLastWriteTimeUtc(file, DateTime.UtcNow.AddDays(1));
        }

        File.WriteAllText(path, "v2");
        var latest = await ProjectRevisionArchive.ArchiveAsync(path, "test", retentionCount: 3);

        var remaining = Directory.GetFiles(archiveDirectory, "*.anlabel");
        Assert.Contains(latest!.Path, remaining);
        Assert.Equal(3, remaining.Length);
    }

    [Fact]
    public async Task A_revision_that_cannot_be_deleted_does_not_fail_the_save()
    {
        var path = Path.Combine(_directory, "label.anlabel");
        File.WriteAllText(path, "v1");
        var first = await ProjectRevisionArchive.ArchiveAsync(path, "test", retentionCount: 1);
        File.SetLastWriteTimeUtc(first!.Path, DateTime.UtcNow.AddDays(-1));
        File.SetAttributes(first.Path, FileAttributes.ReadOnly);

        File.WriteAllText(path, "v2");
        var second = await ProjectRevisionArchive.ArchiveAsync(path, "test", retentionCount: 1);

        Assert.True(File.Exists(second!.Path));
    }
}
