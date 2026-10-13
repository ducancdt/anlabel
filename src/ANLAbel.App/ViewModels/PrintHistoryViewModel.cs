using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ANLAbel.App.Services;
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
    public string TimestampText { get; init; } = "Chưa rõ";
    public string TemplateOrQueue { get; init; } = "Chưa rõ";
    public string Lifecycle { get; init; } = "Chưa rõ";
    public string Evidence { get; init; } = "Chưa rõ";
    public string Source { get; init; } = "Chưa rõ";
    public string Detail { get; init; } = "Không có chi tiết.";
}

/// <summary>Read-only P5 history projection with explicit source precedence and provenance.</summary>
public sealed class PrintHistoryViewModel : INotifyPropertyChanged
{
    public const string JobRecord = "Lệnh in";
    public const string JobTraceRecord = "Vết lệnh in";
    public const string CsvRecord = "Dòng tem (CSV)";

    private readonly Func<CancellationToken, Task<HistorySnapshot>> _load;
    private int _epoch;
    private bool _isRefreshing;
    private string _statusText = "Sẵn sàng đọc lịch sử in trên máy này.";
    private string _searchText = string.Empty;
    private string _detailText = "Chọn một bản ghi để xem nguồn gốc.";
    private readonly List<HistoryActivityRow> _all = [];
    private HistoryActivityRow? _selected;

    public PrintHistoryViewModel(Func<CancellationToken, Task<HistorySnapshot>> load) => _load = load ?? throw new ArgumentNullException(nameof(load));
    public event PropertyChangedEventHandler? PropertyChanged;
    public ObservableCollection<HistoryActivityRow> Rows { get; } = [];
    public bool IsRefreshing { get => _isRefreshing; private set { if (Set(ref _isRefreshing, value)) { OnChanged(nameof(CanRefresh)); OnChanged(nameof(RefreshButtonText)); } } }
    public bool CanRefresh => !IsRefreshing;
    public string RefreshButtonText => IsRefreshing ? "Đang làm mới…" : "Làm mới";
    public string StatusText { get => _statusText; private set => Set(ref _statusText, value); }
    public string SearchText { get => _searchText; set { if (Set(ref _searchText, value)) ApplyFilter(); } }
    public string DetailText { get => _detailText; private set => Set(ref _detailText, value); }
    public HistoryActivityRow? SelectedRow { get => _selected; set { if (Set(ref _selected, value)) DetailText = value?.Detail ?? "Chọn một bản ghi để xem nguồn gốc."; } }

    public async Task<bool> RefreshAsync(CancellationToken token = default)
    {
        var epoch = Interlocked.Increment(ref _epoch); IsRefreshing = true; StatusText = "Đang đọc nhật ký lệnh in, vết thao tác và tóm tắt CSV…";
        try
        {
            HistorySnapshot snapshot;
            try { snapshot = await _load(token).ConfigureAwait(true); }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
            catch (Exception ex) { if (epoch == Volatile.Read(ref _epoch)) StatusText = "Không làm mới được lịch sử: " + OperationFailureExplainer.StatusLine(ex); return false; }
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
        foreach (var csv in snapshot.CsvEntries) _all.Add(new HistoryActivityRow { RecordId = $"csv:{csv.RecordNumber}", RecordType = CsvRecord, TimestampText = csv.PrintedAtLocal is { } printedAt ? printedAt.ToString("yyyy-MM-dd HH:mm:ss") + " (giờ máy)" : "Chưa rõ thời gian", TemplateOrQueue = Join(csv.TemplateName, csv.PrinterName), Lifecycle = "Không phải vòng đời lệnh in", Evidence = "Chỉ là tóm tắt nội dung tem", Source = "CSV", Detail = $"Dòng tem CSV số {csv.RecordNumber}; số lượng {csv.Quantity}; không có mã lệnh in (không tạo mã giả)." });
        SelectedRow = _all.FirstOrDefault(row => row.RecordId == selected);
    }
    private static HistoryActivityRow FromState(PrintJobStateEvent state, PrintOperationLogEntry? operation) => new() { RecordId = state.JobId, RecordType = JobRecord, TimestampText = state.TimestampUtc.ToString("yyyy-MM-dd HH:mm:ss 'UTC'"), TemplateOrQueue = Join(operation?.TemplateName, state.PrinterName), Lifecycle = PrintCenterText.State(state.To), Evidence = state.PhysicalOutputVerified ? "Có bằng chứng từ thiết bị xác minh" : string.IsNullOrWhiteSpace(state.QueueState) ? "Sự kiện vòng đời đã lưu" : $"Hàng đợi: {PrintCenterText.Queue(state.QueueState)}; chưa xác minh tem đã ra", Source = operation is null ? "Nhật ký lệnh in" : "Nhật ký lệnh in + vết thao tác", Detail = $"Sự kiện số {state.Sequence}; thao tác: {Blank(PrintCenterText.Action(state.OperatorAction))}; lệnh liên quan: {Blank(state.RelatedJobId)}; bản kê: {Blank(state.ManifestFingerprint)}; ghi chú trong nhật ký: {Blank(state.Reason)}." };
    private static HistoryActivityRow FromOperation(PrintOperationLogEntry operation) => new() { RecordId = operation.JobId, RecordType = JobTraceRecord, TimestampText = (operation.SpoolStatusObservedAtUtc?.ToString("yyyy-MM-dd HH:mm:ss 'UTC'") ?? operation.TimestampLocal.ToString("yyyy-MM-dd HH:mm:ss '(giờ máy)'")), TemplateOrQueue = Join(operation.TemplateName, operation.PrinterName), Lifecycle = "Chưa rõ (không có sự kiện vòng đời)", Evidence = $"Kết quả thao tác: {Blank(operation.Outcome)}; không thay cho vòng đời", Source = "Vết thao tác", Detail = $"Mã lệnh hàng đợi {operation.SpoolJobId?.ToString() ?? "không có"}: {Blank(PrintCenterText.Queue(operation.SpoolState))}. Chưa xác minh tem đã ra. {Blank(operation.OutcomeEvidence)}" };
    private void ApplyFilter() { var keep = SelectedRow?.RecordId; var query = SearchText.Trim(); Rows.Clear(); foreach (var row in _all.Where(row => string.IsNullOrWhiteSpace(query) || row.RecordId.Contains(query, StringComparison.OrdinalIgnoreCase) || row.TemplateOrQueue.Contains(query, StringComparison.OrdinalIgnoreCase) || row.Lifecycle.Contains(query, StringComparison.OrdinalIgnoreCase))) Rows.Add(row); SelectedRow = Rows.FirstOrDefault(row => row.RecordId == keep) ?? Rows.FirstOrDefault(); }
    private static string BuildStatus(HistorySnapshot snapshot) { var diagnostics = snapshot.State.StoreDiagnostics.Count + snapshot.OperationDiagnostics.Count + snapshot.CsvDiagnostics.Count; return diagnostics > 0 ? $"Đã tải lịch sử; {diagnostics} cảnh báo chẩn đoán từ nguồn dữ liệu, các bản ghi hợp lệ vẫn hiển thị." : $"Đã tải lịch sử: {snapshot.State.LatestEvents.Count} lệnh in, {snapshot.Operations.Count} vết thao tác, {snapshot.CsvEntries.Count} dòng tem CSV."; }
    private static string Join(string? first, string? second) => string.Join(" · ", new[] { first, second }.Where(value => !string.IsNullOrWhiteSpace(value)));
    private static string Blank(string? value) => string.IsNullOrWhiteSpace(value) ? "chưa rõ" : value;
    private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null) { if (EqualityComparer<T>.Default.Equals(field, value)) return false; field = value; OnChanged(name); return true; }
    private void OnChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
