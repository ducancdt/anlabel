using System.Globalization;
using System.Windows;
using System.Windows.Media;
using ANLAbel.Barcode.Renderers;
using ANLAbel.Core.Barcode;
using ANLAbel.Core.Enums;
using ANLAbel.Core.Geometry;
using ANLAbel.Core.Text;
using WpfBrush = System.Windows.Media.Brush;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfFlowDirection = System.Windows.FlowDirection;

namespace ANLAbel.Printing.RenderPipeline;

/// <summary>
/// WPF measurement adapter for the Core HRI geometry contract.  It is shared by
/// the designer canvas, preview/print presenter and preflight; only the
/// thread-affine FormattedText instance stays local to the caller.
/// </summary>
public static class BarcodeHriTextLayout
{
    public static bool Supports(BarcodeType type)
        => type is BarcodeType.Code128
            or BarcodeType.Code39
            or BarcodeType.Code93
            or BarcodeType.Ean13
            or BarcodeType.Ean8
            or BarcodeType.UpcA
            or BarcodeType.UpcE
            or BarcodeType.ITF
            or BarcodeType.Codabar
            or BarcodeType.MSI
            or BarcodeType.Plessey;

    /// <summary>
    /// Legacy bool entry: true → Below, false → None.
    /// </summary>
    public static BarcodeHriLayout Measure(
        BarcodeType type,
        string value,
        double frameWidthMm,
        double frameHeightMm,
        bool showHri,
        double hriFontSizePt)
        => Measure(
            type,
            value,
            frameWidthMm,
            frameHeightMm,
            showHri ? BarcodeHriPlacement.Below : BarcodeHriPlacement.None,
            hriFontSizePt);

    public static BarcodeHriLayout Measure(
        BarcodeType type,
        string value,
        double frameWidthMm,
        double frameHeightMm,
        BarcodeHriPlacement placement,
        double hriFontSizePt)
    {
        if (!Supports(type) || placement == BarcodeHriPlacement.None)
        {
            return BarcodeHriLayout.Disabled(frameHeightMm);
        }

        var text = CreateText(value, hriFontSizePt, WpfBrushes.Black);
        return BarcodeHriLayoutContract.Create(
            supportsHri: true,
            placement,
            frameWidthMm,
            frameHeightMm,
            MmConverter.DipToMm(text.WidthIncludingTrailingWhitespace),
            MmConverter.DipToMm(text.Height),
            hriFontSizePt);
    }

    public static FormattedText CreateText(string value, double hriFontSizePt, WpfBrush brush)
    {
        var text = new FormattedText(
            value,
            CultureInfo.CurrentCulture,
            WpfFlowDirection.LeftToRight,
            new Typeface("Segoe UI"),
            Math.Max(1, hriFontSizePt) * TextLayoutContract.DipPerPoint,
            brush,
            1.0);
        return text;
    }
}
