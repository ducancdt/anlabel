using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ANLAbel.Core.Printing;
using ANLAbel.Data.PrintLogs;

namespace ANLAbel.App.ViewModels;

public sealed record HistorySnapshot(
    PrintJobRecoverySnapshot State,
    IReadOnlyList<PrintOperationLogEntry> Operations,
    IReadOnlyList<string> OperationDiagnostics,
    IReadOnlyList<PrintLogSummary> CsvEntries,
    IReadOnlyList<string> CsvDiagnostics);

public sealed class HistoryActivityRow
{
    public required string RecordId { get; init; }
    public required string RecordType { get; init; }
    public string TimestampText { get; init; } = "Unknown";
    public string TemplateOrQueue { get; init; } = "Unknown";
    public string Lifecycle { get; init; } = "Unknown";
    public string Evidence { get; init; } = "Unknown";
    public string Source { get; init; } = "Unknown";
    public string Detail { get; init; } = "No detail available.";
}

/// <summary>Read-only P5 history projection with explicit source precedence and provenance.</summary>
public sealed class PrintHistoryViewModel : INotifyPropertyChanged
{
    private readonly Func<CancellationToken, Task<HistorySnapshot>> _load;
    private int _epoch;
    private bool _isRefreshing;
    private string _statusText = "Ready to read local print evidence.";
    private string _searchText = string.Empty;
    private string _detailText = "Select an activity record to inspect provenance.";
    private readonly List<HistoryActivityRow> _all = [];
    private HistoryActivityRow? _selected;

    public PrintHistoryViewModel(Func<CancellationToken, Task<HistorySnapshot>> load) => _load = load ?? throw new ArgumentNullException(nameof(load));
    public event PropertyChangedEventHandler? PropertyChanged;
    public ObservableCollection<HistoryActivityRow> Rows { get; } = [];
    public bool IsRefreshing { get => _isRefreshing; private set { if (Set(ref _isRefreshing, value)) { OnChanged(nameof(CanRefresh)); OnChanged(nameof(RefreshButtonText)); } } }
    public bool CanRefresh => !IsRefreshing;
    public string RefreshButtonText => IsRefreshing ? "Refreshing…" : "Refresh";
    public string StatusText { get => _statusText; private set => Set(ref _statusText, value); }
    public string SearchText { get => _searchText; set { if (Set(ref _searchText, value)) ApplyFilter(); } }
    public string DetailText { get => _detailText; private set => Set(ref _detailText, value); }
    public HistoryActivityRow? SelectedRow { get => _selected; set { if (Set(ref _selected, value)) DetailText = value?.Detail ?? "Select an activity record to inspect provenance."; } }

    public async Task<bool> RefreshAsync(CancellationToken token = default)
    {
        var epoch = Interlocked.Increment(ref _epoch); IsRefreshing = true; StatusText = "Reading durable state, operation trace and redacted CSV summaries…";
        try
        {
            HistorySnapshot snapshot;
            try { snapshot = await _load(token).ConfigureAwait(true); }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
            catch (Exception ex) { if (epoch == Volatile.Read(ref _epoch)) StatusText = $"History refresh failed: {ex.Message}"; return false; }
            if (epoch != Volatile.Read(ref _epoch)) return false;
            BuildRows(snapshot); ApplyFilter();
            StatusText = BuildStatus(snapshot); return true;
        }
        finally { if (epoch == Volatile.Read(ref _epoch)) IsRefreshing = false; }
    }

    private void BuildRows(HistorySnapshot snapshot)
    {
        var selected = SelectedRow?.RecordId; _all.Clear();
        var operations = snapshot.Operations.Where(item => !string.IsNullOrWhiteSpace(item.JobId)).GroupBy(item => item.JobId, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.OrderByDescending(item => item.SpoolStatusObservedAtUtc ?? new DateTimeOffset(item.TimestampLocal)).First(), StringComparer.Ordinal);
        foreach (var state in snapshot.State.LatestEvents)
        {
            operations.TryGetValue(state.JobId, out var operation);
            _all.Add(FromState(state, operation));
        }
        foreach (var operation in snapshot.Operations.Where(item => !string.IsNullOrWhiteSpace(item.JobId) && !snapshot.State.LatestEvents.Any(state => state.JobId == item.JobId))) _all.Add(FromOperation(operation));
        foreach (var csv in snapshot.CsvEntries) _all.Add(new HistoryActivityRow { RecordId = $"csv:{csv.RecordNumber}", RecordType = "CsvLabelRecord", TimestampText = csv.PrintedAtLocal?.ToString("yyyy-MM-dd HH:mm:ss") + " local" ?? "Unknown local time", TemplateOrQueue = Join(csv.TemplateName, csv.PrinterName), Lifecycle = "Not a job lifecycle", Evidence = "Label-detail summary only", Source = "CSV", Detail = $"CSV label record {csv.RecordNumber}; quantity {csv.Quantity}; no JobId was fabricated." });
        SelectedRow = _all.FirstOrDefault(row => row.RecordId == selected);
    }
    private static HistoryActivityRow FromState(PrintJobStateEvent state, PrintOperationLogEntry? operation) => new() { RecordId = state.JobId, RecordType = "Job", TimestampText = state.TimestampUtc.ToString("yyyy-MM-dd HH:mm:ss 'UTC'"), TemplateOrQueue = Join(operation?.TemplateName, state.PrinterName), Lifecycle = state.To.ToString(), Evidence = state.PhysicalOutputVerified ? "Verifier evidence recorded" : string.IsNullOrWhiteSpace(state.QueueState) ? "Durable lifecycle event" : $"Job-scoped spool: {state.QueueState}; physical output unverified", Source = operation is null ? "State store" : "State store + operation trace", Detail = $"State sequence {state.Sequence}; action {state.OperatorAction}; related job {Blank(state.RelatedJobId)}; manifest {Blank(state.ManifestFingerprint)}; reason {Blank(state.Reason)}." };
    private static HistoryActivityRow FromOperation(PrintOperationLogEntry operation) => new() { RecordId = operation.JobId, RecordType = "Job trace", TimestampText = (operation.SpoolStatusObservedAtUtc?.ToString("yyyy-MM-dd HH:mm:ss 'UTC'") ?? operation.TimestampLocal.ToString("yyyy-MM-dd HH:mm:ss 'local'")), TemplateOrQueue = Join(operation.TemplateName, operation.PrinterName), Lifecycle = "Unknown (no durable state event)", Evidence = $"Operation outcome: {Blank(operation.Outcome)}; does not replace lifecycle", Source = "Operation trace", Detail = $"Spool {operation.SpoolJobId?.ToString() ?? "none"}: {Blank(operation.SpoolState)}. Physical output unverified. {Blank(operation.OutcomeEvidence)}" };
    private void ApplyFilter() { var keep = SelectedRow?.RecordId; var query = SearchText.Trim(); Rows.Clear(); foreach (var row in _all.Where(row => string.IsNullOrWhiteSpace(query) || row.RecordId.Contains(query, StringComparison.OrdinalIgnoreCase) || row.TemplateOrQueue.Contains(query, StringComparison.OrdinalIgnoreCase) || row.Lifecycle.Contains(query, StringComparison.OrdinalIgnoreCase))) Rows.Add(row); SelectedRow = Rows.FirstOrDefault(row => row.RecordId == keep) ?? Rows.FirstOrDefault(); }
    private static string BuildStatus(HistorySnapshot snapshot) { var diagnostics = snapshot.State.StoreDiagnostics.Count + snapshot.OperationDiagnostics.Count + snapshot.CsvDiagnostics.Count; return diagnostics > 0 ? $"History loaded with {diagnostics} source diagnostic(s); valid evidence remains visible." : $"History loaded: {snapshot.State.LatestEvents.Count} job state record(s), {snapshot.Operations.Count} operation trace record(s), {snapshot.CsvEntries.Count} CSV label summary record(s)."; }
    private static string Join(string? first, string? second) => string.Join(" · ", new[] { first, second }.Where(value => !string.IsNullOrWhiteSpace(value)));
    private static string Blank(string? value) => string.IsNullOrWhiteSpace(value) ? "Unknown" : value;
    private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null) { if (EqualityComparer<T>.Default.Equals(field, value)) return false; field = value; OnChanged(name); return true; }
    private void OnChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
