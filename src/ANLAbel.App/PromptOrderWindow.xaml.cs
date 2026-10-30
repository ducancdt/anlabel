using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using ANLAbel.Core.Models;

namespace ANLAbel.App;

/// <summary>Reorders the prompt variables; <see cref="Order"/> holds the result after OK.</summary>
public partial class PromptOrderWindow : Window
{
    private readonly ObservableCollection<PromptVariableDefinition> _order;

    public PromptOrderWindow(IEnumerable<PromptVariableDefinition> prompts)
    {
        InitializeComponent();
        _order = new ObservableCollection<PromptVariableDefinition>(prompts);
        PromptList.ItemsSource = _order;
        if (_order.Count > 0)
        {
            PromptList.SelectedIndex = 0;
        }

        UpdateButtons();
    }

    /// <summary>The variables in the chosen order.</summary>
    public IReadOnlyList<PromptVariableDefinition> Order => _order;

    private void MoveUp_Click(object sender, RoutedEventArgs e) => Move(-1);

    private void MoveDown_Click(object sender, RoutedEventArgs e) => Move(1);

    private void Move(int delta)
    {
        var index = PromptList.SelectedIndex;
        var target = index + delta;
        if (index < 0 || target < 0 || target >= _order.Count)
        {
            return;
        }

        _order.Move(index, target);
        PromptList.SelectedIndex = target;
        PromptList.ScrollIntoView(_order[target]);
        UpdateButtons();
    }

    private void PromptList_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateButtons();

    private void UpdateButtons()
    {
        var index = PromptList.SelectedIndex;
        MoveUpButton.IsEnabled = index > 0;
        MoveDownButton.IsEnabled = index >= 0 && index < _order.Count - 1;
    }

    private void Ok_Click(object sender, RoutedEventArgs e) => DialogResult = true;
}
