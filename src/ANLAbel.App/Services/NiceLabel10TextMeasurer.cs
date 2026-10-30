using System.Windows.Media;
using ANLAbel.Core.Geometry;
using ANLAbel.Core.Models;
using ANLAbel.Printing.RenderPipeline;
using ANLAbel.Project.NiceLabel10;

namespace ANLAbel.App.Services;

/// <summary>
/// Measures a free Text object with the same FormattedText layout the
/// designer and print renderer use, so the NiceLabel 10 importer can place
/// anchored or rotated position texts where NiceLabel drew them.
/// </summary>
public sealed class NiceLabel10TextMeasurer : INiceLabel10TextMeasurer
{
    public (double WidthMm, double HeightMm)? MeasureText(LabelObject item, string sampleText)
    {
        ArgumentNullException.ThrowIfNull(item);
        try
        {
            var formatted = TextBoxOverflowDetector.CreateFormattedText(item, sampleText ?? string.Empty, Brushes.Black, 1.0);
            return (MmConverter.DipToMm(formatted.WidthIncludingTrailingWhitespace), MmConverter.DipToMm(formatted.Height));
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return null;
        }
    }

    public double? MeasureBarcodeHriStripMm(LabelObject item, string sampleText)
    {
        ArgumentNullException.ThrowIfNull(item);
        // Laid out as the print lays it out, in a frame large enough to hold it.
        var layout = BarcodeHriTextLayout.Measure(
            ANLAbel.Barcode.Renderers.BarcodeType.Code128,
            string.IsNullOrEmpty(sampleText) ? "0" : sampleText,
            1000,
            1000,
            item.BarcodeHriPlacement,
            item.BarcodeTextFontSizePt,
            item.BarcodeHriFontFamily,
            item.BarcodeHriBold);
        return layout.IsEnabled && layout.IsValid ? layout.HriHeightMm + layout.GapMm : null;
    }
}
