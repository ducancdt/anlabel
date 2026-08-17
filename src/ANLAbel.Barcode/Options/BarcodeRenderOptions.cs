namespace ANLAbel.Barcode.Options;

using ANLAbel.Core.Enums;

public sealed class BarcodeRenderOptions
{
    public int QuietZoneModules { get; init; } = 2;
    public string ErrorCorrection { get; init; } = "M";

    /// <summary>
    /// Requests GS1 encoding (including the leading FNC1) after the caller has
    /// passed the shared application-profile preflight.  The renderer still
    /// validates/normalizes the explicit (AI)value notation defensively.
    /// </summary>
    public bool IsGs1 { get; init; }
    public Code39WideNarrowRatio Code39WideNarrowRatio { get; init; } = Code39WideNarrowRatio.LegacyEngineDefault;
    public BearerBarStyle BearerBarStyle { get; init; } = BearerBarStyle.None;
    public double BearerBarThicknessMm { get; init; } = 1.0;
}
