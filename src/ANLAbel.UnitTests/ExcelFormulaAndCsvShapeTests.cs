using ANLAbel.Data.Excel;
using ClosedXML.Excel;
using Xunit;

namespace ANLAbel.UnitTests;

/// <summary>
/// CORE DB04 and ADR-024: a formula saved without its calculated value, or a
/// CSV record with a different field count than its header, must stop the
/// import with the exact row (and column) instead of producing blank or shifted
/// data that would print as if it were valid.
/// </summary>
public sealed class ExcelFormulaAndCsvShapeTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "ANLAbel.DataShape." + Guid.NewGuid().ToString("N"));

    public ExcelFormulaAndCsvShapeTests() => Directory.CreateDirectory(_directory);

    public void Dispose()
    {
        try { Directory.Delete(_directory, recursive: true); }
        catch (IOException) { }
    }

    [Fact]
    public async Task A_formula_without_a_calculated_value_stops_the_import_at_its_cell()
    {
        var path = CreateWorkbook(evaluateBeforeSaving: false);

        var ex = await Assert.ThrowsAsync<ExcelDataReadException>(() => new ExcelDataService().LoadSheetAsync(path, "Data"));

        Assert.Equal(ExcelDataReadError.InvalidData, ex.Error);
        Assert.Equal(3, ex.RowNumber);
        Assert.Equal(2, ex.ColumnNumber);
        Assert.Contains("formula without a calculated value", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_calculated_formula_imports_its_value()
    {
        var path = CreateWorkbook(evaluateBeforeSaving: true);

        var table = await new ExcelDataService().LoadSheetAsync(path, "Data");

        Assert.Equal("PN-002-X", table.Rows[1]["Label"]);
    }

    [Fact]
    public async Task A_csv_record_with_fewer_fields_than_the_header_is_rejected_with_its_row()
    {
        var path = Path.Combine(_directory, "short.csv");
        await File.WriteAllTextAsync(path, "Code,Lot,Qty\nA1,L1,2\nA2,3\n");

        var ex = await Assert.ThrowsAsync<ExcelDataReadException>(() =>
            new ExcelDataService().LoadSheetAsync(path, ExcelDataService.CsvSheetName));

        Assert.Equal(ExcelDataReadError.InvalidData, ex.Error);
        Assert.Equal(3, ex.RowNumber);
        Assert.Contains("Record 3 has 2 fields; the header defines 3", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Empty_lines_and_explicit_empty_fields_still_import()
    {
        var path = Path.Combine(_directory, "complete.csv");
        await File.WriteAllTextAsync(path, "Code,Lot,Qty\nA1,,2\n\nA2,L2,\n");

        var table = await new ExcelDataService().LoadSheetAsync(path, ExcelDataService.CsvSheetName);

        Assert.Equal(2, table.Rows.Count);
        Assert.Equal(string.Empty, table.Rows[0]["Lot"]);
        Assert.Equal(string.Empty, table.Rows[1]["Qty"]);
    }

    private string CreateWorkbook(bool evaluateBeforeSaving)
    {
        var path = Path.Combine(_directory, $"formula-{evaluateBeforeSaving}.xlsx");
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Data");
        sheet.Cell(1, 1).Value = "Part";
        sheet.Cell(1, 2).Value = "Label";
        sheet.Cell(2, 1).Value = "PN-001";
        sheet.Cell(2, 2).Value = "PN-001-X";
        sheet.Cell(3, 1).Value = "PN-002";
        sheet.Cell(3, 2).FormulaA1 = "A3&\"-X\"";
        workbook.SaveAs(path, new SaveOptions { EvaluateFormulasBeforeSaving = evaluateBeforeSaving });
        return path;
    }
}
