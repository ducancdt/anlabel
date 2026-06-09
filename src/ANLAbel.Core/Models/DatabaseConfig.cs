using System.Collections.ObjectModel;

namespace ANLAbel.Core.Models;

public sealed class DatabaseConfig
{
    public string FilePath { get; set; } = string.Empty;
    public string SheetName { get; set; } = string.Empty;
    public int HeaderRowIndex { get; set; } = 1;
    public string KeyField { get; set; } = string.Empty;
    public int LastSelectedRow { get; set; }
    public ObservableCollection<DatabaseField> AvailableFields { get; set; } = new();
    public ObservableCollection<DatabaseField> LabelFields { get; set; } = new();
}
