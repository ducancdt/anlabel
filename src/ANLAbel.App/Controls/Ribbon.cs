using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace ANLAbel.App.Controls;

/// <summary>
/// Attached properties of the NiceLabel 10 style ribbon (Themes/Ribbon.xaml):
/// the icon a ribbon button draws, and the drop-down menu a button opens
/// below itself instead of running a command ("Sắp xếp ▾", "Tùy chỉnh in ▾").
/// </summary>
public static class Ribbon
{
    public static readonly DependencyProperty IconProperty =
        DependencyProperty.RegisterAttached("Icon", typeof(ImageSource), typeof(Ribbon), new FrameworkPropertyMetadata(null));

    public static ImageSource? GetIcon(DependencyObject element) => (ImageSource?)element.GetValue(IconProperty);

    public static void SetIcon(DependencyObject element, ImageSource? value) => element.SetValue(IconProperty, value);

    public static readonly DependencyProperty DropDownProperty =
        DependencyProperty.RegisterAttached("DropDown", typeof(ContextMenu), typeof(Ribbon), new PropertyMetadata(null, OnDropDownChanged));

    public static ContextMenu? GetDropDown(DependencyObject element) => (ContextMenu?)element.GetValue(DropDownProperty);

    public static void SetDropDown(DependencyObject element, ContextMenu? value) => element.SetValue(DropDownProperty, value);

    /// <summary>Opens <paramref name="menu"/> under <paramref name="owner"/>, with the owner's data context for its commands.</summary>
    public static void Open(ContextMenu menu, FrameworkElement owner)
    {
        menu.PlacementTarget = owner;
        menu.Placement = PlacementMode.Bottom;
        menu.DataContext = owner.DataContext;
        menu.IsOpen = true;
    }

    private static void OnDropDownChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
    {
        if (element is not ButtonBase button)
        {
            return;
        }

        button.Click -= OpenDropDown;
        if (e.NewValue is ContextMenu)
        {
            button.Click += OpenDropDown;
        }
    }

    private static void OpenDropDown(object sender, RoutedEventArgs e)
    {
        if (sender is ButtonBase button && GetDropDown(button) is { } menu)
        {
            Open(menu, button);
            e.Handled = true;
        }
    }
}
