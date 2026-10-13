using System.Text.RegularExpressions;

namespace ANLAbel.App.Services;

/// <summary>Plain-language explanation of a print stop: what happened and what to do.</summary>
/// <param name="Problem">What happened, in Vietnamese.</param>
/// <param name="NextAction">The next useful action for the operator.</param>
/// <param name="Technical">The original technical message for the details line and logs.</param>
public sealed record PrintFailureExplanation(string Problem, string NextAction, string Technical)
{
    /// <summary>One status-bar line: problem and action.</summary>
    public string StatusLine => $"{Problem} {NextAction}";

    /// <summary>True when no rule recognised the technical message.</summary>
    public bool IsGeneric { get; init; }
}

/// <summary>
/// Maps the stable technical print-stop diagnostics of the core print contracts
/// to Vietnamese at the UI boundary (plan 13 H25). The core messages stay
/// unchanged because tests and logs depend on them; unknown messages fall back
/// to a generic explanation that still shows the technical text.
/// </summary>
public static class PrintFailureExplainer
{
    private const string NotSent = "Chưa có tem nào được gửi.";

    private static readonly IReadOnlyDictionary<string, string> RevalidationFields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["document-hash"] = "nội dung mẫu",
        ["output-contract-missing"] = "thiếu thiết lập đã kiểm tra",
        ["ticket-evidence"] = "bằng chứng thiết lập driver",
        ["requested-ticket"] = "thiết lập gửi tới driver",
        ["effective-ticket"] = "thiết lập driver thực tế",
        ["queue-name"] = "hàng đợi máy in",
        ["dpi"] = "DPI",
        ["media"] = "khổ giấy",
        ["imageable-area"] = "vùng in",
        ["output-contract-fingerprint"] = "dấu thiết lập đầu ra",
        ["prepared-expectation"] = "thiết lập đã chuẩn bị",
    };

    private static readonly (Regex Pattern, Func<Match, string, (string Problem, string Action)> Explain)[] Rules =
    [
        (Rx(@"^The printer output contract is missing verified driver-ticket evidence or changed after preview"),
            (_, _) => ($"Thiết lập máy in đã khác lúc xem trước (DPI, khổ giấy, lề hoặc thiết lập driver), hoặc driver không xác nhận được thiết lập. {NotSent}",
                       "Đóng rồi mở lại Xem trước để kiểm tra thiết lập mới, sau đó in.")),
        (Rx(@"^Printing stopped because the printer output contract changed immediately before dispatch(?: \((?<fields>[^)]*)\))?"),
            (m, _) => ($"Thiết lập máy in thay đổi ngay trước khi gửi{Fields(m)}. {NotSent}",
                       "Mở lại Xem trước để kiểm tra thiết lập rồi in.")),
        (Rx(@"^Printing stopped because the printer output contract could not be revalidated"),
            (_, _) => ($"Không kiểm tra lại được thiết lập máy in ngay trước khi gửi. {NotSent}",
                       "Kiểm tra máy in đang bật và kết nối, rồi in lại.")),
        (Rx(@"^Printing stopped because the saved driver '(?<saved>[^']*)' changed to '(?<current>[^']*)'"),
            (m, _) => ($"Driver máy in đã đổi từ “{m.Groups["saved"].Value}” sang “{m.Groups["current"].Value}”. {NotSent}",
                       "Mở Cài đặt máy in, kiểm tra khổ tem và DPI rồi lưu lại.")),
        (Rx(@"^Printing stopped because the current printer driver identity could not be (?:read|verified)"),
            (_, _) => ($"Không đọc được thông tin driver của máy in. {NotSent}",
                       "Mở Cài đặt máy in và kiểm tra hàng đợi máy in.")),
        (Rx(@"^Printer queue health could not be read"),
            (_, _) => ($"Không đọc được trạng thái hàng đợi máy in. {NotSent}",
                       "Kiểm tra dịch vụ Print Spooler và máy in rồi thử lại.")),
        (Rx(@"^The printer reports a paper/media jam"),
            (_, _) => ($"Máy in báo kẹt giấy hoặc kẹt tem. {NotSent}", "Gỡ tem bị kẹt, đóng nắp máy in rồi in lại.")),
        (Rx(@"^The printer reports that paper/media is unavailable"),
            (_, _) => ($"Máy in báo hết giấy hoặc hết tem. {NotSent}", "Nạp cuộn tem rồi in lại.")),
        (Rx(@"^The printer queue reports offline"),
            (_, _) => ($"Máy in đang ngoại tuyến (offline). {NotSent}", "Bật máy in, kiểm tra cáp hoặc mạng rồi in lại.")),
        (Rx(@"^The printer requires operator intervention"),
            (_, _) => ($"Máy in đang chờ người vận hành xử lý. {NotSent}", "Kiểm tra màn hình hoặc đèn báo trên máy in rồi in lại.")),
        (Rx(@"^The printer queue is paused"),
            (_, _) => ($"Hàng đợi máy in đang tạm dừng. {NotSent}", "Mở hàng đợi máy in trong Windows, bỏ tạm dừng rồi in lại.")),
        (Rx(@"^The printer queue reports an error"),
            (_, _) => ($"Hàng đợi máy in đang báo lỗi. {NotSent}", "Mở hàng đợi máy in trong Windows để xem và xóa lỗi rồi in lại.")),
        (Rx(@"^(?:No printer queue was (?:explicitly )?selected|No printer is selected|(?:Print preparation stopped|Reprint dispatch was blocked) because no verified printer queue is selected)"),
            (_, _) => ("Chưa chọn máy in cho mẫu này.", "Mở Cài đặt máy in để chọn hàng đợi máy in tem.")),
        (Rx(@"^Printer queue '(?<queue>[^']*)' was inherited from Windows' default"),
            (m, _) => ($"Máy in “{m.Groups["queue"].Value}” chỉ là máy in mặc định của Windows, chưa được chọn cho mẫu này.",
                       "Mở Cài đặt máy in để chọn và lưu máy in tem.")),
        (Rx(@"^Requested printer '(?<queue>[^']*)' is unavailable"),
            (m, _) => ($"Không tìm thấy máy in “{m.Groups["queue"].Value}” trong Windows. {NotSent}",
                       "Bật máy in hoặc chọn lại hàng đợi máy in trong Cài đặt máy in.")),
        (Rx(@"^Windows resolved '(?<queue>[^']*)' to a different printer queue '(?<other>[^']*)'"),
            (m, _) => ($"Windows trỏ máy in “{m.Groups["queue"].Value}” sang hàng đợi khác (“{m.Groups["other"].Value}”). {NotSent}",
                       "Chọn lại máy in trong Cài đặt máy in rồi in.")),
        (Rx(@"^The printer driver coerced media to (?<got>.+?) mm, but the label requires (?<need>.+?) mm"),
            (m, _) => ($"Driver máy in đổi khổ giấy thành {m.Groups["got"].Value} mm, trong khi tem cần {m.Groups["need"].Value} mm. {NotSent}",
                       "Chọn khổ giấy đúng với cuộn tem trong Cài đặt máy in rồi in lại.")),
        (Rx(@"^The printer driver (?:returned an invalid effective DPI|did not return a valid effective PrintTicket)"),
            (_, _) => ($"Driver máy in trả về thiết lập không hợp lệ. {NotSent}",
                       "Mở Cài đặt máy in, kiểm tra DPI và driver rồi thử lại.")),
        (Rx(@"^A4/Letter/Legal/A3 office sheets are not thermal label stock"),
            (_, _) => ("Khổ giấy văn phòng (A4, Letter…) không phải khổ tem.", "Chọn khổ tem trong danh mục hoặc nhập kích thước tem (mm) trong Cài đặt máy in.")),
        (Rx(@"^DPI (?<dpi>\d+) is not a known industrial thermal resolution"),
            (m, _) => ($"DPI {m.Groups["dpi"].Value} không phải độ phân giải máy in tem đã biết.", "Chọn 203, 300, 305, 600 hoặc 609 trong Cài đặt máy in.")),
        (Rx(@"^(?:Print preparation stopped|Printing stopped|Printing is blocked) because the label design is invalid"),
            (_, _) => ($"Thiết kế tem có lỗi nên chưa thể in. {NotSent}", "Sửa thiết kế theo chi tiết kỹ thuật rồi thử lại.")),
        (Rx(@"^Printing is blocked because one or more selected rows have an invalid copies value"),
            (_, _) => ($"Có dòng dữ liệu đã chọn mang số bản in không hợp lệ. {NotSent}", "Sửa cột số bản in của các dòng đó rồi in lại.")),
        (Rx(@"^Printing is blocked\. Resolve the following issues:"),
            (_, technical) => (PreflightProblem(technical),
                               "Mở Xem trước để xem các dòng và đối tượng bị lỗi, sửa rồi in lại.")),
        (Rx(@"^The preview plan no longer matches the current template"),
            (_, _) => ($"Mẫu đã thay đổi sau khi xem trước. {NotSent}", "Làm mới Xem trước rồi in.")),
        (Rx(@"^Image '(?<name>[^']*)' metadata does not match its embedded payload"),
            (m, _) => ($"Ảnh “{m.Groups["name"].Value}” trong mẫu bị sai lệch dữ liệu. {NotSent}", "Chèn lại ảnh đó rồi in.")),
        (Rx(@"^Print was blocked because required job evidence could not be saved"),
            (_, _) => ($"Không ghi được nhật ký lệnh in nên ANLAbel chưa in. {NotSent}",
                       "Kiểm tra ổ đĩa còn chỗ trống và thư mục dữ liệu ANLAbel không bị khóa, rồi thử lại.")),
        (Rx(@"^(?:Print|Reprint) dispatch was blocked because (?:the newly compiled inputs|the current row/label counts|the current template, printer, DPI or data no longer matches)"),
            (_, _) => ($"Mẫu, máy in hoặc dữ liệu đã khác bản được duyệt. {NotSent}", "Mở lại lệnh in từ Trung tâm in để kiểm tra trước khi in.")),
    ];

    public static PrintFailureExplanation Explain(string? technicalMessage)
    {
        var technical = (technicalMessage ?? string.Empty).Trim();
        foreach (var (pattern, explain) in Rules)
        {
            var match = pattern.Match(technical);
            if (match.Success)
            {
                var (problem, action) = explain(match, technical);
                return new PrintFailureExplanation(problem, action, technical);
            }
        }

        return new PrintFailureExplanation(
            "Chưa in được.",
            "Xem chi tiết kỹ thuật bên dưới; nếu không rõ nguyên nhân, mở lại Xem trước để kiểm tra rồi thử lại.",
            technical)
        {
            IsGeneric = true,
        };
    }

    private static Regex Rx(string pattern) => new(pattern, RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static string Fields(Match match)
    {
        var names = match.Groups["fields"].Value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(field => RevalidationFields.TryGetValue(field, out var name) ? name : field)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        return names.Length == 0 ? string.Empty : $" ({string.Join(", ", names)})";
    }

    private static string PreflightProblem(string technical)
    {
        // "- Row 3, Barcode1 (BarcodeCode128): Invalid Code 128 data. …" — row
        // numbers here count labels, not data rows, so only the object is named.
        var first = Regex.Match(
            technical,
            @"^- (?:Template|Rows? [\d, ]+(?: \([^)]*\))?), (?<name>.+?) \((?<type>[A-Za-z0-9]+)\): (?<message>.+?)\s*$",
            RegexOptions.Multiline | RegexOptions.CultureInvariant);
        var example = string.Empty;
        if (first.Success)
        {
            var name = first.Groups["name"].Value;
            var subject = name == "Template" ? string.Empty : $"“{name}” ";
            example = $" Ví dụ: {subject}({PreflightIssueText.ObjectType(first.Groups["type"].Value)}) — {PreflightIssueText.Message(first.Groups["message"].Value).TrimEnd('.')}.";
        }

        return $"Kiểm tra trước khi in phát hiện {PreflightIssueCount(technical)} vấn đề trong mẫu hoặc dữ liệu.{example} {NotSent}";
    }

    private static int PreflightIssueCount(string technical)
    {
        var count = 0;
        foreach (var line in technical.Split('\n'))
        {
            var text = line.Trim();
            var more = Regex.Match(text, @"^- \.\.\.and (?<n>\d+) more distinct issue", RegexOptions.CultureInvariant);
            if (more.Success)
            {
                count += int.Parse(more.Groups["n"].Value, System.Globalization.CultureInfo.InvariantCulture);
            }
            else if (text.StartsWith("- ", StringComparison.Ordinal))
            {
                count++;
            }
        }

        return Math.Max(count, 1);
    }
}
