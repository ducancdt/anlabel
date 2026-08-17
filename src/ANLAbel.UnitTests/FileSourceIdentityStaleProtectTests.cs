using ANLAbel.Core.Data;
using Xunit;

namespace ANLAbel.UnitTests;

/// <summary>
/// L2 Excel/CSV freshness: after a snapshot is captured, a missing or locked
/// source must fail closed as stale. The 0.252 file-drop ChangedAfterClaim
/// slice does not cover this path. Drives <see cref="FileSourceIdentity.TryCapture"/>
/// and <see cref="FileSourceIdentity.IsStale"/> — the same functions preview
/// and quick-print use — not a copy of their comparison.
/// </summary>
public sealed class FileSourceIdentityStaleProtectTests
{
    [Fact]
    public void DeletedSourceAfterCaptureIsStaleAndCannotBeRecaptured()
    {
        var path = Path.Combine(Path.GetTempPath(), $"anlabel-stale-deleted-{Guid.NewGuid():N}.csv");
        File.WriteAllText(path, "SKU,Qty\nA,1\n");
        try
        {
            Assert.True(FileSourceIdentity.TryCapture(path, out var captured));
            Assert.NotNull(captured);

            File.Delete(path);

            Assert.False(FileSourceIdentity.TryCapture(path, out var missing));
            Assert.Null(missing);
            Assert.True(
                FileSourceIdentity.IsStale(captured, missing),
                "A linked Excel/CSV that disappears after the last good snapshot must be stale so print cannot use the old rows.");
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void LockedSourceAfterCaptureIsStaleAndUnchangedFileIsNot()
    {
        var path = Path.Combine(Path.GetTempPath(), $"anlabel-stale-locked-{Guid.NewGuid():N}.csv");
        File.WriteAllText(path, "SKU,Qty\nB,2\n");
        try
        {
            Assert.True(FileSourceIdentity.TryCapture(path, out var captured));
            Assert.NotNull(captured);
            Assert.True(FileSourceIdentity.TryCapture(path, out var same));
            Assert.False(
                FileSourceIdentity.IsStale(captured, same),
                "An unchanged local file must not be treated as stale.");

            using (new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                Assert.False(FileSourceIdentity.TryCapture(path, out var locked));
                Assert.Null(locked);
                Assert.True(
                    FileSourceIdentity.IsStale(captured, locked),
                    "A linked Excel/CSV locked by another process must fail closed as stale.");
            }
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
