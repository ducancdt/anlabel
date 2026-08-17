using System.Windows;
using ANLAbel.App.ViewModels;
namespace ANLAbel.App;
public partial class PrintHistoryWindow : Window
{
    private readonly PrintHistoryViewModel _viewModel; private readonly Action _openCenter;
    public PrintHistoryWindow(MainViewModel main, Action openCenter) { InitializeComponent(); _openCenter = openCenter; _viewModel = new PrintHistoryViewModel(main.ReadPrintHistorySnapshotAsync); DataContext = _viewModel; }
    private async void Window_Loaded(object sender, RoutedEventArgs e) { Loaded -= Window_Loaded; await _viewModel.RefreshAsync(); }
    private async void Refresh_Click(object sender, RoutedEventArgs e) => await _viewModel.RefreshAsync();
    private void Center_Click(object sender, RoutedEventArgs e) { _openCenter(); Activate(); }
    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
