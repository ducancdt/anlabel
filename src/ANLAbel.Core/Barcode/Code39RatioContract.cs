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

    public const double MaximumRealizedRatio = 3.0;

    /// <summary>
    /// Lowest wide:narrow ratio the printed symbol may realize. 2.0:1 is kept
    /// only when the operator chose it (preflight owns its X floor); every
    /// other choice stays at or above 2.2:1, which is legal at any X.
    /// </summary>
    public static double MinimumRealizedRatio(double nominalRatio)
        => nominalRatio <= 2.0 + 1e-9 ? 2.0 : 2.2;

    /// <summary>
    /// Whole printer dots for a wide element when narrow elements use
    /// <paramref name="narrowDots"/>: the integer nearest to N × ratio inside the
    /// legal realized band, so every wide element prints with the same width
    /// and the printed ratio never leaves the USS-39 range after dot rounding.
    /// </summary>
    public static int ResolveWideDots(int narrowDots, double nominalRatio)
    {
        if (narrowDots <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(narrowDots));
        }

        if (!double.IsFinite(nominalRatio) || nominalRatio <= 1)
        {
            throw new ArgumentOutOfRangeException(nameof(nominalRatio));
        }

        var lowest = (int)Math.Ceiling(narrowDots * MinimumRealizedRatio(nominalRatio) - 1e-9);
        var highest = (int)Math.Floor(narrowDots * MaximumRealizedRatio + 1e-9);
        var nearest = (int)Math.Round(narrowDots * nominalRatio, MidpointRounding.AwayFromZero);
        return Math.Clamp(nearest, lowest, highest);
    }

    /// <summary>
    /// Symbol width in X units (both quiet zones included) for size-from-X and
    /// frame-derived X estimates, rounded up so the frame never undersizes the
    /// printed symbol.
    /// </summary>
    public static int LogicalModules(int narrowElements, int wideElements, int quietZoneModules, double nominalRatio)
        => (int)Math.Ceiling(
            (2 * Math.Max(0, quietZoneModules)) + narrowElements + (wideElements * nominalRatio) - 1e-9);
}
