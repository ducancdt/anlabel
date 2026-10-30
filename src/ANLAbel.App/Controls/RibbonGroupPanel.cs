using System.Windows;
using System.Windows.Controls;

namespace ANLAbel.App.Controls;

/// <summary>
/// One ribbon group (NiceLabel 10 layout): the commands stack from the top
/// and the last child — the group caption — sits on the bottom edge, so the
/// captions of every group in a tab line up whatever the group's content.
/// </summary>
public sealed class RibbonGroupPanel : Panel
{
    public static readonly DependencyProperty GroupHeightProperty =
        DependencyProperty.Register(nameof(GroupHeight), typeof(double), typeof(RibbonGroupPanel),
            new FrameworkPropertyMetadata(84.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public double GroupHeight
    {
        get => (double)GetValue(GroupHeightProperty);
        set => SetValue(GroupHeightProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = 0.0;
        var contentHeight = 0.0;
        for (var index = 0; index < InternalChildren.Count; index++)
        {
            var child = InternalChildren[index];
            child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            width = Math.Max(width, child.DesiredSize.Width);
            contentHeight += child.DesiredSize.Height;
        }

        return new Size(width, Math.Max(GroupHeight, contentHeight));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var count = InternalChildren.Count;
        var top = 0.0;
        for (var index = 0; index < count; index++)
        {
            var child = InternalChildren[index];
            var height = child.DesiredSize.Height;
            if (index == count - 1 && count > 1)
            {
                // The caption: bottom edge, centred.
                child.Arrange(new Rect(0, Math.Max(top, finalSize.Height - height), finalSize.Width, height));
                continue;
            }

            child.Arrange(new Rect((finalSize.Width - child.DesiredSize.Width) / 2, top, child.DesiredSize.Width, height));
            top += height;
        }

        return finalSize;
    }
}
