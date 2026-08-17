namespace ANLAbel.Core.Geometry;

/// <summary>
/// Converts device-independent WPF coordinates to the integer dot grid of the
/// effective printer.  Geometry remains in DIP/mm everywhere else; this seam is
/// the only place where printer resolution is introduced into barcode edges and
/// module boundaries.
/// </summary>
public static class DeviceDotQuantizer
{
    private const double DipsPerInch = 96.0;

    public static double DotSizeDip(int dpi)
    {
        ValidateDpi(dpi);
        return DipsPerInch / dpi;
    }

    public static int DipToDots(double dip, int dpi)
    {
        ValidateDpi(dpi);
        if (double.IsNaN(dip) || double.IsInfinity(dip))
        {
            throw new ArgumentOutOfRangeException(nameof(dip), dip, "Coordinate must be finite.");
        }

        var dots = Math.Round(dip / DotSizeDip(dpi), MidpointRounding.AwayFromZero);
        if (dots < int.MinValue || dots > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(dip), dip, "Coordinate is outside the supported dot range.");
        }

        return (int)dots;
    }

    public static double DotsToDip(int dots, int dpi)
    {
        ValidateDpi(dpi);
        return dots * DotSizeDip(dpi);
    }

    public static double SnapDip(double dip, int dpi)
    {
        return DotsToDip(DipToDots(dip, dpi), dpi);
    }

    /// <summary>
    /// Maps a logical barcode module boundary to a monotonic integer-dot
    /// boundary.  Rounding the boundary sequence, rather than each bar width,
    /// preserves the complete dark/light run pattern when the target width is
    /// not an exact multiple of the module count.
    /// </summary>
    public static int QuantizeModuleBoundary(int moduleIndex, int totalModules, int totalWidthDots)
    {
        if (totalModules <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalModules), totalModules, "Module count must be positive.");
        }

        if (moduleIndex < 0 || moduleIndex > totalModules)
        {
            throw new ArgumentOutOfRangeException(nameof(moduleIndex), moduleIndex, "Boundary must be within the module range.");
        }

        if (totalWidthDots < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalWidthDots), totalWidthDots, "Width in dots cannot be negative.");
        }

        var numerator = (long)moduleIndex * totalWidthDots;
        return (int)Math.Round(numerator / (double)totalModules, MidpointRounding.AwayFromZero);
    }

    private static void ValidateDpi(int dpi)
    {
        if (dpi <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dpi), dpi, "DPI must be positive.");
        }
    }
}
