namespace ANLAbel.Core.Barcode;

/// <summary>
/// Integer module scale that fits a native 2D matrix into an authored device
/// frame without stretching modules. At square DPI a module is S×S dots. At
/// non-square DPI the X/Y dot counts keep the same physical millimetre size.
/// Leftover dots are even quiet-zone pad, strictly smaller than one module.
/// </summary>
public readonly record struct MatrixSquareModuleLayout(
    int NativeWidth,
    int NativeHeight,
    int ModuleDotsX,
    int ModuleDotsY,
    int FittedWidth,
    int FittedHeight,
    int FrameWidth,
    int FrameHeight,
    int PadLeft,
    int PadTop)
{
    public int LeftoverX => FrameWidth - FittedWidth;
    public int LeftoverY => FrameHeight - FittedHeight;
}

public static class MatrixSquareModuleFit
{
    public static MatrixSquareModuleLayout Fit(
        int nativeWidth,
        int nativeHeight,
        int frameWidth,
        int frameHeight,
        int dpiX,
        int dpiY)
    {
        if (nativeWidth <= 0 || nativeHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(nativeWidth), "Native matrix dimensions must be positive.");
        }

        if (frameWidth <= 0 || frameHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(frameWidth), "Frame dimensions must be positive.");
        }

        if (dpiX <= 0 || dpiY <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dpiX), "DPI values must be positive.");
        }

        var maxModuleMmX = frameWidth / (double)nativeWidth / dpiX * 25.4;
        var maxModuleMmY = frameHeight / (double)nativeHeight / dpiY * 25.4;
        var moduleMm = Math.Min(maxModuleMmX, maxModuleMmY);
        var moduleDotsX = Math.Max(1, (int)Math.Floor(moduleMm / 25.4 * dpiX + 1e-9));
        var moduleDotsY = Math.Max(1, (int)Math.Floor(moduleMm / 25.4 * dpiY + 1e-9));

        while (nativeWidth * moduleDotsX > frameWidth && moduleDotsX > 1)
        {
            moduleDotsX--;
        }

        while (nativeHeight * moduleDotsY > frameHeight && moduleDotsY > 1)
        {
            moduleDotsY--;
        }

        var fittedWidth = nativeWidth * moduleDotsX;
        var fittedHeight = nativeHeight * moduleDotsY;
        return new MatrixSquareModuleLayout(
            nativeWidth,
            nativeHeight,
            moduleDotsX,
            moduleDotsY,
            fittedWidth,
            fittedHeight,
            frameWidth,
            frameHeight,
            (frameWidth - fittedWidth) / 2,
            (frameHeight - fittedHeight) / 2);
    }
}
