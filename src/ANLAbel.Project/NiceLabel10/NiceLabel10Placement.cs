namespace ANLAbel.Project.NiceLabel10;

/// <summary>
/// Converts NiceLabel 10 anchor-based geometry into ANLAbel's frame-based
/// geometry.
/// </summary>
/// <remarks>
/// <para>
/// NiceLabel stores one point per object (<c>X</c>/<c>Y</c> or
/// <c>Left</c>/<c>Top</c>) plus an <c>AnchoringPoint</c> and a clockwise
/// <c>Rotation</c> in degrees. The runtime geometry in <c>Core.dll</c>
/// (decompiled; see <c>docs/NICELABEL_10_REVERSE_ENGINEERING.md</c> §8)
/// treats the stored point as the anchor's position on the axis-aligned
/// bounding box of the <em>rotated</em> object: with width multiplier
/// <c>wm</c> (Left 0, Center 0.5, Right 1) and height multiplier <c>hm</c>
/// (Top 0, Middle 0.5, Bottom 1), the anchor sits at
/// <c>(W·wm, H·hm)</c> for 0°, <c>(H·(1−hm), W·wm)</c> for 90°,
/// <c>(W·(1−wm), H·(1−hm))</c> for 180° and <c>(H·hm, W·(1−wm))</c> for
/// 270° from the bounding box's top-left corner, where W×H is the
/// unrotated object size.
/// </para>
/// <para>
/// ANLAbel stores the unrotated frame and rotates it about its own
/// centre, so the frame is placed with its centre on the centre of that
/// bounding box.
/// </para>
/// </remarks>
public static class NiceLabel10Placement
{
    /// <summary>
    /// Resolves ANLAbel's unrotated frame origin and rotation for an object
    /// whose unrotated size is known.
    /// </summary>
    /// <param name="anchorXMm">Stored X/Left.</param>
    /// <param name="anchorYMm">Stored Y/Top.</param>
    /// <param name="widthMm">Unrotated object width.</param>
    /// <param name="heightMm">Unrotated object height.</param>
    /// <param name="anchoringPoint">Vendor <c>AnchoringPointEnum</c> value.</param>
    /// <param name="rotationDegrees">Stored clockwise rotation.</param>
    public static (double LeftMm, double TopMm, int Rotation) ResolveFrame(
        double anchorXMm,
        double anchorYMm,
        double widthMm,
        double heightMm,
        int anchoringPoint,
        double rotationDegrees)
    {
        var rotation = NormalizeRotation(rotationDegrees);
        var (wm, hm) = AnchorMultipliers(anchoringPoint);
        var w = Math.Max(0, widthMm);
        var h = Math.Max(0, heightMm);

        // Anchor offset inside the rotated bounding box and that box's size.
        double offsetX, offsetY, boxWidth, boxHeight;
        switch (rotation)
        {
            case 90:
                boxWidth = h;
                boxHeight = w;
                offsetX = h * (1 - hm);
                offsetY = w * wm;
                break;
            case 180:
                boxWidth = w;
                boxHeight = h;
                offsetX = w * (1 - wm);
                offsetY = h * (1 - hm);
                break;
            case 270:
                boxWidth = h;
                boxHeight = w;
                offsetX = h * hm;
                offsetY = w * (1 - wm);
                break;
            default:
                boxWidth = w;
                boxHeight = h;
                offsetX = w * wm;
                offsetY = h * hm;
                break;
        }

        var boxLeft = anchorXMm - offsetX;
        var boxTop = anchorYMm - offsetY;
        var centerX = boxLeft + boxWidth / 2;
        var centerY = boxTop + boxHeight / 2;
        return (centerX - w / 2, centerY - h / 2, rotation);
    }

    /// <summary>
    /// Width/height multipliers of a vendor anchoring point. LeftTop (0) and
    /// None (9) both mean the stored point is the top-left corner.
    /// </summary>
    public static (double Width, double Height) AnchorMultipliers(int anchoringPoint)
    {
        if (anchoringPoint is < 0 or > 8)
        {
            return (0, 0);
        }

        var column = anchoringPoint % 3;
        var row = anchoringPoint / 3;
        return (column * 0.5, row * 0.5);
    }

    /// <summary>
    /// Maps a stored rotation onto ANLAbel's 0/90/180/270 contract. The
    /// vendor <c>Rotations</c> enum offers exactly those four, so any other
    /// value is treated as unrotated rather than approximated.
    /// </summary>
    public static int NormalizeRotation(double rotationDegrees)
    {
        if (!double.IsFinite(rotationDegrees))
        {
            return 0;
        }

        var rounded = (int)Math.Round(rotationDegrees);
        var normalized = ((rounded % 360) + 360) % 360;
        return normalized is 90 or 180 or 270 ? normalized : 0;
    }
}
