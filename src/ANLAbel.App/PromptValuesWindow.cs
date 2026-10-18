using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ANLAbel.App.Services;
using ANLAbel.Core.Models;
using ANLAbel.Core.Printing;

namespace ANLAbel.App;

/// <summary>
/// Asks the operator for the label's {?Name} values before a print. Each value
/// is checked against its definition before the dialog closes.
/// </summary>
public sealed class PromptValuesWindow : Window
{
    private readonly IReadOnlyList<PromptVariableDefinition> _prompts;
    private readonly List<TextBox> _boxes = new();
    private readonly TextBlock _problem = new() { Foreground = Brushes.DarkRed, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 6, 0, 0) };

    private PromptValuesWindow(IReadOnlyList<PromptVariableDefinition> prompts, IReadOnlyDictionary<string, string> previous)
    {
        _prompts = prompts;
        Title = "Nhập giá trị cho lệnh in";
        SizeToContent = SizeToContent.WidthAndHeight;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        MinWidth = 360;

        var panel = new StackPanel { Margin = new Thickness(14) };
        panel.Children.Add(new TextBlock
        {
            Text = "Các giá trị này được in trên mọi tem của lệnh in và được lưu để in lại đúng như cũ.",
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 420,
            Margin = new Thickness(0, 0, 0, 8)
        });
        foreach (var prompt in prompts)
        {
            var label = string.IsNullOrWhiteSpace(prompt.Prompt) ? prompt.Name : prompt.Prompt;
            var hints = new List<string>();
            if (prompt.Required)
            {
                hints.Add("bắt buộc");
            }

            if (prompt.DigitsOnly)
            {
                hints.Add("chỉ chữ số");
            }

            if (prompt.MaxLength > 0)
            {
                hints.Add($"tối đa {prompt.MaxLength} ký tự");
            }

            panel.Children.Add(new TextBlock
            {
                Text = hints.Count == 0 ? label : $"{label} ({string.Join(", ", hints)})",
                Margin = new Thickness(0, 4, 0, 2)
            });
            var box = new TextBox
            {
                Text = previous.TryGetValue(prompt.Name, out var last) ? last : prompt.DefaultValue,
                MinWidth = 320,
                MaxLength = prompt.MaxLength > 0 ? Math.Min(prompt.MaxLength, JobVariableContract.MaximumPromptLength) : JobVariableContract.MaximumPromptLength
            };
            _boxes.Add(box);
            panel.Children.Add(box);
        }

        panel.Children.Add(_problem);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 10, 0, 0) };
        var ok = new Button { Content = "Dùng các giá trị này", IsDefault = true, Padding = new Thickness(10, 3, 10, 3), Margin = new Thickness(0, 0, 8, 0) };
        ok.Click += (_, _) => Accept();
        var cancel = new Button { Content = "Hủy", IsCancel = true, Padding = new Thickness(10, 3, 10, 3) };
        buttons.Children.Add(ok);
        buttons.Children.Add(cancel);
        panel.Children.Add(buttons);
        Content = panel;
        Loaded += (_, _) => _boxes.FirstOrDefault()?.Focus();
    }

    public Dictionary<string, string> Values { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Shows the dialog; null when the operator cancels.</summary>
    public static IReadOnlyDictionary<string, string>? Ask(
        Window? owner,
        IReadOnlyList<PromptVariableDefinition> prompts,
        IReadOnlyDictionary<string, string> previous)
    {
        if (prompts.Count == 0)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        var dialog = new PromptValuesWindow(prompts, previous) { Owner = owner };
        return dialog.ShowDialog() == true ? dialog.Values : null;
    }

    private void Accept()
    {
        Values.Clear();
        for (var i = 0; i < _prompts.Count; i++)
        {
            var problem = JobVariableContract.ValidatePromptValue(_prompts[i], _boxes[i].Text);
            if (!string.IsNullOrEmpty(problem))
            {
                _problem.Text = JobVariableText.Message(problem);
                _boxes[i].Focus();
                return;
            }

            Values[_prompts[i].Name] = _boxes[i].Text;
        }

        DialogResult = true;
    }
}
