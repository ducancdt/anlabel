namespace ANLAbel.Barcode.Options;

public sealed class BarcodeRenderOptions
{
    public int QuietZoneModules { get; init; } = 2;
    public string ErrorCorrection { get; init; } = "M";
}
