using ANLAbel.Core.Enums;

namespace ANLAbel.Core.Barcode;

/// <summary>
/// P4 Code 39 policy derived from the approved USS-39 boundary. It owns legal
/// ratio checks and per-side physical quiet-zone measurements; callers must
/// use a resolved, whole-dot X value rather than the authored value.
/// </summary>
public static class Code39RatioContract
{
    public const double Ratio2MinimumXmm = 0.508;
    public const double MinimumQuietZoneMmPerSide = 2.54;

    public static bool IsSupported(Code39WideNarrowRatio ratio) => ratio is
        Code39WideNarrowRatio.LegacyEngineDefault or
        Code39WideNarrowRatio.Ratio2_0 or
        Code39WideNarrowRatio.Ratio2_2 or
        Code39WideNarrowRatio.Ratio2_5 or
        Code39WideNarrowRatio.Ratio3_0;

    public static double? ToValue(Code39WideNarrowRatio ratio) => ratio switch
    {
        Code39WideNarrowRatio.Ratio2_0 => 2.0,
        Code39WideNarrowRatio.Ratio2_2 => 2.2,
        Code39WideNarrowRatio.Ratio2_5 => 2.5,
        Code39WideNarrowRatio.Ratio3_0 => 3.0,
        _ => null
    };

    public static bool IsLegal(Code39WideNarrowRatio ratio, double effectiveXmm)
    {
        if (!IsSupported(ratio) || !double.IsFinite(effectiveXmm) || effectiveXmm <= 0)
        {
            return false;
        }

        return ratio != Code39WideNarrowRatio.Ratio2_0 || effectiveXmm + 1e-9 >= Ratio2MinimumXmm;
    }

    public static double RequiredQuietZoneMmPerSide(double effectiveXmm)
    {
        if (!double.IsFinite(effectiveXmm) || effectiveXmm <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(effectiveXmm));
        }

        return Math.Max(10 * effectiveXmm, MinimumQuietZoneMmPerSide);
    }

    public static double ObservedQuietZoneMmPerSide(int quietZoneModules, LinearBarcodeModuleResolution x)
        => Math.Max(0, quietZoneModules) * x.EffectiveModuleWidthMm;
}
