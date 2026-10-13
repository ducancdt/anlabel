using System.Threading;
using System.Windows;
using System.Windows.Threading;
using ANLAbel.App.Services;
using ANLAbel.App.ViewModels;

namespace ANLAbel.App;

public partial class App : Application
{
    private int _handlingUnexpectedError;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Plan 13 F03: an unexpected error must not silently close the app and
        // lose the open design, and a failed command must not vanish into Debug
        // output. Every path logs, protects unsaved work and says what happened.
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        AsyncRelayCommand.DefaultErrorHandler = ReportFailedOperation;

        // Plan 13 F06: the installer associates .anlabel files with ANLAbel.
        var window = new MainWindow { StartupFilePath = ANLAbel.App.MainWindow.FindDocumentArgument(e.Args) };
        MainWindow = window;
        ShutdownMode = ShutdownMode.OnMainWindowClose;
        window.Show();
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        var logPath = CrashReporter.WriteCrashLog(e.Exception, "Lỗi trên luồng giao diện");
        if (Interlocked.Exchange(ref _handlingUnexpectedError, 1) == 1)
        {
            // A dialog for an earlier failure is already open; this one is logged.
            return;
        }

        try
        {
            var window = MainWindow as MainWindow;
            var recovered = window?.TryWriteRecoveryNow() ?? false;
            var choice = OperatorDialog.Ask(
                window,
                "ANLAbel gặp lỗi không mong muốn",
                [
                    recovered
                        ? $"Thao tác vừa rồi không hoàn tất. ANLAbel đã lưu bản phục hồi của mẫu “{window!.CurrentDocumentDisplayName}”."
                        : "Thao tác vừa rồi không hoàn tất.",
                    "ANLAbel không tự gửi lại lệnh in nào."
                ],
                logPath is null ? null : $"Chi tiết kỹ thuật: {logPath}",
                OperatorDialogTone.Error,
                new OperatorDialogChoice("continue", "Tiếp tục làm việc", IsDefault: true, IsCancel: true),
                new OperatorDialogChoice("close", "Đóng ANLAbel"));
            if (choice == "close")
            {
                if (window is not null)
                {
                    window.SuppressClosePrompt = true;
                }

                Shutdown(1);
            }
        }
        finally
        {
            Interlocked.Exchange(ref _handlingUnexpectedError, 0);
        }
    }

    private void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception
            ?? new InvalidOperationException(Convert.ToString(e.ExceptionObject));
        var logPath = CrashReporter.WriteCrashLog(exception, "Lỗi ngoài luồng giao diện");
        if (!e.IsTerminating)
        {
            return;
        }

        string[] lines =
        [
            "ANLAbel gặp lỗi nghiêm trọng ngoài luồng giao diện và phải đóng.",
            "ANLAbel không tự gửi lại lệnh in nào."
        ];
        var detail = logPath is null ? null : $"Chi tiết kỹ thuật: {logPath}";
        try
        {
            Dispatcher.Invoke(
                () =>
                {
                    var window = MainWindow as MainWindow;
                    var recovered = window?.TryWriteRecoveryNow() ?? false;
                    if (window is not null)
                    {
                        window.SuppressClosePrompt = true;
                    }

                    OperatorDialog.Ask(
                        window,
                        "ANLAbel gặp lỗi không mong muốn",
                        recovered ? [.. lines, "Bản phục hồi của mẫu đang mở đã được lưu và sẽ được đề nghị khi mở lại ANLAbel."] : lines,
                        detail,
                        OperatorDialogTone.Error,
                        new OperatorDialogChoice("close", "Đóng ANLAbel", IsDefault: true, IsCancel: true));
                },
                DispatcherPriority.Send,
                CancellationToken.None,
                TimeSpan.FromSeconds(30));
        }
        catch (Exception)
        {
            // The UI thread is unavailable: fall back to a plain system message.
            MessageBox.Show(
                string.Join(Environment.NewLine, lines) + (detail is null ? string.Empty : Environment.NewLine + detail),
                "ANLAbel gặp lỗi không mong muốn",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        CrashReporter.AppendErrorLog(e.Exception, "Tác vụ nền lỗi mà không có nơi xử lý");
        e.SetObserved();
    }

    private void ReportFailedOperation(Exception exception)
    {
        if (exception is OperationCanceledException)
        {
            return;
        }

        CrashReporter.AppendErrorLog(exception, "Lệnh bất đồng bộ thất bại");
        var explanation = OperationFailureExplainer.Explain(exception);
        OperatorDialog.Ask(
            MainWindow,
            OperationFailureExplainer.IsDataFailure(exception) ? "Chưa đọc được dữ liệu" : "Thao tác chưa hoàn tất",
            [explanation.Problem, explanation.NextAction],
            $"Chi tiết kỹ thuật: {explanation.Technical} (đã ghi vào logs\\errors.log)",
            OperatorDialogTone.Error,
            new OperatorDialogChoice("ok", "Đóng", IsDefault: true, IsCancel: true));
    }
}
