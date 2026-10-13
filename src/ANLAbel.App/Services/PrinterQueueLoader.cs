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
    internal static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(20);
    private static Task<PrinterDiscoveryResult>? _discovery;
    private static bool _waiting;

    /// <returns>The queues, or null after the operator was told why none can be shown.</returns>
    public static async Task<IReadOnlyList<PrinterInfo>?> LoadForDialogAsync(Window owner, Func<PrinterDiscoveryResult> discover)
    {
        ArgumentNullException.ThrowIfNull(discover);
        if (_waiting)
        {
            // A second click while the list loads opens nothing extra.
            return null;
        }

        PrinterDiscoveryResult? result;
        var previousCursor = Mouse.OverrideCursor;
        Mouse.OverrideCursor = Cursors.Wait;
        _waiting = true;
        try
        {
            result = await DiscoverAsync(discover, DefaultTimeout).ConfigureAwait(true);
        }
        finally
        {
            _waiting = false;
            Mouse.OverrideCursor = previousCursor;
        }

        if (result is null)
        {
            // A hung spooler must not leave a wait cursor and no answer
            // (audit 2026-10-13).
            OperatorDialog.Ask(
                owner,
                "Windows chưa trả lời",
                [
                    $"Đã chờ {DefaultTimeout.TotalSeconds:0} giây mà Windows vẫn chưa liệt kê xong máy in. Thường là do dịch vụ Print Spooler bị treo hoặc một máy in mạng không phản hồi.",
                    "Khởi động lại dịch vụ Print Spooler hoặc kiểm tra máy in mạng, rồi mở lại Cài đặt máy in."
                ],
                null,
                OperatorDialogTone.Error,
                new OperatorDialogChoice("ok", "Đóng", IsDefault: true, IsCancel: true));
            return null;
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

    /// <summary>
    /// Enumerates on an STA thread and stops waiting after <paramref name="timeout"/>.
    /// An enumeration still running from an earlier timeout is awaited again
    /// instead of stacking another thread on a hung spooler.
    /// </summary>
    /// <returns>The result, or null when Windows did not answer in time.</returns>
    internal static async Task<PrinterDiscoveryResult?> DiscoverAsync(Func<PrinterDiscoveryResult> discover, TimeSpan timeout)
    {
        var discovery = _discovery is { IsCompleted: false } running ? running : RunOnStaAsync(discover);
        _discovery = discovery;
        var finished = await Task.WhenAny(discovery, Task.Delay(timeout)).ConfigureAwait(true);
        return finished == discovery ? await discovery.ConfigureAwait(true) : null;
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
