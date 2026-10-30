using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace ANLAbel.App.Controls;

/// <summary>
/// Gives every focusable control without an accessible name the name a
/// sighted operator reads: the label just before it, the text inside it, or
/// the first sentence of its tooltip. Screen readers otherwise announce an
/// unnamed "edit" or "button" (UI audit 2026-10-30).
/// </summary>
public static class AccessibleNames
{
    public static void Apply(DependencyObject root)
    {
        foreach (var child in LogicalTreeHelper.GetChildren(root))
        {
            if (child is not DependencyObject node)
            {
                continue;
            }

            if (node is Control control && NeedsName(control))
            {
                var name = LabelBefore(control) ?? InnerText(control) ?? TooltipSentence(control);
                if (!string.IsNullOrWhiteSpace(name))
                {
                    AutomationProperties.SetName(control, name.Trim());
                }
            }

            if (node is Expander expander)
            {
                // The header toggle lives in the template: name it once the template exists.
                expander.Loaded += NameExpanderHeader;
            }

            Apply(node);
        }
    }

    private static void NameExpanderHeader(object sender, RoutedEventArgs e)
    {
        var expander = (Expander)sender;
        // A collapsed section has not built its template yet.
        expander.ApplyTemplate();
        if (expander.Template?.FindName("HeaderSite", expander) is Control header
            && string.IsNullOrWhiteSpace(AutomationProperties.GetName(header)))
        {
            var texts = new List<string>();
            if (expander.Header is DependencyObject content)
            {
                Collect(content, texts);
            }
            else if (expander.Header is string title)
            {
                texts.Add(title);
            }

            if (texts.Count > 0)
            {
                AutomationProperties.SetName(header, texts[0]);
            }
        }
    }

    private static bool NeedsName(Control control)
        => control is TextBoxBase or ComboBox or ButtonBase or Slider
           && string.IsNullOrWhiteSpace(AutomationProperties.GetName(control))
           && control.ReadLocalValue(AutomationProperties.NameProperty) == DependencyProperty.UnsetValue
           && BindingOperations.GetBindingExpressionBase(control, AutomationProperties.NameProperty) is null
           && control is not ButtonBase { Content: string };

    /// <summary>The TextBlock that labels the control: just before it in a panel, or in the same Grid cell column to its left / row above.</summary>
    private static string? LabelBefore(Control control)
    {
        if (LogicalTreeHelper.GetParent(control) is not Panel panel)
        {
            return null;
        }

        var index = panel.Children.IndexOf(control);
        if (panel is Grid)
        {
            var row = Grid.GetRow(control);
            var column = Grid.GetColumn(control);
            foreach (UIElement sibling in panel.Children)
            {
                if (sibling is TextBlock { Text.Length: > 0 } label
                    && ((Grid.GetRow(label) == row && Grid.GetColumn(label) == column - 1)
                        || (Grid.GetColumn(label) == column && Grid.GetRow(label) == row - 1)))
                {
                    return label.Text;
                }
            }
        }

        if (index > 0 && panel.Children[index - 1] is TextBlock { Text.Length: > 0 } previous)
        {
            return previous.Text;
        }

        // A label above a small Grid of inputs: "Màu" then [colour box][swatch].
        if (panel is Grid && LogicalTreeHelper.GetParent(panel) is Panel outer)
        {
            var gridIndex = outer.Children.IndexOf(panel);
            if (gridIndex > 0 && outer.Children[gridIndex - 1] is TextBlock { Text.Length: > 0 } above)
            {
                return above.Text;
            }
        }

        return null;
    }

    private static string? InnerText(Control control)
    {
        if (control is not ContentControl { Content: DependencyObject content })
        {
            return null;
        }

        var texts = new List<string>();
        Collect(content, texts);
        return texts.Count == 0 ? null : string.Join(" ", texts.Take(2));
    }

    private static void Collect(DependencyObject node, List<string> texts)
    {
        if (node is TextBlock { Text.Length: > 0 } text && text.Text.Any(char.IsLetter))
        {
            texts.Add(text.Text);
        }

        foreach (var child in LogicalTreeHelper.GetChildren(node))
        {
            if (child is DependencyObject next)
            {
                Collect(next, texts);
            }
        }
    }

    private static string? TooltipSentence(Control control)
    {
        if (control.ToolTip is not string tip || tip.Length == 0)
        {
            return null;
        }

        var end = tip.IndexOfAny(['.', ';', ':']);
        var sentence = end > 0 ? tip[..end] : tip;
        return sentence.Length > 60 ? sentence[..60] : sentence;
    }
}
