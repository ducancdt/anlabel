using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using ANLAbel.Data.Automation;

namespace ANLAbel.App.ViewModels;

/// <summary>
/// P8 M0 local evidence projection. It intentionally owns no watcher or print path.
/// </summary>
public sealed class AutomationConsoleViewModel : INotifyPropertyChanged
{
    private readonly Func<FileDropClaimLedger> _ledgerFactory;
    private readonly Func<FileDropTriggerConfigurationStore> _configurationStoreFactory;
    private readonly Func<FileDropLifecycleStore> _lifecycleStoreFactory;
    private string _lifecycleStatus = "Stopped — no file-drop runner is installed or armed.";
    private string _configurationStatus = "No local trigger configuration is active. This console cannot consume files or submit print jobs.";
    private string _eventStatus = "No durable automation events have been recorded.";
    private string _eventDetail = "Automation events remain local, fingerprint-only evidence; raw payloads and paths are not displayed.";
    public ObservableCollection<AutomationEvidenceRow> RecentEvents { get; } = [];

    public AutomationConsoleViewModel(Func<FileDropClaimLedger>? ledgerFactory = null, Func<FileDropTriggerConfigurationStore>? configurationStoreFactory = null, Func<FileDropLifecycleStore>? lifecycleStoreFactory = null)
    {
        _ledgerFactory = ledgerFactory ?? new Func<FileDropClaimLedger>(() => new FileDropClaimLedger(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ANLAbel",
            "automation-claims.jsonl")));
        _configurationStoreFactory = configurationStoreFactory ?? new Func<FileDropTriggerConfigurationStore>(() => new FileDropTriggerConfigurationStore(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ANLAbel",
            "automation-trigger.json")));
        _lifecycleStoreFactory = lifecycleStoreFactory ?? new Func<FileDropLifecycleStore>(() => new FileDropLifecycleStore(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ANLAbel",
            "automation-lifecycle.jsonl")));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public string LifecycleStatus { get => _lifecycleStatus; private set => Set(ref _lifecycleStatus, value); }
    public string ConfigurationStatus { get => _configurationStatus; private set => Set(ref _configurationStatus, value); }
    public string EventStatus { get => _eventStatus; private set => Set(ref _eventStatus, value); }
    public string EventDetail { get => _eventDetail; private set => Set(ref _eventDetail, value); }

    public void Refresh()
    {
        try
        {
            var events = _ledgerFactory().ReadValid(out var diagnostics);
            var lifecycle = _lifecycleStoreFactory().ReadValid(out var lifecycleDiagnostics);
            LifecycleStatus = lifecycleDiagnostics.Count != 0
                ? "Error — local automation lifecycle journal requires repair."
                : lifecycle.LastOrDefault()?.State == "Running"
                    ? "Stopped — previous detect-only watcher ended with the application; start explicitly to arm it again."
                    : lifecycle.LastOrDefault() is { } latest
                        ? $"{latest.State} — {latest.Detail}"
                        : "Stopped — no file-drop runner is installed or armed.";
            var configuration = _configurationStoreFactory().Read(out var configurationDiagnostic);
            ConfigurationStatus = configurationDiagnostic is not null
                ? $"Local trigger configuration requires repair: {configurationDiagnostic}"
                : configuration is null
                    ? "No local trigger configuration is active. This console cannot consume files or submit print jobs."
                    : DescribeConfiguration(configuration);
            if (diagnostics.Count != 0)
            {
                RecentEvents.Clear();
                EventStatus = "Automation audit requires repair before any future claim can proceed.";
                EventDetail = diagnostics[0];
                return;
            }

            EventStatus = events.Count == 0
                ? "No durable automation events have been recorded."
                : $"Local claim ledger: {events.Count} event transition(s), {events.Select(item => item.Identity.EventId).Distinct().Count()} source identity/identities.";
            EventDetail = events.Count == 0
                ? "Automation events remain local, fingerprint-only evidence; raw payloads and paths are not displayed."
                : $"Latest durable outcome: {events[^1].To}. No event can create a print job from this console.";
            RecentEvents.Clear();
            foreach (var item in events.TakeLast(20).Reverse())
                RecentEvents.Add(new AutomationEvidenceRow(item.TimestampUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"), item.To.ToString(), item.Identity.EventId[..12], item.Reason));
        }
        catch (Exception ex)
        {
            LifecycleStatus = "Error — local automation evidence could not be read.";
            EventStatus = "Automation evidence is unavailable.";
            EventDetail = ex.Message;
        }
    }

    public void ReportLifecycle(string status) => LifecycleStatus = status;

    private static string DescribeConfiguration(FileDropTriggerConfigurationSnapshot snapshot)
    {
        var configuration = snapshot.Configuration;
        var readiness = ANLAbel.Core.Automation.FileDropTriggerConfigurationContract.TryValidateDispatchBinding(configuration, out var error)
            ? "Dispatch binding is complete but no dispatch action is installed."
            : $"Not dispatch-ready: {error}";
        return $"{configuration.Name} ({configuration.Pattern}) is saved and {(configuration.Enabled ? "enabled" : "disabled")}. Runner remains unarmed; fingerprint {snapshot.ConfigurationFingerprint[..12]}. {readiness}";
    }

    private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        return true;
    }
}

public sealed record AutomationEvidenceRow(string LocalTime, string State, string EventFingerprint, string Reason);
