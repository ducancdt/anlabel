using ANLAbel.Core.Enums;

namespace ANLAbel.Core.Barcode;

/// <summary>
/// Immutable geometry for a barcode symbol with an optional human-readable
/// interpretation (HRI) strip. Font shaping stays outside Core; callers pass
/// the measured HRI ink width/height and every WPF path consumes this result.
/// </summary>
public readonly record struct BarcodeHriLayout(
    bool IsEnabled,
    bool IsValid,
    BarcodeHriPlacement Placement,
    double SymbolTopMm,
    double SymbolHeightMm,
    double HriTopMm,
    double HriHeightMm,
    double GapMm,
    string? ErrorMessage)
{
    public static BarcodeHriLayout Disabled(double frameHeightMm)
        => new(
            IsEnabled: false,
            IsValid: true,
            Placement: BarcodeHriPlacement.None,
            SymbolTopMm: 0,
            SymbolHeightMm: Math.Max(0, frameHeightMm),
            HriTopMm: Math.Max(0, frameHeightMm),
            HriHeightMm: 0,
            GapMm: 0,
            ErrorMessage: null);
}

/// <summary>
/// Shared, platform-neutral HRI geometry policy. The values are deliberately
/// explicit so designer, preview, print and preflight cannot independently
/// guess how much of the authored frame belongs to the symbol.
/// </summary>
public static class BarcodeHriLayoutContract
{
    public const double GapMm = 0.5;
    public const double VerticalPaddingMm = 0.2;

    /// <summary>
    /// Legacy bool entry point: <paramref name="showHri"/> true maps to
    /// <see cref="BarcodeHriPlacement.Below"/>; false maps to
    /// <see cref="BarcodeHriPlacement.None"/>.
    /// </summary>
    public static BarcodeHriLayout Create(
        bool supportsHri,
        bool showHri,
        double frameWidthMm,
        double frameHeightMm,
        double hriTextWidthMm,
        double hriTextHeightMm,
        double hriFontSizePt)
        => Create(
            supportsHri,
            showHri ? BarcodeHriPlacement.Below : BarcodeHriPlacement.None,
            frameWidthMm,
            frameHeightMm,
            hriTextWidthMm,
            hriTextHeightMm,
            hriFontSizePt);

    public static BarcodeHriLayout Create(
        bool supportsHri,
        BarcodeHriPlacement placement,
        double frameWidthMm,
        double frameHeightMm,
        double hriTextWidthMm,
        double hriTextHeightMm,
        double hriFontSizePt)
    {
        if (!supportsHri || placement == BarcodeHriPlacement.None)
        {
            return BarcodeHriLayout.Disabled(frameHeightMm);
        }

        if (placement is not (BarcodeHriPlacement.Below or BarcodeHriPlacement.Above))
        {
            return Invalid($"Unsupported HRI placement '{placement}'.");
        }

        if (!IsFinitePositive(frameWidthMm) || !IsFinitePositive(frameHeightMm))
        {
            return Invalid("Barcode frame must have positive finite dimensions before HRI can be laid out.");
        }

        if (!IsFinitePositive(hriTextWidthMm) || !IsFinitePositive(hriTextHeightMm))
        {
            return Invalid("HRI text has no measurable width or height; choose an installed font and non-empty barcode data.");
        }

        if (!double.IsFinite(hriFontSizePt)
            || hriFontSizePt < BarcodeApplicationContract.MinimumHriFontSizePt
            || hriFontSizePt > BarcodeApplicationContract.MaximumHriFontSizePt)
        {
            return Invalid($"HRI font size must be between {BarcodeApplicationContract.MinimumHriFontSizePt:0.#} and {BarcodeApplicationContract.MaximumHriFontSizePt:0.#} pt.");
        }

        if (hriTextWidthMm > frameWidthMm + 0.001)
        {
            return Invalid($"HRI text is {hriTextWidthMm:0.##} mm wide but the barcode frame is only {frameWidthMm:0.##} mm. Increase the frame or reduce the HRI font size.");
        }

        var hriHeightMm = hriTextHeightMm + VerticalPaddingMm * 2;
        var symbolHeightMm = frameHeightMm - GapMm - hriHeightMm;
        if (symbolHeightMm <= 0.5)
        {
            return Invalid($"Barcode frame is too short for the HRI strip ({frameHeightMm:0.##} mm). Increase the height or disable HRI.");
        }

        if (placement == BarcodeHriPlacement.Below)
        {
            return new BarcodeHriLayout(
                IsEnabled: true,
                IsValid: true,
                Placement: BarcodeHriPlacement.Below,
                SymbolTopMm: 0,
                SymbolHeightMm: symbolHeightMm,
                HriTopMm: symbolHeightMm + GapMm,
                HriHeightMm: hriHeightMm,
                GapMm: GapMm,
                ErrorMessage: null);
        }

        // Above: HRI occupies the top strip; bars start below the gap.
        return new BarcodeHriLayout(
            IsEnabled: true,
            IsValid: true,
            Placement: BarcodeHriPlacement.Above,
            SymbolTopMm: hriHeightMm + GapMm,
            SymbolHeightMm: symbolHeightMm,
            HriTopMm: 0,
            HriHeightMm: hriHeightMm,
            GapMm: GapMm,
            ErrorMessage: null);
    }

    private static BarcodeHriLayout Invalid(string message)
        => new(
            IsEnabled: true,
            IsValid: false,
            Placement: BarcodeHriPlacement.None,
            SymbolTopMm: 0,
            SymbolHeightMm: 0,
            HriTopMm: 0,
            HriHeightMm: 0,
            GapMm: GapMm,
            ErrorMessage: message);

    private static bool IsFinitePositive(double value)
        => double.IsFinite(value) && value > 0;
}
