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
        (Rx("^(?<n>[A-Z0-9]+) expects exactly one argument[.]$"), m => $"Hàm {m.Groups["n"].Value} cần đúng một đối số."),
        (Rx("^(?<n>LEFT|RIGHT) expects two arguments: text, count[.]$"), m => $"Hàm {m.Groups["n"].Value} cần hai đối số: văn bản, số ký tự."),
        (Rx("^EQUALS expects two arguments[.]$"), _ => "Hàm EQUALS cần hai giá trị để so sánh."),
        (Rx("^DATEFORMAT expects a date, a format and an optional read format[.]$"), _ => "Hàm DATEFORMAT cần một ngày, một định dạng và (tùy chọn) định dạng đọc."),
        (Rx("^DATEADD expects a date, an amount, a unit [(]d, w, mo, y[)] and an optional format and read format[.]$"), _ => "Hàm DATEADD cần ngày, số cộng thêm, đơn vị (d, w, mo, y) và (tùy chọn) định dạng in, định dạng đọc."),
        (Rx("^DATEADD amount must be a whole number[.]$"), _ => "Số cộng thêm của DATEADD phải là số nguyên."),
        (Rx("^HIBCCHECK expects exactly one argument[.]$"), _ => "Hàm HIBCCHECK cần đúng một giá trị."),
        (Rx("^HIBCCHECK accepts digits, capital letters and - [.] space [$] / [+] % only[.]$"), _ => "Mã HIBC chỉ nhận chữ số, chữ in hoa và các ký tự - . dấu cách $ / + %."),
        (Rx("^DATEADD unit must be d, w, mo or y[.]$"), _ => "Đơn vị của DATEADD phải là d (ngày), w (tuần), mo (tháng) hoặc y (năm)."),
        (Rx("^DATEADD moves the date out of range[.]$"), _ => "DATEADD đưa ngày ra ngoài phạm vi cho phép."),
        (Rx("^Date '(?<d>.*)' is not understood; give it as yyyy-MM-dd or dd/MM/yyyy, or name its format[.]$"), m => $"Không đọc được ngày “{m.Groups["d"].Value}”; hãy ghi dạng yyyy-MM-dd hoặc dd/MM/yyyy, hoặc nêu định dạng của nó."),
        (Rx("^Date format '(?<f>.*)' is not valid[.]$"), m => $"Định dạng ngày “{m.Groups["f"].Value}” không hợp lệ."),
        (Rx("^AND expects at least one argument[.]$"), _ => "Hàm AND cần ít nhất một điều kiện."),
        (Rx("^(?<n>LEFT|RIGHT) count must be a whole number of 0 or more[.]$"), m => $"Số ký tự của hàm {m.Groups["n"].Value} phải là số nguyên từ 0 trở lên."),
        (Rx("^MID expects exactly three arguments: text, start, length[.]$"), _ => "Hàm MID cần ba đối số: văn bản, vị trí bắt đầu, số ký tự."),
        (Rx("^MID start must be a whole number of 1 or more[.]$"), _ => "Vị trí bắt đầu của hàm MID phải là số nguyên từ 1 trở lên."),
        (Rx("^MID length must be a whole number of 0 or more[.]$"), _ => "Số ký tự của hàm MID phải là số nguyên từ 0 trở lên."),
        (Rx("^GS1CHECKDIGIT needs digits only[.]$"), _ => "Hàm GS1CHECKDIGIT chỉ nhận chữ số."),
        (Rx("^FIELD field name is empty[.]$"), _ => "Hàm FIELD chưa có tên trường."),
        (Rx("^Field '(?<n>.*)' was not found[.]$"), m => $"Không tìm thấy trường “{m.Groups["n"].Value}”."),
        // Transforms are the Data Manager's functions ("Hàm"); the messages use that name.
        (Rx("^A transform output field name is required[.]$"), _ => "Một hàm chưa có tên trường kết quả."),
        (Rx("^Transform output '(?<n>.*)' conflicts with a source field[.]$"), m => $"Hàm “{m.Groups["n"].Value}” trùng tên với một cột dữ liệu; đổi tên hàm."),
        (Rx("^Transform output '(?<n>.*)' is defined more than once[.]$"), m => $"Có nhiều hàm cùng tên “{m.Groups["n"].Value}”; đổi tên để mỗi hàm một tên."),
        (Rx("^Transform dependency cycle detected: (?<n>.*)[.]$"), m => $"Các hàm đọc lẫn nhau thành vòng: {m.Groups["n"].Value}; sửa công thức để không hàm nào đọc lại chính nó."),
        (Rx("^Transform '(?<n>[^']*)': (?<inner>.*)$"), m => $"Công thức của hàm “{m.Groups["n"].Value}”: {Translate(m.Groups["inner"].Value)}"),
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
