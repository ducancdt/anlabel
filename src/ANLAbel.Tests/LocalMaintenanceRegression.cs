using ANLAbel.App.ViewModels;
using ANLAbel.App;
using ANLAbel.Core.Models;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

internal static class LocalMaintenanceRegression
{
    public static Task Run()
    {
        var present = Path.GetTempFileName();
        try
        {
            var vm = new LocalMaintenanceViewModel(
                () => new[] { new DataSource { FilePath = present }, new DataSource { FilePath = present + ".missing" } },
                () => Path.Combine(Path.GetTempPath(), "anlabel-history.csv"));
            vm.Refresh();
            Require(vm.RegistryStatus.Contains("2 source", StringComparison.OrdinalIgnoreCase), "Registry summary must preserve the local source count.");
            Require(vm.RegistryStatus.Contains("1 missing", StringComparison.OrdinalIgnoreCase), "Unavailable source files must remain explicit.");
            Require(vm.SourceStatus.Contains("local", StringComparison.OrdinalIgnoreCase), "Evidence scope must remain local.");
            Require(vm.RetentionStatus.Contains("unavailable", StringComparison.OrdinalIgnoreCase), "Retention must fail closed without an approved policy.");
        }
        finally { File.Delete(present); }

        var xamlPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "ANLAbel.App", "LocalMaintenanceWindow.xaml"));
        var xaml = File.ReadAllText(xamlPath);
        foreach (var id in new[] { "CC.P7.Maintenance.Root", "CC.P7.Maintenance.DataSources", "CC.P7.Maintenance.Cleanup", "CC.P7.Maintenance.RetentionStatus" }) Require(xaml.Contains(id, StringComparison.Ordinal), $"Maintenance must expose '{id}'.");
        foreach (var forbidden in new[] { "License", "Activation", "Workflow", "Users", "Server", "Sync" }) Require(!xaml.Contains(forbidden, StringComparison.OrdinalIgnoreCase), $"Maintenance must not invent '{forbidden}'.");
        AssertWpfAutomationTree();
        return Task.CompletedTask;
    }

    private static void AssertWpfAutomationTree()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var main = new MainViewModel();
                var window = new LocalMaintenanceWindow(main, () => { }, () => { }, () => { }, () => { }, () => { });
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
                        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(node); index++) Walk(VisualTreeHelper.GetChild(node, index));
                    foreach (var child in LogicalTreeHelper.GetChildren(node).OfType<DependencyObject>()) Walk(child);
                }
                Walk(window);
                foreach (var id in new[] { "CC.P7.Maintenance.Root", "CC.P7.Maintenance.Refresh", "CC.P7.Maintenance.PreferencesStatus", "CC.P7.Maintenance.PrinterSetup", "CC.P7.Maintenance.DataSources", "CC.P7.Maintenance.Cleanup", "CC.P7.Maintenance.History", "CC.P7.Maintenance.Analytics", "CC.P7.Maintenance.PrintCenter", "CC.P7.Maintenance.RetentionStatus" })
                    Require(found.Contains(id), $"Maintenance visual tree must expose '{id}' at 1024 x 600.");
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
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
