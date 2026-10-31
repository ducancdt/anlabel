using System.Data;
using ANLAbel.Core.Models;
using ANLAbel.Data.Excel;

namespace ANLAbel.App.Services;

/// <summary>
/// Merged Excel cells in the operator's words (DB04). Excel shows a merged
/// value on every row it covers but keeps it in the first cell, so the other
/// rows read as empty and their labels print a blank there (a barcode stops
/// at preflight). Rows are numbered as Print Preview lists them; the range is
/// Excel's, so the cells can be found in the workbook.
/// </summary>
public static class MergedCellsText
{
    private const int RangesListed = 2;
    private const int RowsListed = 10;

    /// <summary>
    /// The merged ranges over the columns the label reads, or over every column
    /// when <paramref name="template"/> is null; empty when there are none.
    /// </summary>
    public static string Describe(DataTable? table, LabelTemplate? template)
    {
        IReadOnlyList<ExcelMergedCells> merged = ExcelDataService.MergedCells(table);
        if (template is not null && merged.Count > 0)
        {
            var read = ColumnsRead(template, table);
            merged = merged.Where(range => range.Fields.Any(read.Contains)).ToArray();
        }

        if (merged.Count == 0)
        {
            return string.Empty;
        }

        var ranges = string.Join(", ", merged.Take(RangesListed).Select(range => $"{range.Reference} ({string.Join(", ", range.Fields.Select(field => $"“{field}”"))})"));
        var more = merged.Count > RangesListed ? $" và {merged.Count - RangesListed} vùng khác" : string.Empty;
        var rows = merged.SelectMany(range => range.EmptyRowIndexes).Distinct().Order().ToArray();
        var rowList = string.Join(", ", rows.Take(RowsListed).Select(index => index + 1)) + (rows.Length > RowsListed ? "…" : string.Empty);
        var blank = merged.Count == 1 && merged[0].Fields.Count == 1 ? $"“{merged[0].Fields[0]}”" : "các cột này";
        return $"Ô gộp {ranges}{more} trong Excel chỉ có giá trị ở ô đầu: tem của dòng {rowList} sẽ để trống {blank}. "
               + "Bỏ gộp ô và điền giá trị cho từng dòng trong Excel, lưu rồi bấm Cập nhật Excel.";
    }

    /// <summary>Columns the printed objects and the copies column read, through the functions too.</summary>
    private static HashSet<string> ColumnsRead(LabelTemplate template, DataTable? table)
    {
        var origins = DataColumnOrigin.Describe(table);
        return template.Objects
            .Where(item => item.IsVisible && !string.IsNullOrWhiteSpace(item.BindingExpression))
            .SelectMany(item => DataHeaderCheck.ColumnsRead(item.BindingExpression, origins, template.DataTransforms))
            .Concat(DataHeaderCheck.CopiesColumns(template, origins))
            .Select(read => read.Column.FieldName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}
