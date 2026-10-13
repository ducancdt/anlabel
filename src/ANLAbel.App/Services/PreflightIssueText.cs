using System.Text.RegularExpressions;
using ANLAbel.Printing.PrinterProfiles;

namespace ANLAbel.App.Services;

/// <summary>
/// Vietnamese wording for print preflight issues at the UI boundary (plan 13
/// H25). The validator keeps its English diagnostics because tests, logs and the
/// protected preflight contract depend on them; an issue without a rule keeps
/// its original text so nothing is hidden from the operator.
/// </summary>
public static class PreflightIssueText
{
    private static readonly (Regex Pattern, Func<Match, string> Text)[] Rules =
    [
        (Rx(@"^Missing field\(s\) in Excel data: (?<f>.+?)\. This object will print blank"),
            m => $"dữ liệu thiếu cột {m.Groups["f"].Value}; đối tượng sẽ in trống"),
        (Rx(@"^Formula binding error"), _ => "công thức lấy dữ liệu bị lỗi"),
        (Rx(@"^Text extends outside the design label for this row"),
            _ => "chữ của dòng này tràn ra ngoài tem; rút ngắn dữ liệu, giảm cỡ chữ hoặc dùng khung chữ"),
        (Rx(@"^Fixed text frame overflow"), _ => "chữ tràn khung đã khóa; nới khung hoặc giảm cỡ chữ"),
        (Rx(@"^Text box overflow"), _ => "chữ tràn khung chữ; nới khung hoặc giảm cỡ chữ"),
        (Rx(@"^Object extends outside the design label bounds"), _ => "đối tượng nằm ngoài khung tem; kéo vào trong tem"),
        (Rx(@"^Font '(?<r>[^']*)' is not installed on this machine\. Preview and print will use '(?<f>[^']*)'"),
            m => $"phông “{m.Groups["r"].Value}” chưa cài trên máy này nên sẽ in bằng “{m.Groups["f"].Value}”; cài phông hoặc chọn phông khác"),
        (Rx(@"^Font '(?<f>[^']*)' has no glyph\(s\) for (?<g>.+?)\. Install"),
            m => $"phông “{m.Groups["f"].Value}” không có ký tự {m.Groups["g"].Value}; chọn phông có đủ ký tự"),
        (Rx(@"^Image has no embedded data"), _ => "ảnh không có dữ liệu; chèn lại ảnh"),
        (Rx(@"^Image data is not a valid embedded bitmap"), _ => "dữ liệu ảnh bị hỏng; chèn lại ảnh"),
        (Rx(@"^Image could not be decoded for printing"), _ => "không đọc được ảnh để in; chèn lại ảnh"),
        (Rx(@"^Image raster mode '"), _ => "chế độ in ảnh này không được hỗ trợ; chọn chế độ khác cho ảnh"),
        (Rx(@"^Image source density is only (?<p>[\d.,]+)"),
            m => $"ảnh chỉ khoảng {m.Groups["p"].Value} PPI, thấp hơn độ phân giải máy in; dùng ảnh nét hơn"),
        (Rx(@"^Invalid (?<t>.+?) data\."), m => $"dữ liệu không hợp lệ cho mã {m.Groups["t"].Value}"),
        (Rx(@"^(?<s>.+?) data cannot be empty or whitespace"), m => $"dữ liệu mã {m.Groups["s"].Value} đang trống"),
        (Rx(@"^(?<s>.+?) check-digit policy is Verify but the barcode data is empty"),
            m => $"dữ liệu mã {m.Groups["s"].Value} đang trống nên không kiểm tra được số kiểm tra"),
        (Rx(@"^(?<s>.+?) check-digit policy is Verify: payload does not end with a valid check digit"),
            m => $"dữ liệu mã {m.Groups["s"].Value} không kết thúc bằng số kiểm tra đúng"),
        (Rx(@"^GS1 data is empty"), _ => "dữ liệu GS1 đang trống"),
        (Rx(@"^GS1 data must use \(AI\)value notation"), _ => "dữ liệu GS1 phải viết dạng (AI)giá trị"),
        (Rx(@"^GS1 (?:AI|application identifier) (?<ai>\d+)"), m => $"dữ liệu GS1 sai ở AI ({m.Groups["ai"].Value})"),
        (Rx(@"^Fixed QR capacity exceeded: (?<b>\d+) UTF-8 byte"),
            m => $"dữ liệu ({m.Groups["b"].Value} byte) vượt dung lượng phiên bản QR đã chọn; chọn phiên bản lớn hơn hoặc Tự động"),
        (Rx(@"^2D barcode frame is too small for this row: requires at least (?<r>[\d.,]+) mm but the authored frame is (?<a>[\d.,]+) mm"),
            m => $"khung mã 2D quá nhỏ cho dữ liệu dòng này: cần ít nhất {m.Groups["r"].Value} mm, khung hiện là {m.Groups["a"].Value} mm"),
        (Rx(@"^Module is only ~(?<x>[\d.,]+)"),
            m => $"mỗi ô của mã 2D chỉ khoảng {m.Groups["x"].Value} chấm in nên dễ không quét được; tăng kích thước ô hoặc dùng Tự động"),
        (Rx(@"^Linear module is only (?<d>\d+) printer dot"),
            m => $"vạch hẹp nhất chỉ {m.Groups["d"].Value} chấm in nên dễ không quét được; tăng độ rộng vạch (mm) hoặc DPI"),
        (Rx(@"^Linear module is ~(?<w>[\d.,]+) mm .*below the industrial floor"),
            m => $"vạch hẹp nhất chỉ {m.Groups["w"].Value} mm, dưới mức tối thiểu để quét ổn định; tăng độ rộng vạch"),
        (Rx(@"^Quiet zone is (?<q>\d+) module\(s\); this profile requires at least (?<r>\d+)"),
            m => $"vùng trống hai bên mã chỉ {m.Groups["q"].Value} mô-đun, cần ít nhất {m.Groups["r"].Value}"),
        (Rx(@"^Code 39 quiet zone is"), _ => "vùng trống hai bên mã Code 39 quá hẹp; tăng vùng trống"),
        (Rx(@"^Code 39 wide:narrow ratio"), _ => "tỉ lệ vạch rộng/hẹp của Code 39 quá nhỏ ở DPI này; chọn 2.2:1 trở lên"),
        (Rx(@"^Bearer bars are only supported on 1D"), _ => "viền bảo vệ chỉ dùng cho mã vạch 1D; đặt viền bảo vệ về Không"),
        (Rx(@"^Bearer bar thickness is (?<t>[\d.,]+) mm"), m => $"viền bảo vệ dày {m.Groups["t"].Value} mm, ngoài khoảng 0,2–5 mm"),
        (Rx(@"^HRI font size must be between"), _ => "cỡ chữ dưới mã vạch nằm ngoài khoảng cho phép"),
        (Rx(@"^A4/Letter/Legal/A3 office sheets are not thermal label stock"), _ => "khổ giấy văn phòng (A4, Letter…) không phải khổ tem"),
        (Rx(@"^DPI (?<d>\d+) is not a known industrial thermal resolution"),
            m => $"DPI {m.Groups["d"].Value} không phải độ phân giải máy in tem đã biết; chọn 203, 300, 305, 600 hoặc 609"),
        (Rx(@"^Print scale "), _ => "tỉ lệ in không hợp lệ; tem công nghiệp in 1:1, chỉ dùng hệ số hiệu chỉnh nhỏ"),
        (Rx(@"^(?:Label|Physical) stock "), _ => "kích thước khổ tem trong Cài đặt máy in không hợp lệ"),
        (Rx(@"^Print method is set to PrinterNative"), _ => "chế độ in bằng lệnh máy in chưa có driver hỗ trợ; chuyển về in đồ họa"),
        (Rx(@"^Copies field '(?<f>[^']*)' is blank"), m => $"ô số bản in (cột “{m.Groups["f"].Value}”) đang trống"),
        (Rx(@"^Copies field '(?<f>[^']*)' value '(?<v>[^']*)' is not a whole number"),
            m => $"số bản in “{m.Groups["v"].Value}” (cột “{m.Groups["f"].Value}”) không phải số nguyên"),
        (Rx(@"^Copies field '(?<f>[^']*)' value (?<v>-?\d+) is negative"),
            m => $"số bản in {m.Groups["v"].Value} (cột “{m.Groups["f"].Value}”) là số âm"),
        (Rx(@"^Copies field '(?<f>[^']*)' value (?<v>\d+) exceeds the limit of (?<m>\d+)"),
            m => $"số bản in {m.Groups["v"].Value} (cột “{m.Groups["f"].Value}”) vượt giới hạn {m.Groups["m"].Value}"),
        (Rx(@"^Copies field '(?<f>[^']*)' is missing from the row"), m => $"dữ liệu thiếu cột số bản in “{m.Groups["f"].Value}”"),
        (Rx(@"^Copies field '(?<f>[^']*)' "), m => $"số bản in (cột “{m.Groups["f"].Value}”) không hợp lệ"),
    ];

    /// <summary>Vietnamese text for one validator message; unknown messages are returned unchanged.</summary>
    public static string Message(string? message)
    {
        var text = (message ?? string.Empty).Trim();
        foreach (var (pattern, describe) in Rules)
        {
            var match = pattern.Match(text);
            if (match.Success)
            {
                return describe(match);
            }
        }

        return text;
    }

    /// <summary>True when <see cref="Message"/> has a Vietnamese rule for the message.</summary>
    public static bool IsKnown(string? message)
    {
        var text = (message ?? string.Empty).Trim();
        return Rules.Any(rule => rule.Pattern.IsMatch(text));
    }

    public static string ObjectType(string? objectType) => objectType switch
    {
        "Text" => "chữ",
        "TextBox" => "khung chữ",
        "Rectangle" => "hình chữ nhật",
        "Ellipse" => "hình elip",
        "Line" => "đường kẻ",
        "BarcodeCode128" => "mã vạch",
        "QRCode" => "mã QR",
        "DataMatrix" => "mã DataMatrix",
        "Image" => "ảnh",
        "Copies" => "số bản in",
        "LabelStock" => "khổ tem",
        "PrintDpi" => "DPI máy in",
        "PrintScale" => "tỉ lệ in",
        "PrinterProfile" => "cài đặt máy in",
        "Output" => "máy in",
        _ => objectType ?? string.Empty,
    };

    /// <summary>"Mẫu" for template issues, otherwise the affected data rows.</summary>
    public static string Location(IReadOnlyList<int> rows)
    {
        if (rows.Count == 0)
        {
            return "Mẫu";
        }

        var text = $"Dòng {string.Join(", ", rows.Take(5))}";
        return rows.Count > 5 ? $"{text} (+{rows.Count - 5} dòng khác)" : text;
    }

    public static string Line(IReadOnlyList<int> rows, string objectName, string objectType, string message)
    {
        // The validator names template-level settings issues "Template".
        var name = string.Equals(objectName, "Template", StringComparison.Ordinal) ? string.Empty : $" · “{objectName}”";
        return $"{Location(rows)}{name} ({ObjectType(objectType)}): {Message(message)}";
    }

    /// <summary>Capitalised sentence form of <see cref="Message"/> for dialogs.</summary>
    public static string Sentence(string? message)
    {
        var text = Message(message);
        if (text.Length == 0)
        {
            return text;
        }

        text = char.ToUpperInvariant(text[0]) + text[1..];
        return text.EndsWith('.') ? text : text + ".";
    }

    /// <summary>Grouped Vietnamese lines for the blocking issues, mapped to data rows.</summary>
    /// <param name="sourceRowOf">Maps an issue row number to the data row the operator sees; null keeps it.</param>
    public static IReadOnlyList<string> Lines(PrintPreflightResult result, Func<int, int>? sourceRowOf = null, int maxGroups = 5)
    {
        ArgumentNullException.ThrowIfNull(result);
        var groups = result.Issues
            .Where(issue => issue.IsBlocking)
            .GroupBy(issue => (issue.ObjectName, issue.ObjectType, issue.Message, IsTemplate: issue.RowNumber <= 0))
            .ToArray();
        var lines = groups.Take(Math.Max(maxGroups, 0)).Select(group =>
        {
            var rows = group
                .Where(issue => issue.RowNumber > 0)
                .Select(issue => sourceRowOf?.Invoke(issue.RowNumber) ?? issue.RowNumber)
                .Where(row => row > 0)
                .Distinct()
                .Order()
                .ToArray();
            return "- " + Line(rows, group.Key.ObjectName, group.Key.ObjectType, group.Key.Message);
        }).ToList();
        if (groups.Length > lines.Count)
        {
            lines.Add($"- …và {groups.Length - lines.Count} vấn đề khác.");
        }

        return lines;
    }

    /// <summary>Dialog text for a blocked print: what was found, where, and what to do.</summary>
    public static string Describe(PrintPreflightResult result, Func<int, int>? sourceRowOf = null, int maxGroups = 5)
    {
        var lines = Lines(result, sourceRowOf, maxGroups);
        var count = result.Issues
            .Where(issue => issue.IsBlocking)
            .Select(issue => (issue.ObjectName, issue.ObjectType, issue.Message, issue.RowNumber <= 0))
            .Distinct()
            .Count();
        return $"Kiểm tra trước khi in phát hiện {count} vấn đề. Chưa có tem nào được gửi."
               + Environment.NewLine + Environment.NewLine
               + string.Join(Environment.NewLine, lines)
               + Environment.NewLine + Environment.NewLine
               + "Sửa các mục trên rồi thử lại. Danh sách đầy đủ ở mục “Vấn đề nội dung”.";
    }

    private static Regex Rx(string pattern) => new(pattern, RegexOptions.CultureInvariant | RegexOptions.Compiled);
}
