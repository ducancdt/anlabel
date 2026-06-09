using System.Windows;
using System.Windows.Media;

namespace ANLAbel.App.Controls;

public sealed class VisualPreviewHost : FrameworkElement
{
    public static readonly DependencyProperty PreviewVisualProperty =
        DependencyProperty.Register(nameof(PreviewVisual), typeof(Visual), typeof(VisualPreviewHost),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnPreviewVisualChanged));

    public Visual? PreviewVisual
    {
        get => (Visual?)GetValue(PreviewVisualProperty);
        set => SetValue(PreviewVisualProperty, value);
    }

    protected override int VisualChildrenCount => PreviewVisual is null ? 0 : 1;

    protected override Visual GetVisualChild(int index)
    {
        return PreviewVisual is not null && index == 0 ? PreviewVisual : throw new ArgumentOutOfRangeException(nameof(index));
    }

    private static void OnPreviewVisualChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var host = (VisualPreviewHost)d;
        if (e.OldValue is Visual oldVisual)
        {
            host.RemoveVisualChild(oldVisual);
            host.RemoveLogicalChild(oldVisual);
        }

        if (e.NewValue is Visual newVisual)
        {
            host.AddVisualChild(newVisual);
            host.AddLogicalChild(newVisual);
        }
    }
}
