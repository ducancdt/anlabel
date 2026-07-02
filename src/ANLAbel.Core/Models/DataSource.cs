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
}