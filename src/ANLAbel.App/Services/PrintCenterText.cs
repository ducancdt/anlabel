using System.Text.RegularExpressions;
using ANLAbel.Core.Printing;
using ANLAbel.Data.PrintLogs;

namespace ANLAbel.App.Services;

/// <summary>
/// Vietnamese text for the Print Center and the print-recovery status bar
/// (audit 2026-10-13). The event log keeps its English reasons and summaries
/// as evidence; this is only what the operator reads.
/// </summary>
public static class PrintCenterText
{
    private static readonly (Regex Pattern, string Text)[] ReasonRules =
    [
        (Rx("^The event log has a corrupt or incomplete tail"), "Nhật ký lệnh in bị hỏng ở cuối; lưu trữ nhật ký hỏng trước khi in lại."),
        (Rx("^An operator acknowledged this uncertain job"), "Đã ghi nhận lệnh chưa rõ kết quả; xem bằng chứng hàng đợi hoặc hủy lệnh trước khi in lại."),
        (Rx("^A linked reprint request [(]"), "Đã có yêu cầu in lại liên kết; việc in là một thao tác riêng."),
        (Rx("^A linked reprint was approved against manifest"), "Đã duyệt in lại theo bản kê; khi in, mẫu và dữ liệu hiện tại vẫn phải khớp."),
        (Rx("^The queue reported a terminal state, but physical output is not verified"), "Hàng đợi báo lệnh đã kết thúc nhưng chưa xác minh tem đã ra; người vận hành cần quyết định."),
        (Rx("^A printer and spool identity are available"), "Có thông tin máy in và mã lệnh hàng đợi; kiểm tra hàng đợi trước khi quyết định in lại."),
        (Rx("^Dispatch reached an uncertain state without a safely reusable queue identity"), "Lệnh dừng ở trạng thái chưa rõ và không có mã hàng đợi để kiểm tra; người vận hành cần quyết định."),
        (Rx("^The process stopped before a terminal print outcome was recorded"), "ANLAbel dừng trước khi ghi được kết quả in; không tự in lại."),
        (Rx("^The recovery candidate has no safe printer/job identity"), "Lệnh này không có thông tin máy in và hàng đợi an toàn để kiểm tra; người vận hành cần quyết định."),
    ];

    private static readonly (Regex Pattern, string Text)[] FailureRules =
    [
        (Rx("already has a linked reprint request"), "Lệnh này đã có yêu cầu in lại; hãy xử lý yêu cầu đó trước."),
        (Rx("is not awaiting approval or has no immutable manifest metadata"), "Yêu cầu in lại này không ở trạng thái chờ duyệt hoặc thiếu bản kê."),
        (Rx("^The print-job event log needs repair before an operator action can be recorded"), "Nhật ký lệnh in cần được lưu trữ (sửa) trước khi ghi thao tác."),
        (Rx("was not found in the durable event log"), "Không tìm thấy lệnh này trong nhật ký lệnh in."),
        (Rx("is already terminal and cannot be changed"), "Lệnh này đã kết thúc nên không thay đổi được."),
    ];

    public static string State(PrintJobLifecycleState state) => state switch
    {
        PrintJobLifecycleState.Created => "Đã tạo",
        PrintJobLifecycleState.Preparing => "Đang chuẩn bị",
        PrintJobLifecycleState.PreflightPassed => "Đã kiểm tra trước in",
        PrintJobLifecycleState.Dispatching => "Đang gửi",
        PrintJobLifecycleState.SpoolAccepted => "Hàng đợi đã nhận",
        PrintJobLifecycleState.QueueObserved => "Đã xem hàng đợi",
        PrintJobLifecycleState.Unknown => "Chưa rõ kết quả",
        PrintJobLifecycleState.Completed => "Hoàn tất",
        PrintJobLifecycleState.Failed => "Thất bại",
        PrintJobLifecycleState.Cancelled => "Đã hủy",
        _ => state.ToString(),
    };

    public static string Action(PrintJobOperatorAction action) => action switch
    {
        PrintJobOperatorAction.None => string.Empty,
        PrintJobOperatorAction.Acknowledge => "Đã ghi nhận",
        PrintJobOperatorAction.Void => "Đã hủy",
        PrintJobOperatorAction.ReprintRequested => "Chờ duyệt in lại",
        PrintJobOperatorAction.ReprintApproved => "Đã duyệt in lại",
        _ => action.ToString(),
    };

    public static string RecoveryAction(PrintJobRecoveryAction action) => action switch
    {
        PrintJobRecoveryAction.ReconcileQueue => "Cần đối soát hàng đợi",
        PrintJobRecoveryAction.OperatorDecision => "Cần người vận hành quyết định",
        PrintJobRecoveryAction.RepairEventLog => "Cần lưu trữ nhật ký hỏng",
        _ => string.Empty,
    };

    public static string Queue(string? queueState)
    {
        if (string.IsNullOrWhiteSpace(queueState))
        {
            return string.Empty;
        }

        if (!Enum.TryParse<SpoolJobState>(queueState, ignoreCase: true, out var state))
        {
            return queueState;
        }

        var text = PrintStatusText.QueueState(state);
        return char.ToUpperInvariant(text[0]) + text[1..];
    }

    public static string Reason(string? reason)
    {
        var text = (reason ?? string.Empty).Trim();
        foreach (var (pattern, vietnamese) in ReasonRules)
        {
            if (pattern.IsMatch(text))
            {
                return vietnamese;
            }
        }

        return text;
    }

    public static string Reconcile(PrintJobReconciliationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.Outcome switch
        {
            PrintJobReconciliationOutcome.QueueObserved when result.QueueResult is { } queue
                => $"{PrintStatusText.Queue(queue)} ANLAbel không tự in lại.",
            PrintJobReconciliationOutcome.TimedOut or PrintJobReconciliationOutcome.OperatorDecisionRequired when result.QueueResult is { } queue
                => $"{PrintStatusText.Queue(queue)} Người vận hành cần quyết định.",
            PrintJobReconciliationOutcome.RepairRequired
                => "Nhật ký lệnh in bị hỏng; lưu trữ nhật ký hỏng trước khi đối soát.",
            _ => Reason(result.Summary),
        };
    }

    public static string ActionResult(PrintJobOperatorActionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.Action switch
        {
            PrintJobOperatorAction.Acknowledge => $"Đã ghi nhận lệnh {Short(result.JobId)}. Chưa xác minh tem đã ra; ANLAbel không tự in lại.",
            PrintJobOperatorAction.Void => $"Đã hủy lệnh {Short(result.JobId)} trong lịch sử. Không có lệnh hủy nào được gửi tới máy in.",
            PrintJobOperatorAction.ReprintRequested => $"Đã tạo yêu cầu in lại cho lệnh {Short(result.JobId)}. Chưa có gì được in; hãy duyệt yêu cầu rồi in từ Xem trước.",
            PrintJobOperatorAction.ReprintApproved => $"Đã duyệt yêu cầu in lại cho lệnh {Short(result.RelatedJobId)}. Mở Xem trước để kiểm tra rồi in.",
            _ => result.Summary,
        };
    }

    public static string Failure(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        foreach (var (pattern, vietnamese) in FailureRules)
        {
            if (pattern.IsMatch(exception.Message))
            {
                return vietnamese;
            }
        }

        return OperationFailureExplainer.StatusLine(exception);
    }

    /// <summary>The first characters of a job id, enough to find it in the list.</summary>
    public static string Short(string? jobId)
    {
        var id = (jobId ?? string.Empty).Split(':')[0];
        return id.Length > 8 ? id[..8] : id;
    }

    private static Regex Rx(string pattern) => new(pattern, RegexOptions.CultureInvariant | RegexOptions.Compiled);
}
