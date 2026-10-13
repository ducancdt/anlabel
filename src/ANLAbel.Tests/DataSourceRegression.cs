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
