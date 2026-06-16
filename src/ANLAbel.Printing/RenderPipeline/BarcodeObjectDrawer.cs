using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ANLAbel.Barcode.Options;
using ANLAbel.Barcode.Renderers;
using ANLAbel.Core.Barcode;
using ANLAbel.Core.Enums;
using ANLAbel.Core.Geometry;
using ANLAbel.Core.Models;
using Brushes = System.Windows.Media.Brushes;
using Pen = System.Windows.Media.Pen;
using Typeface = System.Windows.Media.Typeface;
using FlowDirection = System.Windows.FlowDirection;

namespace ANLAbel.Printing.RenderPipeline;

/// <summary>
/// Single rendering path for barcode objects — used by both the designer canvas (via
/// <see cref="RenderToImageSource"/>) and the print renderer (via <see cref="Draw"/>).
/// Keeping one code path guarantees WYSIWYG: the designer preview is pixel-equivalent
/// to the print output.
/// </summary>
public static class BarcodeObjectDrawer
{
    /// <summary>
    /// Draws a barcode object onto an existing <see cref="DrawingContext"/>.
    /// <paramref name="rect"/> is in the caller's coordinate space (typically DIP).
    /// </summary>
    public static void Draw(
        DrawingContext dc,
        LabelObject item,
        Rect rect,
        string data,
        IBarcodeRenderer renderer,
        int dpi,
        double labelWidthMm,
        double labelHeightMm)
    {
        var type = ResolveType(item);

        if (!renderer.ValidateData(data, type))
        {
            dc.DrawRectangle(Brushes.White, new Pen(Brushes.Red, 1), rect);
            DrawErrorFrame(dc, rect, "Invalid barcode data");
            return;
        }

        if (IsSquare2DCodeLike(item))
        {
            var fitSizeMm = item.QrSizingMode == QrSizingMode.FixedVersionAndModuleSize
                ? QrAutoSizeHelper.CalculateFixedSizeMm(
                    item.QrFixedVersion, item.QrModuleSizePx, item.QrQuietZoneModules, item.QrDpi,
                    GetAvailableQrSizeMm(item, labelWidthMm, labelHeightMm))
                : QrAutoSizeHelper.CalculateRequiredSizeMm(
                    data, item.WidthMm, item.HeightMm,
                    item.QrErrorCorrection, item.QrModuleSizePx, item.QrQuietZoneModules, item.QrDpi,
                    maxSizeMm: GetAvailableQrSizeMm(item, labelWidthMm, labelHeightMm));
            if (fitSizeMm is not null)
            {
                rect = new Rect(rect.Left, rect.Top, MmConverter.MmToDip(fitSizeMm.Value), MmConverter.MmToDip(fitSizeMm.Value));
            }
        }

        try
        {
            var widthMm = MmConverter.DipToMm(rect.Width);
            var heightMm = MmConverter.DipToMm(rect.Height);
            var barcodeDpi = item.QrDpi > 0 ? item.QrDpi : dpi;
            var options = CreateOptions(item);

            // 1D barcodes: vector rendering eliminates rasterization/interpolation artifacts.
            var vectorData = renderer.RenderBarcodeVector(data, type, widthMm, heightMm, barcodeDpi, options);
            if (vectorData is not null)
            {
                // Auto-grow: expand rect if barcode needs more width than allocated.
                var requiredWidthMm = vectorData.WidthModules / (double)barcodeDpi * 25.4;
                if (requiredWidthMm > widthMm * 1.05)
                {
                    rect = new Rect(rect.Left, rect.Top, MmConverter.MmToDip(requiredWidthMm), rect.Height);
                }

                var showHri = item.ShowBarcodeText && BarcodeTextLayout.Is1DBarcode(item.Type, item.BarcodeSymbology);
                if (showHri)
                {
                    var barHeightDip = BarcodeTextLayout.BarcodeHeightDip(rect.Height, item.BarcodeTextFontSizePt, true);
                    var barcodeRect = new Rect(rect.Left, rect.Top, rect.Width, barHeightDip);
                    DrawVectorBarcode(dc, vectorData, barcodeRect);
                    DrawHriText(dc, data, rect.Left, rect.Top + BarcodeTextLayout.TextOriginY(barHeightDip), rect.Width, item);
                }
                else
                {
                    DrawVectorBarcode(dc, vectorData, rect);
                }
                return;
            }

            // 2D codes: bitmap rendering — no HRI text.
            var pixels = renderer.RenderBarcode(data, type, widthMm, heightMm, barcodeDpi, options);
            var source = BitmapSource.Create(
                pixels.WidthPixels, pixels.HeightPixels,
                barcodeDpi, barcodeDpi,
                PixelFormats.Bgra32, null,
                pixels.BgraPixels, pixels.Stride);
            source.Freeze();

            var naturalWidthDip = pixels.WidthPixels * 96.0 / barcodeDpi;
            var naturalHeightDip = pixels.HeightPixels * 96.0 / barcodeDpi;
            var guidelines = new GuidelineSet(
                new[] { rect.Left, rect.Left + naturalWidthDip },
                new[] { rect.Top, rect.Top + naturalHeightDip });
            dc.PushGuidelineSet(guidelines);
            dc.DrawImage(source, new Rect(rect.Left, rect.Top, naturalWidthDip, naturalHeightDip));
            dc.Pop();
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            dc.DrawRectangle(Brushes.White, new Pen(Brushes.Red, 1), rect);
            DrawErrorFrame(dc, rect, "Barcode cannot be rendered");
            _ = ex;
        }
    }

    /// <summary>
    /// Renders the barcode object to a <see cref="BitmapSource"/> suitable for use as
    /// a WPF <see cref="System.Windows.Controls.Image"/> source in the designer canvas.
    /// Uses the same drawing code as <see cref="Draw"/>, so the result is pixel-equivalent
    /// to the printed output.
    /// </summary>
    public static BitmapSource? RenderToImageSource(
        LabelObject item,
        double widthDip,
        double heightDip,
        string data,
        IBarcodeRenderer renderer,
        int dpi,
        double labelWidthMm,
        double labelHeightMm)
    {
        if (widthDip < 1 || heightDip < 1)
            return null;

        var visual = new DrawingVisual();
        RenderOptions.SetBitmapScalingMode(visual, BitmapScalingMode.NearestNeighbor);
        RenderOptions.SetEdgeMode(visual, EdgeMode.Aliased);
        using (var dc = visual.RenderOpen())
        {
            dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, widthDip, heightDip));
            Draw(dc, item, new Rect(0, 0, widthDip, heightDip), data, renderer, dpi, labelWidthMm, labelHeightMm);
        }

        var barcodeDpi = item.QrDpi > 0 ? item.QrDpi : dpi;
        var pixelWidth = Math.Max(1, (int)Math.Ceiling(widthDip * barcodeDpi / 96.0));
        var pixelHeight = Math.Max(1, (int)Math.Ceiling(heightDip * barcodeDpi / 96.0));
        var bitmap = new RenderTargetBitmap(pixelWidth, pixelHeight, barcodeDpi, barcodeDpi, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();
        return bitmap;
    }

    internal static BarcodeType ResolveType(LabelObject item) => item.Type switch
    {
        ObjectType.QRCode => BarcodeType.QRCode,
        ObjectType.DataMatrix => BarcodeType.DataMatrix,
        _ => BarcodeTypeMapper.ToRendererType(item.BarcodeSymbology)
    };

    private static bool IsSquare2DCodeLike(LabelObject item)
        => item.Type == ObjectType.QRCode
            || item.Type == ObjectType.DataMatrix
            || item.Type == ObjectType.BarcodeCode128
                && item.BarcodeSymbology is BarcodeSymbology.QRCode
                    or BarcodeSymbology.DataMatrix
                    or BarcodeSymbology.Aztec
                    or BarcodeSymbology.Pdf417;

    private static double GetAvailableQrSizeMm(LabelObject item, double labelWidthMm, double labelHeightMm)
    {
        var availableWidthMm = labelWidthMm - item.XMm;
        var availableHeightMm = labelHeightMm - item.YMm;
        return Math.Max(1, Math.Min(availableWidthMm, availableHeightMm));
    }

    private static BarcodeRenderOptions CreateOptions(LabelObject item) => new()
    {
        ErrorCorrection = item.QrErrorCorrection.ToString(),
        QuietZoneModules = item.QrQuietZoneModules
    };

    private static void DrawVectorBarcode(DrawingContext dc, BarcodeVectorData vectorData, Rect rect)
    {
        var totalModules = vectorData.WidthModules;
        if (totalModules <= 0)
            return;

        var snappedLeft = Math.Round(rect.Left);
        var snappedRight = Math.Round(rect.Left + rect.Width);
        var snappedTop = Math.Round(rect.Top);
        var snappedBottom = Math.Round(rect.Top + rect.Height);
        dc.PushGuidelineSet(new GuidelineSet(
            new[] { snappedLeft, snappedRight },
            new[] { snappedTop, snappedBottom }));

        var targetWidth = snappedRight - snappedLeft;
        var barHeight = snappedBottom - snappedTop;
        var i = 0;

        while (i < totalModules)
        {
            if (vectorData.RowBits[i])
            {
                var startModule = i;
                while (i < totalModules && vectorData.RowBits[i])
                    i++;

                var leftPx = Math.Round(startModule * targetWidth / totalModules);
                var rightPx = Math.Round(i * targetWidth / totalModules);
                var barWidth = Math.Max(1.0, rightPx - leftPx);
                dc.DrawRectangle(Brushes.Black, null, new Rect(snappedLeft + leftPx, snappedTop, barWidth, barHeight));
            }
            else
            {
                i++;
            }
        }

        dc.Pop();
    }

    private static void DrawHriText(DrawingContext dc, string data, double left, double textY, double width, LabelObject item)
    {
        if (string.IsNullOrWhiteSpace(data))
            return;

        var fontSizeDip = BarcodeTextLayout.FontSizeDip(item.BarcodeTextFontSizePt);
        var text = new FormattedText(
            data,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI"),
            fontSizeDip,
            Brushes.Black,
            1.0)
        {
            TextAlignment = TextAlignment.Center
        };
        var textX = left + (width - text.Width) / 2.0;
        dc.DrawText(text, new Point(textX, textY));
    }

    private static void DrawErrorFrame(DrawingContext dc, Rect rect, string message)
    {
        dc.DrawRectangle(null, new Pen(Brushes.Red, 1.4) { DashStyle = DashStyles.Dash }, rect);
        // Error badge
        const double radius = 7;
        var center = new Point(rect.Right - radius, rect.Top + radius);
        dc.DrawEllipse(Brushes.Red, null, center, radius, radius);
        var badge = new FormattedText("!", CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            new Typeface("Segoe UI"), 11, Brushes.White, 1.0) { TextAlignment = TextAlignment.Center };
        dc.DrawText(badge, new Point(center.X, center.Y - badge.Height / 2 - 0.5));
        // Message
        var msg = new FormattedText(message, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            new Typeface("Segoe UI"), 7 * 96.0 / 72.0, Brushes.Red, 1.0);
        dc.DrawText(msg, new Point(rect.Left + 2, rect.Top + 2));
    }
}
