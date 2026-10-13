using System.Text;
using ANLAbel.Data.Excel;
using Xunit;

namespace ANLAbel.UnitTests;

/// <summary>
/// Operator CSV import (ExcelDataService.LoadSheetAsync / PreviewRowsAsync), not the
/// automation parser. A BOM must not switch StreamReader onto a decoder that
/// replaces invalid bytes with U+FFFD.
/// </summary>
public sealed class ExcelCsvStrictEncodingTests
{
    [Fact]
    public async Task LoadSheet_Utf8Bom_PreservesVietnameseText()
    {
        var path = TempCsv();
        try
        {
            var payload = Encoding.UTF8.GetBytes("Mã,Lô\nA,Đồng\n");
            await File.WriteAllBytesAsync(path, WithUtf8Bom(payload));

            var table = await new ExcelDataService().LoadSheetAsync(path, ExcelDataService.CsvSheetName);

            Assert.Equal("Đồng", table.Rows[0]["Lô"]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task LoadSheet_Utf16Bom_PreservesFields()
    {
        var path = TempCsv();
        try
        {
            var utf16 = new UnicodeEncoding(bigEndian: false, byteOrderMark: false);
            var payload = utf16.GetBytes("Part,Qty\nA,1\n");
            await File.WriteAllBytesAsync(path, WithUtf16LeBom(payload));

            var table = await new ExcelDataService().LoadSheetAsync(path, ExcelDataService.CsvSheetName);

            Assert.Equal("A", table.Rows[0]["Part"]);
            Assert.Equal("1", table.Rows[0]["Qty"]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task LoadSheet_InvalidBytesAfterUtf8Bom_FailsClosed()
    {
        var path = TempCsv();
        try
        {
            var payload = Encoding.UTF8.GetBytes("Part,Qty\nA,");
            var bytes = WithUtf8Bom(payload).Concat(new byte[] { 0xFF, (byte)'\n' }).ToArray();
            await File.WriteAllBytesAsync(path, bytes);

            var error = await Assert.ThrowsAsync<ExcelDataReadException>(
                () => new ExcelDataService().LoadSheetAsync(path, ExcelDataService.CsvSheetName));

            Assert.Equal(ExcelDataReadError.InvalidData, error.Error);
            Assert.DoesNotContain("\uFFFD", error.Message);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task LoadSheet_InvalidBytesAfterUtf16Bom_FailsClosed()
    {
        var path = TempCsv();
        try
        {
            var utf16 = new UnicodeEncoding(bigEndian: false, byteOrderMark: false);
            var payload = utf16.GetBytes("Part,Qty\nA,");
            var bytes = WithUtf16LeBom(payload).Concat(new byte[] { 0x00, 0xD8 }).ToArray();
            await File.WriteAllBytesAsync(path, bytes);

            var error = await Assert.ThrowsAsync<ExcelDataReadException>(
                () => new ExcelDataService().LoadSheetAsync(path, ExcelDataService.CsvSheetName));

            Assert.Equal(ExcelDataReadError.InvalidData, error.Error);
            Assert.DoesNotContain("\uFFFD", error.Message);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task PreviewRows_InvalidBytesAfterUtf8Bom_FailsClosed()
    {
        var path = TempCsv();
        try
        {
            var payload = Encoding.UTF8.GetBytes("Part,Qty\nA,");
            var bytes = WithUtf8Bom(payload).Concat(new byte[] { 0xFF }).ToArray();
            await File.WriteAllBytesAsync(path, bytes);

            var error = await Assert.ThrowsAsync<ExcelDataReadException>(
                () => new ExcelDataService().PreviewRowsAsync(path, ExcelDataService.CsvSheetName, 10));

            Assert.Equal(ExcelDataReadError.InvalidData, error.Error);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string TempCsv()
        => Path.Combine(Path.GetTempPath(), $"anlabel-csv-encoding-{Guid.NewGuid():N}.csv");

    private static byte[] WithUtf8Bom(byte[] payload)
        => new byte[] { 0xEF, 0xBB, 0xBF }.Concat(payload).ToArray();

    private static byte[] WithUtf16LeBom(byte[] payload)
        => new byte[] { 0xFF, 0xFE }.Concat(payload).ToArray();
}
