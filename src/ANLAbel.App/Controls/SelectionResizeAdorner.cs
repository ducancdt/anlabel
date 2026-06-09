using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace ANLAbel.App.Controls;

public sealed class SelectionResizeAdorner : Adorner
{
    private const double HandleSize = 8;
    private readonly VisualCollection _visuals;
    private readonly Thumb _topLeft;
    private readonly Thumb _top;
    private readonly Thumb _topRight;
    private readonly Thumb _right;
    private readonly Thumb _bottomRight;
    private readonly Thumb _bottom;
    private readonly Thumb _bottomLeft;
    private readonly Thumb _left;

    public SelectionResizeAdorner(UIElement adornedElement)
        : base(adornedElement)
    {
        _visuals = new VisualCollection(this);
        _topLeft = CreateThumb(Cursors.SizeNWSE);
        _top = CreateThumb(Cursors.SizeNS);
        _topRight = CreateThumb(Cursors.SizeNESW);
        _right = CreateThumb(Cursors.SizeWE);
        _bottomRight = CreateThumb(Cursors.SizeNWSE);
        _bottom = CreateThumb(Cursors.SizeNS);
        _bottomLeft = CreateThumb(Cursors.SizeNESW);
        _left = CreateThumb(Cursors.SizeWE);

        Add(_topLeft);
        Add(_top);
        Add(_topRight);
        Add(_right);
        Add(_bottomRight);
        Add(_bottom);
        Add(_bottomLeft);
        Add(_left);

        _topLeft.DragDelta += (_, e) => ResizeRequested?.Invoke(this, new ResizeDelta(e.HorizontalChange, e.VerticalChange, -e.HorizontalChange, -e.VerticalChange));
        _top.DragDelta += (_, e) => ResizeRequested?.Invoke(this, new ResizeDelta(0, e.VerticalChange, 0, -e.VerticalChange));
        _topRight.DragDelta += (_, e) => ResizeRequested?.Invoke(this, new ResizeDelta(0, e.VerticalChange, e.HorizontalChange, -e.VerticalChange));
        _right.DragDelta += (_, e) => ResizeRequested?.Invoke(this, new ResizeDelta(0, 0, e.HorizontalChange, 0));
        _bottomRight.DragDelta += (_, e) => ResizeRequested?.Invoke(this, new ResizeDelta(0, 0, e.HorizontalChange, e.VerticalChange));
        _bottom.DragDelta += (_, e) => ResizeRequested?.Invoke(this, new ResizeDelta(0, 0, 0, e.VerticalChange));
        _bottomLeft.DragDelta += (_, e) => ResizeRequested?.Invoke(this, new ResizeDelta(e.HorizontalChange, 0, -e.HorizontalChange, e.VerticalChange));
        _left.DragDelta += (_, e) => ResizeRequested?.Invoke(this, new ResizeDelta(e.HorizontalChange, 0, -e.HorizontalChange, 0));
    }

    public event EventHandler<ResizeDelta>? ResizeRequested;

    protected override int VisualChildrenCount => _visuals.Count;

    protected override Visual GetVisualChild(int index)
    {
        return _visuals[index];
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var width = AdornedElement.RenderSize.Width;
        var height = AdornedElement.RenderSize.Height;
        Arrange(_topLeft, 0, 0);
        Arrange(_top, width / 2, 0);
        Arrange(_topRight, width, 0);
        Arrange(_right, width, height / 2);
        Arrange(_bottomRight, width, height);
        Arrange(_bottom, width / 2, height);
        Arrange(_bottomLeft, 0, height);
        Arrange(_left, 0, height / 2);
        return finalSize;
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        var rect = new Rect(AdornedElement.RenderSize);
        var pen = new Pen(new SolidColorBrush(Color.FromRgb(0, 122, 255)), 1.2)
        {
            DashStyle = DashStyles.Solid
        };
        drawingContext.DrawRectangle(null, pen, rect);
    }

    private void Add(Thumb thumb)
    {
        _visuals.Add(thumb);
    }

    private static Thumb CreateThumb(Cursor cursor)
    {
        return new Thumb
        {
            Width = HandleSize,
            Height = HandleSize,
            Cursor = cursor,
            Background = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(0, 122, 255)),
            BorderThickness = new Thickness(1)
        };
    }

    private static void Arrange(Thumb thumb, double x, double y)
    {
        thumb.Arrange(new Rect(x - HandleSize / 2, y - HandleSize / 2, HandleSize, HandleSize));
    }
}

public sealed class ResizeDelta : EventArgs
{
    public ResizeDelta(double deltaX, double deltaY, double deltaWidth, double deltaHeight)
    {
        DeltaX = deltaX;
        DeltaY = deltaY;
        DeltaWidth = deltaWidth;
        DeltaHeight = deltaHeight;
    }

    public double DeltaX { get; }
    public double DeltaY { get; }
    public double DeltaWidth { get; }
    public double DeltaHeight { get; }
}
