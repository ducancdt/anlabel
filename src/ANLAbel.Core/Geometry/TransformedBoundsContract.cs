using ANLAbel.Core.Models;

namespace ANLAbel.Core.Geometry;

/// <summary>
/// Computes the document-space axis-aligned bounds of a rectangular object
/// after its supported cardinal rotation. Rotation is around the authored
/// frame center, matching the WPF designer's RenderTransformOrigin.
/// </summary>
public static class TransformedBoundsContract
{
    public static LabelLayoutBounds GetBounds(LabelObject item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return GetBounds(item.XMm, item.YMm, item.WidthMm, item.HeightMm, item.Rotation);
    }

    public static LabelLayoutBounds GetBounds(
        double xMm,
        double yMm,
        double widthMm,
        double heightMm,
        int rotation)
    {
        if (!double.IsFinite(xMm)
            || !double.IsFinite(yMm)
            || !double.IsFinite(widthMm)
            || !double.IsFinite(heightMm))
        {
            return new LabelLayoutBounds(double.NaN, double.NaN, double.NaN, double.NaN);
        }

        var width = Math.Max(0, widthMm);
        var height = Math.Max(0, heightMm);
        var normalized = NormalizeRotation(rotation);
        var transformedWidth = normalized is 90 or 270 ? height : width;
        var transformedHeight = normalized is 90 or 270 ? width : height;
        var centerX = xMm + width / 2;
        var centerY = yMm + height / 2;

        return new LabelLayoutBounds(
            centerX - transformedWidth / 2,
            centerY - transformedHeight / 2,
            centerX + transformedWidth / 2,
            centerY + transformedHeight / 2);
    }

    public static int NormalizeRotation(int rotation)
    {
        var normalized = ((rotation % 360) + 360) % 360;
        return normalized is 0 or 90 or 180 or 270 ? normalized : 0;
    }
}
