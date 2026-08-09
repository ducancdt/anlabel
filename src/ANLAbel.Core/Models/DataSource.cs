using ANLAbel.Core.Mvvm;

namespace ANLAbel.Core.Models;

/// <summary>
/// Represents a shared data source (Excel file + sheet) that multiple templates
/// can reference by <see cref="Id"/>. Stored in a machine-wide registry file at
/// <c>%AppData%\ANLAbel\data-sources.json</c>.
/// </summary>
public sealed class DataSource : ObservableObject
{
    private string _id = Guid.NewGuid().ToString("N");
    private string _name = string.Empty;
    private string _filePath = string.Empty;
    private string _sheetName = string.Empty;
    private int _headerRowIndex = 1;
    private DateTime? _lastUsedUtc;
    private List<string> _recentTemplates = new();

    public string Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string FilePath
    {
        get => _filePath;
        set => SetProperty(ref _filePath, value);
    }

    public string SheetName
    {
        get => _sheetName;
        set => SetProperty(ref _sheetName, value);
    }

    public int HeaderRowIndex
    {
        get => _headerRowIndex;
        set => SetProperty(ref _headerRowIndex, Math.Max(1, value));
    }

    public string DisplayName => string.IsNullOrWhiteSpace(Name)
        ? $"{Path.GetFileName(FilePath)} / {SheetName}"
        : Name;

    /// <summary>
    /// Last time this source was actually loaded by a template (database-manager-module-plan.md
    /// M3) — lets the Database Manager show which sources are stale/unused before removal.
    /// Null for sources created before this field existed or never used since.
    /// </summary>
    public DateTime? LastUsedUtc
    {
        get => _lastUsedUtc;
        set => SetProperty(ref _lastUsedUtc, value);
    }

    /// <summary>
    /// Absolute paths of the last (up to 10, most-recent-first) .anlabel templates that
    /// loaded this source — answers "which templates does removing this affect" without
    /// scanning the whole disk. Defaults to an empty list so registries written before
    /// this field existed still deserialize cleanly.
    /// </summary>
    public List<string> RecentTemplates
    {
        get => _recentTemplates;
        set => SetProperty(ref _recentTemplates, value ?? new List<string>());
    }
}