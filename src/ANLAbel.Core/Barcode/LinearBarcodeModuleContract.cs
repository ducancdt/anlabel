namespace ANLAbel.Core.Barcode;

/// <summary>
/// Industrial policy for 1D barcode module (X-dimension) width: authored
/// physical millimetres quantized to whole printer dots at the effective print
/// DPI. Thermal scanners need enough physical dots per module; sub-dot stretch
/// is an industrial risk and is reported rather than silently accepted.
/// </summary>
public static class LinearBarcodeModuleContract
{
    /// <summary>~2 printer dots — same floor used for fixed matrix module preflight.</summary>
    public const int MinimumModuleDots = 2;

    /// <summary>
    /// Practical linear X-dimension floor (~7.5 mil). Below this, even multi-dot
    /// modules can be hard to scan on worn thermal media; preflight may warn.
    /// </summary>
    public const double MinimumIndustrialXDimensionMm = 0.19;

    /// <summary>
    /// Common industrial default (~13 mil) for new objects when the operator has
    /// not set an explicit module. Zero on the model still means "legacy: derive
    /// from frame width / module count".
    /// </summary>
    public const double RecommendedDefaultXDimensionMm = 0.33;

    public const double MillimetersPerInch = 25.4;

    /// <summary>
    /// Quantizes an authored module width (mm) to whole dots at <paramref name="dpi"/>
    /// and reconstructs the effective printed module width.
    /// </summary>
    public static LinearBarcodeModuleResolution Resolve(double moduleWidthMm, int dpi)
    {
        if (!double.IsFinite(moduleWidthMm) || moduleWidthMm <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(moduleWidthMm), moduleWidthMm, "Module width must be finite and positive.");
        }

        if (dpi <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dpi), dpi, "DPI must be positive.");
        }

        var rawDots = moduleWidthMm / MillimetersPerInch * dpi;
        var moduleDots = Math.Max(1, (int)Math.Round(rawDots, MidpointRounding.AwayFromZero));
        var effectiveMm = moduleDots * MillimetersPerInch / dpi;
        var belowDots = moduleDots < MinimumModuleDots;
        var belowFloor = effectiveMm + 1e-9 < MinimumIndustrialXDimensionMm;
        return new LinearBarcodeModuleResolution(
            AuthoredModuleWidthMm: moduleWidthMm,
            Dpi: dpi,
            ModuleDots: moduleDots,
            EffectiveModuleWidthMm: effectiveMm,
            IsBelowMinimumDots: belowDots,
            IsBelowIndustrialFloorMm: belowFloor);
    }

    /// <summary>
    /// When the operator has not authored an X-dimension, estimate module width
    /// from the object frame and the encoded module count (including quiet-zone
    /// columns that the engine reports as modules).
    /// </summary>
    public static double EstimateModuleWidthMmFromFrame(double frameWidthMm, int totalModules)
    {
        if (!double.IsFinite(frameWidthMm) || frameWidthMm <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(frameWidthMm), frameWidthMm, "Frame width must be finite and positive.");
        }

        if (totalModules <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalModules), totalModules, "Module count must be positive.");
        }

        return frameWidthMm / totalModules;
    }

    /// <summary>
    /// Resolves production module geometry from either the authored X-dimension
    /// (preferred) or a frame-derived estimate when the authored value is unset (≤ 0).
    /// </summary>
    public static LinearBarcodeModuleResolution ResolveForObject(
        double authoredModuleWidthMm,
        double frameWidthMm,
        int totalModules,
        int dpi)
    {
        var moduleMm = authoredModuleWidthMm > 0
            ? authoredModuleWidthMm
            : EstimateModuleWidthMmFromFrame(frameWidthMm, totalModules);
        return Resolve(moduleMm, dpi);
    }

    public static string FormatIndustrialRiskMessage(LinearBarcodeModuleResolution resolution)
    {
        if (resolution.IsBelowMinimumDots)
        {
            return $"Linear module is only {resolution.ModuleDots} printer dot(s) " +
                   $"(~{resolution.EffectiveModuleWidthMm:0.###} mm) at {resolution.Dpi} DPI — " +
                   "likely to fail scanning. Increase X-dimension (mm) or print DPI, or widen the barcode.";
        }

        if (resolution.IsBelowIndustrialFloorMm)
        {
            return $"Linear module is ~{resolution.EffectiveModuleWidthMm:0.###} mm " +
                   $"({resolution.ModuleDots} dots at {resolution.Dpi} DPI), below the industrial floor " +
                   $"of {MinimumIndustrialXDimensionMm:0.##} mm (~7.5 mil). Increase X-dimension for reliable scanning.";
        }

        return string.Empty;
    }

    /// <summary>
    /// True when the object should use X×modules for production width (P1.a).
    /// Requires an explicit SizedFromX mode and a positive authored X-dimension.
    /// </summary>
    public static bool UsesSizedFromX(Enums.BarcodeWidthMode widthMode, double authoredModuleWidthMm)
        => widthMode == Enums.BarcodeWidthMode.SizedFromX && authoredModuleWidthMm > 0;

    /// <summary>
    /// Production symbol width for size-from-X: effective quantized module mm × pure logical modules.
    /// </summary>
    public static double SizedFromXWidthMm(double authoredModuleWidthMm, int logicalModules, int dpi)
    {
        if (logicalModules <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(logicalModules), logicalModules, "Logical module count must be positive.");
        }

        var resolution = Resolve(authoredModuleWidthMm, dpi);
        return resolution.EffectiveModuleWidthMm * logicalModules;
    }

    /// <summary>
    /// One printer-dot width in millimetres — tolerance for size-from-X width equality.
    /// </summary>
    public static double OnePrinterDotMm(int dpi)
    {
        if (dpi <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dpi), dpi, "DPI must be positive.");
        }

        return MillimetersPerInch / dpi;
    }
}

/// <summary>Result of quantizing a linear barcode module to the print device grid.</summary>
public readonly record struct LinearBarcodeModuleResolution(
    double AuthoredModuleWidthMm,
    int Dpi,
    int ModuleDots,
    double EffectiveModuleWidthMm,
    bool IsBelowMinimumDots,
    bool IsBelowIndustrialFloorMm)
{
    public bool HasIndustrialRisk => IsBelowMinimumDots || IsBelowIndustrialFloorMm;
}
