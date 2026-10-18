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

    /// <summary>Code 39 space between characters in narrow-bar widths (1 = standard).</summary>
    public int Code39IntercharacterGapModules { get; init; } = 1;

    public Code128CodeSet Code128CodeSet { get; init; } = Code128CodeSet.Auto;

    /// <summary>
    /// The one place a label object's barcode settings become encoder options,
    /// so designer, preflight, sizing and print always encode the same symbol.
    /// </summary>
    public static BarcodeRenderOptions FromObject(ANLAbel.Core.Models.LabelObject item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return new BarcodeRenderOptions
        {
            ErrorCorrection = item.QrErrorCorrection.ToString(),
            QuietZoneModules = Math.Max(0, item.QrQuietZoneModules),
            IsGs1 = item.BarcodeApplicationProfile == BarcodeApplicationProfile.Gs1,
            Code39WideNarrowRatio = item.Code39WideNarrowRatio,
            BearerBarStyle = item.BearerBarStyle,
            BearerBarThicknessMm = item.BearerBarThicknessMm,
            Code39IntercharacterGapModules = item.Code39IntercharacterGapModules,
            Code128CodeSet = item.Code128CodeSet
        };
    }
}
