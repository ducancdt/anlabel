using ANLAbel.Core.Enums;

namespace ANLAbel.Core.Geometry;

public static class LabelGeometry
{
    public static LabelOrientation ResolveOrientation(double widthMm, double heightMm)
    {
        return widthMm >= heightMm ? LabelOrientation.Landscape : LabelOrientation.Portrait;
    }

    public static (double WidthMm, double HeightMm) OrientSize(double widthMm, double heightMm, LabelOrientation orientation)
    {
        var shortSide = Math.Min(widthMm, heightMm);
        var longSide = Math.Max(widthMm, heightMm);
        return orientation == LabelOrientation.Landscape
            ? (longSide, shortSide)
            : (shortSide, longSide);
    }
}
