using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using ANLAbel.Core.Automation;
using ANLAbel.Data.Automation;
using ANLAbel.Core.Workflow;

namespace ANLAbel.App.ViewModels;

public sealed class AutomationConfigurationViewModel : INotifyPropertyChanged
{
    private readonly FileDropTriggerConfigurationStore _store;
    private string _triggerId = "local-file-drop";
    private string _name = "Local file drop";
    private string _watchRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ANLAbel", "Incoming");
    private string _pattern = "*.csv";
    private bool _recursive;
    private bool _enabled;
    private string _targetTemplatePath = "";
    private string _queueName = "";
    private DocumentWorkflowPrintPolicyMode _printPolicyMode;
    private string _status = "Save settings to create a local trigger definition. Saving does not start a watcher.";

    public AutomationConfigurationViewModel(FileDropTriggerConfigurationStore store)
    {
        _store = store;
        Load();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public string TriggerId { get => _triggerId; set => Set(ref _triggerId, value); }
    public string Name { get => _name; set => Set(ref _name, value); }
    public string WatchRoot { get => _watchRoot; set => Set(ref _watchRoot, value); }
    public string Pattern { get => _pattern; set => Set(ref _pattern, value); }
    public bool Recursive { get => _recursive; set => Set(ref _recursive, value); }
    public bool Enabled { get => _enabled; set => Set(ref _enabled, value); }
    public string TargetTemplatePath { get => _targetTemplatePath; set => Set(ref _targetTemplatePath, value); }
    public string QueueName { get => _queueName; set => Set(ref _queueName, value); }
    public DocumentWorkflowPrintPolicyMode PrintPolicyMode { get => _printPolicyMode; set => Set(ref _printPolicyMode, value); }
    public IReadOnlyList<DocumentWorkflowPrintPolicyMode> PrintPolicyModes { get; } = Enum.GetValues<DocumentWorkflowPrintPolicyMode>();
    public string Status { get => _status; private set => Set(ref _status, value); }

    public bool Save()
    {
        var configuration = new FileDropTriggerConfiguration(TriggerId, Name, WatchRoot, Pattern, Recursive, Enabled, TargetTemplatePath, QueueName, PrintPolicyMode);
        if (!FileDropTriggerConfigurationContract.TryValidate(configuration, out var error))
        {
            Status = error;
            return false;
        }
        _store.Save(configuration);
        Status = $"Local settings saved. Runner remains unarmed; configuration fingerprint {configuration.ConfigurationFingerprint[..12]}.";
        return true;
    }

    private void Load()
    {
        var snapshot = _store.Read(out var diagnostic);
        if (diagnostic is not null) { Status = $"Saved configuration requires repair: {diagnostic}"; return; }
        if (snapshot is null) return;
        TriggerId = snapshot.Configuration.TriggerId;
        Name = snapshot.Configuration.Name;
        WatchRoot = snapshot.Configuration.WatchRoot;
        Pattern = snapshot.Configuration.Pattern;
        Recursive = snapshot.Configuration.Recursive;
        Enabled = snapshot.Configuration.Enabled;
        TargetTemplatePath = snapshot.Configuration.TargetTemplatePath;
        QueueName = snapshot.Configuration.QueueName;
        PrintPolicyMode = snapshot.Configuration.PrintPolicyMode;
        Status = "Saved local settings loaded. Runner remains unarmed.";
    }

    private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        return true;
    }
}
