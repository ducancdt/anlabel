using ANLAbel.Core.Enums;

namespace ANLAbel.Core.Geometry;

/// <summary>
/// World-space semantics for an authored resize edge. This is deliberately
/// independent of WPF so the designer and a future non-WPF editor can apply
/// the same opposite-edge and rotated-edge rules.
/// </summary>
public enum TransformedBoundsEdge
{
    Left,
    Right,
    Top,
    Bottom
}

public readonly record struct ResizeFrame(
    double XMm,
    double YMm,
    double WidthMm,
    double HeightMm)
{
    public double RightMm => XMm + WidthMm;
    public double BottomMm => YMm + HeightMm;
}

public static class ResizeGeometryContract
{
    private const double DerivativeProbeMm = 0.001;

    public static TransformedBoundsEdge MapToWorldEdge(int rotation, ResizeEdge localEdge)
    {
        var normalized = TransformedBoundsContract.NormalizeRotation(rotation);
        return normalized switch
        {
            90 => localEdge switch
            {
                ResizeEdge.Left => TransformedBoundsEdge.Top,
                ResizeEdge.Right => TransformedBoundsEdge.Bottom,
                ResizeEdge.Top => TransformedBoundsEdge.Right,
                _ => TransformedBoundsEdge.Left
            },
            180 => localEdge switch
            {
                ResizeEdge.Left => TransformedBoundsEdge.Right,
                ResizeEdge.Right => TransformedBoundsEdge.Left,
                ResizeEdge.Top => TransformedBoundsEdge.Bottom,
                _ => TransformedBoundsEdge.Top
            },
            270 => localEdge switch
            {
                ResizeEdge.Left => TransformedBoundsEdge.Bottom,
                ResizeEdge.Right => TransformedBoundsEdge.Top,
                ResizeEdge.Top => TransformedBoundsEdge.Left,
                _ => TransformedBoundsEdge.Right
            },
            _ => localEdge switch
            {
                ResizeEdge.Left => TransformedBoundsEdge.Left,
                ResizeEdge.Right => TransformedBoundsEdge.Right,
                ResizeEdge.Top => TransformedBoundsEdge.Top,
                _ => TransformedBoundsEdge.Bottom
            }
        };
    }

    public static double GetWorldEdgePosition(
        ResizeFrame frame,
        int rotation,
        ResizeEdge localEdge)
    {
        var bounds = TransformedBoundsContract.GetBounds(
            frame.XMm,
            frame.YMm,
            frame.WidthMm,
            frame.HeightMm,
            rotation);
        return GetEdgePosition(bounds, MapToWorldEdge(rotation, localEdge));
    }

    public static ResizeFrame ApplyWorldEdgeSnap(
        ResizeFrame frame,
        int rotation,
        ResizeEdge localEdge,
        double targetWorldPositionMm,
        double minimumSizeMm = 1)
    {
        if (!double.IsFinite(targetWorldPositionMm)
            || !double.IsFinite(frame.XMm)
            || !double.IsFinite(frame.YMm)
            || !double.IsFinite(frame.WidthMm)
            || !double.IsFinite(frame.HeightMm))
        {
            return frame;
        }

        var currentValue = localEdge is ResizeEdge.Left or ResizeEdge.Right
            ? frame.WidthMm
            : frame.HeightMm;
        var currentEdge = GetWorldEdgePosition(frame, rotation, localEdge);
        var probeValue = currentValue + DerivativeProbeMm;
        var probeFrame = SetDimensionPreservingOpposite(frame, localEdge, probeValue);
        var probeEdge = GetWorldEdgePosition(probeFrame, rotation, localEdge);
        var derivative = (probeEdge - currentEdge) / DerivativeProbeMm;
        if (!double.IsFinite(derivative) || Math.Abs(derivative) <= 0.000001)
        {
            return frame;
        }

        var desiredValue = currentValue + (targetWorldPositionMm - currentEdge) / derivative;
        var clampedValue = Math.Max(minimumSizeMm, desiredValue);
        return SetDimensionPreservingOpposite(frame, localEdge, clampedValue);
    }

    private static ResizeFrame SetDimensionPreservingOpposite(
        ResizeFrame frame,
        ResizeEdge edge,
        double value)
    {
        value = Math.Max(0, value);
        return edge switch
        {
            ResizeEdge.Left => frame with { XMm = frame.XMm + frame.WidthMm - value, WidthMm = value },
            ResizeEdge.Right => frame with { WidthMm = value },
            ResizeEdge.Top => frame with { YMm = frame.YMm + frame.HeightMm - value, HeightMm = value },
            _ => frame with { HeightMm = value }
        };
    }

    private static double GetEdgePosition(LabelLayoutBounds bounds, TransformedBoundsEdge edge)
    {
        return edge switch
        {
            TransformedBoundsEdge.Left => bounds.Left,
            TransformedBoundsEdge.Right => bounds.Right,
            TransformedBoundsEdge.Top => bounds.Top,
            _ => bounds.Bottom
        };
    }
}
