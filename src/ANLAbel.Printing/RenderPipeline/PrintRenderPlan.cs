using ANLAbel.Core.Enums;

namespace ANLAbel.Printing.RenderPipeline;

public sealed class PrintRenderPlan
{
    public int Dpi { get; init; } = 300;
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
}
