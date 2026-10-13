using System.IO;
using System.Text.RegularExpressions;

namespace ANLAbel.App.Services;

/// <summary>
/// Plain-language explanation of a template file that could not be opened or
/// saved: what happened and what to do (audit 2026-10-13). The core messages
/// stay unchanged for the details line and the logs.
/// </summary>
public static class TemplateFailureExplainer
{
    private const int SharingViolation = unchecked((int)0x80070020);
    private const int LockViolation = unchecked((int)0x80070021);
    private const int HandleDiskFull = unchecked((int)0x80070027);
    private const int DiskFull = unchecked((int)0x80070070);

    private static readonly (Regex Pattern, string Problem, string Action)[] Rules =
    [
        (Rx("^The template uses schema version [0-9]+, but this build supports up to [0-9]+"),
            "Tệp mẫu được lưu bởi phiên bản ANLAbel mới hơn bản đang dùng.",
            "Cập nhật ANLAbel rồi mở lại tệp."),
        (Rx("^The existing template cannot be overwritten because it uses an unsupported schema"),
            "Không ghi đè được: tệp hiện có do phiên bản ANLAbel mới hơn lưu.",
            "Dùng Lưu thành… để lưu sang tệp khác, hoặc cập nhật ANLAbel."),
        (Rx("^(?:The template schema version -?[0-9]+ is not supported|The template file format marker is missing or unsupported)"),
            "Tệp đã chọn không phải mẫu ANLAbel được hỗ trợ.",
            "Kiểm tra lại tệp, hoặc mở một tệp .anlabel khác."),
        (Rx("^The template and its recovery backup are both invalid"),
            "Cả tệp mẫu và bản sao lưu của nó đều bị hỏng.",
            "Mở một phiên bản cũ trong Lịch sử phiên bản, hoặc dùng tệp mẫu khác."),
        (Rx("^(?:The template file contains malformed JSON|The template file root must be a JSON object|The template schemaVersion is (?:invalid|missing)|The versioned template (?:envelope|payload)|The legacy template payload is invalid|The template file is invalid)"),
            "Tệp mẫu bị hỏng hoặc không đọc được.",
            "Mở bản sao lưu (.bak) hoặc một phiên bản cũ trong Lịch sử phiên bản."),
    ];

    /// <summary>Set on an open failure whose recovery backup is still readable.</summary>
    public const string BackupPathKey = "ANLAbel.TemplateBackupPath";

    /// <summary>The technical reason the primary file could not be read.</summary>
    public const string PrimaryErrorKey = "ANLAbel.TemplatePrimaryError";

    public static PrintFailureExplanation Explain(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        if (exception.Data[BackupPathKey] is string backupPath)
        {
            var primaryError = exception.Data[PrimaryErrorKey] as string;
            return new PrintFailureExplanation(
                $"{ExplainMessage(primaryError).Problem} Bản sao lưu của tệp vẫn đọc được: “{backupPath}”.",
                "Mẫu đang mở không bị thay. Mở tệp sao lưu đó nếu muốn dùng nó.",
                primaryError ?? exception.Message);
        }

        var technical = exception.Message.Trim();
        if (TryMatch(technical) is { } known)
        {
            return known;
        }

        return exception switch
        {
            FileNotFoundException or DirectoryNotFoundException => new(
                "Không tìm thấy tệp mẫu hoặc thư mục chứa nó.",
                "Kiểm tra tệp còn ở vị trí cũ và ổ mạng đã kết nối, rồi thử lại.",
                technical),
            IOException io when io.HResult is SharingViolation or LockViolation => new(
                "Tệp mẫu đang bị chương trình khác mở hoặc khóa.",
                "Đóng chương trình đó (hoặc cửa sổ ANLAbel khác) rồi thử lại.",
                technical),
            IOException io when io.HResult is DiskFull or HandleDiskFull => new(
                "Ổ đĩa đã đầy.",
                "Giải phóng dung lượng ổ đĩa hoặc lưu sang ổ khác.",
                technical),
            UnauthorizedAccessException => new(
                "ANLAbel không có quyền đọc hoặc ghi tệp mẫu này.",
                "Kiểm tra tệp không ở chế độ chỉ đọc và thư mục cho phép ghi, hoặc lưu sang thư mục khác.",
                technical),
            _ => OperationFailureExplainer.Explain(exception),
        };
    }

    /// <summary>A template load error message, for example a backup's primary error.</summary>
    public static PrintFailureExplanation ExplainMessage(string? technicalMessage)
    {
        var technical = (technicalMessage ?? string.Empty).Trim();
        return TryMatch(technical) ?? new PrintFailureExplanation(
            "Tệp mẫu bị hỏng hoặc không đọc được.",
            "Mở bản sao lưu (.bak) hoặc một phiên bản cũ trong Lịch sử phiên bản.",
            technical)
        {
            IsGeneric = true,
        };
    }

    private static PrintFailureExplanation? TryMatch(string technical)
    {
        foreach (var (pattern, problem, action) in Rules)
        {
            if (pattern.IsMatch(technical))
            {
                return new PrintFailureExplanation(problem, action, technical);
            }
        }

        return null;
    }

    private static Regex Rx(string pattern) => new(pattern, RegexOptions.CultureInvariant | RegexOptions.Compiled);
}
