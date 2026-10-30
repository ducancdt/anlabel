using ANLAbel.Data.Excel;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class ExcelOwnerFileTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"anl-owner-{Guid.NewGuid():N}");

    public ExcelOwnerFileTests() => Directory.CreateDirectory(_directory);

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    [Theory]
    [InlineData("Hang.xlsx", "~$Hang.xlsx")]
    [InlineData("Danh sach hang hoa.xlsx", "~$nh sach hang hoa.xlsx")]
    public void AnOwnerFileBesideTheWorkbookMeansItIsOpen(string workbook, string owner)
    {
        var path = Path.Combine(_directory, workbook);
        File.WriteAllText(path, "data");
        Assert.False(ExcelOwnerFile.IsOpenInExcel(path));

        File.WriteAllText(Path.Combine(_directory, owner), "owner");
        Assert.True(ExcelOwnerFile.IsOpenInExcel(path));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("relative.xlsx")]
    public void NoPathIsNotOpen(string? path) => Assert.False(ExcelOwnerFile.IsOpenInExcel(path));
}
