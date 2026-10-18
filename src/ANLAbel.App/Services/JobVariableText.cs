using System.Text.RegularExpressions;

namespace ANLAbel.App.Services;

/// <summary>
/// Vietnamese wording for job variable messages (date fields and values
/// entered at print time) at the UI boundary; unknown messages keep their
/// technical text.
/// </summary>
public static class JobVariableText
{
    private static readonly (Regex Pattern, Func<Match, string> Text)[] Rules =
    [
        (Rx(@"^Prompt '(?<p>.*)' needs a value"), m => $"chưa nhập “{m.Groups["p"].Value}”"),
        (Rx(@"^Prompt '(?<p>.*)' allows at most (?<n>\d+) characters; (?<k>\d+) were entered"),
            m => $"“{m.Groups["p"].Value}” chỉ cho tối đa {m.Groups["n"].Value} ký tự, đang nhập {m.Groups["k"].Value}"),
        (Rx(@"^Prompt '(?<p>.*)' accepts digits only"), m => $"“{m.Groups["p"].Value}” chỉ nhận chữ số"),
        (Rx(@"^Date field '(?<t>.*)' is not understood"),
            m => $"trường ngày “{m.Groups["t"].Value}” sai cú pháp; dùng {{#NOW}}, {{#NOW:dd/MM/yyyy}} hoặc {{#NOW+30d:dd/MM/yyyy}} (đơn vị d, w, mo, y, h)"),
        (Rx(@"^Date field '(?<t>.*)' has an invalid format '(?<f>.*)'"),
            m => $"trường ngày “{m.Groups["t"].Value}” có định dạng “{m.Groups["f"].Value}” không hợp lệ"),
        (Rx(@"^Fields (?<f>.+) differ only in letter case"),
            m => $"các trường {m.Groups["f"].Value} chỉ khác chữ hoa/thường nên không thể in cùng lúc; đổi tên hoặc định dạng một trường"),
        (Rx(@"^The print job value record .* is damaged at line (?<l>\d+)"),
            m => $"sổ ghi giá trị lệnh in bị hỏng ở dòng {m.Groups["l"].Value} nên lệnh in lại không lấy lại được ngày và giá trị đã nhập; khôi phục tệp từ bản sao lưu"),
        (Rx(@"^Another ANLAbel window is still recording print job values"),
            _ => "một cửa sổ ANLAbel khác đang ghi giá trị lệnh in; thử lại sau giây lát"),
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

        return string.IsNullOrEmpty(text) ? text : $"Lỗi giá trị lệnh in. Chi tiết kỹ thuật: {text}";
    }

    public static bool IsKnown(string? message)
    {
        var text = (message ?? string.Empty).Trim();
        return Rules.Any(rule => rule.Pattern.IsMatch(text));
    }

    private static Regex Rx(string pattern) => new(pattern, RegexOptions.CultureInvariant | RegexOptions.Singleline);
}
