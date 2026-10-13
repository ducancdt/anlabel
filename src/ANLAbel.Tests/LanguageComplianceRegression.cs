using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

// Bộ quy chuẩn test lỗi ngôn ngữ — canh giữ "100% tiếng Việt có dấu, bỏ hẳn
// tiếng Anh" trên mọi đối tượng hiển thị. Xem docs/reinvention/12-khung-kien-truc-viet-hoa.md.
//
// Cơ chế ratchet an toàn: chỉ FAIL trên tập file đã tuyên bố Việt hóa
// (EnforcedXamlFiles). Mọi file XAML còn lại chỉ được liệt kê để theo dõi tiến
// độ, không làm đỏ build. Mỗi lần Việt hóa xong một file → thêm vào tập enforce.
internal static class LanguageComplianceRegression
{
    // Tập file đã Việt hóa 100% — scanner FAIL nếu còn tiếng Anh trong các file này.
    private static readonly string[] EnforcedXamlFiles =
    {
        "MainWindow.xaml",
        "PrintPreviewWindow.xaml",
        "PrinterSetupWindow.xaml",
        "DatabaseManagerWindow.xaml",
        "ExcelImportWindow.xaml",
        "PrintCenterWindow.xaml",
        "PrintHistoryWindow.xaml",
        "TemplateRevisionWindow.xaml",
        "DataSourceCleanupWindow.xaml",
        "ExcelHeaderRowWindow.xaml",
        "NewTemplateWindow.xaml",
        "ExcelSheetWindow.xaml",
        "TemplateLibraryWindow.xaml",
        "UpdateWindow.xaml",
        "HelpWindow.xaml",
        "SettingsWindow.xaml",
        "OperatorDialog.xaml",
        "App.xaml",
    };

    // Thuộc tính XAML mang chuỗi người vận hành nhìn thấy.
    private static readonly string[] VisibleAttributes =
    {
        "Content", "Header", "Text", "ToolTip", "Title", "Watermark",
        "Description", "PlaceholderText",
    };

    // Token kỹ thuật được phép giữ nguyên (tên riêng, mã, đơn vị, phím tắt).
    // So khớp không phân biệt hoa thường, theo từ trọn vẹn.
    private static readonly HashSet<string> AllowedTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        // Tên riêng / thương hiệu
        "ANLAbel", "GitHub", "Excel", "NiceLabel", "Windows", "USS", "ISO",
        // Mã / định dạng / thuật ngữ mã hoá
        "CSV", "QR", "PDF", "ZPL", "GS1", "DPI", "ID", "URL", "RGB", "HRI",
        "ECC", "ITF", "FNC1", "UTF", "mil", "px", "pt", "mm", "cm", "kg", "g",
        // Phím tắt / phím chức năng
        "Ctrl", "Alt", "Shift", "Del", "Enter", "Tab", "Esc",
        "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12",
        // Ký hiệu / trục / định dạng chữ (một ký tự) — allowlist rõ ràng
        "x", "X", "Y", "B", "U", "I", "L", "R", "T", "X1", "Y1", "X2", "Y2",
        // Giấy phép / nhãn kỹ thuật hỗn hợp
        "GPL", "cre", "Code",
        // Từ tiếng Việt hợp lệ nhưng không có dấu (tránh dương tính giả)
        "In", "Xoa", "Lo", "Ma",
    };

    // Cụm tên riêng kỹ thuật được phép (bóc trước khi tách từ) — ví dụ tên ký
    // hiệu mã vạch chuẩn công nghiệp, giữ nguyên như "QR".
    private static readonly string[] AllowedPhrases =
    {
        "Data Matrix", "DataMatrix", "Print Preview", "DriverManaged",
        "Print Schema", "Win32",
    };

    // Denylist tiếng Anh — các từ tiếng Anh thực tế từng xuất hiện trong UI.
    // Bắt cả chuỗi lẫn tiếng Việt để không bỏ sót trường hợp trộn ngôn ngữ.
    private static readonly HashSet<string> ForbiddenEnglish = new(StringComparer.OrdinalIgnoreCase)
    {
        "New", "Open", "Save", "Undo", "Redo", "Print", "Preview", "Settings",
        "Setup", "Import", "Export", "Update", "Delete", "Remove", "Cancel",
        "Close", "Create", "Apply", "Clear", "Add", "Manage", "Choose", "Relink",
        "Unlink", "Reload", "Restore", "Hide", "Show", "Review", "Select",
        "Text", "Box", "Barcode", "Line", "Rectangle", "Ellipse", "Image",
        "Workspace", "Layers", "Layer", "Template", "Objects", "Object", "Data",
        "Database", "Paper", "Queue", "History", "Calibration", "Retry",
        "Properties", "Property", "Document", "Label", "Layout", "Advanced",
        "Position", "Size", "Width", "Height", "Rotate", "Align", "Arrange",
        "Distribute", "Reference", "Selection", "Frame", "Key", "Artboard",
        "Left", "Right", "Center", "Middle", "Top", "Bottom", "Front", "Back",
        "Forward", "Backward", "Bring", "Send", "Visible", "Locked", "Baselines",
        "Baseline", "Optical", "Ink", "Centers", "Gaps", "Horizontal", "Vertical",
        "Source", "Field", "Binding", "Formula", "Builder", "Separators", "Space",
        "Parts", "Expression", "Output", "Style", "Font", "Bold", "Italic",
        "Underline", "Fit", "inside", "When", "content", "too", "tall", "Minimum",
        "Maximum", "scale", "Direction", "height", "Inner", "padding", "Edge",
        "override", "Tight", "Compact", "Comfort", "Color", "Shape", "Fill",
        "Corner", "radius", "Outline", "Thickness", "Standard", "Mode", "level",
        "Application", "profile", "Quiet", "zone", "placement", "check", "digit",
        "ratio", "Bearer", "bars", "thick", "Fixed", "Symbol", "version", "Module",
        "Choose", "Raster", "policy", "Aspect", "preserved", "picture", "centered",
        "Rows", "Row", "Tracking", "Copies", "Per", "Record", "Shared", "Sources",
        "Transforms", "Calculated", "fields", "run", "before", "and", "Current",
        "linked", "sheet", "Reload", "every", "imported", "append", "only",
        "Insert", "toolbox", "single", "owned", "docked", "Selected", "industrial",
        "printer", "status", "Hide", "Bring", "Send", "Move", "All", "No", "Yes",
        "English", "Help", "Guide", "User",
        "file", "point", "spool", "Release", "Releases", "Notes", "alpha",
        "monochrome", "Sheet", "Driver", "Manifest",
    };

    public static Task Run()
    {
        var root = FindRepositoryRoot();
        var appDir = Path.Combine(root, "src", "ANLAbel.App");

        var enforcedViolations = new List<Violation>();
        var trackingCounts = new List<(string File, int Count)>();

        foreach (var xaml in Directory.GetFiles(appDir, "*.xaml", SearchOption.TopDirectoryOnly)
                     .OrderBy(p => p, StringComparer.Ordinal))
        {
            var name = Path.GetFileName(xaml);
            var violations = ScanXaml(xaml);
            var enforced = EnforcedXamlFiles.Contains(name, StringComparer.OrdinalIgnoreCase);
            if (enforced)
            {
                enforcedViolations.AddRange(violations);
            }
            else if (violations.Count > 0)
            {
                trackingCounts.Add((name, violations.Count));
            }
        }

        // Báo cáo theo dõi (không làm đỏ build) — bức tranh tiến độ toàn app.
        if (trackingCounts.Count > 0)
        {
            var remaining = trackingCounts.Sum(t => t.Count);
            Console.WriteLine(
                $"  [Việt hóa] còn {remaining} chuỗi tiếng Anh ở {trackingCounts.Count} file chưa enforce: " +
                string.Join(", ", trackingCounts.OrderByDescending(t => t.Count).Select(t => $"{t.File}({t.Count})")));
        }

        // Cổng cứng: file đã tuyên bố Việt hóa không được còn tiếng Anh.
        if (enforcedViolations.Count > 0)
        {
            var report = new StringBuilder();
            report.AppendLine(
                $"Vi phạm chuẩn Việt hóa: {enforcedViolations.Count} chuỗi tiếng Anh trong file đã enforce.");
            foreach (var v in enforcedViolations.Take(60))
            {
                report.AppendLine($"  {v.File}:{v.Line}  \"{v.Value}\"  → từ cấm: {string.Join(", ", v.Words)}");
            }
            throw new InvalidOperationException(report.ToString());
        }

        return Task.CompletedTask;
    }

    private static List<Violation> ScanXaml(string path)
    {
        var results = new List<Violation>();
        var lines = File.ReadAllLines(path);
        var attrPattern = new Regex(
            // (?<!\w): match whole attribute names only, so SizeToContent="Height"
            // is not read as Content="Height"; ToolTipService.ToolTip still matches.
            "(?<!\\w)(?<attr>" + string.Join("|", VisibleAttributes) + ")=\"(?<val>[^\"]*)\"",
            RegexOptions.CultureInvariant);

        for (var i = 0; i < lines.Length; i++)
        {
            foreach (Match m in attrPattern.Matches(lines[i]))
            {
                var value = m.Groups["val"].Value;
                if (IsMarkupOrEmpty(value))
                {
                    continue;
                }

                var words = ForbiddenWordsIn(value);
                if (words.Count > 0)
                {
                    results.Add(new Violation(Path.GetFileName(path), i + 1, value, words));
                }
            }
        }

        return results;
    }

    private static bool IsMarkupOrEmpty(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.Length == 0)
        {
            return true;
        }

        // Bỏ qua binding / tài nguyên / x:Static.
        return trimmed.StartsWith("{", StringComparison.Ordinal);
    }

    // Trả về danh sách từ vi phạm; rỗng nghĩa là chuỗi hợp lệ.
    private static List<string> ForbiddenWordsIn(string value)
    {
        var decoded = value
            .Replace("&amp;", "&", StringComparison.Ordinal)
            .Replace("&gt;", ">", StringComparison.Ordinal)
            .Replace("&lt;", "<", StringComparison.Ordinal);

        foreach (var phrase in AllowedPhrases)
        {
            decoded = decoded.Replace(phrase, " ", StringComparison.OrdinalIgnoreCase);
        }

        // Tách token Latin và đối chiếu denylist tiếng Anh. Denylist là chuẩn
        // chính xác: tránh dương tính giả với từ tiếng Việt không dấu hợp lệ
        // (Tem, Cao, Khung, In...) mà một luật "đoán theo hình dạng" sẽ bắt nhầm.
        var violations = new List<string>();
        foreach (Match m in Regex.Matches(decoded, "[A-Za-z]+", RegexOptions.CultureInvariant))
        {
            var word = m.Value;
            if (AllowedTokens.Contains(word))
            {
                continue;
            }

            if (ForbiddenEnglish.Contains(word)
                && !violations.Contains(word, StringComparer.OrdinalIgnoreCase))
            {
                violations.Add(word);
            }
        }

        return violations;
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(Environment.CurrentDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ANLAbel.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("Không tìm thấy thư mục gốc kho mã (ANLAbel.slnx).");
    }

    private readonly record struct Violation(string File, int Line, string Value, List<string> Words);
}
