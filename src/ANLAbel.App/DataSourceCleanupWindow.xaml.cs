using System.Windows;
using ANLAbel.App.ViewModels;
using ANLAbel.Core.Models;

namespace ANLAbel.App;

/// <summary>
/// Lets the user bulk-remove shared data sources whose Excel file is missing and that
/// have not been used recently (database-manager-module-plan.md M3). Opened from
/// <see cref="DatabaseManagerWindow"/>'s "Clean up..." button.
/// </summary>
public partial class DataSourceCleanupWindow : Window
{
    private readonly MainViewModel _viewModel;

    public DataSourceCleanupWindow(MainViewModel viewModel, IReadOnlyList<DataSource> candidates)
    {
        InitializeComponent();
        _viewModel = viewModel;
        OrphanListBox.ItemsSource = candidates.Select(source => new OrphanRow(source)).ToArray();
    }

    private void RemoveSelected_Click(object sender, RoutedEventArgs e)
    {
        var rows = (IEnumerable<OrphanRow>)OrphanListBox.ItemsSource;
        var selected = rows.Where(row => row.IsSelected).ToArray();
        if (selected.Length == 0)
        {
            MessageBox.Show(this, "Chưa chọn nguồn dữ liệu nào.", "Dọn dẹp nguồn dữ liệu", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var confirmed = MessageBox.Show(
            this,
            $"Xóa {selected.Length} nguồn dữ liệu khỏi sổ đăng ký? Thao tác này không thể hoàn tác.",
            "Dọn dẹp nguồn dữ liệu",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning) == MessageBoxResult.Yes;
        if (!confirmed)
        {
            return;
        }

        foreach (var row in selected)
        {
            _viewModel.RemoveDataSourceCommand.Execute(row.Source);
        }

        Close();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private sealed class OrphanRow
    {
        public OrphanRow(DataSource source)
        {
            Source = source;
            DisplayName = source.DisplayName;
            var lastUsedText = source.LastUsedUtc is { } lastUsed
                ? $"last used {lastUsed.ToLocalTime():yyyy-MM-dd}"
                : "never used";
            DetailText = $"{source.FilePath} — {lastUsedText}";
        }

        public DataSource Source { get; }
        public string DisplayName { get; }
        public string DetailText { get; }
        public bool IsSelected { get; set; }
    }
}
