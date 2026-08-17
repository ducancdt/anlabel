using System.ComponentModel;
using System.Runtime.CompilerServices;
using ANLAbel.Core.Printing;

namespace ANLAbel.App.ViewModels;

/// <summary>Read-only P6 counters. Job, event and CSV-row units remain separate.</summary>
public sealed class AnalyticsViewModel : INotifyPropertyChanged
{
    private readonly Func<CancellationToken, Task<HistorySnapshot>> _load;
    private bool _isRefreshing;
    private string _sourceHealth = "Not refreshed";
    private int _recordedLabelRows;
    private int _recordedJobs;
    private int _errorsOrUncertain;
    public AnalyticsViewModel(Func<CancellationToken, Task<HistorySnapshot>> load) => _load = load;
    public event PropertyChangedEventHandler? PropertyChanged;
    public bool IsRefreshing { get => _isRefreshing; private set { if (Set(ref _isRefreshing, value)) { Changed(nameof(CanRefresh)); Changed(nameof(RefreshButtonText)); } } }
    public bool CanRefresh => !IsRefreshing;
    public string RefreshButtonText => IsRefreshing ? "Refreshing…" : "Refresh";
    public string SourceHealth { get => _sourceHealth; private set => Set(ref _sourceHealth, value); }
    public int RecordedLabelRows { get => _recordedLabelRows; private set => Set(ref _recordedLabelRows, value); }
    public int RecordedJobs { get => _recordedJobs; private set => Set(ref _recordedJobs, value); }
    public int ErrorsOrUncertain { get => _errorsOrUncertain; private set => Set(ref _errorsOrUncertain, value); }
    public async Task RefreshAsync(CancellationToken token = default)
    {
        IsRefreshing = true;
        try
        {
            var snapshot = await _load(token).ConfigureAwait(true);
            RecordedLabelRows = snapshot.CsvEntries.Count;
            RecordedJobs = snapshot.State.LatestEvents.Select(item => item.JobId).Distinct(StringComparer.Ordinal).Count();
            ErrorsOrUncertain = snapshot.State.LatestEvents.Count(item => item.To is PrintJobLifecycleState.Failed or PrintJobLifecycleState.Unknown) + snapshot.Operations.Count(item => !item.Success) + snapshot.State.StoreDiagnostics.Count;
            var diagnostics = snapshot.State.StoreDiagnostics.Count + snapshot.OperationDiagnostics.Count + snapshot.CsvDiagnostics.Count;
            SourceHealth = diagnostics == 0 ? "Local evidence refreshed. Units remain separate by source." : $"Partial local evidence: {diagnostics} diagnostic(s); totals are not complete coverage.";
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch (Exception ex) { SourceHealth = $"Analytics source read failed: {ex.Message}"; }
        finally { IsRefreshing = false; }
    }
    private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null) { if (EqualityComparer<T>.Default.Equals(field, value)) return false; field = value; Changed(name); return true; }
    private void Changed([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
