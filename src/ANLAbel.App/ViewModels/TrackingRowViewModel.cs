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
    private bool _isPrinted;

    public int SourceRowNumber { get; init; }
    public int PageNumber { get; set; }
    public string? Col1 { get; init; }
    public string? Col2 { get; init; }
    public string? Col3 { get; init; }
    public string? Col4 { get; init; }

    public string CheckmarkText => _isSelected ? "☑" : "☐";
    public string CopiesDisplay => _copies.ToString();

    /// <summary>
    /// True once this row has been sent to the printer during the current Print Preview
    /// session (print-preview-reliability-plan Đợt 4 item 11 — "chống trùng tem"). Reset
    /// each time the preview is rebuilt/reopened; this is a same-session duplicate guard,
    /// not a persistent cross-session record.
    /// </summary>
    public bool IsPrinted
    {
        get => _isPrinted;
        set
        {
            if (_isPrinted == value) return;
            _isPrinted = value;
            OnPropertyChanged(nameof(IsPrinted));
            OnPropertyChanged(nameof(PrintedIndicatorText));
        }
    }

    public string PrintedIndicatorText => _isPrinted ? "🖶" : string.Empty;

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