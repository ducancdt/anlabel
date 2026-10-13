using System.Text.RegularExpressions;
using ANLAbel.Project.SaveLoad;

namespace ANLAbel.App.Services;

/// <summary>
/// Vietnamese text for the template revision window (audit 2026-10-13). The
/// revision service keeps its English statuses and diff lines as data.
/// </summary>
public static class RevisionText
{
    private static readonly (string English, string Vietnamese)[] DiffLabels =
    [
        ("Template name", "Tên mẫu"),
        ("Label size", "Kích thước tem"),
        ("Design DPI", "DPI thiết kế"),
        ("Orientation", "Hướng"),
        ("Gap / margin", "Khe / lề"),
        ("Printer queue", "Hàng đợi máy in"),
        ("Paper", "Khổ giấy"),
        ("Media / feed", "Vật liệu / cách nạp"),
        ("Printer DPI", "DPI máy in"),
        ("Printer offsets / scale", "Bù lệch / tỉ lệ máy in"),
        ("Objects", "Đối tượng"),
        ("Guides", "Đường gióng"),
        ("Linked data", "Dữ liệu liên kết"),
    ];

    private static readonly Regex DiffLine = new("^(?<label>[^:]+): primary '(?<p>.*)' · backup '(?<b>.*)'$", RegexOptions.CultureInvariant);
    private static readonly Regex Unavailable = new("^Diff unavailable: primary is (?<p>.+) and backup is (?<b>.+)[.]$", RegexOptions.CultureInvariant);

    public static string Kind(ProjectRevisionKind kind) => kind switch
    {
        ProjectRevisionKind.Primary => "Bản hiện tại",
        ProjectRevisionKind.Backup => "Bản sao lưu phục hồi",
        _ => "Bản lưu trữ",
    };

    public static string Status(string? status) => (status ?? string.Empty).Trim().ToLowerInvariant() switch
    {
        "valid" => "Hợp lệ",
        "invalid" => "Không hợp lệ",
        "missing" => "Không có",
        "unsupported schema" => "Phiên bản mới hơn",
        "unreadable" => "Không đọc được",
        _ => status ?? string.Empty,
    };

    public static string Diagnostic(string? diagnostic)
    {
        var text = (diagnostic ?? string.Empty).Trim();
        if (text.Length == 0)
        {
            return string.Empty;
        }

        if (text == "No committed revision is present at this path.")
        {
            return "Không có bản đã lưu ở đường dẫn này.";
        }

        if (text.StartsWith("Reason: ", StringComparison.Ordinal))
        {
            return "Lý do: " + text["Reason: ".Length..];
        }

        var explained = TemplateFailureExplainer.ExplainMessage(text);
        return explained.IsGeneric ? text : explained.Problem;
    }

    public static string Details(ProjectRevisionDiff diff)
    {
        ArgumentNullException.ThrowIfNull(diff);
        var lines = new List<string> { Summary(diff.Summary) };
        lines.AddRange(diff.Differences.Select(item => "• " + Difference(item)));
        return string.Join(Environment.NewLine, lines);
    }

    public static string Summary(string? summary)
    {
        var text = (summary ?? string.Empty).Trim();
        if (text.StartsWith("Primary and backup differ", StringComparison.Ordinal))
        {
            return "Bản hiện tại và bản sao lưu khác nhau; xem các mục dưới đây trước khi khôi phục.";
        }

        if (text.StartsWith("Primary and backup have the same document identity", StringComparison.Ordinal))
        {
            return "Bản hiện tại và bản sao lưu giống nhau.";
        }

        var unavailable = Unavailable.Match(text);
        return unavailable.Success
            ? $"Không so sánh được: bản hiện tại {Status(unavailable.Groups["p"].Value).ToLowerInvariant()}, bản sao lưu {Status(unavailable.Groups["b"].Value).ToLowerInvariant()}."
            : text;
    }

    public static string Difference(string? line)
    {
        var text = (line ?? string.Empty).Trim();
        if (text.StartsWith("Document content/style/resource identity changed", StringComparison.Ordinal))
        {
            return "Nội dung, kiểu hoặc tài nguyên của mẫu đã khác (xem mã băm).";
        }

        var match = DiffLine.Match(text);
        if (!match.Success)
        {
            return text;
        }

        var label = match.Groups["label"].Value;
        var vietnamese = DiffLabels.FirstOrDefault(pair => pair.English == label).Vietnamese ?? label;
        return $"{vietnamese}: bản hiện tại “{match.Groups["p"].Value}” · bản sao lưu “{match.Groups["b"].Value}”";
    }
}
