using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace ANLAbel.App.Controls;

/// <summary>
/// NiceLabel 10's hover overlay (DesignerLib OverlayItem, DefaultHoverOverlayTemplate and
/// EllipseHoverOverlayTemplate): the object under the pointer gets a half-transparent #0072EB frame,
/// 2 DIP wide and 4 DIP outside it, so the designer sees which object a click picks. Drawing only:
/// it takes no pointer input, or it would take the hover away from the object.
/// </summary>
public sealed class HoverFrameAdorner : Adorner
{
    private static readonly Pen FramePen = CreatePen();
    private readonly bool _ellipse;

    public HoverFrameAdorner(UIElement adornedElement, bool ellipse)
        : base(adornedElement)
    {
        _ellipse = ellipse;
        IsHitTestVisible = false;
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        var size = AdornedElement.RenderSize;
        // NiceLabel's HoverMargin -4 with a 2 DIP stroke inside it: the stroke runs from 4 to 2 DIP outside.
        var rect = new Rect(-3, -3, size.Width + 6, size.Height + 6);
        if (_ellipse)
        {
            drawingContext.DrawEllipse(null, FramePen, new Point(rect.Left + rect.Width / 2, rect.Top + rect.Height / 2), rect.Width / 2, rect.Height / 2);
        }
        else
        {
            drawingContext.DrawRectangle(null, FramePen, rect);
        }
    }

    private static Pen CreatePen()
    {
        var pen = new Pen(new SolidColorBrush(Color.FromArgb(0x80, 0x00, 0x72, 0xEB)), 2);
        pen.Freeze();
        return pen;
    }
}
