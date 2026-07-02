using System.Windows;
#if TRIAL_BUILD
using System.Windows.Threading;
using ANLAbel.App.Services;
#endif

namespace ANLAbel.App;

public partial class App : Application
{
#if TRIAL_BUILD
    private readonly TrialLicenseService _trialService = new();
    private DispatcherTimer? _trialTimer;
#endif

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
#if TRIAL_BUILD
        var result = _trialService.Check();
        if (!result.IsAllowed)
        {
            if (result.Status == TrialCheckStatus.StorageError || !ShowActivationDialog(null))
            {
                if (result.Status == TrialCheckStatus.StorageError) ShowBlockedMessage(result);
                Shutdown(2);
                return;
            }
            result = _trialService.Check();
        }
#endif

        var window = new MainWindow();
        MainWindow = window;
#if TRIAL_BUILD
        ApplyLicenseDisplay(window, result);
        window.ActivationButton.Visibility = Visibility.Visible;
        window.ActivationButton.Click += (_, _) => ShowActivationDialog(window);
#endif
        ShutdownMode = ShutdownMode.OnMainWindowClose;
        window.Show();

#if TRIAL_BUILD
        if (result.IsFirstRun)
        {
            MessageBox.Show(
                "Bản dùng thử ANLAbel đã bắt đầu và có hiệu lực trong 7 ngày. Bạn có thể kích hoạt bất cứ lúc nào bằng nút Kích hoạt trên thanh tiêu đề.",
                "ANLAbel — Bản dùng thử 7 ngày", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        _trialTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(5) };
        _trialTimer.Tick += (_, _) => RecheckLicense();
        _trialTimer.Start();
#endif
    }

#if TRIAL_BUILD
    private bool ShowActivationDialog(Window? owner)
    {
        var dialog = new ActivationWindow(_trialService);
        if (owner is not null) dialog.Owner = owner;
        if (dialog.ShowDialog() != true) return false;
        if (MainWindow is MainWindow main) ApplyLicenseDisplay(main, _trialService.Check());
        return true;
    }

    private void RecheckLicense()
    {
        var result = _trialService.Check();
        if (result.IsAllowed)
        {
            if (MainWindow is MainWindow main) ApplyLicenseDisplay(main, result);
            return;
        }

        _trialTimer?.Stop();
        if (ShowActivationDialog(MainWindow))
        {
            _trialTimer?.Start();
            return;
        }
        Shutdown(2);
    }

    private static void ApplyLicenseDisplay(MainWindow window, TrialCheckResult result)
    {
        if (result.IsActivated)
        {
            window.Title = "ANLAbel - Label Designer v0.062 — Đã kích hoạt";
            window.BuildChannelText.Text = "LICENSED · v0.062";
        }
        else
        {
            var days = Math.Max(1, (int)Math.Ceiling(result.Remaining.TotalDays));
            window.Title = $"ANLAbel - Label Designer v0.062 — Dùng thử còn {days} ngày";
            window.BuildChannelText.Text = "TRIAL 7 NGÀY · v0.062";
        }
    }

    private static void ShowBlockedMessage(TrialCheckResult result)
    {
        var message = result.ErrorMessage ?? "ANLAbel không thể xác minh trạng thái bản quyền an toàn.";
        MessageBox.Show(message, "ANLAbel — Bản quyền", MessageBoxButton.OK, MessageBoxImage.Stop);
    }
#endif
}
