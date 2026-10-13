using ANLAbel.App.Services;
using ANLAbel.App.ViewModels;
using ANLAbel.Data.Excel;
using ClosedXML.Excel;

/// <summary>
/// Plan 13 GĐ4: data problems are explained with file, row, column and the fix
/// (H14), and a failed import or relink keeps the working link (H16). Each
/// method is one named gate in Program.cs.
/// </summary>
internal static class DataSourceRegression
{
    public static async Task DataFailuresNameTheFileRowAndColumn()
    {
        var directory = NewDirectory();
        try
        {
            var service = new ExcelDataService();

            var formulaPath = Path.Combine(directory, "ton-kho.xlsx");
            using (var workbook = new XLWorkbook())
            {
                var sheet = workbook.AddWorksheet("Data");
                sheet.Cell(1, 1).Value = "Part";
                sheet.Cell(1, 2).Value = "Label";
                sheet.Cell(2, 1).Value = "PN-001";
                sheet.Cell(2, 2).FormulaA1 = "A2&\"-X\"";
                workbook.SaveAs(formulaPath);
            }

            var formula = Explain(await Capture(() => service.LoadSheetAsync(formulaPath, "Data")));
            Require(formula.Problem.Contains("Ô B2 (dòng 2, cột 2) trong sheet “Data” của tệp “ton-kho.xlsx” có công thức", StringComparison.Ordinal)
                    && formula.NextAction.Contains("tính lại", StringComparison.Ordinal),
                "A formula without a value must name the cell and say to recalculate: " + formula.Problem);

            var missingSheet = Explain(await Capture(() => service.LoadSheetAsync(formulaPath, "Sheet9")));
            Require(missingSheet.Problem.StartsWith("Tệp “ton-kho.xlsx” không có sheet “Sheet9”. Các sheet hiện có: Data", StringComparison.Ordinal),
                "A missing sheet must list the sheets that exist: " + missingSheet.Problem);

            var csvPath = Path.Combine(directory, "lo-hang.csv");
            await File.WriteAllTextAsync(csvPath, "Code,Lot,Qty\nA1,L1,2\nA2,3\n");
            var shortRecord = Explain(await Capture(() => service.LoadSheetAsync(csvPath, ExcelDataService.CsvSheetName)));
            Require(shortRecord.Problem == "Dòng 3 của tệp CSV “lo-hang.csv” có 2 trường, còn dòng tiêu đề có 3 trường.",
                "A short CSV record must be explained with its row: " + shortRecord.Problem);

            await File.WriteAllTextAsync(csvPath, "Code\nA1\n");
            Exception locked;
            using (new FileStream(csvPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                locked = await Capture(() => service.LoadSheetAsync(csvPath, ExcelDataService.CsvSheetName));
            }

            Require(Explain(locked).Problem == "Tệp dữ liệu đang bị chương trình khác khóa.",
                "A locked file must say so: " + Explain(locked).Problem + " / " + locked.GetType().Name + ": " + locked.Message);

            var neutral = OperationFailureExplainer.Explain(new InvalidOperationException("Something unexpected"));
            Require(neutral.IsGeneric && neutral.Problem == "ANLAbel không hoàn tất được thao tác vừa rồi.",
                "An unknown failure must not be described as a print failure: " + neutral.Problem);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    public static async Task FailedImportOrRelinkKeepsTheWorkingLink()
    {
        var directory = NewDirectory();
        try
        {
            var good = Path.Combine(directory, "good.csv");
            await File.WriteAllTextAsync(good, "Code,Qty\nA1,2\nA2,3\n");
            var bad = Path.Combine(directory, "bad.csv");
            await File.WriteAllTextAsync(bad, "Code,Qty\nB1\n");

            var vm = new MainViewModel();
            await vm.ImportExcelAsync(good, ExcelDataService.CsvSheetName, 1);
            Require(vm.Template.DatabaseConfig.FilePath == good && vm.Template.DatabaseConfig.HeaderRowIndex == 1,
                "The first import must link the good file.");

            // Wrong header row on another file: nothing about the working link may change.
            await Capture(() => vm.ImportExcelAsync(bad, ExcelDataService.CsvSheetName, 9));
            Require(vm.Template.DatabaseConfig.FilePath == good, "A failed import must keep the linked file.");
            Require(vm.Template.DatabaseConfig.HeaderRowIndex == 1, "A failed import must keep the header row.");
            Require(vm.ExcelDataView?.Count == 2, "A failed import must keep the loaded rows.");
            Require(vm.StatusText.StartsWith("Chưa đọc được dữ liệu: ", StringComparison.Ordinal),
                "A failed import must say why in Vietnamese: " + vm.StatusText);

            var root = AppContext.BaseDirectory;
            while (!File.Exists(Path.Combine(root, "ANLAbel.slnx")))
            {
                root = Path.GetDirectoryName(root.TrimEnd(Path.DirectorySeparatorChar))!;
            }

            var viewModel = File.ReadAllText(Path.Combine(root, "src", "ANLAbel.App", "ViewModels", "MainViewModel.cs")).Replace("\r\n", "\n");
            var relinkStart = viewModel.IndexOf("private async Task RelinkExcelAsync()", StringComparison.Ordinal);
            var relink = viewModel[relinkStart..viewModel.IndexOf("\n    }\n", relinkStart, StringComparison.Ordinal)];
            Require(!relink.Contains("Template.DatabaseConfig.FilePath =", StringComparison.Ordinal),
                "Relink must not store the new path before the new file was read.");
            var importWindow = File.ReadAllText(Path.Combine(root, "src", "ANLAbel.App", "ExcelImportWindow.xaml.cs"));
            Require(!importWindow.Contains("DatabaseConfig.HeaderRowIndex =", StringComparison.Ordinal),
                "The import window must not store the header row before the import succeeds.");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    public static async Task OverlappingImportsPublishOnlyTheLatest()
    {
        var directory = NewDirectory();
        try
        {
            var large = Path.Combine(directory, "large.csv");
            var content = new System.Text.StringBuilder("Code,Lot,Qty\n");
            for (var row = 0; row < 150_000; row++)
            {
                content.Append('L').Append(row).Append(",LOT,1\n");
            }

            await File.WriteAllTextAsync(large, content.ToString());
            var small = Path.Combine(directory, "small.csv");
            await File.WriteAllTextAsync(small, "Code,Lot,Qty\nS1,LOT,1\nS2,LOT,2\n");

            // A slow import is overtaken by a newer one (e.g. a refresh): only the newer result may publish.
            var vm = new MainViewModel();
            var slow = vm.ImportExcelAsync(large, ExcelDataService.CsvSheetName, 1);
            var fast = vm.ImportExcelAsync(small, ExcelDataService.CsvSheetName, 1);
            await fast;
            var outcome = await Capture(() => slow);
            Require(outcome is OperationCanceledException, "The older import must be discarded, not published: " + outcome.GetType().Name);
            Require(vm.Template.DatabaseConfig.FilePath == small && vm.ExcelDataView?.Count == 2,
                "The newer import must stay published.");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    public static async Task RecordNumbersFollowTheSourceFileNotTheGridSort()
    {
        var directory = NewDirectory();
        try
        {
            var path = Path.Combine(directory, "records.csv");
            await File.WriteAllTextAsync(path, "Code,Qty\nC,1\nA,2\nB,3\n");
            var vm = new MainViewModel();
            await vm.ImportExcelAsync(path, ExcelDataService.CsvSheetName, 1);

            // What the data grid does when the operator clicks the Code header.
            vm.ExcelDataView!.Sort = "Code ASC";
            vm.SelectedDataItem = vm.ExcelDataView[0];
            Require(vm.CurrentExcelRowText == "Dòng 2 / 3", "Record A is the second record of the file: " + vm.CurrentExcelRowText);
            Require(vm.TryBuildPrintPreviewRows(out var rows, out var error)
                    && string.Join(",", rows.Select(row => row!["Code"])) == "C,A,B",
                "Print rows must keep the file's order after a grid sort: " + error);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    public static async Task QuickPrintRechecksTheLinkedFileBeforePrinting()
    {
        var directory = NewDirectory();
        try
        {
            var path = Path.Combine(directory, "linked.csv");
            await File.WriteAllTextAsync(path, "Code,Qty\nA1,1\n");
            var vm = new MainViewModel();
            await vm.ImportExcelAsync(path, ExcelDataService.CsvSheetName, 1);
            var lastWrite = File.GetLastWriteTimeUtc(path);

            // Same length and timestamp, different bytes: the watcher's mtime check cannot see it.
            await File.WriteAllTextAsync(path, "Code,Qty\nB2,1\n");
            File.SetLastWriteTimeUtc(path, lastWrite);
            await Task.Delay(300);

            var check = typeof(MainViewModel).GetMethod("LinkedDataChangedSinceImportAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
            var changed = await (Task<bool>)check.Invoke(vm, null)!;
            Require(changed, "Quick print must detect a same-size, same-time replacement before printing.");
            foreach (var name in new[] { "private async Task PrintCurrentRowAsync()", "private async Task PrintAllRowsAsync()" })
            {
                var root = AppContext.BaseDirectory;
                while (!File.Exists(Path.Combine(root, "ANLAbel.slnx")))
                {
                    root = Path.GetDirectoryName(root.TrimEnd(Path.DirectorySeparatorChar))!;
                }

                var source = File.ReadAllText(Path.Combine(root, "src", "ANLAbel.App", "ViewModels", "MainViewModel.cs"));
                var start = source.IndexOf(name, StringComparison.Ordinal);
                var body = source.Substring(start, 1200);
                Require(body.Contains("await LinkedDataChangedSinceImportAsync()", StringComparison.Ordinal),
                    name + " must re-check the linked file before printing.");
            }
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    public static async Task InvalidBarcodeDataInAFileBlocksPrinting()
    {
        var directory = NewDirectory();
        try
        {
            // B01/B02 through the data path (plan 13 GĐ4): row 2 carries a wrong EAN-13 check digit.
            var path = Path.Combine(directory, "ean.csv");
            await File.WriteAllTextAsync(path, "Part,Ean\nP1,5901234123457\nP2,5901234123458\n");
            var table = await new ExcelDataService().LoadSheetAsync(path, ExcelDataService.CsvSheetName);
            var rows = table.Rows.Cast<System.Data.DataRow>()
                .Select(row => (IReadOnlyDictionary<string, string>?)table.Columns.Cast<System.Data.DataColumn>()
                    .ToDictionary(column => column.ColumnName, column => row[column]?.ToString() ?? string.Empty, StringComparer.OrdinalIgnoreCase))
                .ToArray();
            Require(rows[1]!["Ean"] == "5901234123458", "The payload must reach the label exactly as stored in the file.");

            var template = new ANLAbel.Core.Models.LabelTemplate { WidthMm = 100, HeightMm = 50, Dpi = 203 };
            template.PrinterProfile.Dpi = 203;
            template.Objects.Add(new ANLAbel.Core.Models.LabelObject
            {
                Id = "ean",
                Name = "EAN",
                Type = ANLAbel.Core.Enums.ObjectType.BarcodeCode128,
                BarcodeSymbology = ANLAbel.Core.Enums.BarcodeSymbology.Ean13,
                BindingExpression = "{Ean}",
                XMm = 5,
                YMm = 5,
                WidthMm = 60,
                HeightMm = 25
            });

            var preflight = new ANLAbel.Printing.PrinterProfiles.PrintService().ValidateRows(template, rows);
            var blocking = preflight.Issues.Where(issue => issue.IsBlocking).ToArray();
            Require(!preflight.IsSuccess && blocking.Length > 0 && blocking.All(issue => issue.RowNumber == 2),
                "Only row 2 must block, and it must block: " + preflight.ToUserMessage());
            var text = PreflightIssueText.Describe(preflight);
            Require(text.Contains("Dòng 2 · “EAN” (mã vạch)", StringComparison.Ordinal),
                "The operator must see the data row and object: " + text);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static PrintFailureExplanation Explain(Exception exception) => OperationFailureExplainer.Explain(exception);

    private static async Task<Exception> Capture(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            return ex;
        }

        throw new InvalidOperationException("The operation was expected to fail.");
    }

    private static string NewDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "anlabel-data-gate-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
