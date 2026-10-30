using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace ANLAbel.App.Controls;

/// <summary>
/// NiceLabel 10 large split button ("Quản lý dữ liệu ▾"): the icon runs the
/// button's own command, the caption with the arrow opens <see cref="DropDown"/>.
/// Down or F4 opens the menu from the keyboard.
/// </summary>
public sealed class RibbonSplitButton : Button
{
    public const string DropDownPartName = "PART_DropDown";

    public static readonly DependencyProperty DropDownProperty =
        DependencyProperty.Register(nameof(DropDown), typeof(ContextMenu), typeof(RibbonSplitButton), new PropertyMetadata(null));

    private ButtonBase? _dropDownPart;

    public ContextMenu? DropDown
    {
        get => (ContextMenu?)GetValue(DropDownProperty);
        set => SetValue(DropDownProperty, value);
    }

    public override void OnApplyTemplate()
    {
        if (_dropDownPart is not null)
        {
            _dropDownPart.Click -= DropDownPart_Click;
        }

        base.OnApplyTemplate();
        _dropDownPart = GetTemplateChild(DropDownPartName) as ButtonBase;
        if (_dropDownPart is not null)
        {
            _dropDownPart.Click += DropDownPart_Click;
        }
    }

    public void OpenDropDown()
    {
        if (DropDown is { } menu)
        {
            Ribbon.Open(menu, this);
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key is Key.Down or Key.F4 || (e.Key == Key.System && e.SystemKey == Key.Down))
        {
            OpenDropDown();
            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }

    private void DropDownPart_Click(object sender, RoutedEventArgs e)
    {
        OpenDropDown();
        e.Handled = true;
    }
}
