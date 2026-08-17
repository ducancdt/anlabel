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
using ANLAbel.Core.Scene;

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

        // Clip rect must be generous enough to not cut content that is offset/scaled.
        // Use 2x the label dimensions as the clip boundary so content near edges
        // is not prematurely clipped by the transform pipeline.
        var clipRect = new Rect(
            -labelWidthDip,
            -labelHeightDip,
            labelWidthDip * 3,
            labelHeightDip * 3);
        dc.PushClip(new RectangleGeometry(clipRect));
        PushOutputTransforms(dc, labelRect, plan);

        var dpiX = plan.DpiX > 0 ? plan.DpiX : plan.Dpi;
        var dpiY = plan.DpiY > 0 ? plan.DpiY : plan.Dpi;
        var compiled = plan.CompiledScene;
        var canUseCompiledScene = plan.SceneCompilationVerified
            && compiled is { Succeeded: true }
            && compiled.Snapshot.Objects.Length == compiled.Nodes.Length;
        if (canUseCompiledScene)
        {
            RenderCompiledScene(dc, compiled!, row, dpiX, dpiY, labelWidthMm, labelHeightMm);
        }
        else
        {
            foreach (var item in template.Objects.Where(item => item.IsVisible).OrderBy(item => item.ZIndex))
            {
                DrawObject(dc, item, row, dpiX, dpiY, labelWidthMm, labelHeightMm);
            }
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

    private void RenderCompiledScene(
        DrawingContext dc,
        SceneCompilationResult compiled,
        IReadOnlyDictionary<string, string>? row,
        int dpiX,
        int dpiY,
        double labelWidthMm,
        double labelHeightMm)
    {
        var snapshots = compiled.Snapshot.Objects
            .ToDictionary(item => item.Id, StringComparer.Ordinal);
        foreach (var node in compiled.Nodes
                     .Where(item => item.IsVisible)
                     .OrderBy(item => item.ZIndex)
                     .ThenBy(item => item.Id, StringComparer.Ordinal))
        {
            if (!snapshots.TryGetValue(node.Id, out var snapshot))
            {
                continue;
            }

            var item = CreateRenderObject(snapshot);
            DrawObject(dc, item, row, dpiX, dpiY, labelWidthMm, labelHeightMm, node);
        }
    }

    private static LabelObject CreateRenderObject(SceneObjectSnapshot snapshot)
    {
        // Set data/style first because LabelObject's QR convenience setters may
        // auto-size; restore the compiled geometry last so the presenter cannot
        // drift from the immutable scene bounds.
        var item = new LabelObject
        {
            Id = snapshot.Id,
            Name = snapshot.Name,
            Type = snapshot.Type,
            BindingExpression = snapshot.BindingExpression,
            Text = snapshot.Text,
            BarcodeSymbology = snapshot.BarcodeSymbology,
            BarcodeApplicationProfile = snapshot.BarcodeApplicationProfile,
            QrSizingMode = snapshot.QrSizingMode,
            QrErrorCorrection = snapshot.QrErrorCorrection,
            QrFixedVersion = snapshot.QrFixedVersion,
            QrModuleSizePx = snapshot.QrModuleSizePx,
            QrQuietZoneModules = snapshot.QrQuietZoneModules,
            QrDpi = snapshot.QrDpi,
            ShowBarcodeText = snapshot.ShowBarcodeText,
            // Legacy snapshots may only carry ShowBarcodeText; map true+None → Below.
            BarcodeHriPlacement = snapshot.BarcodeHriPlacement == Core.Enums.BarcodeHriPlacement.None
                && snapshot.ShowBarcodeText
                    ? Core.Enums.BarcodeHriPlacement.Below
                    : snapshot.BarcodeHriPlacement,
            BarcodeTextFontSizePt = snapshot.BarcodeTextFontSizePt,
            BarcodeCheckDigitPolicy = snapshot.BarcodeCheckDigitPolicy,
            BarcodeHriShowCheckDigit = snapshot.BarcodeHriShowCheckDigit,
            BarcodeModuleWidthMm = snapshot.BarcodeModuleWidthMm,
            BarcodeWidthMode = snapshot.BarcodeWidthMode,
            Code39WideNarrowRatio = snapshot.Code39WideNarrowRatio,
            BearerBarStyle = snapshot.BearerBarStyle,
            BearerBarThicknessMm = snapshot.BearerBarThicknessMm,
            ImageDataBase64 = snapshot.ImageDataBase64,
            ImageRasterMode = snapshot.ImageRasterMode,
            ImagePixelWidth = snapshot.ImagePixelWidth,
            ImagePixelHeight = snapshot.ImagePixelHeight,
            Style = new ObjectStyle
            {
                FontFamily = snapshot.Style.FontFamily,
                FontSizePt = snapshot.Style.FontSizePt,
                LineHeightPt = snapshot.Style.LineHeightPt,
                Bold = snapshot.Style.Bold,
                Italic = snapshot.Style.Italic,
                Underline = snapshot.Style.Underline,
                Alignment = snapshot.Style.Alignment,
                TextDirection = snapshot.Style.TextDirection,
                TextSizing = snapshot.Style.TextSizing,
                TextOverflow = snapshot.Style.TextOverflow,
                TextFitMinimumFontSizePt = snapshot.Style.TextFitMinimumFontSizePt,
                TextFitMaximumFontSizePt = snapshot.Style.TextFitMaximumFontSizePt,
                TextFitMinimumScale = snapshot.Style.TextFitMinimumScale,
                TextFitMaximumScale = snapshot.Style.TextFitMaximumScale,
                VerticalAlignment = snapshot.Style.VerticalAlignment,
                TextPaddingMm = snapshot.Style.TextPaddingMm,
                TextPaddingLeftMm = snapshot.Style.TextPaddingLeftMm,
                TextPaddingRightMm = snapshot.Style.TextPaddingRightMm,
                TextPaddingTopMm = snapshot.Style.TextPaddingTopMm,
                TextPaddingBottomMm = snapshot.Style.TextPaddingBottomMm,
                BorderThicknessMm = snapshot.Style.BorderThicknessMm,
                OutlineStyle = snapshot.Style.OutlineStyle,
                FillStyle = snapshot.Style.FillStyle,
                CornerRadiusMm = snapshot.Style.CornerRadiusMm,
                FillColor = snapshot.Style.FillColor,
                StrokeColor = snapshot.Style.StrokeColor
            },
            IsLocked = snapshot.IsLocked,
            IsVisible = snapshot.IsVisible,
            ZIndex = snapshot.ZIndex
        };

        item.XMm = snapshot.XMm;
        item.YMm = snapshot.YMm;
        item.LineEndXMm = snapshot.LineEndXMm;
        item.LineEndYMm = snapshot.LineEndYMm;
        item.Rotation = snapshot.Rotation;
        item.WidthMm = snapshot.WidthMm;
        item.HeightMm = snapshot.HeightMm;
        return item;
    }

    private void DrawObject(
        DrawingContext dc,
        LabelObject item,
        IReadOnlyDictionary<string, string>? row,
        int dpiX,
        int dpiY,
        double labelWidthMm,
        double labelHeightMm,
        CompiledSceneNode? compiledNode = null)
    {
        var layoutBounds = compiledNode?.LayoutBoundsMm;
        var xMm = layoutBounds?.LeftMm ?? item.XMm;
        var yMm = layoutBounds?.TopMm ?? item.YMm;
        var widthMm = layoutBounds?.WidthMm ?? item.WidthMm;
        var heightMm = layoutBounds?.HeightMm ?? item.HeightMm;
        var objectType = compiledNode?.Type ?? item.Type;
        var rotation = compiledNode?.Rotation ?? item.Rotation;
        var rect = new Rect(
            MmConverter.MmToDip(xMm),
            MmConverter.MmToDip(yMm),
            MmConverter.MmToDip(widthMm),
            MmConverter.MmToDip(heightMm));

        var needsRotation = rotation != 0 && objectType != ObjectType.Line;
        if (needsRotation)
        {
            var centerX = rect.Left + rect.Width / 2;
            var centerY = rect.Top + rect.Height / 2;
            dc.PushTransform(new RotateTransform(rotation, centerX, centerY));
        }

        switch (objectType)
        {
            case ObjectType.Text:
                DrawTextObject(dc, item, rect, row, constrainToBox: false);
                break;
            case ObjectType.TextBox:
                DrawTextObject(dc, item, rect, row, constrainToBox: true);
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
                var lineStart = compiledNode?.LineStartMm ?? new ScenePoint(item.XMm, item.YMm);
                var lineEnd = compiledNode?.LineEndMm ?? new ScenePoint(
                    item.LineEndXMm == 0 && item.LineEndYMm == 0 ? item.XMm + item.WidthMm : item.LineEndXMm,
                    item.LineEndXMm == 0 && item.LineEndYMm == 0 ? item.YMm + item.HeightMm : item.LineEndYMm);
                var linePen = CreateOutlinePen(item);
                if (linePen is not null)
                {
                    dc.DrawLine(
                        linePen,
                        new Point(MmConverter.MmToDip(lineStart.XMm), MmConverter.MmToDip(lineStart.YMm)),
                        new Point(MmConverter.MmToDip(lineEnd.XMm), MmConverter.MmToDip(lineEnd.YMm)));
                }
                break;
            case ObjectType.BarcodeCode128:
            case ObjectType.QRCode:
            case ObjectType.DataMatrix:
                DrawBarcode(dc, item, rect, row, dpiX, dpiY, labelWidthMm, labelHeightMm);
                break;
            case ObjectType.Image:
                DrawImage(dc, item, rect);
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
        var brush = ParseBrush(item.Style.StrokeColor, Brushes.Black);
        // Shared layout path for Text and TextBox (including free-Text frame-fit
        // compress) so designer preview and print share the same scale outcome.
        if (!constrainToBox
            || TextBoxOverflowDetector.HasExplicitLineHeight(item)
            || TextBoxOverflowDetector.UsesShrinkFont(item)
            || TextBoxOverflowDetector.UsesScaleWidth(item)
            || TextBoxOverflowDetector.UsesTextFrameFitCompress(item))
        {
            var layout = TextBoxOverflowDetector.CreateTextLayout(item, value, rect.Width, rect.Height, constrainToBox, brush);
            if (!constrainToBox)
            {
                TextBoxOverflowDetector.DrawTextLayout(dc, layout, new Point(rect.Left + TextBoxOverflowDetector.GetHorizontalOriginDip(item, constrainToBox), rect.Top + layout.Metrics.VerticalOffsetDip));
                return;
            }

            dc.PushClip(new RectangleGeometry(rect));
            TextBoxOverflowDetector.DrawTextLayout(dc, layout, new Point(rect.Left + TextBoxOverflowDetector.GetHorizontalOriginDip(item, constrainToBox), rect.Top + layout.Metrics.VerticalOffsetDip));
            dc.Pop();
            if (layout.Metrics.IsOverflowing && TextBoxOverflowDetector.ShouldBlockOverflow(item))
            {
                DrawErrorFrame(dc, rect, "Text overflow");
            }

            return;
        }

        var displayValue = TextBoxOverflowDetector.WrapTextToBox(item, value, TextBoxOverflowDetector.GetContentWidthDip(item, rect.Width, constrainToBox));
        var text = TextBoxOverflowDetector.CreateFormattedText(item, displayValue, brush);
        TextBoxOverflowDetector.ApplyLayoutBounds(text, item, rect.Width, rect.Height, constrainToBox);
        var metrics = TextBoxOverflowDetector.Measure(text, item, rect.Width, rect.Height, constrainToBox, value);

        var contentRect = new Rect(
            rect.Left + TextBoxOverflowDetector.GetHorizontalOriginDip(item, constrainToBox),
            rect.Top,
            TextBoxOverflowDetector.GetContentWidthDip(item, rect.Width, constrainToBox),
            Math.Max(1, rect.Height));
        var overflow = TextBoxOverflowDetector.IsOverflowing(item, value, rect.Width, rect.Height);
        dc.PushClip(new RectangleGeometry(rect));
        var textY = contentRect.Top + metrics.VerticalOffsetDip;
        dc.DrawText(text, new Point(contentRect.Left, textY));
        dc.Pop();
        if (overflow && TextBoxOverflowDetector.ShouldBlockOverflow(item))
        {
            DrawErrorFrame(dc, rect, "Text overflow");
        }
    }

    private void DrawBarcode(DrawingContext dc, LabelObject item, Rect rect, IReadOnlyDictionary<string, string>? row, int dpiX, int dpiY, double labelWidthMm, double labelHeightMm)
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

        // Print-preview-reliability-plan R5: plan DPI first for module geometry.
        var barcodeDpi = dpiX > 0 ? dpiX : item.QrDpi;
        // P1.a: SizedFromX production width (effMm × logical modules) shared with designer.
        var productionWidthMm = LinearBarcodeProductionWidth.ResolveSymbolWidthMm(
            item,
            _barcodeRenderer,
            barcodeDpi > 0 ? barcodeDpi : 203,
            data);
        var productionWidthDip = MmConverter.MmToDip(productionWidthMm);
        var objectRect = rect;
        if (Math.Abs(productionWidthDip - rect.Width) > 0.01)
        {
            objectRect = new Rect(rect.Left, rect.Top, Math.Max(1, productionWidthDip), rect.Height);
        }

        var hriLayout = BarcodeHriTextLayout.Measure(
            type,
            data,
            productionWidthMm,
            MmConverter.DipToMm(objectRect.Height),
            item.BarcodeHriPlacement,
            item.BarcodeTextFontSizePt);
        if (!hriLayout.IsValid)
        {
            DrawErrorFrame(dc, objectRect, hriLayout.ErrorMessage ?? "HRI cannot be laid out");
            return;
        }

        // No QR auto-size in print renderer — render at model size to stay aligned with linked text

        try
        {
            var symbolRect = hriLayout.IsEnabled
                ? new Rect(
                    objectRect.Left,
                    objectRect.Top + MmConverter.MmToDip(hriLayout.SymbolTopMm),
                    objectRect.Width,
                    MmConverter.MmToDip(hriLayout.SymbolHeightMm))
                : objectRect;
            var widthMm = MmConverter.DipToMm(symbolRect.Width);
            var heightMm = MmConverter.DipToMm(symbolRect.Height);

            // Try vector rendering for 1D barcodes — eliminates all rasterization/interpolation
            // artifacts ("crease/wrinkle" effect) by drawing sharp vector rectangles.
            // The shared HRI layout reserves a deterministic strip below linear bars;
            // the symbol itself is never stretched into that text area.
            var vectorData = _barcodeRenderer.RenderBarcodeVector(data, type, widthMm, heightMm, barcodeDpi, CreateBarcodeRenderOptions(item));
            if (vectorData is not null)
            {
                DrawVectorBarcode(dc, vectorData, symbolRect, dpiX, dpiY);
                DrawBearerBars(dc, item, symbolRect);
                DrawHri(dc, data, item, hriLayout, objectRect);
                return;
            }

            // Fallback to bitmap for 2D codes (QR, DataMatrix, PDF417, Aztec)
            var renderOptions = CreateBarcodeRenderOptions(item);
            var pixels = _barcodeRenderer is INonSquareBarcodeRenderer nonSquareRenderer
                && dpiX > 0
                && dpiY > 0
                ? nonSquareRenderer.RenderBarcode(data, type, widthMm, heightMm, dpiX, dpiY, renderOptions)
                : _barcodeRenderer.RenderBarcode(data, type, widthMm, heightMm, barcodeDpi, renderOptions);
            var isSquareMatrix = type is BarcodeType.QRCode or BarcodeType.DataMatrix or BarcodeType.Aztec;
            var isMatrix = isSquareMatrix || type == BarcodeType.Pdf417;
            if (dpiX > 0 && dpiY > 0 && !isMatrix)
            {
                // Linear-fallback / PDF417 bitmaps that only expose one DPI still
                // need an explicit device-dot frame. Square 2D already used
                // MatrixSquareModuleFit — a second independent resize would squash modules.
                var targetWidthDots = Math.Max(1, MmConverter.MmToPrinterDots(widthMm, dpiX));
                var targetHeightDots = Math.Max(1, MmConverter.MmToPrinterDots(heightMm, dpiY));
                pixels = pixels.ResizeNearest(targetWidthDots, targetHeightDots);
            }
            var paintDpiX = dpiX > 0 ? dpiX : barcodeDpi;
            var paintDpiY = dpiY > 0 ? dpiY : barcodeDpi;
            var source = BitmapSource.Create(pixels.WidthPixels, pixels.HeightPixels, paintDpiX, paintDpiY, PixelFormats.Bgra32, null, pixels.BgraPixels, pixels.Stride);
            source.Freeze();

            var destWidth = pixels.WidthPixels * 96.0 / paintDpiX;
            var destHeight = pixels.HeightPixels * 96.0 / paintDpiY;
            if (isSquareMatrix)
            {
                destWidth = Math.Min(destWidth, symbolRect.Width);
                destHeight = Math.Min(destHeight, symbolRect.Height);
            }

            var dest = new Rect(
                symbolRect.Left + Math.Max(0, (symbolRect.Width - destWidth) / 2),
                symbolRect.Top + Math.Max(0, (symbolRect.Height - destHeight) / 2),
                destWidth,
                destHeight);
            var guidelines = new GuidelineSet(
                new[] { dest.Left, dest.Right },
                new[] { dest.Top, dest.Bottom });
            dc.PushGuidelineSet(guidelines);
            dc.DrawImage(source, dest);
            dc.Pop();
            DrawBearerBars(dc, item, symbolRect);
            DrawHri(dc, data, item, hriLayout, objectRect);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            dc.DrawRectangle(Brushes.White, new Pen(Brushes.Red, 1), objectRect);
            DrawErrorFrame(dc, objectRect, "Barcode cannot be rendered");
        }
    }

    private void DrawImage(DrawingContext dc, LabelObject item, Rect rect)
    {
        var source = ImageRasterizer.Decode(item.ImageDataBase64, item.ImageRasterMode);
        if (source is null)
        {
            DrawErrorFrame(dc, rect, "No image");
            return;
        }

        var scale = Math.Min(rect.Width / source.PixelWidth, rect.Height / source.PixelHeight);
        var drawWidth = source.PixelWidth * scale;
        var drawHeight = source.PixelHeight * scale;
        var drawRect = new Rect(
            rect.Left + (rect.Width - drawWidth) / 2,
            rect.Top + (rect.Height - drawHeight) / 2,
            drawWidth,
            drawHeight);
        dc.DrawImage(source, drawRect);
    }

    private static void DrawHri(
        DrawingContext dc,
        string value,
        LabelObject item,
        BarcodeHriLayout layout,
        Rect frame)
    {
        if (!layout.IsEnabled || !layout.IsValid)
        {
            return;
        }

        var symbology = item.Type switch
        {
            ObjectType.QRCode => BarcodeSymbology.QRCode,
            ObjectType.DataMatrix => BarcodeSymbology.DataMatrix,
            _ => item.BarcodeSymbology
        };
        var hriValue = BarcodeCheckDigitContract.FormatHriText(
            symbology,
            value,
            item.BarcodeCheckDigitPolicy,
            item.BarcodeHriShowCheckDigit);
        var fontSizePt = item.BarcodeTextFontSizePt;
        var text = BarcodeHriTextLayout.CreateText(hriValue, fontSizePt, Brushes.Black);
        var hriTopDip = frame.Top + MmConverter.MmToDip(layout.HriTopMm);
        var hriHeightDip = MmConverter.MmToDip(layout.HriHeightMm);
        text.MaxTextWidth = frame.Width;
        text.MaxTextHeight = hriHeightDip;
        text.TextAlignment = TextAlignment.Center;
        var y = hriTopDip + Math.Max(0, (hriHeightDip - text.Height) / 2);
        dc.PushClip(new RectangleGeometry(new Rect(frame.Left, hriTopDip, frame.Width, hriHeightDip)));
        dc.DrawText(text, new Point(frame.Left, y));
        dc.Pop();
    }

    private static void DrawVectorBarcode(DrawingContext dc, BarcodeVectorData vectorData, Rect rect, int dpiX, int dpiY)
    {
        // Keep all printer-grid decisions in the platform-neutral layout seam.
        // WPF only paints the already-quantized runs; it never rounds DIP
        // widths independently, which would reintroduce DPI-dependent drift.
        var layout = DeviceBarcodeLayout.Create(
            rect.Left,
            rect.Top,
            rect.Width,
            rect.Height,
            dpiX,
            dpiY,
            vectorData.WidthModules,
            vectorData.RowBits);
        var snappedLeft = DeviceDotQuantizer.DotsToDip(layout.LeftDot, dpiX);
        var snappedRight = DeviceDotQuantizer.DotsToDip(layout.LeftDot + layout.WidthDots, dpiX);
        var snappedTop = DeviceDotQuantizer.DotsToDip(layout.TopDot, dpiY);
        var snappedBottom = DeviceDotQuantizer.DotsToDip(layout.TopDot + layout.HeightDots, dpiY);
        var guidelines = new GuidelineSet(
            new[] { snappedLeft, snappedRight },
            new[] { snappedTop, snappedBottom });
        dc.PushGuidelineSet(guidelines);

        var brush = Brushes.Black;
        var barHeight = DeviceDotQuantizer.DotsToDip(layout.HeightDots, dpiY);
        foreach (var run in layout.DarkRuns)
        {
            var leftDip = DeviceDotQuantizer.DotsToDip(layout.LeftDot + run.StartDot, dpiX);
            var barWidth = DeviceDotQuantizer.DotsToDip(run.WidthDots, dpiX);
            dc.DrawRectangle(brush, null, new Rect(leftDip, snappedTop, barWidth, barHeight));
        }

        dc.Pop(); // Pop GuidelineSet
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
            QuietZoneModules = item.QrQuietZoneModules,
            IsGs1 = item.BarcodeApplicationProfile == BarcodeApplicationProfile.Gs1,
            Code39WideNarrowRatio = item.Code39WideNarrowRatio,
            BearerBarStyle = item.BearerBarStyle,
            BearerBarThicknessMm = item.BearerBarThicknessMm
        };
    }

    private static void DrawBearerBars(DrawingContext dc, LabelObject item, Rect symbolRect)
    {
        if (item.BearerBarStyle == BearerBarStyle.None)
        {
            return;
        }

        var thicknessMm = item.BearerBarThicknessMm > 0 ? item.BearerBarThicknessMm : 1.0;
        var thicknessDip = MmConverter.MmToDip(thicknessMm);
        if (thicknessDip <= 0 || symbolRect.Width <= 0 || symbolRect.Height <= 0)
        {
            return;
        }

        var t = Math.Min(thicknessDip, symbolRect.Height / 3);

        // Top horizontal bearer bar
        dc.DrawRectangle(Brushes.Black, null, new Rect(symbolRect.Left, symbolRect.Top, symbolRect.Width, t));
        // Bottom horizontal bearer bar
        dc.DrawRectangle(Brushes.Black, null, new Rect(symbolRect.Left, Math.Max(symbolRect.Top, symbolRect.Bottom - t), symbolRect.Width, t));

        if (item.BearerBarStyle == BearerBarStyle.Frame)
        {
            var tw = Math.Min(thicknessDip, symbolRect.Width / 4);
            // Left vertical bearer bar
            dc.DrawRectangle(Brushes.Black, null, new Rect(symbolRect.Left, symbolRect.Top, tw, symbolRect.Height));
            // Right vertical bearer bar
            dc.DrawRectangle(Brushes.Black, null, new Rect(Math.Max(symbolRect.Left, symbolRect.Right - tw), symbolRect.Top, tw, symbolRect.Height));
        }
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
