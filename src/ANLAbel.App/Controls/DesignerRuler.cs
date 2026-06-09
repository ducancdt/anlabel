using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ANLAbel.Core.Geometry;

namespace ANLAbel.App.Controls;

public sealed class DesignerRuler : FrameworkElement
{
    public static readonly DependencyProperty LengthMmProperty =
        DependencyProperty.Register(nameof(LengthMm), typeof(double), typeof(DesignerRuler),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ZoomProperty =
        DependencyProperty.Register(nameof(Zoom), typeof(double), typeof(DesignerRuler),
            new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty OrientationProperty =
        DependencyProperty.Register(nameof(Orientation), typeof(Orientation), typeof(DesignerRuler),
            new FrameworkPropertyMetadata(Orientation.Horizontal, FrameworkPropertyMetadataOptions.AffectsRender));

    public double LengthMm
    {
        get => (double)GetValue(LengthMmProperty);
        set => SetValue(LengthMmProperty, value);
    }

    public double Zoom
    {
        get => (double)GetValue(ZoomProperty);
        set => SetValue(ZoomProperty, value);
    }

    public Orientation Orientation
    {
        get => (Orientation)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        if (LengthMm <= 0)
        {
            return;
        }

        var lengthDip = MmConverter.MmToDip(LengthMm) * Zoom;
        var majorPen = new Pen(new SolidColorBrush(Color.FromRgb(100, 116, 139)), 1);
        var minorPen = new Pen(new SolidColorBrush(Color.FromRgb(203, 213, 225)), 1);
        var textBrush = new SolidColorBrush(Color.FromRgb(71, 85, 105));

        for (var mm = 0; mm <= LengthMm + 0.001; mm += 1)
        {
            var position = MmConverter.MmToDip(mm) * Zoom;
            var isMajor = Math.Abs(mm % 10) < 0.001;
            var isMid = Math.Abs(mm % 5) < 0.001;
            var isEnd = Math.Abs(mm - LengthMm) < 0.001;
            var tick = isMajor ? 10 : isMid ? 7 : 4;

            if (Orientation == Orientation.Horizontal)
            {
                drawingContext.DrawLine(isMajor ? majorPen : minorPen, new Point(position, ActualHeight), new Point(position, ActualHeight - tick));
                if (isMajor && !isEnd)
                {
                    DrawText(drawingContext, mm.ToString("0", CultureInfo.InvariantCulture), new Point(position + 3, 2), textBrush);
                }
            }
            else
            {
                drawingContext.DrawLine(isMajor ? majorPen : minorPen, new Point(ActualWidth, position), new Point(ActualWidth - tick, position));
                if (isMajor && !isEnd)
                {
                    DrawText(drawingContext, mm.ToString("0", CultureInfo.InvariantCulture), new Point(4, position + 2), textBrush);
                }
            }
        }

        if (Orientation == Orientation.Horizontal)
        {
            drawingContext.DrawLine(majorPen, new Point(0, ActualHeight - 1), new Point(lengthDip, ActualHeight - 1));
        }
        else
        {
            drawingContext.DrawLine(majorPen, new Point(ActualWidth - 1, 0), new Point(ActualWidth - 1, lengthDip));
        }

        DrawEndLabel(drawingContext, LengthMm, lengthDip, textBrush);
    }

    private void DrawEndLabel(DrawingContext drawingContext, double lengthMm, double lengthDip, Brush brush)
    {
        var text = lengthMm.ToString("0.##", CultureInfo.InvariantCulture);
        var formatted = CreateFormattedText(text, brush);

        if (Orientation == Orientation.Horizontal)
        {
            var x = Math.Max(0, lengthDip - formatted.WidthIncludingTrailingWhitespace - 3);
            drawingContext.DrawText(formatted, new Point(x, 2));
        }
        else
        {
            var y = Math.Max(0, lengthDip - formatted.Height - 2);
            drawingContext.DrawText(formatted, new Point(4, y));
        }
    }

    private void DrawText(DrawingContext drawingContext, string text, Point origin, Brush brush)
    {
        drawingContext.DrawText(CreateFormattedText(text, brush), origin);
    }

    private FormattedText CreateFormattedText(string text, Brush brush)
    {
        return new FormattedText(
            text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI"),
            12,
            brush,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);
    }
}