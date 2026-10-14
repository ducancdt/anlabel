using ANLAbel.App.Services;
using ANLAbel.App.ViewModels;
using ANLAbel.Core.Data;
using ANLAbel.Core.Enums;
using ANLAbel.Core.Expressions;
using ANLAbel.Core.Models;
using ANLAbel.Printing.PrinterProfiles;
using ClosedXML.Excel;

/// <summary>
/// DB03 (CORE_SOFTWARE_STANDARD §4): blank or repeated headers and renamed or
/// deleted columns are reported by position, and a binding never moves to
/// another column because of where it now sits. Before 2026-10-14 a blank
/// header became "Column3" and a repeated one "Part No_2" with no notice, so a
/// column added or swapped in the file could change what printed.
/// </summary>
internal static class DataHeaderRegression
{
    public static async Task ColumnsKnownOnlyByPositionAreReportedAndCannotPrint()
    {
        var directory = Path.Combine(Path.GetTempPath(), "anlabel-db03-gate-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, "data.xlsx");
            WriteSheet(path, ["Part No", "Lot", "Qty", "Note"], ["PN-1", "L-1", "5", "n1"], ["PN-2", "L-2", "6", "n2"]);

            var vm = new MainViewModel();
            vm.Template.WidthMm = 60;
            vm.Template.HeightMm = 40;
            AddText(vm.Template, "part", "Mã hàng", "{Part No}");
            AddText(vm.Template, "lot", "Số lô", "{Lot}");
            AddText(vm.Template, "qty", "Số lượng", "{Qty}");
            await vm.ImportExcelAsync(path, "Data", 1);
            Require(vm.StatusText == "Đã nhập 2 dòng từ data.xlsx / Data",
                "Unique headers that all exist need no note: " + vm.StatusText);

            // Rename Lot, delete Qty and insert a column in front. Bound names must
            // not slide to whatever sits in their old position.
            WriteSheet(path, ["Batch", "Part No", "Lot No", "Note"], ["B-1", "PN-1", "L-1", "n1"], ["B-2", "PN-2", "L-2", "n2"]);
            await vm.RefreshExcelDataAsync();
            Require(vm.StatusText.Contains(
                    "Dòng tiêu đề 1 không có các cột “Lot” (lần nhập trước ở cột B), “Qty” (lần nhập trước ở cột C); 2 đối tượng dùng các cột này chưa in được.",
                    StringComparison.Ordinal),
                "Renamed and deleted columns must be named with the column they had: " + vm.StatusText);
            Require(vm.PreviewRow is not null && BindingExpressionEvaluator.Evaluate("{Part No}", vm.PreviewRow) == "PN-1",
                "A unique header keeps its binding after a column is inserted in front of it.");
            Require(vm.TryBuildPrintPreviewRows(out var renamedRows, out _), "Preview rows must build.");
            var renamed = Check(vm, renamedRows);
            Require(!renamed.IsSuccess
                    && renamed.Issues.Where(issue => issue.IsBlocking).Select(issue => issue.ObjectName).Distinct().Order()
                        .SequenceEqual(new[] { "Số lô", "Số lượng" }),
                "Only the objects whose columns are gone may block: " + renamed.ToUserMessage());

            // Blank and repeated headers: Part No in A and D, nothing in C. The
            // transform reads the blank column, so its object depends on position too.
            var blankAndRepeated = Path.Combine(directory, "repeated.xlsx");
            WriteSheet(blankAndRepeated, ["Part No", "Lot", "", "Part No"], ["PN-1", "L-1", "C-1", "PN-OLD"]);
            var template = vm.Template;
            template.Objects.Clear();
            AddText(template, "first", "Mã đầu", "{Part No}");
            AddText(template, "second", "Mã sau", "{Part No_2}");
            AddText(template, "lot", "Số lô", "{Lot}");
            AddText(template, "derived", "Mã phụ", "{Code2}");
            template.DataTransforms.Add(new DataTransformDefinition("Code2", "FIELD(\"Column3\")"));
            await vm.ImportExcelAsync(blankAndRepeated, "Data", 1);
            Require(vm.StatusText == "Đã nhập 1 dòng từ repeated.xlsx / Data — Tiêu đề “Part No” lặp ở cột A, D (ANLAbel tạm gọi là “Part No”, “Part No_2”); "
                    + "cột C không có tiêu đề (ANLAbel tạm gọi là “Column3”); 3 đối tượng dùng các cột này chưa in được cho tới khi mỗi cột có tiêu đề riêng.",
                "Bound repeated and blank columns must be reported by letter: " + vm.StatusText);
            // The Properties panel and the layer list agree with the print block,
            // and a data transform output is not reported as a missing column.
            var issues = vm.BindingIssues.ToDictionary(issue => issue.ObjectName);
            Require(issues.Keys.Order().SequenceEqual(new[] { "Mã phụ", "Mã sau", "Mã đầu" }.Order()),
                "Exactly the three objects that read position-only columns have a binding issue: " + string.Join(", ", issues.Keys));
            Require(issues["Mã sau"].ErrorsSummary == "Cột D có tiêu đề “Part No” trùng với cột A (ANLAbel tạm gọi là “Part No_2”)."
                    && issues["Mã phụ"].ErrorsSummary == "Cột C không có tiêu đề (ANLAbel tạm gọi là “Column3”)."
                    && issues["Mã phụ"].MissingFields.Count == 0,
                "Each object must name its column: " + string.Join(" | ", issues.Values.Select(issue => issue.ErrorsSummary)));
            Require(template.Objects.Single(item => item.Name == "Mã sau").BindingStateDisplayText == "Cột chỉ theo vị trí: Part No_2"
                    && template.Objects.Single(item => item.Name == "Mã phụ").BindingStateDisplayText == "Cột chỉ theo vị trí: Column3"
                    && !template.Objects.Single(item => item.Name == "Số lô").HasBindingIssue,
                "The layer list must mark the objects that cannot print.");

            Require(vm.TryBuildPrintPreviewRows(out var ambiguousRows, out _), "Preview rows must build.");
            var ambiguous = Check(vm, ambiguousRows);
            var blocked = ambiguous.Issues.Where(issue => issue.IsBlocking && issue.RowNumber == 0).ToArray();
            Require(blocked.Select(issue => issue.ObjectName).Distinct().Order().SequenceEqual(new[] { "Mã phụ", "Mã sau", "Mã đầu" }.Order()),
                "Both columns named Part No and the transform over the blank column must block; Lot must not: " + ambiguous.ToUserMessage());
            var text = PreflightIssueText.Describe(ambiguous);
            Require(text.Contains("Mẫu · “Mã sau” (chữ): trường “Part No_2” lấy từ cột D, nhưng tiêu đề “Part No” lặp ở các cột A, D", StringComparison.Ordinal)
                    && text.Contains("trường “Column3” lấy từ cột C không có tiêu đề", StringComparison.Ordinal),
                "The operator must see the column letters in Vietnamese: " + text);

            // The copies column decides how many labels print; it may not be one of them either.
            template.DatabaseConfig.CopiesField = "Part No_2";
            Require(Check(vm, ambiguousRows).Issues.Any(issue => issue.IsBlocking && issue.ObjectType == "Copies" && issue.ObjectName == "Part No_2"),
                "A copies column with a repeated header must block.");
            template.DatabaseConfig.CopiesField = string.Empty;

            // Swapping the two Part No columns cannot be seen from the headers, so
            // both bindings stay blocked whatever the order is.
            WriteSheet(blankAndRepeated, ["Part No", "Lot", "", "Part No"], ["PN-OLD", "L-1", "C-1", "PN-1"]);
            await vm.RefreshExcelDataAsync();
            Require(vm.TryBuildPrintPreviewRows(out var swappedRows, out _), "Preview rows must build.");
            Require(Check(vm, swappedRows).Issues.Count(issue => issue.IsBlocking && issue.RowNumber == 0 && issue.ObjectName.StartsWith("Mã ", StringComparison.Ordinal)) == 3,
                "Swapped repeated columns must stay blocked.");

            // Unused gaps and repeats are only reported, never blocking.
            template.Objects.Clear();
            template.DataTransforms.Clear();
            AddText(template, "lot", "Số lô", "{Lot}");
            await vm.ImportExcelAsync(blankAndRepeated, "Data", 1);
            Require(vm.StatusText.EndsWith("— Dòng tiêu đề 1 để trống ở cột C; tiêu đề “Part No” lặp ở cột A, D.", StringComparison.Ordinal),
                "Unused blank and repeated headers must be named: " + vm.StatusText);
            Require(vm.TryBuildPrintPreviewRows(out var lotRows, out _) && Check(vm, lotRows).IsSuccess,
                "A label that reads only unique columns must print.");

            // Both print paths run the same check before anything is sent.
            var root = AppContext.BaseDirectory;
            while (!File.Exists(Path.Combine(root, "ANLAbel.slnx")))
            {
                root = Path.GetDirectoryName(root.TrimEnd(Path.DirectorySeparatorChar))!;
            }

            var preview = File.ReadAllText(Path.Combine(root, "src", "ANLAbel.App", "PrintPreviewWindow.Diagnostics.cs"));
            var viewModel = File.ReadAllText(Path.Combine(root, "src", "ANLAbel.App", "ViewModels", "MainViewModel.cs"));
            Require(preview.Contains("DataHeaderCheck.Append(result, _template, _excelDataView?.Table)", StringComparison.Ordinal)
                    && viewModel.Contains("DataHeaderCheck.Append(preflight, printTemplate, ExcelDataView?.Table)", StringComparison.Ordinal),
                "Print Preview and quick print must both block position-only columns.");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static PrintPreflightResult Check(MainViewModel vm, IReadOnlyList<IReadOnlyDictionary<string, string>?> rows)
        => DataHeaderCheck.Append(new PrintService().ValidateRows(vm.Template, rows), vm.Template, vm.ExcelDataView?.Table);

    private static void AddText(LabelTemplate template, string id, string name, string binding)
    {
        template.Objects.Add(new LabelObject
        {
            Id = id,
            Name = name,
            Type = ObjectType.Text,
            BindingExpression = binding,
            XMm = 2,
            YMm = 2 + template.Objects.Count * 6,
            WidthMm = 40,
            HeightMm = 5
        });
    }

    private static void WriteSheet(string path, string[] headers, params string[][] rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Data");
        for (var column = 0; column < headers.Length; column++)
        {
            sheet.Cell(1, column + 1).Value = headers[column];
        }

        for (var row = 0; row < rows.Length; row++)
        {
            for (var column = 0; column < rows[row].Length; column++)
            {
                sheet.Cell(row + 2, column + 1).Value = rows[row][column];
            }
        }

        workbook.SaveAs(path);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
