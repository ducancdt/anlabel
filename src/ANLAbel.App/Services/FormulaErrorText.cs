using System.Text.RegularExpressions;

namespace ANLAbel.App.Services;

/// <summary>
/// Vietnamese text for the formula and data-transform errors of the core
/// expression engine (audit 2026-10-13). The core messages stay unchanged;
/// an unknown message is shown as it is.
/// </summary>
public static class FormulaErrorText
{
    private static readonly (Regex Pattern, Func<Match, string> Text)[] Rules =
    [
        (Rx("^Formula is empty[.]$"), _ => "Công thức đang trống."),
        (Rx("^Unexpected token '(?<t>.*)' at position (?<p>[0-9]+)[.]$"), m => $"Ký tự “{m.Groups["t"].Value}” không hợp lệ ở vị trí {m.Groups["p"].Value}."),
        (Rx("^Expected expression but reached end of formula[.]$"), _ => "Công thức chưa đầy đủ."),
        (Rx("^Expected '[(]' after function name '(?<n>.*)'[.]$"), m => $"Thiếu “(” sau tên hàm “{m.Groups["n"].Value}”."),
        (Rx("^Expected ',' or '[)]' after argument in function '(?<n>.*)'[.]$"), m => $"Thiếu “,” hoặc “)” sau đối số của hàm “{m.Groups["n"].Value}”."),
        (Rx("^Expected '[)]' to close function '(?<n>.*)'[.]$"), m => $"Thiếu “)” để đóng hàm “{m.Groups["n"].Value}”."),
        (Rx("^Expected string literal[.]$"), _ => "Cần một chuỗi trong dấu nháy."),
        (Rx("^Unterminated escape sequence in string literal[.]$"), _ => "Chuỗi có ký tự thoát chưa hoàn tất."),
        (Rx("^Unterminated string literal[.]$"), _ => "Chuỗi thiếu dấu nháy đóng."),
        (Rx("^Unknown function '(?<n>.*)'[.]$"), m => $"Hàm “{m.Groups["n"].Value}” không được hỗ trợ."),
        (Rx("^FIELD expects exactly one argument[.]$"), _ => "Hàm FIELD cần đúng một đối số."),
        (Rx("^FIELD field name is empty[.]$"), _ => "Hàm FIELD chưa có tên trường."),
        (Rx("^Field '(?<n>.*)' was not found[.]$"), m => $"Không tìm thấy trường “{m.Groups["n"].Value}”."),
        (Rx("^A transform output field name is required[.]$"), _ => "Trường chuyển đổi cần có tên."),
        (Rx("^Transform output '(?<n>.*)' conflicts with a source field[.]$"), m => $"Trường chuyển đổi “{m.Groups["n"].Value}” trùng tên với một cột dữ liệu."),
        (Rx("^Transform output '(?<n>.*)' is defined more than once[.]$"), m => $"Trường chuyển đổi “{m.Groups["n"].Value}” được khai báo nhiều lần."),
        (Rx("^Transform dependency cycle detected: (?<n>.*)[.]$"), m => $"Các trường chuyển đổi phụ thuộc vòng: {m.Groups["n"].Value}."),
        (Rx("^Transform '(?<n>[^']*)': (?<inner>.*)$"), m => $"Trường chuyển đổi “{m.Groups["n"].Value}”: {Translate(m.Groups["inner"].Value)}"),
    ];

    public static string Translate(string? error)
    {
        var text = (error ?? string.Empty).Trim();
        foreach (var (pattern, translate) in Rules)
        {
            var match = pattern.Match(text);
            if (match.Success)
            {
                return translate(match);
            }
        }

        return text;
    }

    public static string Join(IEnumerable<string> errors) => string.Join(Environment.NewLine, errors.Select(Translate));

    private static Regex Rx(string pattern) => new(pattern, RegexOptions.CultureInvariant | RegexOptions.Compiled);
}
