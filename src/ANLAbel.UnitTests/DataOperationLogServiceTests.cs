using ANLAbel.Data.DataLogs;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class DataOperationLogServiceTests
{
    [Fact]
    public async Task AppendWritesOneJsonLinePerEntry()
    {
        var path = Path.Combine(Path.GetTempPath(), $"anlabel-log-{Guid.NewGuid():N}.jsonl");
        try
        {
            var service = new DataOperationLogService(path);
            await service.AppendAsync(new DataOperationLogEntry { Operation = "import" });
            await service.AppendAsync(new DataOperationLogEntry { Operation = "refresh" });

            var lines = await File.ReadAllLinesAsync(path);
            Assert.Equal(2, lines.Length);
            Assert.Contains("import", lines[0]);
            Assert.Contains("refresh", lines[1]);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task ConcurrentWritesToDifferentFilesEachPersist()
    {
        var pathA = Path.Combine(Path.GetTempPath(), $"anlabel-log-{Guid.NewGuid():N}.jsonl");
        var pathB = Path.Combine(Path.GetTempPath(), $"anlabel-log-{Guid.NewGuid():N}.jsonl");
        try
        {
            var serviceA = new DataOperationLogService(pathA);
            var serviceB = new DataOperationLogService(pathB);

            var writes = new List<Task>();
            for (var i = 0; i < 25; i++)
            {
                writes.Add(serviceA.AppendAsync(new DataOperationLogEntry { Operation = "a" }));
                writes.Add(serviceB.AppendAsync(new DataOperationLogEntry { Operation = "b" }));
            }

            await Task.WhenAll(writes);

            Assert.Equal(25, (await File.ReadAllLinesAsync(pathA)).Length);
            Assert.Equal(25, (await File.ReadAllLinesAsync(pathB)).Length);
        }
        finally
        {
            File.Delete(pathA);
            File.Delete(pathB);
        }
    }

    [Fact]
    public async Task ConcurrentWritesToSameFileAreSerializedWithoutLoss()
    {
        var path = Path.Combine(Path.GetTempPath(), $"anlabel-log-{Guid.NewGuid():N}.jsonl");
        try
        {
            var writes = new List<Task>();
            for (var i = 0; i < 40; i++)
            {
                // Distinct instances pointed at the SAME path must still share a lock.
                var service = new DataOperationLogService(path);
                writes.Add(service.AppendAsync(new DataOperationLogEntry { Operation = "x" }));
            }

            await Task.WhenAll(writes);

            var lines = await File.ReadAllLinesAsync(path);
            Assert.Equal(40, lines.Length);
            Assert.All(lines, line => Assert.False(string.IsNullOrWhiteSpace(line)));
        }
        finally { File.Delete(path); }
    }
}
