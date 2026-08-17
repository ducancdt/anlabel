namespace ANLAbel.Core.Printing;

/// <summary>
/// Describes the source-pixel density of an image placed in a physical label
/// frame.  The contract is platform-neutral; decoding and color/dither policy
/// stay in the printing layer, while this value object keeps the PPI math
/// deterministic for preview, preflight and future device plans.
/// </summary>
public readonly record struct ImageResolutionObservation(
    int PixelWidth,
    int PixelHeight,
    double EffectivePpiX,
    double EffectivePpiY)
{
    public bool IsValid => PixelWidth > 0
        && PixelHeight > 0
        && double.IsFinite(EffectivePpiX)
        && double.IsFinite(EffectivePpiY)
        && EffectivePpiX > 0
        && EffectivePpiY > 0;

    public bool MeetsDeviceGrid(int dpiX, int dpiY)
    {
        if (dpiX <= 0 || dpiY <= 0)
        {
            return false;
        }

        return IsValid
            && EffectivePpiX + 0.0001 >= dpiX
            && EffectivePpiY + 0.0001 >= dpiY;
    }
}

public static class ImageResolutionContract
{
    private const double MillimetresPerInch = 25.4;

    public static ImageResolutionObservation Observe(
        int pixelWidth,
        int pixelHeight,
        double frameWidthMm,
        double frameHeightMm)
    {
        if (pixelWidth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pixelWidth), pixelWidth, "Image width must be positive.");
        }

        if (pixelHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pixelHeight), pixelHeight, "Image height must be positive.");
        }

        if (!double.IsFinite(frameWidthMm) || frameWidthMm <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(frameWidthMm), frameWidthMm, "Image frame width must be finite and positive.");
        }

        if (!double.IsFinite(frameHeightMm) || frameHeightMm <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(frameHeightMm), frameHeightMm, "Image frame height must be finite and positive.");
        }

        var effectivePpiX = pixelWidth * MillimetresPerInch / frameWidthMm;
        var effectivePpiY = pixelHeight * MillimetresPerInch / frameHeightMm;
        return new ImageResolutionObservation(pixelWidth, pixelHeight, effectivePpiX, effectivePpiY);
    }
}
