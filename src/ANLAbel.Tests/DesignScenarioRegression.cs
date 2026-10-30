using System.Reflection;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ANLAbel.Core.Enums;
using ANLAbel.Core.Geometry;
using ANLAbel.Core.Models;
using ANLAbel.Printing.PrinterProfiles;
using ANLAbel.Printing.RenderPipeline;
using ZXing;
using ZXing.Common;

/// <summary>
/// Design scenarios beyond one label per page (owner request 2026-10-30,
/// "nhiều trường hợp nhiều diễn biến thiết kế hơn"): sheets of labels in
/// every NiceLabel processing order, with margins, gaps, a turned page and a
/// last page that is not full; and objects turned 0/90/180/270°. Every page
/// comes from the print paginator and every code is decoded in the cell where
/// the processing order puts that data row, worked out here from the order's
/// definition rather than from the product's own placement code.
/// </summary>
internal static class DesignScenarioRegression
{
    private const int Dpi = 300;

    private sealed record SheetCase(
        string Id,
        double SheetWidthMm,
        double SheetHeightMm,
        double LabelWidthMm,
        double LabelHeightMm,
        double LeftMm,
        double TopMm,
        int Across,
        int Down,
        double HorizontalGapMm,
        double VerticalGapMm,
        bool DownFirst,
        bool FromRight,
        bool FromBottom,
        bool Rotated,
        int Rows,
        int Skip = 0);

    public static Task LabelSheetsFillEveryCellInProcessingOrder()
    {
        SheetCase[] cases =
        [
            // A4 sheet of 3 x 8 labels 70 x 37 mm; 26 rows leave 22 cells of page 2 empty.
            new("A", 210, 297, 70, 37, 0, 0.5, 3, 8, 0, 0, false, false, false, false, 26),
            new("B", 210, 297, 70, 37, 0, 0.5, 3, 8, 0, 0, true, true, false, false, 26),
            new("C", 210, 297, 99, 57, 4, 6, 2, 5, 3, 1.5, false, false, true, false, 13),
            // A5 sheet, 2 x 2 labels 60 x 90 mm with margins and gaps, printed turned 180°.
            new("D", 148, 210, 60, 90, 10, 10, 2, 2, 8, 8, true, false, true, true, 4),
            // A partly used A4 sheet: 5 cells already used (NiceLabel "Number of skipped labels").
            new("E", 210, 297, 70, 37, 0, 0.5, 3, 8, 0, 0, false, false, false, false, 26, Skip: 5),
            new("F", 148, 210, 60, 90, 10, 10, 2, 2, 8, 8, true, true, false, true, 5, Skip: 3)
        ];
        var failures = new List<string>();
        foreach (var sheet in cases)
        {
            RunOnSta(() => CheckSheet(sheet, failures));
        }

        if (failures.Count > 0)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, failures.Take(30)));
        }

        return Task.CompletedTask;
    }

    private static void CheckSheet(SheetCase sheet, List<string> failures)
    {
        var (template, rows) = CreateSheet(sheet);
        var plan = new PrintRenderPlan
        {
            Dpi = Dpi,
            DpiX = Dpi,
            DpiY = Dpi,
            LabelWidthMm = sheet.LabelWidthMm,
            LabelHeightMm = sheet.LabelHeightMm,
            OffsetXMm = sheet.LeftMm,
            OffsetYMm = sheet.TopMm,
            ScaleX = 1,
            ScaleY = 1,
            Rotated180 = sheet.Rotated
        };
        var paginator = (DocumentPaginator)typeof(PrintService)
            .GetMethod("CreateRowsPaginator", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(new PrintService(), new object?[] { template, rows, plan, sheet.Skip })!;
        var perPage = sheet.Across * sheet.Down;
        var pages = (sheet.Rows + sheet.Skip + perPage - 1) / perPage;
        if (paginator.PageCount != pages)
        {
            failures.Add($"Sheet {sheet.Id}: {sheet.Rows} rows at {perPage} per sheet need {pages} pages, the paginator made {paginator.PageCount}.");
            return;
        }

        for (var page = 0; page < pages; page++)
        {
            CheckSheetCells(sheet, Rasterize(paginator.GetPage(page)), page, "page", failures);
        }

        // Print Preview shows the sheet that carries a label, drawn as the paginator lays it out.
        foreach (var labelIndex in new[] { 0, sheet.Rows - 1 })
        {
            var preview = new PrintService().CreateSheetPreview(template, rows, labelIndex, plan, sheet.Skip);
            var position = labelIndex + sheet.Skip;
            if (preview.SheetIndex != position / perPage || preview.Cell != position % perPage || preview.SheetCount != pages)
            {
                failures.Add($"Sheet {sheet.Id}: label {labelIndex + 1} previews sheet {preview.SheetIndex + 1}/{preview.SheetCount} cell {preview.Cell + 1}, expected sheet {position / perPage + 1}/{pages} cell {position % perPage + 1}.");
                continue;
            }

            var visual = new DrawingVisual();
            using (var context = visual.RenderOpen())
            {
                context.DrawDrawing(preview.Drawing);
            }

            CheckSheetCells(sheet, Rasterize(visual, sheet.SheetWidthMm, sheet.SheetHeightMm), preview.SheetIndex, "preview of sheet", failures);
        }
    }

    /// <summary>A label with one Code 128 bound to {Ma}, laid out on the case's sheet, and its rows.</summary>
    private static (LabelTemplate Template, IReadOnlyDictionary<string, string>?[] Rows) CreateSheet(SheetCase sheet)
    {
        var template = new LabelTemplate { Name = "Sheet " + sheet.Id, WidthMm = sheet.LabelWidthMm, HeightMm = sheet.LabelHeightMm, Dpi = Dpi };
        template.Objects.Add(new LabelObject
        {
            Type = ObjectType.BarcodeCode128,
            Name = "Mã",
            BarcodeSymbology = BarcodeSymbology.Code128,
            BindingExpression = "{Ma}",
            XMm = 3,
            YMm = sheet.LabelHeightMm / 2 - 7,
            WidthMm = sheet.LabelWidthMm - 6,
            HeightMm = 14,
            BarcodeWidthMode = BarcodeWidthMode.SizedFromX,
            BarcodeModuleWidthMm = 0.254,
            QrQuietZoneModules = 10,
            ShowBarcodeText = true,
            Style = { BorderThicknessMm = 0 }
        });
        var profile = template.PrinterProfile;
        profile.SettingsSource = PrinterSettingsSource.Label;
        profile.PaperSizeSource = PaperSizeSource.Manual;
        profile.PhysicalWidthMm = sheet.SheetWidthMm;
        profile.PhysicalHeightMm = sheet.SheetHeightMm;
        profile.OffsetXMm = sheet.LeftMm;
        profile.OffsetYMm = sheet.TopMm;
        profile.LabelsAcross = sheet.Across;
        profile.LabelsDown = sheet.Down;
        profile.HorizontalGapMm = sheet.HorizontalGapMm;
        profile.VerticalGapMm = sheet.VerticalGapMm;
        profile.FillDownFirst = sheet.DownFirst;
        profile.StartFromRight = sheet.FromRight;
        profile.StartFromBottom = sheet.FromBottom;
        profile.Rotated180 = sheet.Rotated;
        var rows = Enumerable.Range(1, sheet.Rows)
            .Select(index => (IReadOnlyDictionary<string, string>?)new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Ma"] = $"{sheet.Id}-R{index:000}" })
            .ToArray();
        return (template, rows);
    }

    private static void CheckSheetCells(SheetCase sheet, Raster raster, int page, string what, List<string> failures)
    {
        var perPage = sheet.Across * sheet.Down;
        for (var slot = 0; slot < perPage; slot++)
        {
            var index = page * perPage + slot - sheet.Skip;
            var (column, row) = Cell(slot, sheet);
            var x = sheet.LeftMm + column * (sheet.LabelWidthMm + sheet.HorizontalGapMm);
            var y = sheet.TopMm + row * (sheet.LabelHeightMm + sheet.VerticalGapMm);
            if (sheet.Rotated)
            {
                x = sheet.SheetWidthMm - x - sheet.LabelWidthMm;
                y = sheet.SheetHeightMm - y - sheet.LabelHeightMm;
            }

            var decoded = Decode(raster, x, y, sheet.LabelWidthMm, sheet.LabelHeightMm, BarcodeFormat.CODE_128);
            var expected = index >= 0 && index < sheet.Rows ? $"{sheet.Id}-R{index + 1:000}" : string.Empty;
            if (decoded != expected)
            {
                failures.Add($"Sheet {sheet.Id} {what} {page + 1} cell column {column + 1} row {row + 1}: read '{decoded}', expected '{expected}'.");
            }
        }
    }

    /// <summary>
    /// NiceLabel processing order: across then down (or down then across),
    /// starting from the chosen corner.
    /// </summary>
    private static (int Column, int Row) Cell(int slot, SheetCase sheet)
    {
        var column = sheet.DownFirst ? slot / sheet.Down : slot % sheet.Across;
        var row = sheet.DownFirst ? slot % sheet.Down : slot / sheet.Across;
        return (sheet.FromRight ? sheet.Across - 1 - column : column, sheet.FromBottom ? sheet.Down - 1 - row : row);
    }

    public static Task PrintPreviewShowsSheetsAndSkipsUsedCells()
    {
        RunOnSta(() =>
        {
            var (template, rows) = CreateSheet(new SheetCase("P", 210, 297, 70, 37, 0, 0.5, 3, 8, 0, 0, false, false, false, false, 26));
            var vm = new ANLAbel.App.ViewModels.MainViewModel();
            var window = new ANLAbel.App.PrintPreviewWindow(template, rows[0], null, vm.PrintService, vm.PrintLogService, string.Empty, preparedRows: rows);
            try
            {
                // The preview prepares its labels on the dispatcher after construction.
                var deadline = DateTime.UtcNow.AddMinutes(2);
                while ((window.IsPreviewBusy || window.Pages.Count == 0) && DateTime.UtcNow < deadline)
                {
                    System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);
                    Thread.Sleep(5);
                }

                Require(window.IsSheetOfLabels, "A 3 x 8 sheet must preview as a sheet of labels.");
                Require(window.Pages.Count == 26 && Math.Abs(window.Pages[0].Width - MmConverter.MmToDip(210)) < 0.5,
                    $"Each label previews the whole A4 sheet it prints on: {window.Pages.Count} pages, first {window.Pages.FirstOrDefault()?.Width:0.#} x {window.Pages.FirstOrDefault()?.Height:0.#} DIP.");
                Require(window.PageStatusText == "Nhãn 1 / 26 · tờ 1 / 2, ô 1", "Status before skipping: " + window.PageStatusText);
                ((System.Windows.Controls.TextBox)window.FindName("SkipLabelsBox")).Text = "5";
                typeof(ANLAbel.App.PrintPreviewWindow).GetMethod("ApplySkipLabels", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(window, null);
                Require(window.PageStatusText == "Nhãn 1 / 26 · tờ 1 / 2, ô 6", "With 5 used cells the first label goes to cell 6: " + window.PageStatusText);
                Require(window.SheetLayoutText.Contains("26 tem in trên 2 tờ", StringComparison.Ordinal), window.SheetLayoutText);
            }
            finally
            {
                window.Close();
            }
        });

        return Task.CompletedTask;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    public static Task TurnedObjectsPrintReadable()
    {
        RunOnSta(() =>
        {
            // A 110 x 110 mm label with Code 128, QR and Data Matrix each turned 0/90/180/270°.
            var template = new LabelTemplate { Name = "Turned", WidthMm = 110, HeightMm = 110, Dpi = Dpi };
            var expectations = new List<(string Name, double Cx, double Cy, double W, double H, int Turn, BarcodeFormat Format, string Data)>();
            var turns = new[] { 0, 90, 180, 270 };
            for (var index = 0; index < turns.Length; index++)
            {
                var turn = turns[index];
                var cx = 28 + (index % 2) * 54;
                var cy = 28 + (index / 2) * 54;
                var linear = $"L{turn:000}-1234";
                template.Objects.Add(new LabelObject
                {
                    Type = ObjectType.BarcodeCode128,
                    Name = "Code 128 " + turn,
                    BarcodeSymbology = BarcodeSymbology.Code128,
                    BindingExpression = "{C" + turn + "}",
                    XMm = cx - 22,
                    YMm = cy - 6,
                    WidthMm = 44,
                    HeightMm = 12,
                    Rotation = turn,
                    QrQuietZoneModules = 10,
                    ShowBarcodeText = false,
                    Style = { BorderThicknessMm = 0 }
                });
                expectations.Add(("Code 128 " + turn, cx, cy, 44, 12, turn, BarcodeFormat.CODE_128, linear));
            }

            template.Objects.Add(new LabelObject
            {
                Type = ObjectType.QRCode,
                Name = "QR 90",
                BindingExpression = "{Q}",
                XMm = 40,
                YMm = 2,
                WidthMm = 14,
                HeightMm = 14,
                Rotation = 90,
                Style = { BorderThicknessMm = 0 }
            });
            expectations.Add(("QR 90", 47, 9, 14, 14, 90, BarcodeFormat.QR_CODE, "Mã QR xoay 90°"));
            template.Objects.Add(new LabelObject
            {
                Type = ObjectType.DataMatrix,
                Name = "DM 270",
                BindingExpression = "{D}",
                XMm = 40,
                YMm = 92,
                WidthMm = 14,
                HeightMm = 14,
                Rotation = 270,
                Style = { BorderThicknessMm = 0 }
            });
            expectations.Add(("DM 270", 47, 99, 14, 14, 270, BarcodeFormat.DATA_MATRIX, "DM-270-OK"));
            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Q"] = "Mã QR xoay 90°", ["D"] = "DM-270-OK" };
            foreach (var turn in turns)
            {
                row["C" + turn] = $"L{turn:000}-1234";
            }

            var plan = new PrintRenderPlan { Dpi = Dpi, DpiX = Dpi, DpiY = Dpi, LabelWidthMm = 110, LabelHeightMm = 110, ScaleX = 1, ScaleY = 1 };
            var raster = Rasterize(new LabelVisualRenderer().Render(template, row, plan), 110, 110);
            var failures = new List<string>();
            foreach (var (name, cx, cy, w, h, turn, format, data) in expectations)
            {
                // A quarter turn swaps the frame's width and height about its centre.
                var (bw, bh) = turn % 180 == 0 ? (w, h) : (h, w);
                var decoded = Decode(raster, cx - bw / 2, cy - bh / 2, bw, bh, format);
                if (decoded != data)
                {
                    failures.Add($"{name}: read '{decoded}', expected '{data}'.");
                }
            }

            if (failures.Count > 0)
            {
                throw new InvalidOperationException(string.Join(Environment.NewLine, failures));
            }
        });

        return Task.CompletedTask;
    }

    private sealed record Raster(byte[] Gray, int Width, int Height);

    private static Raster Rasterize(DocumentPage page)
        => Rasterize(page.Visual, MmConverter.DipToMm(page.Size.Width), MmConverter.DipToMm(page.Size.Height));

    private static Raster Rasterize(Visual visual, double widthMm, double heightMm)
    {
        var width = (int)Math.Ceiling(widthMm / 25.4 * Dpi);
        var height = (int)Math.Ceiling(heightMm / 25.4 * Dpi);
        var bitmap = new RenderTargetBitmap(width, height, Dpi, Dpi, PixelFormats.Pbgra32);
        var background = new DrawingVisual();
        using (var context = background.RenderOpen())
        {
            context.DrawRectangle(Brushes.White, null, new Rect(0, 0, MmConverter.MmToDip(widthMm), MmConverter.MmToDip(heightMm)));
        }

        bitmap.Render(background);
        bitmap.Render(visual);
        var pixels = new byte[width * height * 4];
        bitmap.CopyPixels(pixels, width * 4, 0);
        var gray = new byte[width * height];
        for (var index = 0; index < gray.Length; index++)
        {
            gray[index] = (byte)((pixels[index * 4] + pixels[index * 4 + 1] + pixels[index * 4 + 2]) / 3);
        }

        return new Raster(gray, width, height);
    }

    /// <summary>Decodes one rectangle (mm) of the page plus 1.5 mm around it.</summary>
    private static string Decode(Raster raster, double xMm, double yMm, double widthMm, double heightMm, BarcodeFormat format)
    {
        int Px(double mm) => (int)Math.Round(mm / 25.4 * Dpi);
        var x0 = Math.Max(0, Px(xMm - 1.5));
        var y0 = Math.Max(0, Px(yMm - 1.5));
        var x1 = Math.Min(raster.Width, Px(xMm + widthMm + 1.5));
        var y1 = Math.Min(raster.Height, Px(yMm + heightMm + 1.5));
        if (x1 <= x0 || y1 <= y0)
        {
            return "(outside the page)";
        }

        var w = x1 - x0;
        var h = y1 - y0;
        var crop = new byte[w * h];
        for (var y = 0; y < h; y++)
        {
            Array.Copy(raster.Gray, (y0 + y) * raster.Width + x0, crop, y * w, w);
        }

        var hints = new Dictionary<DecodeHintType, object>
        {
            [DecodeHintType.POSSIBLE_FORMATS] = new[] { format },
            [DecodeHintType.TRY_HARDER] = true
        };
        try
        {
            return new MultiFormatReader().decode(new BinaryBitmap(new HybridBinarizer(new RGBLuminanceSource(crop, w, h, RGBLuminanceSource.BitmapFormat.Gray8))), hints)?.Text ?? string.Empty;
        }
        catch (ReaderException)
        {
            return string.Empty;
        }
    }

    private static void RunOnSta(Action work)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                work();
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
    }
}
