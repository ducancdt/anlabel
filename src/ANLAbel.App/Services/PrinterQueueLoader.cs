using System.Threading;
using System.Windows;
using System.Windows.Input;
using ANLAbel.Printing.PrinterProfiles;

namespace ANLAbel.App.Services;

/// <summary>
/// Lists Windows printer queues for a dialog without freezing the window
/// (plan 13 H17). Enumeration runs on a dedicated STA thread because network
/// queues and a stopped spooler can take seconds. A spooler failure is reported
/// as such; it is never shown as "no printers".
/// </summary>
public static class PrinterQueueLoader
{
    /// <returns>The queues, or null after the operator was told why none can be shown.</returns>
    public static async Task<IReadOnlyList<PrinterInfo>?> LoadForDialogAsync(Window owner, Func<PrinterDiscoveryResult> discover)
    {
        ArgumentNullException.ThrowIfNull(discover);
        PrinterDiscoveryResult result;
        var previousCursor = Mouse.OverrideCursor;
        Mouse.OverrideCursor = Cursors.Wait;
        try
        {
            result = await RunOnStaAsync(discover).ConfigureAwait(true);
        }
        finally
        {
            Mouse.OverrideCursor = previousCursor;
        }

        if (!result.IsSuccess)
        {
            OperatorDialog.Ask(
                owner,
                "Không liệt kê được máy in",
                [
                    "Windows không trả về danh sách máy in. Thường là do dịch vụ Print Spooler đang dừng hoặc bị treo.",
                    "Mở Services, khởi động lại dịch vụ Print Spooler rồi mở lại Cài đặt máy in."
                ],
                result.ErrorMessage,
                OperatorDialogTone.Error,
                new OperatorDialogChoice("ok", "Đóng", IsDefault: true, IsCancel: true));
            return null;
        }

        if (result.Printers.Count == 0)
        {
            OperatorDialog.Ask(
                owner,
                "Chưa có máy in",
                [
                    "Windows chưa có hàng đợi máy in nào.",
                    "Hãy cài driver máy in tem (hoặc kết nối máy in mạng) rồi mở lại Cài đặt máy in."
                ],
                null,
                OperatorDialogTone.Warning,
                new OperatorDialogChoice("ok", "Đóng", IsDefault: true, IsCancel: true));
            return null;
        }

        return result.Printers;
    }

    private static Task<T> RunOnStaAsync<T>(Func<T> work)
    {
        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                completion.SetResult(work());
            }
            catch (Exception ex)
            {
                completion.SetException(ex);
            }
        })
        {
            IsBackground = true,
            Name = "ANLAbel printer discovery"
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return completion.Task;
    }
}
