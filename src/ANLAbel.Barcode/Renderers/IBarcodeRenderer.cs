using ANLAbel.Barcode.Options;

namespace ANLAbel.Barcode.Renderers;

public interface IBarcodeRenderer
{
    BarcodePixelImage RenderBarcode(string data, BarcodeType type, double widthMm, double heightMm, int dpi, BarcodeRenderOptions? options = null);
    BarcodeVectorData? RenderBarcodeVector(string data, BarcodeType type, double widthMm, double heightMm, int dpi, BarcodeRenderOptions? options = null);
    bool ValidateData(string data, BarcodeType type);
    string GetBarcodeInfo(string data, BarcodeType type);
}
