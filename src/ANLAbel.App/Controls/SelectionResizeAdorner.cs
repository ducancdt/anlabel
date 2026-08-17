using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using ANLAbel.Core.Enums;

namespace ANLAbel.App.Controls;

public sealed class SelectionResizeAdorner : Adorner
{
    // Keep an accessible pointer target while drawing a smaller marker. On
    // compact labels an 8-DIP solid square can hide an entire line of text.
    private const double HandleHitSize = 10;
    private const double HandleMarkerSize = 5;
    private readonly VisualCollection _visuals;
    private readonly Thumb _topLeft;
    private readonly Thumb _top;
    private readonly Thumb _topRight;
    private readonly Thumb _right;
    private readonly Thumb _bottomRight;
    private readonly Thumb _bottom;
    private readonly Thumb _bottomLeft;
    private readonly Thumb _left;
    private readonly Func<Rect>? _boundsProvider;
    private bool _resizeActive;

    public SelectionResizeAdorner(UIElement adornedElement, Func<Rect>? boundsProvider = null)
        : base(adornedElement)
    {
        _boundsProvider = boundsProvider;
        _visuals = new VisualCollection(this);
        _topLeft = CreateThumb(Cursors.SizeNWSE);
        _top = CreateThumb(Cursors.SizeNS);
        _topRight = CreateThumb(Cursors.SizeNESW);
        _right = CreateThumb(Cursors.SizeWE);
        _bottomRight = CreateThumb(Cursors.SizeNWSE);
        _bottom = CreateThumb(Cursors.SizeNS);
        _bottomLeft = CreateThumb(Cursors.SizeNESW);
        _left = CreateThumb(Cursors.SizeWE);

        foreach (var thumb in new[] { _topLeft, _top, _topRight, _right, _bottomRight, _bottom, _bottomLeft, _left })
        {
            thumb.DragStarted += (_, _) => BeginResize();
            thumb.DragCompleted += (_, e) => CompleteResize(e.Canceled);
        }

        Add(_topLeft);
        Add(_top);
        Add(_topRight);
        Add(_right);
        Add(_bottomRight);
        Add(_bottom);
        Add(_bottomLeft);
        Add(_left);

        _topLeft.DragDelta += (_, e) => ResizeRequested?.Invoke(this, CreateDelta(ResizeHandle.TopLeft, e.HorizontalChange, e.VerticalChange, -e.HorizontalChange, -e.VerticalChange));
        _top.DragDelta += (_, e) => ResizeRequested?.Invoke(this, CreateDelta(ResizeHandle.Top, 0, e.VerticalChange, 0, -e.VerticalChange));
        _topRight.DragDelta += (_, e) => ResizeRequested?.Invoke(this, CreateDelta(ResizeHandle.TopRight, 0, e.VerticalChange, e.HorizontalChange, -e.VerticalChange));
        _right.DragDelta += (_, e) => ResizeRequested?.Invoke(this, CreateDelta(ResizeHandle.Right, 0, 0, e.HorizontalChange, 0));
        _bottomRight.DragDelta += (_, e) => ResizeRequested?.Invoke(this, CreateDelta(ResizeHandle.BottomRight, 0, 0, e.HorizontalChange, e.VerticalChange));
        _bottom.DragDelta += (_, e) => ResizeRequested?.Invoke(this, CreateDelta(ResizeHandle.Bottom, 0, 0, 0, e.VerticalChange));
        _bottomLeft.DragDelta += (_, e) => ResizeRequested?.Invoke(this, CreateDelta(ResizeHandle.BottomLeft, e.HorizontalChange, 0, -e.HorizontalChange, e.VerticalChange));
        _left.DragDelta += (_, e) => ResizeRequested?.Invoke(this, CreateDelta(ResizeHandle.Left, e.HorizontalChange, 0, -e.HorizontalChange, 0));
    }

    public event EventHandler<ResizeDelta>? ResizeRequested;
    public event EventHandler? ResizeStarted;
    public event EventHandler? ResizeCompleted;
    public event EventHandler? ResizeCanceled;

    public bool IsResizeActive => _resizeActive;

    /// <summary>
    /// Cancels the active gesture exactly once. Thumb.DragCompleted is the
    /// authority for pointer gestures because a normal mouse-up releases
    /// capture before reporting a successful completion. Treating
    /// LostMouseCapture itself as cancellation would therefore roll back
    /// every successful resize.
    /// </summary>
    public void CancelResize()
    {
        if (!_resizeActive)
        {
            return;
        }

        CompleteResize(canceled: true);
    }

    protected override int VisualChildrenCount => _visuals.Count;

    protected override Visual GetVisualChild(int index)
    {
        return _visuals[index];
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var bounds = GetAdornerBounds();
        Arrange(_topLeft, bounds.Left, bounds.Top);
        Arrange(_top, bounds.Left + bounds.Width / 2, bounds.Top);
        Arrange(_topRight, bounds.Right, bounds.Top);
        Arrange(_right, bounds.Right, bounds.Top + bounds.Height / 2);
        Arrange(_bottomRight, bounds.Right, bounds.Bottom);
        Arrange(_bottom, bounds.Left + bounds.Width / 2, bounds.Bottom);
        Arrange(_bottomLeft, bounds.Left, bounds.Bottom);
        Arrange(_left, bounds.Left, bounds.Top + bounds.Height / 2);
        return finalSize;
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        var rect = GetAdornerBounds();
        var pen = new Pen(new SolidColorBrush(Color.FromRgb(0, 122, 255)), 1.2)
        {
            DashStyle = DashStyles.Solid
        };
        drawingContext.DrawRectangle(null, pen, rect);
    }

    private Rect GetAdornerBounds()
    {
        var bounds = _boundsProvider?.Invoke() ?? new Rect(AdornedElement.RenderSize);
        return bounds.Width >= 0 && bounds.Height >= 0 && !double.IsNaN(bounds.Left) && !double.IsNaN(bounds.Top)
            ? bounds
            : Rect.Empty;
    }

    private void Add(Thumb thumb)
    {
        _visuals.Add(thumb);
    }

    private static Thumb CreateThumb(Cursor cursor)
    {
        var hitSurface = new FrameworkElementFactory(typeof(Grid));
        hitSurface.SetValue(Panel.BackgroundProperty, Brushes.Transparent);
        var marker = new FrameworkElementFactory(typeof(Border));
        marker.SetValue(FrameworkElement.WidthProperty, HandleMarkerSize);
        marker.SetValue(FrameworkElement.HeightProperty, HandleMarkerSize);
        marker.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        marker.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        marker.SetValue(Border.BackgroundProperty, Brushes.White);
        marker.SetValue(Border.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(0, 122, 255)));
        marker.SetValue(Border.BorderThicknessProperty, new Thickness(1));
        hitSurface.AppendChild(marker);

        return new Thumb
        {
            Width = HandleHitSize,
            Height = HandleHitSize,
            Cursor = cursor,
            ToolTip = "Shift: giữ tỉ lệ · Ctrl: giữ tâm · Alt: bỏ bắt điểm",
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Template = new ControlTemplate(typeof(Thumb)) { VisualTree = hitSurface }
        };
    }

    private void BeginResize()
    {
        _resizeActive = true;
        ResizeStarted?.Invoke(this, EventArgs.Empty);
    }

    private void CompleteResize(bool canceled)
    {
        if (!_resizeActive)
        {
            return;
        }

        _resizeActive = false;
        if (canceled)
        {
            ResizeCanceled?.Invoke(this, EventArgs.Empty);
        }

        ResizeCompleted?.Invoke(this, EventArgs.Empty);
    }

    private static ResizeDelta CreateDelta(
        ResizeHandle handle,
        double deltaX,
        double deltaY,
        double deltaWidth,
        double deltaHeight)
    {
        var modifiers = Keyboard.Modifiers;
        return new ResizeDelta(
            deltaX,
            deltaY,
            deltaWidth,
            deltaHeight,
            handle,
            preserveAspectRatio: (modifiers & ModifierKeys.Shift) != 0,
            resizeFromCenter: (modifiers & ModifierKeys.Control) != 0,
            disableSnapping: (modifiers & ModifierKeys.Alt) != 0);
    }

    private static void Arrange(Thumb thumb, double x, double y)
    {
        thumb.Arrange(new Rect(x - HandleHitSize / 2, y - HandleHitSize / 2, HandleHitSize, HandleHitSize));
    }
}

public sealed class ResizeDelta : EventArgs
{
    public ResizeDelta(
        double deltaX,
        double deltaY,
        double deltaWidth,
        double deltaHeight,
        ResizeHandle handle = ResizeHandle.None,
        bool preserveAspectRatio = false,
        bool resizeFromCenter = false,
        bool disableSnapping = false)
    {
        DeltaX = deltaX;
        DeltaY = deltaY;
        DeltaWidth = deltaWidth;
        DeltaHeight = deltaHeight;
        Handle = handle;
        PreserveAspectRatio = preserveAspectRatio;
        ResizeFromCenter = resizeFromCenter;
        DisableSnapping = disableSnapping;
    }

    public double DeltaX { get; }
    public double DeltaY { get; }
    public double DeltaWidth { get; }
    public double DeltaHeight { get; }
    public ResizeHandle Handle { get; }
    public bool PreserveAspectRatio { get; }
    public bool ResizeFromCenter { get; }
    public bool DisableSnapping { get; }
}
