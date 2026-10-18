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
        (Rx(@"^Placeholder \{\} has no field name"),
            _ => "dấu ngoặc {} chưa có tên cột nên đối tượng sẽ in trống; nhập tên cột vào giữa hai dấu ngoặc hoặc xóa chúng"),
        (Rx(@"^Field '(?<f>.*?)' reads column (?<c>[A-Z]+), whose header cell is empty"),
            m => $"trường “{m.Groups["f"].Value}” lấy từ cột {m.Groups["c"].Value} không có tiêu đề nên chỉ nhận ra được theo vị trí cột; đặt tiêu đề cho cột này trong tệp dữ liệu rồi bấm Cập nhật Excel"),
        (Rx(@"^Field '(?<f>.*?)' matches columns (?<cols>.+?) equally, so only their order decides"),
            m => $"trường “{m.Groups["f"].Value}” khớp ngang nhau với các cột {m.Groups["cols"].Value} nên chỉ thứ tự cột quyết định cột nào được in; đổi tên một cột trong tệp dữ liệu hoặc liên kết đúng tên tiêu đề rồi bấm Cập nhật Excel"),
        (Rx(@"^Field '(?<f>.*?)' reads column (?<c>[A-Z]+), whose header '(?<h>.*?)' is repeated in columns (?<all>[A-Z, ]+), so"),
            m => $"trường “{m.Groups["f"].Value}” lấy từ cột {m.Groups["c"].Value}, nhưng tiêu đề “{m.Groups["h"].Value}” lặp ở các cột {m.Groups["all"].Value} nên chỉ nhận ra được theo vị trí cột; đặt tên riêng cho từng cột trong tệp dữ liệu rồi bấm Cập nhật Excel"),
        (Rx("^Printing is blocked because the label design is invalid [(](?<d>.*)[)][.]"),
            m => $"thiết kế tem có lỗi nên chưa thể in (chi tiết kỹ thuật: {m.Groups["d"].Value}); sửa thiết kế rồi làm mới xem trước"),
        (Rx(@"^Text extends outside the design label for this row"),
            _ => "chữ của dòng này tràn ra ngoài tem; rút ngắn dữ liệu, giảm cỡ chữ hoặc dùng khung chữ"),
        (Rx(@"^Fixed text frame overflow"), _ => "chữ tràn khung đã khóa; nới khung hoặc giảm cỡ chữ"),
        (Rx(@"^Text box overflow"), _ => "chữ tràn khung chữ; nới khung hoặc giảm cỡ chữ"),
        (Rx(@"^Object extends outside the design label bounds"), _ => "đối tượng nằm ngoài khung tem; kéo vào trong tem"),
        (Rx(@"^Barcode for this row extends past the label edge at its X-dimension"),
            _ => "mã vạch của dòng này tràn ra ngoài mép tem ở độ rộng vạch đã đặt; rút ngắn dữ liệu, giảm độ rộng vạch hoặc dời mã vào trong tem"),
        (Rx(@"^2D barcode for this row does not fit its frame at (?<d>\d+) DPI"),
            m => $"mã 2D của dòng này không vừa khung ở {m.Groups["d"].Value} DPI khi mỗi ô cần ít nhất một chấm in nên sẽ bị bóp hoặc bị cắt; nới khung hoặc rút ngắn dữ liệu"),
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
        (Rx(@"^(?<s>.+?) check-digit policy is Verify: payload does not end with a valid custom check digit"),
            m => $"dữ liệu mã {m.Groups["s"].Value} không kết thúc bằng số kiểm tra đúng theo thuật toán riêng; sửa dữ liệu hoặc thuật toán"),
        (Rx(@"^(?<s>.+?) check-digit policy is Generate but the barcode data is empty"),
            m => $"dữ liệu mã {m.Groups["s"].Value} đang trống nên không tự sinh được số kiểm tra"),
        (Rx(@"^Code 39 check digit cannot be generated"),
            _ => "không tự sinh được số kiểm tra Code 39 vì dữ liệu có ký tự ngoài bộ Code 39"),
        (Rx(@"^ITF check digit cannot be generated"),
            _ => "không tự sinh được số kiểm tra ITF vì dữ liệu phải chỉ gồm chữ số"),
        (Rx(@"^A custom check digit can only be generated for data made of digits"),
            _ => "thuật toán số kiểm tra riêng chỉ dùng cho dữ liệu toàn chữ số"),
        (Rx(@"^ITF needs an even number of digits including the check digit; (?<n>\d+) data digits"),
            m => $"mã ITF cần tổng số chữ số chẵn kể cả số kiểm tra; dữ liệu có {m.Groups["n"].Value} chữ số nên cần thêm hoặc bớt một chữ số"),
        (Rx(@"^Custom check digit modulus must be"),
            _ => "modulo của thuật toán số kiểm tra riêng phải từ 2 đến 99"),
        (Rx(@"^Custom check digit value map "),
            _ => "ánh xạ kết quả của thuật toán số kiểm tra riêng không hợp lệ; viết dạng 10=X;11=0"),
        (Rx(@"^Custom check digit weights "),
            _ => "trọng số của thuật toán số kiểm tra riêng không hợp lệ; nhập các số nguyên 0–99 cách nhau bởi dấu phẩy, ví dụ 3,1"),
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
        (Rx(@"^(?<s>Ean13|Ean8|UpcA|UpcE) prints at (?<m>\d+)% magnification"),
            m => $"mã {m.Groups["s"].Value} in ở độ phóng {m.Groups["m"].Value}%, ngoài khoảng 80–200% của GS1 cho bán lẻ (X 0,264–0,660 mm); có thể vẫn quét được trong nhà máy nhưng không bảo đảm quét tại quầy; đặt X trong khoảng đó"),
        (Rx(@"^Line thickness (?<t>[\d.,]+) mm is thinner than one printer dot \((?<d>[\d.,]+) mm at (?<dpi>\d+) DPI\)"),
            m => $"nét dày {m.Groups["t"].Value} mm mảnh hơn một chấm in ({m.Groups["d"].Value} mm ở {m.Groups["dpi"].Value} DPI) nên có thể in mờ hoặc mất dù màn hình vẫn thấy; tăng độ dày nét lên ít nhất {m.Groups["d"].Value} mm"),
        (Rx(@"^Code 39 quiet zone is"), _ => "vùng trống hai bên mã Code 39 quá hẹp; tăng vùng trống"),
        (Rx(@"^Code 39 wide:narrow ratio"), _ => "tỉ lệ vạch rộng/hẹp của Code 39 quá nhỏ ở DPI này; chọn 2.2:1 trở lên"),
        (Rx(@"^Bearer bars are only supported on 1D"), _ => "viền bảo vệ chỉ dùng cho mã vạch 1D; đặt viền bảo vệ về Không"),
        (Rx(@"^Bearer bar thickness is (?<t>[\d.,]+) mm"), m => $"viền bảo vệ dày {m.Groups["t"].Value} mm, ngoài khoảng 0,2–5 mm"),
        (Rx(@"^Frame bearer bars are .*Increase quiet zone to at least (?<n>\d+) modules"),
            m => $"khung bao hai bên dày hơn vùng trống nên đè lên vạch đầu và cuối của mã; tăng vùng trống lên ít nhất {m.Groups["n"].Value} mô-đun, giảm độ dày hoặc chỉ dùng vạch trên dưới"),
        (Rx(@"^Bar width reduction of (?<r>\d+) dot\(s\) leaves the narrow bar (?<left>\d+) dot\(s\) wide .*Use at most (?<max>\d+) dot"),
            m => $"bù loang vạch {m.Groups["r"].Value} chấm làm vạch hẹp chỉ còn {m.Groups["left"].Value} chấm, dưới mức tối thiểu 2 chấm nên mã có thể không quét được; bù tối đa {m.Groups["max"].Value} chấm hoặc tăng cỡ X"),
        (Rx(@"^HRI font size must be between"), _ => "cỡ chữ dưới mã vạch nằm ngoài khoảng cho phép"),
        (Rx(@"^HRI mask has (?<p>\d+) position\(s\) for '.' but the text under the barcode has (?<n>\d+)"),
            m => $"mặt nạ chữ dưới mã có {m.Groups["p"].Value} vị trí nhưng dữ liệu có {m.Groups["n"].Value} ký tự nên sẽ làm mất hoặc thêm ký tự; sửa mặt nạ cho khớp độ dài dữ liệu hoặc xóa mặt nạ"),
        (Rx(@"^HRI mask character must be a single character"), _ => "ký tự đại diện của mặt nạ chữ dưới mã phải là một ký tự"),
        (Rx(@"^HRI mask is longer than"), _ => "mặt nạ chữ dưới mã quá dài (tối đa 128 ký tự)"),
        (Rx("^HRI text is (?<w>[0-9.,]+) mm wide but the barcode frame is only (?<f>[0-9.,]+) mm"),
            m => $"dòng chữ dưới mã rộng {m.Groups["w"].Value} mm nhưng khung mã chỉ {m.Groups["f"].Value} mm; nới khung hoặc giảm cỡ chữ"),
        (Rx("^Barcode frame is too short for the HRI strip [(](?<h>[0-9.,]+) mm[)]"),
            m => $"khung mã quá thấp để có dòng chữ dưới mã ({m.Groups["h"].Value} mm); tăng chiều cao hoặc tắt dòng chữ"),
        (Rx("^HRI text has no measurable width or height"), _ => "không đo được dòng chữ dưới mã; chọn phông đã cài và nhập dữ liệu mã"),
        (Rx("^Barcode frame must have positive finite dimensions before HRI"), _ => "khung mã chưa có kích thước hợp lệ"),
        (Rx("^Unsupported HRI placement '"), _ => "vị trí dòng chữ dưới mã không được hỗ trợ"),
        (Rx(@"^A4/Letter/Legal/A3 office sheets are not thermal label stock"), _ => "khổ giấy văn phòng (A4, Letter…) không phải khổ tem"),
        (Rx(@"^DPI (?<d>\d+) is not a known industrial thermal resolution"),
            m => $"DPI {m.Groups["d"].Value} không phải độ phân giải máy in tem đã biết; chọn 203, 300, 305, 600 hoặc 609"),
        (Rx(@"^Print scale "), _ => "tỉ lệ in không hợp lệ; tem công nghiệp in 1:1, chỉ dùng hệ số hiệu chỉnh nhỏ"),
        (Rx(@"^(?:Label|Physical) stock "), _ => "kích thước khổ tem trong Cài đặt máy in không hợp lệ"),
        (Rx(@"^Print method is set to PrinterNative"), _ => "chế độ in bằng lệnh máy in chưa có driver hỗ trợ; chuyển về in đồ họa"),
        (Rx(@"^No labels to print\. Selected rows have zero copies"), _ => "không có tem nào để in: các dòng đã chọn có số bản bằng 0"),
        (Rx(@"^Copies must be a whole number from 1 to (?<max>\d+)\. The current value is (?<v>-?\d+)"),
            m => $"số bản in phải là số nguyên từ 1 đến {m.Groups["max"].Value}; giá trị hiện tại là {m.Groups["v"].Value}"),
        (Rx(@"^A print job cannot exceed (?<max>\d+) labels; the selected rows need (?<n>\d+)"),
            m => $"một lệnh in tối đa {m.Groups["max"].Value} tem, các dòng đã chọn cần {m.Groups["n"].Value} tem; hãy chia thành nhiều lần in"),
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
        "TextBox" => "ô chữ",
        "Rectangle" => "hình chữ nhật",
        "Ellipse" => "hình bầu dục",
        "Line" => "đường kẻ",
        "BarcodeCode128" => "mã vạch",
        "QRCode" => "mã QR",
        "DataMatrix" => "mã Data Matrix",
        "Image" => "hình ảnh",
        "Copies" => "số bản in",
        "LabelStock" => "khổ tem",
        "PrintDpi" => "DPI máy in",
        "PrintScale" => "tỉ lệ in",
        "PrinterProfile" => "cài đặt máy in",
        "Output" => "máy in",
        "Design" => "thiết kế",
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
