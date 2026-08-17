using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ANLAbel.Printing.PrinterProfiles;

namespace ANLAbel.App.ViewModels;

public sealed record SavedQueueEvidence(
    string RequestedName,
    bool IsAvailable,
    string CanonicalName,
    string ErrorMessage,
    DateTimeOffset ObservedAtUtc);

public sealed class PrintQueueRow
{
    public required string QueueName { get; init; }
    public string DriverName { get; init; } = "Unknown";
    public bool IsDefault { get; init; }
    public bool IsDiscovered { get; init; }
    public string SavedRelation { get; init; } = "Different discovered queue";
    public string Availability { get; init; } = "Unknown";
    public string Detail { get; init; } = "Discovery is not queue health.";
    public DateTimeOffset ObservedAtUtc { get; init; }
}

/// <summary>
/// Read-only P2 projection. It makes source errors explicit and never turns a
/// default marker, discovered queue, or job observation into a selection or a
/// command capability.
/// </summary>
public sealed class PrintQueueConsoleViewModel : INotifyPropertyChanged
{
    private readonly Func<CancellationToken, Task<PrinterDiscoveryResult>> _discover;
    private readonly Func<CancellationToken, Task<SavedQueueEvidence>> _loadSavedQueue;
    private readonly Func<DateTimeOffset> _clock;
    private readonly List<PrintQueueRow> _allRows = [];
    private int _epoch;
    private bool _isRefreshing;
    private string _searchText = string.Empty;
    private string _filter = "All";
    private string _statusText = "Ready to read local queue evidence.";
    private string _detailText = "Select a queue to inspect only the evidence available locally.";
    private DateTimeOffset? _refreshedAtUtc;
    private PrintQueueRow? _selectedRow;

    public PrintQueueConsoleViewModel(
        Func<CancellationToken, Task<PrinterDiscoveryResult>> discover,
        Func<CancellationToken, Task<SavedQueueEvidence>> loadSavedQueue,
        Func<DateTimeOffset>? clock = null)
    {
        _discover = discover ?? throw new ArgumentNullException(nameof(discover));
        _loadSavedQueue = loadSavedQueue ?? throw new ArgumentNullException(nameof(loadSavedQueue));
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public ObservableCollection<PrintQueueRow> Rows { get; } = [];
    public IReadOnlyList<string> Filters { get; } = ["All", "Saved", "Unavailable", "Unknown"];
    public bool IsRefreshing { get => _isRefreshing; private set { if (Set(ref _isRefreshing, value)) { OnChanged(nameof(CanRefresh)); OnChanged(nameof(RefreshButtonText)); } } }
    public bool CanRefresh => !IsRefreshing;
    public string RefreshButtonText => IsRefreshing ? "Refreshing…" : "Refresh";
    public string StatusText { get => _statusText; private set => Set(ref _statusText, value); }
    public string DetailText { get => _detailText; private set => Set(ref _detailText, value); }
    public DateTimeOffset? RefreshedAtUtc { get => _refreshedAtUtc; private set { if (Set(ref _refreshedAtUtc, value)) OnChanged(nameof(RefreshedAtText)); } }
    public string RefreshedAtText => RefreshedAtUtc is null ? "Not refreshed" : $"Refreshed {RefreshedAtUtc.Value.ToLocalTime():yyyy-MM-dd HH:mm:ss}";
    public string SearchText { get => _searchText; set { if (Set(ref _searchText, value)) ApplyFilter(); } }
    public string Filter { get => _filter; set { if (Set(ref _filter, value)) ApplyFilter(); } }
    public PrintQueueRow? SelectedRow { get => _selectedRow; set { if (Set(ref _selectedRow, value)) DetailText = value is null ? "Select a queue to inspect only the evidence available locally." : value.Detail; } }

    public async Task<bool> RefreshAsync(CancellationToken cancellationToken = default)
    {
        var epoch = Interlocked.Increment(ref _epoch);
        IsRefreshing = true;
        StatusText = "Reading Local and Connections queues plus saved-queue evidence…";
        try
        {
            var discoveryTask = ObserveAsync(_discover, cancellationToken);
            var savedTask = ObserveAsync(_loadSavedQueue, cancellationToken);
            await Task.WhenAll(discoveryTask, savedTask).ConfigureAwait(true);
            if (epoch != Volatile.Read(ref _epoch)) return false;

            var discovery = await discoveryTask.ConfigureAwait(true);
            var saved = await savedTask.ConfigureAwait(true);
            BuildRows(discovery.Value, discovery.Error, saved.Value, saved.Error);
            RefreshedAtUtc = _clock();
            StatusText = BuildStatus(discovery.Value, discovery.Error, saved.Error);
            ApplyFilter();
            return true;
        }
        finally
        {
            if (epoch == Volatile.Read(ref _epoch)) IsRefreshing = false;
        }
    }

    private void BuildRows(PrinterDiscoveryResult? discovery, Exception? discoveryError, SavedQueueEvidence? saved, Exception? savedError)
    {
        var selectedName = SelectedRow?.QueueName;
        _allRows.Clear();
        var observedAt = _clock();
        if (discoveryError is null && discovery is not null)
        {
            foreach (var printer in discovery.Printers)
            {
                _allRows.Add(CreateRow(printer, saved, savedError, observedAt));
            }
        }

        var savedName = saved?.RequestedName;
        var hasSavedRow = !string.IsNullOrWhiteSpace(savedName) && _allRows.Any(row => string.Equals(row.QueueName, savedName, StringComparison.OrdinalIgnoreCase));
        if (!hasSavedRow && !string.IsNullOrWhiteSpace(savedName))
        {
            _allRows.Add(new PrintQueueRow
            {
                QueueName = savedName,
                IsDiscovered = false,
                SavedRelation = savedError is null && saved?.IsAvailable == true ? "Saved and available" : "Saved but unavailable",
                Availability = savedError is null && saved?.IsAvailable == true ? "Available" : "Unavailable",
                Detail = savedError?.Message ?? saved?.ErrorMessage ?? "Saved queue was not present in the current discovery result.",
                ObservedAtUtc = saved?.ObservedAtUtc ?? observedAt
            });
        }

        if (discoveryError is not null)
        {
            _allRows.Add(new PrintQueueRow { QueueName = "Discovery source", IsDiscovered = false, SavedRelation = "Unknown", Availability = "Enumeration failed", Detail = discoveryError.Message, ObservedAtUtc = observedAt });
        }
        else if (discovery is { IsSuccess: false })
        {
            _allRows.Add(new PrintQueueRow { QueueName = "Discovery source", IsDiscovered = false, SavedRelation = "Unknown", Availability = "Enumeration failed", Detail = discovery.ErrorMessage, ObservedAtUtc = observedAt });
        }

        SelectedRow = _allRows.FirstOrDefault(row => string.Equals(row.QueueName, selectedName, StringComparison.OrdinalIgnoreCase));
    }

    private static PrintQueueRow CreateRow(PrinterInfo printer, SavedQueueEvidence? saved, Exception? savedError, DateTimeOffset observedAt)
    {
        var matchesSaved = !string.IsNullOrWhiteSpace(saved?.RequestedName) && string.Equals(printer.Name, saved.RequestedName, StringComparison.OrdinalIgnoreCase);
        var available = matchesSaved && savedError is null && saved?.IsAvailable == true;
        var unavailable = matchesSaved && (!available);
        return new PrintQueueRow
        {
            QueueName = printer.Name,
            DriverName = string.IsNullOrWhiteSpace(printer.DriverName) ? "Unknown" : printer.DriverName,
            IsDefault = printer.IsDefault,
            IsDiscovered = true,
            SavedRelation = string.IsNullOrWhiteSpace(saved?.RequestedName) ? "Not selected" : matchesSaved ? available ? "Saved and available" : "Saved but unavailable" : "Different discovered queue",
            Availability = available ? "Available" : unavailable ? "Unavailable" : "Unknown",
            Detail = available ? "Explicit saved queue resolved exactly; default marker is informational only." : unavailable ? savedError?.Message ?? saved?.ErrorMessage ?? "Saved queue is unavailable." : "Discovered queue only; no queue-level health or job observation is implied.",
            ObservedAtUtc = matchesSaved ? saved?.ObservedAtUtc ?? observedAt : observedAt
        };
    }

    private void ApplyFilter()
    {
        var prior = SelectedRow?.QueueName;
        var query = SearchText.Trim();
        var filtered = _allRows.Where(row =>
            (string.IsNullOrWhiteSpace(query) || row.QueueName.Contains(query, StringComparison.OrdinalIgnoreCase) || row.DriverName.Contains(query, StringComparison.OrdinalIgnoreCase)) &&
            (Filter == "All" || Filter == "Saved" && row.SavedRelation.StartsWith("Saved", StringComparison.Ordinal) || Filter == "Unavailable" && row.Availability is "Unavailable" or "Enumeration failed" || Filter == "Unknown" && row.Availability == "Unknown"))
            .ToArray();
        Rows.Clear();
        foreach (var row in filtered) Rows.Add(row);
        SelectedRow = Rows.FirstOrDefault(row => string.Equals(row.QueueName, prior, StringComparison.OrdinalIgnoreCase)) ?? Rows.FirstOrDefault();
    }

    private static string BuildStatus(PrinterDiscoveryResult? discovery, Exception? discoveryError, Exception? savedError) =>
        discoveryError is not null || discovery is { IsSuccess: false }
            ? "Printer enumeration failed; any saved-queue evidence remains separate."
            : savedError is not null ? "Queue discovery completed; saved-queue lookup could not be read."
            : discovery is null || discovery.Printers.Count == 0 ? "No Local or Connections queues were found."
            : $"{discovery.Printers.Count} discovered queue(s); default markers are informational only.";

    private static async Task<(T? Value, Exception? Error)> ObserveAsync<T>(Func<CancellationToken, Task<T>> loader, CancellationToken token) where T : class
    {
        try { return (await loader(token).ConfigureAwait(true), null); }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch (Exception ex) { return (null, ex); }
    }
    private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null) { if (EqualityComparer<T>.Default.Equals(field, value)) return false; field = value; OnChanged(name); return true; }
    private void OnChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
