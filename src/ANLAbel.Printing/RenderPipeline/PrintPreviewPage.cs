using System.Windows.Media;
using ANLAbel.Core.Printing;

namespace ANLAbel.Printing.RenderPipeline;

public sealed class PrintPreviewPage
{
    public int PageNumber { get; init; }
    public Visual Visual { get; init; } = null!;
    public double WidthDip { get; init; }
    public double HeightDip { get; init; }
    public string DocumentHash { get; init; } = string.Empty;
    public string TextResourceFingerprint { get; init; } = string.Empty;
    public string ImageRasterFingerprint { get; init; } = string.Empty;
    public string SceneHash { get; init; } = string.Empty;
    public bool SceneCompilationVerified { get; init; }
    /// <summary>
    /// Effective printer-output identity used when this page was rendered.
    /// Empty means the page came from a design-only preview with no verified
    /// queue/driver ticket yet.
    /// </summary>
    public string OutputContractHash { get; init; } = string.Empty;
    public bool OutputContractTicketVerified { get; init; }
    public ThermalRasterGoldenBinding? ThermalRasterGolden { get; init; }
    public int DpiX { get; init; }
    public int DpiY { get; init; }
    public DeviceRenderGeometry DeviceGeometry { get; init; } = new();
    public bool PrintableAreaVerified { get; init; }
}
