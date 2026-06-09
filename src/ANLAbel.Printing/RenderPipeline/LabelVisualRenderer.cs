using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Pen = System.Windows.Media.Pen;
using Color = System.Windows.Media.Color;
using Point = System.Windows.Point;
using Rect = System.Windows.Rect;
using Size = System.Windows.Size;
using FlowDirection = System.Windows.FlowDirection;
using Typeface = System.Windows.Media.Typeface;
using ANLAbel.Barcode.Options;
using ANLAbel.Barcode.Renderers;
using ANLAbel.Core.Barcode;
using ANLAbel.Core.Enums;
using ANLAbel.Core.Expressions;
using ANLAbel.Core.Expressions.Formulas;
using ANLAbel.Core.Geometry;
using ANLAbel.Core.Models;

namespace ANLAbel.Printing.RenderPipeline;

public sealed class LabelVisualRenderer
{
    private readonly IBarcodeRenderer _barcodeRenderer;

    public LabelVisualRenderer()
        : this(new ZxingBarcodeRenderer())
    {
    }

    public LabelVisualRenderer(IBarcodeRenderer barcodeRenderer)
    {
        _barcodeRenderer = barcodeRenderer;
    }

    public DrawingVisual Render(LabelTemplate template, IReadOnlyDictionary<string, string>? row, PrintRenderPlan plan)
    {
        var visual = new DrawingVisual();
        // Use NearestNeighbor for barcode bitmaps to prevent bilinear interpolation
        // that causes "crease/wrinkle" artifacts on barcode bars.
        RenderOptions.SetBitmapScalingMode(visual, BitmapScalingMode.NearestNeighbor);
        RenderOptions.SetEdgeMode(visual, EdgeMode.Aliased);
        using var dc = visual.RenderOpen();

        var labelWidthMm = plan.LabelWidthMm > 0 ? plan.LabelWidthMm : template.WidthMm;
        var labelHeightMm = plan.LabelHeightMm > 0 ? plan.LabelHeightMm : template.HeightMm;
        var labelWidthDip = MmConverter.MmToDip(labelWidthMm);
        var labelHeightDip = MmConverter.MmToDip(labelHeightMm);
        var labelRect = new Rect(0, 0, labelWidthDip, labelHeightDip);
        dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, labelWidthDip, labelHeightDip));

        dc.PushTransform(new TranslateTransform(MmConverter.MmToDip(plan.OffsetXMm), MmConverter.MmToDip(plan.OffsetYMm)));
        dc.PushTransform(new ScaleTransform(plan.ScaleX, plan.ScaleY));
        dc.PushClip(new RectangleGeometry(labelRect));
        PushOutputTransforms(dc, labelRect, plan);

        foreach (var item in template.Objects.Where(item => item.IsVisible).OrderBy(item => item.ZIndex))
        {
            DrawObject(dc, item, row, plan.Dpi, labelWidthMm, labelHeightMm);
        }

        PopOutputTransforms(dc, plan);
        dc.Pop();
        dc.Pop();
        dc.Pop();
        return visual;
    }

    public DrawingVisual RenderCalibration(PrintRenderPlan plan)
    {
        var visual = new DrawingVisual();
        using var dc = visual.RenderOpen();
        var width = MmConverter.MmToDip(plan.LabelWidthMm);
        var height = MmConverter.MmToDip(plan.LabelHeightMm);
        dc.DrawRectangle(Brushes.White, new Pen(Brushes.Black, 1), new Rect(0, 0, width, height));

        var pen = new Pen(Brushes.Black, 1);
        for (var xMm = 0; xMm <= plan.LabelWidthMm; xMm += 10)
        {
            var x = MmConverter.MmToDip(xMm);
            dc.DrawLine(pen, new Point(x, 0), new Point(x, MmConverter.MmToDip(xMm % 50 == 0 ? 8 : 5)));
            DrawText(dc, $"{xMm:0}", x + 2, MmConverter.MmToDip(9), 7, Brushes.Black);
        }

        for (var yMm = 0; yMm <= plan.LabelHeightMm; yMm += 10)
        {
            var y = MmConverter.MmToDip(yMm);
            dc.DrawLine(pen, new Point(0, y), new Point(MmConverter.MmToDip(yMm % 50 == 0 ? 8 : 5), y));
            DrawText(dc, $"{yMm:0}", MmConverter.MmToDip(9), y + 2, 7, Brushes.Black);
        }

        DrawText(dc, $"{plan.LabelWidthMm:0.##} x {plan.LabelHeightMm:0.##} mm @ {plan.Dpi} DPI", MmConverter.MmToDip(4), height - MmConverter.MmToDip(12), 8, Brushes.Black);
        DrawText(dc, $"Media: {plan.MediaType} | Gap: {plan.GapMm:0.##} mm | Feed: {plan.FeedDirection} | Rotated 180: {(plan.Rotated180 ? "Yes" : "No")}", MmConverter.MmToDip(4), height - MmConverter.MmToDip(11), 7, Brushes.Black);
        DrawText(dc, $"Printable area margin: {plan.MarginMm:0.##} mm", MmConverter.MmToDip(4), height - MmConverter.MmToDip(6), 7, Brushes.Black);
        return visual;
    }

    private void DrawObject(DrawingContext dc, LabelObject item, IReadOnlyDictionary<string, string>? row, int dpi, double labelWidthMm, double labelHeightMm)
    {
        var rect = new Rect(
            MmConverter.MmToDip(item.XMm),
            MmConverter.MmToDip(item.YMm),
            MmConverter.MmToDip(item.WidthMm),
            MmConverter.MmToDip(item.HeightMm));

        var needsRotation = item.Rotation != 0 && item.Type != ObjectType.Line;
        if (needsRotation)
        {
            var centerX = rect.Left + rect.Width / 2;
            var centerY = rect.Top + rect.Height / 2;
            dc.PushTransform(new RotateTransform(item.Rotation, centerX, centerY));
        }

        switch (item.Type)
        {
            case ObjectType.Text:
                DrawTextObject(dc, item, rect, row, false);
                break;
            case ObjectType.TextBox:
                DrawTextObject(dc, item, rect, row, true);
                break;
            case ObjectType.Rectangle:
                dc.DrawRoundedRectangle(
                    GetFillBrush(item),
                    CreateOutlinePen(item),
                    rect,
                    MmConverter.MmToDip(item.Style.CornerRadiusMm),
                    MmConverter.MmToDip(item.Style.CornerRadiusMm));
                break;
            case ObjectType.Ellipse:
                dc.DrawEllipse(
                    GetFillBrush(item),
                    CreateOutlinePen(item),
                    new Point(rect.Left + rect.Width / 2, rect.Top + rect.Height / 2),
                    rect.Width / 2,
                    rect.Height / 2);
                break;
            case ObjectType.Line:
                var lineEndXMm = item.LineEndXMm == 0 && item.LineEndYMm == 0 ? item.XMm + item.WidthMm : item.LineEndXMm;
                var lineEndYMm = item.LineEndXMm == 0 && item.LineEndYMm == 0 ? item.YMm + item.HeightMm : item.LineEndYMm;
                var linePen = CreateOutlinePen(item);
                if (linePen is not null)
                {
                    dc.DrawLine(
                        linePen,
                        new Point(MmConverter.MmToDip(item.XMm), MmConverter.MmToDip(item.YMm)),
                        new Point(MmConverter.MmToDip(lineEndXMm), MmConverter.MmToDip(lineEndYMm)));
                }
                break;
            case ObjectType.BarcodeCode128:
            case ObjectType.QRCode:
            case ObjectType.DataMatrix:
                DrawBarcode(dc, item, rect, row, dpi, labelWidthMm, labelHeightMm);
                break;
        }

        if (needsRotation)
        {
            dc.Pop();
        }
    }

    private void DrawTextObject(DrawingContext dc, LabelObject item, Rect rect, IReadOnlyDictionary<string, string>? row, bool constrainToBox)
    {
        var value = ResolveData(item, row);
        var displayValue = constrainToBox
            ? TextBoxOverflowDetector.WrapTextToBox(item, value, Math.Max(1, rect.Width - TextBoxOverflowDetector.HorizontalPaddingDip * 2))
            : value;
        var text = TextBoxOverflowDetector.CreateFormattedText(item, displayValue, ParseBrush(item.Style.StrokeColor, Brushes.Black));

        if (!constrainToBox)
        {
            var x = rect.Left + 2;
            var y = rect.Top + Math.Max(0, (rect.Height - text.Height) / 2);
            dc.DrawText(text, new Point(x, y));
            return;
        }

        var contentRect = new Rect(
            rect.Left + TextBoxOverflowDetector.HorizontalPaddingDip,
            rect.Top,
            Math.Max(1, rect.Width - TextBoxOverflowDetector.HorizontalPaddingDip * 2),
            Math.Max(1, rect.Height));
        var overflow = TextBoxOverflowDetector.IsOverflowing(item, value, rect.Width, rect.Height);
        text.MaxTextWidth = contentRect.Width;
        text.MaxTextHeight = Math.Max(1, rect.Height);
        dc.PushClip(new RectangleGeometry(rect));
        dc.DrawText(text, contentRect.TopLeft);
        dc.Pop();
        if (overflow)
        {
            DrawErrorFrame(dc, rect, "Text overflow");
        }
    }

    private void DrawBarcode(DrawingContext dc, LabelObject item, Rect rect, IReadOnlyDictionary<string, string>? row, int dpi, double labelWidthMm, double labelHeightMm)
    {
        var data = ResolveData(item, row);
        var type = item.Type switch
        {
            ObjectType.QRCode => BarcodeType.QRCode,
            ObjectType.DataMatrix => BarcodeType.DataMatrix,
            _ => BarcodeTypeMapper.ToRendererType(item.BarcodeSymbology)
        };

        if (!_barcodeRenderer.ValidateData(data, type))
        {
            dc.DrawRectangle(Brushes.White, new Pen(Brushes.Red, 1), rect);
            DrawErrorFrame(dc, rect, "Invalid barcode data");
            return;
        }

        if (IsSquare2DCodeLike(item))
        {
            var fitSizeMm = item.QrSizingMode == QrSizingMode.FixedVersionAndModuleSize
                ? QrAutoSizeHelper.CalculateFixedSizeMm(item.QrFixedVersion, item.QrModuleSizePx, item.QrQuietZoneModules, item.QrDpi, GetAvailableQrSizeMm(item, labelWidthMm, labelHeightMm))
                : QrAutoSizeHelper.CalculateRequiredSizeMm(
                    data,
                    item.WidthMm,
                    item.HeightMm,
                    item.QrErrorCorrection,
                    item.QrModuleSizePx,
                    item.QrQuietZoneModules,
                    item.QrDpi,
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

            // Try vector rendering for 1D barcodes — eliminates all rasterization/interpolation
            // artifacts ("crease/wrinkle" effect) by drawing sharp vector rectangles.
            var vectorData = _barcodeRenderer.RenderBarcodeVector(data, type, widthMm, heightMm, barcodeDpi, CreateBarcodeRenderOptions(item));
            if (vectorData is not null)
            {
                DrawVectorBarcode(dc, vectorData, rect, barcodeDpi);
                return;
            }

            // Fallback to bitmap for 2D codes (QR, DataMatrix, PDF417, Aztec)
            var pixels = _barcodeRenderer.RenderBarcode(data, type, widthMm, heightMm, barcodeDpi, CreateBarcodeRenderOptions(item));
            var source = BitmapSource.Create(pixels.WidthPixels, pixels.HeightPixels, barcodeDpi, barcodeDpi, PixelFormats.Bgra32, null, pixels.BgraPixels, pixels.Stride);
            source.Freeze();

            // Draw the barcode at its natural DIP size to prevent WPF scaling artifacts.
            var naturalWidthDip = pixels.WidthPixels * 96.0 / barcodeDpi;
            var naturalHeightDip = pixels.HeightPixels * 96.0 / barcodeDpi;

            var guidelines = new GuidelineSet(
                new[] { rect.Left, rect.Left + naturalWidthDip },
                new[] { rect.Top, rect.Top + naturalHeightDip });
            dc.PushGuidelineSet(guidelines);

            dc.DrawImage(source, new Rect(rect.Left, rect.Top, naturalWidthDip, naturalHeightDip));

            dc.Pop(); // Pop GuidelineSet
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            dc.DrawRectangle(Brushes.White, new Pen(Brushes.Red, 1), rect);
            DrawErrorFrame(dc, rect, "Barcode cannot be rendered");
        }
    }

    private static void DrawVectorBarcode(DrawingContext dc, BarcodeVectorData vectorData, Rect rect, int dpi)
    {
        // Each module maps to exactly one DIP-width unit (printer dot / (dpi/96)).
        // This produces pixel-perfect bars at the printer's native resolution.
        var moduleWidthDip = 96.0 / dpi; // one pixel's width in DIPs
        var totalModuleWidthDip = vectorData.WidthModules * moduleWidthDip;

        // Scale if the natural pixel width differs from the target rect width
        var scaleX = rect.Width / totalModuleWidthDip;
        var scaleY = 1.0; // 1D barcode: full height always

        if (Math.Abs(scaleX - 1.0) > 0.001)
        {
            dc.PushTransform(new ScaleTransform(scaleX, scaleY, rect.Left, rect.Top));
        }

        var brush = Brushes.Black;
        var x = rect.Left;
        var i = 0;

        while (i < vectorData.WidthModules)
        {
            if (vectorData.RowBits[i])
            {
                // Find the end of this contiguous dark run
                var startX = x;
                while (i < vectorData.WidthModules && vectorData.RowBits[i])
                {
                    x += moduleWidthDip;
                    i++;
                }
                // Draw one rectangle for the entire contiguous dark run
                dc.DrawRectangle(brush, null, new Rect(startX, rect.Top, x - startX, rect.Height));
            }
            else
            {
                x += moduleWidthDip;
                i++;
            }
        }

        if (Math.Abs(scaleX - 1.0) > 0.001)
        {
            dc.Pop(); // Pop ScaleTransform
        }
    }

    private static void DrawErrorFrame(DrawingContext dc, Rect rect, string message)
    {
        var pen = new Pen(Brushes.Red, 1.4)
        {
            DashStyle = DashStyles.Dash
        };
        dc.DrawRectangle(null, pen, rect);
        DrawErrorBadge(dc, rect);
        DrawText(dc, message, rect.Left + 2, rect.Top + 2, 7, Brushes.Red);
    }

    private static void DrawErrorBadge(DrawingContext dc, Rect rect)
    {
        const double radius = 7;
        var center = new Point(rect.Right - radius, rect.Top + radius);
        dc.DrawEllipse(Brushes.Red, null, center, radius, radius);
        var text = new FormattedText("!", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), 11, Brushes.White, 1.0)
        {
            TextAlignment = TextAlignment.Center
        };
        dc.DrawText(text, new Point(center.X, center.Y - text.Height / 2 - 0.5));
    }

    private static void PushOutputTransforms(DrawingContext dc, Rect printableRect, PrintRenderPlan plan)
    {
        var rotation = GetOutputRotation(plan);
        if (rotation == 0)
        {
            return;
        }

        var center = new Point(printableRect.Left + printableRect.Width / 2, printableRect.Top + printableRect.Height / 2);
        dc.PushTransform(new RotateTransform(rotation, center.X, center.Y));
    }

    private static void PopOutputTransforms(DrawingContext dc, PrintRenderPlan plan)
    {
        if (GetOutputRotation(plan) != 0)
        {
            dc.Pop();
        }
    }

    private static double GetOutputRotation(PrintRenderPlan plan)
    {
        var rotation = plan.FeedDirection switch
        {
            FeedDirection.BottomToTop => 180d,
            FeedDirection.LeftToRight => 90d,
            FeedDirection.RightToLeft => 270d,
            _ => 0d
        };

        if (plan.Rotated180)
        {
            rotation += 180d;
        }

        rotation %= 360d;
        return rotation;
    }

    private static BarcodeRenderOptions CreateBarcodeRenderOptions(LabelObject item)
    {
        return new BarcodeRenderOptions
        {
            ErrorCorrection = item.QrErrorCorrection.ToString(),
            QuietZoneModules = item.QrQuietZoneModules
        };
    }

    private static bool IsSquare2DCodeLike(LabelObject item)
    {
        return item.Type == ObjectType.QRCode
            || item.Type == ObjectType.DataMatrix
            || item.Type == ObjectType.BarcodeCode128
                && item.BarcodeSymbology is BarcodeSymbology.QRCode
                    or BarcodeSymbology.DataMatrix
                    or BarcodeSymbology.Aztec
                    or BarcodeSymbology.Pdf417;
    }

    private static double GetAvailableQrSizeMm(LabelObject item, double labelWidthMm, double labelHeightMm)
    {
        var availableWidthMm = labelWidthMm - item.XMm;
        var availableHeightMm = labelHeightMm - item.YMm;
        return Math.Max(1, Math.Min(availableWidthMm, availableHeightMm));
    }

    private static string ResolveData(LabelObject item, IReadOnlyDictionary<string, string>? row)
    {
        if (string.IsNullOrWhiteSpace(item.BindingExpression))
        {
            return item.Text;
        }

        if (row is null)
        {
            return item.BindingExpression;
        }

        return FormulaBindingEvaluator.LooksLikeFormula(item.BindingExpression)
            ? FormulaBindingEvaluator.Evaluate(item.BindingExpression, row).Value
            : BindingExpressionEvaluator.Evaluate(item.BindingExpression, row);
    }

    private static void DrawText(DrawingContext dc, string value, double x, double y, double fontSizePt, Brush brush)
    {
        var text = new FormattedText(value, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), fontSizePt * 96.0 / 72.0, brush, 1.0);
        dc.DrawText(text, new Point(x, y));
    }

    private static Brush ParseBrush(string color, Brush fallback)
    {
        try
        {
            return (Brush)new BrushConverter().ConvertFromString(color)!;
        }
        catch
        {
            return fallback;
        }
    }

    private static Brush? GetFillBrush(LabelObject item)
    {
        return item.Style.FillStyle == FillStyle.None
            ? null
            : ParseBrush(item.Style.FillColor, Brushes.Transparent);
    }

    private static Pen? CreateOutlinePen(LabelObject item)
    {
        if (item.Style.OutlineStyle == OutlineStyle.None || item.Style.BorderThicknessMm <= 0)
        {
            return null;
        }

        var pen = new Pen(ParseBrush(item.Style.StrokeColor, Brushes.Black), Math.Max(0.1, MmConverter.MmToDip(item.Style.BorderThicknessMm)))
        {
            DashStyle = item.Style.OutlineStyle switch
            {
                OutlineStyle.Dash => DashStyles.Dash,
                OutlineStyle.Dot => DashStyles.Dot,
                _ => DashStyles.Solid
            }
        };
        return pen;
    }
}
