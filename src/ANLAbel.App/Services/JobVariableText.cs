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
        (Rx(@"^Prompt '(?<p>.*)' needs a date such as (?<e>.*)\.$"), m => $"“{m.Groups["p"].Value}” cần một ngày, ví dụ {m.Groups["e"].Value}"),
        (Rx(@"^Prompt '(?<p>.*)' does not accept '(?<c>.)'; allowed characters: (?<a>.*)\.$"),
            m => $"“{m.Groups["p"].Value}” không nhận ký tự “{m.Groups["c"].Value}”; chỉ được dùng: {m.Groups["a"].Value}"),
        (Rx(@"^Prompt '(?<p>.*)' must be one of: (?<v>.*)\.$"),
            m => $"“{m.Groups["p"].Value}” phải là một giá trị trong danh sách: {m.Groups["v"].Value}"),
        (Rx(@"^Prompt '(?<p>.*)' needs exactly (?<n>\d+) characters; (?<k>\d+) were entered"),
            m => $"“{m.Groups["p"].Value}” cần đúng {m.Groups["n"].Value} ký tự, đã nhập {m.Groups["k"].Value}"),
        (Rx(@"^Prompt '(?<p>.*)' needs at least (?<n>\d+) characters but allows at most (?<m>\d+)"),
            m => $"biến “{m.Groups["p"].Value}” cần ít nhất {m.Groups["n"].Value} ký tự nhưng chỉ cho tối đa {m.Groups["m"].Value}, nên người in không nhập được giá trị nào; sửa số ký tự tối thiểu hoặc tối đa"),
        (Rx(@"^Prompt '(?<p>.*)' needs at least (?<n>\d+) characters; (?<k>\d+) were entered"),
            m => $"“{m.Groups["p"].Value}” cần ít nhất {m.Groups["n"].Value} ký tự, mới nhập {m.Groups["k"].Value}"),
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
        if (text.StartsWith("Transform ", StringComparison.Ordinal))
        {
            // A function reading a prompt or date runs with the job's values.
            return FormulaErrorText.Translate(text);
        }

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
