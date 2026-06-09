namespace ANLAbel.Core.Barcode;

public sealed record QrBarcodeOptions
{
    public QrSizingMode SizingMode { get; init; } = QrSizingMode.AutoSizeByData;
    public QrErrorCorrection ErrorCorrection { get; init; } = QrErrorCorrection.M;
    public int? FixedVersion { get; init; }
    public int ModuleSizePx { get; init; } = 6;
    public int QuietZoneModules { get; init; } = 4;
    public int Dpi { get; init; } = 300;
}