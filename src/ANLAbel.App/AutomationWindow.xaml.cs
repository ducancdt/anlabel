using System.Windows;
using System.IO;
using ANLAbel.App.ViewModels;
using ANLAbel.Data.Automation;

namespace ANLAbel.App;

public partial class AutomationWindow : Window
{
    private readonly AutomationConsoleViewModel _viewModel;
    private readonly Action _history;
    private readonly Action _printCenter;
    private readonly Func<bool> _configure;
    private FileDropDetectionService? _runner;
    private readonly FileDropLifecycleStore _lifecycleStore;

    public AutomationWindow(Action history, Action printCenter, AutomationConsoleViewModel? viewModel = null, Func<bool>? configure = null)
    {
        InitializeComponent();
        _history = history;
        _printCenter = printCenter;
        _configure = configure ?? (() => new AutomationConfigurationWindow { Owner = this }.ShowDialog() == true);
        _lifecycleStore = new FileDropLifecycleStore(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ANLAbel", "automation-lifecycle.jsonl"));
        _viewModel = viewModel ?? new AutomationConsoleViewModel();
        DataContext = _viewModel;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e) => _viewModel.Refresh();
    private void Refresh_Click(object sender, RoutedEventArgs e) => _viewModel.Refresh();
    private void History_Click(object sender, RoutedEventArgs e) { _history(); Activate(); }
    private void PrintCenter_Click(object sender, RoutedEventArgs e) { _printCenter(); Activate(); }
    private void Configure_Click(object sender, RoutedEventArgs e) { if (_configure()) _viewModel.Refresh(); }
    private void Start_Click(object sender, RoutedEventArgs e)
    {
        if (_runner is { IsRunning: true }) { ReportLifecycle("Running", "Detect-only watcher is active; it cannot claim or dispatch."); return; }
        var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ANLAbel");
        var configuration = new FileDropTriggerConfigurationStore(Path.Combine(root, "automation-trigger.json")).Read(out var diagnostic);
        if (configuration is null) { ReportLifecycle(diagnostic is null ? "Stopped" : "Error", diagnostic is null ? "Configure a local trigger before starting." : $"Configuration requires repair: {diagnostic}"); return; }
        if (!TryRecordLifecycle("Starting", "Validated local configuration; starting detect-only watcher.")) return;
        _runner?.Dispose();
        _runner = new FileDropDetectionService(configuration.Configuration, new FileDropClaimLedger(Path.Combine(root, "automation-claims.jsonl")), message => Dispatcher.BeginInvoke(() => ReportLifecycle("Error", message)));
        if (_runner.TryStart(out var error)) ReportLifecycle("Running", "Detect-only watcher is active; it cannot claim or dispatch.");
        else { _runner.Dispose(); _runner = null; ReportLifecycle("Error", $"Watcher was not started: {error}"); }
    }
    private void Stop_Click(object sender, RoutedEventArgs e) { _runner?.Dispose(); _runner = null; ReportLifecycle("Stopped", "Detect-only watcher is not armed."); }
    private void Claim_Click(object sender, RoutedEventArgs e)
    {
        var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ANLAbel");
        var result = new FileDropClaimReviewService(new FileDropClaimLedger(Path.Combine(root, "automation-claims.jsonl"))).ClaimDetected(out var message);
        _viewModel.Refresh();
        ReportLifecycle(result > 0 ? "Claimed" : "Stopped", message);
    }
    private async void ValidateTemplate_Click(object sender, RoutedEventArgs e)
    {
        var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ANLAbel");
        var configuration = new FileDropTriggerConfigurationStore(Path.Combine(root, "automation-trigger.json")).Read(out var diagnostic);
        if (configuration is null) { _viewModel.ReportLifecycle(diagnostic is null ? "Template policy cannot be validated: configure a target template first." : $"Template policy configuration requires repair: {diagnostic}"); return; }
        var result = await new AutomationTemplateBindingValidator().ValidateAsync(configuration.Configuration);
        _viewModel.ReportLifecycle(result.Allowed
            ? $"Template binding valid: {result.Diagnostic} No manifest, queue or print was created."
            : $"Template binding blocked: {result.Diagnostic}");
    }
    private void Close_Click(object sender, RoutedEventArgs e) { _runner?.Dispose(); _runner = null; TryRecordLifecycle("Stopped", "Detect-only watcher stopped because the console closed."); Close(); }
    private bool TryRecordLifecycle(string state, string detail)
    {
        if (_lifecycleStore.TryAppend(state, detail, out _, out var error)) { _viewModel.ReportLifecycle($"{state} — {detail}"); return true; }
        _viewModel.ReportLifecycle($"Error — lifecycle journal requires repair: {error}");
        return false;
    }
    private void ReportLifecycle(string state, string detail) => TryRecordLifecycle(state, detail);
}
