using ANLAbel.Core.Models;

namespace ANLAbel.Project.NiceLabel10;

/// <summary>
/// Measures the natural (unwrapped) size of a free-flowing Text object so
/// the importer can place NiceLabel position texts whose stored point is an
/// anchor other than the top-left corner, or that are rotated. The
/// application supplies the same text layout the designer uses; without a
/// measurer the importer falls back to a font-metric estimate.
/// </summary>
public interface INiceLabel10TextMeasurer
{
    /// <summary>
    /// Natural width/height in millimetres of <paramref name="sampleText"/>
    /// drawn with the object's font style, or null when it cannot be measured.
    /// </summary>
    (double WidthMm, double HeightMm)? MeasureText(LabelObject item, string sampleText);

    /// <summary>
    /// Height in millimetres the HRI strip of a linear barcode takes in its
    /// frame (text line, padding and gap) in the object's HRI font, or null
    /// when it cannot be measured.
    /// </summary>
    double? MeasureBarcodeHriStripMm(LabelObject item, string sampleText) => null;
}
