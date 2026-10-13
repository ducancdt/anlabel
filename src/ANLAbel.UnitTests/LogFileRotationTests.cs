using ANLAbel.Data;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class LogFileRotationTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "ANLAbel.LogRotation." + Guid.NewGuid().ToString("N"));

    public LogFileRotationTests() => Directory.CreateDirectory(_directory);

    public void Dispose()
    {
        try { Directory.Delete(_directory, recursive: true); }
        catch (IOException) { }
    }

    [Fact]
    public void Small_log_is_not_rotated()
    {
        var log = Path.Combine(_directory, "data-operations.jsonl");
        File.WriteAllText(log, "{\"a\":1}\n");

        Assert.Null(LogFileRotation.RotateIfNeeded(log, maxBytes: 1024));
        Assert.True(File.Exists(log));
    }

    [Fact]
    public void Missing_log_is_not_an_error()
    {
        Assert.Null(LogFileRotation.RotateIfNeeded(Path.Combine(_directory, "absent.jsonl"), maxBytes: 1));
    }

    [Fact]
    public void Full_log_is_archived_with_its_content_and_the_live_name_is_freed()
    {
        var log = Path.Combine(_directory, "data-operations.jsonl");
        File.WriteAllText(log, new string('x', 2048));

        var archive = LogFileRotation.RotateIfNeeded(log, maxBytes: 1024, now: new DateTime(2026, 10, 13, 15, 0, 0));

        Assert.NotNull(archive);
        Assert.False(File.Exists(log));
        Assert.Equal("data-operations.20261013-150000.jsonl", Path.GetFileName(archive));
        Assert.Equal(2048, new FileInfo(archive!).Length);
    }

    [Fact]
    public void Only_the_newest_archives_are_kept()
    {
        var log = Path.Combine(_directory, "print-operations.jsonl");
        for (var hour = 1; hour <= 4; hour++)
        {
            File.WriteAllText(log, new string('x', 2048));
            LogFileRotation.RotateIfNeeded(log, maxBytes: 1024, keepArchives: 2, now: new DateTime(2026, 10, 13, hour, 0, 0));
        }

        var archives = Directory.GetFiles(_directory, "print-operations.*.jsonl").Select(Path.GetFileName).OrderBy(name => name).ToArray();
        Assert.Equal(new[] { "print-operations.20261013-030000.jsonl", "print-operations.20261013-040000.jsonl" }, archives);
    }

    [Fact]
    public void Same_second_rotation_does_not_overwrite_an_earlier_archive()
    {
        var log = Path.Combine(_directory, "data-operations.jsonl");
        var stamp = new DateTime(2026, 10, 13, 15, 0, 0);
        File.WriteAllText(log, new string('a', 2048));
        var first = LogFileRotation.RotateIfNeeded(log, maxBytes: 1024, now: stamp);
        File.WriteAllText(log, new string('b', 2048));
        var second = LogFileRotation.RotateIfNeeded(log, maxBytes: 1024, now: stamp);

        Assert.NotEqual(first, second);
        Assert.Equal('a', File.ReadAllText(first!)[0]);
        Assert.Equal('b', File.ReadAllText(second!)[0]);
    }
}
