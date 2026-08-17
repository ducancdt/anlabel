using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using ANLAbel.Core.Models;

namespace ANLAbel.App.ViewModels;

/// <summary>CC-P7 read-only local-maintenance status. Mutation stays with its existing owners.</summary>
public sealed class LocalMaintenanceViewModel : INotifyPropertyChanged
{
    private readonly Func<IReadOnlyList<DataSource>> _sources;
    private readonly Func<string> _historyPath;
    private string _sourceStatus = "Not refreshed";
    private string _registryStatus = "Not refreshed";
    private string _preferencesStatus = "Not refreshed";
    private string _retentionStatus = "Retention preview unavailable: no approved retention, archive, backup, or recovery policy exists.";

    public LocalMaintenanceViewModel(Func<IReadOnlyList<DataSource>> sources, Func<string> historyPath)
    {
        _sources = sources;
        _historyPath = historyPath;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public string SourceStatus { get => _sourceStatus; private set => Set(ref _sourceStatus, value); }
    public string RegistryStatus { get => _registryStatus; private set => Set(ref _registryStatus, value); }
    public string PreferencesStatus { get => _preferencesStatus; private set => Set(ref _preferencesStatus, value); }
    public string RetentionStatus { get => _retentionStatus; private set => Set(ref _retentionStatus, value); }

    public void Refresh()
    {
        try
        {
            var sources = _sources();
            var missing = sources.Count(source => string.IsNullOrWhiteSpace(source.FilePath) || !File.Exists(source.FilePath));
            RegistryStatus = sources.Count == 0
                ? "Local registry is available; it currently has no shared data sources."
                : $"Local registry: {sources.Count} source(s), {missing} missing or unavailable file(s).";
            var history = _historyPath();
            SourceStatus = string.IsNullOrWhiteSpace(history)
                ? "History evidence location is unavailable."
                : $"History evidence is local: {Path.GetFileName(history)}.";
            var preferencesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ANLAbel", "designer-preferences.json");
            PreferencesStatus = File.Exists(preferencesPath)
                ? "Designer preferences are saved locally; change them through their existing designer controls."
                : "Designer preferences have no saved file yet; application defaults remain in effect.";
        }
        catch (Exception ex)
        {
            SourceStatus = $"Maintenance status read failed: {ex.Message}";
            RegistryStatus = "Data-source registry status is unavailable.";
            PreferencesStatus = "Designer-preferences status is unavailable.";
        }
    }

    private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        return true;
    }
}
