using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ANLAbel.App.Controls;
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

                // The paginator renders each page with LabelVisualRenderer and
                // the effective plan; preview pages come from PrintService.
                var print = RunOnSta(() => Measure(new LabelVisualRenderer().Render(template, row, plan), template));
                var preview = RunOnSta(() => Measure(new PrintService().CreatePreviewPage(template, row, 1, plan).Visual, template));
                var designer = RunOnSta(() => MeasureDesigner(template, row));
                var label = $"{testCase.Name} @ {dpi} DPI";
                Console.WriteLine(
                    $"MEASURE B06 {label}: frame {item.WidthMm:0.##}x{item.HeightMm:0.##} mm; "
                    + $"print {Describe(print)}; designer {Describe(designer)}");

                if (!print.Raw.AsSpan().SequenceEqual(preview.Raw))
                {
                    failures.Add($"{label}: the preview page is not the drawing the paginator prints.");
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

        if (failures.Count > 0)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, failures));
        }

        return Task.CompletedTask;
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
