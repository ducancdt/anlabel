using System.Windows.Media;

namespace ANLAbel.Printing.RenderPipeline;

/// <summary>
/// Explicit line layout used when a label requests a custom line height. The
/// contained <see cref="FormattedText"/> instances are thread-affine and must
/// be consumed on the drawing thread that created this result. Value metrics are
/// copied into <see cref="TextLayoutMetrics"/> for diagnostics and alignment.
/// </summary>
public sealed class TextLayoutResult
{
    public required IReadOnlyList<FormattedText> Lines { get; init; }
    public required TextLayoutMetrics Metrics { get; init; }
}
