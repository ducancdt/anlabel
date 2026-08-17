using ANLAbel.Barcode.Options;

namespace ANLAbel.Barcode.Renderers;

/// <summary>
/// Optional renderer capability for a device whose effective horizontal and
/// vertical DPI differ. The legacy <see cref="IBarcodeRenderer"/> contract
/// accepts one DPI; this seam prevents the print adapter from pretending that
/// a rectangular device grid is square.
/// </summary>
public interface INonSquareBarcodeRenderer
{
    BarcodePixelImage RenderBarcode(
        string data,
        BarcodeType type,
        double widthMm,
        double heightMm,
        int dpiX,
        int dpiY,
        BarcodeRenderOptions? options = null);
}
