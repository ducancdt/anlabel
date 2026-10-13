using System.Windows;
using System.IO;
using ANLAbel.App.ViewModels;
using ANLAbel.Data.Automation;

namespace ANLAbel.App;

public partial class AutomationConfigurationWindow : Window
{
    private readonly AutomationConfigurationViewModel _viewModel;
    public AutomationConfigurationWindow(FileDropTriggerConfigurationStore? store = null)
    {
        InitializeComponent();
        _viewModel = new AutomationConfigurationViewModel(store ?? new FileDropTriggerConfigurationStore(ANLAbel.Data.AppDataPaths.Local("automation-trigger.json")));
        DataContext = _viewModel;
    }
    private void Save_Click(object sender, RoutedEventArgs e) { if (_viewModel.Save()) DialogResult = true; }
    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
