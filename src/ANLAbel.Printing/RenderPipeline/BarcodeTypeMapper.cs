using ANLAbel.Barcode.Renderers;
using ANLAbel.Core.Enums;

namespace ANLAbel.Printing.RenderPipeline;

internal static class BarcodeTypeMapper
{
    public static BarcodeType ToRendererType(BarcodeSymbology symbology)
    {
        return symbology switch
        {
            BarcodeSymbology.QRCode => BarcodeType.QRCode,
            BarcodeSymbology.DataMatrix => BarcodeType.DataMatrix,
            BarcodeSymbology.Code39 => BarcodeType.Code39,
            BarcodeSymbology.Code93 => BarcodeType.Code93,
            BarcodeSymbology.Ean13 => BarcodeType.Ean13,
            BarcodeSymbology.Ean8 => BarcodeType.Ean8,
            BarcodeSymbology.UpcA => BarcodeType.UpcA,
            BarcodeSymbology.UpcE => BarcodeType.UpcE,
            BarcodeSymbology.ITF => BarcodeType.ITF,
            BarcodeSymbology.Codabar => BarcodeType.Codabar,
            BarcodeSymbology.Pdf417 => BarcodeType.Pdf417,
            BarcodeSymbology.Aztec => BarcodeType.Aztec,
            BarcodeSymbology.MSI => BarcodeType.MSI,
            BarcodeSymbology.Plessey => BarcodeType.Plessey,
            _ => BarcodeType.Code128
        };
    }
}
