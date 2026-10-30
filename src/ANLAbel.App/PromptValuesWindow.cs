using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using ANLAbel.App.Services;
using ANLAbel.Core.Enums;
using ANLAbel.Core.Models;
using ANLAbel.Core.Printing;

namespace ANLAbel.App;

/// <summary>
/// Asks the operator for the label's {?Name} values before a print. Each value
/// is checked against its definition before the dialog closes. A pick list is
/// offered as a list to choose from, a date as a calendar, and a value with
/// output rules (date format, prefix, suffix, padding) shows what will print
/// (NiceLabel 10 variable rules).
/// </summary>
public sealed class PromptValuesWindow : Window
{
    private readonly IReadOnlyList<PromptVariableDefinition> _prompts;
    private readonly List<Control> _inputs = new();
    private readonly List<Func<string>> _readers = new();
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
            var choices = prompt.PickListValues;
            var hints = new List<string>();
            if (prompt.Required)
            {
                hints.Add("bắt buộc");
            }

            if (choices.Count > 0)
            {
                hints.Add("chọn trong danh sách");
            }

            var isDate = JobVariableContract.IsDatePrompt(prompt);
            if (isDate)
            {
                hints.Add("chọn ngày");
            }

            if (prompt.DigitsOnly)
            {
                hints.Add("chỉ chữ số");
            }

            if (prompt.AllowedCharacters.Length > 0)
            {
                hints.Add($"chỉ các ký tự {prompt.AllowedCharacters}");
            }

            if (prompt.MaxLength > 0)
            {
                hints.Add(prompt.FixedLength && prompt.Padding == PromptPadding.None
                    ? $"đúng {prompt.MaxLength} ký tự"
                    : $"tối đa {prompt.MaxLength} ký tự");
            }

            panel.Children.Add(new TextBlock
            {
                Text = hints.Count == 0 ? label : $"{label} ({string.Join(", ", hints)})",
                Margin = new Thickness(0, 4, 0, 2)
            });

            var initial = previous.TryGetValue(prompt.Name, out var last) ? last : prompt.DefaultValue;
            Control input;
            Func<string> read;
            if (isDate)
            {
                var picker = new DatePicker
                {
                    MinWidth = 320,
                    // The date sits mid-height like the text boxes around it.
                    VerticalContentAlignment = VerticalAlignment.Center,
                    // Vietnamese dates (dd/MM/yyyy) whatever the Windows region.
                    Language = System.Windows.Markup.XmlLanguage.GetLanguage("vi-VN"),
                    SelectedDate = JobVariableContract.TryParsePromptDate(prompt, initial, out var initialDate) ? initialDate : null
                };
                input = picker;
                read = () => picker.SelectedDate is { } date
                    ? date.ToString(JobVariableContract.DateAnswerFormat, CultureInfo.InvariantCulture)
                    : picker.Text;
            }
            else if (choices.Count > 0)
            {
                var list = new ComboBox
                {
                    ItemsSource = choices,
                    MinWidth = 320,
                    SelectedItem = choices.Contains(initial, StringComparer.Ordinal) ? initial : null
                };
                input = list;
                read = () => list.SelectedItem as string ?? string.Empty;
            }
            else
            {
                var box = new TextBox
                {
                    Text = initial,
                    MinWidth = 320,
                    MaxLength = prompt.MaxLength > 0 ? Math.Min(prompt.MaxLength, JobVariableContract.MaximumPromptLength) : JobVariableContract.MaximumPromptLength
                };
                input = box;
                read = () => box.Text;
            }

            AutomationProperties.SetName(input, label);
            _inputs.Add(input);
            _readers.Add(read);
            panel.Children.Add(input);
            if (isDate || prompt.Prefix.Length > 0 || prompt.Suffix.Length > 0 || prompt.Padding != PromptPadding.None)
            {
                var printed = new TextBlock { Foreground = Brushes.DimGray, FontSize = 11, Margin = new Thickness(0, 2, 0, 0) };
                void Show() => printed.Text = "Sẽ in: " + JobVariableContract.FormatPromptValue(prompt, read());
                if (input is DatePicker datePicker)
                {
                    datePicker.SelectedDateChanged += (_, _) => Show();
                }
                else if (input is ComboBox comboBox)
                {
                    comboBox.SelectionChanged += (_, _) => Show();
                }
                else if (input is TextBox textBox)
                {
                    textBox.TextChanged += (_, _) => Show();
                }

                Show();
                panel.Children.Add(printed);
            }
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
        Loaded += (_, _) => _inputs.FirstOrDefault()?.Focus();
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
            // The answer as typed or chosen; the output rules apply when it prints.
            var answer = _readers[i]();
            var problem = JobVariableContract.ValidatePromptValue(_prompts[i], answer);
            if (!string.IsNullOrEmpty(problem))
            {
                _problem.Text = JobVariableText.Message(problem);
                _inputs[i].Focus();
                return;
            }

            // A date is kept as yyyy-MM-dd whichever way it was entered.
            Values[_prompts[i].Name] = JobVariableContract.NormalizePromptAnswer(_prompts[i], answer);
        }

        DialogResult = true;
    }
}
