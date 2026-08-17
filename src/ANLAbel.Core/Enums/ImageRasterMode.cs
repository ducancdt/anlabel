namespace ANLAbel.Core.Enums;

/// <summary>
/// Explicit policy for turning an embedded image into the bitmap consumed by
/// the designer and the print presenter.  <see cref="DriverManaged"/> keeps
/// the original colour/alpha payload and leaves monochrome conversion to the
/// printer driver; the two monochrome modes are deterministic application
/// transforms that can be proven in preview and print.
/// </summary>
public enum ImageRasterMode
{
    DriverManaged,
    MonochromeThreshold,
    MonochromeOrderedDither
}
