using ANLAbel.Core.Enums;
using ANLAbel.Core.Models;
using ANLAbel.Core.Scene;

namespace ANLAbel.Core.Geometry;

/// <summary>
/// Shared document-space bounds for line objects. A visible stroke is centered
/// on its endpoints, so its safety hull extends by half the physical stroke
/// width. Designer geometry and print preflight must use the same hull.
/// </summary>
public static class LineBoundsContract
{
    public static LabelLayoutBounds GetBounds(LabelObject item)
    {
        ArgumentNullException.ThrowIfNull(item);
        var endX = item.LineEndXMm == 0 && item.LineEndYMm == 0
            ? item.XMm + item.WidthMm
            : item.LineEndXMm;
        var endY = item.LineEndXMm == 0 && item.LineEndYMm == 0
            ? item.YMm + item.HeightMm
            : item.LineEndYMm;
        return GetBounds(item.XMm, item.YMm, endX, endY, item.Style.OutlineStyle, item.Style.BorderThicknessMm);
    }

    public static LabelLayoutBounds GetBounds(SceneObjectSnapshot item)
    {
        ArgumentNullException.ThrowIfNull(item);
        var endX = item.LineEndXMm == 0 && item.LineEndYMm == 0
            ? item.XMm + item.WidthMm
            : item.LineEndXMm;
        var endY = item.LineEndXMm == 0 && item.LineEndYMm == 0
            ? item.YMm + item.HeightMm
            : item.LineEndYMm;
        return GetBounds(item.XMm, item.YMm, endX, endY, item.Style.OutlineStyle, item.Style.BorderThicknessMm);
    }

    public static LabelLayoutBounds GetBounds(
        double startX,
        double startY,
        double endX,
        double endY,
        OutlineStyle outlineStyle,
        double strokeWidthMm)
    {
        if (!double.IsFinite(startX)
            || !double.IsFinite(startY)
            || !double.IsFinite(endX)
            || !double.IsFinite(endY))
        {
            return new LabelLayoutBounds(double.NaN, double.NaN, double.NaN, double.NaN);
        }

        var padding = outlineStyle == OutlineStyle.None
            ? 0
            : Math.Max(0, double.IsFinite(strokeWidthMm) ? strokeWidthMm : 0) / 2;
        return new LabelLayoutBounds(
            Math.Min(startX, endX) - padding,
            Math.Min(startY, endY) - padding,
            Math.Max(startX, endX) + padding,
            Math.Max(startY, endY) + padding);
    }
}
