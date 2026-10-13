using System.Text.RegularExpressions;
using ANLAbel.Core.Printing;

namespace ANLAbel.App.Services;

/// <summary>
/// Vietnamese text for the Printer Setup compatibility card (audit
/// 2026-10-13). The media resolver keeps its English message for logs and
/// tests; the card shows this text and keeps the original as its tooltip.
/// </summary>
public static class MediaCompatibilityText
{
    private const string Number = "[0-9.,]+";

    private static readonly (Regex Pattern, Func<Match, string> Text)[] Rules =
    [
        (Rx($"^Current driver preferences own the print ticket, but driver configured page (?<pw>{Number}) × (?<ph>{Number}) mm does not match authored label (?<lw>{Number}) × (?<lh>{Number}) mm"),
            m => $"Thiết lập trình điều khiển đang quyết định khổ in, nhưng trang {m.Groups["pw"].Value} × {m.Groups["ph"].Value} mm không khớp tem {m.Groups["lw"].Value} × {m.Groups["lh"].Value} mm. Chọn nguồn thiết lập “Tem” để dùng khổ của tem, hoặc chỉnh trong Tùy chọn trình điều khiển."),
        (Rx("^Current driver preferences own the print ticket [(]effective page"),
            _ => "Thiết lập trình điều khiển đang quyết định khổ in và trang khớp với tem. Khổ chọn tay không được dùng."),
        (Rx("^Current driver preferences own the print ticket[.]"),
            _ => "Thiết lập trình điều khiển đang quyết định khổ in; trình điều khiển đặt khổ trang khi in. Khổ chọn tay không được dùng."),
        (Rx("^Physical page sizing will be resolved by the printer driver at dispatch time"),
            _ => "Trình điều khiển sẽ đặt khổ trang theo kích thước tem khi in."),
        (Rx("^The requested physical media dimensions are invalid"),
            _ => "Kích thước khổ giấy không hợp lệ."),
        (Rx($"^Physical media (?<mw>{Number}) × (?<mh>{Number}) mm does not contain the authored label (?<lw>{Number}) × (?<lh>{Number}) mm"),
            m => $"Khổ giấy {m.Groups["mw"].Value} × {m.Groups["mh"].Value} mm không chứa được tem {m.Groups["lw"].Value} × {m.Groups["lh"].Value} mm. Với tem cuộn, chọn “Tự động đặt khổ trang theo kích thước tem”, hoặc chọn khổ giấy đủ lớn."),
        (Rx("^The requested physical size is within the custom-media range"),
            _ => "Kích thước nằm trong khoảng khổ tùy chỉnh mà trình điều khiển hỗ trợ."),
        (Rx("^The driver claims custom-media support without usable size limits"),
            _ => "Trình điều khiển báo hỗ trợ khổ tùy chỉnh nhưng không cho biết giới hạn kích thước."),
        (Rx("^The requested physical size is outside the driver custom-media range (?<r>.+) mm"),
            m => $"Kích thước nằm ngoài khoảng khổ tùy chỉnh của trình điều khiển ({m.Groups["r"].Value} mm)."),
        (Rx("^Printer media capabilities are incomplete"),
            _ => "Thông tin khổ giấy của máy in chưa đầy đủ; khổ giấy sẽ được kiểm tra lại trước khi in."),
        (Rx("^No same-orientation driver media matches the requested physical size"),
            _ => "Không có khổ giấy cùng hướng trong trình điều khiển khớp với kích thước tem."),
        (Rx("^Driver media '(?<n>[^']*)' matches the requested physical size"),
            m => $"Khổ giấy “{m.Groups["n"].Value}” của trình điều khiển khớp với kích thước tem."),
        (Rx("^Media '(?<n>[^']*)' matches the requested physical size but is a software stock"),
            m => $"Khổ “{m.Groups["n"].Value}” khớp kích thước nhưng chỉ là khổ mẫu của phần mềm, không phải khổ giấy trình điều khiển công bố; chưa thể dùng để in."),
        (Rx("^The selected printer queue is unavailable"),
            _ => "Hàng đợi máy in đã chọn không khả dụng."),
    ];

    public static string Describe(PrinterMediaResolution resolution)
    {
        ArgumentNullException.ThrowIfNull(resolution);
        var message = (resolution.Message ?? string.Empty).Trim();
        foreach (var (pattern, text) in Rules)
        {
            var match = pattern.Match(message);
            if (match.Success)
            {
                return text(match);
            }
        }

        return resolution.Status switch
        {
            PrinterMediaResolutionStatus.Compatible => "Khổ giấy tương thích với tem.",
            PrinterMediaResolutionStatus.CustomSupported => "Kích thước nằm trong khoảng khổ tùy chỉnh mà trình điều khiển hỗ trợ.",
            PrinterMediaResolutionStatus.DriverAutomatic => "Trình điều khiển sẽ đặt khổ trang khi in.",
            PrinterMediaResolutionStatus.SoftwareStockOnly => "Khổ đã chọn chỉ là khổ mẫu của phần mềm; chưa thể dùng để in.",
            PrinterMediaResolutionStatus.QueueBlocked => PrintFailureExplainer.ExplainQueueStatus(message).StatusLine,
            PrinterMediaResolutionStatus.Mismatch => "Khổ giấy không khớp với tem.",
            PrinterMediaResolutionStatus.Unavailable => "Không đọc được thông tin máy in đã chọn.",
            _ => "Chưa xác định được khổ giấy có tương thích không; sẽ kiểm tra lại trước khi in.",
        };
    }

    private static Regex Rx(string pattern) => new(pattern, RegexOptions.CultureInvariant | RegexOptions.Compiled);
}
