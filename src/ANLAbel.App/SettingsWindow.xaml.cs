using System.Windows;
using ANLAbel.App.ViewModels;

namespace ANLAbel.App;

public partial class SettingsWindow : Window
{
    private readonly UiStrings _ui;

    public SettingsWindow()
        : this(UiStrings.Current)
    {
    }

    public SettingsWindow(UiStrings ui)
    {
        _ui = ui;
        DataContext = _ui;
        InitializeComponent();
        Title = "Cài đặt ANLAbel";
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
