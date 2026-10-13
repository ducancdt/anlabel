using ANLAbel.Core.Data;
using Xunit;

namespace ANLAbel.UnitTests;

/// <summary>Plan 13 H15: an Excel file that is open in Excel is not "changed".</summary>
public sealed class FileSourceIdentitySharingTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), "ANLAbel.Identity." + Guid.NewGuid().ToString("N") + ".xlsx");

    public FileSourceIdentitySharingTests() => File.WriteAllBytes(_path, [1, 2, 3, 4, 5]);

    public void Dispose()
    {
        try { File.Delete(_path); }
        catch (IOException) { }
    }

    [Fact]
    public void Identity_is_captured_while_another_program_holds_the_file_open_for_writing()
    {
        Assert.True(FileSourceIdentity.TryCapture(_path, out var before));

        // Excel keeps an open workbook with write access and read sharing.
        using (new FileStream(_path, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
        {
            Assert.True(FileSourceIdentity.TryCapture(_path, out var whileOpen));
            Assert.False(FileSourceIdentity.IsStale(before, whileOpen));
        }
    }

    [Fact]
    public void Changed_content_is_still_stale()
    {
        Assert.True(FileSourceIdentity.TryCapture(_path, out var before));
        File.WriteAllBytes(_path, [9, 9, 9, 9, 9, 9]);

        Assert.True(FileSourceIdentity.TryCapture(_path, out var after));
        Assert.True(FileSourceIdentity.IsStale(before, after));
    }
}
