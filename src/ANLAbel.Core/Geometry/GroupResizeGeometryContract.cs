namespace ANLAbel.Core.Geometry;

/// <summary>
/// Pure document-space geometry for resizing a multi-selection.  The canvas
/// sends the drag delta in millimetres; this contract keeps the opposite edge
/// fixed, enforces a minimum frame, and returns a deterministic transform for
/// each selected object's display-space bounds.
/// </summary>
public readonly record struct GroupResizeFrame(
    double XMm,
    double YMm,
    double WidthMm,
    double HeightMm)
{
    public double RightMm => XMm + WidthMm;
    public double BottomMm => YMm + HeightMm;
}

public readonly record struct GroupResizeTransform(
    GroupResizeFrame Source,
    GroupResizeFrame Target)
{
    public double ScaleX => Source.WidthMm <= 0 ? 1 : Target.WidthMm / Source.WidthMm;
    public double ScaleY => Source.HeightMm <= 0 ? 1 : Target.HeightMm / Source.HeightMm;

    public double MapX(double xMm) => Target.XMm + (xMm - Source.XMm) * ScaleX;
    public double MapY(double yMm) => Target.YMm + (yMm - Source.YMm) * ScaleY;
}

public static class GroupResizeGeometryContract
{
    public static LabelLayoutBounds MapBounds(
        GroupResizeTransform transform,
        LabelLayoutBounds sourceBounds)
    {
        var left = transform.MapX(sourceBounds.Left);
        var top = transform.MapY(sourceBounds.Top);
        var right = transform.MapX(sourceBounds.Right);
        var bottom = transform.MapY(sourceBounds.Bottom);
        return new LabelLayoutBounds(left, top, right, bottom);
    }

    public static ResizeFrame ToAuthoredFrame(
        LabelLayoutBounds displayBounds,
        int rotation,
        double minimumSizeMm = 0.5)
    {
        var displayWidth = Math.Max(minimumSizeMm, displayBounds.Width);
        var displayHeight = Math.Max(minimumSizeMm, displayBounds.Height);
        var normalizedRotation = TransformedBoundsContract.NormalizeRotation(rotation);
        var authoredWidth = normalizedRotation is 90 or 270 ? displayHeight : displayWidth;
        var authoredHeight = normalizedRotation is 90 or 270 ? displayWidth : displayHeight;
        return new ResizeFrame(
            displayBounds.CenterX - authoredWidth / 2,
            displayBounds.CenterY - authoredHeight / 2,
            authoredWidth,
            authoredHeight);
    }

    public static bool TryResize(
        GroupResizeFrame source,
        double deltaXMm,
        double deltaYMm,
        double deltaWidthMm,
        double deltaHeightMm,
        double minimumWidthMm,
        double minimumHeightMm,
        out GroupResizeFrame target)
    {
        target = default;
        if (!IsValid(source)
            || !double.IsFinite(deltaXMm)
            || !double.IsFinite(deltaYMm)
            || !double.IsFinite(deltaWidthMm)
            || !double.IsFinite(deltaHeightMm))
        {
            return false;
        }

        var minimumWidth = Math.Max(0.001, double.IsFinite(minimumWidthMm) ? minimumWidthMm : 0.001);
        var minimumHeight = Math.Max(0.001, double.IsFinite(minimumHeightMm) ? minimumHeightMm : 0.001);
        var movesLeft = Math.Abs(deltaXMm) > 0.000001 && Math.Abs(deltaWidthMm) > 0.000001;
        var movesTop = Math.Abs(deltaYMm) > 0.000001 && Math.Abs(deltaHeightMm) > 0.000001;

        var left = source.XMm + deltaXMm;
        var right = source.RightMm + deltaXMm + deltaWidthMm;
        var top = source.YMm + deltaYMm;
        var bottom = source.BottomMm + deltaYMm + deltaHeightMm;

        if (right - left < minimumWidth)
        {
            if (movesLeft)
            {
                left = right - minimumWidth;
            }
            else
            {
                right = left + minimumWidth;
            }
        }

        if (bottom - top < minimumHeight)
        {
            if (movesTop)
            {
                top = bottom - minimumHeight;
            }
            else
            {
                bottom = top + minimumHeight;
            }
        }

        target = new GroupResizeFrame(left, top, right - left, bottom - top);
        return IsValid(target);
    }

    /// <summary>
    /// Clamps the moving group to the artboard without changing the anchored
    /// opposite edge unless the requested frame is larger than the artboard.
    /// </summary>
    public static GroupResizeFrame ClampToCanvas(
        GroupResizeFrame frame,
        double canvasWidthMm,
        double canvasHeightMm,
        double minimumWidthMm = 1,
        double minimumHeightMm = 1)
    {
        if (!IsValid(frame) || !double.IsFinite(canvasWidthMm) || !double.IsFinite(canvasHeightMm))
        {
            return frame;
        }

        var canvasWidth = Math.Max(0, canvasWidthMm);
        var canvasHeight = Math.Max(0, canvasHeightMm);
        var left = frame.XMm;
        var top = frame.YMm;
        var right = frame.RightMm;
        var bottom = frame.BottomMm;

        if (frame.WidthMm >= canvasWidth)
        {
            left = 0;
            right = canvasWidth;
        }
        else
        {
            if (left < 0)
            {
                right -= left;
                left = 0;
            }

            if (right > canvasWidth)
            {
                left -= right - canvasWidth;
                right = canvasWidth;
            }

            left = Math.Max(0, left);
            right = Math.Min(canvasWidth, right);
        }

        if (frame.HeightMm >= canvasHeight)
        {
            top = 0;
            bottom = canvasHeight;
        }
        else
        {
            if (top < 0)
            {
                bottom -= top;
                top = 0;
            }

            if (bottom > canvasHeight)
            {
                top -= bottom - canvasHeight;
                bottom = canvasHeight;
            }

            top = Math.Max(0, top);
            bottom = Math.Min(canvasHeight, bottom);
        }

        var width = Math.Max(Math.Max(0.001, minimumWidthMm), right - left);
        var height = Math.Max(Math.Max(0.001, minimumHeightMm), bottom - top);
        if (left + width > canvasWidth)
        {
            left = Math.Max(0, canvasWidth - width);
        }

        if (top + height > canvasHeight)
        {
            top = Math.Max(0, canvasHeight - height);
        }

        return new GroupResizeFrame(left, top, width, height);
    }

    public static bool IsValid(GroupResizeFrame frame) =>
        double.IsFinite(frame.XMm)
        && double.IsFinite(frame.YMm)
        && double.IsFinite(frame.WidthMm)
        && double.IsFinite(frame.HeightMm)
        && frame.WidthMm > 0
        && frame.HeightMm > 0;
}
