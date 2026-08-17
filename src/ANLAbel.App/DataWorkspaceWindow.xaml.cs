using System.Windows;
using ANLAbel.App.ViewModels;

namespace ANLAbel.App;
public partial class DataWorkspaceWindow : Window
{
    private readonly DataWorkspaceViewModel _viewModel;
    public DataWorkspaceWindow(MainViewModel main) { InitializeComponent(); _viewModel = new DataWorkspaceViewModel(main.GetSelectedDataRecordForWorkspace, main.DataTransforms, main.ReplaceDataTransforms); DataContext = _viewModel; }
    private void Refresh_Click(object s, RoutedEventArgs e) => _viewModel.RefreshSample();
    private void Add_Click(object s, RoutedEventArgs e) => _viewModel.Add();
    private void Remove_Click(object s, RoutedEventArgs e) => _viewModel.Remove();
    private void Update_Click(object s, RoutedEventArgs e) => _viewModel.CommitEditor();
    private void Validate_Click(object s, RoutedEventArgs e) => _viewModel.Validate();
    private void Apply_Click(object s, RoutedEventArgs e) => _viewModel.Apply();
    private void Close_Click(object s, RoutedEventArgs e) => Close();
}
