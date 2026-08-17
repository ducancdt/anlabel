using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using ANLAbel.App;
using ANLAbel.App.ViewModels;
using ANLAbel.Core.Automation;
using ANLAbel.Data.Automation;

internal static class AutomationConsoleRegression
{
    public static Task Run()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".jsonl");
        var configurationPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        var lifecyclePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".jsonl");
        try
        {
            var ledger = new FileDropClaimLedger(path);
            var identity = FileDropClaimContract.CreateIdentity("test-trigger", "config", "source");
            ledger.TryRecordDetection(identity, out _, out _);
            var viewModel = new AutomationConsoleViewModel(
                () => ledger,
                () => new FileDropTriggerConfigurationStore(configurationPath),
                () => new FileDropLifecycleStore(lifecyclePath));
            viewModel.Refresh();
            Require(viewModel.LifecycleStatus.StartsWith("Stopped", StringComparison.Ordinal), "The evidence console must never imply a runner is armed.");
            Require(viewModel.EventStatus.Contains("1 event", StringComparison.Ordinal), "The evidence console must project durable ledger counts.");
            Require(viewModel.RecentEvents.Count == 1 && viewModel.RecentEvents[0].State == "Detected", "The evidence console must project redacted recent event state.");
            var lifecycle = new FileDropLifecycleStore(lifecyclePath);
            lifecycle.TryAppend("Running", "Previous session", out _, out _);
            viewModel.Refresh();
            Require(viewModel.LifecycleStatus.StartsWith("Stopped", StringComparison.Ordinal), "A persisted running state must never imply the watcher survived an application restart.");
            AssertWpfAutomationTree(viewModel);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
            if (File.Exists(configurationPath)) File.Delete(configurationPath);
            if (File.Exists(lifecyclePath)) File.Delete(lifecyclePath);
        }
        return Task.CompletedTask;
    }

    private static void AssertWpfAutomationTree(AutomationConsoleViewModel viewModel)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var window = new AutomationWindow(() => { }, () => { }, viewModel);
                window.Measure(new Size(1024, 600));
                window.Arrange(new Rect(0, 0, 1024, 600));
                window.UpdateLayout();
                var found = new HashSet<string>(StringComparer.Ordinal);
                var visited = new HashSet<DependencyObject>();
                void Walk(DependencyObject node)
                {
                    if (!visited.Add(node)) return;
                    if (node is UIElement element)
                    {
                        var id = System.Windows.Automation.AutomationProperties.GetAutomationId(element);
                        if (!string.IsNullOrWhiteSpace(id)) found.Add(id);
                    }
                    if (node is Visual or Visual3D)
                        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++) Walk(VisualTreeHelper.GetChild(node, i));
                    foreach (var child in LogicalTreeHelper.GetChildren(node).OfType<DependencyObject>()) Walk(child);
                }
                Walk(window);
                foreach (var id in new[] { "CC.P8.Automation.Root", "CC.P8.Automation.StatusFilter", "CC.P8.Automation.Configuration", "CC.P8.Automation.EventList", "CC.P8.Automation.EventDetail", "CC.P8.Automation.Start", "CC.P8.Automation.Stop", "CC.P8.Automation.Claim", "CC.P8.Automation.ValidateTemplate", "CC.P8.Automation.Configure", "CC.P8.Automation.OpenHistory", "CC.P8.Automation.OpenPrintCenter" })
                    Require(found.Contains(id), $"Automation console must expose {id} at 1024 x 600.");
                window.Close();
            }
            catch (Exception ex) { failure = ex; }
            finally { System.Windows.Threading.Dispatcher.CurrentDispatcher.InvokeShutdown(); }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (failure is not null) throw failure;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
