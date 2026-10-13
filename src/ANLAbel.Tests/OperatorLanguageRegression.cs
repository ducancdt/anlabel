using ANLAbel.App.Services;
using ANLAbel.Core.Printing;
using ANLAbel.Data.Excel;
using ANLAbel.Printing.PrinterProfiles;

/// <summary>
/// Audit 2026-10-13: printer, print-result, template-file and data-link texts
/// the operator sees are Vietnamese. Core messages stay English for logs and
/// evidence; each rule is anchored to its core message so a rewording fails
/// here instead of silently falling back to a generic text.
/// </summary>
internal static class OperatorLanguageRegression
{
    public static async Task FailuresAndStatusesAreExplainedInVietnamese()
    {
        // Printer chip and startup status: nothing was printed yet.
        var none = PrintFailureExplainer.ExplainQueueStatus("No printer queue is selected. Choose a verified industrial printer before printing.");
        Require(!none.IsGeneric && none.Problem == "Chưa chọn máy in cho mẫu này.", $"Startup printer warning: {none.Problem}");
        var offline = PrintFailureExplainer.ExplainQueueStatus("The printer queue reports offline. Resolve the queue/device condition and prepare again; no label was submitted.");
        Require(offline.Problem.Contains("ngoại tuyến", StringComparison.Ordinal)
                && !offline.Problem.Contains("Chưa có tem nào được gửi", StringComparison.Ordinal),
            $"A queue warning before printing must not talk about a label not sent: {offline.Problem}");
        Require(PrintFailureExplainer.ExplainQueueStatus("Windows could not open the saved queue: Access denied").Problem.StartsWith("Windows không mở được", StringComparison.Ordinal),
            "A queue that cannot be opened must be explained.");

        // Queue observation after a print.
        var printing = new SpoolJobMonitorResult(new SpoolJobObservation("P", 12, SpoolJobState.Printing, "Driver reports printing."), 2, TimeSpan.FromSeconds(1), TimedOut: false);
        Require(PrintStatusText.Queue(printing) == "Lệnh hàng đợi #12: đang in. Chưa xác minh tem đã ra.", $"Queue status: {PrintStatusText.Queue(printing)}");
        var slow = printing with { TimedOut = true, Elapsed = TimeSpan.FromSeconds(3) };
        Require(PrintStatusText.Queue(slow).StartsWith("Đã chờ 3 giây", StringComparison.Ordinal) && PrintStatusText.Queue(slow).Contains("không tự in lại", StringComparison.Ordinal),
            $"A queue that did not answer in time: {PrintStatusText.Queue(slow)}");

        // Job result with a technical reason.
        var failed = new PrintJobResult(PrintJobOutcome.Failed, "P", "Current row", 0, "The printer queue reports offline.");
        Require(PrintStatusText.Result(failed).StartsWith("In thất bại: Máy in đang ngoại tuyến", StringComparison.Ordinal), $"Failed result: {PrintStatusText.Result(failed)}");
        Require(PrintStatusText.Result(new PrintJobResult(PrintJobOutcome.Cancelled, "P", "Current row", 0)) == "Đã hủy in.", "A cancelled job keeps its text.");

        // Template files.
        Require(TemplateFailureExplainer.Explain(new InvalidDataException("The template file contains malformed JSON.")).Problem == "Tệp mẫu bị hỏng hoặc không đọc được.",
            "A corrupt template must be explained.");
        Require(TemplateFailureExplainer.Explain(new InvalidDataException("The template uses schema version 9, but this build supports up to 2. Update ANLAbel before opening it.")).Problem.Contains("mới hơn", StringComparison.Ordinal),
            "A template from a newer ANLAbel must be explained.");
        var locked = TemplateFailureExplainer.Explain(new IOException("The process cannot access the file.", unchecked((int)0x80070020)));
        Require(locked.Problem.StartsWith("Tệp mẫu đang bị chương trình khác", StringComparison.Ordinal), $"A locked template: {locked.Problem}");
        var withBackup = new InvalidDataException("Không mở được tệp mẫu.");
        withBackup.Data[TemplateFailureExplainer.BackupPathKey] = "C:/Tem/a.anlabel.bak";
        withBackup.Data[TemplateFailureExplainer.PrimaryErrorKey] = "The template file contains malformed JSON.";
        var backup = TemplateFailureExplainer.Explain(withBackup);
        Require(backup.Problem.Contains("C:/Tem/a.anlabel.bak", StringComparison.Ordinal) && backup.Problem.StartsWith("Tệp mẫu bị hỏng", StringComparison.Ordinal),
            $"An open with a readable backup must name the backup: {backup.Problem}");
        var unknown = new InvalidOperationException("Something unexpected");
        Require(OperationFailureExplainer.StatusLine(unknown).EndsWith("Chi tiết kỹ thuật: Something unexpected", StringComparison.Ordinal),
            "An unrecognised failure keeps its technical text as a labelled detail.");

        // Data link check.
        var missing = await new ExcelDataService().CheckConnectionAsync(Path.Combine(Path.GetTempPath(), "anlabel-missing-" + Guid.NewGuid().ToString("N") + ".xlsx"), "Sheet1");
        Require(!missing.Ok && missing.Failure is not null, "A missing data file must fail the link check.");
        Require(OperationFailureExplainer.StatusLine(missing.Failure!).StartsWith("Không tìm thấy tệp dữ liệu", StringComparison.Ordinal),
            $"A missing data file: {OperationFailureExplainer.StatusLine(missing.Failure!)}");

        // Each rule anchors on a core message.
        var root = FindRepositoryRoot();
        var sources = string.Concat(new[] { "ANLAbel.Core", "ANLAbel.Data", "ANLAbel.Printing", "ANLAbel.Project", "ANLAbel.App" }
            .SelectMany(project => Directory.EnumerateFiles(Path.Combine(root, "src", project), "*.cs", SearchOption.AllDirectories))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                           && !path.EndsWith("PrintFailureExplainer.cs", StringComparison.Ordinal)
                           && !path.EndsWith("TemplateFailureExplainer.cs", StringComparison.Ordinal))
            .Select(File.ReadAllText));
        string[] anchors =
        [
            "No printer queue is selected",
            "No verified printer queue is selected",
            "No printer queue was selected",
            "The requested Windows printer queue is unavailable",
            "Windows resolved the requested queue to '",
            "Windows could not open the saved queue",
            "Printer queue lookup failed",
            "The template uses schema version ",
            "The existing template cannot be overwritten because it uses an unsupported schema",
            "The template file format marker is missing or unsupported",
            "The template and its recovery backup are both invalid",
            "The template file contains malformed JSON",
            "The template file root must be a JSON object",
            "The template schemaVersion is invalid",
            "The template schemaVersion is missing",
            "The versioned template envelope",
            "The versioned template payload is invalid",
            "The legacy template payload is invalid",
        ];
        foreach (var anchor in anchors)
        {
            Require(sources.Contains(anchor, StringComparison.Ordinal), $"Core message changed; update the Vietnamese explanation: {anchor}");
        }
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ANLAbel.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("Could not locate the repository root.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
