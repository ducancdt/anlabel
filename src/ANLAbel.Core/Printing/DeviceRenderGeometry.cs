using System.Globalization;
using ANLAbel.Core.Geometry;

namespace ANLAbel.Core.Printing;

/// <summary>
/// Integer device-grid geometry derived from one effective DPI pair. Physical
/// millimetres remain the authored source of truth; this value object records
/// the exact dot frame used by barcode/image adapters and output evidence.
/// </summary>
public sealed record DeviceRenderGeometry
{
    public const string ContractVersion = "device-render-geometry/v1";

    public int DpiX { get; init; }
    public int DpiY { get; init; }
    public int LabelWidthDots { get; init; }
    public int LabelHeightDots { get; init; }
    public int PrintableOriginXDots { get; init; }
    public int PrintableOriginYDots { get; init; }
    public int PrintableWidthDots { get; init; }
    public int PrintableHeightDots { get; init; }
    public bool PrintableAreaVerified { get; init; }
    public string Diagnostic { get; init; } = string.Empty;

    public bool IsValid => DpiX > 0
        && DpiY > 0
        && LabelWidthDots > 0
        && LabelHeightDots > 0
        && (string.IsNullOrWhiteSpace(Diagnostic) || Diagnostic == "printable-area-unverified")
        && (!PrintableAreaVerified
            || (PrintableWidthDots > 0 && PrintableHeightDots > 0
                && PrintableOriginXDots >= 0
                && PrintableOriginYDots >= 0));

    public string CanonicalForm()
    {
        static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
        return string.Join('|', new[]
        {
            ContractVersion,
            Number(DpiX),
            Number(DpiY),
            Number(LabelWidthDots),
            Number(LabelHeightDots),
            Number(PrintableOriginXDots),
            Number(PrintableOriginYDots),
            Number(PrintableWidthDots),
            Number(PrintableHeightDots),
            PrintableAreaVerified ? "1" : "0"
        });
    }

    public static DeviceRenderGeometry Create(
        double labelWidthMm,
        double labelHeightMm,
        int dpiX,
        int dpiY,
        double printableOriginXDip = 0,
        double printableOriginYDip = 0,
        double printableWidthDip = 0,
        double printableHeightDip = 0,
        bool printableAreaVerified = false)
    {
        if (dpiX <= 0 || dpiY <= 0)
        {
            return Invalid("dpi-invalid");
        }

        if (!IsFinitePositive(labelWidthMm) || !IsFinitePositive(labelHeightMm))
        {
            return Invalid("label-size-invalid") with { DpiX = dpiX, DpiY = dpiY };
        }

        var labelWidthDots = MmConverter.MmToPrinterDots(labelWidthMm, dpiX);
        var labelHeightDots = MmConverter.MmToPrinterDots(labelHeightMm, dpiY);
        if (labelWidthDots <= 0 || labelHeightDots <= 0)
        {
            return Invalid("label-dot-size-invalid") with
            {
                DpiX = dpiX,
                DpiY = dpiY,
                LabelWidthDots = labelWidthDots,
                LabelHeightDots = labelHeightDots
            };
        }

        if (!printableAreaVerified)
        {
            return new DeviceRenderGeometry
            {
                DpiX = dpiX,
                DpiY = dpiY,
                LabelWidthDots = labelWidthDots,
                LabelHeightDots = labelHeightDots,
                Diagnostic = "printable-area-unverified"
            };
        }

        if (!IsFinitePositive(printableWidthDip)
            || !IsFinitePositive(printableHeightDip)
            || !IsFiniteNonNegative(printableOriginXDip)
            || !IsFiniteNonNegative(printableOriginYDip))
        {
            return Invalid("printable-area-invalid") with
            {
                DpiX = dpiX,
                DpiY = dpiY,
                LabelWidthDots = labelWidthDots,
                LabelHeightDots = labelHeightDots
            };
        }

        var originX = DeviceDotQuantizer.DipToDots(printableOriginXDip, dpiX);
        var originY = DeviceDotQuantizer.DipToDots(printableOriginYDip, dpiY);
        var width = DeviceDotQuantizer.DipToDots(printableWidthDip, dpiX);
        var height = DeviceDotQuantizer.DipToDots(printableHeightDip, dpiY);
        if (originX < 0 || originY < 0 || width <= 0 || height <= 0)
        {
            return Invalid("printable-area-dot-size-invalid") with
            {
                DpiX = dpiX,
                DpiY = dpiY,
                LabelWidthDots = labelWidthDots,
                LabelHeightDots = labelHeightDots,
                PrintableOriginXDots = originX,
                PrintableOriginYDots = originY,
                PrintableWidthDots = width,
                PrintableHeightDots = height
            };
        }

        return new DeviceRenderGeometry
        {
            DpiX = dpiX,
            DpiY = dpiY,
            LabelWidthDots = labelWidthDots,
            LabelHeightDots = labelHeightDots,
            PrintableOriginXDots = originX,
            PrintableOriginYDots = originY,
            PrintableWidthDots = width,
            PrintableHeightDots = height,
            PrintableAreaVerified = true
        };
    }

    private static DeviceRenderGeometry Invalid(string diagnostic)
        => new() { Diagnostic = diagnostic };

    private static bool IsFinitePositive(double value)
        => double.IsFinite(value) && value > 0;

    private static bool IsFiniteNonNegative(double value)
        => double.IsFinite(value) && value >= 0;
}
