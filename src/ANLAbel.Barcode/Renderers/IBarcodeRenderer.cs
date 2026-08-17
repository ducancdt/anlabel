using ANLAbel.Barcode.Options;

namespace ANLAbel.Barcode.Renderers;

public interface IBarcodeRenderer
{
    BarcodePixelImage RenderBarcode(string data, BarcodeType type, double widthMm, double heightMm, int dpi, BarcodeRenderOptions? options = null);
    BarcodeVectorData? RenderBarcodeVector(string data, BarcodeType type, double widthMm, double heightMm, int dpi, BarcodeRenderOptions? options = null);

    /// <summary>
    /// Returns the pure logical module (column) count for a linear symbology,
    /// independent of target frame width and print DPI scaling. Null when the
    /// type is not linear or data is invalid. Callers must not use
    /// <see cref="BarcodeVectorData.WidthModules"/> from a frame-scaled vector
    /// render as a substitute — that value is pixel columns after stretch.
    /// </summary>
    int? CountLinearModules(string data, BarcodeType type, BarcodeRenderOptions? options = null);

    bool ValidateData(string data, BarcodeType type);
    string GetBarcodeInfo(string data, BarcodeType type);
}
