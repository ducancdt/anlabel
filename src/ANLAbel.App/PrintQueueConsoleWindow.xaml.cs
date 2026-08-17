using System.Windows;
using System.Windows.Input;
using ANLAbel.App.ViewModels;

namespace ANLAbel.App;

public partial class PrintQueueConsoleWindow : Window
{
    private readonly PrintQueueConsoleViewModel _viewModel;
    private readonly Action _openSetup;
    private readonly Action _openCenter;
    private readonly Action _openHistory;

    public PrintQueueConsoleWindow(MainViewModel main, Action openSetup, Action openCenter, Action openHistory)
    {
        ArgumentNullException.ThrowIfNull(main);
        _openSetup = openSetup ?? throw new ArgumentNullException(nameof(openSetup));
        _openCenter = openCenter ?? throw new ArgumentNullException(nameof(openCenter));
        _openHistory = openHistory ?? throw new ArgumentNullException(nameof(openHistory));
        InitializeComponent();
        _viewModel = new PrintQueueConsoleViewModel(
            token => Task.Run(main.DiscoverInstalledPrinters, token),
            async token =>
            {
                await main.RefreshPrinterQueueStatusAsync(token);
                var status = main.PrinterQueueStatus;
                return new SavedQueueEvidence(status.RequestedName, status.IsAvailable, status.CanonicalName, status.ErrorMessage, DateTimeOffset.UtcNow);
            });
        DataContext = _viewModel;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e) { Loaded -= Window_Loaded; await RefreshAsync(); }
    private async void Refresh_Click(object sender, RoutedEventArgs e) => await RefreshAsync();
    private async Task RefreshAsync() { try { await _viewModel.RefreshAsync(); } catch (OperationCanceledException) { } }
    private async void PrinterSetup_Click(object sender, RoutedEventArgs e) { _openSetup(); Activate(); await RefreshAsync(); }
    private void PrintCenter_Click(object sender, RoutedEventArgs e) { _openCenter(); Activate(); }
    private void History_Click(object sender, RoutedEventArgs e) => _openHistory();
    private void Close_Click(object sender, RoutedEventArgs e) => Close();
    private async void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F5 && Keyboard.Modifiers == ModifierKeys.None && _viewModel.CanRefresh) { e.Handled = true; await RefreshAsync(); }
        else if (e.Key == Key.Escape && Keyboard.Modifiers == ModifierKeys.None && !_viewModel.IsRefreshing) { e.Handled = true; Close(); }
    }
}
