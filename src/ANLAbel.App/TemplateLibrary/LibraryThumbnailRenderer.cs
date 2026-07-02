using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ANLAbel.Core.Enums;
using ANLAbel.Core.Models;

namespace ANLAbel.App.TemplateLibrary;

/// <summary>
/// Renders a lightweight, accurate-enough thumbnail of a label template for the
/// gallery. Text/lines/rectangles are drawn faithfully; barcodes and QR codes
/// are drawn as representative glyphs (stripes / checker) — fast and dependency-free.
/// </summary>
public static partial class LibraryThumbnailRenderer
{
    private static readonly Brush Ink = new SolidColorBrush(Color.FromRgb(0x11, 0x18, 0x27));
    private static readonly Brush Paper = Brushes.White;
    private static readonly Brush Edge = new SolidColorBrush(Color.FromRgb(0xCB, 0xD5, 0xE1));
    private static readonly Brush GreekInk = new SolidColorBrush(Color.FromArgb(90, 0x11, 0x18, 0x27));

    static LibraryThumbnailRenderer()
    {
        Ink.Freeze(); Edge.Freeze(); GreekInk.Freeze();
    }

    public static ImageSource Render(LabelTemplate t, double boxW, double boxH)
    {
        // scale mm -> device pixels so the whole label fits inside the box with padding
        const double pad = 8;
        var scale = Math.Min((boxW - pad * 2) / t.WidthMm, (boxH - pad * 2) / t.HeightMm);
        if (double.IsNaN(scale) || scale <= 0) scale = 1;
        double w = t.WidthMm * scale, h = t.HeightMm * scale;
        double ox = (boxW - w) / 2, oy = (boxH - h) / 2;

        var sample = BuildSample(t);
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, boxW, boxH));
            dc.PushTransform(new TranslateTransform(ox, oy));
            dc.PushClip(new RectangleGeometry(new Rect(0, 0, w, h)));
            dc.DrawRectangle(Paper, new Pen(Edge, 1), new Rect(0, 0, w, h));

            foreach (var o in t.Objects.OrderBy(o => o.ZIndex))
            {
                double x = o.XMm * scale, y = o.YMm * scale, ow = o.WidthMm * scale, oh = o.HeightMm * scale;
                var value = Resolve(o, sample);
                switch (o.Type)
                {
                    case ObjectType.Rectangle:
                        dc.DrawRectangle(null, new Pen(Ink, 0.8), new Rect(x, y, ow, oh));
                        break;
                    case ObjectType.Ellipse:
                        dc.DrawEllipse(null, new Pen(Ink, 0.8), new Point(x + ow / 2, y + oh / 2), ow / 2, oh / 2);
                        break;
                    case ObjectType.Line:
                        dc.DrawLine(new Pen(Ink, 0.8), new Point(x, y),
                            new Point(o.LineEndXMm * scale, o.LineEndYMm * scale));
                        break;
                    case ObjectType.BarcodeCode128:
                        DrawBars(dc, new Rect(x, y, ow, oh), value, o.ShowBarcodeText, scale);
                        break;
                    case ObjectType.QRCode:
                    case ObjectType.DataMatrix:
                        DrawMatrix(dc, new Rect(x, y, ow, oh), value);
                        break;
                    default:
                        DrawText(dc, value, x, y, ow, o.Style, scale);
                        break;
                }
            }
            dc.Pop(); // clip
            dc.Pop(); // transform
        }

        var bmp = new RenderTargetBitmap((int)Math.Ceiling(boxW), (int)Math.Ceiling(boxH), 96, 96, PixelFormats.Pbgra32);
        bmp.Render(visual);
        bmp.Freeze();
        return bmp;
    }

    private static void DrawText(DrawingContext dc, string value, double x, double y, double maxW, ObjectStyle st, double scale)
    {
        if (string.IsNullOrEmpty(value)) return;
        var rawPx = st.FontSizePt / 72.0 * 96.0 * scale * 0.94;

        // Scale-proportional cap: large labels (low scale) get smaller text to avoid overcrowding.
        // scale * 4 ≈ pixel height of a "4 mm row" at this thumbnail scale.
        var scaledCap = Math.Min(11.0, scale * 4.0);
        var px = Math.Max(3.0, Math.Min(rawPx, scaledCap));

        if (px < 5.0)
        {
            // Text too small to render legibly at this density — draw a thin bar placeholder.
            var barH = Math.Max(0.8, px * 0.45);
            var barW = Math.Min(maxW, Math.Max(3.0, value.Length * px * 0.42));
            dc.DrawRectangle(GreekInk, null, new Rect(x, y + px * 0.2, barW, barH));
            return;
        }

        var typeface = new Typeface(new FontFamily(string.IsNullOrWhiteSpace(st.FontFamily) ? "Arial" : st.FontFamily),
            st.Italic ? FontStyles.Italic : FontStyles.Normal,
            st.Bold ? FontWeights.Bold : FontWeights.Normal, FontStretches.Normal);
        var ft = new FormattedText(value, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            typeface, px, Ink, 1.0)
        {
            MaxTextWidth = Math.Max(2, maxW),
            MaxLineCount = 1,
            Trimming = TextTrimming.CharacterEllipsis
        };
        dc.DrawText(ft, new Point(x, y));
    }

    private static void DrawBars(DrawingContext dc, Rect r, string value, bool showText, double scale)
    {
        var rng = new Random(value.GetHashCode());
        double barsH = showText ? r.Height * 0.72 : r.Height;
        double bx = r.X;
        while (bx < r.Right - 1)
        {
            double bw = rng.Next(0, 4) switch { 0 => 0.7, 1 => 0.7, 2 => 1.3, _ => 2.0 };
            if (rng.NextDouble() > 0.4)
                dc.DrawRectangle(Ink, null, new Rect(bx, r.Y, bw, barsH));
            bx += bw + 0.8;
        }
        if (showText && !string.IsNullOrEmpty(value))
        {
            var ft = new FormattedText(value, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                new Typeface("Arial"), Math.Max(5, r.Height * 0.16), Ink, 1.0)
            { MaxTextWidth = r.Width, MaxLineCount = 1, Trimming = TextTrimming.CharacterEllipsis };
            dc.DrawText(ft, new Point(r.X, r.Y + barsH + 0.5));
        }
    }

    private static void DrawMatrix(DrawingContext dc, Rect r, string value)
    {
        int n = 13;
        double cw = r.Width / n, ch = r.Height / n;
        var rng = new Random(string.IsNullOrEmpty(value) ? 7 : value.GetHashCode());
        bool Finder(int rr, int cc) => rr == 0 || rr == 6 || cc == 0 || cc == 6 || (rr >= 2 && rr <= 4 && cc >= 2 && cc <= 4);
        for (int rr = 0; rr < n; rr++)
        {
            for (int cc = 0; cc < n; cc++)
            {
                bool on;
                if (rr < 7 && cc < 7) on = Finder(rr, cc);
                else if (rr < 7 && cc >= n - 7) on = Finder(rr, cc - (n - 7));
                else if (rr >= n - 7 && cc < 7) on = Finder(rr - (n - 7), cc);
                else on = rng.NextDouble() > 0.5;
                if (on)
                    dc.DrawRectangle(Ink, null, new Rect(r.X + cc * cw, r.Y + rr * ch, cw + 0.4, ch + 0.4));
            }
        }
    }

    private static Dictionary<string, string> BuildSample(LabelTemplate t)
    {
        var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var f in t.DatabaseConfig.AvailableFields)
        {
            if (!string.IsNullOrWhiteSpace(f.Name))
                d[f.Name] = f.SampleValue ?? string.Empty;
        }
        return d;
    }

    private static string Resolve(LabelObject o, Dictionary<string, string> sample)
    {
        var expr = o.BindingExpression;
        if (string.IsNullOrWhiteSpace(expr)) return o.Text ?? string.Empty;
        // Thumbnail only: show field name as placeholder, not sample data
        return FieldToken().Replace(expr, m => m.Groups[1].Value);
    }

    [GeneratedRegex(@"\{([^}]+)\}")]
    private static partial Regex FieldToken();
}
