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
            var previewWindow = File.ReadAllText(Path.Combine(root, "src", "ANLAbel.App", "PrintPreviewWindow.xaml.cs"));
            var viewModel = File.ReadAllText(Path.Combine(root, "src", "ANLAbel.App", "ViewModels", "MainViewModel.cs"));
            // Quick print checks the table its rows were built from, captured before
            // the driver and preflight awaits during which a refresh could run.
            var dispatch = viewModel[viewModel.IndexOf("private async Task<TrackedPrintResult> DispatchTrackedPrintAsync(", StringComparison.Ordinal)..];
            var captured = dispatch.IndexOf("var headerTable = ExcelDataView?.Table;", StringComparison.Ordinal);
            Require(preview.Contains("DataHeaderCheck.Append(result, _template, _excelDataView?.Table)", StringComparison.Ordinal)
                    && captured > 0
                    && captured < dispatch.IndexOf("await ", StringComparison.Ordinal)
                    && dispatch.Contains("DataHeaderCheck.Append(preflight, printTemplate, headerTable)", StringComparison.Ordinal),
                "Print Preview and quick print must both block position-only columns, quick print on the table its rows came from.");
            Require(previewWindow.Contains("if (templateBlocked || badSourceRows.Length == 0", StringComparison.Ordinal),
                "A template-level stop must not be offered as 'skip the bad rows'.");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    /// Code review 2026-10-14 of DB03: fields are found and matched the way
    /// printing reads them, so no other spelling of a binding slips past the
    /// check, and a backslash in a formula can no longer break opening or importing.
    /// </summary>
    public static async Task PositionOnlyColumnsCannotBeReachedAnotherWay()
    {
        var directory = Path.Combine(Path.GetTempPath(), "anlabel-db03-review-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            // C blank, Qty repeated in D and F, lookalike headers Part No (A) and Part-No (E).
            var path = Path.Combine(directory, "data.xlsx");
            WriteSheet(path, ["Part No", "Lot", "", "Qty", "Part-No", "Qty"], ["PN-1", "L-1", "C-1", "5", "PN-OLD", "6"]);

            var vm = new MainViewModel();
            var template = vm.Template;
            template.WidthMm = 60;
            template.HeightMm = 60;
            AddText(template, "slash", "Gạch chéo", "FIELD(\"W\\H\")");
            AddText(template, "spaced", "Cách", "CONCAT(FIELD (\"Column3\"), \"-x\")");
            AddText(template, "computed", "Ghép tên", "FIELD(CONCAT(\"Col\", \"umn3\"))");
            template.DataTransforms.Add(new DataTransformDefinition("Code2", "FIELD(\"Column3\")"));
            AddText(template, "loose", "Tên lỏng", "{code 2}");
            AddText(template, "lookalike", "Gần giống", "{PartNo}");
            template.DataTransforms.Add(new DataTransformDefinition("Gone2", "FIELD(\"Gone\")"));
            AddText(template, "gone", "Mất cột", "{Gone2}");
            template.DatabaseConfig.CopiesField = "{Qty_2}";

            // A backslash escape the parser keeps (\H) used to throw from Regex.Unescape.
            await vm.ImportExcelAsync(path, "Data", 1);
            var issues = vm.BindingIssues.ToDictionary(issue => issue.ObjectName);

            var blocked = DataHeaderCheck.FindPositionalBindings(template, vm.ExcelDataView?.Table);
            Require(blocked.Select(issue => issue.ObjectName).Order().SequenceEqual(new[] { "Cách", "Ghép tên", "Tên lỏng", "Gần giống", "Qty_2" }.Order()),
                "Spaced and computed FIELD calls, a loosely named transform, lookalike headers and a braced copies column must all block: "
                + string.Join(" | ", blocked.Select(issue => issue.ObjectName + ": " + issue.Message)));
            Require(blocked.Single(issue => issue.ObjectName == "Gần giống").Message
                    .StartsWith("Field 'PartNo' matches columns A (Part No), E (Part-No) equally", StringComparison.Ordinal),
                "Lookalike headers must be named with their letters.");
            Require(template.Objects.Single(item => item.Name == "Gần giống").BindingExpression == "{PartNo}",
                "A name that ties between lookalike headers must not be rewritten to the first of them.");
            Require(issues.TryGetValue("Mất cột", out var gone) && gone.MissingFields.Contains("Gone")
                    && issues.TryGetValue("Gạch chéo", out var slash) && slash.MissingFields.Contains("W\\H"),
                "A transform's missing input and a field with a backslash must show as missing: "
                + string.Join(" | ", issues.Values.Select(issue => issue.ObjectName + ": " + string.Join(",", issue.MissingFields))));
            Require(vm.StatusText.Contains("khớp ngang nhau với cột A (“Part No”), cột E (“Part-No”)", StringComparison.Ordinal)
                    && vm.StatusText.Contains("cột số bản in dùng các cột này chưa in được", StringComparison.Ordinal)
                    && vm.StatusText.Contains("“Gone”", StringComparison.Ordinal),
                "The import note must name lookalikes, the copies column and a transform's missing input: " + vm.StatusText);

            // Verify, relink, open and a shared source write their own status line
            // after importing; the header note must survive it.
            WriteSheet(path, ["Part No", "Lot", "", "Qty", "Part-No", "Qty"], ["PN-2", "L-2", "C-2", "7", "PN-OLD", "8"]);
            await vm.VerifyExcelLinkAsync();
            Require(vm.StatusText.StartsWith("Đã cập nhật và xác thực Excel:", StringComparison.Ordinal)
                    && vm.StatusText.Contains("cột số bản in", StringComparison.Ordinal),
                "A verify that re-imports must keep the header note: " + vm.StatusText);

            // An advisory note about unused headers must not hide a broken link, and a
            // long run of blank headers is shortened.
            var advisoryPath = Path.Combine(directory, "advisory.xlsx");
            WriteSheet(advisoryPath, ["Part No", "Lot", "", "", "", "", "", "", "", ""], ["PN-1", "L-1", "c", "d", "e", "f", "g", "h", "i", "J-1"]);
            var advisory = new MainViewModel();
            AddText(advisory.Template, "broken", "Hỏng", "CONCAT(FIELD(\"Lot\"), FIELD(\"\"))");
            await advisory.ImportExcelAsync(advisoryPath, "Data", 1);
            Require(advisory.StatusText.EndsWith(
                    "— 1 đối tượng có liên kết bị thiếu/hỏng. Dòng tiêu đề 1 để trống ở cột C, D, E, F, G, H và 2 cột khác.",
                    StringComparison.Ordinal),
                "The broken-link count must stay next to an advisory note: " + advisory.StatusText);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    /// Second code review 2026-10-14 of DB03: a lookalike tie is caught whatever
    /// else reads the column, a binding to a data transform output or a tied name
    /// is never rewritten to a column it resembles, print rows keep the file's
    /// column order, a FIELD name built from the row's data and a blank
    /// placeholder are not missing columns, the copies field is followed through
    /// a transform, each place words a tie as a tie, and statuses written after
    /// an import keep its count of broken links.
    /// </summary>
    public static async Task TiesTransformsAndCopiesAreCheckedAsPrintingReadsThem()
    {
        var directory = Path.Combine(Path.GetTempPath(), "anlabel-db03-review2-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            // Lookalike headers Part No (A) and Part-No (E); Qty repeated in D and F.
            var path = Path.Combine(directory, "data.xlsx");
            string[] headers = ["Part No", "Lot No", "Currency", "Qty", "Part-No", "Qty", "Price_USD"];
            WriteSheet(path, headers, ["PN-1", "L-1", "USD", "5", "PN-OLD", "6", "9.50"]);

            var vm = new MainViewModel();
            var template = vm.Template;
            template.WidthMm = 60;
            template.HeightMm = 60;
            AddText(template, "both", "Cả hai", "CONCAT(FIELD(\"Part No\"), FIELD(\"PartNo\"))");
            AddText(template, "price", "Giá", "FIELD(CONCAT(\"Price_\", FIELD(\"Currency\")))");
            template.DataTransforms.Add(new DataTransformDefinition("LotNo", "CONCAT(\"LOT-\", FIELD(\"Lot No\"))"));
            AddText(template, "lot", "Số lô", "{LotNo}");
            // A label field named like column A must not pin the tied name to it.
            template.DatabaseConfig.LabelFields.Add(new DatabaseField { Name = "Part No", DisplayName = "Part No" });
            AddText(template, "tie", "Gần giống", "{PartNo}");
            template.DataTransforms.Add(new DataTransformDefinition("Copies", "FIELD(\"Qty_2\")"));
            template.DatabaseConfig.CopiesField = "Copies";
            await vm.ImportExcelAsync(path, "Data", 1);

            Require(template.Objects.Single(item => item.Name == "Gần giống").BindingExpression == "{PartNo}"
                    && template.Objects.Single(item => item.Name == "Cả hai").BindingExpression == "CONCAT(FIELD(\"Part No\"), FIELD(\"PartNo\"))"
                    && template.Objects.Single(item => item.Name == "Số lô").BindingExpression == "{LotNo}",
                "Tied names and a transform output must stay as written: "
                + string.Join(" | ", template.Objects.Select(item => item.Name + ": " + item.BindingExpression)));

            var blocked = DataHeaderCheck.FindPositionalBindings(template, vm.ExcelDataView?.Table);
            Require(blocked.Select(issue => issue.ObjectName).Order().SequenceEqual(new[] { "Cả hai", "Copies", "Gần giống" }.Order())
                    && blocked.Single(issue => issue.ObjectName == "Copies").ObjectType == "Copies",
                "The tie read after an exact read and the copies field through a transform must block; the computed name must not: "
                + string.Join(" | ", blocked.Select(issue => issue.ObjectName + ": " + issue.Message)));
            var issues = vm.BindingIssues.ToDictionary(issue => issue.ObjectName);
            Require(!issues.ContainsKey("Giá") && !issues.ContainsKey("Số lô"),
                "A FIELD name built from Currency and a transform output are not broken links: "
                + string.Join(" | ", issues.Values.Select(issue => issue.ObjectName + ": " + issue.MissingFieldsSummary + " " + issue.ErrorsSummary)));
            Require(vm.StatusText.Contains("chưa in được cho tới khi mỗi cột có tiêu đề riêng và mỗi tên chỉ khớp một cột.", StringComparison.Ordinal),
                "A note about a repeated header and a tie must ask for both fixes: " + vm.StatusText);

            // Print rows list the columns in file order, then the transform outputs.
            Require(vm.TryBuildPrintPreviewRows(out var rows, out _)
                    && rows[0] is { } first
                    && first.Keys.SequenceEqual(new[] { "Part No", "Lot No", "Currency", "Qty", "Part-No", "Qty_2", "Price_USD", "LotNo", "Copies" })
                    && BindingExpressionEvaluator.Evaluate("{LotNo}", first) == "LOT-L-1",
                "A print row must keep the order DB03 checks: " + string.Join(", ", rows.FirstOrDefault()?.Keys ?? Array.Empty<string>()));

            // Only lookalike names: the note, the status and the layer list say it is a tie.
            WriteSheet(path, ["Part No", "Lot No", "Currency", "Qty", "Part-No", "Count", "Price_USD"], ["PN-1", "L-1", "USD", "5", "PN-OLD", "6", "9.50"]);
            template.DatabaseConfig.CopiesField = string.Empty;
            template.DataTransforms.RemoveAt(1);
            await vm.RefreshExcelDataAsync();
            Require(vm.StatusText.EndsWith(
                    "— Tên “PartNo” khớp ngang nhau với cột A (“Part No”), cột E (“Part-No”); 2 đối tượng dùng các cột này chưa in được cho tới khi mỗi tên chỉ khớp một cột.",
                    StringComparison.Ordinal),
                "A tie must not ask for a header of its own: " + vm.StatusText);
            vm.SelectedObject = template.Objects.Single(item => item.Name == "Gần giống");
            Require(vm.SelectedBindingStatusText.StartsWith("Tên trong liên kết khớp ngang nhau với nhiều cột", StringComparison.Ordinal)
                    && vm.SelectedObject.BindingStateDisplayText == "Tên khớp nhiều cột: PartNo",
                $"The Properties status and the layer list must name the tie: {vm.SelectedBindingStatusText} | {vm.SelectedObject.BindingStateDisplayText}");

            // A missing placeholder written with spaces is one missing field, named without them.
            AddText(template, "spaced", "Cách", "{ Gone }");
            Require(vm.BindingIssues.Single(issue => issue.ObjectName == "Cách").MissingFields.SequenceEqual(new[] { "Gone" }),
                "{ Gone } must be one missing field: "
                + string.Join(" | ", vm.BindingIssues.Single(issue => issue.ObjectName == "Cách").MissingFields));

            // A blank placeholder is not a missing column, and a status written after
            // the import keeps its count of broken links.
            var blankPath = Path.Combine(directory, "blank.xlsx");
            WriteSheet(blankPath, ["Part No", "Lot", ""], ["PN-1", "L-1", "x"]);
            var blank = new MainViewModel();
            AddText(blank.Template, "braces", "Ngoặc trống", "Size: { }");
            await blank.ImportExcelAsync(blankPath, "Data", 1);
            const string suffix = " — 1 đối tượng có liên kết bị thiếu/hỏng. Dòng tiêu đề 1 để trống ở cột C.";
            Require(blank.StatusText.EndsWith(suffix, StringComparison.Ordinal),
                "A blank placeholder must not be named as a missing column: " + blank.StatusText);
            // It is still a broken binding, and both the designer and the print stop say why.
            var braces = blank.BindingIssues.Single();
            Require(braces.MissingFields.Count == 0
                    && braces.ErrorsSummary.StartsWith("Dấu ngoặc {} chưa có tên cột.", StringComparison.Ordinal)
                    && blank.Template.Objects.Single().BindingStateDisplayText == "Lỗi liên kết",
                $"Empty braces must be explained in the designer: {braces.ErrorsSummary} | {blank.Template.Objects.Single().BindingStateDisplayText}");
            Require(blank.TryBuildPrintPreviewRows(out var blankRows, out _), "Preview rows must build.");
            var bracesStop = PreflightIssueText.Describe(Check(blank, blankRows));
            Require(bracesStop.Contains("dấu ngoặc {} chưa có tên cột", StringComparison.Ordinal),
                "The print stop must explain empty braces: " + bracesStop);
            WriteSheet(blankPath, ["Part No", "Lot", ""], ["PN-2", "L-2", "y"]);
            await blank.VerifyExcelLinkAsync();
            Require(blank.StatusText.StartsWith("Đã cập nhật và xác thực Excel:", StringComparison.Ordinal)
                    && blank.StatusText.EndsWith(suffix, StringComparison.Ordinal),
                "A verify that re-imports must keep the broken-link count: " + blank.StatusText);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    /// Plan 13 GĐ4, the Excel import that hung on a 4 GB PC: a column with neither
    /// a header nor data is not a field. Before 2026-10-14 one stray cell in column
    /// XFD made 16,384 fields, 1.4 GB and half a minute for 1,000 rows, and the data
    /// grid then generated a column for each. A field keeps the column it came from,
    /// so a later import still names the right column when the field is gone.
    /// </summary>
    public static async Task StrayFarCellsMakeNoFieldsAndLettersStayRight()
    {
        var directory = Path.Combine(Path.GetTempPath(), "anlabel-stray-column-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            // Part in A, an empty spacer column B, Lot in C, and a stray note in XFD1.
            var path = Path.Combine(directory, "wide.xlsx");
            using (var workbook = new XLWorkbook())
            {
                var sheet = workbook.AddWorksheet("Data");
                sheet.Cell(1, 1).Value = "Part";
                sheet.Cell(1, 3).Value = "Lot";
                sheet.Cell(1, 16384).Value = "x";
                for (var row = 0; row < 1_000; row++)
                {
                    sheet.Cell(row + 2, 1).Value = $"PN-{row:0000}";
                    sheet.Cell(row + 2, 3).Value = $"L-{row % 7}";
                }

                workbook.SaveAs(path);
            }

            var vm = new MainViewModel();
            AddText(vm.Template, "lot", "Số lô", "{Lot}");
            var watch = System.Diagnostics.Stopwatch.StartNew();
            await vm.ImportExcelAsync(path, "Data", 1);
            watch.Stop();
            var fields = vm.AvailableDatabaseFields.Select(field => $"{field.Name}@{field.SourceColumn}").ToArray();
            Console.WriteLine($"MEASURE stray far cell: 1,000 rows imported in {watch.ElapsedMilliseconds} ms with {fields.Length} fields");
            Require(fields.SequenceEqual(new[] { "Part@1", "Lot@3", "x@16384" }) && vm.ExcelDataView?.Count == 1_000,
                "Only columns with a header or data may be fields: " + string.Join(", ", fields.Take(8)) + $" ({fields.Length} fields)");
            Require(watch.Elapsed < TimeSpan.FromSeconds(20), $"The import took {watch.Elapsed.TotalSeconds:0.0} s.");

            // Lot is renamed: the note names column C, where it was, not B (its
            // place in the field list once the empty spacer is not a field).
            WriteSheet(path, ["Part", "", "Batch"], ["PN-0000", "", "B-1"]);
            await vm.RefreshExcelDataAsync();
            Require(vm.StatusText.Contains("“Lot” (lần nhập trước ở cột C)", StringComparison.Ordinal),
                "A missing field must be named with the column it had: " + vm.StatusText);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static PrintPreflightResult Check(MainViewModel vm, IReadOnlyList<IReadOnlyDictionary<string, string>?> rows)
        => DataHeaderCheck.Append(new PrintService().ValidateRows(vm.Template, rows), vm.Template, vm.ExcelDataView?.Table);

    /// <summary>
    /// Opening a label without a data link kept the rows read for the label opened before: the
    /// designer filled the new label's fields from them, and "In mọi dòng" printed it once per
    /// row of that file. A label without a data link now opens without rows, as a new label does.
    /// </summary>
    public static async Task OpeningAnUnlinkedLabelDropsTheLastRows()
    {
        var directory = Path.Combine(Path.GetTempPath(), "anlabel-unlinked-gate-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, "data.xlsx");
            WriteSheet(path, ["Part No", "Lot"], ["PN-1", "L-1"], ["PN-2", "L-2"]);
            var vm = new MainViewModel();
            AddText(vm.Template, "part", "Mã hàng", "{Part No}");
            await vm.ImportExcelAsync(path, "Data", 1);
            Require(vm.ExcelDataView?.Count == 2 && vm.PrintAllRowsCommand.CanExecute(null), "The first label reads its two rows.");

            var unlinked = new LabelTemplate { Name = "Không liên kết", WidthMm = 60, HeightMm = 40 };
            AddText(unlinked, "part", "Mã hàng", "{Part No}");
            await vm.LoadTemplateFromLibraryAsync(unlinked);
            Require(vm.ExcelDataView is null && vm.ExcelHeaders.Count == 0 && vm.PreviewRow is null && !vm.PrintAllRowsCommand.CanExecute(null),
                $"A label without a data link opens without the last label's rows: {vm.ExcelDataView?.Count} rows, {vm.ExcelHeaders.Count} headers, print all {vm.PrintAllRowsCommand.CanExecute(null)}");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    /// Opening labels overlapped: a label linked to a large workbook was still reading it when
    /// another, unlinked label was opened, and the read then filled the new label with its rows
    /// and linked it to the first label's file; a label read more slowly also took the place of
    /// one opened after it. The label opened last now keeps its own data. Run on a dispatcher,
    /// as the app runs, so the awaits interleave as they do there.
    /// </summary>
    public static Task OverlappingOpensKeepTheLastLabel()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                SynchronizationContext.SetSynchronizationContext(
                    new System.Windows.Threading.DispatcherSynchronizationContext(System.Windows.Threading.Dispatcher.CurrentDispatcher));
                OverlappingOpens();
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (failure is not null)
        {
            throw failure;
        }

        return Task.CompletedTask;
    }

    private static void OverlappingOpens()
    {
        void Wait(Task task)
        {
            var frame = new System.Windows.Threading.DispatcherFrame();
            task.ContinueWith(_ => frame.Continue = false, TaskScheduler.Default);
            System.Windows.Threading.Dispatcher.PushFrame(frame);
            task.GetAwaiter().GetResult();
        }

        void Pump() => System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);
        LabelTemplate Unlinked(string name)
        {
            var template = new LabelTemplate { Name = name, WidthMm = 60, HeightMm = 40 };
            AddText(template, "part", "Mã hàng", "{Part No}");
            return template;
        }

        var directory = Path.Combine(Path.GetTempPath(), "anlabel-overlap-gate-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            // A workbook large enough that its read is still running when the next label opens.
            var big = Path.Combine(directory, "big.xlsx");
            WriteSheet(big, ["Part No", "Lot"], Enumerable.Range(1, 20000).Select(row => new[] { "PN-" + row, "L-" + row }).ToArray());
            var linked = new MainViewModel();
            linked.Template.Name = "Tem A";
            AddText(linked.Template, "part", "Mã hàng", "{Part No}");
            Wait(linked.ImportExcelAsync(big, "Data", 1));
            var labelA = Path.Combine(directory, "a.anlabel");
            Wait(linked.SaveAsync(labelA));

            // A read still running for label A does not fill label B opened meanwhile.
            var vm = new MainViewModel();
            var openA = vm.OpenAsync(labelA);
            var deadline = DateTime.UtcNow.AddSeconds(60);
            while (vm.Template.Name != "Tem A" && !openA.IsCompleted && DateTime.UtcNow < deadline)
            {
                Pump();
                Thread.Sleep(1);
            }

            if (openA.IsCompleted)
            {
                Console.WriteLine("INFO the workbook was read before the next label opened; the overlap was not reached.");
            }
            else
            {
                var b = Unlinked("Tem B");
                Wait(vm.LoadTemplateFromLibraryAsync(b));
                Wait(openA);
                Require(ReferenceEquals(vm.Template, b) && vm.ExcelDataView is null && string.IsNullOrEmpty(b.DatabaseConfig.FilePath)
                        && !vm.PrintAllRowsCommand.CanExecute(null),
                    $"Label B keeps no rows and no link of label A: {vm.Template.Name}, rows {vm.ExcelDataView?.Count}, link '{b.DatabaseConfig.FilePath}'");
            }

            // A label read more slowly does not take the place of one opened after it.
            var later = new MainViewModel();
            var slowOpen = later.OpenAsync(labelA);
            if (later.Template.Name == "Tem A")
            {
                Console.WriteLine("INFO the label file was read at once; the slower open was not reached.");
            }
            else
            {
                var c = Unlinked("Tem C");
                Wait(later.LoadTemplateFromLibraryAsync(c));
                Wait(slowOpen);
                Require(ReferenceEquals(later.Template, c) && later.ExcelDataView is null,
                    $"The label opened last stays open: {later.Template.Name}, rows {later.ExcelDataView?.Count}");
            }
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    /// A barcode bound to {MaHang} was selected while the linked workbook loaded again
    /// (Nạp lại dữ liệu, the workbook saved in Excel, a field added in the import
    /// window). Each load picked the first column in the Properties field list, and the
    /// list took that for the operator's choice: the barcode was bound to the first
    /// column and renamed, and printed it with no warning. The Source page's one source
    /// list now shows what the object prints and only a pick in it connects the object.
    /// </summary>
    public static Task ReloadingDataKeepsTheSelectedObjectsColumn()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                SynchronizationContext.SetSynchronizationContext(
                    new System.Windows.Threading.DispatcherSynchronizationContext(System.Windows.Threading.Dispatcher.CurrentDispatcher));
                ReloadKeepsTheColumn();
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (failure is not null)
        {
            throw failure;
        }

        return Task.CompletedTask;
    }

    private static void ReloadKeepsTheColumn()
    {
        void Wait(Task task)
        {
            var frame = new System.Windows.Threading.DispatcherFrame();
            task.ContinueWith(_ => frame.Continue = false, TaskScheduler.Default);
            System.Windows.Threading.Dispatcher.PushFrame(frame);
            task.GetAwaiter().GetResult();
        }

        var directory = Path.Combine(Path.GetTempPath(), "anlabel-keep-column-gate-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, "data.xlsx");
            WriteSheet(path, ["MaKien", "MaHang", "SoLuong"], ["K-1", "TERM-1", "10"]);
            var window = new ANLAbel.App.MainWindow();
            var vm = (MainViewModel)window.DataContext;
            void Settle()
            {
                window.Measure(new System.Windows.Size(1440, 900));
                window.Arrange(new System.Windows.Rect(0, 0, 1440, 900));
                window.UpdateLayout();
                System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);
            }

            vm.IsPropertiesPinned = true;
            vm.IsPropertiesVisible = true;
            Settle();
            Wait(vm.ImportExcelAsync(path, "Data", 1));
            var barcode = new LabelObject
            {
                Type = ObjectType.BarcodeCode128,
                Name = "Mã vạch mã hàng",
                BarcodeSymbology = BarcodeSymbology.Code128,
                BindingExpression = "{MaHang}",
                XMm = 3,
                YMm = 3,
                WidthMm = 40,
                HeightMm = 10
            };
            vm.Template.Objects.Add(barcode);
            vm.SelectedObject = barcode;
            Settle();
            var list = (System.Windows.Controls.ComboBox)window.FindName("DataSourceComboBox");
            Require((list.SelectedItem as DataSourceChoice)?.Binding == "{MaHang}",
                "The source list shows the column the barcode prints: " + (list.SelectedItem as DataSourceChoice)?.Label);

            Wait(vm.RefreshExcelDataAsync());
            Settle();
            Require(barcode.BindingExpression == "{MaHang}" && barcode.Name == "Mã vạch mã hàng",
                $"Reloading the data with the barcode selected must keep its column and name, it is now '{barcode.Name}' {barcode.BindingExpression}.");
            Require((list.SelectedItem as DataSourceChoice)?.Binding == "{MaHang}",
                "After the reload the source list still shows the barcode's column: " + (list.SelectedItem as DataSourceChoice)?.Label);

            // A field picked in another list (the import window's) is no choice for the selected object either.
            vm.SelectedLabelDatabaseField = vm.LabelDatabaseFields.First(field => field.Name == "SoLuong");
            Settle();
            Require(barcode.BindingExpression == "{MaHang}",
                "Selecting a field in the import window's list must not bind the selected barcode: " + barcode.BindingExpression);

            // The operator's own pick in the source list connects the object, and keeps its name.
            list.SelectedItem = list.Items.OfType<DataSourceChoice>().First(choice => choice.Binding == "{MaKien}");
            Settle();
            Require(barcode.BindingExpression == "{MaKien}" && barcode.Name == "Mã vạch mã hàng",
                $"Picking MaKien in the source list binds the barcode to it and keeps its name: '{barcode.Name}' {barcode.BindingExpression}");

            // Opening a linked label selects its top object, and the link restored after that
            // picked the first column: the top object was bound there on every open.
            var label = Path.Combine(directory, "tem.anlabel");
            var author = new MainViewModel();
            Wait(author.ImportExcelAsync(path, "Data", 1));
            author.Template.Objects.Add(new LabelObject { Type = ObjectType.Text, Name = "Số lượng", BindingExpression = "{SoLuong}", XMm = 3, YMm = 20, WidthMm = 30, HeightMm = 6, ZIndex = 9 });
            Wait(author.SaveAsync(label));
            var opener = new ANLAbel.App.MainWindow();
            var opened = (MainViewModel)opener.DataContext;
            opener.Measure(new System.Windows.Size(1440, 900));
            opener.Arrange(new System.Windows.Rect(0, 0, 1440, 900));
            opener.UpdateLayout();
            Wait(opened.OpenAsync(label));
            opener.UpdateLayout();
            System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);
            var top = opened.Template.Objects.Single(item => item.ZIndex == 9);
            Require(ReferenceEquals(opened.SelectedObject, top), "Opening selects the top object, as before.");
            Require(top.BindingExpression == "{SoLuong}" && top.Name == "Số lượng" && !opened.HasUnsavedChanges(),
                $"Opening the label must keep its top object's column and name and leave it unchanged: '{top.Name}' {top.BindingExpression}, unsaved {opened.HasUnsavedChanges()}.");
            // Not closed: closing an edited label asks whether to save it.
        }
        finally
        {
            try
            {
                Directory.Delete(directory, recursive: true);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    /// <summary>
    /// NiceLabel 10's Source page connects an object with one list ("Connected data source":
    /// fixed data, variables, functions, internal values, database fields). ANLAbel's took two
    /// steps (Nguồn, then Trường Excel) and showed an object reading a prompt, a function or the
    /// print date as "Trường Excel" with an empty field list. The one list now shows what each
    /// object prints and connects it with one pick, in one undo step, keeping its name.
    /// </summary>
    public static Task SourceListConnectsEveryKindOfSource()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                SynchronizationContext.SetSynchronizationContext(
                    new System.Windows.Threading.DispatcherSynchronizationContext(System.Windows.Threading.Dispatcher.CurrentDispatcher));
                SourceListKinds();
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (failure is not null)
        {
            throw failure;
        }

        return Task.CompletedTask;
    }

    private static void SourceListKinds()
    {
        void Wait(Task task)
        {
            var frame = new System.Windows.Threading.DispatcherFrame();
            task.ContinueWith(_ => frame.Continue = false, TaskScheduler.Default);
            System.Windows.Threading.Dispatcher.PushFrame(frame);
            task.GetAwaiter().GetResult();
        }

        var directory = Path.Combine(Path.GetTempPath(), "anlabel-source-list-gate-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, "data.xlsx");
            WriteSheet(path, ["MaKien", "MaHang", "SoLuong"], ["K-1", "TERM-1", "10"]);
            var window = new ANLAbel.App.MainWindow();
            var vm = (MainViewModel)window.DataContext;
            void Settle()
            {
                window.Measure(new System.Windows.Size(1440, 900));
                window.Arrange(new System.Windows.Rect(0, 0, 1440, 900));
                window.UpdateLayout();
                System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);
            }

            vm.IsPropertiesPinned = true;
            vm.IsPropertiesVisible = true;
            Settle();
            Wait(vm.ImportExcelAsync(path, "Data", 1));
            vm.Template.PromptVariables.Add(new PromptVariableDefinition { Name = "Lô" });
            vm.Template.DataTransforms.Add(new DataTransformDefinition("MaDongGoi", "CONCAT(FIELD(\"MaHang\"), \"/\", FIELD(\"SoLuong\"))"));
            LabelObject Add(string name, string text, string binding)
            {
                var item = new LabelObject { Type = ObjectType.Text, Name = name, Text = text, BindingExpression = binding, XMm = 3, YMm = 3 + vm.Template.Objects.Count * 6, WidthMm = 40, HeightMm = 5 };
                vm.Template.Objects.Add(item);
                return item;
            }

            var prompt = Add("Lô hàng", "", "{?Lô}");
            var function = Add("Mã đóng gói", "", "{MaDongGoi}");
            var date = Add("Ngày", "", "{#NOW:dd/MM/yyyy}");
            var fixedText = Add("Ghi chú", "Hàng mới", "");
            var composed = Add("Dòng lô", "", "Lô {?Lô} / {MaHang}");
            var missing = Add("Cột cũ", "", "{Thiếu}");
            var list = (System.Windows.Controls.ComboBox)window.FindName("DataSourceComboBox");
            DataSourceChoice Shown(LabelObject item)
            {
                vm.SelectedObject = item;
                Settle();
                return (DataSourceChoice)list.SelectedItem;
            }

            Require(Shown(prompt) is { Group: "Biến nhập khi in", Binding: "{?Lô}" }, "A prompt shows as its variable: " + Shown(prompt));
            Require(Shown(function) is { Group: "Hàm", Binding: "{MaDongGoi}" }, "A function shows as itself, not as an empty Excel field: " + Shown(function));
            Require(Shown(date) is { Group: "Khi in", Label: "Ngày in" }, "The print date shows as a value given at print time: " + Shown(date));
            Require(Shown(fixedText).Kind == DataSourceChoiceKind.Fixed, "Text of its own shows as fixed text: " + Shown(fixedText));
            Require(Shown(composed).Kind == DataSourceChoiceKind.Compose, "Text and several sources show as a composition: " + Shown(composed));
            var lacking = Shown(missing);
            Require(lacking.Group == "Trường dữ liệu" && lacking.Label.Contains("không có trong dữ liệu", StringComparison.Ordinal),
                "A column the file lacks still shows, and says so: " + lacking);

            // One pick connects the fixed text to the function, keeps its name, and undoes in one step.
            Shown(fixedText);
            list.SelectedItem = list.Items.OfType<DataSourceChoice>().Single(choice => choice.Binding == "{MaDongGoi}");
            Settle();
            Require(fixedText.BindingExpression == "{MaDongGoi}" && fixedText.Name == "Ghi chú",
                $"Picking the function connects the object and keeps its name: '{fixedText.Name}' {fixedText.BindingExpression}");
            Require((list.SelectedItem as DataSourceChoice)?.Binding == "{MaDongGoi}", "The list then shows the function.");
            vm.UndoCommand.Execute(null);
            Settle();
            // Undo restores the label: its objects are the restored ones.
            LabelObject Current(LabelObject item) => vm.Template.Objects.Single(candidate => candidate.Id == item.Id);
            fixedText = Current(fixedText);
            function = Current(function);
            Require(fixedText.BindingExpression == string.Empty && fixedText.Text == "Hàng mới", "One undo takes the pick back: " + fixedText.BindingExpression);

            // Fixed text keeps showing what the object showed.
            Shown(function);
            list.SelectedItem = list.Items.OfType<DataSourceChoice>().Single(choice => choice.Kind == DataSourceChoiceKind.Fixed);
            Settle();
            Require(function.BindingExpression == string.Empty && function.Text == "TERM-1/10" && function.Name == "Mã đóng gói",
                $"Fixed text keeps the value the object printed: '{function.Text}' {function.BindingExpression}");

            // A composition is built in its editor: picking it changes nothing yet.
            var column = Add("Mã hàng", "", "{MaHang}");
            Shown(column);
            list.SelectedItem = list.Items.OfType<DataSourceChoice>().Single(choice => choice.Kind == DataSourceChoiceKind.Compose);
            Settle();
            Require(column.BindingExpression == "{MaHang}", "Picking the composition leaves the source until it is applied: " + column.BindingExpression);
            // Not closed: closing an edited label asks whether to save it.
        }
        finally
        {
            try
            {
                Directory.Delete(directory, recursive: true);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

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
