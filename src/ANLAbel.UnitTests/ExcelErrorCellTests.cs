using System.IO.Compression;
using System.Xml.Linq;
using ANLAbel.Data.Excel;
using ClosedXML.Excel;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class ExcelErrorCellTests
{
    [Theory]
    [InlineData(XLError.DivisionByZero, "#DIV/0!")]
    [InlineData(XLError.NoValueAvailable, "#N/A")]
    [InlineData(XLError.CellReference, "#REF!")]
    public async Task ImportRejectsErrorCells(XLError cellError, string expectedErrorDisplay)
    {
        var path = Path.Combine(Path.GetTempPath(), $"anlabel-cellerror-{Guid.NewGuid():N}.xlsx");
        try
        {
            using (var workbook = new XLWorkbook())
            {
                var sheet = workbook.AddWorksheet("Data");
                sheet.Cell(1, 1).Value = "Code";
                sheet.Cell(2, 1).Value = "GOOD";
                sheet.Cell(3, 1).Value = cellError;
                workbook.SaveAs(path);
            }
            var error = await Assert.ThrowsAsync<ExcelDataReadException>(() =>
                new ExcelDataService().LoadSheetAsync(path, "Data"));
            Assert.Equal(ExcelDataReadError.InvalidData, error.Error);
            Assert.Contains("row 3", error.Message);
            Assert.Contains("column 1", error.Message);
            Assert.Contains($"({expectedErrorDisplay})", error.Message);
            Assert.Equal(path, error.FilePath);
        }
        finally { File.Delete(path); }
    }

    [Theory]
    [InlineData(XLError.DivisionByZero, "#DIV/0!")]
    [InlineData(XLError.NoValueAvailable, "#N/A")]
    [InlineData(XLError.CellReference, "#REF!")]
    [InlineData(XLError.NullValue, "#NULL!")]
    [InlineData(XLError.NameNotRecognized, "#NAME?")]
    [InlineData(XLError.NumberInvalid, "#NUM!")]
    public async Task PreviewRows_ReflectsCellErrorsTruthfully_DoesNotMaskAsEmpty(XLError cellError, string expectedErrorDisplay)
    {
        var path = Path.Combine(Path.GetTempPath(), $"anlabel-preview-error-{Guid.NewGuid():N}.xlsx");
        try
        {
            using (var workbook = new XLWorkbook())
            {
                var sheet = workbook.AddWorksheet("Data");
                sheet.Cell(1, 1).Value = "Code";
                sheet.Cell(2, 1).Value = "GOOD";
                sheet.Cell(3, 1).Value = cellError;
                workbook.SaveAs(path);
            }

            var service = new ExcelDataService();
            var rows = await service.PreviewRowsAsync(path, "Data", 5);

            Assert.Equal(3, rows.Count);
            Assert.Equal("Code", rows[0].Cells[0]);
            Assert.Equal("GOOD", rows[1].Cells[0]);
            Assert.Equal(expectedErrorDisplay, rows[2].Cells[0]);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task GetSheetsWithPreview_ReflectsCellErrorsInHeaderAndDataRows()
    {
        var path = Path.Combine(Path.GetTempPath(), $"anlabel-sheet-preview-error-{Guid.NewGuid():N}.xlsx");
        try
        {
            using (var workbook = new XLWorkbook())
            {
                var sheet = workbook.AddWorksheet("Errors");
                sheet.Cell(1, 1).Value = XLError.DivisionByZero;
                sheet.Cell(1, 2).Value = "ValidHeader";
                sheet.Cell(2, 1).Value = "Value1";
                sheet.Cell(2, 2).Value = XLError.NoValueAvailable;
                workbook.SaveAs(path);
            }

            var service = new ExcelDataService();
            var sheetPreviews = await service.GetSheetsWithPreviewAsync(path, 5);
            var preview = Assert.Single(sheetPreviews);
            Assert.Equal("Errors", preview.SheetName);
            Assert.Equal(2, preview.Rows.Count);
            Assert.Equal("#DIV/0!", preview.Rows[0].Cells[0]);
            Assert.Equal("ValidHeader", preview.Rows[0].Cells[1]);
            Assert.Equal("Value1", preview.Rows[1].Cells[0]);
            Assert.Equal("#N/A", preview.Rows[1].Cells[1]);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task ImportRejectsErrorCellOnHeaderRow()
    {
        var path = Path.Combine(Path.GetTempPath(), $"anlabel-header-error-{Guid.NewGuid():N}.xlsx");
        try
        {
            using (var workbook = new XLWorkbook())
            {
                var sheet = workbook.AddWorksheet("Data");
                sheet.Cell(1, 1).Value = XLError.DivisionByZero;
                sheet.Cell(1, 2).Value = "ValidHeader";
                sheet.Cell(2, 1).Value = "Value1";
                sheet.Cell(2, 2).Value = "Value2";
                workbook.SaveAs(path);
            }

            var error = await Assert.ThrowsAsync<ExcelDataReadException>(() =>
                new ExcelDataService().LoadSheetAsync(path, "Data"));
            Assert.Equal(ExcelDataReadError.InvalidData, error.Error);
            Assert.Contains("row 1", error.Message);
            Assert.Contains("column 1", error.Message);
            Assert.Contains("(#DIV/0!)", error.Message);
        }
        finally { File.Delete(path); }
    }

    [Theory]
    [InlineData("str", "\" 000123 \"", " 000123 ", " 000123 ")]
    [InlineData("str", "\"\"", "", "")]
    [InlineData("n", "120+3", "123", "000123")]
    public async Task CachedFormulaPreservesPayloadInImportAndBothPreviews(
        string cellType, string formula, string cachedValue, string expected)
    {
        var path = CreateCachedFormulaWorkbook(cellType, formula, cachedValue);
        try
        {
            var service = new ExcelDataService();
            var table = await service.LoadSheetAsync(path, "Data");
            Assert.Equal("GOOD", table.Rows[0]["Code"]);
            Assert.Equal(expected, table.Rows[1]["Code"]);
            Assert.Equal("KEEP", table.Rows[1]["Marker"]);

            var preview = await service.PreviewRowsAsync(path, "Data");
            Assert.Equal(expected, preview[2].Cells[0]);
            var combined = await service.GetSheetsWithPreviewAsync(path);
            Assert.Equal(expected, Assert.Single(combined).Rows[2].Cells[0]);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task CachedFormulaErrorRejectsImportWithSourceCoordinatesAndRemainsVisibleInPreviews()
    {
        var path = CreateCachedFormulaWorkbook("e", "1/0", "#DIV/0!");
        try
        {
            var service = new ExcelDataService();
            var error = await Assert.ThrowsAsync<ExcelDataReadException>(() =>
                service.LoadSheetAsync(path, "Data"));
            Assert.Equal(ExcelDataReadError.InvalidData, error.Error);
            Assert.Equal(path, error.FilePath);
            Assert.Equal("Data", error.SheetName);
            Assert.Contains("row 3", error.Message);
            Assert.Contains("column 1", error.Message);
            Assert.Contains("#DIV/0!", error.Message);

            var preview = await service.PreviewRowsAsync(path, "Data");
            Assert.Equal("#DIV/0!", preview[2].Cells[0]);
            var combined = await service.GetSheetsWithPreviewAsync(path);
            Assert.Equal("#DIV/0!", Assert.Single(combined).Rows[2].Cells[0]);
        }
        finally { File.Delete(path); }
    }

    private static string CreateCachedFormulaWorkbook(string cellType, string formula, string cachedValue)
    {
        var path = Path.Combine(Path.GetTempPath(), $"anlabel-formula-cache-{Guid.NewGuid():N}.xlsx");
        try
        {
            using (var workbook = new XLWorkbook())
            {
                var sheet = workbook.AddWorksheet("Data");
                sheet.Cell("A1").Value = "Code";
                sheet.Cell("B1").Value = "Marker";
                sheet.Cell("A2").Value = "GOOD";
                sheet.Cell("A3").Value = 123;
                sheet.Cell("A3").Style.NumberFormat.Format = "000000";
                sheet.Cell("B3").Value = "KEEP";
                workbook.SaveAs(path);
            }

            // Author the stored formula/cache directly: no calculation engine may
            // fill in or normalize the very cache boundary under test.
            using var archive = ZipFile.Open(path, ZipArchiveMode.Update);
            var entry = archive.GetEntry("xl/worksheets/sheet1.xml")!;
            XDocument document;
            using (var stream = entry.Open()) document = XDocument.Load(stream);
            XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            var cell = document.Descendants(ns + "c").Single(c => (string?)c.Attribute("r") == "A3");
            cell.SetAttributeValue("t", cellType);
            cell.ReplaceNodes(new XElement(ns + "f", formula), new XElement(ns + "v", cachedValue));
            using (var stream = entry.Open())
            {
                stream.SetLength(0);
                document.Save(stream);
            }
            return path;
        }
        catch
        {
            File.Delete(path);
            throw;
        }
    }
}
