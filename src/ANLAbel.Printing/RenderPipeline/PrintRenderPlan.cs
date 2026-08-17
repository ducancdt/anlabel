using ANLAbel.Core.Enums;
using ANLAbel.Core.Printing;
using ANLAbel.Core.Scene;

namespace ANLAbel.Printing.RenderPipeline;

public sealed class PrintRenderPlan
{
    public int Dpi { get; init; } = 300;
    public int DpiX { get; init; }
    public int DpiY { get; init; }
    public DeviceRenderGeometry DeviceGeometry { get; init; } = new();
    public double LabelWidthMm { get; init; }
    public double LabelHeightMm { get; init; }
    public double OffsetXMm { get; init; }
    public double OffsetYMm { get; init; }
    public double ScaleX { get; init; } = 1;
    public double ScaleY { get; init; } = 1;
    public double GapMm { get; init; }
    public bool Rotated180 { get; init; }
    public LabelMediaType MediaType { get; init; } = LabelMediaType.Gap;
    public FeedDirection FeedDirection { get; init; } = FeedDirection.TopToBottom;
    public double MarginMm { get; init; }
    public double PrintableOriginXDip { get; init; }
    public double PrintableOriginYDip { get; init; }
    public double PrintableWidthDip { get; init; }
    public double PrintableHeightDip { get; init; }
    public bool PrintableAreaVerified { get; init; }
    /// <summary>
    /// Hash of the immutable document snapshot used to create this plan.  It is
    /// independent of the printer ticket and lets preview/print logs prove which
    /// saved design state was rendered.
    /// </summary>
    public string DocumentHash { get; init; } = string.Empty;
    /// <summary>
    /// Aggregate identity of requested text font/style/fallback resources in
    /// the immutable scene. Empty means the design has no text-capable objects.
    /// </summary>
    public string TextResourceFingerprint { get; init; } = string.Empty;
    /// <summary>
    /// Aggregate embedded-image payload/dimension/raster-policy identity used
    /// by preview, preflight and the print manifest.
    /// </summary>
    public string ImageRasterFingerprint { get; init; } = string.Empty;
    /// <summary>
    /// Hash of the deterministically compiled scene geometry.  Empty means the
    /// scene compiler rejected the snapshot and the plan must not be dispatched.
    /// </summary>
    public string SceneHash { get; init; } = string.Empty;
    public bool SceneCompilationVerified { get; init; }
    public string SceneDiagnostics { get; init; } = string.Empty;
    /// <summary>
    /// Immutable compiled scene captured at plan creation.  Keeping it on the
    /// plan prevents a mutable authoring model from drifting between preview,
    /// preflight and paginator callbacks.
    /// </summary>
    public SceneCompilationResult? CompiledScene { get; init; }
    /// <summary>
    /// Fingerprint of the validated effective output contract. Empty for a
    /// design-time preview that has not yet been reconciled with a print queue.
    /// </summary>
    public string OutputContractHash { get; init; } = string.Empty;
    /// <summary>
    /// True only when the validated PrintTicket could be serialized and included
    /// in the contract fingerprint. A non-empty plan hash alone is not proof of
    /// ticket evidence because a driver may deny XML serialization.
    /// </summary>
    public bool OutputContractTicketVerified { get; init; }
    /// <summary>
    /// Optional thermal-driver golden binding. Null means no driver/firmware/
    /// media/calibration golden has been approved for this plan; it must not be
    /// interpreted as physical verification.
    /// </summary>
    public ThermalRasterGoldenBinding? ThermalRasterGolden { get; init; }

    /// <summary>
    /// Full effective output contract captured when the plan was bound to a
    /// queue.  Last-mile revalidation compares this object field-by-field so
    /// DPI/media/ticket/imageable drift is named instead of only a hash.
    /// </summary>
    public EffectiveOutputContract? EffectiveOutput { get; init; }

    public PrintRenderPlan WithOutputContractHash(string outputContractHash, bool outputContractTicketVerified = false)
        => Clone(
            outputContractHash: outputContractHash ?? string.Empty,
            outputContractTicketVerified: outputContractTicketVerified,
            thermalRasterGolden: ThermalRasterGolden,
            effectiveOutput: EffectiveOutput);

    public PrintRenderPlan WithEffectiveOutput(EffectiveOutputContract contract)
    {
        ArgumentNullException.ThrowIfNull(contract);
        return Clone(
            outputContractHash: contract.Fingerprint,
            outputContractTicketVerified: contract.IsTicketValidated,
            thermalRasterGolden: ThermalRasterGolden,
            effectiveOutput: contract);
    }

    public PrintRenderPlan WithThermalRasterGolden(ThermalRasterGoldenBinding? thermalRasterGolden)
        => Clone(
            outputContractHash: OutputContractHash,
            outputContractTicketVerified: OutputContractTicketVerified,
            thermalRasterGolden: thermalRasterGolden,
            effectiveOutput: EffectiveOutput);

    private PrintRenderPlan Clone(
        string outputContractHash,
        bool outputContractTicketVerified,
        ThermalRasterGoldenBinding? thermalRasterGolden,
        EffectiveOutputContract? effectiveOutput)
    {
        return new PrintRenderPlan
        {
            Dpi = Dpi,
            DpiX = DpiX,
            DpiY = DpiY,
            DeviceGeometry = DeviceGeometry,
            LabelWidthMm = LabelWidthMm,
            LabelHeightMm = LabelHeightMm,
            OffsetXMm = OffsetXMm,
            OffsetYMm = OffsetYMm,
            ScaleX = ScaleX,
            ScaleY = ScaleY,
            GapMm = GapMm,
            Rotated180 = Rotated180,
            MediaType = MediaType,
            FeedDirection = FeedDirection,
            MarginMm = MarginMm,
            PrintableOriginXDip = PrintableOriginXDip,
            PrintableOriginYDip = PrintableOriginYDip,
            PrintableWidthDip = PrintableWidthDip,
            PrintableHeightDip = PrintableHeightDip,
            PrintableAreaVerified = PrintableAreaVerified,
            DocumentHash = DocumentHash,
            TextResourceFingerprint = TextResourceFingerprint,
            ImageRasterFingerprint = ImageRasterFingerprint,
            SceneHash = SceneHash,
            SceneCompilationVerified = SceneCompilationVerified,
            SceneDiagnostics = SceneDiagnostics,
            CompiledScene = CompiledScene,
            OutputContractHash = outputContractHash,
            OutputContractTicketVerified = outputContractTicketVerified,
            ThermalRasterGolden = thermalRasterGolden,
            EffectiveOutput = effectiveOutput
        };
    }
}
