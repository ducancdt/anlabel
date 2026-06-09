using System.ComponentModel;

namespace ANLAbel.App.ViewModels;

/// <summary>
/// Simple view model for one row in the Excel tracking list.
/// Replaces DataTable/DataRowView to eliminate all binding crash issues.
/// </summary>
public sealed class TrackingRowViewModel : INotifyPropertyChanged
{
    private bool _isSelected = true;
    private int _copies = 1;

    public int SourceRowNumber { get; init; }
    public int PageNumber { get; set; }
    public string? Col1 { get; init; }
    public string? Col2 { get; init; }
    public string? Col3 { get; init; }
    public string? Col4 { get; init; }

    public string CheckmarkText => _isSelected ? "☑" : "☐";
    public string CopiesDisplay => _copies.ToString();

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value) return;
            _isSelected = value;
            OnPropertyChanged(nameof(IsSelected));
            OnPropertyChanged(nameof(CheckmarkText));
        }
    }

    public int Copies
    {
        get => _copies;
        set
        {
            var clamped = Math.Max(0, Math.Min(999, value));
            if (_copies == clamped) return;
            _copies = clamped;
            OnPropertyChanged(nameof(Copies));
            OnPropertyChanged(nameof(CopiesDisplay));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}