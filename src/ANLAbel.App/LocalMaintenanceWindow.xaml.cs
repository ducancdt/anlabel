using System.Windows;
using ANLAbel.App.ViewModels;

namespace ANLAbel.App;

public partial class LocalMaintenanceWindow : Window
{
    private readonly LocalMaintenanceViewModel _viewModel;
    private readonly Action _printerSetup, _dataSources, _history, _analytics, _printCenter;
    public LocalMaintenanceWindow(MainViewModel main, Action printerSetup, Action dataSources, Action history, Action analytics, Action printCenter)
    {
        InitializeComponent();
        _viewModel = new LocalMaintenanceViewModel(() => main.DataSources.ToArray(), () => main.PrintHistoryFilePath);
        _printerSetup = printerSetup; _dataSources = dataSources; _history = history; _analytics = analytics; _printCenter = printCenter;
        DataContext = _viewModel;
    }
    private void Window_Loaded(object s, RoutedEventArgs e) { Loaded -= Window_Loaded; _viewModel.Refresh(); }
    private void Refresh_Click(object s, RoutedEventArgs e) => _viewModel.Refresh();
    private void PrinterSetup_Click(object s, RoutedEventArgs e) { _printerSetup(); Activate(); }
    private void DataSources_Click(object s, RoutedEventArgs e) { _dataSources(); Activate(); }
    private void Cleanup_Click(object s, RoutedEventArgs e) { _dataSources(); Activate(); }
    private void History_Click(object s, RoutedEventArgs e) { _history(); Activate(); }
    private void Analytics_Click(object s, RoutedEventArgs e) { _analytics(); Activate(); }
    private void PrintCenter_Click(object s, RoutedEventArgs e) { _printCenter(); Activate(); }
    private void Close_Click(object s, RoutedEventArgs e) => Close();
}
