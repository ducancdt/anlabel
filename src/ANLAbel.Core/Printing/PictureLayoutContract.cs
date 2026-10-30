using ANLAbel.Core.Enums;

namespace ANLAbel.Core.Printing;

/// <summary>
/// Where a picture is drawn in its frame, shared by the designer, preview,
/// print and the image density check so all four agree.
/// </summary>
public static class PictureLayoutContract
{
    /// <summary>Resolution assumed when an image file states none.</summary>
    public const double DefaultDpi = 96;

    /// <param name="unitsPerInch">25.4 for millimetres, 96 for WPF device-independent pixels.</param>
    public static (double X, double Y, double Width, double Height) Place(
        PictureSizing sizing,
        double frameX,
        double frameY,
        double frameWidth,
        double frameHeight,
        int pixelWidth,
        int pixelHeight,
        double dpiX,
        double dpiY,
        double unitsPerInch)
    {
        if (pixelWidth <= 0 || pixelHeight <= 0 || frameWidth <= 0 || frameHeight <= 0)
        {
            return (frameX, frameY, Math.Max(0, frameWidth), Math.Max(0, frameHeight));
        }

        switch (sizing)
        {
            case PictureSizing.Stretch:
                return (frameX, frameY, frameWidth, frameHeight);
            case PictureSizing.Original:
                var width = pixelWidth / Usable(dpiX) * unitsPerInch;
                var height = pixelHeight / Usable(dpiY) * unitsPerInch;
                return (frameX, frameY, width, height);
            default:
                var scale = Math.Min(frameWidth / pixelWidth, frameHeight / pixelHeight);
                var drawWidth = pixelWidth * scale;
                var drawHeight = pixelHeight * scale;
                return (frameX + (frameWidth - drawWidth) / 2, frameY + (frameHeight - drawHeight) / 2, drawWidth, drawHeight);
        }
    }

    /// <summary>The picture's own size in millimetres from its pixels and resolution.</summary>
    public static (double WidthMm, double HeightMm) OriginalSizeMm(int pixelWidth, int pixelHeight, double dpiX, double dpiY)
        => (pixelWidth / Usable(dpiX) * 25.4, pixelHeight / Usable(dpiY) * 25.4);

    private static double Usable(double dpi) => double.IsFinite(dpi) && dpi > 1 ? dpi : DefaultDpi;
}
