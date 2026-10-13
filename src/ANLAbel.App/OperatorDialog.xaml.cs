using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;

namespace ANLAbel.App;

public enum OperatorDialogTone
{
    Normal,
    Warning,
    Error
}

/// <summary>One button of an <see cref="OperatorDialog"/>.</summary>
/// <param name="Id">Stable result id returned to the caller.</param>
/// <param name="Label">Vietnamese button text.</param>
/// <param name="IsDefault">Primary action, triggered by Enter.</param>
/// <param name="IsCancel">Triggered by Esc and by closing the window.</param>
public sealed record OperatorDialogChoice(string Id, string Label, bool IsDefault = false, bool IsCancel = false);

/// <summary>
/// Plain-language operator dialog (plan 13: unsaved changes, unexpected error,
/// recovery offer, configuration notice, Excel refresh failure). It states the
/// problem, what was affected and the next useful action; technical detail
/// stays in the muted detail line.
/// </summary>
public partial class OperatorDialog : Window
{
    private readonly string? _cancelId;

    public OperatorDialog(
        string heading,
        IReadOnlyList<string> lines,
        string? detail,
        OperatorDialogTone tone,
        IReadOnlyList<OperatorDialogChoice> choices)
    {
        ArgumentNullException.ThrowIfNull(lines);
        ArgumentNullException.ThrowIfNull(choices);
        if (choices.Count == 0)
        {
            throw new ArgumentException("A dialog needs at least one choice.", nameof(choices));
        }

        InitializeComponent();
        Title = heading;
        HeadingText.Text = heading;
        HeadingText.Foreground = tone switch
        {
            OperatorDialogTone.Error => new SolidColorBrush(Color.FromRgb(0xB9, 0x1C, 0x1C)),
            OperatorDialogTone.Warning => new SolidColorBrush(Color.FromRgb(0xB4, 0x53, 0x09)),
            _ => TryFindResource("AppInkBrush") as Brush ?? new SolidColorBrush(Color.FromRgb(0x1E, 0x29, 0x3B))
        };
        LinesList.ItemsSource = lines.Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();
        if (!string.IsNullOrWhiteSpace(detail))
        {
            DetailText.Text = detail;
            DetailText.Visibility = Visibility.Visible;
        }

        _cancelId = choices.FirstOrDefault(choice => choice.IsCancel)?.Id;
        for (var index = 0; index < choices.Count; index++)
        {
            var choice = choices[index];
            var button = new Button
            {
                Content = choice.Label,
                IsDefault = choice.IsDefault,
                IsCancel = choice.IsCancel,
                MinWidth = 88,
                Margin = new Thickness(index == 0 ? 0 : 8, 0, 0, 0)
            };
            AutomationProperties.SetAutomationId(button, "OperatorDialog.Choice." + choice.Id);
            if (choice.IsDefault)
            {
                button.Background = TryFindResource("AppPrimaryBrush") as Brush ?? new SolidColorBrush(Color.FromRgb(0x14, 0x64, 0xD2));
                button.Foreground = Brushes.White;
                button.FontWeight = FontWeights.SemiBold;
            }

            button.Click += (_, _) =>
            {
                ChosenId = choice.Id;
                DialogResult = true;
            };
            ButtonsPanel.Children.Add(button);
        }

        Closed += (_, _) => ChosenId ??= _cancelId;
    }

    /// <summary>The id of the chosen button; the cancel choice when the window was closed.</summary>
    public string? ChosenId { get; private set; }

    /// <summary>Shows the dialog modally and returns the chosen id.</summary>
    public static string? Ask(
        Window? owner,
        string heading,
        IReadOnlyList<string> lines,
        string? detail,
        OperatorDialogTone tone,
        params OperatorDialogChoice[] choices)
    {
        var dialog = new OperatorDialog(heading, lines, detail, tone, choices);
        if (owner is { IsLoaded: true, IsVisible: true })
        {
            dialog.Owner = owner;
        }
        else
        {
            dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        dialog.ShowDialog();
        return dialog.ChosenId;
    }
}
