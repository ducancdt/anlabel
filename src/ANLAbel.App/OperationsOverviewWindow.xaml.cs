using System.Windows;
using System.Windows.Input;
using ANLAbel.App.ViewModels;

namespace ANLAbel.App;

public partial class OperationsOverviewWindow : Window
{
    private readonly OperationsOverviewViewModel _overview;
    private readonly Action _openPrinterSetup;
    private readonly Action _openPrintCenter;
    private readonly Action _openPrintHistory;

    public OperationsOverviewWindow(
        MainViewModel mainViewModel,
        Action openPrinterSetup,
        Action openPrintCenter,
        Action openPrintHistory)
    {
        ArgumentNullException.ThrowIfNull(mainViewModel);
        _openPrinterSetup = openPrinterSetup ?? throw new ArgumentNullException(nameof(openPrinterSetup));
        _openPrintCenter = openPrintCenter ?? throw new ArgumentNullException(nameof(openPrintCenter));
        _openPrintHistory = openPrintHistory ?? throw new ArgumentNullException(nameof(openPrintHistory));

        InitializeComponent();
        _overview = new OperationsOverviewViewModel(
            async cancellationToken =>
            {
                await mainViewModel.RefreshPrinterQueueStatusAsync(cancellationToken);
                var queue = mainViewModel.PrinterQueueStatus;
                return new OperationsQueueEvidence(
                    queue.RequestedName,
                    queue.IsAvailable,
                    queue.CanonicalName,
                    queue.ErrorMessage,
                    DateTimeOffset.UtcNow);
            },
            async cancellationToken =>
            {
                await mainViewModel.RefreshPrintRecoveryAsync(cancellationToken);
                return mainViewModel.PrintRecoveryReport;
            });
        DataContext = _overview;
    }

    private async void OperationsOverviewWindow_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OperationsOverviewWindow_Loaded;
        await RefreshAsync();
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        try
        {
            await _overview.RefreshAsync();
        }
        catch (OperationCanceledException)
        {
            // Window lifetime cancellation is not surfaced as an operator fault.
        }
    }

    private async void OpenPrinterSetup_Click(object sender, RoutedEventArgs e)
    {
        _openPrinterSetup();
        Activate();
        await RefreshAsync();
    }

    private void OpenPrintCenter_Click(object sender, RoutedEventArgs e)
    {
        _openPrintCenter();
        Activate();
    }

    private void OpenPrintHistory_Click(object sender, RoutedEventArgs e)
    {
        _openPrintHistory();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void OperationsOverviewWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F5 && Keyboard.Modifiers == ModifierKeys.None && _overview.CanRefresh)
        {
            e.Handled = true;
            await RefreshAsync();
            return;
        }

        if (e.Key == Key.Escape && Keyboard.Modifiers == ModifierKeys.None && !_overview.IsRefreshing)
        {
            e.Handled = true;
            Close();
        }
    }
}
