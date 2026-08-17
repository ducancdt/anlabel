namespace ANLAbel.Core.Geometry;

/// <summary>
/// Converts the designer's screen-space snap budget into document-space
/// millimetres. Pointer feel is defined in DIP, while committed geometry and
/// candidate positions remain physical millimetres. Invalid zoom values fall
/// back to 100% so a transient binding value cannot disable snapping or create
/// an unbounded tolerance.
/// </summary>
public static class SnapToleranceContract
{
    public const double DefaultAcquireToleranceDip = 6.0;
    public const double DefaultReleaseToleranceDip = 10.0;
    public const double MinimumZoom = 0.25;
    public const double MaximumZoom = 4.0;

    public static double NormalizeZoom(double zoom)
    {
        return !double.IsFinite(zoom)
            ? 1.0
            : Math.Clamp(zoom, MinimumZoom, MaximumZoom);
    }

    public static double ToDocumentMm(double screenToleranceDip, double zoom)
    {
        if (!double.IsFinite(screenToleranceDip) || screenToleranceDip < 0)
        {
            return 0;
        }

        return MmConverter.DipToMm(screenToleranceDip / NormalizeZoom(zoom));
    }

    public static double AcquireToleranceMm(double zoom)
        => ToDocumentMm(DefaultAcquireToleranceDip, zoom);

    public static double ReleaseToleranceMm(double zoom)
        => ToDocumentMm(DefaultReleaseToleranceDip, zoom);
}
