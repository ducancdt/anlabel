namespace ANLAbel.Barcode.Renderers;

public sealed class BarcodePixelImage
{
    public BarcodePixelImage(int widthPixels, int heightPixels, byte[] bgraPixels)
    {
        WidthPixels = widthPixels;
        HeightPixels = heightPixels;
        BgraPixels = bgraPixels;
    }

    public int WidthPixels { get; }
    public int HeightPixels { get; }
    public byte[] BgraPixels { get; }
    public int Stride => WidthPixels * 4;

    /// <summary>
    /// Resamples a decoded barcode with nearest-neighbour mapping. This is a
    /// compatibility fallback for renderers that only expose one DPI; it keeps
    /// every output pixel binary and makes the target device dimensions
    /// explicit instead of letting WPF stretch the image later.
    /// </summary>
    public BarcodePixelImage ResizeNearest(int widthPixels, int heightPixels)
    {
        if (widthPixels <= 0 || heightPixels <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(widthPixels), "Barcode output dimensions must be positive.");
        }

        if (widthPixels == WidthPixels && heightPixels == HeightPixels)
        {
            return this;
        }

        var output = new byte[checked(widthPixels * heightPixels * 4)];
        for (var y = 0; y < heightPixels; y++)
        {
            var sourceY = Math.Min(HeightPixels - 1, (int)((long)y * HeightPixels / heightPixels));
            for (var x = 0; x < widthPixels; x++)
            {
                var sourceX = Math.Min(WidthPixels - 1, (int)((long)x * WidthPixels / widthPixels));
                var sourceOffset = checked((sourceY * WidthPixels + sourceX) * 4);
                var targetOffset = checked((y * widthPixels + x) * 4);
                BgraPixels.AsSpan(sourceOffset, 4).CopyTo(output.AsSpan(targetOffset, 4));
            }
        }

        return new BarcodePixelImage(widthPixels, heightPixels, output);
    }

    /// <summary>
    /// Repeats every native module into a <paramref name="moduleDotsX"/> by
    /// <paramref name="moduleDotsY"/> block. X and Y may differ only to match
    /// non-square printer DPI; they stay equal at square DPI.
    /// </summary>
    public BarcodePixelImage ScaleIntegerModules(int moduleDotsX, int moduleDotsY)
    {
        if (moduleDotsX <= 0 || moduleDotsY <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(moduleDotsX), "Module dot size must be positive.");
        }

        if (moduleDotsX == 1 && moduleDotsY == 1)
        {
            return this;
        }

        var width = checked(WidthPixels * moduleDotsX);
        var height = checked(HeightPixels * moduleDotsY);
        var output = new byte[checked(width * height * 4)];
        for (var y = 0; y < HeightPixels; y++)
        {
            for (var repeatY = 0; repeatY < moduleDotsY; repeatY++)
            {
                var destY = y * moduleDotsY + repeatY;
                for (var x = 0; x < WidthPixels; x++)
                {
                    var sourceOffset = (y * WidthPixels + x) * 4;
                    for (var repeatX = 0; repeatX < moduleDotsX; repeatX++)
                    {
                        var destOffset = (destY * width + x * moduleDotsX + repeatX) * 4;
                        BgraPixels.AsSpan(sourceOffset, 4).CopyTo(output.AsSpan(destOffset, 4));
                    }
                }
            }
        }

        return new BarcodePixelImage(width, height, output);
    }
}
