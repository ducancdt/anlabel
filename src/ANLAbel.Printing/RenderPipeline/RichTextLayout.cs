using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;
using DataFormats = System.Windows.DataFormats;
using FontFamily = System.Windows.Media.FontFamily;
using Size = System.Windows.Size;

namespace ANLAbel.Printing.RenderPipeline;

/// <summary>
/// Lays out a Rich Text Box (NiceLabel 10) from its RTF in a frame. Print,
/// preview and the designer share this layout, so all three wrap the same
/// way; the result is clipped at the frame like a TextBox.
/// </summary>
public static class RichTextLayout
{
    public static FlowDocument CreateDocument(string? rtf, double widthDip)
    {
        var document = new FlowDocument
        {
            PagePadding = new Thickness(0),
            ColumnWidth = double.PositiveInfinity,
            PageWidth = Math.Max(1, widthDip),
            FontFamily = new FontFamily("Arial"),
            FontSize = 9 * 96.0 / 72.0,
            TextAlignment = TextAlignment.Left
        };
        if (string.IsNullOrWhiteSpace(rtf))
        {
            return document;
        }

        var range = new TextRange(document.ContentStart, document.ContentEnd);
        try
        {
            using var stream = new MemoryStream(Encoding.ASCII.GetBytes(rtf));
            range.Load(stream, DataFormats.Rtf);
        }
        catch (ArgumentException)
        {
            // Not RTF: keep the stored text readable instead of dropping it.
            range.Text = rtf;
        }

        return document;
    }

    /// <summary>A brush that paints the laid-out document at its natural size from the top-left.</summary>
    public static Brush CreateBrush(string? rtf, double widthDip, double heightDip)
    {
        widthDip = Math.Max(1, widthDip);
        heightDip = Math.Max(1, heightDip);
        var document = CreateDocument(rtf, widthDip);
        var paginator = ((IDocumentPaginatorSource)document).DocumentPaginator;
        paginator.PageSize = new Size(widthDip, Math.Max(heightDip, 10000));
        var page = paginator.GetPage(0);
        var brush = new VisualBrush(page.Visual)
        {
            Stretch = Stretch.Fill,
            ViewboxUnits = BrushMappingMode.Absolute,
            Viewbox = new Rect(0, 0, widthDip, heightDip)
        };
        return brush;
    }

    public static void Draw(DrawingContext dc, string? rtf, Rect frame)
    {
        if (string.IsNullOrWhiteSpace(rtf) || frame.Width <= 0 || frame.Height <= 0)
        {
            return;
        }

        dc.PushClip(new RectangleGeometry(frame));
        dc.DrawRectangle(CreateBrush(rtf, frame.Width, frame.Height), null, frame);
        dc.Pop();
    }

    /// <summary>Serialises an editor's document back to RTF.</summary>
    public static string ToRtf(FlowDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        var range = new TextRange(document.ContentStart, document.ContentEnd);
        using var stream = new MemoryStream();
        range.Save(stream, DataFormats.Rtf);
        return Encoding.ASCII.GetString(stream.ToArray());
    }
}
