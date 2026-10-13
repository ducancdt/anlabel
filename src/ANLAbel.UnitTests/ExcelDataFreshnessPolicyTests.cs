using ANLAbel.Core.Data;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class ExcelDataFreshnessPolicyTests
{
    [Fact]
    public void SameMtimeDifferentHashIsNotUnchanged()
    {
        var stamp = new DateTime(2026, 3, 26, 8, 0, 0, DateTimeKind.Utc);
        var known = new FileSourceIdentity(100, stamp, "AAA");
        var current = new FileSourceIdentity(100, stamp, "BBB");

        Assert.False(ExcelDataFreshnessPolicy.IsUnchanged(known, current));
        Assert.True(FileSourceIdentity.IsStale(known, current));
    }

    [Fact]
    public void MatchingContentIdentityIsUnchanged()
    {
        var stamp = new DateTime(2026, 3, 26, 8, 0, 0, DateTimeKind.Utc);
        var known = new FileSourceIdentity(42, stamp, "SAME");
        var current = new FileSourceIdentity(42, stamp, "SAME");

        Assert.True(ExcelDataFreshnessPolicy.IsUnchanged(known, current));
        Assert.False(FileSourceIdentity.IsStale(known, current));
    }

    [Fact]
    public void MissingKnownOrCurrentForcesRefresh()
    {
        var stamp = new DateTime(2026, 3, 26, 8, 0, 0, DateTimeKind.Utc);
        var identity = new FileSourceIdentity(1, stamp, "X");

        Assert.False(ExcelDataFreshnessPolicy.IsUnchanged(null, identity));
        Assert.False(ExcelDataFreshnessPolicy.IsUnchanged(identity, null));
        Assert.False(ExcelDataFreshnessPolicy.IsUnchanged(null, null));
    }

    [Fact]
    public void TryCaptureContentChangeWithPreservedTimestampRequiresRefresh()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"anlabel-freshness-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "data.csv");
        try
        {
            File.WriteAllText(path, "Part,Qty\nA,1\n");
            Assert.True(FileSourceIdentity.TryCapture(path, out var first));
            Assert.NotNull(first);

            // Preserve length+mtime window while replacing content — DB06 requires hash.
            var info = new FileInfo(path);
            var length = info.Length;
            var writeTime = info.LastWriteTimeUtc;
            File.WriteAllText(path, "Part,Qty\nB,1\n");
            File.SetLastWriteTimeUtc(path, writeTime);
            info.Refresh();
            Assert.Equal(length, info.Length);
            Assert.Equal(writeTime, info.LastWriteTimeUtc);

            Assert.True(FileSourceIdentity.TryCapture(path, out var second));
            Assert.NotNull(second);
            Assert.False(ExcelDataFreshnessPolicy.IsUnchanged(first, second));
        }
        finally
        {
            try { Directory.Delete(dir, recursive: true); } catch { /* best-effort */ }
        }
    }
}
