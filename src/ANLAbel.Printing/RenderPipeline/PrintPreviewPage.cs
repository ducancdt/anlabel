using System.Windows.Media;

namespace ANLAbel.Printing.RenderPipeline;

public sealed class PrintPreviewPage
{
    public int PageNumber { get; init; }
    public Visual Visual { get; init; } = null!;
    public double WidthDip { get; init; }
    public double HeightDip { get; init; }
}
