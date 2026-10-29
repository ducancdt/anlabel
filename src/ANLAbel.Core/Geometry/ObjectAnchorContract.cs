using ANLAbel.Core.Enums;

namespace ANLAbel.Core.Geometry;

/// <summary>
/// Keeps an object's anchoring point fixed while a variable-size object
/// (Text AutoFit, variable-height TextBox) grows or shrinks, as NiceLabel 10
/// does: the size changes towards the side opposite the anchor.
/// </summary>
public static class ObjectAnchorContract
{
    /// <summary>Horizontal and vertical position of the anchor in the frame (0, 0.5 or 1).</summary>
    public static (double X, double Y) Fractions(ObjectAnchorPoint anchor)
    {
        var value = (int)anchor;
        if (value is < 0 or > 8)
        {
            return (0, 0);
        }

        return (value % 3 * 0.5, value / 3 * 0.5);
    }

    /// <summary>Top-left of the resized frame that keeps the anchor where it was.</summary>
    public static (double XMm, double YMm) Resize(
        double xMm,
        double yMm,
        double oldWidthMm,
        double oldHeightMm,
        double newWidthMm,
        double newHeightMm,
        ObjectAnchorPoint anchor)
    {
        var (fx, fy) = Fractions(anchor);
        return (xMm + (oldWidthMm - newWidthMm) * fx, yMm + (oldHeightMm - newHeightMm) * fy);
    }
}
