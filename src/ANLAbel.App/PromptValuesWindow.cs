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

            var minimum = prompt.MinLength > 0 && prompt.Padding == PromptPadding.None ? prompt.MinLength : 0;
            if (prompt.MaxLength > 0)
            {
                hints.Add(prompt.FixedLength && prompt.Padding == PromptPadding.None
                    ? $"đúng {prompt.MaxLength} ký tự"
                    : minimum > 0 && minimum < prompt.MaxLength
                        ? $"từ {minimum} đến {prompt.MaxLength} ký tự"
                        : $"tối đa {prompt.MaxLength} ký tự");
            }
            else if (minimum > 0)
            {
                hints.Add($"ít nhất {minimum} ký tự");
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
                    VerticalContentAlignment = VerticalAlignment.Center,
                    // Vietnamese dates (dd/MM/yyyy) whatever the Windows region.
                    Language = System.Windows.Markup.XmlLanguage.GetLanguage("vi-VN"),
                    SelectedDate = JobVariableContract.TryParsePromptDate(prompt, initial, out var initialDate) ? initialDate : null
                };
                picker.Loaded += (_, _) => CenterDateText(picker);

                // The picker reads dates in its own (vi-VN) format and, when the
                // box loses focus on a text it cannot read, falls back to its last
                // good date, so a typo would print the old date. What was typed is
                // kept for Accept to judge: a date in the prompt's own input format
                // (yyMMdd) is taken and shown, anything else is turned back.
                string? typed = null;
                var reverting = false;
                picker.DateValidationError += (_, e) =>
                {
                    typed = e.Text;
                    reverting = true;
                    DateTime? readable = JobVariableContract.TryParsePromptDate(prompt, e.Text, out var date) ? date : null;
                    Dispatcher.BeginInvoke(() =>
                    {
                        reverting = false;
                        if (readable is { } value)
                        {
                            picker.SelectedDate = value;
                        }
                        else if (picker.Template?.FindName("PART_TextBox", picker) is TextBox box && box.Text != e.Text)
                        {
                            // Show the typo where it was typed, beside the message about it.
                            box.Text = e.Text;
                        }
                    }, System.Windows.Threading.DispatcherPriority.Background);
                };
                picker.AddHandler(System.Windows.Controls.Primitives.TextBoxBase.TextChangedEvent, new TextChangedEventHandler((_, _) =>
                {
                    if (!reverting)
                    {
                        typed = null;
                    }
                }));
                input = picker;
                read = () => typed ?? (picker.SelectedDate is { } date
                    ? date.ToString(JobVariableContract.DateAnswerFormat, CultureInfo.InvariantCulture)
                    : picker.Text);
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
                // No MaxLength on the box: a scanner types every character, and a
                // box that drops the extra ones would print a shortened value without
                // a word. Accept turns an over-long value back instead.
                var box = new TextBox
                {
                    Text = initial,
                    MinWidth = 320
                };
                input = box;
                read = () => box.Text;
            }

            AutomationProperties.SetName(input, label);
            // Enter goes to the next question and only the last one's Enter
            // accepts: a scanner that ends each scan with Enter must not send
            // the dialog after the first field with the other fields' old values.
            var position = _inputs.Count;
            input.PreviewKeyDown += (_, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Enter
                    && System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.None
                    && position < _prompts.Count - 1
                    && input is not ComboBox { IsDropDownOpen: true })
                {
                    _inputs[position + 1].Focus();
                    e.Handled = true;
                }
            };
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
        foreach (var picker in _inputs.OfType<DatePicker>())
        {
            CenterDateText(picker);
        }

        Loaded += (_, _) => _inputs.FirstOrDefault()?.Focus();
    }

    public Dictionary<string, string> Values { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The date sits mid-height like the text boxes around it: the picker's
    /// template stretches its inner text box, so once the template exists that
    /// box is centred itself.
    /// </summary>
    private static void CenterDateText(DatePicker picker)
    {
        picker.ApplyTemplate();
        if (picker.Template?.FindName("PART_TextBox", picker) is System.Windows.Controls.Primitives.DatePickerTextBox text)
        {
            text.VerticalContentAlignment = VerticalAlignment.Center;
        }
    }

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
