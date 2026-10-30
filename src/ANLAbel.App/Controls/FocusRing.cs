using System.Windows;
using System.Windows.Controls;

namespace ANLAbel.App.Controls;

/// <summary>
/// Keyboard focus ring for the whole app: the solid blue "AppFocusRing" style
/// instead of the Windows theme's thin black dotted ring, which disappears on
/// light buttons. The theme sets its ring through style setters, so the ring is
/// given as a local value; a control that turns its ring off (null) keeps it off.
/// </summary>
public static class FocusRing
{
    public const string ResourceKey = "AppFocusRing";

    public static void Apply(DependencyObject root, Style ring)
    {
        foreach (var child in LogicalTreeHelper.GetChildren(root))
        {
            if (child is not DependencyObject node)
            {
                continue;
            }

            if (node is Control control && control.FocusVisualStyle is not null)
            {
                control.FocusVisualStyle = ring;
            }

            Apply(node, ring);
        }
    }
}
