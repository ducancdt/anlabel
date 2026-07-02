using System.Windows;

namespace ANLAbel.LicenseGenerator;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        if (e.Args.Contains("--self-test", StringComparer.OrdinalIgnoreCase))
        {
            Environment.ExitCode = MasterLicenseSigner.SelfTest() ? 0 : 3;
            Shutdown(Environment.ExitCode);
            return;
        }

        MainWindow = new MainWindow();
        MainWindow.Show();
    }
}
