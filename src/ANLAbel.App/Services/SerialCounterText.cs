using System.Text.RegularExpressions;

namespace ANLAbel.App.Services;

/// <summary>
/// Vietnamese wording for serial counter messages at the UI boundary. The
/// counter contract and store keep English diagnostics for logs and tests; an
/// unknown message is shown with its original technical text.
/// </summary>
public static class SerialCounterText
{
    private static readonly (Regex Pattern, Func<Match, string> Text)[] Rules =
    [
        (Rx(@"^Serial counter has no identity"), _ => "bộ đếm chưa có mã định danh; tắt rồi bật lại bộ đếm"),
        (Rx(@"^Serial counter step must not be 0"), _ => "bước tăng của bộ đếm không được bằng 0"),
        (Rx(@"^Serial counter must change every 1 to (?<max>\d+) labels"),
            m => $"số tem dùng chung một giá trị phải từ 1 đến {m.Groups["max"].Value}"),
        (Rx(@"^Serial counter minimum (?<min>-?\d+) is greater than its maximum (?<max>-?\d+)"),
            m => $"giá trị nhỏ nhất {m.Groups["min"].Value} lớn hơn giá trị lớn nhất {m.Groups["max"].Value} của bộ đếm"),
        (Rx(@"^Serial counter start value (?<v>-?\d+) is outside (?<min>-?\d+) to (?<max>-?\d+)"),
            m => $"giá trị bắt đầu {m.Groups["v"].Value} nằm ngoài khoảng {m.Groups["min"].Value} – {m.Groups["max"].Value} của bộ đếm"),
        (Rx(@"^Serial counter digits must be"), _ => "số chữ số của bộ đếm phải từ 0 đến 18"),
        (Rx(@"^Serial counter prefix and suffix are limited"), _ => "tiền tố và hậu tố của bộ đếm tối đa 32 ký tự"),
        (Rx(@"^Serial counter runs past its (?:maximum|minimum) (?<lim>-?\d+) at label (?<i>\d+) of (?<n>\d+)"),
            m => $"bộ đếm vượt giới hạn {m.Groups["lim"].Value} ở tem thứ {m.Groups["i"].Value}/{m.Groups["n"].Value}; in ít tem hơn, nới giới hạn, bật quay vòng hoặc đặt lại bộ đếm"),
        (Rx(@"^Serial numbers changed since the preview: .* the next number is now (?<v>.+?)\. Check"),
            m => $"số serial đã thay đổi kể từ lúc xem trước vì một lệnh in khác vừa dùng; số kế tiếp hiện là {m.Groups["v"].Value}. Kiểm tra lại xem trước rồi in"),
        (Rx(@"^Serial numbers for job .* were already reserved"),
            _ => "lệnh in này đã được cấp dải số serial khác; tạo lệnh in mới"),
        (Rx(@"^The serial counter record .* is damaged at line (?<l>\d+)"),
            m => $"sổ ghi bộ đếm serial bị hỏng ở dòng {m.Groups["l"].Value} nên không biết chắc số kế tiếp; khôi phục tệp từ bản sao lưu trước khi in số serial"),
        (Rx(@"^Another ANLAbel window is still reserving serial numbers"),
            _ => "một cửa sổ ANLAbel khác đang cấp số serial; thử lại sau giây lát"),
        (Rx(@"^Serial counter minimum (?<min>-?\d+) must not be negative with a character sequence alphabet"),
            m => $"giá trị nhỏ nhất {m.Groups["min"].Value} không được âm khi bộ đếm dùng dãy ký tự; đặt nhỏ nhất từ 0 trở lên"),
        (Rx(@"^Serial counter custom alphabet must have (?<lo>\d+) to (?<hi>\d+) characters; it has (?<n>\d+)"),
            m => $"dãy ký tự tự định nghĩa phải có {m.Groups["lo"].Value} – {m.Groups["hi"].Value} ký tự; hiện có {m.Groups["n"].Value}"),
        (Rx(@"^Serial counter custom alphabet repeats a character"),
            _ => "dãy ký tự tự định nghĩa có ký tự lặp lại nên hai giá trị có thể in trùng một số serial; bỏ ký tự lặp"),
        (Rx(@"^Serial counter custom alphabet must not contain spaces"),
            _ => "dãy ký tự tự định nghĩa không được chứa dấu cách hoặc ký tự điều khiển"),
        (Rx(@"^Serial counter custom alphabet is too short"),
            _ => "dãy ký tự tự định nghĩa quá ngắn để đếm; sửa định nghĩa bộ đếm trước khi in"),
    ];

    public static string Message(string? message)
    {
        var text = (message ?? string.Empty).Trim();
        foreach (var (pattern, describe) in Rules)
        {
            var match = pattern.Match(text);
            if (match.Success)
            {
                var result = describe(match);
                return char.ToUpperInvariant(result[0]) + result[1..] + ".";
            }
        }

        return string.IsNullOrEmpty(text) ? text : $"Lỗi bộ đếm serial. Chi tiết kỹ thuật: {text}";
    }

    public static bool IsKnown(string? message)
    {
        var text = (message ?? string.Empty).Trim();
        return Rules.Any(rule => rule.Pattern.IsMatch(text));
    }

    private static Regex Rx(string pattern) => new(pattern, RegexOptions.CultureInvariant | RegexOptions.Singleline);
}
