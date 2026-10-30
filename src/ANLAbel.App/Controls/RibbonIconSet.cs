using System.Windows;
using System.Windows.Media;
using ANLAbel.Core.Enums;

namespace ANLAbel.App.Controls;

/// <summary>
/// The ANLAbel vector icon family (Themes/RibbonIcons.xaml) for views built in
/// code, such as the guide and the template library, and for the icon of an
/// object type. The icons are frozen, so one dictionary serves every window.
/// </summary>
public static class RibbonIconSet
{
    // LoadComponent reads the compiled dictionary without an Application, so
    // windows built in tests resolve the same icons.
    private static readonly Lazy<ResourceDictionary> Icons = new(() => (ResourceDictionary)Application.LoadComponent(
        new Uri("/ANLAbel.App;component/Themes/RibbonIcons.xaml", UriKind.Relative)));

    /// <summary>The icon <c>Icon.<paramref name="name"/></c>, e.g. <c>Get("Excel16")</c>.</summary>
    public static ImageSource Get(string name)
        => Icons.Value["Icon." + name] as ImageSource
           ?? throw new KeyNotFoundException($"Themes/RibbonIcons.xaml has no Icon.{name}.");

    /// <summary>The 16 px icon of an object type, the same artwork as its toolbox tile.</summary>
    public static ImageSource ForObjectType(ObjectType type) => Get(type switch
    {
        ObjectType.Text => "Text16",
        ObjectType.TextBox => "TextBox16",
        ObjectType.RichTextBox => "RichText16",
        ObjectType.BarcodeCode128 => "Barcode16",
        ObjectType.QRCode => "QRCode16",
        ObjectType.DataMatrix => "DataMatrix16",
        ObjectType.Image => "Picture16",
        ObjectType.Rectangle => "Rectangle16",
        ObjectType.Line => "Line16",
        ObjectType.Ellipse => "Ellipse16",
        ObjectType.Inverse => "Inverse16",
        _ => "Pointer16"
    });
}
