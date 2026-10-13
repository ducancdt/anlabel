using System.IO;
using System.Text.RegularExpressions;
using ANLAbel.Data.Excel;

namespace ANLAbel.App.Services;

/// <summary>
/// Plain-language explanation of a data import or refresh failure (plan 13
/// GĐ4, H14): which file, sheet, row and column, and how to fix it. The
/// technical message stays in the details line and the logs.
/// </summary>
public static class DataFailureExplainer
{
    private const int SharingViolation = unchecked((int)0x80070020);
    private const int LockViolation = unchecked((int)0x80070021);

    /// <summary>Returns null when the failure is not a data-source problem.</summary>
    public static PrintFailureExplanation? TryExplain(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return exception switch
        {
            ExcelDataReadException data => Explain(data),
            TimeoutException => new(
                "Đọc tệp dữ liệu quá lâu nên ANLAbel đã dừng.",
                "Đóng các chương trình đang mở tệp, kiểm tra ổ mạng rồi thử lại. Nếu tệp rất lớn, hãy tách thành nhiều tệp nhỏ.",
                exception.Message),
            IOException io when io.HResult is SharingViolation or LockViolation => new(
                "Tệp dữ liệu đang bị chương trình khác khóa.",
                "Đóng tệp trong Excel hoặc chương trình khác rồi thử lại.",
                exception.Message),
            UnauthorizedAccessException => new(
                "ANLAbel không có quyền đọc tệp dữ liệu.",
                "Kiểm tra quyền truy cập tệp hoặc thư mục rồi thử lại.",
                exception.Message),
            _ => null,
        };
    }

    private static PrintFailureExplanation Explain(ExcelDataReadException data)
    {
        var file = Path.GetFileName(data.FilePath);
        var sheet = string.IsNullOrWhiteSpace(data.SheetName) ? $"tệp “{file}”" : $"sheet “{data.SheetName}” của tệp “{file}”";
        var message = data.Message;
        var (problem, action) = data.Error switch
        {
            ExcelDataReadError.MissingFile => (
                $"Không tìm thấy tệp dữ liệu “{file}”.",
                "Kiểm tra tệp còn ở vị trí cũ, hoặc dùng “Liên kết lại” để chọn tệp."),
            ExcelDataReadError.MissingSheet => (
                $"Tệp “{file}” không có sheet “{data.SheetName}”.{AvailableSheets(message)}",
                "Chọn lại sheet khi nhập dữ liệu, hoặc đổi tên sheet trong Excel cho khớp."),
            ExcelDataReadError.InvalidWorkbook => (
                $"Tệp “{file}” bị hỏng hoặc không phải tệp Excel (.xlsx, .xlsm) hợp lệ.",
                "Mở tệp bằng Excel, lưu lại dưới dạng .xlsx rồi thử lại."),
            ExcelDataReadError.InvalidHeaderRow => (
                $"Dòng tiêu đề đã chọn nằm ngoài vùng có dữ liệu của {sheet}.",
                "Nhập lại dữ liệu và chọn đúng dòng tiêu đề."),
            _ => ExplainInvalidData(data, file, sheet, message),
        };
        return new PrintFailureExplanation(problem, action, message);
    }

    private static (string Problem, string Action) ExplainInvalidData(ExcelDataReadException data, string file, string sheet, string message)
    {
        var cell = data.RowNumber is { } row
            ? data.ColumnNumber is { } column ? $"ô {ColumnName(column)}{row} (dòng {row}, cột {column})" : $"dòng {row}"
            : "một ô";

        if (Regex.Match(message, @"contains an Excel error \((?<error>[^)]*)\)", RegexOptions.CultureInvariant) is { Success: true } error)
        {
            return ($"{Capitalize(cell)} trong {sheet} đang báo lỗi Excel {error.Groups["error"].Value}.",
                "Sửa công thức hoặc giá trị của ô đó trong Excel, lưu lại rồi cập nhật dữ liệu.");
        }

        if (message.Contains("has a formula without a calculated value", StringComparison.Ordinal))
        {
            return ($"{Capitalize(cell)} trong {sheet} có công thức nhưng tệp chưa lưu giá trị đã tính, nên tem sẽ in trống ở đó.",
                "Mở tệp bằng Excel để tính lại, lưu tệp rồi cập nhật dữ liệu.");
        }

        if (message.Contains("uses an unsupported number format", StringComparison.Ordinal))
        {
            return ($"{Capitalize(cell)} trong {sheet} dùng định dạng số mà ANLAbel không đọc được.",
                "Đổi định dạng ô đó trong Excel (ví dụ sang Text hoặc Number), lưu lại rồi cập nhật dữ liệu.");
        }

        if (Regex.Match(message, @"Record (?<row>\d+) has (?<fields>\d+) fields; the header defines (?<header>\d+)", RegexOptions.CultureInvariant) is { Success: true } shape)
        {
            return ($"Dòng {shape.Groups["row"].Value} của tệp CSV “{file}” có {shape.Groups["fields"].Value} trường, còn dòng tiêu đề có {shape.Groups["header"].Value} trường.",
                "Sửa dòng đó cho đủ trường (ô trống vẫn cần dấu phân cách), lưu lại rồi nhập lại.");
        }

        if (message.Contains("not valid for its encoding", StringComparison.Ordinal))
        {
            return ($"Tệp CSV “{file}” có ký tự không đúng bảng mã.",
                "Lưu lại tệp CSV với bảng mã UTF-8 rồi nhập lại.");
        }

        if (message.StartsWith("CSV file ", StringComparison.Ordinal))
        {
            return ($"Tệp CSV “{file}” sai cú pháp{(data.RowNumber is { } csvRow ? $" ở dòng {csvRow}" : string.Empty)}.",
                "Kiểm tra dấu ngoặc kép và dấu phân cách trong tệp, lưu lại rồi nhập lại.");
        }

        return ($"Dữ liệu trong {sheet} không hợp lệ ({cell}).",
            "Xem chi tiết kỹ thuật, sửa tệp nguồn rồi thử lại.");
    }

    private static string AvailableSheets(string message)
    {
        var match = Regex.Match(message, @"Available sheets: (?<sheets>.*?)\.?$", RegexOptions.CultureInvariant);
        return match.Success && match.Groups["sheets"].Value.Length > 0
            ? $" Các sheet hiện có: {match.Groups["sheets"].Value}."
            : string.Empty;
    }

    private static string Capitalize(string text) => text.Length == 0 ? text : char.ToUpperInvariant(text[0]) + text[1..];

    private static string ColumnName(int column)
    {
        var name = string.Empty;
        for (var value = column; value > 0; value = (value - 1) / 26)
        {
            name = (char)('A' + (value - 1) % 26) + name;
        }

        return name;
    }
}

/// <summary>
/// Explanation for any failed operation shown to the operator: data-source
/// problems first, then known print stops, otherwise a neutral message that
/// keeps the technical text (plan 13 F03/H25).
/// </summary>
public static class OperationFailureExplainer
{
    public static PrintFailureExplanation Explain(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        if (DataFailureExplainer.TryExplain(exception) is { } data)
        {
            return data;
        }

        var print = PrintFailureExplainer.Explain(exception.Message);
        return print.IsGeneric
            ? new PrintFailureExplanation(
                "ANLAbel không hoàn tất được thao tác vừa rồi.",
                "Xem chi tiết kỹ thuật bên dưới rồi thử lại. Nếu lỗi lặp lại, gửi tệp logs\\errors.log cho bộ phận hỗ trợ.",
                print.Technical)
            {
                IsGeneric = true,
            }
            : print;
    }

    /// <summary>True when the failure came from reading a data source.</summary>
    public static bool IsDataFailure(Exception exception) => DataFailureExplainer.TryExplain(exception) is not null;
}
