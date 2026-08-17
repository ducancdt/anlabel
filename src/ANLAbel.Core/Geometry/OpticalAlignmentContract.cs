namespace ANLAbel.Core.Geometry;

/// <summary>
/// Which document axis receives an optical alignment translation.
/// </summary>
public enum OpticalAlignmentAxis
{
    Horizontal,
    Vertical,
    Both
}

/// <summary>
/// Which visible-ink anchor is aligned on the selected axis.
/// Leading/trailing map to left/right on X and top/bottom on Y.
/// </summary>
public enum OpticalAlignmentAnchor
{
    Leading,
    Center,
    Trailing
}

/// <summary>
/// Platform-neutral bounds of the visible glyph/stroke ink. Layout/frame
/// bounds are intentionally not accepted here: optical alignment is an
/// explicit command and must never be confused with ordinary frame alignment.
/// </summary>
public readonly record struct OpticalBounds(
    double Left,
    double Top,
    double Right,
    double Bottom)
{
    public const double MinimumExtentMm = 0.0001;

    public double Width => Right - Left;
    public double Height => Bottom - Top;
    public double CenterX => (Left + Right) / 2;
    public double CenterY => (Top + Bottom) / 2;

    public bool IsFinite =>
        double.IsFinite(Left)
        && double.IsFinite(Top)
        && double.IsFinite(Right)
        && double.IsFinite(Bottom);

    public bool HasInk => IsFinite && Width > MinimumExtentMm && Height > MinimumExtentMm;
}

/// <summary>
/// Result of one optical translation. The delta is in the same document unit
/// as the supplied bounds (ANLAbel uses millimetres).
/// </summary>
public readonly record struct OpticalAlignmentResult(
    bool Succeeded,
    double DeltaX,
    double DeltaY,
    string? ErrorMessage)
{
    public static OpticalAlignmentResult Success(double deltaX, double deltaY) =>
        new(true, deltaX, deltaY, null);

    public static OpticalAlignmentResult Failure(string message) =>
        new(false, 0, 0, message);
}

/// <summary>
/// Deterministic optical alignment math. The caller supplies ink bounds from
/// its font/raster backend; this contract remains free of WPF and therefore
/// can be reused by preview, print and a future platform-neutral text engine.
/// </summary>
public static class OpticalAlignmentContract
{
    public static OpticalAlignmentResult Align(
        OpticalBounds source,
        OpticalBounds target,
        OpticalAlignmentAxis axis,
        OpticalAlignmentAnchor anchor = OpticalAlignmentAnchor.Center)
    {
        if (!Enum.IsDefined(axis) || !Enum.IsDefined(anchor))
        {
            return OpticalAlignmentResult.Failure("Optical alignment received an unknown axis or anchor.");
        }

        if (!source.HasInk || !target.HasInk)
        {
            return OpticalAlignmentResult.Failure("Both text objects must expose finite visible ink bounds.");
        }

        var deltaX = axis is OpticalAlignmentAxis.Vertical
            ? 0
            : Anchor(target, horizontal: true, anchor) - Anchor(source, horizontal: true, anchor);
        var deltaY = axis is OpticalAlignmentAxis.Horizontal
            ? 0
            : Anchor(target, horizontal: false, anchor) - Anchor(source, horizontal: false, anchor);

        if (!double.IsFinite(deltaX) || !double.IsFinite(deltaY))
        {
            return OpticalAlignmentResult.Failure("Optical alignment produced a non-finite translation.");
        }

        return OpticalAlignmentResult.Success(deltaX, deltaY);
    }

    private static double Anchor(OpticalBounds bounds, bool horizontal, OpticalAlignmentAnchor anchor)
    {
        return anchor switch
        {
            OpticalAlignmentAnchor.Leading => horizontal ? bounds.Left : bounds.Top,
            OpticalAlignmentAnchor.Trailing => horizontal ? bounds.Right : bounds.Bottom,
            _ => horizontal ? bounds.CenterX : bounds.CenterY
        };
    }
}
