using ANLAbel.App.ViewModels;
using ANLAbel.App;
using ANLAbel.Printing.PrinterProfiles;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

internal static class PrintQueueConsoleRegression
{
    public static async Task Run()
    {
        await DistinguishesEnumerationFailureFromEmptyAsync();
        await RejectsStaleRefreshAsync();
        AssertConsoleExcludesCommandsAndLicensing();
        AssertWpfAutomationTree();
    }

    private static async Task DistinguishesEnumerationFailureFromEmptyAsync()
    {
        var saved = new SavedQueueEvidence("Saved", false, string.Empty, "Saved queue unavailable", DateTimeOffset.UtcNow);
        var failed = new PrintQueueConsoleViewModel(
            _ => Task.FromResult(new PrinterDiscoveryResult(Array.Empty<PrinterInfo>(), "access denied")),
            _ => Task.FromResult(saved));
        await failed.RefreshAsync();
        Require(failed.Rows.Any(row => row.Availability == "Enumeration failed"), "Enumeration failure must remain distinct from zero queues.");

        var empty = new PrintQueueConsoleViewModel(
            _ => Task.FromResult(new PrinterDiscoveryResult(Array.Empty<PrinterInfo>())),
            _ => Task.FromResult(saved));
        await empty.RefreshAsync();
        Require(empty.StatusText.Contains("No Local or Connections", StringComparison.Ordinal), "An empty discovery must be explicit without claiming an enumeration failure.");
    }

    private static async Task RejectsStaleRefreshAsync()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<PrinterDiscoveryResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        var vm = new PrintQueueConsoleViewModel(
            token => Interlocked.Increment(ref calls) == 1
                ? WaitFirstAsync(entered, release, token)
                : Task.FromResult(new PrinterDiscoveryResult([new PrinterInfo { Name = "New queue", DriverName = "Driver" }])),
            _ => Task.FromResult(new SavedQueueEvidence("", false, "", "", DateTimeOffset.UtcNow)));
        var old = vm.RefreshAsync();
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Require(await vm.RefreshAsync(), "Newest queue refresh must apply.");
        release.SetResult(new PrinterDiscoveryResult([new PrinterInfo { Name = "Old queue" }]));
        Require(!await old, "Late queue discovery must not overwrite a newer refresh.");
        Require(vm.Rows.Any(row => row.QueueName == "New queue"), "Latest queue rows must remain visible.");
    }

    private static async Task<PrinterDiscoveryResult> WaitFirstAsync(TaskCompletionSource entered, TaskCompletionSource<PrinterDiscoveryResult> release, CancellationToken token)
    {
        entered.SetResult();
        return await release.Task.WaitAsync(token);
    }

    private static void AssertConsoleExcludesCommandsAndLicensing()
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "ANLAbel.App", "PrintQueueConsoleWindow.xaml"));
        var xaml = File.ReadAllText(path);
        foreach (var id in new[] { "CC.P2.QueueConsole.Root", "CC.P2.QueueConsole.QueueTable", "CC.P2.QueueConsole.Commands", "CC.P2.QueueConsole.OpenPrintCenter" })
            Require(xaml.Contains(id, StringComparison.Ordinal), $"Queue console must expose '{id}'.");
        foreach (var excluded in new[] { "license", "activation", "entitlement", "Pause", "Resume", "Delete documents", "Reserve", "Unreserve" })
            Require(!xaml.Contains(excluded, StringComparison.OrdinalIgnoreCase), $"Queue console must exclude '{excluded}'.");
    }

    private static void AssertWpfAutomationTree()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var window = new PrintQueueConsoleWindow(new MainViewModel(), () => { }, () => { }, () => { });
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
                    {
                        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(node); index++) Walk(VisualTreeHelper.GetChild(node, index));
                    }
                    foreach (var child in LogicalTreeHelper.GetChildren(node).OfType<DependencyObject>()) Walk(child);
                }
                Walk(window);
                foreach (var id in new[] { "CC.P2.QueueConsole.Root", "CC.P2.QueueConsole.Refresh", "CC.P2.QueueConsole.Filters", "CC.P2.QueueConsole.QueueTable", "CC.P2.QueueConsole.Detail", "CC.P2.QueueConsole.Commands", "CC.P2.QueueConsole.OpenPrinterSetup", "CC.P2.QueueConsole.OpenPrintCenter", "CC.P2.QueueConsole.OpenHistory" })
                    Require(found.Contains(id), $"Queue console WPF visual tree must expose '{id}' at 1024 x 600.");
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
