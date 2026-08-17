namespace ANLAbel.Printing.RenderPipeline;

/// <summary>
/// Value-only measurements shared by designer alignment, preview/print
/// rendering and preflight.  The WPF <c>FormattedText</c> object remains local
/// to its drawing thread; this record is safe to carry into diagnostics,
/// command calculations and future scene/compiler adapters.
/// </summary>
public readonly record struct TextLayoutMetrics(
    double WidthDip,
    double HeightDip,
    double InkExtentDip,
    double BaselineDip,
    double LineHeightDip,
    int LineCount,
    double ContentWidthDip,
    double VerticalOffsetDip,
    bool IsOverflowing)
{
    /// <summary>
    /// Effective point size used by the shared renderer. For ordinary text it
    /// equals the authored size; <c>ShrinkFont</c> may report a smaller value.
    /// </summary>
    public double EffectiveFontSizePt { get; init; } = double.NaN;

    /// <summary>Effective horizontal glyph scale (1 means unscaled).</summary>
    public double HorizontalScale { get; init; } = 1.0;

    /// <summary>
    /// Effective vertical glyph scale (1 means unscaled). Free <c>ObjectType.Text</c>
    /// may compress independently of <see cref="HorizontalScale"/> when the authored
    /// frame is smaller than natural ink (design WYSIWYG; distortion allowed).
    /// </summary>
    public double VerticalScale { get; init; } = 1.0;

    /// <summary>
    /// Anchor fraction used when applying <see cref="HorizontalScale"/>:
    /// 0 = left, 0.5 = center, 1 = right within the content frame.
    /// </summary>
    public double HorizontalScaleAnchorFraction { get; init; }

    /// <summary>
    /// Stable value-only identity computed from the normalized text/resource
    /// inputs and these metrics.  Empty means the caller did not have the
    /// source value available (for example a low-level WPF-only measurement).
    /// </summary>
    public string IdentityFingerprint { get; init; } = string.Empty;
}
