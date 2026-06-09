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
}
