using ANLAbel.Core.Printing;
using ANLAbel.Printing.PrinterProfiles;

namespace ANLAbel.App.Services;

/// <summary>
/// Operator text for a finished print, in Vietnamese: the job result with any
/// failure reason explained, and the queue observation. The core texts stay in
/// English because logs, evidence and tests depend on them (audit 2026-10-13).
/// </summary>
public static class PrintStatusText
{
    public static string Result(PrintJobResult result)
    {
        if (string.IsNullOrWhiteSpace(result.ErrorMessage))
        {
            return result.UserFacingStatus;
        }

        return result.Outcome switch
        {
            PrintJobOutcome.Failed => "In thất bại: " + PrintFailureExplainer.Explain(result.ErrorMessage).StatusLine,
            PrintJobOutcome.Unknown => "Chưa rõ kết quả in: " + PrintFailureExplainer.Explain(result.ErrorMessage).StatusLine,
            _ => result.UserFacingStatus,
        };
    }

    public static string Queue(SpoolJobMonitorResult status)
    {
        var observation = status.FinalObservation;
        var state = observation.State switch
        {
            SpoolJobState.NotFound => "không còn thấy trong hàng đợi",
            SpoolJobState.Pending => "đang chờ trong hàng đợi",
            SpoolJobState.Spooling => "đang đưa vào hàng đợi",
            SpoolJobState.Printing => "đang in",
            SpoolJobState.Paused => "hàng đợi đang tạm dừng",
            SpoolJobState.Blocked => "bị chặn",
            SpoolJobState.Offline => "máy in ngoại tuyến",
            SpoolJobState.PaperOut => "máy in hết giấy hoặc hết tem",
            SpoolJobState.UserIntervention => "máy in đang chờ người vận hành xử lý",
            SpoolJobState.Error => "hàng đợi báo lỗi",
            SpoolJobState.Completed => "hàng đợi báo đã xong",
            SpoolJobState.Retained => "hàng đợi đã xử lý xong",
            SpoolJobState.Deleted => "đã bị xóa khỏi hàng đợi",
            _ => "chưa rõ trạng thái",
        };
        var line = $"Lệnh hàng đợi #{observation.JobId}: {state}. Chưa xác minh tem đã ra.";
        return status.TimedOut
            ? $"Đã chờ {status.Elapsed.TotalSeconds:0.#} giây mà hàng đợi chưa báo xong; không tự in lại. {line}"
            : line;
    }
}
