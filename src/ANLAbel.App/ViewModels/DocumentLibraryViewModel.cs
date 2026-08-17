using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace ANLAbel.App.ViewModels;

public sealed class LocalDocumentItem
{
    public required string FullPath { get; init; }
    public required string RelativePath { get; init; }
    public long SizeBytes { get; init; }
    public DateTime LastWriteLocal { get; init; }
    public string Validation { get; init; } = "Not opened";
}

/// <summary>Filesystem-first P3 browse projection. It never creates folders or opens files.</summary>
public sealed class DocumentLibraryViewModel : INotifyPropertyChanged
{
    private readonly Func<string> _rootProvider;
    private readonly List<LocalDocumentItem> _all = [];
    private string _rootStatus = "Not scanned";
    private string _searchText = string.Empty;
    private LocalDocumentItem? _selected;
    public DocumentLibraryViewModel(Func<string> rootProvider) => _rootProvider = rootProvider;
    public event PropertyChangedEventHandler? PropertyChanged;
    public ObservableCollection<LocalDocumentItem> Items { get; } = [];
    public string RootPath { get; private set; } = string.Empty;
    public string RootStatus { get => _rootStatus; private set => Set(ref _rootStatus, value); }
    public string SearchText { get => _searchText; set { if (Set(ref _searchText, value)) ApplyFilter(); } }
    public LocalDocumentItem? SelectedItem { get => _selected; set => Set(ref _selected, value); }
    public Task RefreshAsync(CancellationToken token = default) => Task.Run(() =>
    {
        RootPath = _rootProvider(); _all.Clear();
        try
        {
            if (string.IsNullOrWhiteSpace(RootPath) || !Directory.Exists(RootPath)) { RootStatus = "Local library root is not configured or unavailable. No fallback path was used."; ApplyFilter(); return; }
            foreach (var path in Directory.EnumerateFiles(RootPath, "*.*", SearchOption.TopDirectoryOnly).Where(path => Path.GetExtension(path).Equals(".anlabel", StringComparison.OrdinalIgnoreCase) || Path.GetExtension(path).Equals(".json", StringComparison.OrdinalIgnoreCase)))
            {
                token.ThrowIfCancellationRequested(); var info = new FileInfo(path);
                _all.Add(new LocalDocumentItem { FullPath = path, RelativePath = Path.GetRelativePath(RootPath, path), SizeBytes = info.Length, LastWriteLocal = info.LastWriteTime, Validation = "Local file; validation occurs on explicit open." });
            }
            RootStatus = _all.Count == 0 ? "Local root is available; no supported template files were found." : $"{_all.Count} local document(s) found. Built-in templates remain in the existing Template Library.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { RootStatus = $"Local library root could not be enumerated: {ex.Message}"; }
        ApplyFilter();
    }, token);
    private void ApplyFilter() { var selected = SelectedItem?.FullPath; var q = SearchText.Trim(); Items.Clear(); foreach (var item in _all.Where(item => string.IsNullOrWhiteSpace(q) || item.RelativePath.Contains(q, StringComparison.OrdinalIgnoreCase))) Items.Add(item); SelectedItem = Items.FirstOrDefault(item => item.FullPath == selected) ?? Items.FirstOrDefault(); }
    private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null) { if (EqualityComparer<T>.Default.Equals(field, value)) return false; field = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name)); return true; }
}
