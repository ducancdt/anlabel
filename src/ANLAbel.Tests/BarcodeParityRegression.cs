using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ANLAbel.App.Controls;
using ANLAbel.App.Services;
using ANLAbel.Core.Enums;
using ANLAbel.Core.Geometry;
using ANLAbel.Core.Models;
using ANLAbel.Printing.PrinterProfiles;
using ANLAbel.Printing.RenderPipeline;
using ZXing;
using ZXing.Common;

/// <summary>
/// B06 (CORE_SOFTWARE_STANDARD §5): the designer, the print preview and the
/// print paginator draw a bound barcode with one encode and layout policy.
/// Each output is rasterised, the dark symbol is measured in millimetres and
/// decoded. Before 2026-10-14 the designer rasterised at the object's QrDpi
/// and stretched the bitmap into the frame, so at 203 DPI it showed a 45 mm
/// Code 128 frame as 33.9 mm of bars while the label carried 25.0 mm, and a
/// 16 mm QR as 13.8 mm instead of 12.5 mm.
/// </summary>
internal static class BarcodeParityRegression
{
    private const int MeasureDpi = 609;
    private const double ToleranceMm = 0.3;
    private const string Payload = "PN-000123|LOT-77";

    private sealed record Case(string Name, Func<LabelObject> Create, BarcodeFormat Format, double LabelWidthMm = 60);

    private sealed record Measurement(double LeftMm, double TopMm, double WidthMm, double HeightMm, byte[] Gray, byte[] Raw, int Width, int Height);

    public static Task DesignerPreviewAndPrintDrawTheSameBarcode()
    {
        var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Code"] = Payload };
        var failures = new List<string>();
        foreach (var dpi in new[] { 203, 300 })
        {
            foreach (var testCase in CreateCases())
            {
                var item = testCase.Create();
                var template = new LabelTemplate { Name = "B06 " + testCase.Name, WidthMm = testCase.LabelWidthMm, HeightMm = 40, Dpi = dpi };
                template.PrinterProfile.Dpi = dpi;
                template.Objects.Add(item);
                var plan = new PrintRenderPlan
                {
                    Dpi = dpi,
                    DpiX = dpi,
                    DpiY = dpi,
                    LabelWidthMm = template.WidthMm,
                    LabelHeightMm = template.HeightMm
                };

                // "print" is LabelVisualRenderer with the plan, the call the paginator
                // makes for each page (checked in the source below); the preview page
                // from PrintService must be that same drawing.
                var print = RunOnSta(() => Measure(new LabelVisualRenderer().Render(template, row, plan), template));
                var preview = RunOnSta(() => Measure(new PrintService().CreatePreviewPage(template, row, 1, plan).Visual, template));
                var designer = RunOnSta(() => MeasureDesigner(template, row));
                var label = $"{testCase.Name} @ {dpi} DPI";
                Console.WriteLine(
                    $"MEASURE B06 {label}: frame {item.WidthMm:0.##}x{item.HeightMm:0.##} mm; "
                    + $"print {Describe(print)}; designer {Describe(designer)}");

                if (!print.Raw.AsSpan().SequenceEqual(preview.Raw))
                {
                    failures.Add($"{label}: the preview page is not the renderer's drawing for the plan.");
                }

                foreach (var (name, output) in new[] { ("print", print), ("designer", designer) })
                {
                    var decoded = Decode(output, testCase.Format);
                    if (decoded != Payload)
                    {
                        failures.Add($"{label}: {name} decodes as '{decoded}', expected '{Payload}'.");
                    }
                }

                var drift = new[]
                {
                    ("left", designer.LeftMm - print.LeftMm),
                    ("top", designer.TopMm - print.TopMm),
                    ("width", designer.WidthMm - print.WidthMm),
                    ("height", designer.HeightMm - print.HeightMm)
                }.Where(edge => Math.Abs(edge.Item2) > ToleranceMm).ToArray();
                if (drift.Length > 0)
                {
                    failures.Add($"{label}: the designer draws {Describe(designer)} but the label prints {Describe(print)} ("
                        + string.Join(", ", drift.Select(edge => $"{edge.Item1} {edge.Item2:+0.###;-0.###} mm")) + ").");
                }
            }
        }

        // The paginator and the preview page both draw with the one renderer call
        // and the same plan; a paginator that reused a preview bitmap would fail here.
        var root = AppContext.BaseDirectory;
        while (!File.Exists(Path.Combine(root, "ANLAbel.slnx")))
        {
            root = Path.GetDirectoryName(root.TrimEnd(Path.DirectorySeparatorChar))!;
        }

        var printService = File.ReadAllText(Path.Combine(root, "src", "ANLAbel.Printing", "PrinterProfiles", "PrintService.cs"));
        if (!printService.Contains("pageNumber => _renderer.Render(template, rowList[pageNumber], plan)", StringComparison.Ordinal)
            || !printService.Contains("Visual = _renderer.Render(template, row, plan)", StringComparison.Ordinal))
        {
            failures.Add("The print paginator and the preview page must draw with the same renderer call and plan.");
        }

        if (failures.Count > 0)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, failures));
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// B05: a SizedFromX symbol prints at the width its row needs, past the
    /// frame. A row that pushes it over the label edge must block, also when
    /// the barcode is rotated, and a short row must not.
    /// </summary>
    public static Task LinearSymbolPastTheLabelEdgeBlocksItsRow()
    {
        // A short row, two long rows of different lengths and an empty cell.
        var rows = new IReadOnlyDictionary<string, string>?[]
        {
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Code"] = "A1" },
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Code"] = Payload },
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Code"] = Payload + "-9" },
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Code"] = string.Empty }
        };
        foreach (var (rotation, labelWidth, labelHeight, x, y) in new[] { (0, 40.0, 30.0, 3.0, 5.0), (90, 40.0, 60.0, 10.0, 20.0) })
        {
            var template = new LabelTemplate { Name = "B05 rotation " + rotation, WidthMm = labelWidth, HeightMm = labelHeight, Dpi = 203 };
            template.PrinterProfile.Dpi = 203;
            template.Objects.Add(new LabelObject
            {
                Id = "c128-x", Type = ObjectType.BarcodeCode128, Name = "C128 X", XMm = x, YMm = y, WidthMm = 20, HeightMm = 10,
                Rotation = rotation, Text = "A1", BindingExpression = "{Code}", BarcodeSymbology = BarcodeSymbology.Code128,
                ShowBarcodeText = false, BarcodeWidthMode = BarcodeWidthMode.SizedFromX, BarcodeModuleWidthMm = 0.25,
                Style = { BorderThicknessMm = 0 }
            });

            var preflight = new PrintService().ValidateRows(template, rows);
            var overflow = preflight.Issues
                .Where(issue => issue.IsBlocking && issue.Message.StartsWith("Barcode for this row is ", StringComparison.Ordinal))
                .ToArray();
            Require(overflow.Select(issue => issue.RowNumber).Order().SequenceEqual(new[] { 2, 3 })
                    && preflight.Issues.Where(issue => issue.IsBlocking).All(issue => issue.RowNumber is 2 or 3 or 4),
                $"Rotation {rotation}: only the long rows may block for running past the label; the empty cell is not measured: {preflight.ToUserMessage()}");
            // One message for every long row, so the rows group into one line.
            Require(PreflightIssueText.Describe(preflight).Contains("Dòng 2, 3 · “C128 X” (mã vạch): mã vạch của dòng này rộng hơn tem", StringComparison.Ordinal),
                $"Rotation {rotation}: the long rows must be one Vietnamese line: {PreflightIssueText.Describe(preflight)}");

            // The print renderer really draws the long row past the label edge.
            var plan = new PrintRenderPlan { Dpi = 203, DpiX = 203, DpiY = 203, LabelWidthMm = labelWidth, LabelHeightMm = labelHeight };
            var (shortInside, longInside) = RunOnSta(() => (
                DrawsInsideLabel(new LabelVisualRenderer().Render(template, rows[0], plan), template),
                DrawsInsideLabel(new LabelVisualRenderer().Render(template, rows[1], plan), template)));
            Require(shortInside && !longInside,
                $"Rotation {rotation}: the short row must print inside the label and the long row past it (short {shortInside}, long {longInside}).");
        }

        // The human-readable text is laid out under the row's symbol, as printed,
        // not under the narrower frame stored from the short sample.
        var hriTemplate = new LabelTemplate { Name = "B05 HRI", WidthMm = 90, HeightMm = 40, Dpi = 203 };
        hriTemplate.PrinterProfile.Dpi = 203;
        hriTemplate.Objects.Add(new LabelObject
        {
            Id = "c128-x-hri", Type = ObjectType.BarcodeCode128, Name = "C128 X HRI", XMm = 3, YMm = 5, WidthMm = 12, HeightMm = 14,
            Text = "A1", BindingExpression = "{Code}", BarcodeSymbology = BarcodeSymbology.Code128,
            ShowBarcodeText = true, BarcodeHriPlacement = BarcodeHriPlacement.Below,
            BarcodeWidthMode = BarcodeWidthMode.SizedFromX, BarcodeModuleWidthMm = 0.25, Style = { BorderThicknessMm = 0 }
        });
        var hriPreflight = new PrintService().ValidateRows(hriTemplate, rows[..2]);
        Require(hriPreflight.IsSuccess,
            "A long row whose text fits under its own symbol must not be blocked by the stored frame width: " + hriPreflight.ToUserMessage());

        return Task.CompletedTask;
    }

    /// <summary>
    /// B04/B06: a 2D symbol whose row needs more printer dots than its frame holds
    /// blocks, and until fixed the designer shows what the print would do: a square
    /// code squeezed into the frame, a PDF417 drawn from the frame's corner past it.
    /// A printer or DPI change redraws the designer's barcodes.
    /// </summary>
    public static Task OversizedTwoDimensionalSymbolsBlockAndDrawLikeThePrint()
    {
        var longRow = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Code"] = string.Concat(Enumerable.Repeat("PN-000123|LOT-77|", 6))
        };
        var failures = new List<string>();
        foreach (var (name, item, format) in new (string, LabelObject, BarcodeFormat)[]
                 {
                     ("PDF417", new LabelObject
                     {
                         Id = "pdf-small", Type = ObjectType.BarcodeCode128, Name = "PDF417", XMm = 20, YMm = 5, WidthMm = 22, HeightMm = 14,
                         BindingExpression = "{Code}", BarcodeSymbology = BarcodeSymbology.Pdf417, ShowBarcodeText = false,
                         Style = { BorderThicknessMm = 0 }
                     }, BarcodeFormat.PDF_417),
                     ("QR", new LabelObject
                     {
                         Id = "qr-small", Type = ObjectType.QRCode, Name = "QR", XMm = 20, YMm = 5, WidthMm = 5, HeightMm = 5,
                         BindingExpression = "{Code}", Style = { BorderThicknessMm = 0 }
                     }, BarcodeFormat.QR_CODE)
                 })
        {
            var template = new LabelTemplate { Name = "B04 " + name, WidthMm = 60, HeightMm = 40, Dpi = 203 };
            template.PrinterProfile.Dpi = 203;
            template.Objects.Add(item);
            var preflight = new PrintService().ValidateRows(template, new IReadOnlyDictionary<string, string>?[] { longRow });
            if (preflight.IsSuccess)
            {
                failures.Add($"{name}: a symbol larger than its frame must block.");
            }

            var plan = new PrintRenderPlan { Dpi = 203, DpiX = 203, DpiY = 203, LabelWidthMm = template.WidthMm, LabelHeightMm = template.HeightMm };
            var print = RunOnSta(() => Measure(new LabelVisualRenderer().Render(template, longRow, plan), template));
            var designer = RunOnSta(() => MeasureDesigner(template, longRow));
            Console.WriteLine($"MEASURE B04 {name} oversized: print {Describe(print)}; designer {Describe(designer)}");
            if (Math.Abs(designer.LeftMm - print.LeftMm) > ToleranceMm || Math.Abs(designer.WidthMm - print.WidthMm) > ToleranceMm
                || Math.Abs(designer.TopMm - print.TopMm) > ToleranceMm || Math.Abs(designer.HeightMm - print.HeightMm) > ToleranceMm)
            {
                failures.Add($"{name}: the designer draws {Describe(designer)} but the label prints {Describe(print)}.");
            }
        }

        // A printer or DPI change redraws the designer's barcodes at the new DPI.
        var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Code"] = Payload };
        var dpiTemplate = new LabelTemplate { Name = "B06 DPI change", WidthMm = 60, HeightMm = 40, Dpi = 300 };
        dpiTemplate.PrinterProfile.Dpi = 300;
        dpiTemplate.Objects.Add(new LabelObject
        {
            Id = "c128-dpi", Type = ObjectType.BarcodeCode128, Name = "C128", XMm = 5, YMm = 5, WidthMm = 45, HeightMm = 10,
            BindingExpression = "{Code}", BarcodeSymbology = BarcodeSymbology.Code128, ShowBarcodeText = false,
            Style = { BorderThicknessMm = 0 }
        });
        var (before, after) = RunOnSta(() =>
        {
            var canvas = new LabelDesignerCanvas { Template = dpiTemplate, Zoom = 1, PreviewRow = row };
            var size = new Size(MmConverter.MmToDip(dpiTemplate.WidthMm), MmConverter.MmToDip(dpiTemplate.HeightMm));
            canvas.Measure(size);
            canvas.Arrange(new Rect(size));
            canvas.UpdateLayout();
            var at300 = Measure(canvas, dpiTemplate);
            dpiTemplate.PrinterProfile.Dpi = 203;
            dpiTemplate.Dpi = 203;
            canvas.UpdateLayout();
            return (at300, Measure(canvas, dpiTemplate));
        });
        var printAt203 = RunOnSta(() => Measure(new LabelVisualRenderer().Render(dpiTemplate, row,
            new PrintRenderPlan { Dpi = 203, DpiX = 203, DpiY = 203, LabelWidthMm = dpiTemplate.WidthMm, LabelHeightMm = dpiTemplate.HeightMm }), dpiTemplate));
        Console.WriteLine($"MEASURE B06 DPI change: designer at 300 {Describe(before)}; after the change {Describe(after)}; print at 203 {Describe(printAt203)}");
        if (Math.Abs(after.WidthMm - printAt203.WidthMm) > ToleranceMm || Math.Abs(before.WidthMm - after.WidthMm) < 1)
        {
            failures.Add($"After the DPI change the designer draws {Describe(after)}, the label prints {Describe(printAt203)}.");
        }

        if (failures.Count > 0)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, failures));
        }

        return Task.CompletedTask;
    }

    /// <summary>True when no dark pixel of the drawing falls outside the label.</summary>
    private static bool DrawsInsideLabel(Visual visual, LabelTemplate template)
    {
        const double marginMm = 40;
        var marginDip = MmConverter.MmToDip(marginMm);
        var widthPx = (int)Math.Ceiling((template.WidthMm + 2 * marginMm) / 25.4 * 203);
        var heightPx = (int)Math.Ceiling((template.HeightMm + 2 * marginMm) / 25.4 * 203);
        var shifted = new DrawingVisual();
        using (var context = shifted.RenderOpen())
        {
            context.DrawRectangle(Brushes.White, null, new Rect(0, 0, widthPx * 96.0 / 203, heightPx * 96.0 / 203));
            context.PushTransform(new TranslateTransform(marginDip, marginDip));
            context.DrawDrawing(((DrawingVisual)visual).Drawing);
            context.Pop();
        }

        var bitmap = new RenderTargetBitmap(widthPx, heightPx, 203, 203, PixelFormats.Pbgra32);
        bitmap.Render(shifted);
        var pixels = new byte[widthPx * heightPx * 4];
        bitmap.CopyPixels(pixels, widthPx * 4, 0);
        var labelLeft = (int)Math.Floor(marginMm / 25.4 * 203);
        var labelTop = labelLeft;
        var labelRight = (int)Math.Ceiling((marginMm + template.WidthMm) / 25.4 * 203);
        var labelBottom = (int)Math.Ceiling((marginMm + template.HeightMm) / 25.4 * 203);
        for (var py = 0; py < heightPx; py++)
        {
            for (var px = 0; px < widthPx; px++)
            {
                var index = (py * widthPx + px) * 4;
                var dark = (pixels[index] + pixels[index + 1] + pixels[index + 2]) / 3 < 64;
                if (dark && (px < labelLeft || px >= labelRight || py < labelTop || py >= labelBottom))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static IEnumerable<Case> CreateCases()
    {
        // Toolbox defaults: zero-width outline, QR module 6 px at the object's 300 DPI.
        yield return new Case("QR 16 mm", () => new LabelObject
        {
            Id = "qr-16", Type = ObjectType.QRCode, Name = "QR", XMm = 5, YMm = 5, WidthMm = 16, HeightMm = 16,
            BindingExpression = "{Code}", Style = { BorderThicknessMm = 0 }
        }, BarcodeFormat.QR_CODE);
        yield return new Case("QR 20 mm", () => new LabelObject
        {
            Id = "qr-20", Type = ObjectType.QRCode, Name = "QR", XMm = 7.3, YMm = 4.1, WidthMm = 20, HeightMm = 20,
            BindingExpression = "{Code}", Style = { BorderThicknessMm = 0 }
        }, BarcodeFormat.QR_CODE);
        yield return new Case("Data Matrix 12 mm", () => new LabelObject
        {
            Id = "dm-12", Type = ObjectType.DataMatrix, Name = "DM", XMm = 5, YMm = 5, WidthMm = 12, HeightMm = 12,
            BindingExpression = "{Code}", Style = { BorderThicknessMm = 0 }
        }, BarcodeFormat.DATA_MATRIX);
        yield return new Case("Code 128 frame 45 mm", () => new LabelObject
        {
            Id = "c128-frame", Type = ObjectType.BarcodeCode128, Name = "C128", XMm = 5, YMm = 5, WidthMm = 45, HeightMm = 10,
            BindingExpression = "{Code}", BarcodeSymbology = BarcodeSymbology.Code128, ShowBarcodeText = false,
            Style = { BorderThicknessMm = 0 }
        }, BarcodeFormat.CODE_128);
        // SizedFromX: the frame was sized for a short sample, so the bound row
        // prints wider than the frame; and a frame wider than the row's symbol.
        yield return new Case("Code 128 X 0.25 mm past the frame", () => new LabelObject
        {
            Id = "c128-x-wide", Type = ObjectType.BarcodeCode128, Name = "C128 X", XMm = 3, YMm = 20, WidthMm = 20, HeightMm = 10,
            Text = "A1", BindingExpression = "{Code}", BarcodeSymbology = BarcodeSymbology.Code128, ShowBarcodeText = false,
            BarcodeWidthMode = BarcodeWidthMode.SizedFromX, BarcodeModuleWidthMm = 0.25, Style = { BorderThicknessMm = 0 }
        }, BarcodeFormat.CODE_128, LabelWidthMm: 90);
        yield return new Case("Code 128 X 0.25 mm inside the frame", () => new LabelObject
        {
            Id = "c128-x-narrow", Type = ObjectType.BarcodeCode128, Name = "C128 X", XMm = 3, YMm = 20, WidthMm = 75, HeightMm = 10,
            Text = "A1", BindingExpression = "{Code}", BarcodeSymbology = BarcodeSymbology.Code128, ShowBarcodeText = false,
            BarcodeWidthMode = BarcodeWidthMode.SizedFromX, BarcodeModuleWidthMm = 0.25, Style = { BorderThicknessMm = 0 }
        }, BarcodeFormat.CODE_128, LabelWidthMm: 90);
        yield return new Case("PDF417 40 mm", () => new LabelObject
        {
            Id = "pdf417", Type = ObjectType.BarcodeCode128, Name = "PDF417", XMm = 5, YMm = 5, WidthMm = 40, HeightMm = 14,
            BindingExpression = "{Code}", BarcodeSymbology = BarcodeSymbology.Pdf417, ShowBarcodeText = false,
            Style = { BorderThicknessMm = 0 }
        }, BarcodeFormat.PDF_417);
    }

    private static Measurement MeasureDesigner(LabelTemplate template, IReadOnlyDictionary<string, string> row)
    {
        var canvas = new LabelDesignerCanvas { Template = template, Zoom = 1, PreviewRow = row };
        var size = new Size(MmConverter.MmToDip(template.WidthMm), MmConverter.MmToDip(template.HeightMm));
        canvas.Measure(size);
        canvas.Arrange(new Rect(size));
        canvas.UpdateLayout();
        return Measure(canvas, template);
    }

    private static Measurement Measure(Visual visual, LabelTemplate template)
    {
        var widthPx = (int)Math.Ceiling(template.WidthMm / 25.4 * MeasureDpi);
        var heightPx = (int)Math.Ceiling(template.HeightMm / 25.4 * MeasureDpi);
        var bitmap = new RenderTargetBitmap(widthPx, heightPx, MeasureDpi, MeasureDpi, PixelFormats.Pbgra32);
        var background = new DrawingVisual();
        using (var context = background.RenderOpen())
        {
            context.DrawRectangle(Brushes.White, null, new Rect(0, 0, widthPx * 96.0 / MeasureDpi, heightPx * 96.0 / MeasureDpi));
        }

        bitmap.Render(background);
        bitmap.Render(visual);
        var pixels = new byte[widthPx * heightPx * 4];
        bitmap.CopyPixels(pixels, widthPx * 4, 0);

        var gray = new byte[widthPx * heightPx];
        int minX = int.MaxValue, minY = int.MaxValue, maxX = -1, maxY = -1;
        for (var y = 0; y < heightPx; y++)
        {
            for (var x = 0; x < widthPx; x++)
            {
                var index = (y * widthPx + x) * 4;
                var luminance = (pixels[index] + pixels[index + 1] + pixels[index + 2]) / 3;
                gray[y * widthPx + x] = (byte)luminance;
                // The designer also paints a light label outline and grid; only
                // near-black pixels belong to the symbol.
                if (luminance < 64)
                {
                    minX = Math.Min(minX, x);
                    minY = Math.Min(minY, y);
                    maxX = Math.Max(maxX, x);
                    maxY = Math.Max(maxY, y);
                }
            }
        }

        if (maxX < 0)
        {
            return new Measurement(0, 0, 0, 0, gray, pixels, widthPx, heightPx);
        }

        static double ToMm(int count) => count / (double)MeasureDpi * 25.4;
        return new Measurement(ToMm(minX), ToMm(minY), ToMm(maxX - minX + 1), ToMm(maxY - minY + 1), gray, pixels, widthPx, heightPx);
    }

    private static string Decode(Measurement measurement, BarcodeFormat format)
    {
        // A crop around the symbol with a white margin, as a scanner sees one code.
        var marginPx = (int)Math.Round(3 / 25.4 * MeasureDpi);
        var left = Math.Max(0, (int)Math.Floor(measurement.LeftMm / 25.4 * MeasureDpi) - marginPx);
        var top = Math.Max(0, (int)Math.Floor(measurement.TopMm / 25.4 * MeasureDpi) - marginPx);
        var right = Math.Min(measurement.Width, (int)Math.Ceiling((measurement.LeftMm + measurement.WidthMm) / 25.4 * MeasureDpi) + marginPx);
        var bottom = Math.Min(measurement.Height, (int)Math.Ceiling((measurement.TopMm + measurement.HeightMm) / 25.4 * MeasureDpi) + marginPx);
        if (right <= left || bottom <= top)
        {
            return string.Empty;
        }

        var cropWidth = right - left;
        var cropHeight = bottom - top;
        var crop = new byte[cropWidth * cropHeight];
        for (var y = 0; y < cropHeight; y++)
        {
            Array.Copy(measurement.Gray, (top + y) * measurement.Width + left, crop, y * cropWidth, cropWidth);
        }

        // ZXing.Net switches to its pure-barcode path when the PURE_BARCODE key is
        // present at all, whatever its value, so the key is never added here.
        var hints = new Dictionary<DecodeHintType, object>
        {
            [DecodeHintType.POSSIBLE_FORMATS] = new[] { format },
            [DecodeHintType.TRY_HARDER] = true
        };
        var dumpDirectory = Environment.GetEnvironmentVariable("ANLABEL_B06_DUMP");
        if (!string.IsNullOrWhiteSpace(dumpDirectory))
        {
            Directory.CreateDirectory(dumpDirectory);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(BitmapSource.Create(cropWidth, cropHeight, MeasureDpi, MeasureDpi, PixelFormats.Gray8, null, crop, cropWidth)));
            using var stream = File.Create(Path.Combine(dumpDirectory, $"{format}-{measurement.WidthMm:0.000}-{Guid.NewGuid():N}.png"));
            encoder.Save(stream);
        }

        var source = new RGBLuminanceSource(crop, cropWidth, cropHeight, RGBLuminanceSource.BitmapFormat.Gray8);
        try
        {
            return new MultiFormatReader().decode(new BinaryBitmap(new HybridBinarizer(source)), hints)?.Text ?? string.Empty;
        }
        catch (ReaderException)
        {
            return string.Empty;
        }
    }

    private static string Describe(Measurement measurement)
        => $"{measurement.WidthMm:0.###}x{measurement.HeightMm:0.###} mm at ({measurement.LeftMm:0.###}, {measurement.TopMm:0.###})";

    private static T RunOnSta<T>(Func<T> work)
    {
        T result = default!;
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                result = work();
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (failure is not null)
        {
            throw failure;
        }

        return result;
    }
}
