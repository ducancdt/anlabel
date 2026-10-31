using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ANLAbel.App.Controls;
using ANLAbel.Barcode.Renderers;
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

    /// <summary>
    /// Serial numbers on sheets of labels. A job of 26 labels on an A4 sheet of
    /// 3 x 8, filled down the columns from the right (NiceLabel processing
    /// order), numbers its labels S00101 to S00126 in that order over two
    /// sheets; with two labels per number and five cells already used, the
    /// pairs follow on from the sixth cell. Every cell is decoded where the
    /// processing order puts it.
    /// </summary>
    public static Task SerialNumbersFillASheetInProcessingOrder()
    {
        (SheetCase Sheet, int ChangeEvery)[] cases =
        [
            (new("S", 210, 297, 70, 37, 0, 0.5, 3, 8, 0, 0, true, true, false, false, 26), 1),
            (new("T", 210, 297, 70, 37, 0, 0.5, 3, 8, 0, 0, false, false, false, false, 12, Skip: 5), 2)
        ];
        var failures = new List<string>();
        foreach (var (sheet, changeEvery) in cases)
        {
            RunOnSta(() =>
            {
                var (template, _) = CreateSheet(sheet);
                template.Objects.Single().BindingExpression = "{#SERIAL}";
                template.SerialCounter = new SerialCounterDefinition { StartValue = 101, ChangeEvery = changeEvery, Digits = 5, Prefix = "S", MaximumValue = 99999, Enabled = true };
                if (!ANLAbel.Core.Printing.SerialCounterContract.TryApply(template.SerialCounter, 101, new IReadOnlyDictionary<string, string>?[sheet.Rows], out var rows, out var error))
                {
                    failures.Add($"Sheet {sheet.Id}: {error}");
                    return;
                }

                var plan = new PrintRenderPlan { Dpi = Dpi, DpiX = Dpi, DpiY = Dpi, LabelWidthMm = sheet.LabelWidthMm, LabelHeightMm = sheet.LabelHeightMm, OffsetXMm = sheet.LeftMm, OffsetYMm = sheet.TopMm, ScaleX = 1, ScaleY = 1 };
                var paginator = (DocumentPaginator)typeof(PrintService)
                    .GetMethod("CreateRowsPaginator", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(new PrintService(), new object?[] { template, rows, plan, sheet.Skip })!;
                var perPage = sheet.Across * sheet.Down;
                var pages = (sheet.Rows + sheet.Skip + perPage - 1) / perPage;
                if (paginator.PageCount != pages)
                {
                    failures.Add($"Sheet {sheet.Id}: {sheet.Rows} labels need {pages} sheets, the paginator made {paginator.PageCount}.");
                    return;
                }

                for (var page = 0; page < pages; page++)
                {
                    var raster = Rasterize(paginator.GetPage(page));
                    for (var slot = 0; slot < perPage; slot++)
                    {
                        var index = page * perPage + slot - sheet.Skip;
                        var (column, row) = Cell(slot, sheet);
                        var x = sheet.LeftMm + column * (sheet.LabelWidthMm + sheet.HorizontalGapMm);
                        var y = sheet.TopMm + row * (sheet.LabelHeightMm + sheet.VerticalGapMm);
                        var decoded = Decode(raster, x, y, sheet.LabelWidthMm, sheet.LabelHeightMm, BarcodeFormat.CODE_128);
                        var expected = index >= 0 && index < sheet.Rows ? $"S{101 + index / changeEvery:00000}" : string.Empty;
                        if (decoded != expected)
                        {
                            failures.Add($"Sheet {sheet.Id} page {page + 1} cell column {column + 1} row {row + 1}: read '{decoded}', expected '{expected}'.");
                        }
                    }
                }
            });
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

    /// <summary>
    /// "In" and "Đóng" sat at the end of the scrolling settings panel, so with
    /// the print answers, a merged-cell note and a list of warnings the operator
    /// scrolled to reach Print. They now stay in a footer under the panel.
    /// </summary>
    public static Task PrintPreviewKeepsPrintInView()
    {
        RunOnSta(() =>
        {
            var (template, rows) = CreateSheet(new SheetCase("P", 210, 297, 70, 37, 0, 0.5, 3, 8, 0, 0, false, false, false, false, 26));
            var vm = new ANLAbel.App.ViewModels.MainViewModel();
            var window = new ANLAbel.App.PrintPreviewWindow(template, rows[0], null, vm.PrintService, vm.PrintLogService, string.Empty, preparedRows: rows);
            try
            {
                // The window is not shown, so the search follows the logical tree.
                Button? Find(DependencyObject parent)
                {
                    foreach (var child in LogicalTreeHelper.GetChildren(parent).OfType<DependencyObject>())
                    {
                        if (child is Button button && AutomationProperties.GetAutomationId(button) == "PrintPreview.Print")
                        {
                            return button;
                        }

                        if (Find(child) is { } found)
                        {
                            return found;
                        }
                    }

                    return null;
                }

                var print = Find(window);
                Require(print is not null, "Print Preview has its Print button.");
                var path = new List<string>();
                for (var node = LogicalTreeHelper.GetParent(print!); node is not null; node = LogicalTreeHelper.GetParent(node))
                {
                    path.Add(node.GetType().Name);
                }

                Require(!path.Contains(nameof(ScrollViewer)), "Print stays in view, outside the scrolling settings: " + string.Join(" < ", path));
            }
            finally
            {
                window.Close();
            }
        });

        return Task.CompletedTask;
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

    /// <summary>
    /// NiceLabel 10 anchoring point for a bound X-sized code: a row whose data
    /// prints wider or narrower than the frame grows or shrinks away from the
    /// anchor, in print and in the designer, and preflight measures it where it
    /// prints. Three Code 128 frames sized for "12345" with left, centre and
    /// right anchors; rows "1" and "1234567890ABCDEFGH". Selecting a bound code
    /// keeps its frame for the designer's row instead of a placeholder "0".
    /// </summary>
    public static Task XSizedBarcodesGrowAroundTheirAnchor()
    {
        RunOnSta(() =>
        {
            const double moduleMm = 0.33;
            var renderer = new ZxingBarcodeRenderer();
            var template = new LabelTemplate { Name = "Anchors", WidthMm = 120, HeightMm = 70, Dpi = Dpi };
            var anchors = new[] { (ObjectAnchorPoint.TopLeft, 0.0, 10.0), (ObjectAnchorPoint.TopCenter, 0.5, 30.0), (ObjectAnchorPoint.TopRight, 1.0, 50.0) };
            foreach (var (anchor, _, top) in anchors)
            {
                var code = new LabelObject
                {
                    Type = ObjectType.BarcodeCode128,
                    BarcodeSymbology = BarcodeSymbology.Code128,
                    Name = anchor.ToString(),
                    BindingExpression = "{Ma}",
                    BarcodeModuleWidthMm = moduleMm,
                    BarcodeWidthMode = BarcodeWidthMode.SizedFromX,
                    BarcodeHriPlacement = BarcodeHriPlacement.None,
                    AnchorPoint = anchor,
                    YMm = top,
                    HeightMm = 8,
                    Style = { BorderThicknessMm = 0, OutlineStyle = OutlineStyle.None }
                };
                code.WidthMm = LinearBarcodeProductionWidth.ResolveSymbolWidthMm(code, renderer, Dpi, "12345");
                // The anchor sits at x = 60 mm whatever the anchor kind.
                code.XMm = 60 - code.WidthMm * ObjectAnchorContract.Fractions(anchor).X;
                template.Objects.Add(code);
            }

            var failures = new List<string>();
            var plan = new PrintRenderPlan { Dpi = Dpi, DpiX = Dpi, DpiY = Dpi, LabelWidthMm = template.WidthMm, LabelHeightMm = template.HeightMm };
            foreach (var value in new[] { "1", "1234567890ABCDEFGH" })
            {
                var row = new Dictionary<string, string> { ["Ma"] = value };
                var printed = Rasterize(new LabelVisualRenderer().Render(template, row, plan), template.WidthMm, template.HeightMm);
                var canvas = new LabelDesignerCanvas { Template = template, Zoom = 1, PreviewRow = row };
                var size = new Size(MmConverter.MmToDip(template.WidthMm), MmConverter.MmToDip(template.HeightMm));
                canvas.Measure(size);
                canvas.Arrange(new Rect(size));
                canvas.UpdateLayout();
                var designed = Rasterize(canvas, template.WidthMm, template.HeightMm);
                foreach (var (anchor, fraction, top) in anchors)
                {
                    var code = template.Objects.Single(item => item.Name == anchor.ToString());
                    var symbolMm = LinearBarcodeProductionWidth.ResolveSymbolWidthMm(code, renderer, Dpi, value);
                    // The symbol's bars sit inside its quiet zone; compare where the anchor
                    // point falls on the drawn bars, worked out from the anchor definition.
                    var band = new PictureCase(code.Name, PictureSizing.Proportional, false, false, 0, top, template.WidthMm, 8);
                    foreach (var (what, raster) in new[] { ("print", printed), ("designer", designed) })
                    {
                        var bars = Box(raster, band, 64);
                        var anchorOnBars = bars.X + bars.W * fraction;
                        if (bars.W <= 0 || Math.Abs(anchorOnBars - 60) > 0.6 + 2 * moduleMm)
                        {
                            failures.Add($"{anchor} '{value}' ({what}): bars {Show(bars)}, the anchor should stay at 60 mm (symbol {symbolMm:0.##} mm).");
                        }
                    }
                }

                // The centred code with the long row runs off neither edge; moved to x = 20 it
                // runs off the left edge, which only an anchored measurement sees.
                if (value.Length > 1)
                {
                    var centred = template.Objects.Single(item => item.Name == nameof(ObjectAnchorPoint.TopCenter));
                    var shifted = new LabelTemplate { Name = "Edge", WidthMm = 120, HeightMm = 70, Dpi = Dpi };
                    var copy = LabelObjectCloner.Clone(centred);
                    copy.XMm = 20 - copy.WidthMm / 2;
                    shifted.Objects.Add(copy);
                    var issues = new PrintPreflightValidator(renderer).Validate(shifted, new IReadOnlyDictionary<string, string>?[] { row }, Dpi).Issues;
                    if (!issues.Any(issue => issue.IsBlocking && issue.ObjectName == copy.Name))
                    {
                        failures.Add($"A centred code whose row prints {LinearBarcodeProductionWidth.ResolveSymbolWidthMm(copy, renderer, Dpi, value):0.#} mm wide around x = 20 mm must be reported running off the left edge.");
                    }
                }
            }

            // Selecting a bound code whose designer row is unknown keeps its frame.
            var viewModel = new ANLAbel.App.ViewModels.MainViewModel();
            var bound = new LabelObject { Type = ObjectType.BarcodeCode128, BarcodeSymbology = BarcodeSymbology.Code128, Name = "Bound", BindingExpression = "{?Lot}", XMm = 10, YMm = 10, WidthMm = 60, HeightMm = 12, BarcodeModuleWidthMm = 0.33, BarcodeWidthMode = BarcodeWidthMode.SizedFromX };
            viewModel.Template.Objects.Add(bound);
            viewModel.SelectedObject = bound;
            if (Math.Abs(bound.WidthMm - 60) > 0.01 || Math.Abs(bound.XMm - 10) > 0.01)
            {
                failures.Add($"Selecting a bound code with no designer value changed its frame to {bound.XMm:0.##} + {bound.WidthMm:0.##} mm.");
            }

            if (failures.Count > 0)
            {
                throw new InvalidOperationException(string.Join(Environment.NewLine, failures));
            }
        });

        return Task.CompletedTask;
    }

    /// <summary>
    /// NiceLabel 10 variable rules at print time: a pick list is a list to choose
    /// from, padding and a suffix show what will print, a value outside the list
    /// or with a refused character is turned back in Vietnamese, and the answer
    /// is kept as entered (the rules apply when it prints).
    /// </summary>
    public static Task PromptRulesGuideTheOperator()
    {
        RunOnSta(() =>
        {
            var prompts = new[]
            {
                new PromptVariableDefinition { Name = "Shift", Prompt = "Ca", PickList = "A\nB\nC" },
                new PromptVariableDefinition { Name = "PO", Prompt = "Số PO", MaxLength = 8, Padding = PromptPadding.Left, PadCharacter = "0", Suffix = "/26", AllowedCharacters = "0123456789" }
            };
            var window = (ANLAbel.App.PromptValuesWindow)Activator.CreateInstance(
                typeof(ANLAbel.App.PromptValuesWindow),
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new object[] { prompts, new Dictionary<string, string> { ["PO"] = "42" } },
                null)!;
            var panel = (StackPanel)window.Content;
            var list = panel.Children.OfType<ComboBox>().SingleOrDefault();
            var box = panel.Children.OfType<TextBox>().SingleOrDefault();
            Require(list is not null && list.Items.Count == 3, "A pick list is offered as a list of its 3 values.");
            Require(box is not null && box.Text == "42", "The last answer comes back in the text box.");
            var printed = panel.Children.OfType<TextBlock>().Select(text => text.Text).FirstOrDefault(text => text.StartsWith("Sẽ in:", StringComparison.Ordinal));
            Require(printed == "Sẽ in: 00000042/26", "Padding and the suffix show what will print: " + printed);

            void Accept() => typeof(ANLAbel.App.PromptValuesWindow).GetMethod("Accept", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(window, null);
            var problem = panel.Children.OfType<TextBlock>().Last(text => text.Foreground == Brushes.DarkRed);
            box!.Text = "4x2";
            list!.SelectedItem = "B";
            Accept();
            Require(problem.Text.Contains("không nhận ký tự “x”", StringComparison.Ordinal), "A refused character is turned back in Vietnamese: " + problem.Text);
            box.Text = "42";
            try
            {
                Accept();
            }
            catch (TargetInvocationException closing) when (closing.InnerException is InvalidOperationException)
            {
                // The window was never shown as a dialog, so DialogResult cannot close it; the values are set first.
            }
            Require(window.Values.TryGetValue("PO", out var po) && po == "42" && window.Values["Shift"] == "B", "The answers are kept as entered.");
            window.Close();

            // A date prompt offers a calendar in Vietnamese order, shows the printed format and keeps yyyy-MM-dd.
            var dates = new[] { new PromptVariableDefinition { Name = "Exp", Prompt = "Hạn dùng", DateFormat = "yyMMdd", DateInputFormat = "yyMMdd" } };
            var dateWindow = (ANLAbel.App.PromptValuesWindow)Activator.CreateInstance(
                typeof(ANLAbel.App.PromptValuesWindow),
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new object[] { dates, new Dictionary<string, string>() },
                null)!;
            var datePanel = (StackPanel)dateWindow.Content;
            var picker = datePanel.Children.OfType<DatePicker>().SingleOrDefault();
            Require(picker is not null && string.Equals(picker.Language.IetfLanguageTag, "vi-VN", StringComparison.OrdinalIgnoreCase), "A date prompt is a calendar in Vietnamese date order.");
            Require(picker!.Template?.FindName("PART_TextBox", picker) is System.Windows.Controls.Primitives.DatePickerTextBox { VerticalContentAlignment: VerticalAlignment.Center },
                "The date sits mid-height in its box like the text boxes around it.");
            picker!.SelectedDate = new DateTime(2026, 12, 31);
            var datePrinted = datePanel.Children.OfType<TextBlock>().Select(text => text.Text).FirstOrDefault(text => text.StartsWith("Sẽ in:", StringComparison.Ordinal));
            Require(datePrinted == "Sẽ in: 261231", "The date shows as it prints: " + datePrinted);
            void AcceptDate() => typeof(ANLAbel.App.PromptValuesWindow).GetMethod("Accept", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(dateWindow, null);
            var dateProblem = datePanel.Children.OfType<TextBlock>().Last(text => text.Foreground == Brushes.DarkRed);
            picker.SelectedDate = null;
            picker.Text = "ngày mai";
            AcceptDate();
            Require(dateProblem.Text.Contains("cần một ngày", StringComparison.Ordinal), "A text that is no date is turned back in Vietnamese: " + dateProblem.Text);
            picker.Text = "261231";
            try
            {
                AcceptDate();
            }
            catch (TargetInvocationException closing) when (closing.InnerException is InvalidOperationException)
            {
                // Never shown as a dialog; the values are set before DialogResult.
            }

            Require(dateWindow.Values.TryGetValue("Exp", out var exp) && exp == "2026-12-31", "A date typed in its input format is kept as yyyy-MM-dd: " + exp);
            dateWindow.Close();

            // A typo in the date box: the picker falls back to its last good date
            // when the box loses focus; the dialog must not take that old date.
            var typo = new[] { new PromptVariableDefinition { Name = "Exp", Prompt = "Hạn dùng", DateFormat = "dd/MM/yyyy" } };
            var typoWindow = (ANLAbel.App.PromptValuesWindow)Activator.CreateInstance(
                typeof(ANLAbel.App.PromptValuesWindow),
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new object[] { typo, new Dictionary<string, string> { ["Exp"] = "2026-12-31" } },
                null)!;
            var typoPanel = (StackPanel)typoWindow.Content;
            var typoPicker = typoPanel.Children.OfType<DatePicker>().Single();
            var dateBox = (System.Windows.Controls.Primitives.DatePickerTextBox)typoPicker.Template.FindName("PART_TextBox", typoPicker);
            void AcceptTypo() => typeof(ANLAbel.App.PromptValuesWindow).GetMethod("Accept", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(typoWindow, null);
            var typoProblem = typoPanel.Children.OfType<TextBlock>().Last(text => text.Foreground == Brushes.DarkRed);
            dateBox.Text = "31/13/2026";
            dateBox.RaiseEvent(new RoutedEventArgs(UIElement.LostFocusEvent, dateBox));
            AcceptTypo();
            Require(!typoWindow.Values.ContainsKey("Exp") && typoProblem.Text.Contains("cần một ngày", StringComparison.Ordinal),
                $"A mistyped date is turned back, not replaced by the last good date (kept '{(typoWindow.Values.TryGetValue("Exp", out var kept) ? kept : "")}', said '{typoProblem.Text}').");
            // The dialog idles between the operator's actions; the typo comes back into the box.
            System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);
            Require(dateBox.Text == "31/13/2026", $"The typo shows again beside the message (box '{dateBox.Text}').");
            dateBox.Text = "15/01/2027";
            dateBox.RaiseEvent(new RoutedEventArgs(UIElement.LostFocusEvent, dateBox));
            try
            {
                AcceptTypo();
            }
            catch (TargetInvocationException closing) when (closing.InnerException is InvalidOperationException)
            {
                // Never shown as a dialog; the values are set before DialogResult.
            }

            Require(typoWindow.Values.TryGetValue("Exp", out var fixedDate) && fixedDate == "2027-01-15", "The corrected date is taken: " + fixedDate);
            typoWindow.Close();

            // A scanner ends each scan with Enter: it moves to the next question,
            // and only the last question's Enter sends the dialog.
            var three = new[]
            {
                new PromptVariableDefinition { Name = "Lot", Prompt = "Số lô" },
                new PromptVariableDefinition { Name = "Qty", Prompt = "Số lượng" },
                new PromptVariableDefinition { Name = "Op", Prompt = "Người in" }
            };
            var threeWindow = (ANLAbel.App.PromptValuesWindow)Activator.CreateInstance(
                typeof(ANLAbel.App.PromptValuesWindow),
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new object[] { three, new Dictionary<string, string> { ["Qty"] = "50", ["Op"] = "An" } },
                null)!;
            var boxes = ((StackPanel)threeWindow.Content).Children.OfType<TextBox>().ToArray();
            bool Enter(TextBox box)
            {
                var press = new System.Windows.Input.KeyEventArgs(System.Windows.Input.Keyboard.PrimaryDevice, new TestPresentationSource(), 0, System.Windows.Input.Key.Enter)
                {
                    RoutedEvent = System.Windows.Input.Keyboard.PreviewKeyDownEvent
                };
                box.RaiseEvent(press);
                return press.Handled;
            }

            Require(boxes.Length == 3 && Enter(boxes[0]) && Enter(boxes[1]) && !Enter(boxes[2]),
                "Enter moves on from the first two questions and is left to the default button on the last.");
            threeWindow.Close();

            // A scan longer than the maximum is turned back, never cut to fit.
            var lots = new[] { new PromptVariableDefinition { Name = "Lot", Prompt = "Số lô", MaxLength = 8 } };
            var lotWindow = (ANLAbel.App.PromptValuesWindow)Activator.CreateInstance(
                typeof(ANLAbel.App.PromptValuesWindow),
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new object[] { lots, new Dictionary<string, string>() },
                null)!;
            var lotPanel = (StackPanel)lotWindow.Content;
            var lotBox = lotPanel.Children.OfType<TextBox>().Single();
            var lotProblem = lotPanel.Children.OfType<TextBlock>().Last(text => text.Foreground == Brushes.DarkRed);
            // Typed character by character, as a keyboard-wedge scanner sends it
            // (setting Text in code would bypass any MaxLength on the box).
            foreach (var character in "LOT123456")
            {
                var composition = new System.Windows.Input.TextComposition(System.Windows.Input.InputManager.Current, lotBox, character.ToString());
                lotBox.RaiseEvent(new System.Windows.Input.TextCompositionEventArgs(System.Windows.Input.Keyboard.PrimaryDevice, composition) { RoutedEvent = UIElement.TextInputEvent });
            }
            try
            {
                typeof(ANLAbel.App.PromptValuesWindow).GetMethod("Accept", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(lotWindow, null);
            }
            catch (TargetInvocationException closing) when (closing.InnerException is InvalidOperationException)
            {
                // Accepted (the value was cut to fit): the window was never shown, so it cannot close.
            }
            Require(lotBox.Text == "LOT123456" && !lotWindow.Values.ContainsKey("Lot") && lotProblem.Text.Contains("tối đa 8 ký tự, đang nhập 9", StringComparison.Ordinal),
                $"A 9-character scan into an 8-character prompt is refused as typed (box '{lotBox.Text}', said '{lotProblem.Text}').");
            lotWindow.Close();

            // A minimum length (NiceLabel 10 input rule) is shown and turns a short value back.
            var parts = new[] { new PromptVariableDefinition { Name = "Part", Prompt = "Mã hàng", MinLength = 6, MaxLength = 10 } };
            var partWindow = (ANLAbel.App.PromptValuesWindow)Activator.CreateInstance(
                typeof(ANLAbel.App.PromptValuesWindow),
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new object[] { parts, new Dictionary<string, string>() },
                null)!;
            var partPanel = (StackPanel)partWindow.Content;
            Require(partPanel.Children.OfType<TextBlock>().Any(text => text.Text == "Mã hàng (bắt buộc, từ 6 đến 10 ký tự)"), "The prompt says how long the value must be: " + string.Join(" | ", partPanel.Children.OfType<TextBlock>().Select(text => text.Text)));
            void AcceptPart() => typeof(ANLAbel.App.PromptValuesWindow).GetMethod("Accept", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(partWindow, null);
            var partProblem = partPanel.Children.OfType<TextBlock>().Last(text => text.Foreground == Brushes.DarkRed);
            var partBox = partPanel.Children.OfType<TextBox>().Single();
            partBox.Text = "AB12";
            AcceptPart();
            Require(partProblem.Text.Contains("cần ít nhất 6 ký tự", StringComparison.Ordinal), "A value scanned short is turned back in Vietnamese: " + partProblem.Text);
            partBox.Text = "AB1234";
            try
            {
                AcceptPart();
            }
            catch (TargetInvocationException closing) when (closing.InnerException is InvalidOperationException)
            {
                // Never shown as a dialog; the values are set before DialogResult.
            }

            Require(partWindow.Values.TryGetValue("Part", out var part) && part == "AB1234", "A value long enough is kept.");
            partWindow.Close();
        });

        return Task.CompletedTask;
    }

    /// <summary>
    /// NiceLabel 10 Data explorer drag and drop: a field dropped on the label
    /// becomes a Text bound to it at the drop point, a prompt dropped on an
    /// object binds that object, Ctrl adds a new Text even over an object, and
    /// each drop is one undo step. The canvas finds the topmost object under
    /// the drop point.
    /// </summary>
    public static Task DataSourcesDropOnTheLabel()
    {
        RunOnSta(() =>
        {
            var viewModel = new ANLAbel.App.ViewModels.MainViewModel();
            var box = new LabelObject { Type = ObjectType.TextBox, Name = "Hộp", Text = "Ô chữ", XMm = 30, YMm = 20, WidthMm = 30, HeightMm = 8, ZIndex = 1 };
            viewModel.Template.Objects.Add(box);

            viewModel.DropDataSource("{Lot}", 12.5, 6, target: null, newObject: false);
            var lot = viewModel.Template.Objects.LastOrDefault();
            Require(lot is not null && lot.Type == ObjectType.Text && lot.BindingExpression == "{Lot}" && lot.Name == "Lot", "A field dropped on the label becomes a Text bound to it.");
            Require(Math.Abs(lot!.XMm - 12.5) < 0.001 && Math.Abs(lot.YMm - 6) < 0.001, $"The Text's top-left corner is the drop point ({lot.XMm:0.##}, {lot.YMm:0.##}).");

            viewModel.DropDataSource("{?PO}", 35, 22, box, newObject: false);
            Require(box.BindingExpression == "{?PO}" && viewModel.Template.Objects.Count == 2, "A prompt dropped on an object binds that object.");
            viewModel.UndoCommand.Execute(null);
            // Undo restores a copy of the template; find the box again.
            box = viewModel.Template.Objects.Single(item => item.Name == "Hộp");
            Require(box.BindingExpression.Length == 0 && viewModel.Template.Objects.Count == 2, "Undo takes back the binding in one step.");
            viewModel.DropDataSource("{?PO}", 35, 22, box, newObject: true);
            Require(viewModel.Template.Objects.Count == 3 && box.BindingExpression.Length == 0, "Ctrl adds a new Text even over an object.");

            var canvas = new LabelDesignerCanvas { Template = viewModel.Template, Zoom = 2 };
            // (32, 21) mm lies in the box only; the Ctrl-added Text starts at (35, 22).
            var target = canvas.DataSourceDropTarget(new Point(MmConverter.MmToDip(32) * 2, MmConverter.MmToDip(21) * 2));
            Require(ReferenceEquals(target, box), "The canvas finds the object under the drop point at any zoom.");
            Require(canvas.DataSourceDropTarget(new Point(MmConverter.MmToDip(90) * 2, MmConverter.MmToDip(50) * 2)) is null, "An empty spot has no target.");
        });

        return Task.CompletedTask;
    }

    /// <summary>
    /// A NiceLabel 10 table label as customers bring them: captions in a left
    /// column with an open frame box drawn over them, a Code 128 bound to an
    /// Excel field with NiceLabel's default text below the bars, and a free
    /// text bound to another field whose stored contents are the placeholder
    /// "Text". Opened, linked to an Excel sheet and printed row by row: the
    /// captions print, each row's code decodes, the bars keep NiceLabel's
    /// height, the field text is sized from NiceLabel's field sample and
    /// prints, and preflight lets every row print.
    /// </summary>
    public static async Task ImportedTableLabelPrintsEveryRow(Func<IReadOnlyDictionary<string, string>, byte[]> buildPackage)
    {
        const string partId = "a1a1a1a1-0000-4000-8000-000000000001";
        const string lotId = "a1a1a1a1-0000-4000-8000-000000000002";
        var directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "anlabel-table-" + Guid.NewGuid().ToString("N")));
        try
        {
            var formatXml = $"""
                <?xml version="1.0" encoding="utf-8"?>
                <EuroPlus.NiceLabel Type="Format">
                  <Name>Table</Name>
                  <Media Type="FormatMedia"><Width>100000</Width><Height>60000</Height></Media>
                  <DocumentItems>
                    <Item Type="TextDocumentItem">
                      <Name>CaptionPart</Name>
                      <FontDescriptor Type="FontDescriptor"><Name>Arial</Name><Height>12</Height></FontDescriptor>
                      <Geometry Type="PositionGeometry"><X>3000</X><Y>8000</Y></Geometry>
                      <FixedContents>Part No</FixedContents>
                      <ZOrder>1</ZOrder>
                    </Item>
                    <Item Type="TextDocumentItem">
                      <Name>CaptionLot</Name>
                      <FontDescriptor Type="FontDescriptor"><Name>Arial</Name><Height>12</Height></FontDescriptor>
                      <Geometry Type="PositionGeometry"><X>3000</X><Y>38000</Y></Geometry>
                      <FixedContents>Lot</FixedContents>
                      <ZOrder>2</ZOrder>
                    </Item>
                    <Item Type="RectangleDocumentItem">
                      <Name>Frame</Name>
                      <Geometry Type="RectGeometry"><Left>1000</Left><Top>1000</Top><Width>98000</Width><Height>58000</Height></Geometry>
                      <Thickness>400</Thickness>
                      <FillStyle>0</FillStyle>
                      <FillColor>FF000000</FillColor>
                      <ZOrder>3</ZOrder>
                    </Item>
                    <Item Type="LineDocumentItem">
                      <Name>Rule</Name>
                      <Geometry Type="PointGeometry"><StartPointX>1000</StartPointX><StartPointY>30000</StartPointY><EndPointX>99000</EndPointX><EndPointY>30000</EndPointY></Geometry>
                      <Thickness>300</Thickness>
                      <ZOrder>4</ZOrder>
                    </Item>
                    <Item Type="LineDocumentItem">
                      <Name>Column</Name>
                      <Geometry Type="PointGeometry"><StartPointX>22000</StartPointX><StartPointY>1000</StartPointY><EndPointX>22000</EndPointX><EndPointY>59000</EndPointY></Geometry>
                      <Thickness>300</Thickness>
                      <ZOrder>5</ZOrder>
                    </Item>
                    <Item Type="BarcodeDocumentItem">
                      <Name>PartCode</Name>
                      <Geometry Type="PositionGeometry"><X>25000</X><Y>5000</Y></Geometry>
                      <BarcodeData Type="Code128BarcodeData"><BaseBarWidth>330</BaseBarWidth><ModuleHeight>9000</ModuleHeight></BarcodeData>
                      <FixedContents>PN-SAMPLE</FixedContents>
                      <DataSourceReference Type="SolutionDataSourceReference"><Id>{partId}</Id></DataSourceReference>
                      <ZOrder>6</ZOrder>
                    </Item>
                    <Item Type="TextDocumentItem">
                      <Name>LotText</Name>
                      <FontDescriptor Type="FontDescriptor"><Name>Arial</Name><Height>12</Height></FontDescriptor>
                      <Geometry Type="PositionGeometry"><X>25000</X><Y>38000</Y></Geometry>
                      <FixedContents>Text</FixedContents>
                      <DataSourceReference Type="SolutionDataSourceReference"><Id>{lotId}</Id></DataSourceReference>
                      <ZOrder>7</ZOrder>
                    </Item>
                  </DocumentItems>
                </EuroPlus.NiceLabel>
                """;
            var solutionXml = $"""
                <?xml version="1.0" encoding="utf-8"?>
                <EuroPlus.NiceLabel Type="Solution">
                  <Variables></Variables>
                  <Databases>
                    <Item Type="OleDb">
                      <Name>Excel</Name>
                      <DataSetCollection>
                        <Item Type="OleDBDatabaseDataSet">
                          <Name>Parts$</Name>
                          <FieldCollection>
                            <Item Type="DBField"><Id>{partId}</Id><Name>Parts$.PartNo</Name><SampleValue Type="StringContents"><StringValue>PN-SAMPLE</StringValue></SampleValue><FieldName>PartNo</FieldName></Item>
                            <Item Type="DBField"><Id>{lotId}</Id><Name>Parts$.Lot</Name><SampleValue Type="StringContents"><StringValue>LOT-2026-0001</StringValue></SampleValue><FieldName>Lot</FieldName></Item>
                          </FieldCollection>
                        </Item>
                      </DataSetCollection>
                    </Item>
                  </Databases>
                </EuroPlus.NiceLabel>
                """;
            var nlblPath = Path.Combine(directory.FullName, "Table.nlbl");
            await File.WriteAllBytesAsync(nlblPath, buildPackage(new Dictionary<string, string>
            {
                ["Formats/label"] = formatXml,
                ["label.slnx"] = solutionXml
            }));
            var excelPath = Path.Combine(directory.FullName, "parts.xlsx");
            var rowsIn = new[] { ("PN-100-AB", "LOT-0042"), ("PN-2026-00042", "LOT-2026-0099"), ("X7", "L1") };
            using (var workbook = new ClosedXML.Excel.XLWorkbook())
            {
                var sheet = workbook.AddWorksheet("Parts");
                sheet.Cell(1, 1).Value = "PartNo";
                sheet.Cell(1, 2).Value = "Lot";
                for (var index = 0; index < rowsIn.Length; index++)
                {
                    sheet.Cell(index + 2, 1).Value = rowsIn[index].Item1;
                    sheet.Cell(index + 2, 2).Value = rowsIn[index].Item2;
                }

                workbook.SaveAs(excelPath);
            }

            var viewModel = new ANLAbel.App.ViewModels.MainViewModel();
            await viewModel.OpenAsync(nlblPath);
            await viewModel.ImportExcelAsync(excelPath, "Parts");
            var template = viewModel.Template;
            var rows = viewModel.ExcelDataView!.Cast<System.Data.DataRowView>()
                .Select(view => (IReadOnlyDictionary<string, string>)view.Row.Table.Columns.Cast<System.Data.DataColumn>()
                    .ToDictionary(column => column.ColumnName, column => view.Row[column]?.ToString() ?? string.Empty, StringComparer.OrdinalIgnoreCase))
                .ToArray();
            Require(rows.Length == rowsIn.Length, $"The Excel sheet links {rows.Length} rows.");

            RunOnSta(() =>
            {
                var failures = new List<string>();
                var frame = template.Objects.Single(item => item.Name == "Frame");
                var code = template.Objects.Single(item => item.Name == "PartCode");
                var lot = template.Objects.Single(item => item.Name == "LotText");
                if (frame.Style.FillStyle != FillStyle.None)
                {
                    failures.Add("The open NiceLabel frame imported filled.");
                }

                var renderer = new ZxingBarcodeRenderer();
                var plan = new PrintRenderPlan { Dpi = Dpi, DpiX = Dpi, DpiY = Dpi, LabelWidthMm = template.WidthMm, LabelHeightMm = template.HeightMm };
                var sampleWidth = new ANLAbel.App.Services.NiceLabel10TextMeasurer().MeasureText(lot, "LOT-2026-0001")!.Value.WidthMm;
                if (lot.WidthMm < sampleWidth * 0.95)
                {
                    failures.Add($"The field text is {lot.WidthMm:0.#} mm wide, not the {sampleWidth:0.#} mm of NiceLabel's field sample.");
                }

                foreach (var row in rows)
                {
                    var raster = Rasterize(new LabelVisualRenderer().Render(template, row, plan), template.WidthMm, template.HeightMm);
                    foreach (var caption in template.Objects.Where(item => item.Name.StartsWith("Caption", StringComparison.Ordinal)))
                    {
                        if (Ink(raster, caption.XMm, caption.YMm, caption.WidthMm, caption.HeightMm) < 30)
                        {
                            failures.Add($"Row {row["PartNo"]}: caption {caption.Name} does not print.");
                        }
                    }

                    var widthMm = LinearBarcodeProductionWidth.ResolveSymbolWidthMm(code, renderer, Dpi, row["PartNo"]);
                    var decoded = Decode(raster, code.XMm, code.YMm, widthMm, code.HeightMm, BarcodeFormat.CODE_128);
                    if (decoded != row["PartNo"])
                    {
                        failures.Add($"Row {row["PartNo"]}: the code reads '{decoded}'.");
                    }

                    var layout = BarcodeHriTextLayout.Measure(BarcodeType.Code128, row["PartNo"], widthMm, code.HeightMm,
                        code.BarcodeHriPlacement, code.BarcodeTextFontSizePt, code.BarcodeHriFontFamily, code.BarcodeHriBold);
                    if (!layout.IsValid || Math.Abs(layout.SymbolHeightMm - 9) > 0.01)
                    {
                        failures.Add($"Row {row["PartNo"]}: the bars are {layout.SymbolHeightMm:0.##} mm, not NiceLabel's 9 mm ({layout.ErrorMessage}).");
                    }

                    if (Ink(raster, lot.XMm, lot.YMm, lot.WidthMm, lot.HeightMm) < 30)
                    {
                        failures.Add($"Row {row["PartNo"]}: the lot text does not print.");
                    }
                }

                var blocking = new PrintPreflightValidator(renderer).Validate(template, rows, Dpi).Issues.Where(issue => issue.IsBlocking).ToArray();
                failures.AddRange(blocking.Select(issue => $"Preflight blocks {issue.ObjectName}: {issue.Message}"));
                if (failures.Count > 0)
                {
                    throw new InvalidOperationException(string.Join(Environment.NewLine, failures));
                }
            });
        }
        finally
        {
            try
            {
                directory.Delete(recursive: true);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    /// <summary>
    /// Variants chosen by the data, as NiceLabel 10 layers do: a QR code and a
    /// Code 128 share one place on the label; each prints only on rows whose
    /// Type is (or is not) "qr", compared ignoring case. Every row prints
    /// exactly the variant it asks for, it decodes, and preflight, which
    /// checks only what a row prints, blocks no row.
    /// </summary>
    public static Task RowsChooseThePrintedVariant()
    {
        RunOnSta(() =>
        {
            var template = new LabelTemplate { Name = "Variants", WidthMm = 60, HeightMm = 40, Dpi = Dpi };
            const string isQr = "EQUALS(FIELD(\"Type\"), \"qr\")";
            template.Objects.Add(new LabelObject
            {
                Type = ObjectType.QRCode,
                Name = "QR variant",
                BindingExpression = "{Code}",
                XMm = 5,
                YMm = 5,
                WidthMm = 22,
                HeightMm = 22,
                VisibilityCondition = isQr,
                ZIndex = 1
            });
            template.Objects.Add(new LabelObject
            {
                Type = ObjectType.BarcodeCode128,
                BarcodeSymbology = BarcodeSymbology.Code128,
                Name = "Linear variant",
                BindingExpression = "{Code}",
                BarcodeModuleWidthMm = 0.33,
                BarcodeWidthMode = BarcodeWidthMode.SizedFromX,
                BarcodeHriPlacement = BarcodeHriPlacement.None,
                XMm = 5,
                YMm = 5,
                WidthMm = 50,
                HeightMm = 12,
                VisibilityCondition = "NOT(" + isQr + ")",
                ZIndex = 2
            });

            var rows = new IReadOnlyDictionary<string, string>[]
            {
                new Dictionary<string, string> { ["Type"] = "QR", ["Code"] = "A-0001" },
                new Dictionary<string, string> { ["Type"] = "Code 128", ["Code"] = "B-0002" },
                new Dictionary<string, string> { ["Type"] = "qr", ["Code"] = "C-0003" }
            };
            var failures = new List<string>();
            var plan = new PrintRenderPlan { Dpi = Dpi, DpiX = Dpi, DpiY = Dpi, LabelWidthMm = template.WidthMm, LabelHeightMm = template.HeightMm };
            foreach (var row in rows)
            {
                var raster = Rasterize(new LabelVisualRenderer().Render(template, row, plan), template.WidthMm, template.HeightMm);
                var wantsQr = string.Equals(row["Type"], "qr", StringComparison.OrdinalIgnoreCase);
                var qr = Decode(raster, 5, 5, 22, 22, BarcodeFormat.QR_CODE);
                var linear = Decode(raster, 5, 5, 50, 12, BarcodeFormat.CODE_128);
                if ((wantsQr ? qr : linear) != row["Code"] || (wantsQr ? linear : qr).Length > 0)
                {
                    failures.Add($"Row {row["Code"]} ({row["Type"]}): QR '{qr}', Code 128 '{linear}'.");
                }
            }

            var blocking = new PrintPreflightValidator(new ZxingBarcodeRenderer()).Validate(template, rows, Dpi).Issues.Where(issue => issue.IsBlocking).ToArray();
            failures.AddRange(blocking.Select(issue => $"Preflight blocks {issue.ObjectName}: {issue.Message}"));
            if (failures.Count > 0)
            {
                throw new InvalidOperationException(string.Join(Environment.NewLine, failures));
            }
        });

        return Task.CompletedTask;
    }

    /// <summary>
    /// A GS1 carton label whose expiry date comes from a date prompt (NiceLabel
    /// 10 Date variable printed yyMMdd): GTIN and batch come from the data
    /// rows, the operator picks the date once for the job, every label's
    /// GS1-128 decodes with AI 17 = that date, the human date shows
    /// dd/MM/yyyy, and preflight blocks no row.
    /// </summary>
    public static Task CartonLabelTakesItsExpiryFromADatePrompt()
    {
        RunOnSta(() =>
        {
            var template = new LabelTemplate { Name = "Carton", WidthMm = 100, HeightMm = 60, Dpi = Dpi };
            template.PromptVariables.Add(new PromptVariableDefinition { Name = "Exp", Prompt = "Hạn dùng", DateFormat = "yyMMdd" });
            template.PromptVariables.Add(new PromptVariableDefinition { Name = "ExpText", Prompt = "Hạn dùng (chữ)", DateFormat = "dd/MM/yyyy", Required = false });
            var code = new LabelObject
            {
                Type = ObjectType.BarcodeCode128,
                BarcodeSymbology = BarcodeSymbology.Code128,
                BarcodeApplicationProfile = BarcodeApplicationProfile.Gs1,
                Name = "SSCC carton",
                BindingExpression = "(01){GTIN}(17){?Exp}(10){Lot}",
                BarcodeModuleWidthMm = 0.33,
                BarcodeWidthMode = BarcodeWidthMode.SizedFromX,
                QrQuietZoneModules = 10,
                BarcodeHriPlacement = BarcodeHriPlacement.None,
                XMm = 5,
                YMm = 5,
                WidthMm = 90,
                HeightMm = 25,
                ZIndex = 1
            };
            template.Objects.Add(code);
            template.Objects.Add(new LabelObject
            {
                Type = ObjectType.Text,
                Name = "Expiry",
                BindingExpression = "{?ExpText}",
                XMm = 5,
                YMm = 40,
                WidthMm = 40,
                HeightMm = 8,
                Style = { FontSizePt = 12, BorderThicknessMm = 0, TextSizing = TextSizingMode.AutoFit },
                ZIndex = 2
            });

            IReadOnlyDictionary<string, string>?[] rows =
            [
                new Dictionary<string, string> { ["GTIN"] = "09501101530003", ["Lot"] = "LOT42" },
                new Dictionary<string, string> { ["GTIN"] = "09501101530003", ["Lot"] = "B7" }
            ];
            var answers = new Dictionary<string, string> { ["Exp"] = "2026-12-31", ["ExpText"] = "2026-12-31" };
            Require(ANLAbel.Core.Printing.JobVariableContract.TryApply(template, rows, DateTimeOffset.Now, answers, out var applied, out var error), error);

            var failures = new List<string>();
            var renderer = new ZxingBarcodeRenderer();
            var plan = new PrintRenderPlan { Dpi = Dpi, DpiX = Dpi, DpiY = Dpi, LabelWidthMm = template.WidthMm, LabelHeightMm = template.HeightMm };
            foreach (var row in applied)
            {
                var raster = Rasterize(new LabelVisualRenderer().Render(template, row, plan), template.WidthMm, template.HeightMm);
                var data = "(01)" + row!["GTIN"] + "(17)261231(10)" + row["Lot"];
                var widthMm = LinearBarcodeProductionWidth.ResolveSymbolWidthMm(code, renderer, Dpi, data);
                var decoded = Decode(raster, code.XMm, code.YMm, widthMm, code.HeightMm, BarcodeFormat.CODE_128).Replace("\u001D", string.Empty, StringComparison.Ordinal);
                if (decoded != data.Replace("(", string.Empty, StringComparison.Ordinal).Replace(")", string.Empty, StringComparison.Ordinal))
                {
                    failures.Add($"Lot {row["Lot"]}: the GS1-128 reads '{decoded}'.");
                }

                if (row["?ExpText"] != "31/12/2026")
                {
                    failures.Add($"Lot {row["Lot"]}: the human date is '{row["?ExpText"]}'.");
                }
            }

            var blocking = new PrintPreflightValidator(renderer).Validate(template, applied, Dpi).Issues.Where(issue => issue.IsBlocking).ToArray();
            failures.AddRange(blocking.Select(issue => $"Preflight blocks {issue.ObjectName}: {issue.Message}"));
            if (failures.Count > 0)
            {
                throw new InvalidOperationException(string.Join(Environment.NewLine, failures));
            }
        });

        return Task.CompletedTask;
    }

    /// <summary>
    /// A NiceLabel serial label (a counter variable "0001" with prefix "SN-")
    /// opened in ANLAbel numbers every label: the counter becomes the label's
    /// serial counter, one job of five labels scans SN-0001 to SN-0005, the next
    /// job continues at SN-0006 and a reprint of the first job finds its own
    /// numbers. Reservations go to a store of this test only.
    /// </summary>
    public static async Task ImportedSerialLabelNumbersEveryLabel(Func<IReadOnlyDictionary<string, string>, byte[]> buildPackage)
    {
        const string counterId = "b2b2b2b2-0000-4000-8000-000000000001";
        var directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "anlabel-serial-" + Guid.NewGuid().ToString("N")));
        try
        {
            var formatXml = $"""
                <?xml version="1.0" encoding="utf-8"?>
                <EuroPlus.NiceLabel Type="Format">
                  <Name>Serial</Name>
                  <Media Type="FormatMedia"><Width>60000</Width><Height>30000</Height></Media>
                  <DocumentItems>
                    <Item Type="BarcodeDocumentItem">
                      <Name>SerialCode</Name>
                      <Geometry Type="PositionGeometry"><X>4000</X><Y>4000</Y></Geometry>
                      <BarcodeData Type="Code128BarcodeData"><BaseBarWidth>330</BaseBarWidth><ModuleHeight>12000</ModuleHeight></BarcodeData>
                      <FixedContents>0001</FixedContents>
                      <DataSourceReference Type="SolutionDataSourceReference"><Id>{counterId}</Id></DataSourceReference>
                    </Item>
                  </DocumentItems>
                </EuroPlus.NiceLabel>
                """;
            var solutionXml = $"""
                <?xml version="1.0" encoding="utf-8"?>
                <EuroPlus.NiceLabel Type="Solution">
                  <Variables>
                    <Item Type="Variable">
                      <Id>{counterId}</Id>
                      <Name>varCounter</Name>
                      <DefaultValue Type="StringContents"><StringValue>0001</StringValue></DefaultValue>
                      <IncrementType>1</IncrementType>
                      <PromptText>Start counter Value</PromptText>
                      <Constraints Type="DataSourceConstraints"><Length>4</Length><FixedLength>True</FixedLength></Constraints>
                      <Prefix>SN-</Prefix>
                    </Item>
                  </Variables>
                </EuroPlus.NiceLabel>
                """;
            var nlblPath = Path.Combine(directory.FullName, "Serial.nlbl");
            await File.WriteAllBytesAsync(nlblPath, buildPackage(new Dictionary<string, string>
            {
                ["Formats/label"] = formatXml,
                ["label.slnx"] = solutionXml
            }));

            var viewModel = new ANLAbel.App.ViewModels.MainViewModel();
            await viewModel.OpenAsync(nlblPath);
            var template = viewModel.Template;
            var counter = template.SerialCounter;
            var code = template.Objects.Single(item => item.Name == "SerialCode");
            Require(ANLAbel.Core.Printing.SerialCounterContract.IsActive(template), "The NiceLabel counter did not become the serial counter.");
            Require(code.BindingExpression == "{#SERIAL}", $"The code binds '{code.BindingExpression}'.");
            Require(template.PromptVariables.Count == 0, "The counter still asks for a value at print.");

            var store = new ANLAbel.Data.PrintLogs.SerialCounterStore(Path.Combine(directory.FullName, "reservations.jsonl"));
            var failures = new List<string>();
            RunOnSta(() =>
            {
                var renderer = new ZxingBarcodeRenderer();
                var plan = new PrintRenderPlan { Dpi = Dpi, DpiX = Dpi, DpiY = Dpi, LabelWidthMm = template.WidthMm, LabelHeightMm = template.HeightMm };
                foreach (var (job, labels, expectedFirst) in new[] { ("job-1", 5, 1L), ("job-2", 3, 6L) })
                {
                    var first = store.PeekNextValue(counter);
                    if (first != expectedFirst || !store.TryReserve(counter, job, first, labels, out _, out var reserveError))
                    {
                        failures.Add($"{job}: the next number is {first}, expected {expectedFirst}.");
                        continue;
                    }

                    if (!ANLAbel.Core.Printing.SerialCounterContract.TryApply(counter, first, new IReadOnlyDictionary<string, string>?[labels], out var rows, out var applyError))
                    {
                        failures.Add($"{job}: {applyError}");
                        continue;
                    }

                    for (var index = 0; index < rows.Length; index++)
                    {
                        var expected = $"SN-{first + index:0000}";
                        var raster = Rasterize(new LabelVisualRenderer().Render(template, rows[index], plan), template.WidthMm, template.HeightMm);
                        var widthMm = LinearBarcodeProductionWidth.ResolveSymbolWidthMm(code, renderer, Dpi, expected);
                        var decoded = Decode(raster, code.XMm, code.YMm, widthMm, code.HeightMm, BarcodeFormat.CODE_128);
                        if (decoded != expected)
                        {
                            failures.Add($"{job} label {index + 1}: the code reads '{decoded}', expected '{expected}'.");
                        }
                    }

                    var blocking = new PrintPreflightValidator(renderer).Validate(template, rows, Dpi).Issues.Where(issue => issue.IsBlocking);
                    failures.AddRange(blocking.Select(issue => $"{job}: preflight blocks {issue.ObjectName}: {issue.Message}"));
                }

                if (store.FindReservation(counter.CounterId, "job-1") is not { FirstValue: 1, LabelCount: 5 })
                {
                    failures.Add("A reprint of the first job does not find its own numbers.");
                }
            });

            if (failures.Count > 0)
            {
                throw new InvalidOperationException(string.Join(Environment.NewLine, failures));
            }
        }
        finally
        {
            try
            {
                directory.Delete(recursive: true);
            }
            catch (IOException)
            {
            }
        }
    }

    /// <summary>
    /// Pharmaceutical serialization in a GS1 DataMatrix: GTIN (01) and lot (10)
    /// from the Excel row, expiry (17) from a date prompt and serial (21) from
    /// the serial counter. Every pack decodes to its own element string, the
    /// lot (variable length) is closed by the GS separator before the serial,
    /// the symbol starts with FNC1 (]d2: a GS1 DataMatrix, not plain text), the
    /// serials count up and preflight lets the job print.
    /// </summary>
    public static Task PharmaPacksCarryGs1DataMatrixSerials()
    {
        RunOnSta(() =>
        {
            var template = new LabelTemplate { Name = "Pack", WidthMm = 50, HeightMm = 30, Dpi = Dpi };
            template.PromptVariables.Add(new PromptVariableDefinition { Name = "Exp", Prompt = "Hạn dùng", DateFormat = "yyMMdd" });
            template.SerialCounter = new SerialCounterDefinition { StartValue = 1001, Digits = 8, MaximumValue = 99_999_999, Enabled = true };
            var code = new LabelObject
            {
                Type = ObjectType.DataMatrix,
                Name = "GS1 DataMatrix",
                BarcodeApplicationProfile = BarcodeApplicationProfile.Gs1,
                BindingExpression = "(01){GTIN}(17){?Exp}(10){Lot}(21){#SERIAL}",
                XMm = 5,
                YMm = 5,
                WidthMm = 20,
                HeightMm = 20,
                Style = { BorderThicknessMm = 0 },
                ZIndex = 1
            };
            template.Objects.Add(code);
            (string Gtin, string Lot)[] packs = [("09506000134352", "A1B2"), ("09501101530003", "LOT-2026-10"), ("04895123000107", "7")];
            var rows = packs
                .Select(pack => (IReadOnlyDictionary<string, string>?)new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["GTIN"] = pack.Gtin, ["Lot"] = pack.Lot })
                .ToArray();
            Require(ANLAbel.Core.Printing.JobVariableContract.TryApply(template, rows, DateTimeOffset.Now, new Dictionary<string, string> { ["Exp"] = "2027-06-30" }, out var answered, out var promptError), promptError);
            Require(ANLAbel.Core.Printing.SerialCounterContract.TryApply(template.SerialCounter, 1001, answered, out var labels, out var serialError), serialError);

            var failures = new List<string>();
            var plan = new PrintRenderPlan { Dpi = Dpi, DpiX = Dpi, DpiY = Dpi, LabelWidthMm = template.WidthMm, LabelHeightMm = template.HeightMm };
            for (var index = 0; index < labels.Length; index++)
            {
                var expected = "01" + packs[index].Gtin + "17270630" + "10" + packs[index].Lot + "\u001D" + "21" + (1001 + index).ToString("00000000", System.Globalization.CultureInfo.InvariantCulture);
                var raster = Rasterize(new LabelVisualRenderer().Render(template, labels[index], plan), template.WidthMm, template.HeightMm);
                var (decoded, symbology) = DecodeWithSymbology(raster, code.XMm, code.YMm, code.WidthMm, code.HeightMm, BarcodeFormat.DATA_MATRIX);
                if (decoded != expected)
                {
                    failures.Add($"Pack {index + 1}: the DataMatrix reads '{decoded.Replace("\u001D", "<GS>", StringComparison.Ordinal)}', expected '{expected.Replace("\u001D", "<GS>", StringComparison.Ordinal)}'.");
                }

                // A GS1 scanner knows a GS1 DataMatrix by its leading FNC1 (]d2).
                if (symbology != "]d2")
                {
                    failures.Add($"Pack {index + 1}: the symbol reads as {symbology}, not as a GS1 DataMatrix (]d2) that starts with FNC1.");
                }
            }

            var blocking = new PrintPreflightValidator(new ZxingBarcodeRenderer()).Validate(template, labels, Dpi).Issues.Where(issue => issue.IsBlocking);
            failures.AddRange(blocking.Select(issue => $"Preflight blocks {issue.ObjectName}: {issue.Message}"));
            if (failures.Count > 0)
            {
                throw new InvalidOperationException(string.Join(Environment.NewLine, failures));
            }
        });

        return Task.CompletedTask;
    }

    /// <summary>
    /// A GS1 logistics label numbers its cartons: the SSCC (AI 00) is the
    /// extension digit and company prefix, the serial counter's nine digits and
    /// the GS1 check digit. Three cartons scan as three consecutive SSCCs whose
    /// check digits a scanner would accept, the human line shows the same
    /// number, and preflight lets them print.
    /// </summary>
    public static Task SsccCartonsNumberThemselves()
    {
        RunOnSta(() =>
        {
            var template = new LabelTemplate { Name = "SSCC", WidthMm = 100, HeightMm = 50, Dpi = Dpi };
            template.SerialCounter = new SerialCounterDefinition { StartValue = 41, Digits = 9, MaximumValue = 999_999_999, Enabled = true };
            var code = new LabelObject
            {
                Type = ObjectType.BarcodeCode128,
                BarcodeSymbology = BarcodeSymbology.Code128,
                BarcodeApplicationProfile = BarcodeApplicationProfile.Gs1,
                Name = "SSCC",
                BindingExpression = "CONCAT(\"(00)\", GS1CHECKDIGIT(CONCAT(\"09501101\", FIELD(\"#SERIAL\"))))",
                BarcodeModuleWidthMm = 0.33,
                BarcodeWidthMode = BarcodeWidthMode.SizedFromX,
                QrQuietZoneModules = 10,
                BarcodeHriPlacement = BarcodeHriPlacement.Below,
                BarcodeTextFontSizePt = 9,
                XMm = 6,
                YMm = 6,
                WidthMm = 88,
                HeightMm = 30,
                ZIndex = 1
            };
            template.Objects.Add(code);
            if (!ANLAbel.Core.Printing.SerialCounterContract.TryApply(template.SerialCounter, 41, new IReadOnlyDictionary<string, string>?[3], out var rows, out var error))
            {
                throw new InvalidOperationException(error);
            }

            var failures = new List<string>();
            var renderer = new ZxingBarcodeRenderer();
            var plan = new PrintRenderPlan { Dpi = Dpi, DpiX = Dpi, DpiY = Dpi, LabelWidthMm = template.WidthMm, LabelHeightMm = template.HeightMm };
            for (var index = 0; index < rows.Length; index++)
            {
                var body = "09501101" + (41 + index).ToString("000000000", System.Globalization.CultureInfo.InvariantCulture);
                var sum = body.Select((digit, position) => (digit - '0') * ((body.Length - position) % 2 == 1 ? 3 : 1)).Sum();
                var sscc = "00" + body + (char)('0' + (10 - sum % 10) % 10);
                var visual = new LabelVisualRenderer().Render(template, rows[index], plan);
                var raster = Rasterize(visual, template.WidthMm, template.HeightMm);
                var widthMm = LinearBarcodeProductionWidth.ResolveSymbolWidthMm(code, renderer, Dpi, "(00)" + sscc[2..]);
                var decoded = Decode(raster, code.XMm, code.YMm, widthMm, code.HeightMm, BarcodeFormat.CODE_128).Replace("\u001D", string.Empty, StringComparison.Ordinal);
                if (decoded != sscc)
                {
                    failures.Add($"Carton {index + 1}: the SSCC reads '{decoded}', expected '{sscc}'.");
                }

                var line = new string(DrawnCharacters(visual.Drawing).Where(char.IsAsciiDigit).ToArray());
                if (line != sscc)
                {
                    failures.Add($"Carton {index + 1}: the human line shows the digits '{line}', not the SSCC '{sscc}'.");
                }
            }

            var blocking = new PrintPreflightValidator(renderer).Validate(template, rows, Dpi).Issues.Where(issue => issue.IsBlocking);
            failures.AddRange(blocking.Select(issue => $"Preflight blocks {issue.ObjectName}: {issue.Message}"));
            if (failures.Count > 0)
            {
                throw new InvalidOperationException(string.Join(Environment.NewLine, failures));
            }
        });

        return Task.CompletedTask;
    }

    /// <summary>
    /// EAN-13, EAN-8, UPC-A and UPC-E typed without their check digit, and a
    /// UPC-E as its six digits: the print path encodes the whole number (number
    /// system 0 for the six-digit UPC-E, as NiceLabel 10 does), and the digits
    /// drawn under the bars are exactly the digits the scanner reads.
    /// </summary>
    public static Task RetailCodesPrintTheNumberTheScannerReads()
    {
        RunOnSta(() =>
        {
            var failures = new List<string>();
            var renderer = new ZxingBarcodeRenderer();
            (BarcodeSymbology Symbology, BarcodeFormat Format, string Data, string Read)[] cases =
            [
                (BarcodeSymbology.Ean13, BarcodeFormat.EAN_13, "590123412345", "5901234123457"),
                (BarcodeSymbology.Ean8, BarcodeFormat.EAN_8, "9638507", "96385074"),
                (BarcodeSymbology.UpcA, BarcodeFormat.UPC_A, "03600029145", "036000291452"),
                (BarcodeSymbology.UpcE, BarcodeFormat.UPC_E, "123456", "01234565")
            ];
            foreach (var (symbology, format, data, read) in cases)
            {
                var template = new LabelTemplate { Name = "Retail", WidthMm = 60, HeightMm = 35, Dpi = Dpi };
                var code = new LabelObject
                {
                    Type = ObjectType.BarcodeCode128,
                    BarcodeSymbology = symbology,
                    Name = symbology.ToString(),
                    Text = data,
                    BarcodeModuleWidthMm = 0.33,
                    BarcodeWidthMode = BarcodeWidthMode.SizedFromX,
                    BarcodeHriPlacement = BarcodeHriPlacement.Below,
                    BarcodeTextFontSizePt = 9,
                    XMm = 6,
                    YMm = 4,
                    WidthMm = 40,
                    HeightMm = 26,
                    ZIndex = 1
                };
                template.Objects.Add(code);
                var plan = new PrintRenderPlan { Dpi = Dpi, DpiX = Dpi, DpiY = Dpi, LabelWidthMm = template.WidthMm, LabelHeightMm = template.HeightMm };
                var visual = new LabelVisualRenderer().Render(template, null, plan);
                var raster = Rasterize(visual, template.WidthMm, template.HeightMm);
                var widthMm = LinearBarcodeProductionWidth.ResolveSymbolWidthMm(code, renderer, Dpi, data);
                var decoded = Decode(raster, code.XMm, code.YMm, widthMm, code.HeightMm, format);
                var drawn = DrawnCharacters(visual.Drawing);
                if (decoded != read)
                {
                    failures.Add($"{symbology} '{data}': the scanner reads '{decoded}', expected '{read}'.");
                }

                if (drawn != decoded)
                {
                    failures.Add($"{symbology} '{data}': the line under the bars says '{drawn}' but the scanner reads '{decoded}'.");
                }

                var blocking = new PrintPreflightValidator(renderer).Validate(template, [null], Dpi).Issues.Where(issue => issue.IsBlocking);
                failures.AddRange(blocking.Select(issue => $"{symbology} '{data}': preflight blocks it: {issue.Message}"));
            }

            if (failures.Count > 0)
            {
                throw new InvalidOperationException(string.Join(Environment.NewLine, failures));
            }
        });

        return Task.CompletedTask;
    }

    /// <summary>Characters of every glyph run in a drawing, in drawing order.</summary>
    private static string DrawnCharacters(Drawing? drawing)
    {
        var text = new StringBuilder();
        void Visit(Drawing? current)
        {
            switch (current)
            {
                case DrawingGroup group:
                    foreach (var child in group.Children)
                    {
                        Visit(child);
                    }

                    break;
                case GlyphRunDrawing glyphs when glyphs.GlyphRun.Characters is { } characters:
                    text.Append(characters.ToArray());
                    break;
            }
        }

        Visit(drawing);
        return text.ToString();
    }

    /// <summary>
    /// Print Preview lists each row's values without column names; hovering a
    /// row names them ("Ma: SP-0001", "Ten: ..."), so two codes side by side can
    /// be told apart before choosing what to print.
    /// </summary>
    public static Task PreviewRowsNameTheirFields()
    {
        RunOnSta(() =>
        {
            var template = new LabelTemplate { Name = "Hàng", WidthMm = 60, HeightMm = 30, Dpi = 203 };
            template.PrinterProfile.Dpi = 203;
            template.Objects.Add(new LabelObject { Type = ObjectType.Text, Name = "Tên", BindingExpression = "{Ten}", XMm = 3, YMm = 3, WidthMm = 40, HeightMm = 6, Style = { BorderThicknessMm = 0 } });
            IReadOnlyDictionary<string, string>?[] rows =
            [
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Ma"] = "SP-0001", ["Ten"] = "Bu lông M8x40" },
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Ma"] = "SP-0002", ["Ten"] = "Đai ốc M8" }
            ];
            var window = new ANLAbel.App.PrintPreviewWindow(
                template,
                rows[0],
                null,
                new PrintService(),
                new ANLAbel.Data.PrintLogs.PrintLogService(Path.Combine(Path.GetTempPath(), $"anlabel-rows-{Guid.NewGuid():N}.csv")),
                string.Empty,
                preparedRows: rows);
            var deadline = DateTime.UtcNow.AddMinutes(1);
            while ((window.IsPreviewBusy || window.Pages.Count == 0) && DateTime.UtcNow < deadline)
            {
                System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);
                Thread.Sleep(5);
            }

            var list = (ListBox)window.FindName("TrackingList");
            var details = list.Items.Cast<ANLAbel.App.ViewModels.TrackingRowViewModel>().Select(row => row.DataDetails).ToArray();
            Require(details.SequenceEqual(["Ma: SP-0001\nTen: Bu lông M8x40", "Ma: SP-0002\nTen: Đai ốc M8"]),
                "Each listed row names its fields: " + string.Join(" | ", details));
            window.Close();
        });

        return Task.CompletedTask;
    }

    /// <summary>
    /// Print Preview's row filter as an operator uses it: scanning a product
    /// code selects only the row whose cell is that code (not SP-00010 for
    /// SP-0001), a partial value still selects every row containing it, and
    /// clearing the filter selects all rows again; the prepared labels follow
    /// the selection.
    /// </summary>
    public static Task PreviewFilterSelectsTheScannedRow()
    {
        RunOnSta(() =>
        {
            var template = new LabelTemplate { Name = "Hàng", WidthMm = 60, HeightMm = 30, Dpi = 203 };
            template.PrinterProfile.Dpi = 203;
            template.Objects.Add(new LabelObject { Type = ObjectType.Text, Name = "Mã", BindingExpression = "{Ma}", XMm = 3, YMm = 3, WidthMm = 40, HeightMm = 6, Style = { BorderThicknessMm = 0 } });
            IReadOnlyDictionary<string, string>?[] rows =
            [
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Ma"] = "SP-0001", ["Ten"] = "Bu lông M8" },
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Ma"] = "SP-00010", ["Ten"] = "Bu lông M10" },
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Ma"] = "SP-0002", ["Ten"] = "Đai ốc M8" }
            ];
            var window = new ANLAbel.App.PrintPreviewWindow(
                template,
                rows[0],
                null,
                new PrintService(),
                new ANLAbel.Data.PrintLogs.PrintLogService(Path.Combine(Path.GetTempPath(), $"anlabel-filter-{Guid.NewGuid():N}.csv")),
                string.Empty,
                preparedRows: rows);
            void Settle()
            {
                var deadline = DateTime.UtcNow.AddSeconds(30);
                do
                {
                    System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);
                    Thread.Sleep(20);
                }
                while (window.IsPreviewBusy && DateTime.UtcNow < deadline);
            }

            Settle();
            var filter = (TextBox)window.FindName("RowFilterBox");
            var list = (ListBox)window.FindName("TrackingList");
            string[] Selected() => list.Items.Cast<ANLAbel.App.ViewModels.TrackingRowViewModel>().Where(row => row.IsSelected).Select(row => row.Col1 ?? string.Empty).ToArray();
            void Search(string text)
            {
                // Typing filters the list at once; Enter prepares the selected labels.
                filter.Text = text;
                typeof(ANLAbel.App.PrintPreviewWindow).GetMethod("RefreshPreviewPagesOnly", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(window, null);
                Settle();
            }

            Search("SP-0001");
            Require(Selected().SequenceEqual(["SP-0001"]) && window.Pages.Count == 1,
                $"Scanning SP-0001 selects that row only (selected {string.Join(", ", Selected())}; {window.Pages.Count} labels prepared).");
            Search("m8");
            Require(Selected().SequenceEqual(["SP-0001", "SP-0002"]) && window.Pages.Count == 2,
                $"A partial value selects every row containing it (selected {string.Join(", ", Selected())}; {window.Pages.Count} labels prepared).");
            Search(string.Empty);
            Require(Selected().Length == 3 && window.Pages.Count == 3, $"Clearing the filter selects every row again ({Selected().Length} selected, {window.Pages.Count} prepared).");
            window.Close();
        });

        return Task.CompletedTask;
    }

    /// <summary>
    /// The Data Manager's prompt editor follows the kind of value (NiceLabel 10
    /// variable data type): a text prompt shows the text rules; choosing Ngày
    /// gives it a date format and shows the date format instead; back to Chữ
    /// the date format is cleared.
    /// </summary>
    public static Task PromptEditorFollowsTheKindOfValue()
    {
        RunOnSta(() =>
        {
            var viewModel = new ANLAbel.App.ViewModels.MainViewModel();
            viewModel.Template.PromptVariables.Add(new PromptVariableDefinition { Name = "Lot", Prompt = "Số lô" });
            var window = new ANLAbel.App.DataManagerWindow(viewModel);
            var drafts = (System.Collections.IList)typeof(ANLAbel.App.DataManagerWindow).GetField("_prompts", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(window)!;
            var draft = (PromptVariableDefinition)drafts[0]!;
            typeof(ANLAbel.App.DataManagerWindow).GetMethod("RebuildTree", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(window, new object[] { draft });
            System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);

            T Find<T>(string automationId) where T : FrameworkElement
                => FindDescendants<T>((DependencyObject)window.Content).Single(element => AutomationProperties.GetAutomationId(element) == automationId);
            var kindText = Find<RadioButton>("DataManager.Prompt.KindText");
            var kindDate = Find<RadioButton>("DataManager.Prompt.KindDate");
            var dateBlock = (FrameworkElement)Find<ComboBox>("DataManager.Prompt.DateFormat").Parent;
            var textRules = (FrameworkElement)Find<TextBox>("DataManager.Prompt.AllowedCharacters").Parent;
            Require(kindText.IsChecked == true && dateBlock.Visibility == Visibility.Collapsed && textRules.Visibility == Visibility.Visible,
                "A text prompt shows its text rules and no date format.");
            var minimum = Find<TextBox>("DataManager.Prompt.MinLength");
            var maximum = Find<TextBox>("DataManager.Prompt.MaxLength");
            Require(ReferenceEquals(minimum.Parent, maximum.Parent) && ReferenceEquals(((FrameworkElement)minimum.Parent).Parent, textRules),
                "The minimum and maximum length share one row of the text rules.");
            minimum.Text = "4";
            minimum.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();
            Require(draft.MinLength == 4, $"The minimum length is stored on the prompt ({draft.MinLength}).");

            // The output rules: labelled prefix and suffix, the pad character only with padding.
            var editorTexts = FindDescendants<TextBlock>((DependencyObject)window.Content).Select(text => text.Text).ToHashSet();
            Require(editorTexts.IsSupersetOf(["Quy tắc nhập", "Khi in", "Tiền tố", "Hậu tố"]), "The rules are grouped and the prefix and suffix boxes are labelled.");
            var padCharacter = Find<TextBox>("DataManager.Prompt.PadCharacter");
            Require(!padCharacter.IsEnabled, "Without padding there is no pad character to type.");
            draft.Padding = PromptPadding.Left;
            System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);
            Require(padCharacter.IsEnabled, "With padding the pad character can be typed.");
            draft.Padding = PromptPadding.None;

            kindDate.IsChecked = true;
            System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);
            Require(draft.DateFormat == "dd/MM/yyyy" && dateBlock.Visibility == Visibility.Visible && textRules.Visibility == Visibility.Collapsed,
                $"Choosing Ngày gives the prompt a date format ({draft.DateFormat}) and shows it instead of the text rules.");

            kindText.IsChecked = true;
            System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);
            Require(draft.DateFormat.Length == 0 && textRules.Visibility == Visibility.Visible, "Back to Chữ the date format is cleared.");
            // Not closed: closing an edited draft asks to keep or discard it.
        });

        return Task.CompletedTask;
    }

    /// <summary>
    /// Renaming a print question in the Data Manager carries the objects that
    /// use it, as NiceLabel (which binds a variable by id): a text "Lô: {?Lot}"
    /// and a barcode formula reading FIELD("?Lot") follow the new name, so the
    /// print dialog asks the renamed question with its rules (digits only, at
    /// most 8) instead of a bare "Lot" without them. The editor says so before
    /// Đồng ý, and Ctrl+Z restores the name and both objects.
    /// </summary>
    public static Task PromptRenameCarriesItsObjects()
    {
        RunOnSta(() =>
        {
            var viewModel = new ANLAbel.App.ViewModels.MainViewModel();
            viewModel.Template.PromptVariables.Add(new PromptVariableDefinition { Name = "Lot", Prompt = "Số lô", MaxLength = 8, DigitsOnly = true, Required = true });
            viewModel.Template.Objects.Add(new LabelObject { Type = ObjectType.Text, Name = "Lô", BindingExpression = "Lô: {?Lot}", XMm = 3, YMm = 3, WidthMm = 30, HeightMm = 6 });
            viewModel.Template.Objects.Add(new LabelObject { Type = ObjectType.BarcodeCode128, Name = "Mã lô", BindingExpression = "CONCAT(\"L\", FIELD(\"?Lot\"))", XMm = 3, YMm = 12, WidthMm = 40, HeightMm = 10 });
            var window = new ANLAbel.App.DataManagerWindow(viewModel);
            var drafts = (System.Collections.IList)typeof(ANLAbel.App.DataManagerWindow).GetField("_prompts", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(window)!;
            var draft = (PromptVariableDefinition)drafts[0]!;
            typeof(ANLAbel.App.DataManagerWindow).GetMethod("RebuildTree", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(window, new object[] { draft });
            System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);

            draft.Name = "SoLo";
            System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);
            var usage = ((TextBlock)window.FindName("PromptUsageText")).Text;
            Require(usage.Contains("Đổi tên từ {?Lot}", StringComparison.Ordinal) && usage.Contains("2 đối tượng (Lô, Mã lô)", StringComparison.Ordinal) && usage.Contains("{?SoLo}", StringComparison.Ordinal),
                "Before Đồng ý the editor says which objects move to the new name: " + usage);

            typeof(ANLAbel.App.DataManagerWindow).GetMethod("Ok_Click", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(window, new object?[] { null, null });
            string[] Bindings() => viewModel.Template.Objects.Select(item => item.BindingExpression).ToArray();
            Require(Bindings().SequenceEqual(["Lô: {?SoLo}", "CONCAT(\"L\", FIELD(\"?SoLo\"))"]),
                "Both objects use the renamed question: " + string.Join(" | ", Bindings()));
            var asked = ANLAbel.Core.Printing.JobVariableContract.RequiredPrompts(viewModel.Template);
            Require(asked.Count == 1 && asked[0] is { Name: "SoLo", MaxLength: 8, DigitsOnly: true, Required: true },
                "The print dialog asks the renamed question with its rules: " + string.Join(", ", asked.Select(prompt => $"{prompt.Name} max {prompt.MaxLength}")));
            Require(viewModel.StatusText.Contains("2 đối tượng dùng tên mới", StringComparison.Ordinal), "The status line says the objects moved: " + viewModel.StatusText);

            viewModel.UndoCommand.Execute(null);
            Require(Bindings().SequenceEqual(["Lô: {?Lot}", "CONCAT(\"L\", FIELD(\"?Lot\"))"]) && viewModel.Template.PromptVariables.Single().Name == "Lot",
                "Ctrl+Z restores the name and both objects: " + string.Join(" | ", Bindings()));
        });

        return Task.CompletedTask;
    }

    /// <summary>
    /// Fields given at print time are not broken links, and functions read the
    /// serial. With a sheet linked, a formula reading a question or the date,
    /// {#SERIAL} and a formula reading it were listed as broken bindings
    /// ("Field '?Lot' was not found"); a Data Manager function SN reading
    /// {#SERIAL} printed nothing. Now the designer row and every printed row
    /// carry SN with that label's number, the function editor shows a sample,
    /// and {#SERIAL} is still reported when the counter is off.
    /// </summary>
    public static Task PrintTimeFieldsAreNotBrokenLinks()
    {
        var directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "anlabel-print-time-" + Guid.NewGuid().ToString("N")));
        try
        {
            RunOnSta(() => PrintTimeFieldsAreNotBrokenLinksOnSta(directory.FullName));
        }
        finally
        {
            try
            {
                directory.Delete(recursive: true);
            }
            catch (IOException)
            {
            }
        }

        return Task.CompletedTask;
    }

    private static void PrintTimeFieldsAreNotBrokenLinksOnSta(string directory)
    {
        {
            var csv = Path.Combine(directory, "rows.csv");
            File.WriteAllText(csv, "Ma,Ten\nSP-1,Bu long\nSP-2,Dai oc\n");
            var viewModel = new ANLAbel.App.ViewModels.MainViewModel();
            // Before any window: no dispatcher context for the import to wait on.
            viewModel.ImportExcelAsync(csv, "CSV").GetAwaiter().GetResult();
            var template = viewModel.Template;
            template.SerialCounter.Enabled = true;
            template.SerialCounter.StartValue = 41;
            template.PromptVariables.Add(new PromptVariableDefinition { Name = "Lot" });
            template.DataTransforms.Add(new ANLAbel.Core.Data.DataTransformDefinition("SN", "CONCAT(FIELD(\"Ma\"), \"/\", FIELD(\"#SERIAL\"))"));
            LabelObject Text(string name, string binding, double y) => new() { Type = ObjectType.Text, Name = name, BindingExpression = binding, XMm = 3, YMm = y, WidthMm = 40, HeightMm = 6 };
            template.Objects.Add(Text("Lô", "CONCAT(\"L-\", FIELD(\"?Lot\"))", 2));
            template.Objects.Add(Text("Ngày", "CONCAT(FIELD(\"Ma\"), FIELD(\"#NOW:yyMMdd\"))", 10));
            template.Objects.Add(Text("Sê-ri", "{#SERIAL}", 18));
            template.Objects.Add(Text("Sê-ri công thức", "CONCAT(\"N\", FIELD(\"#SERIAL\"))", 26));
            template.Objects.Add(Text("Mã sê-ri", "{SN}", 34));

            Require(!viewModel.HasBindingIssues, "Print-time fields are not broken links: " + string.Join(" | ", viewModel.BindingIssues.Select(issue => $"{issue.ObjectName}: {string.Join(", ", issue.MissingFields.Concat(issue.Errors))}")));
            var design = viewModel.DesignPreviewRow!;
            Require(design.TryGetValue("SN", out var designSn) && designSn == "SP-1/" + design["#SERIAL"], "The designer shows the function with the next serial: " + string.Join(";", design.Select(pair => pair.Key + "=" + pair.Value)));

            Require(viewModel.TryBuildPrintPreviewRows(out var rows, out var rowError), "The print rows build: " + rowError);
            Require(ANLAbel.Core.Printing.JobVariableContract.TryApply(template, rows, DateTimeOffset.Now, new Dictionary<string, string> { ["Lot"] = "7" }, out var withValues, out var valueError), valueError);
            Require(ANLAbel.Core.Printing.SerialCounterContract.TryApply(template.SerialCounter, 41, withValues, out var numbered, out var serialError), serialError);
            Require(ANLAbel.Core.Printing.JobVariableContract.TryApplySerialTransforms(template, numbered, out var printed, out var functionError)
                    && printed.Select(row => row!["SN"]).SequenceEqual(["SP-1/41", "SP-2/42"]),
                "Each printed row carries its own serial in the function: " + functionError + " " + string.Join(", ", printed.Select(row => row?.GetValueOrDefault("SN"))));

            var window = new ANLAbel.App.DataManagerWindow(viewModel, "Functions");
            var functions = (System.Collections.IList)typeof(ANLAbel.App.DataManagerWindow).GetField("_functions", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(window)!;
            typeof(ANLAbel.App.DataManagerWindow).GetMethod("RebuildTree", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(window, new[] { functions[0]! });
            System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);
            var result = ((TextBlock)window.FindName("FunctionResultText")).Text;
            Require(result == "SP-1/41", "The function editor tries the function with the counter's first number: " + result);

            template.SerialCounter.Enabled = false;
            Require(viewModel.BindingIssues.Any(issue => issue.ObjectName == "Sê-ri" && issue.MissingFields.Contains("#SERIAL")),
                "With the counter off, {#SERIAL} is still reported: " + string.Join(" | ", viewModel.BindingIssues.Select(issue => issue.ObjectName)));
            // Not closed: closing an edited draft asks to keep or discard it.
        }
    }

    /// <summary>
    /// The Data panel lists what a label can show, as NiceLabel's data explorer:
    /// prompts, sheet fields, the Data Manager functions and the values given at
    /// print time (the serial while the counter is on, the print date); each
    /// drags onto the label with its binding.
    /// </summary>
    public static Task DataPanelListsFunctionsAndPrintTimeFields()
    {
        RunOnSta(() =>
        {
            var viewModel = new ANLAbel.App.ViewModels.MainViewModel();
            viewModel.Template.PromptVariables.Add(new PromptVariableDefinition { Name = "Lot" });
            viewModel.Template.DataTransforms.Add(new ANLAbel.Core.Data.DataTransformDefinition("MaLo", "CONCAT(\"L-\", FIELD(\"?Lot\"))"));
            var counter = viewModel.Template.SerialCounter.Clone();
            counter.Enabled = true;
            viewModel.ApplyDataDefinitions(viewModel.Template.PromptVariables.ToArray(), viewModel.Template.DataTransforms.ToArray(), counter);
            var window = new ANLAbel.App.MainWindow { DataContext = viewModel };
            void Settle()
            {
                window.Measure(new Size(1440, 900));
                window.Arrange(new Rect(0, 0, 1440, 900));
                window.UpdateLayout();
                System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);
            }

            Settle();
            var tree = FindDescendants<TreeView>(window).Single(element => AutomationProperties.GetAutomationId(element) == "Shell.Workspace.DataTree");
            var sections = tree.Items.OfType<TreeViewItem>().ToArray();
            string Heading(TreeViewItem item) => string.Concat(FindDescendants<TextBlock>((DependencyObject)item.Header).Select(text => text.Text).Where(text => text.Length > 1));
            Require(sections.Select(Heading).SequenceEqual(["Biến nhập khi in", "Trường dữ liệu", "Hàm", "Khi in"]),
                "The Data panel has prompts, fields, functions and print-time values: " + string.Join(", ", sections.Select(Heading)));

            var binding = typeof(ANLAbel.App.MainWindow).GetMethod("DataTreeBinding", BindingFlags.Static | BindingFlags.NonPublic)!;
            string?[] Bindings(TreeViewItem section) => section.Items.Cast<object>().Select(item => (string?)binding.Invoke(null, new[] { item })).ToArray();
            Require(Bindings(sections[0]).SequenceEqual(["{?Lot}"]) && Bindings(sections[2]).SequenceEqual(["{MaLo}"])
                    && Bindings(sections[3]).SequenceEqual(["{#SERIAL}", "{#NOW:dd/MM/yyyy}"]),
                $"Each item drags with its binding: {string.Join(" ", Bindings(sections[0]))} | {string.Join(" ", Bindings(sections[2]))} | {string.Join(" ", Bindings(sections[3]))}");

            var off = viewModel.Template.SerialCounter.Clone();
            off.Enabled = false;
            viewModel.ApplyDataDefinitions(viewModel.Template.PromptVariables.ToArray(), viewModel.Template.DataTransforms.ToArray(), off);
            Settle();
            Require(Bindings(sections[3]).SequenceEqual(["{#NOW:dd/MM/yyyy}"]), "Without the counter only the print date is offered: " + string.Join(" ", Bindings(sections[3])));

            // Dropped on the label, each becomes a text named after what it shows.
            viewModel.DropDataSource("{#NOW:dd/MM/yyyy}", 5, 5, null, newObject: true);
            viewModel.DropDataSource("{#SERIAL}", 5, 15, null, newObject: true);
            viewModel.DropDataSource("{MaLo}", 5, 25, null, newObject: true);
            var dropped = viewModel.Template.Objects.TakeLast(3).Select(item => $"{item.Name}={item.BindingExpression}").ToArray();
            Require(dropped.SequenceEqual(["Ngày in={#NOW:dd/MM/yyyy}", "Số sê-ri={#SERIAL}", "MaLo={MaLo}"]),
                "Dropped fields get readable names: " + string.Join(", ", dropped));
            window.Close();
        });

        return Task.CompletedTask;
    }

    /// <summary>
    /// The linked data file open in Excel: ANLAbel prints the saved file, so
    /// quantities typed in Excel and not saved print as before. While Excel's
    /// owner file "~$name" sits beside it, the Data card and Print Preview say
    /// to save in Excel and press Cập nhật Excel; once it is gone, they don't.
    /// Nothing is blocked.
    /// </summary>
    public static Task OpenWorkbookRemindsToSaveFirst()
    {
        var directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "anlabel-open-in-excel-" + Guid.NewGuid().ToString("N")));
        try
        {
            RunOnSta(() =>
            {
                var csv = Path.Combine(directory.FullName, "Hang.csv");
                var owner = Path.Combine(directory.FullName, "~$Hang.csv");
                File.WriteAllText(csv, "Ma,SoLuong\nSP-1,10\nSP-2,20\n");
                var viewModel = new ANLAbel.App.ViewModels.MainViewModel();
                viewModel.ImportExcelAsync(csv, "CSV").GetAwaiter().GetResult();
                Require(viewModel.ExcelOpenInExcelText.Length == 0, "A closed file needs no reminder: " + viewModel.ExcelOpenInExcelText);

                File.WriteAllText(owner, "owner");
                viewModel.RefreshExcelOpenHintAsync().GetAwaiter().GetResult();
                Require(viewModel.ExcelOpenInExcelText == ANLAbel.App.ViewModels.MainViewModel.ExcelOpenInExcelHint,
                    "The Data card reminds to save in Excel: " + viewModel.ExcelOpenInExcelText);

                viewModel.Template.Objects.Add(new LabelObject { Type = ObjectType.Text, Name = "Mã", BindingExpression = "{Ma}", XMm = 3, YMm = 3, WidthMm = 30, HeightMm = 6, Style = { BorderThicknessMm = 0 } });
                Require(viewModel.TryBuildPrintPreviewRows(out var rows, out var rowError), rowError);
                var window = new ANLAbel.App.PrintPreviewWindow(
                    viewModel.Template,
                    rows[0],
                    viewModel.ExcelDataView,
                    new PrintService(),
                    new ANLAbel.Data.PrintLogs.PrintLogService(Path.Combine(directory.FullName, "log.csv")),
                    string.Empty,
                    preparedRows: rows,
                    excelSourceIdentity: viewModel.ImportedExcelDataSourceIdentity);
                var deadline = DateTime.UtcNow.AddSeconds(10);
                while (window.DataOpenInExcelText.Length == 0 && DateTime.UtcNow < deadline)
                {
                    System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);
                    Thread.Sleep(20);
                }

                Require(window.DataOpenInExcelText == ANLAbel.App.ViewModels.MainViewModel.ExcelOpenInExcelHint,
                    "Print Preview reminds to save in Excel: " + window.DataOpenInExcelText);
                window.Close();

                File.Delete(owner);
                viewModel.RefreshExcelOpenHintAsync().GetAwaiter().GetResult();
                Require(viewModel.ExcelOpenInExcelText.Length == 0, "Once Excel closes the file the reminder goes: " + viewModel.ExcelOpenInExcelText);
            });
        }
        finally
        {
            try
            {
                directory.Delete(recursive: true);
            }
            catch (IOException)
            {
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// A lot merged over three rows in Excel shows on each of them, but the
    /// file keeps it in the first cell only: the other rows read as empty and
    /// their labels printed without a lot, without a word. The import status
    /// names the merged range and rows, and Print Preview repeats it when the
    /// label reads that column.
    /// </summary>
    public static Task MergedExcelCellsAreNamed()
    {
        var directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "anlabel-merged-" + Guid.NewGuid().ToString("N")));
        try
        {
            RunOnSta(() =>
            {
                var path = Path.Combine(directory.FullName, "lots.xlsx");
                using (var workbook = new ClosedXML.Excel.XLWorkbook())
                {
                    var sheet = workbook.AddWorksheet("Lots");
                    sheet.Cell(1, 1).Value = "Ma";
                    sheet.Cell(1, 2).Value = "Lot";
                    for (var index = 0; index < 4; index++)
                    {
                        sheet.Cell(index + 2, 1).Value = "SP-" + (index + 1);
                    }

                    sheet.Cell(2, 2).Value = "L2610";
                    sheet.Range(2, 2, 4, 2).Merge();
                    sheet.Cell(5, 2).Value = "L2611";
                    workbook.SaveAs(path);
                }

                var viewModel = new ANLAbel.App.ViewModels.MainViewModel();
                viewModel.ImportExcelAsync(path, "Lots").GetAwaiter().GetResult();
                const string note = "Ô gộp B2:B4 (“Lot”) trong Excel chỉ có giá trị ở ô đầu: tem của dòng 2, 3 sẽ để trống “Lot”.";
                Require(viewModel.StatusText.Contains(note, StringComparison.Ordinal), "The import names the merged lot: " + viewModel.StatusText);

                viewModel.Template.Objects.Add(new LabelObject { Type = ObjectType.Text, Name = "Mã", BindingExpression = "{Ma}", XMm = 3, YMm = 3, WidthMm = 30, HeightMm = 6, Style = { BorderThicknessMm = 0 } });
                Require(ANLAbel.App.Services.MergedCellsText.Describe(viewModel.ExcelDataView!.Table, viewModel.Template).Length == 0,
                    "A label that does not read the column is not told.");
                viewModel.Template.Objects.Add(new LabelObject { Type = ObjectType.Text, Name = "Lô", BindingExpression = "{Lot}", XMm = 3, YMm = 10, WidthMm = 30, HeightMm = 6, Style = { BorderThicknessMm = 0 } });
                Require(viewModel.TryBuildPrintPreviewRows(out var rows, out var rowError) && rows.Count == 4, "All four rows are read: " + rowError);
                var window = new ANLAbel.App.PrintPreviewWindow(
                    viewModel.Template,
                    rows[0],
                    viewModel.ExcelDataView,
                    new PrintService(),
                    new ANLAbel.Data.PrintLogs.PrintLogService(Path.Combine(directory.FullName, "log.csv")),
                    string.Empty,
                    preparedRows: rows,
                    excelSourceIdentity: viewModel.ImportedExcelDataSourceIdentity);
                var deadline = DateTime.UtcNow.AddSeconds(30);
                do
                {
                    System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);
                    Thread.Sleep(20);
                }
                while ((window.IsPreviewBusy || window.Pages.Count == 0) && DateTime.UtcNow < deadline);

                Require(window.MergedCellsText.StartsWith(note, StringComparison.Ordinal), "Print Preview names it: " + window.MergedCellsText);
                Require(window.PreflightStatusText.EndsWith("Xem lưu ý về ô gộp trong Excel ở trên.", StringComparison.Ordinal),
                    "The content check points to the note: " + window.PreflightStatusText);
                window.Close();
            });
        }
        finally
        {
            try
            {
                directory.Delete(recursive: true);
            }
            catch (IOException)
            {
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Rows the operator filtered away in Excel (AutoFilter: today's orders
    /// only) used to print with the rest: five labels for three orders. They
    /// are still read, but Print Preview starts them unselected and names them,
    /// and "In mọi dòng" skips them and says which; re-checking a row prints it.
    /// </summary>
    public static Task FilteredExcelRowsAreNotPrintedByDefault()
    {
        var directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "anlabel-filtered-" + Guid.NewGuid().ToString("N")));
        try
        {
            RunOnSta(() =>
            {
                var path = Path.Combine(directory.FullName, "orders.xlsx");
                using (var workbook = new ClosedXML.Excel.XLWorkbook())
                {
                    var sheet = workbook.AddWorksheet("Orders");
                    sheet.Cell(1, 1).Value = "Ma";
                    sheet.Cell(1, 2).Value = "Ngay";
                    for (var index = 0; index < 5; index++)
                    {
                        sheet.Cell(index + 2, 1).Value = "SP-" + (index + 1);
                        sheet.Cell(index + 2, 2).Value = index % 2 == 0 ? "31/10" : "30/10";
                    }

                    sheet.Range(1, 1, 6, 2).SetAutoFilter().Column(2).EqualTo("31/10");
                    workbook.SaveAs(path);
                }

                var viewModel = new ANLAbel.App.ViewModels.MainViewModel();
                viewModel.ImportExcelAsync(path, "Orders").GetAwaiter().GetResult();
                viewModel.Template.Objects.Add(new LabelObject { Type = ObjectType.Text, Name = "Mã", BindingExpression = "{Ma}", XMm = 3, YMm = 3, WidthMm = 30, HeightMm = 6, Style = { BorderThicknessMm = 0 } });
                Require(viewModel.TryBuildPrintPreviewRows(out var rows, out var rowError) && rows.Count == 5, "All five rows are read: " + rowError);
                var window = new ANLAbel.App.PrintPreviewWindow(
                    viewModel.Template,
                    rows[0],
                    viewModel.ExcelDataView,
                    new PrintService(),
                    new ANLAbel.Data.PrintLogs.PrintLogService(Path.Combine(directory.FullName, "log.csv")),
                    string.Empty,
                    preparedRows: rows,
                    excelSourceIdentity: viewModel.ImportedExcelDataSourceIdentity);
                var deadline = DateTime.UtcNow.AddSeconds(30);
                do
                {
                    System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);
                    Thread.Sleep(20);
                }
                while ((window.IsPreviewBusy || window.Pages.Count == 0) && DateTime.UtcNow < deadline);

                var list = (ListBox)window.FindName("TrackingList");
                var selected = list.Items.Cast<ANLAbel.App.ViewModels.TrackingRowViewModel>().Where(row => row.IsSelected).Select(row => row.Col1 ?? string.Empty).ToArray();
                Require(selected.SequenceEqual(["SP-1", "SP-3", "SP-5"]) && window.Pages.Count == 3,
                    $"Only the rows Excel shows start selected ({string.Join(", ", selected)}; {window.Pages.Count} labels prepared).");
                Require(window.HiddenRowsText.StartsWith("2 dòng đang ẩn trong Excel (bộ lọc) chưa được chọn in: dòng 2, 4.", StringComparison.Ordinal),
                    "Print Preview names the hidden rows: " + window.HiddenRowsText);
                window.Close();
            });

            // "In mọi dòng" skips them too (source check, like the other quick print gates).
            var root = AppContext.BaseDirectory;
            while (!File.Exists(Path.Combine(root, "ANLAbel.slnx")))
            {
                root = Path.GetDirectoryName(root.TrimEnd(Path.DirectorySeparatorChar))!;
            }

            var source = File.ReadAllText(Path.Combine(root, "src", "ANLAbel.App", "ViewModels", "MainViewModel.cs"));
            var start = source.IndexOf("private async Task PrintAllRowsAsync()", StringComparison.Ordinal);
            var body = source.Substring(start, source.IndexOf("private async Task<TrackedPrintResult> DispatchTrackedPrintAsync(", StringComparison.Ordinal) - start);
            Require(body.Contains("ExcelDataService.HiddenRowIndexes(ExcelDataView.Table)", StringComparison.Ordinal)
                    && body.Contains("if (hidden.Contains(index))", StringComparison.Ordinal)
                    && body.Contains("+ hiddenNote", StringComparison.Ordinal),
                "In mọi dòng must skip rows hidden in Excel and say which.");
        }
        finally
        {
            try
            {
                directory.Delete(recursive: true);
            }
            catch (IOException)
            {
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// A quantity from an Excel cell in comma style reads "1,200", and the comma
    /// went into the Code 128 as well, where NiceLabel printed 1200; a receiving
    /// system takes "1,200" as text. Preflight names such rows in Vietnamese,
    /// without blocking. A QR code, plain digits and "12,5" are not flagged.
    /// </summary>
    public static Task BarcodeThousandsSeparatorsAreFlagged()
    {
        var template = new LabelTemplate { Name = "Thùng", WidthMm = 80, HeightMm = 40, Dpi = 203 };
        template.PrinterProfile.Dpi = 203;
        template.Objects.Add(new LabelObject
        {
            Type = ObjectType.BarcodeCode128,
            BarcodeSymbology = BarcodeSymbology.Code128,
            Name = "QTY",
            BindingExpression = "{SL}",
            XMm = 3,
            YMm = 3,
            WidthMm = 50,
            HeightMm = 12
        });
        template.Objects.Add(new LabelObject { Type = ObjectType.QRCode, Name = "QR", BindingExpression = "{SL}", XMm = 58, YMm = 3, WidthMm = 18, HeightMm = 18 });
        IReadOnlyDictionary<string, string>?[] rows =
        [
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["SL"] = "1,200" },
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["SL"] = "1200" },
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["SL"] = "12,5" },
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["SL"] = "1,234,567.25" }
        ];
        var issues = new PrintPreflightValidator(new ZxingBarcodeRenderer()).Validate(template, rows, 203).Issues
            .Where(issue => issue.Message.StartsWith("Barcode data looks like a number with thousands separators", StringComparison.Ordinal))
            .ToArray();
        Require(issues.Select(issue => issue.RowNumber).SequenceEqual([1, 4]) && issues.All(issue => !issue.IsBlocking && issue.ObjectName == "QTY"),
            "Rows 1 and 4 of the Code 128 are flagged, without blocking: " + string.Join(", ", issues.Select(issue => $"{issue.RowNumber}:{issue.ObjectName}:{issue.IsBlocking}")));
        var text = ANLAbel.App.Services.PreflightIssueText.Line([1, 4], issues[0].ObjectName, issues[0].ObjectType, issues[0].Message);
        Require(text.Contains("dấu phân cách hàng nghìn", StringComparison.Ordinal),
            "The operator reads it in Vietnamese: " + text);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Barcode data from an export often ends in a space ("SP-0001 "), which
    /// the symbol carries and the scanner reads, so the receiving system's
    /// lookup fails. Preflight flags such rows as one advisory group, in
    /// Vietnamese, without blocking: the label prints what the data says.
    /// </summary>
    public static Task BarcodeEdgeSpacesAreFlaggedNotBlocked()
    {
        var template = new LabelTemplate { Name = "Hàng", WidthMm = 60, HeightMm = 30, Dpi = 203 };
        template.PrinterProfile.Dpi = 203;
        template.Objects.Add(new LabelObject
        {
            Type = ObjectType.BarcodeCode128,
            BarcodeSymbology = BarcodeSymbology.Code128,
            Name = "Mã hàng",
            BindingExpression = "{Ma}",
            XMm = 3,
            YMm = 3,
            WidthMm = 50,
            HeightMm = 12
        });
        IReadOnlyDictionary<string, string>?[] rows =
        [
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Ma"] = "SP-0001 " },
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Ma"] = " SP-0002" },
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Ma"] = "SP-0003" }
        ];
        var issues = new PrintPreflightValidator(new ZxingBarcodeRenderer()).Validate(template, rows, 203).Issues
            .Where(issue => issue.Message.StartsWith("Barcode data starts or ends with a space", StringComparison.Ordinal))
            .ToArray();
        Require(issues.Select(issue => issue.RowNumber).SequenceEqual([1, 2]) && issues.All(issue => !issue.IsBlocking),
            "Rows 1 and 2 are flagged, without blocking: " + string.Join(", ", issues.Select(issue => $"{issue.RowNumber}:{issue.IsBlocking}")));
        var text = ANLAbel.App.Services.PreflightIssueText.Line([1, 2], issues[0].ObjectName, issues[0].ObjectType, issues[0].Message);
        Require(text.Contains("khoảng trắng ở đầu hoặc cuối", StringComparison.Ordinal) && text.Contains("1", StringComparison.Ordinal),
            "The operator reads one grouped line in Vietnamese: " + text);
        return Task.CompletedTask;
    }

    /// <summary>
    /// With warnings only, Print Preview said "only scan-risk warnings: widen
    /// the barcode or raise the DPI" whatever the warning was, also for a thin
    /// line or a space at the end of the data. It now only says that printing
    /// is allowed; each warning below carries its own advice.
    /// </summary>
    public static Task WarningsOnlyStatusGivesNoBarcodeAdvice()
    {
        RunOnSta(() =>
        {
            var template = new LabelTemplate { Name = "Hàng", WidthMm = 100, HeightMm = 60, Dpi = 203 };
            template.PrinterProfile.Dpi = 203;
            template.Objects.Add(new LabelObject
            {
                Type = ObjectType.BarcodeCode128,
                BarcodeSymbology = BarcodeSymbology.Code128,
                Name = "Mã hàng",
                BindingExpression = "{Ma}",
                XMm = 5,
                YMm = 5,
                WidthMm = 80,
                HeightMm = 25,
                BarcodeModuleWidthMm = 0.375,
                BarcodeWidthMode = BarcodeWidthMode.SizedFromX,
                QrQuietZoneModules = 10,
                BarcodeHriPlacement = BarcodeHriPlacement.Below
            });
            var table = new System.Data.DataTable();
            table.Columns.Add("Ma");
            table.Rows.Add("SP-0001 ");
            var window = new ANLAbel.App.PrintPreviewWindow(
                template,
                null,
                table.DefaultView,
                new PrintService(),
                new ANLAbel.Data.PrintLogs.PrintLogService(Path.Combine(Path.GetTempPath(), $"anlabel-warnings-{Guid.NewGuid():N}.csv")),
                string.Empty);
            var deadline = DateTime.UtcNow.AddSeconds(30);
            do
            {
                System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);
                Thread.Sleep(20);
            }
            while ((window.IsPreviewBusy || window.Pages.Count == 0) && DateTime.UtcNow < deadline);

            Require(window.HasPreflightIssues && !window.HasBlockingPreflightIssues
                    && window.PreflightStatusText == "Chỉ có cảnh báo, vẫn được in. Xem từng cảnh báo bên dưới.",
                $"Warnings only, without barcode advice ({window.HasPreflightIssues}/{window.HasBlockingPreflightIssues}): {window.PreflightStatusText}");
            window.Close();
        });

        return Task.CompletedTask;
    }

    /// <summary>
    /// Importing a CSV or a one-sheet workbook asked to pick its only sheet
    /// before the header row: a click with nothing to decide. The sheet list
    /// now opens only when the file has several sheets.
    /// </summary>
    public static async Task ImportSkipsTheSheetChoiceWhenThereIsOne()
    {
        var directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "anlabel-one-sheet-" + Guid.NewGuid().ToString("N")));
        try
        {
            var csv = Path.Combine(directory.FullName, "rows.csv");
            await File.WriteAllTextAsync(csv, "Ma\nSP-1\n");
            var many = Path.Combine(directory.FullName, "many.xlsx");
            using (var workbook = new ClosedXML.Excel.XLWorkbook())
            {
                workbook.AddWorksheet("Don hang").Cell(1, 1).Value = "Ma";
                workbook.AddWorksheet("Ghi chu").Cell(1, 1).Value = "x";
                workbook.SaveAs(many);
            }

            var service = new ANLAbel.Data.Excel.ExcelDataService();
            var onlySheet = typeof(ANLAbel.App.ExcelImportWindow).GetMethod("OnlySheet", BindingFlags.Static | BindingFlags.NonPublic)!;
            string? Only(IReadOnlyList<ANLAbel.Data.Excel.ExcelSheetPreview> previews) => (string?)onlySheet.Invoke(null, new object[] { previews });
            var csvSheets = await service.GetSheetsWithPreviewAsync(csv);
            var bookSheets = await service.GetSheetsWithPreviewAsync(many);
            Require(Only(csvSheets) == ANLAbel.Data.Excel.ExcelDataService.CsvSheetName, "A CSV has one sheet and is read without asking: " + Only(csvSheets));
            Require(Only(bookSheets) is null && bookSheets.Count == 2, "A workbook with two sheets still asks which: " + string.Join(", ", bookSheets.Select(sheet => sheet.SheetName)));
        }
        finally
        {
            try
            {
                directory.Delete(recursive: true);
            }
            catch (IOException)
            {
            }
        }
    }

    /// <summary>
    /// Right-click on the design surface, as in NiceLabel 10: on an object the
    /// menu offers its properties, clipboard, lock, group, arrange and align
    /// (the click selects the object first); on empty label space the label's
    /// properties, paste and zoom; snapping, grid and guides sit under "Căn và
    /// lưới". Before, every right-click offered only snapping and guides.
    /// </summary>
    public static Task RightClickMenusOfferObjectAndLabelCommands()
    {
        RunOnSta(() =>
        {
            // The window's own view model, as in the app (its code-behind uses it).
            var window = new ANLAbel.App.MainWindow();
            var viewModel = (ANLAbel.App.ViewModels.MainViewModel)window.DataContext;
            window.Measure(new Size(1440, 900));
            window.Arrange(new Rect(0, 0, 1440, 900));
            window.UpdateLayout();
            viewModel.Template.Objects.Add(new LabelObject { Type = ObjectType.Text, Name = "Tên", Text = "Bu lông", XMm = 5, YMm = 5, WidthMm = 30, HeightMm = 6, Style = { BorderThicknessMm = 0 } });
            viewModel.Template.Objects.Add(new LabelObject { Type = ObjectType.Text, Name = "Mã", Text = "SP-0001", XMm = 5, YMm = 20, WidthMm = 30, HeightMm = 6, Style = { BorderThicknessMm = 0 } });
            System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);

            var canvas = (ANLAbel.App.Controls.LabelDesignerCanvas)window.FindName("DesignerCanvas");
            var type = typeof(ANLAbel.App.MainWindow);
            void RightClickAt(double xMm, double yMm)
            {
                var point = new Point(ANLAbel.Core.Geometry.MmConverter.MmToDip(xMm) * canvas.Zoom, ANLAbel.Core.Geometry.MmConverter.MmToDip(yMm) * canvas.Zoom);
                type.GetMethod("SelectForContextMenu", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(window, new object[] { point });
                type.GetMethod("UpdateDesignerContextMenu", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(window, null);
            }

            string[] Shown() => canvas.ContextMenu!.Items.OfType<MenuItem>().Where(item => item.Visibility == Visibility.Visible).Select(item => (string)item.Header).ToArray();
            MenuItem Find(string header) => canvas.ContextMenu!.Items.OfType<MenuItem>().Single(item => (string)item.Header == header);

            RightClickAt(10, 22);
            Require(viewModel.SelectedObject?.Name == "Mã", "A right-click selects the object under the pointer: " + viewModel.SelectedObject?.Name);
            Require(Shown().SequenceEqual(["Thuộc tính", "Cắt", "Sao chép", "Dán", "Xóa", "Khóa vị trí", "Nhóm đối tượng", "Bỏ nhóm", "Sắp xếp", "Căn chỉnh", "Căn và lưới"]),
                "On an object the menu offers its commands: " + string.Join(" | ", Shown()));
            Require(!Find("Dán").IsEnabled && !Find("Nhóm đối tượng").IsEnabled, "Nothing to paste and one object to group: both disabled.");

            var lockItem = Find("Khóa vị trí");
            lockItem.IsChecked = true;
            lockItem.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            Require(viewModel.Template.Objects.Single(item => item.Name == "Mã").IsLocked, "Khóa vị trí locks the object.");
            Find("Sao chép").RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));

            RightClickAt(60, 40);
            Require(Shown().SequenceEqual(["Thuộc tính tem", "Dán", "Chọn tất cả", "Vừa tài liệu", "Vừa đối tượng", "Căn và lưới"]) && Find("Dán").IsEnabled,
                "On the label the menu offers the label's commands, and the copy can be pasted: " + string.Join(" | ", Shown()));
            var gridHeaders = Find("Căn và lưới").Items.OfType<MenuItem>().Select(item => (string)item.Header).ToArray();
            Require(gridHeaders.Contains("Dính theo đối tượng") && gridHeaders.Contains("Đường gióng"), "Snapping and guides stay under Căn và lưới: " + string.Join(" | ", gridHeaders));

            RightClickAt(10, 7);
            Require(viewModel.SelectedObject?.Name == "Tên" && Find("Khóa vị trí").IsChecked == false, "Each object shows its own lock state.");

            // With equal ZIndex the object drawn on top is the one picked.
            var cover = new LabelObject { Type = ObjectType.Text, Name = "Phủ", Text = "Trên cùng", XMm = 5, YMm = 5, WidthMm = 30, HeightMm = 6, Style = { BorderThicknessMm = 0 } };
            cover.ZIndex = viewModel.Template.Objects.Single(item => item.Name == "Tên").ZIndex;
            viewModel.Template.Objects.Add(cover);
            System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);
            RightClickAt(10, 7);
            Require(viewModel.SelectedObject?.Name == "Phủ", "The object drawn on top is the one picked: " + viewModel.SelectedObject?.Name);

            // Xóa removes the whole selection, as the Delete key does.
            var picked = (HashSet<LabelObject>)typeof(ANLAbel.App.Controls.LabelDesignerCanvas)
                .GetField("_selectedObjects", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(canvas)!;
            foreach (var item in viewModel.Template.Objects)
            {
                picked.Add(item);
            }

            Find("Xóa").RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            Require(viewModel.Template.Objects.Count == 0, $"Xóa removes every selected object: {viewModel.Template.Objects.Count} left.");
            // Not closed: closing an edited label asks whether to save it.
        });

        return Task.CompletedTask;
    }

    /// <summary>
    /// Undo restores whole snapshots, and the Undo ▾ list several at once. Over a
    /// varied design session (add, move, font, label size, Data Manager,
    /// restack, rename, a drag of every object, delete) any mix of one-step and
    /// multi-step undo and redo lands exactly on the label as it was after that
    /// step.
    /// </summary>
    public static Task UndoAndRedoLandOnEveryStep()
    {
        RunOnSta(() =>
        {
            var viewModel = new ANLAbel.App.ViewModels.MainViewModel();
            var type = typeof(ANLAbel.App.ViewModels.MainViewModel);
            string Snapshot() => (string)type.GetMethod("CaptureTemplateSnapshot", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(viewModel, null)!;
            void Commit() => type.GetMethod("CommitPendingHistory", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(viewModel, null);
            var before = viewModel.UndoHistory.Count;
            var states = new List<string> { Snapshot() };
            void Step(Action edit)
            {
                edit();
                Commit();
                states.Add(Snapshot());
            }

            Step(() => viewModel.AddTextCommand.Execute(null));
            Step(() => viewModel.AddBarcodeCommand.Execute(null));
            var text = viewModel.Template.Objects[0];
            Step(() => text.XMm += 5);
            Step(() => text.Style.FontSizePt = 16);
            Step(() => viewModel.Template.WidthMm -= 10);
            Step(() => viewModel.ApplyDataDefinitions(
                [new PromptVariableDefinition { Name = "Lot", Prompt = "Số lô" }],
                [new ANLAbel.Core.Data.DataTransformDefinition("MaLo", "CONCAT(\"L-\", FIELD(\"?Lot\"))")],
                new SerialCounterDefinition { Enabled = true, StartValue = 5, Digits = 4 }));
            Step(() => viewModel.AddTextCommand.Execute(null));
            Step(() => viewModel.Template.Objects.Move(0, viewModel.Template.Objects.Count - 1));
            Step(() => text.Name = "Tiêu đề");
            Step(() =>
            {
                viewModel.BeginTemplateEditGesture();
                foreach (var item in viewModel.Template.Objects)
                {
                    item.YMm += 2;
                }

                viewModel.CommitTemplateEditGesture();
            });
            Step(() => viewModel.Template.Objects.RemoveAt(1));
            var steps = states.Count - 1;
            Require(viewModel.UndoHistory.Count - before == steps && states.Distinct().Count() == states.Count,
                $"Each of the {steps} edits is one named step: " + string.Join(" | ", viewModel.UndoHistory));

            var at = steps;
            void Check(string what)
            {
                Require(Snapshot() == states[at], $"{what}: the label must be as it was after step {at}. Undo list: " + string.Join(" | ", viewModel.UndoHistory));
            }

            viewModel.UndoSteps(steps);
            at = 0;
            Check("Undo all at once");
            viewModel.RedoSteps(3);
            at = 3;
            Check("Redo three");
            viewModel.RedoSteps(steps - 3);
            at = steps;
            Check("Redo the rest");
            viewModel.UndoSteps(5);
            at = steps - 5;
            Check("Undo five");
            viewModel.UndoCommand.Execute(null);
            at--;
            Check("Undo one more");
            viewModel.RedoCommand.Execute(null);
            at++;
            Check("Redo one");
            for (var index = 0; index < at; index++)
            {
                viewModel.UndoCommand.Execute(null);
            }

            at = 0;
            Check("Undo one by one to the start");
            viewModel.RedoSteps(steps);
            at = steps;
            Check("Redo everything");
        });

        return Task.CompletedTask;
    }

    /// <summary>
    /// With captions of 10 and 12 pt selected, the ribbon's size box showed the
    /// key caption's 10 pt as if it were the size of all three. As NiceLabel 10
    /// does, a box now shows a value only when every selected text shares it,
    /// and a size chosen there goes to all of them.
    /// </summary>
    public static Task RibbonFontShowsOnlyACommonValue()
    {
        RunOnSta(() =>
        {
            var window = new ANLAbel.App.MainWindow();
            var viewModel = (ANLAbel.App.ViewModels.MainViewModel)window.DataContext;
            window.Measure(new Size(1440, 900));
            window.Arrange(new Rect(0, 0, 1440, 900));
            window.UpdateLayout();
            void Pump() => System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);
            var canvas = (ANLAbel.App.Controls.LabelDesignerCanvas)window.FindName("DesignerCanvas");
            var selection = (HashSet<LabelObject>)typeof(ANLAbel.App.Controls.LabelDesignerCanvas)
                .GetField("_selectedObjects", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(canvas)!;
            foreach (var (name, size) in new[] { ("Vendor", 10.0), ("PO", 12.0), ("QTY", 10.0) })
            {
                viewModel.Template.Objects.Add(new LabelObject
                {
                    Type = ObjectType.Text, Name = name, Text = name, XMm = 3, YMm = 3 + viewModel.Template.Objects.Count * 10, WidthMm = 20, HeightMm = 6,
                    Style = { FontFamily = "Arial", FontSizePt = size, BorderThicknessMm = 0 }
                });
            }

            Pump();
            viewModel.SelectedObject = viewModel.Template.Objects[0];
            Pump();
            foreach (var item in viewModel.Template.Objects)
            {
                selection.Add(item);
            }

            viewModel.RefreshSelectionScope();
            ComboBox? Find(DependencyObject parent, string automationName)
            {
                foreach (var child in LogicalTreeHelper.GetChildren(parent).OfType<DependencyObject>())
                {
                    if (child is ComboBox combo && AutomationProperties.GetName(combo) == automationName)
                    {
                        return combo;
                    }

                    if (Find(child, automationName) is { } found)
                    {
                        return found;
                    }
                }

                return null;
            }

            var sizeBox = Find(window, "Ribbon font size");
            Require(viewModel.RibbonFontSize is null && viewModel.RibbonFontFamily == "Arial" && sizeBox is { SelectedItem: null },
                $"Mixed sizes leave the size box blank, the shared family shows: {viewModel.RibbonFontSize} / {viewModel.RibbonFontFamily} / {sizeBox?.SelectedItem}");

            viewModel.RibbonFontSize = 14;
            Pump();
            Require(viewModel.Template.Objects.All(item => item.Style.FontSizePt == 14) && viewModel.RibbonFontSize == 14,
                "A size chosen in the ribbon goes to every selected text: " + string.Join(", ", viewModel.Template.Objects.Select(item => item.Style.FontSizePt)));

            // The key's own size, chosen for a mixed selection, also goes to every text.
            viewModel.Template.Objects[1].Style.FontSizePt = 10;
            Require(viewModel.RibbonFontSize is null, "Mixed again: " + viewModel.RibbonFontSize);
            viewModel.RibbonFontSize = viewModel.Template.Objects[0].Style.FontSizePt;
            Require(viewModel.Template.Objects.All(item => item.Style.FontSizePt == 14),
                "The key's size goes to the others: " + string.Join(", ", viewModel.Template.Objects.Select(item => item.Style.FontSizePt)));
            // Not closed: closing an edited label asks whether to save it.
        });

        return Task.CompletedTask;
    }

    /// <summary>
    /// The ribbon's name box took any name, also one another object already had,
    /// so two objects could share "PO" again. As in NiceLabel 10, a taken name
    /// (case ignored) or an empty one is refused, the name stays and the status
    /// bar says why; an object's own name in other letters is fine.
    /// </summary>
    public static Task TakenOrEmptyNamesAreRefused()
    {
        RunOnSta(() =>
        {
            var viewModel = new ANLAbel.App.ViewModels.MainViewModel();
            viewModel.Template.Objects.Add(new LabelObject { Type = ObjectType.Text, Name = "PO", Text = "PO", XMm = 3, YMm = 3, WidthMm = 20, HeightMm = 6 });
            viewModel.Template.Objects.Add(new LabelObject { Type = ObjectType.Text, Name = "QTY", Text = "1200", XMm = 3, YMm = 12, WidthMm = 20, HeightMm = 6 });
            var quantity = viewModel.Template.Objects[1];
            viewModel.SelectedObject = quantity;

            viewModel.SelectedObjectName = "po ";
            Require(quantity.Name == "QTY" && viewModel.SelectedObjectName == "QTY"
                    && viewModel.ObjectNameMessage == "Không đổi tên: đã có đối tượng “PO” trên tem; giữ tên “QTY”."
                    && viewModel.StatusText == viewModel.ObjectNameMessage,
                $"A taken name is refused: {quantity.Name} / {viewModel.ObjectNameMessage} / {viewModel.StatusText}");
            viewModel.SelectedObjectName = "   ";
            Require(quantity.Name == "QTY" && viewModel.ObjectNameMessage.StartsWith("Không đổi tên: tên đối tượng không được để trống", StringComparison.Ordinal),
                $"An empty name is refused: {quantity.Name} / {viewModel.ObjectNameMessage}");
            viewModel.SelectedObjectName = " Số lượng ";
            Require(quantity.Name == "Số lượng" && viewModel.ObjectNameMessage.Length == 0, $"A free name is taken, trimmed: '{quantity.Name}' / {viewModel.ObjectNameMessage}");
            viewModel.SelectedObjectName = "số LƯỢNG";
            Require(quantity.Name == "số LƯỢNG", "Its own name in other letters is fine: " + quantity.Name);
            viewModel.SelectedObjectName = "PO";
            viewModel.SelectedObject = viewModel.Template.Objects[0];
            Require(viewModel.SelectedObjectName == "PO" && viewModel.ObjectNameMessage.Length == 0,
                $"Another selection shows its name and no old message: {viewModel.SelectedObjectName} / {viewModel.ObjectNameMessage}");
        });

        return Task.CompletedTask;
    }

    /// <summary>
    /// Three texts added in a row were all named "Chữ", so a preflight message
    /// or an undo step about “Chữ” could not say which one. An added object now
    /// takes a free name ("Chữ 2", "Chữ 3"), as NiceLabel 10 numbers a new
    /// item; a label opened with two objects of one name keeps both names.
    /// </summary>
    public static Task AddedObjectsGetUniqueNames()
    {
        RunOnSta(() =>
        {
            var viewModel = new ANLAbel.App.ViewModels.MainViewModel();
            var existing = new LabelTemplate { Name = "Tem cũ", WidthMm = 60, HeightMm = 40 };
            existing.Objects.Add(new LabelObject { Type = ObjectType.Text, Name = "Chữ", Text = "A", XMm = 3, YMm = 3, WidthMm = 20, HeightMm = 6 });
            existing.Objects.Add(new LabelObject { Type = ObjectType.Text, Name = "Chữ", Text = "B", XMm = 3, YMm = 12, WidthMm = 20, HeightMm = 6 });
            typeof(ANLAbel.App.ViewModels.MainViewModel).GetProperty("Template")!.SetValue(viewModel, existing);
            string Names() => string.Join(" | ", viewModel.Template.Objects.Select(item => item.Name));
            Require(Names() == "Chữ | Chữ", "The names a label already has stay: " + Names());

            viewModel.AddTextCommand.Execute(null);
            viewModel.AddTextCommand.Execute(null);
            viewModel.Template.Objects.Add(new LabelObject { Type = ObjectType.Text, Name = "PO (bản sao)", Text = "PO", XMm = 3, YMm = 21, WidthMm = 20, HeightMm = 6 });
            viewModel.Template.Objects.Add(new LabelObject { Type = ObjectType.Text, Name = "po (bản sao) ", Text = "PO", XMm = 3, YMm = 30, WidthMm = 20, HeightMm = 6 });
            Require(Names() == "Chữ | Chữ | Chữ 2 | Chữ 3 | PO (bản sao) | po (bản sao) 2",
                "Each added object has its own name: " + Names());
        });

        return Task.CompletedTask;
    }

    /// <summary>
    /// Undo took back a step without saying which. As in NiceLabel 10 each step
    /// now has a name: the Undo button says what it will undo, the status bar
    /// what it undid, and the Undo ▾ list takes back several steps at once.
    /// </summary>
    public static Task UndoStepsHaveNames()
    {
        RunOnSta(() =>
        {
            var window = new ANLAbel.App.MainWindow();
            var viewModel = (ANLAbel.App.ViewModels.MainViewModel)window.DataContext;
            void Commit() => typeof(ANLAbel.App.ViewModels.MainViewModel).GetMethod("CommitPendingHistory", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(viewModel, null);
            LabelObject Named(string name) => viewModel.Template.Objects.Single(item => item.Name == name);
            string Text(MenuItem item) => ((TextBlock)item.Header).Text;

            viewModel.Template.Objects.Add(new LabelObject { Type = ObjectType.Text, Name = "PO", Text = "PO", XMm = 3, YMm = 3, WidthMm = 20, HeightMm = 6 });
            Commit();
            viewModel.Template.Objects.Add(new LabelObject { Type = ObjectType.Text, Name = "SO_LUONG", Text = "1200", XMm = 3, YMm = 12, WidthMm = 20, HeightMm = 6 });
            Commit();
            viewModel.BeginTemplateEditGesture();
            Named("PO").XMm = 5;
            Named("SO_LUONG").XMm = 5;
            viewModel.CommitTemplateEditGesture();
            Named("PO").Style.FontSizePt = 14;
            Commit();
            var width = viewModel.Template.WidthMm;
            viewModel.Template.WidthMm = width - 10;
            Commit();

            string[] expected = ["Đổi khổ tem", "Đổi phông chữ “PO”", "Di chuyển 2 đối tượng", "Thêm “SO_LUONG”", "Thêm “PO”"];
            Require(viewModel.UndoHistory.Take(5).SequenceEqual(expected), "Each step has a name: " + string.Join(" | ", viewModel.UndoHistory));
            Require(viewModel.UndoToolTip == "Hoàn tác: Đổi khổ tem (Ctrl+Z)", "The Undo button names its step: " + viewModel.UndoToolTip);

            viewModel.UndoCommand.Execute(null);
            Require(viewModel.Template.WidthMm == width && viewModel.StatusText == "Đã hoàn tác: Đổi khổ tem.",
                $"The status bar names what was undone: {viewModel.StatusText} ({viewModel.Template.WidthMm} mm).");
            Require(viewModel.RedoToolTip == "Làm lại: Đổi khổ tem (Ctrl+Y)", "The Redo button names its step: " + viewModel.RedoToolTip);

            var menu = (ContextMenu)typeof(ANLAbel.App.MainWindow).GetMethod("BuildHistoryMenu", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(window, new object[] { false })!;
            var items = menu.Items.OfType<MenuItem>().ToArray();
            Require(items.Length >= 3 && Text(items[0]) == "Đổi phông chữ “PO”" && Text(items[2]) == "Thêm “SO_LUONG”" && items[2].InputGestureText == "3 bước",
                "The Undo list shows the steps, newest first: " + string.Join(" | ", items.Select(Text)));
            items[2].RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            Require(viewModel.Template.Objects.Count(item => item.Name is "PO" or "SO_LUONG") == 1 && Named("PO").XMm == 3 && Named("PO").Style.FontSizePt != 14,
                "The third entry takes back three steps: " + string.Join(" | ", viewModel.Template.Objects.Select(item => $"{item.Name} {item.XMm}")));
            Require(viewModel.StatusText == "Đã hoàn tác 3 bước: Đổi phông chữ “PO” … Thêm “SO_LUONG”." && viewModel.UndoHistory[0] == "Thêm “PO”",
                "The status bar names the steps undone: " + viewModel.StatusText);

            menu = (ContextMenu)typeof(ANLAbel.App.MainWindow).GetMethod("BuildHistoryMenu", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(window, new object[] { true })!;
            menu.Items.OfType<MenuItem>().ElementAt(1).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            Require(Named("SO_LUONG").XMm == 5 && Named("PO").XMm == 5 && viewModel.RedoHistory[0] == "Đổi phông chữ “PO”",
                "The Redo list puts back two steps: " + string.Join(" | ", viewModel.RedoHistory));

            // An edit still being recorded heads the list.
            Named("PO").YMm += 1;
            menu = (ContextMenu)typeof(ANLAbel.App.MainWindow).GetMethod("BuildHistoryMenu", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(window, new object[] { false })!;
            Require(Text(menu.Items.OfType<MenuItem>().First()) == "Di chuyển “PO”",
                "The edit being recorded heads the list: " + Text(menu.Items.OfType<MenuItem>().First()));
            // Not closed: closing an edited label asks whether to save it.
        });

        return Task.CompletedTask;
    }

    /// <summary>
    /// An ISO/IEC 15434 envelope needs RS, GS and EOT, which no key types;
    /// NiceLabel offers "Insert special character". The function editor now
    /// inserts them from a menu (inside quotes as an escape, elsewhere as a
    /// quoted piece) and shows them by name in its result.
    /// </summary>
    public static Task FunctionEditorInsertsControlCharacters()
    {
        RunOnSta(() =>
        {
            var viewModel = new ANLAbel.App.ViewModels.MainViewModel();
            viewModel.Template.DataTransforms.Add(new ANLAbel.Core.Data.DataTransformDefinition("Env", "CONCAT(\"[)>06\")"));
            var window = new ANLAbel.App.DataManagerWindow(viewModel, "Functions");
            var functions = (System.Collections.IList)typeof(ANLAbel.App.DataManagerWindow).GetField("_functions", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(window)!;
            typeof(ANLAbel.App.DataManagerWindow).GetMethod("RebuildTree", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(window, new[] { functions[0] });
            window.Measure(new Size(980, 820));
            window.Arrange(new Rect(0, 0, 980, 820));
            window.UpdateLayout();
            System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);
            var box = (TextBox)window.FindName("FunctionFormulaBox");
            Require(box.Text == "CONCAT(\"[)>06\")", "The editor shows the function: " + box.Text);
            var insert = typeof(ANLAbel.App.DataManagerWindow).GetMethod("InsertControlCharacter", BindingFlags.Instance | BindingFlags.NonPublic)!;

            box.CaretIndex = box.Text.IndexOf("06", StringComparison.Ordinal);
            insert.Invoke(window, new object[] { @"\u001E" });
            Require(box.Text == "CONCAT(\"[)>\\u001E06\")", "Inside quotes the escape goes in as it is: " + box.Text);

            box.CaretIndex = box.Text.Length - 1;
            insert.Invoke(window, new object[] { @"\u0004" });
            Require(box.Text == "CONCAT(\"[)>\\u001E06\"\"\\u0004\")", "Outside quotes it goes in as a quoted piece: " + box.Text);

            box.Text = "CONCAT(\"[)>\\u001E06\\u001D\", \"P1\\u001E\\u0004\")";
            var result = ((TextBlock)window.FindName("FunctionResultText")).Text;
            Require(result == "[)>‹RS›06‹GS›P1‹RS›‹EOT›", "The result shows the control characters by name: " + result);
            // Not closed: closing with edits may ask whether to keep them.
        });

        return Task.CompletedTask;
    }

    /// <summary>
    /// A vendor label carries up to eight Code 128 symbols, and their X
    /// dimension and readable text were set one by one. As NiceLabel 10's
    /// barcode pages do, a setting of the key barcode now goes to every linear
    /// barcode selected with it, in one undo step; a QR code keeps its own.
    /// </summary>
    public static Task BarcodeSettingReachesEverySelectedBarcode()
    {
        RunOnSta(() =>
        {
            var window = new ANLAbel.App.MainWindow();
            var viewModel = (ANLAbel.App.ViewModels.MainViewModel)window.DataContext;
            window.Measure(new Size(1440, 900));
            window.Arrange(new Rect(0, 0, 1440, 900));
            window.UpdateLayout();
            void Pump() => System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);
            var canvas = (ANLAbel.App.Controls.LabelDesignerCanvas)window.FindName("DesignerCanvas");
            var selection = (HashSet<LabelObject>)typeof(ANLAbel.App.Controls.LabelDesignerCanvas)
                .GetField("_selectedObjects", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(canvas)!;
            LabelObject Named(string name) => viewModel.Template.Objects.Single(item => item.Name == name);

            viewModel.Template.Objects.Add(new LabelObject { Type = ObjectType.BarcodeCode128, BarcodeSymbology = BarcodeSymbology.Code128, Name = "PO", Text = "4500123456", XMm = 3, YMm = 3, WidthMm = 50, HeightMm = 8, BarcodeModuleWidthMm = 0.25, BarcodeWidthMode = BarcodeWidthMode.SizedFromX });
            viewModel.Template.Objects.Add(new LabelObject { Type = ObjectType.BarcodeCode128, BarcodeSymbology = BarcodeSymbology.Code128, Name = "QTY", Text = "1200", XMm = 3, YMm = 15, WidthMm = 30, HeightMm = 8, BarcodeModuleWidthMm = 0.25, BarcodeWidthMode = BarcodeWidthMode.SizedFromX });
            viewModel.Template.Objects.Add(new LabelObject { Type = ObjectType.QRCode, Name = "QR", Text = "PO4500123456", XMm = 60, YMm = 3, WidthMm = 20, HeightMm = 20 });
            Pump();
            typeof(ANLAbel.App.ViewModels.MainViewModel).GetMethod("CommitPendingHistory", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(viewModel, null);
            var quantityPlacement = Named("QTY").BarcodeHriPlacement;
            var qrPlacement = Named("QR").BarcodeHriPlacement;
            var qrModule = Named("QR").BarcodeModuleWidthMm;

            viewModel.SelectedObject = Named("PO");
            Pump();
            foreach (var name in new[] { "PO", "QTY", "QR" })
            {
                selection.Add(Named(name));
            }

            Named("PO").BarcodeModuleWidthMm = 0.375;
            Named("PO").BarcodeHriPlacement = quantityPlacement == BarcodeHriPlacement.None ? BarcodeHriPlacement.Below : BarcodeHriPlacement.None;
            Pump();
            Require(Named("QTY").BarcodeModuleWidthMm == Named("PO").BarcodeModuleWidthMm && Named("PO").BarcodeModuleWidthMm > 0.25 && Named("QTY").BarcodeHriPlacement == Named("PO").BarcodeHriPlacement,
                $"The other barcode follows: X {Named("QTY").BarcodeModuleWidthMm} mm, text {Named("QTY").BarcodeHriPlacement}.");
            Require(Named("QR").BarcodeHriPlacement == qrPlacement && Named("QR").BarcodeModuleWidthMm == qrModule,
                "The QR code keeps its own settings.");

            viewModel.UndoCommand.Execute(null);
            Pump();
            Require(Named("QTY").BarcodeModuleWidthMm == 0.25 && Named("QTY").BarcodeHriPlacement == quantityPlacement,
                $"One undo puts both barcodes back: X {Named("QTY").BarcodeModuleWidthMm} mm.");

            // The other barcode's frame follows its new X, and Sized from X brings an X along.
            selection.Clear();
            viewModel.SelectedObject = Named("PO");
            Pump();
            selection.Add(Named("PO"));
            selection.Add(Named("QTY"));
            Named("PO").BarcodeModuleWidthMm = 0.5;
            Pump();
            var followed = Named("QTY").WidthMm;
            typeof(ANLAbel.App.ViewModels.MainViewModel).GetMethod("TryApplySizedFromXWidth", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(viewModel, new object[] { Named("QTY") });
            Require(Named("QTY").BarcodeModuleWidthMm == 0.5 && Named("QTY").WidthMm == followed,
                $"The other barcode's frame follows its X: {followed} mm, recomputed {Named("QTY").WidthMm} mm.");
            Named("QTY").BarcodeWidthMode = BarcodeWidthMode.FrameOwned;
            Named("QTY").BarcodeModuleWidthMm = 0;
            Named("PO").BarcodeWidthMode = BarcodeWidthMode.FrameOwned;
            Named("PO").BarcodeWidthMode = BarcodeWidthMode.SizedFromX;
            Pump();
            Require(Named("QTY").BarcodeWidthMode == BarcodeWidthMode.SizedFromX && Named("QTY").BarcodeModuleWidthMm == 0.5,
                $"Sized from X brings the key's X: {Named("QTY").BarcodeWidthMode}, X {Named("QTY").BarcodeModuleWidthMm} mm.");
            // Not closed: closing an edited label asks whether to save it.
        });

        return Task.CompletedTask;
    }

    /// <summary>
    /// The Cut button has always said "Ctrl+X", and nothing cut; NiceLabel's
    /// designer keys Ctrl+A, Ctrl+G, Ctrl+Shift+G and Ctrl+F did nothing either.
    /// They now select every object, cut, group, ungroup and open the object
    /// search, and the keys that change the label wait while it is protected.
    /// </summary>
    public static Task DesignerKeysSelectCutGroupAndFind()
    {
        RunOnSta(() =>
        {
            var window = new ANLAbel.App.MainWindow();
            var viewModel = (ANLAbel.App.ViewModels.MainViewModel)window.DataContext;
            window.Measure(new Size(1440, 900));
            window.Arrange(new Rect(0, 0, 1440, 900));
            window.UpdateLayout();
            void Pump() => System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);
            var canvas = (ANLAbel.App.Controls.LabelDesignerCanvas)window.FindName("DesignerCanvas");
            void Press(System.Windows.Input.Key key, System.Windows.Input.ModifierKeys modifiers)
            {
                var binding = window.InputBindings.OfType<System.Windows.Input.KeyBinding>().Single(item => item.Key == key && item.Modifiers == modifiers);
                binding.Command.Execute(binding.CommandParameter);
                Pump();
            }

            foreach (var name in new[] { "PO", "QTY", "Lot" })
            {
                viewModel.Template.Objects.Add(new LabelObject { Type = ObjectType.Text, Name = name, Text = name, XMm = 3, YMm = 3 + viewModel.Template.Objects.Count * 10, WidthMm = 20, HeightMm = 6, Style = { BorderThicknessMm = 0 } });
            }

            Pump();
            const System.Windows.Input.ModifierKeys control = System.Windows.Input.ModifierKeys.Control;
            Press(System.Windows.Input.Key.A, control);
            Require(canvas.SelectedObjectCount == 3, "Ctrl+A selects every object: " + canvas.SelectedObjectCount);
            Press(System.Windows.Input.Key.G, control);
            var group = viewModel.Template.Objects[0].GroupId;
            Require(group.Length > 0 && viewModel.Template.Objects.All(item => item.GroupId == group), "Ctrl+G groups them.");
            Press(System.Windows.Input.Key.G, control | System.Windows.Input.ModifierKeys.Shift);
            Require(viewModel.Template.Objects.All(item => item.GroupId.Length == 0), "Ctrl+Shift+G ungroups them.");
            Press(System.Windows.Input.Key.X, control);
            Require(viewModel.Template.Objects.Count == 0 && canvas.HasCopiedObjects, $"Ctrl+X cuts them: {viewModel.Template.Objects.Count} left.");
            viewModel.IsToolboxVisible = false;
            Press(System.Windows.Input.Key.F, control);
            var search = (TextBox)window.FindName("ObjectBrowserSearch");
            DependencyObject? node = search;
            while (node is not null and not TabItem)
            {
                node = LogicalTreeHelper.GetParent(node);
            }

            Require(node is TabItem { IsSelected: true } && viewModel.IsToolboxVisible, "Ctrl+F shows the left panel and opens the Đối tượng tab with its search box.");

            // Right-click on empty label space offers Chọn tất cả, as NiceLabel's label menu does.
            void LabelMenu() => typeof(ANLAbel.App.MainWindow).GetMethod("UpdateDesignerContextMenu", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(window, null);
            MenuItem SelectAllItem() => canvas.ContextMenu!.Items.OfType<MenuItem>().Single(item => (string)item.Header == "Chọn tất cả");
            LabelMenu();
            Require(SelectAllItem() is { IsEnabled: false, InputGestureText: "Ctrl+A", Visibility: Visibility.Visible }, "An empty label offers Chọn tất cả (Ctrl+A), not enabled.");
            Require(canvas.PasteCopiedObjects() && viewModel.Template.Objects.Count == 3, "Pasting the cut objects brings them back.");
            Pump();
            ((HashSet<LabelObject>)typeof(ANLAbel.App.Controls.LabelDesignerCanvas)
                .GetField("_selectedObjects", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(canvas)!).Clear();
            viewModel.SelectedObject = null;
            Pump();
            LabelMenu();
            SelectAllItem().RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            Pump();
            Require(canvas.SelectedObjectCount == 3, "Chọn tất cả selects every object: " + canvas.SelectedObjectCount);
            // Not closed: closing an edited label asks whether to save it.
        });

        return Task.CompletedTask;
    }

    /// <summary>
    /// NiceLabel 10's Ctrl+B, Ctrl+I, Ctrl+U and PgUp/PgDn (Ctrl: to the top or the
    /// bottom) had no key in ANLAbel. They now format every selected text and order
    /// the selected object; none of the designer keys acts behind the File backstage
    /// or changes a protected label.
    /// </summary>
    public static Task DesignerKeysFormatAndOrder()
    {
        RunOnSta(() =>
        {
            var window = new ANLAbel.App.MainWindow();
            var viewModel = (ANLAbel.App.ViewModels.MainViewModel)window.DataContext;
            window.Measure(new Size(1440, 900));
            window.Arrange(new Rect(0, 0, 1440, 900));
            window.UpdateLayout();
            void Pump() => System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);
            var canvas = (ANLAbel.App.Controls.LabelDesignerCanvas)window.FindName("DesignerCanvas");
            const System.Windows.Input.ModifierKeys control = System.Windows.Input.ModifierKeys.Control;
            void Press(System.Windows.Input.Key key, System.Windows.Input.ModifierKeys modifiers)
            {
                var binding = window.InputBindings.OfType<System.Windows.Input.KeyBinding>().Single(item => item.Key == key && item.Modifiers == modifiers);
                binding.Command.Execute(binding.CommandParameter);
                Pump();
            }

            foreach (var name in new[] { "PO", "QTY", "Lot" })
            {
                viewModel.Template.Objects.Add(new LabelObject { Type = ObjectType.Text, Name = name, Text = name, XMm = 3, YMm = 3 + viewModel.Template.Objects.Count * 10, WidthMm = 20, HeightMm = 6, ZIndex = viewModel.Template.Objects.Count, Style = { BorderThicknessMm = 0 } });
            }

            Pump();
            LabelObject Named(string name) => viewModel.Template.Objects.Single(item => item.Name == name);
            Press(System.Windows.Input.Key.A, control);
            Press(System.Windows.Input.Key.B, control);
            Require(viewModel.Template.Objects.All(item => item.Style.Bold), "Ctrl+B makes every selected text bold.");
            Press(System.Windows.Input.Key.I, control);
            Press(System.Windows.Input.Key.U, control);
            Require(viewModel.Template.Objects.All(item => item.Style.Italic && item.Style.Underline), "Ctrl+I and Ctrl+U reach every selected text.");
            Press(System.Windows.Input.Key.B, control);
            Require(viewModel.Template.Objects.All(item => !item.Style.Bold), "Ctrl+B again takes bold off.");

            // PgUp on the label raises the selected object one layer; the key reaches the canvas as a real key press.
            // A click on PO alone: the canvas keeps a multi-selection when only its key changes.
            var selection = (HashSet<LabelObject>)typeof(ANLAbel.App.Controls.LabelDesignerCanvas)
                .GetField("_selectedObjects", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(canvas)!;
            selection.Clear();
            viewModel.SelectedObject = Named("PO");
            Pump();
            var press = new System.Windows.Input.KeyEventArgs(System.Windows.Input.Keyboard.PrimaryDevice, new TestPresentationSource(), 0, System.Windows.Input.Key.PageUp)
            {
                RoutedEvent = System.Windows.Input.Keyboard.PreviewKeyDownEvent
            };
            canvas.RaiseEvent(press);
            Require(press.Handled && Named("PO").ZIndex == 1 && Named("QTY").ZIndex == 0, $"PgUp raises PO one layer: PO {Named("PO").ZIndex}, QTY {Named("QTY").ZIndex}.");
            bool Arrange(System.Windows.Input.Key key, System.Windows.Input.ModifierKeys modifiers)
                => (bool)typeof(ANLAbel.App.MainWindow).GetMethod("ArrangeByKey", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(window, new object[] { key, modifiers })!;
            Require(Arrange(System.Windows.Input.Key.PageUp, control) && Named("PO").ZIndex > viewModel.Template.Objects.Where(item => item.Name != "PO").Max(item => item.ZIndex), "Ctrl+PgUp brings PO to the top.");
            Require(Arrange(System.Windows.Input.Key.PageDown, control) && Named("PO").ZIndex < viewModel.Template.Objects.Where(item => item.Name != "PO").Min(item => item.ZIndex), "Ctrl+PgDn sends PO to the bottom.");
            Require(!Arrange(System.Windows.Input.Key.PageDown, System.Windows.Input.ModifierKeys.Shift), "Shift+PgDn is not an arrange key.");

            // An arrow-key move still open is finished before Ctrl+B: Esc then cannot take the bold back with it.
            selection.Clear();
            viewModel.SelectedObject = Named("Lot");
            Pump();
            var start = Named("Lot").XMm;
            void CanvasKey(System.Windows.Input.Key key) => canvas.RaiseEvent(
                new System.Windows.Input.KeyEventArgs(System.Windows.Input.Keyboard.PrimaryDevice, new TestPresentationSource(), 0, key)
                {
                    RoutedEvent = System.Windows.Input.Keyboard.KeyDownEvent
                });
            CanvasKey(System.Windows.Input.Key.Right);
            Press(System.Windows.Input.Key.B, control);
            CanvasKey(System.Windows.Input.Key.Escape);
            Pump();
            Require(Named("Lot").Style.Bold && Math.Abs(Named("Lot").XMm - start - 0.1) < 0.001,
                $"Esc after an arrow move and Ctrl+B keeps both: bold {Named("Lot").Style.Bold}, moved {Named("Lot").XMm - start:0.###} mm.");
            Press(System.Windows.Input.Key.B, control);

            // Ctrl+A puts an armed Format Painter down rather than painting the object it makes the key.
            var painter = (System.Windows.Controls.Primitives.ToggleButton)window.FindName("FormatPainterToggle");
            Named("QTY").Style.FontSizePt = 20;
            selection.Clear();
            viewModel.SelectedObject = Named("QTY");
            Pump();
            painter.IsChecked = true;
            Press(System.Windows.Input.Key.A, control);
            Require(painter.IsChecked == false && viewModel.Template.Objects.Count(item => item.Style.FontSizePt == 20) == 1,
                "Ctrl+A puts the Format Painter down: " + string.Join(", ", viewModel.Template.Objects.Select(item => $"{item.Name} {item.Style.FontSizePt}")));

            // Behind the File backstage the designer keys leave the label alone.
            typeof(ANLAbel.App.MainWindow).GetMethod("ShowBackstage", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(window, new object[] { "Open" });
            selection.Clear();
            viewModel.SelectedObject = Named("QTY");
            Pump();
            Require(canvas.SelectedObjectCount == 1, "Only QTY is selected: " + canvas.SelectedObjectCount);
            Press(System.Windows.Input.Key.A, control);
            Press(System.Windows.Input.Key.B, control);
            Require(canvas.SelectedObjectCount == 1 && !Named("QTY").Style.Bold, $"Behind the backstage Ctrl+A and Ctrl+B do nothing: {canvas.SelectedObjectCount} selected.");
            typeof(ANLAbel.App.MainWindow).GetMethod("HideBackstage", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(window, null);

            // A protected label keeps its objects, groups, style and order.
            viewModel.Template.ProtectFromEditing = true;
            Pump();
            Require(viewModel.IsEditingLocked, "The label is protected.");
            Press(System.Windows.Input.Key.A, control);
            Press(System.Windows.Input.Key.G, control);
            Press(System.Windows.Input.Key.B, control);
            Press(System.Windows.Input.Key.X, control);
            var order = viewModel.Template.Objects.Select(item => item.ZIndex).ToArray();
            Require(!Arrange(System.Windows.Input.Key.PageUp, System.Windows.Input.ModifierKeys.None), "PgUp is not taken on a protected label.");
            Require(viewModel.Template.Objects.Count == 3
                    && viewModel.Template.Objects.All(item => item.GroupId.Length == 0 && !item.Style.Bold)
                    && viewModel.Template.Objects.Select(item => item.ZIndex).SequenceEqual(order),
                "Ctrl+G, Ctrl+B, Ctrl+X and PgUp leave a protected label unchanged.");
            // Not closed: closing an edited label asks whether to save it.
        });

        return Task.CompletedTask;
    }

    /// <summary>
    /// With a white box and a logo selected, "Xuống dưới cùng" moved only the key
    /// object and the other stayed over the texts. As NiceLabel 10 does, the
    /// arrange commands now move every selected object, keep their order among
    /// themselves and change no other object's layer, in one undo step; equal
    /// layer values no longer hide a move.
    /// </summary>
    public static Task ArrangeMovesEverySelectedObject()
    {
        RunOnSta(() =>
        {
            var window = new ANLAbel.App.MainWindow();
            var viewModel = (ANLAbel.App.ViewModels.MainViewModel)window.DataContext;
            window.Measure(new Size(1440, 900));
            window.Arrange(new Rect(0, 0, 1440, 900));
            window.UpdateLayout();
            void Pump() => System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);
            void Commit() => typeof(ANLAbel.App.ViewModels.MainViewModel).GetMethod("CommitPendingHistory", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(viewModel, null);
            var canvas = (ANLAbel.App.Controls.LabelDesignerCanvas)window.FindName("DesignerCanvas");
            var selection = (HashSet<LabelObject>)typeof(ANLAbel.App.Controls.LabelDesignerCanvas)
                .GetField("_selectedObjects", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(canvas)!;
            LabelObject Named(string name) => viewModel.Template.Objects.Single(item => item.Name == name);
            void Select(params string[] names)
            {
                selection.Clear();
                viewModel.SelectedObject = Named(names[0]);
                Pump();
                foreach (var name in names)
                {
                    selection.Add(Named(name));
                }
            }

            string Order() => string.Join(" ", viewModel.Template.Objects
                .Select((item, index) => (item, index))
                .OrderBy(entry => entry.item.ZIndex)
                .ThenBy(entry => entry.index)
                .Select(entry => entry.item.Name));
            string Layers(params string[] names) => string.Join(",", names.Select(name => Named(name).ZIndex));

            foreach (var name in new[] { "A", "S1", "B", "S2", "C" })
            {
                viewModel.Template.Objects.Add(new LabelObject { Type = ObjectType.Rectangle, Name = name, XMm = 3, YMm = 3, WidthMm = 20, HeightMm = 10, ZIndex = viewModel.Template.Objects.Count });
            }

            Pump();
            Commit();
            foreach (var (command, expected) in new[]
            {
                (viewModel.SendToBackCommand, "S1 S2 A B C"),
                (viewModel.BringToFrontCommand, "A B C S1 S2"),
                (viewModel.BringForwardCommand, "A B S1 C S2"),
                (viewModel.SendBackwardCommand, "S1 A S2 B C"),
            })
            {
                Select("S1", "S2");
                command.Execute(null);
                Pump();
                Require(Order() == expected, $"Both selected objects move, in their order: {Order()} (expected {expected}).");
                if (expected is "S1 S2 A B C" or "A B C S1 S2")
                {
                    Require(Layers("A", "B", "C") == "0,2,4", "To the top or the bottom changes only the moved objects: " + Layers("A", "B", "C"));
                }

                Commit();
                viewModel.UndoCommand.Execute(null);
                Pump();
                Require(Order() == "A S1 B S2 C", "One undo puts the order back: " + Order());
            }

            // Equal layer values: S2 is painted over B by the collection order, and B steps above it.
            Named("B").ZIndex = Named("S2").ZIndex;
            Pump();
            Commit();
            Require(Order() == "A S1 B S2 C", "B and S2 share a layer value: " + Order());
            Select("B");
            viewModel.BringForwardCommand.Execute(null);
            Pump();
            Require(Order() == "A S1 S2 B C", "Bring forward shows over an equal layer value: " + Order());
            // Not closed: closing an edited label asks whether to save it.
        });

        return Task.CompletedTask;
    }

    /// <summary>
    /// Every paste stepped 3 mm aside and renamed its objects "(bản sao)", even into
    /// another label or after a cut, and a barcode built from a function or a print-time
    /// answer arrived without them. As NiceLabel 10 pastes, objects now keep their place
    /// and names unless their originals or an earlier copy are on the label, and bring
    /// the functions and prompt variables they read; a name the label already has keeps
    /// the label's definition and the status line says so. One undo removes it all.
    /// </summary>
    public static Task PasteIntoAnotherLabelKeepsPlaceNamesAndDefinitions()
    {
        RunOnSta(() =>
        {
            var window = new ANLAbel.App.MainWindow();
            var viewModel = (ANLAbel.App.ViewModels.MainViewModel)window.DataContext;
            window.Measure(new Size(1440, 900));
            window.Arrange(new Rect(0, 0, 1440, 900));
            window.UpdateLayout();
            void Pump() => System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);
            void Commit() => typeof(ANLAbel.App.ViewModels.MainViewModel).GetMethod("CommitPendingHistory", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(viewModel, null);
            var canvas = (ANLAbel.App.Controls.LabelDesignerCanvas)window.FindName("DesignerCanvas");
            var selection = (HashSet<LabelObject>)typeof(ANLAbel.App.Controls.LabelDesignerCanvas)
                .GetField("_selectedObjects", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(canvas)!;
            LabelObject Named(string name) => viewModel.Template.Objects.Single(item => item.Name == name);
            void Select(params string[] names)
            {
                selection.Clear();
                viewModel.SelectedObject = Named(names[0]);
                Pump();
                foreach (var name in names)
                {
                    selection.Add(Named(name));
                }
            }

            string Places() => string.Join(" ", viewModel.Template.Objects.OrderBy(item => item.Name, StringComparer.Ordinal).Select(item => $"{item.Name}@{item.XMm:0.#},{item.YMm:0.#}"));

            var source = viewModel.Template;
            source.DataTransforms.Add(new ANLAbel.Core.Data.DataTransformDefinition("Mã QR", "CONCAT(FIELD(\"PO\"), \"|\", FIELD(\"?Lô\"), \"|\", FIELD(\"Tổng\"))"));
            source.DataTransforms.Add(new ANLAbel.Core.Data.DataTransformDefinition("Tổng", "FIELD(\"QTY\")"));
            source.DataTransforms.Add(new ANLAbel.Core.Data.DataTransformDefinition("Thừa", "FIELD(\"X\")"));
            source.DataTransforms.Add(new ANLAbel.Core.Data.DataTransformDefinition("Mã hàng", "CONCAT(FIELD(\"PO\"), \"-\", FIELD(\"QTY\"))"));
            source.PromptVariables.Add(new PromptVariableDefinition { Name = "Lô", Prompt = "Số lô", MaxLength = 8 });
            source.PromptVariables.Add(new PromptVariableDefinition { Name = "Ca", Prompt = "Ca làm" });
            source.Objects.Add(new LabelObject { Type = ObjectType.Text, Name = "QR", BindingExpression = "{Mã QR}", XMm = 5, YMm = 5, WidthMm = 20, HeightMm = 6, Style = { BorderThicknessMm = 0 } });
            source.Objects.Add(new LabelObject { Type = ObjectType.Text, Name = "PO", BindingExpression = "{Mã hàng}", XMm = 30, YMm = 5, WidthMm = 20, HeightMm = 6, Style = { BorderThicknessMm = 0 } });
            Pump();
            Commit();

            // A cut and a paste put the object back where it was, under its own name.
            Select("PO");
            Require(canvas.CutSelectedObjects() && viewModel.Template.Objects.Count == 1, "PO is cut.");
            Require(canvas.PasteCopiedObjects() && Places() == "PO@30,5 QR@5,5", "Cut and paste keeps PO's place and name: " + Places());
            Commit();

            // Beside the originals a paste still steps aside as a copy.
            Select("QR", "PO");
            Require(canvas.CopySelectedObjects() && canvas.PasteCopiedObjects(), "QR and PO are copied and pasted.");
            Require(Places() == "PO@30,5 PO (bản sao)@33,8 QR@5,5 QR (bản sao)@8,8", "Beside the originals the copies step aside: " + Places());
            Require(source.DataTransforms.Count == 4 && source.PromptVariables.Count == 2, "The same label gets no second definition.");

            // Copied again from the originals, the next paste steps past the first copies.
            Select("QR", "PO");
            Require(canvas.CopySelectedObjects() && canvas.PasteCopiedObjects(), "The originals are copied and pasted again.");
            Require(Places() == "PO@30,5 PO (bản sao)@33,8 PO (bản sao) 2@36,11 QR@5,5 QR (bản sao)@8,8 QR (bản sao) 2@11,11",
                "Copied again, the paste does not land on the first copies: " + Places());
            Commit();

            // Another label: the objects keep their place and names and bring what they read.
            viewModel.NewTemplate(new ANLAbel.App.ViewModels.NewTemplateRequest("Tem B", 80, 50, 203));
            var target = viewModel.Template;
            target.DataTransforms.Add(new ANLAbel.Core.Data.DataTransformDefinition("Tổng", "CONCAT(FIELD(\"QTY\"), \" cái\")"));
            var table = new System.Data.DataTable();
            table.Columns.Add("PO");
            table.Columns.Add("QTY");
            table.Rows.Add("P1", "5");
            viewModel.SelectedDataItem = table.DefaultView[0];
            Pump();
            Commit();
            Require(canvas.PasteCopiedObjects() && Places() == "PO@30,5 QR@5,5", "Into another label the objects keep their place and names: " + Places());
            Require(viewModel.PreviewRow is { } shown && shown.TryGetValue("Mã hàng", out var code) && code == "P1-5",
                "A function the paste brings shows its value on the data row at once: " + string.Join(", ", viewModel.PreviewRow?.Select(pair => $"{pair.Key}={pair.Value}") ?? []));
            Require(string.Join(", ", target.DataTransforms.Select(transform => transform.Name)) == "Tổng, Mã QR, Mã hàng"
                    && target.DataTransforms[0].Formula == "CONCAT(FIELD(\"QTY\"), \" cái\")",
                "The function comes along; the label keeps its own Tổng: " + string.Join(", ", target.DataTransforms.Select(transform => $"{transform.Name}={transform.Formula}")));
            Require(target.PromptVariables.Count == 1 && target.PromptVariables[0] is { Name: "Lô", MaxLength: 8, Prompt: "Số lô" },
                "The prompt comes with its rules; Ca, which nothing pasted reads, stays behind.");
            Require(viewModel.StatusText.Contains("“Mã QR”", StringComparison.Ordinal) && viewModel.StatusText.Contains("“Lô”", StringComparison.Ordinal)
                    && viewModel.StatusText.Contains("đã có hàm “Tổng”", StringComparison.Ordinal),
                "The status line names what came and the function the label kept: " + viewModel.StatusText);
            Commit();

            viewModel.UndoCommand.Execute(null);
            Pump();
            Require(viewModel.Template.Objects.Count == 0 && viewModel.Template.DataTransforms.Count == 1 && viewModel.Template.PromptVariables.Count == 0,
                $"One undo takes the objects and the definitions back out: {viewModel.Template.Objects.Count} objects, {viewModel.Template.DataTransforms.Count} functions, {viewModel.Template.PromptVariables.Count} prompts.");

            // Pasted twice, the second copy steps aside from the first.
            Require(canvas.PasteCopiedObjects() && canvas.PasteCopiedObjects(), "Two pastes.");
            Require(Places() == "PO@30,5 PO (bản sao)@33,8 QR@5,5 QR (bản sao)@8,8", "The second paste steps aside from the first: " + Places());

            // With the first copies deleted, a paste takes their free place, and the next one
            // steps past every copy still on the label.
            foreach (var first in viewModel.Template.Objects.Where(item => item.Name is "PO" or "QR").ToArray())
            {
                viewModel.Template.Objects.Remove(first);
            }

            Pump();
            Require(canvas.PasteCopiedObjects() && Places() == "PO@30,5 PO (bản sao)@33,8 QR@5,5 QR (bản sao)@8,8",
                "A paste takes the free place of the deleted copies: " + Places());
            Require(canvas.PasteCopiedObjects() && Places() == "PO@30,5 PO (bản sao)@33,8 PO (bản sao) 2@36,11 QR@5,5 QR (bản sao)@8,8 QR (bản sao) 2@11,11",
                "A paste never lands on a copy still on the label: " + Places());

            // A function named like a column of the label's data stays behind: a function of that
            // name would stop all the label's functions. The pasted object reads the column.
            viewModel.NewTemplate(new ANLAbel.App.ViewModels.NewTemplateRequest("Tem C", 80, 50, 203));
            viewModel.ExcelHeaders.Add("Mã hàng");
            Pump();
            Require(canvas.PasteCopiedObjects(), "The objects paste into a third label.");
            Require(string.Join(", ", viewModel.Template.DataTransforms.Select(transform => transform.Name)) == "Mã QR, Tổng",
                "Mã hàng stays behind for the column of that name: " + string.Join(", ", viewModel.Template.DataTransforms.Select(transform => transform.Name)));
            Require(viewModel.StatusText.Contains("đọc cột “Mã hàng”", StringComparison.Ordinal), "The status line says the label reads the column: " + viewModel.StatusText);

            // An object reaching past the label edge keeps its place in a paste at its own place:
            // the layout is copied as it is.
            viewModel.Template.Objects.Add(new LabelObject { Type = ObjectType.Rectangle, Name = "Lề", XMm = -1, YMm = 2, WidthMm = 10, HeightMm = 5 });
            Pump();
            Select("Lề");
            Require(canvas.CopySelectedObjects(), "Lề is copied.");
            viewModel.NewTemplate(new ANLAbel.App.ViewModels.NewTemplateRequest("Tem D", 80, 50, 203));
            Pump();
            Require(canvas.PasteCopiedObjects() && viewModel.Template.Objects.Single() is { XMm: -1.0, YMm: 2.0 },
                "A paste at its own place keeps an object past the edge where it was: " + Places());

            // The Data Manager replacing every function computes the shown row once, not once per function.
            viewModel.SelectedDataItem = table.DefaultView[0];
            Pump();
            var rowChanges = 0;
            void CountRow(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
            {
                if (e.PropertyName == nameof(ANLAbel.App.ViewModels.MainViewModel.PreviewRow))
                {
                    rowChanges++;
                }
            }

            viewModel.PropertyChanged += CountRow;
            viewModel.ApplyDataDefinitions(viewModel.Template.PromptVariables.ToArray(), viewModel.Template.DataTransforms.ToArray(), viewModel.Template.SerialCounter.Clone());
            viewModel.PropertyChanged -= CountRow;
            Require(rowChanges <= 1, "Applying the Data Manager computes the shown row once: " + rowChanges);

            // Several objects step together: a place is free only when none of them lands on an
            // object of its kind, also when one original has moved away or one copy is gone.
            viewModel.Template.Objects.Add(new LabelObject { Type = ObjectType.Rectangle, Name = "A", ZIndex = 1, XMm = 10, YMm = 10, WidthMm = 5, HeightMm = 5 });
            viewModel.Template.Objects.Add(new LabelObject { Type = ObjectType.Rectangle, Name = "B", ZIndex = 2, XMm = 30, YMm = 10, WidthMm = 5, HeightMm = 5 });
            Pump();
            Select("A", "B");
            Require(canvas.CopySelectedObjects(), "A and B are copied.");
            Named("A").XMm = 50;
            Pump();
            Require(canvas.PasteCopiedObjects() && Places() == "A@50,10 A (bản sao)@13,13 B@30,10 B (bản sao)@33,13 Lề@-1,2",
                "With A moved away the paste still steps past B: " + Places());
            viewModel.Template.Objects.Remove(Named("A (bản sao)"));
            Pump();
            Require(canvas.PasteCopiedObjects() && Places() == "A@50,10 A (bản sao)@16,16 B@30,10 B (bản sao)@33,13 B (bản sao) 2@36,16 Lề@-1,2",
                "A copy gone from under one object does not free the place for all: " + Places());

            // An object reading a column of its label carries no function of a like name.
            viewModel.Template.DataTransforms.Add(new ANLAbel.Core.Data.DataTransformDefinition("Mã QR", "CONCAT(\"Q\", \"R\")"));
            // A column saved with the label counts while its file is not read.
            viewModel.LabelDatabaseFields.Add(new DatabaseField { Name = "Mã_QR" });
            viewModel.Template.Objects.Add(new LabelObject { Type = ObjectType.Text, Name = "Cột", BindingExpression = "{Mã_QR}", XMm = 5, YMm = 30, WidthMm = 20, HeightMm = 6 });
            Pump();
            Select("Cột");
            Require(canvas.CopySelectedObjects(), "Cột is copied.");
            var carried = (ANLAbel.Core.Data.DataDefinitionImport.Carried)typeof(ANLAbel.App.Controls.LabelDesignerCanvas)
                .GetField("_clipboardDefinitions", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(canvas)!;
            Require(carried.Transforms.Count == 0,
                "Cột reads the column “Mã_QR”, not the function “Mã QR”: " + string.Join(", ", carried.Transforms.Select(transform => transform.Name)));
            // Not closed: closing an edited label asks whether to save it.
        });

        return Task.CompletedTask;
    }

    /// <summary>
    /// Deleting a function or a print-time question in Quản lý dữ liệu was silent, even when
    /// objects used it: a barcode then read a missing field and a question lost its rules.
    /// NiceLabel 10 refuses such a deletion; the Data Manager now names the objects and
    /// functions that use the item and what deleting breaks, and keeps it by default.
    /// </summary>
    public static Task DeletingAUsedDefinitionAsksFirst()
    {
        RunOnSta(() =>
        {
            var viewModel = new ANLAbel.App.ViewModels.MainViewModel();
            var template = viewModel.Template;
            template.DataTransforms.Add(new ANLAbel.Core.Data.DataTransformDefinition("Mã QR", "CONCAT(FIELD(\"PO\"), FIELD(\"?Lô\"))"));
            template.DataTransforms.Add(new ANLAbel.Core.Data.DataTransformDefinition("Tổng hợp", "CONCAT(FIELD(\"Mã QR\"), \"#\")"));
            template.DataTransforms.Add(new ANLAbel.Core.Data.DataTransformDefinition("Rời", "CONCAT(\"Mã QR\", FIELD(\"X\"))"));
            template.PromptVariables.Add(new PromptVariableDefinition { Name = "Lô", MaxLength = 8 });
            template.PromptVariables.Add(new PromptVariableDefinition { Name = "Ca" });
            template.PromptVariables.Add(new PromptVariableDefinition { Name = "Thừa" });
            template.Objects.Add(new LabelObject { Type = ObjectType.Text, Name = "QR", BindingExpression = "{Mã QR}" });
            template.Objects.Add(new LabelObject { Type = ObjectType.Text, Name = "Lô in", BindingExpression = "Lô: {?Lô}" });
            template.Objects.Add(new LabelObject { Type = ObjectType.Text, Name = "Chỉ ca", BindingExpression = "Ca", VisibilityCondition = "{?Ca}" });
            var window = new ANLAbel.App.DataManagerWindow(viewModel);
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var functions = ((System.Collections.IEnumerable)typeof(ANLAbel.App.DataManagerWindow).GetField("_functions", flags)!.GetValue(window)!).Cast<object>().ToArray();
            var prompts = ((System.Collections.IEnumerable)typeof(ANLAbel.App.DataManagerWindow).GetField("_prompts", flags)!.GetValue(window)!).Cast<PromptVariableDefinition>().ToArray();
            ANLAbel.App.Services.DefinitionUsers.Users UsersOf(object item)
                => (ANLAbel.App.Services.DefinitionUsers.Users)typeof(ANLAbel.App.DataManagerWindow).GetMethod("UsersOf", flags)!.Invoke(window, new[] { item })!;
            string Name(object draft) => (string)draft.GetType().GetProperty("Name")!.GetValue(draft)!;
            object Function(string name) => functions.Single(item => Name(item) == name);
            string Show(ANLAbel.App.Services.DefinitionUsers.Users users) => string.Join(",", users.Objects) + " / " + string.Join(",", users.Functions);

            var qr = UsersOf(Function("Mã QR"));
            Require(Show(qr) == "QR / Tổng hợp", "Mã QR is used by the QR text and the Tổng hợp function: " + Show(qr));
            Require(Show(UsersOf(Function("Rời"))) == " / ", "Nothing reads Rời; a quoted “Mã QR” in its formula is no use of Mã QR.");
            Require(Show(UsersOf(prompts.Single(prompt => prompt.Name == "Lô"))) == "Lô in / Mã QR", "Lô is asked by the Lô in text and read by Mã QR.");
            Require(Show(UsersOf(prompts.Single(prompt => prompt.Name == "Ca"))) == "Chỉ ca / ", "A visibility condition is a use of Ca.");
            Require(!UsersOf(prompts.Single(prompt => prompt.Name == "Thừa")).Any, "Nothing uses Thừa: it is deleted without a question.");

            // Renamed in the draft, the objects still read the old name until Đồng ý: still a use.
            var draft = Function("Mã QR");
            draft.GetType().GetProperty("Name")!.SetValue(draft, "QR mới");
            Require(Show(UsersOf(draft)) == "QR / Tổng hợp", "Renamed in the draft, Mã QR is still in use: " + Show(UsersOf(draft)));

            var lines = ANLAbel.App.Services.DefinitionUsers.FunctionLines("Mã QR", qr);
            Require(lines[0] == "Hàm “Mã QR” đang được dùng ở đối tượng “QR”; hàm “Tổng hợp”.", "The question names the users: " + lines[0]);
            var promptLines = ANLAbel.App.Services.DefinitionUsers.PromptLines("Lô", UsersOf(prompts.Single(prompt => prompt.Name == "Lô")));
            Require(promptLines[1].Contains("mất quy tắc nhập", StringComparison.Ordinal), "The question says the prompt would lose its rules: " + promptLines[1]);

            // A function read by a loose name, as FIELD finds it, is in use too.
            template.Objects.Add(new LabelObject { Type = ObjectType.Text, Name = "Mã rời", BindingExpression = "{Mã_QR}" });
            Require(UsersOf(draft).Objects.Contains("Mã rời"), "A loose name reaches the function as FIELD does: " + Show(UsersOf(draft)));

            // A column of that name saved with the label is what the object reads, file read or not.
            template.DatabaseConfig.LabelFields.Add(new DatabaseField { Name = "Mã_QR" });
            Require(!UsersOf(draft).Objects.Contains("Mã rời"), "A saved column is no use of the function: " + Show(UsersOf(draft)));
            template.DatabaseConfig.LabelFields.Clear();
            // Not closed: closing an edited Data Manager asks whether to keep the changes.
        });

        return Task.CompletedTask;
    }

    /// <summary>
    /// A small object under a larger one could only be reached by clicking around it, and
    /// zoom needed the mouse wheel. As on NiceLabel 10's design surface, Tab and Shift+Tab
    /// now select the next or previous object in paint order, round to the start, and Ctrl
    /// with plus, minus or 0 zooms in, out and to 100 %.
    /// </summary>
    public static Task TabWalksTheObjectsAndCtrlZooms()
    {
        RunOnSta(() =>
        {
            var window = new ANLAbel.App.MainWindow();
            var viewModel = (ANLAbel.App.ViewModels.MainViewModel)window.DataContext;
            window.Measure(new Size(1440, 900));
            window.Arrange(new Rect(0, 0, 1440, 900));
            window.UpdateLayout();
            void Pump() => System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);
            var canvas = (ANLAbel.App.Controls.LabelDesignerCanvas)window.FindName("DesignerCanvas");
            foreach (var (name, z, visible) in new[] { ("Nền", 0, true), ("Mã", 2, true), ("Ẩn", 3, false), ("Logo", 1, true) })
            {
                viewModel.Template.Objects.Add(new LabelObject { Type = ObjectType.Rectangle, Name = name, ZIndex = z, IsVisible = visible, XMm = 3, YMm = 3, WidthMm = 20, HeightMm = 10 });
            }

            Pump();
            viewModel.SelectedObject = null;
            Pump();
            string Tab()
            {
                var press = new System.Windows.Input.KeyEventArgs(System.Windows.Input.Keyboard.PrimaryDevice, new TestPresentationSource(), 0, System.Windows.Input.Key.Tab)
                {
                    RoutedEvent = System.Windows.Input.Keyboard.PreviewKeyDownEvent
                };
                canvas.RaiseEvent(press);
                Pump();
                Require(press.Handled, "Tab on the label is taken by the label.");
                return viewModel.SelectedObject?.Name ?? "(none)";
            }

            var walk = string.Join(" ", Tab(), Tab(), Tab(), Tab());
            Require(walk == "Nền Logo Mã Nền", "Tab walks the shown objects in paint order and starts again: " + walk);
            bool Back() => (bool)typeof(ANLAbel.App.MainWindow).GetMethod("SelectNextByKey", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(window, new object[] { System.Windows.Input.Key.Tab, System.Windows.Input.ModifierKeys.Shift })!;
            Require(Back() && viewModel.SelectedObject?.Name == "Mã", "Shift+Tab goes back round to the top object: " + viewModel.SelectedObject?.Name);

            // From a multiple selection, Tab goes on alone from the key object.
            window.InputBindings.OfType<System.Windows.Input.KeyBinding>()
                .Single(binding => binding.Key == System.Windows.Input.Key.A && binding.Modifiers == System.Windows.Input.ModifierKeys.Control)
                .Command.Execute(null);
            Pump();
            Require(canvas.SelectedObjectCount == 3, "Ctrl+A selects the three shown objects: " + canvas.SelectedObjectCount);
            var after = Tab();
            Require(canvas.SelectedObjectCount == 1, $"Tab leaves one object selected ({after}): {canvas.SelectedObjectCount}.");

            // A group is one stop, at its first member in paint order, selected whole as a click
            // selects it, even when another object sits between its members.
            LabelObject Named(string name) => viewModel.Template.Objects.Single(item => item.Name == name);
            viewModel.Template.Objects.Add(new LabelObject { Type = ObjectType.Rectangle, Name = "Viền", ZIndex = 4, XMm = 3, YMm = 3, WidthMm = 20, HeightMm = 10 });
            Named("Logo").GroupId = "nhóm";
            Named("Viền").GroupId = "nhóm";
            Pump();
            canvas.SelectAsClicked(Named("Nền"));
            Pump();
            var grouped = Tab();
            Require(grouped == "Logo" && canvas.SelectedObjectCount == 2, $"Tab selects the group whole: {grouped}, {canvas.SelectedObjectCount} selected.");
            var rest = string.Join(" ", Tab(), Tab());
            Require(rest == "Mã Nền", "Tab passes the group's later member: " + rest);

            double Zoom(System.Windows.Input.Key key)
            {
                window.InputBindings.OfType<System.Windows.Input.KeyBinding>()
                    .Single(binding => binding.Key == key && binding.Modifiers == System.Windows.Input.ModifierKeys.Control)
                    .Command.Execute(null);
                return Math.Round(viewModel.Zoom, 2);
            }

            viewModel.Zoom = 1;
            Require(Zoom(System.Windows.Input.Key.OemPlus) == 1.1 && Zoom(System.Windows.Input.Key.Add) == 1.2, "Ctrl and plus zoom in: " + viewModel.Zoom);
            Require(Zoom(System.Windows.Input.Key.OemMinus) == 1.1 && Zoom(System.Windows.Input.Key.Subtract) == 1.0, "Ctrl and minus zoom out: " + viewModel.Zoom);
            viewModel.Zoom = 2.5;
            Require(Zoom(System.Windows.Input.Key.D0) == 1 && Zoom(System.Windows.Input.Key.NumPad0) == 1, "Ctrl+0 shows 100 %: " + viewModel.Zoom);
            // Not closed: closing an edited label asks whether to save it.
        });

        return Task.CompletedTask;
    }

    /// <summary>
    /// A locked object showed nothing on the label, and a drag on it did nothing without a
    /// word. As NiceLabel 10 marks locked objects, a padlock now sits at the top-right of
    /// each one (View: Đối tượng khóa), and pressing the selected locked object says why
    /// it stays and how to unlock it.
    /// </summary>
    public static Task LockedObjectsShowALockAndSayWhyTheyStay()
    {
        RunOnSta(() =>
        {
            var template = new LabelTemplate { WidthMm = 60, HeightMm = 30 };
            template.Objects.Add(new LabelObject { Type = ObjectType.Rectangle, Name = "Hộp", XMm = 10, YMm = 10, WidthMm = 20, HeightMm = 8, IsLocked = true });
            var canvas = new ANLAbel.App.Controls.LabelDesignerCanvas { Template = template, Zoom = 1 };
            var size = new Size(MmConverter.MmToDip(60), MmConverter.MmToDip(30));
            canvas.Measure(size);
            canvas.Arrange(new Rect(size));
            canvas.UpdateLayout();
            int MarkerPixels()
            {
                var bitmap = new RenderTargetBitmap((int)Math.Ceiling(size.Width), (int)Math.Ceiling(size.Height), 96, 96, PixelFormats.Pbgra32);
                bitmap.Render(canvas);
                var stride = bitmap.PixelWidth * 4;
                var pixels = new byte[stride * bitmap.PixelHeight];
                bitmap.CopyPixels(pixels, stride, 0);
                var left = (int)MmConverter.MmToDip(30) + 1;
                var top = (int)MmConverter.MmToDip(10) - 11;
                var count = 0;
                for (var y = top; y < top + 12; y++)
                {
                    for (var x = left; x < left + 11; x++)
                    {
                        var index = y * stride + x * 4;
                        // The marker colour, rgb(124, 58, 237): much more blue than green.
                        if (pixels[index] > 150 && pixels[index + 1] < 120 && pixels[index + 3] > 0)
                        {
                            count++;
                        }
                    }
                }

                return count;
            }

            var shown = MarkerPixels();
            canvas.ShowLockedMarkers = false;
            canvas.UpdateLayout();
            var hidden = MarkerPixels();
            Require(shown > 10 && hidden == 0, $"A locked object carries a padlock that the View switch hides: {shown} marker pixels shown, {hidden} hidden.");

            var window = new ANLAbel.App.MainWindow();
            var viewModel = (ANLAbel.App.ViewModels.MainViewModel)window.DataContext;
            window.Measure(new Size(1440, 900));
            window.Arrange(new Rect(0, 0, 1440, 900));
            window.UpdateLayout();
            void Pump() => System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);
            var designer = (ANLAbel.App.Controls.LabelDesignerCanvas)window.FindName("DesignerCanvas");
            var box = new LabelObject { Type = ObjectType.Rectangle, Name = "Hộp", XMm = 10, YMm = 10, WidthMm = 20, HeightMm = 8, IsLocked = true };
            viewModel.Template.Objects.Add(box);
            viewModel.Template.Objects.Add(new LabelObject { Type = ObjectType.Rectangle, Name = "Khác", XMm = 40, YMm = 10, WidthMm = 10, HeightMm = 8 });
            Pump();
            viewModel.SelectedObject = viewModel.Template.Objects.Single(item => item.Name == "Khác");
            Pump();
            var element = ((System.Collections.IDictionary)typeof(ANLAbel.App.Controls.LabelDesignerCanvas)
                .GetField("_objectElements", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(designer)!)[box] as UIElement;
            void Press()
            {
                element!.RaiseEvent(new System.Windows.Input.MouseButtonEventArgs(System.Windows.Input.Mouse.PrimaryDevice, Environment.TickCount, System.Windows.Input.MouseButton.Left)
                {
                    RoutedEvent = UIElement.PreviewMouseLeftButtonDownEvent
                });
                Pump();
            }

            viewModel.StatusText = "Sẵn sàng";
            Press();
            Require(ReferenceEquals(viewModel.SelectedObject, box) && viewModel.StatusText == "Sẵn sàng", "The first press selects the locked object without a message: " + viewModel.StatusText);
            Press();
            Require(viewModel.StatusText == "“Hộp” đã khóa vị trí: bỏ Khóa vị trí (chuột phải) để di chuyển." && box.XMm == 10,
                "Pressing the selected locked object says why it stays: " + viewModel.StatusText);
            // Not closed: closing an edited label asks whether to save it.
        });

        return Task.CompletedTask;
    }

    /// <summary>
    /// Customer sheets fill a formula or a constant down past their records (1
    /// record and 198 rows holding only a weight; 1 record and 8 rows holding
    /// only the maker), and every row started selected, so each job asked to
    /// skip them or printed near-blank labels. Rows without a record for the
    /// label now start unselected, Print Preview says which, and "In mọi dòng"
    /// leaves them out. Identical rows meant as copies, a run of two, and a sheet
    /// without the label's columns are left alone.
    /// </summary>
    public static Task RowsWithoutLabelDataStartUnselected()
    {
        var template = new LabelTemplate { Name = "Thùng", WidthMm = 50, HeightMm = 30 };
        template.Objects.Add(new LabelObject { Type = ObjectType.Text, Name = "Mã", BindingExpression = "{Ma}", XMm = 3, YMm = 3, WidthMm = 30, HeightMm = 6 });
        template.Objects.Add(new LabelObject { Type = ObjectType.Text, Name = "Lô", BindingExpression = "{Lo}", XMm = 3, YMm = 12, WidthMm = 30, HeightMm = 6 });
        template.Objects.Add(new LabelObject { Type = ObjectType.Text, Name = "Hãng", BindingExpression = "{Hang}", XMm = 3, YMm = 21, WidthMm = 30, HeightMm = 6 });
        IReadOnlyDictionary<string, string>? Row(string ma, string lo, string hang) =>
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Ma"] = ma, ["Lo"] = lo, ["Hang"] = hang, ["KL"] = "0" };
        IReadOnlyList<int> Without(params IReadOnlyDictionary<string, string>?[] rows) => ANLAbel.App.Services.LabelDataRows.WithoutData(template, rows);
        var filledDown = Without(Row("SP-1", "L1", "ACME"), Row("SP-2", "L2", "ACME"), Row("", "", "ACME"), Row("", "", "ACME"), Row("", "", "ACME"), Row("", "", "ACME"));
        Require(filledDown.SequenceEqual([2, 3, 4, 5]), "A constant filled down past the records: " + string.Join(", ", filledDown));
        Require(ANLAbel.App.Services.LabelDataRows.WithoutData(template, [Row("SP-1", "L1", "ACME"), Row("", "", "ACME"), Row("", "", "ACME"), Row("", "", "ACME")], filledDown: false).Count == 0,
            "Quick print leaves out blank rows only, not a guessed run.");
        Require(Without(Row("SP-1", "L1", "ACME"), Row("", "", ""), Row("SP-2", "L2", "ACME")).SequenceEqual([1]), "A blank row in the middle.");
        Require(Without(Row("SP-1", "L1", "ACME"), Row("SP-1", "L1", "ACME"), Row("SP-1", "L1", "ACME"), Row("SP-1", "L1", "ACME")).Count == 0, "Identical full rows are copies.");
        Require(Without(Row("SP-1", "L1", "ACME"), Row("", "", "ACME"), Row("", "", "ACME")).Count == 0, "A run of two is left alone.");
        Require(Without(Row("", "", "ACME"), Row("", "", "ACME"), Row("", "", "ACME")).Count == 0, "A run with no record above it is left alone.");
        Require(Without(Row("A", "L1", "ACME"), Row("B", "L2", ""), Row("B", "L2", ""), Row("B", "L2", "")).Count == 0,
            "Records that leave one field blank are not a filled-down run.");
        var pair = new LabelTemplate { Name = "Hai cột", WidthMm = 50, HeightMm = 30 };
        pair.Objects.Add(new LabelObject { Type = ObjectType.Text, Name = "Mã", BindingExpression = "{Ma}", XMm = 3, YMm = 3, WidthMm = 30, HeightMm = 6 });
        pair.Objects.Add(new LabelObject { Type = ObjectType.Text, Name = "Hãng", BindingExpression = "{Hang}", XMm = 3, YMm = 12, WidthMm = 30, HeightMm = 6 });
        Require(ANLAbel.App.Services.LabelDataRows.WithoutData(pair, [Row("A", "", "ACME"), Row("B", "", ""), Row("B", "", ""), Row("B", "", "")]).Count == 0,
            "Half the fields blank (product B, no maker) is a record, not a filled-down run.");
        var other = new LabelTemplate { Name = "Khác", WidthMm = 50, HeightMm = 30 };
        other.Objects.Add(new LabelObject { Type = ObjectType.Text, Name = "Khác", BindingExpression = "{Khac}", XMm = 3, YMm = 3, WidthMm = 30, HeightMm = 6 });
        Require(ANLAbel.App.Services.LabelDataRows.WithoutData(other, [Row("", "", ""), Row("", "", "")]).Count == 0,
            "A sheet without the label's columns is not read as empty rows.");

        var directory = Directory.CreateTempSubdirectory("anlabel-empty-rows-");
        try
        {
            RunOnSta(() =>
            {
                var path = Path.Combine(directory.FullName, "orders.xlsx");
                using (var workbook = new ClosedXML.Excel.XLWorkbook())
                {
                    var sheet = workbook.AddWorksheet("Orders");
                    sheet.Cell(1, 1).Value = "Ma";
                    sheet.Cell(1, 2).Value = "Lo";
                    sheet.Cell(1, 3).Value = "KL";
                    for (var index = 0; index < 5; index++)
                    {
                        if (index < 2)
                        {
                            sheet.Cell(index + 2, 1).Value = "SP-" + (index + 1);
                            sheet.Cell(index + 2, 2).Value = "L" + (index + 1);
                        }

                        sheet.Cell(index + 2, 3).Value = 0;
                    }

                    workbook.SaveAs(path);
                }

                var viewModel = new ANLAbel.App.ViewModels.MainViewModel();
                viewModel.ImportExcelAsync(path, "Orders").GetAwaiter().GetResult();
                viewModel.Template.Objects.Add(new LabelObject { Type = ObjectType.Text, Name = "Mã", BindingExpression = "{Ma}", XMm = 3, YMm = 3, WidthMm = 30, HeightMm = 6, Style = { BorderThicknessMm = 0 } });
                viewModel.Template.Objects.Add(new LabelObject { Type = ObjectType.Text, Name = "Lô", BindingExpression = "{Lo}", XMm = 3, YMm = 12, WidthMm = 30, HeightMm = 6, Style = { BorderThicknessMm = 0 } });
                Require(viewModel.TryBuildPrintPreviewRows(out var rows, out var rowError) && rows.Count == 5, "All five rows are read: " + rowError);
                var window = new ANLAbel.App.PrintPreviewWindow(
                    viewModel.Template,
                    rows[0],
                    viewModel.ExcelDataView,
                    new PrintService(),
                    new ANLAbel.Data.PrintLogs.PrintLogService(Path.Combine(directory.FullName, "log.csv")),
                    string.Empty,
                    preparedRows: rows,
                    excelSourceIdentity: viewModel.ImportedExcelDataSourceIdentity);
                var deadline = DateTime.UtcNow.AddSeconds(30);
                do
                {
                    System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);
                    Thread.Sleep(20);
                }
                while ((window.IsPreviewBusy || window.Pages.Count == 0) && DateTime.UtcNow < deadline);

                var list = (ListBox)window.FindName("TrackingList");
                var selected = list.Items.Cast<ANLAbel.App.ViewModels.TrackingRowViewModel>().Where(row => row.IsSelected).Select(row => row.Col1 ?? string.Empty).ToArray();
                Require(selected.SequenceEqual(["SP-1", "SP-2"]) && window.Pages.Count == 2,
                    $"Only the rows with a record start selected ({string.Join(", ", selected)}; {window.Pages.Count} labels prepared).");
                Require(window.EmptyRowsText.StartsWith("3 dòng không có dữ liệu cho tem", StringComparison.Ordinal) && window.EmptyRowsText.Contains("dòng 3–5.", StringComparison.Ordinal),
                    "Print Preview names the rows without a record: " + window.EmptyRowsText);
                window.Close();
            });

            // "In mọi dòng" leaves them out too (source check, like the hidden rows).
            var root = AppContext.BaseDirectory;
            while (!File.Exists(Path.Combine(root, "ANLAbel.slnx")))
            {
                root = Path.GetDirectoryName(root.TrimEnd(Path.DirectorySeparatorChar))!;
            }

            var source = File.ReadAllText(Path.Combine(root, "src", "ANLAbel.App", "ViewModels", "MainViewModel.cs"));
            var start = source.IndexOf("private async Task PrintAllRowsAsync()", StringComparison.Ordinal);
            var body = source.Substring(start, source.IndexOf("private async Task<TrackedPrintResult> DispatchTrackedPrintAsync(", StringComparison.Ordinal) - start);
            Require(body.Contains("LabelDataRows.WithoutData(Template", StringComparison.Ordinal) && body.Contains("filledDown: false", StringComparison.Ordinal)
                    && body.Contains("+ emptyNote", StringComparison.Ordinal),
                "In mọi dòng must leave out the blank rows, and only those, and say which.");
        }
        finally
        {
            try
            {
                directory.Delete(recursive: true);
            }
            catch (IOException)
            {
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// With no file linked, the Properties panel checked a bound barcode's
    /// placeholder as its data: "{Mã Viettel}" was "a character that cannot be
    /// encoded: ã", and a long placeholder "a readable line wider than the
    /// frame", under "Dữ liệu không hợp lệ". A barcode without a row to read is
    /// not checked as data, a readable line too wide is told with the barcode
    /// settings only, and real data is checked as before.
    /// </summary>
    public static Task PlaceholderBarcodesAreNotCheckedAsData()
    {
        RunOnSta(() =>
        {
            var viewModel = new ANLAbel.App.ViewModels.MainViewModel();
            viewModel.Template.Objects.Add(new LabelObject { Type = ObjectType.BarcodeCode128, BarcodeSymbology = BarcodeSymbology.Code128, Name = "Viettel", BindingExpression = "{Mã Viettel}", XMm = 3, YMm = 3, WidthMm = 40, HeightMm = 12 });
            viewModel.SelectedObject = viewModel.Template.Objects[0];
            Require(viewModel.BarcodeValidationMessage.Length == 0 && viewModel.BarcodeApplicationValidationMessage.Length == 0,
                $"A placeholder is not data: {viewModel.BarcodeValidationMessage} / {viewModel.BarcodeApplicationValidationMessage}");

            var previewRow = typeof(ANLAbel.App.ViewModels.MainViewModel).GetProperty("PreviewRow")!;
            previewRow.SetValue(viewModel, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Mã Viettel"] = "Mã 01" });
            Require(viewModel.BarcodeApplicationValidationMessage.Contains("không mã hóa được", StringComparison.Ordinal),
                "Real data is still checked: " + viewModel.BarcodeApplicationValidationMessage);

            viewModel.Template.Objects.Add(new LabelObject
            {
                Type = ObjectType.BarcodeCode128, BarcodeSymbology = BarcodeSymbology.Code128, Name = "Hẹp", Text = "SEMCP-MC-00018-LF-2026-WEEK-14",
                XMm = 3, YMm = 20, WidthMm = 12, HeightMm = 12, BarcodeHriPlacement = BarcodeHriPlacement.Below, BarcodeTextFontSizePt = 14
            });
            viewModel.SelectedObject = viewModel.Template.Objects[1];
            Require(!viewModel.BarcodeValidationMessage.StartsWith("Dữ liệu không hợp lệ", StringComparison.Ordinal)
                    && viewModel.BarcodeApplicationValidationMessage.Contains("Dòng chữ dưới mã", StringComparison.Ordinal),
                $"A readable line too wide is a setting, not bad data: {viewModel.BarcodeValidationMessage} / {viewModel.BarcodeApplicationValidationMessage}");
        });

        return Task.CompletedTask;
    }

    /// <summary>
    /// A customer sheet with one record and a weight formula filled down 198
    /// rows: printing skips the 198 blocked rows after asking, and the question
    /// listed all 198 row numbers. Runs of rows now read as ranges.
    /// </summary>
    public static Task SkippedRowsReadAsRanges()
    {
        var all = ANLAbel.App.Services.PreflightIssueText.RowRanges(Enumerable.Range(3, 198));
        var mixed = ANLAbel.App.Services.PreflightIssueText.RowRanges([9, 2, 4, 5, 6, 5]);
        Require(all == "3–200" && mixed == "2, 4–6, 9" && ANLAbel.App.Services.PreflightIssueText.RowRanges([]) == string.Empty,
            $"Rows read as ranges: {all} / {mixed}");
        return Task.CompletedTask;
    }

    /// <summary>
    /// A formula over columns not linked yet also said "Field 'PO' was not
    /// found." for each column it already named as missing, so an imported
    /// label's check stayed red. The repeat is dropped and the formula waits for
    /// the file like a plain field; a real formula fault still shows. A Data
    /// Matrix too small for its data no longer shows ZXing's English words.
    /// </summary>
    public static Task FormulaColumnsWaitForTheFile()
    {
        var viewModel = new ANLAbel.App.ViewModels.MainViewModel();
        viewModel.Template.Objects.Add(new LabelObject { Type = ObjectType.Text, Name = "Mã", BindingExpression = "CONCAT(FIELD(\"PO\"), \"/\", FIELD(\"Lot\"))", XMm = 3, YMm = 3, WidthMm = 30, HeightMm = 6 });
        var issue = viewModel.BindingIssues.Single();
        Require(viewModel.AwaitsExcelData && issue.Errors.Count == 0 && issue.MissingFieldsSummary == "Cần cột: PO, Lot"
                && issue.StatusText == "Chưa liên kết tệp Excel; đối tượng cần 2 cột.",
            $"The formula waits for the file: {viewModel.BindingIssuesSummary} / {issue.StatusText} / {issue.MissingFieldsSummary} / {string.Join(" ", issue.Errors)}");

        viewModel.Template.Objects.Add(new LabelObject { Type = ObjectType.Text, Name = "Sai", BindingExpression = "CONCAT(FIELD(\"PO\"), MID(\"x\"))", XMm = 3, YMm = 12, WidthMm = 30, HeightMm = 6 });
        var fault = viewModel.BindingIssues.Single(item => item.ObjectName == "Sai");
        Require(!viewModel.AwaitsExcelData && fault.Errors.Any(error => error.Contains("MID", StringComparison.Ordinal)),
            "A formula fault still shows: " + string.Join(" ", fault.Errors));

        var capacity = ANLAbel.App.Services.PreflightIssueText.Sentence("Can't find a symbol arrangement that matches the message. Data codewords: 79");
        Require(capacity == "Dữ liệu cần 79 từ mã, quá sức chứa của khổ Data Matrix đã chọn; chọn khổ lớn hơn hoặc Tự động, hoặc rút ngắn dữ liệu.",
            "The Data Matrix capacity message is in Vietnamese: " + capacity);
        return Task.CompletedTask;
    }

    /// <summary>
    /// A NiceLabel label just imported, with no file linked yet, opened with a
    /// red "12 đối tượng bị hỏng…" and each object "thiếu trường trong bảng tính
    /// hiện tại", when there was no sheet at all. With no column known and no
    /// other fault, the check now waits for the file and names the columns it
    /// must have; a real fault, or a linked file without the column, still reads
    /// as before.
    /// </summary>
    public static Task BindingCheckWaitsForTheFile()
    {
        var viewModel = new ANLAbel.App.ViewModels.MainViewModel();
        viewModel.Template.Objects.Add(new LabelObject { Type = ObjectType.Text, Name = "Số PO", BindingExpression = "{PO}", XMm = 3, YMm = 3, WidthMm = 30, HeightMm = 6 });
        viewModel.Template.Objects.Add(new LabelObject { Type = ObjectType.BarcodeCode128, BarcodeSymbology = BarcodeSymbology.Code128, Name = "QTY", BindingExpression = "{QTY}", XMm = 3, YMm = 12, WidthMm = 40, HeightMm = 10 });
        var issue = viewModel.BindingIssues.Single(item => item.ObjectName == "QTY");
        Require(viewModel.HasBindingIssues && viewModel.AwaitsExcelData
                && viewModel.BindingIssuesSummary == "Chưa liên kết tệp Excel: 2 đối tượng đọc 2 cột (PO, QTY). Liên kết tệp có các cột này để kiểm tra."
                && issue.StatusText == "Chưa liên kết tệp Excel; đối tượng cần 1 cột." && issue.MissingFieldsSummary == "Cần cột: QTY",
            $"With no file linked the check names the columns: {viewModel.BindingIssuesSummary} / {issue.StatusText} / {issue.MissingFieldsSummary}");

        viewModel.SelectedObject = viewModel.Template.Objects[1];
        Require(viewModel.SelectedBindingMissingFieldsSummary == "Cần cột: QTY", "The Properties panel waits too: " + viewModel.SelectedBindingMissingFieldsSummary);

        // A linked file that cannot be read is a broken link, not a label waiting for its first file.
        viewModel.Template.DatabaseConfig.FilePath = "C:/khong-co/nhan.xlsx";
        viewModel.Template.DatabaseConfig.SheetName = "Sheet1";
        viewModel.Template.Objects[0].XMm = 3.5;
        Require(!viewModel.AwaitsExcelData && !viewModel.BindingIssuesSummary.StartsWith("Chưa liên kết tệp Excel", StringComparison.Ordinal),
            "A broken link does not read as no link: " + viewModel.BindingIssuesSummary);
        issue = viewModel.BindingIssues.Single(item => item.ObjectName == "QTY");
        Require(issue.StatusText == "Chưa đọc được tệp Excel đã liên kết; đối tượng cần 1 cột." && issue.MissingFieldsSummary == "Thiếu: QTY"
                && viewModel.SelectedBindingMissingFieldsSummary == "Trường còn thiếu: QTY",
            $"A broken link names the file not read: {issue.StatusText} / {issue.MissingFieldsSummary} / {viewModel.SelectedBindingMissingFieldsSummary}");
        viewModel.Template.DatabaseConfig.FilePath = string.Empty;
        viewModel.Template.DatabaseConfig.SheetName = string.Empty;
        viewModel.Template.Objects[0].XMm = 3;

        viewModel.Template.Objects.Add(new LabelObject { Type = ObjectType.Text, Name = "Trống", BindingExpression = "Size: { }", XMm = 3, YMm = 24, WidthMm = 30, HeightMm = 6 });
        Require(!viewModel.AwaitsExcelData && viewModel.BindingIssuesSummary.Contains("bị hỏng", StringComparison.Ordinal),
            "A real fault still reads as one: " + viewModel.BindingIssuesSummary);
        viewModel.Template.Objects.RemoveAt(2);

        viewModel.AvailableDatabaseFields.Add(new DatabaseField { Name = "PO" });
        viewModel.Template.Objects[0].XMm = 4;
        issue = viewModel.BindingIssues.Single(item => item.ObjectName == "QTY");
        Require(!viewModel.AwaitsExcelData && issue.StatusText == "Liên kết bị thiếu 1 trường trong bảng tính hiện tại." && issue.MissingFieldsSummary == "Thiếu: QTY",
            $"A linked file without the column is a missing column: {viewModel.BindingIssuesSummary} / {issue.StatusText} / {issue.MissingFieldsSummary}");
        Require(viewModel.SelectedBindingMissingFieldsSummary == "Trường còn thiếu: QTY", "With a file linked the Properties panel names a missing column: " + viewModel.SelectedBindingMissingFieldsSummary);
        return Task.CompletedTask;
    }

    /// <summary>
    /// "Kiểm tra ràng buộc" said in green that every Excel link matched the
    /// workbook on a label that read nothing from Excel. It now says there is
    /// nothing to check yet, and a field without linked data is still an issue.
    /// </summary>
    public static Task BindingCheckSaysWhenThereIsNothingToCheck()
    {
        var viewModel = new ANLAbel.App.ViewModels.MainViewModel();
        viewModel.Template.PromptVariables.Add(new PromptVariableDefinition { Name = "Lot", Prompt = "Số lô" });
        viewModel.Template.Objects.Add(new LabelObject { Type = ObjectType.Text, Name = "Tiêu đề", Text = "PHIẾU HÀNG", XMm = 3, YMm = 3, WidthMm = 30, HeightMm = 6 });
        viewModel.Template.Objects.Add(new LabelObject { Type = ObjectType.Text, Name = "Lô", BindingExpression = "{?Lot}", XMm = 3, YMm = 12, WidthMm = 30, HeightMm = 6 });
        Require(!viewModel.HasBindingIssues && !viewModel.ReadsExcelFields
                && viewModel.BindingIssuesSummary == "Chưa đối tượng nào lấy dữ liệu từ Excel; chưa có liên kết để kiểm tra.",
            "Fixed text and a print-time answer leave nothing to check: " + viewModel.BindingIssuesSummary);

        viewModel.Template.Objects.Add(new LabelObject { Type = ObjectType.Text, Name = "Mã", BindingExpression = "{Ma}", XMm = 3, YMm = 21, WidthMm = 30, HeightMm = 6 });
        Require(viewModel.ReadsExcelFields && viewModel.HasBindingIssues,
            "A field without linked data is an issue: " + viewModel.BindingIssuesSummary);
        return Task.CompletedTask;
    }

    /// <summary>
    /// With three captions selected the Properties title still named one,
    /// "Thuộc tính - Vendor [Chữ]", while a font change reaches all three. The
    /// title now counts the selection after the click that made it.
    /// </summary>
    public static Task PropertiesTitleCountsTheSelection()
    {
        RunOnSta(() =>
        {
            var window = new ANLAbel.App.MainWindow();
            var viewModel = (ANLAbel.App.ViewModels.MainViewModel)window.DataContext;
            window.Measure(new Size(1440, 900));
            window.Arrange(new Rect(0, 0, 1440, 900));
            window.UpdateLayout();
            void Pump()
            {
                for (var i = 0; i < 3; i++)
                {
                    System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);
                }
            }

            foreach (var name in new[] { "Vendor", "PO", "QTY" })
            {
                viewModel.Template.Objects.Add(new LabelObject { Type = ObjectType.Text, Name = name, Text = name, XMm = 3, YMm = 3 + viewModel.Template.Objects.Count * 10, WidthMm = 20, HeightMm = 6, Style = { BorderThicknessMm = 0 } });
            }

            Pump();
            var canvas = (ANLAbel.App.Controls.LabelDesignerCanvas)window.FindName("DesignerCanvas");
            var selection = (HashSet<LabelObject>)typeof(ANLAbel.App.Controls.LabelDesignerCanvas)
                .GetField("_selectedObjects", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(canvas)!;
            // The panel may float in a popup, outside the window's visual tree: search its logical tree.
            TextBlock? Find(DependencyObject parent)
            {
                foreach (var child in LogicalTreeHelper.GetChildren(parent).OfType<DependencyObject>())
                {
                    if (child is TextBlock block && AutomationProperties.GetAutomationId(block) == "Shell.Properties.Title")
                    {
                        return block;
                    }

                    if (Find(child) is { } found)
                    {
                        return found;
                    }
                }

                return null;
            }

            void ClickEnds()
            {
                window.RaiseEvent(new System.Windows.Input.MouseButtonEventArgs(System.Windows.Input.Mouse.PrimaryDevice, 0, System.Windows.Input.MouseButton.Left)
                {
                    RoutedEvent = System.Windows.Input.Mouse.PreviewMouseUpEvent
                });
                Pump();
            }

            viewModel.SelectedObject = viewModel.Template.Objects[0];
            Pump();
            foreach (var item in viewModel.Template.Objects)
            {
                selection.Add(item);
            }

            ClickEnds();
            var panel = (DependencyObject)window.FindName("PropertiesPanel");
            var title = Find(panel)?.Text;
            Require(title == "Thuộc tính - Vendor [Chữ] · 3 đối tượng đang chọn", "The title counts the selection: " + title);

            selection.Clear();
            selection.Add(viewModel.Template.Objects[0]);
            ClickEnds();
            title = Find(panel)?.Text;
            Require(title == "Thuộc tính - Vendor [Chữ]", "One object again: " + title);
            // Not closed: closing an edited label asks whether to save it.
        });

        return Task.CompletedTask;
    }

    /// <summary>
    /// The version list showed each file's hash in a column of its own. A hash
    /// is for support (AGENTS.md keeps technical hashes in details and logs):
    /// the list keeps what the operator chooses a version by, and the details
    /// below it keep the current and backup hashes.
    /// </summary>
    public static Task VersionListKeepsHashesInTheDetails()
    {
        RunOnSta(() =>
        {
            var window = new ANLAbel.App.TemplateRevisionWindow(new ANLAbel.App.ViewModels.MainViewModel());
            var grid = (DataGrid)window.FindName("RevisionGrid");
            var headers = grid.Columns.Select(column => column.Header?.ToString() ?? string.Empty).ToArray();
            Require(!headers.Contains("Mã băm") && headers.Contains("Phiên bản") && headers.Contains("Ghi lần cuối") && headers.Contains("Chẩn đoán"),
                "The version list has no hash column: " + string.Join(" | ", headers));
            window.Close();
        });

        return Task.CompletedTask;
    }

    /// <summary>
    /// "HÀNG DỄ VỠ" printed only when the row's Fragile column said so. Once the
    /// column was renamed in Excel the condition read empty and the warning was
    /// left off every carton without a word. A visibility condition that reads a
    /// column the data does not have now stops the print and names the column;
    /// a condition on a print-time answer is not a column.
    /// </summary>
    public static Task ConditionOnAMissingColumnStopsThePrint()
    {
        var template = new LabelTemplate { Name = "Thùng", WidthMm = 100, HeightMm = 60, Dpi = 203 };
        template.PromptVariables.Add(new PromptVariableDefinition { Name = "Ship", Prompt = "Gửi đi" });
        template.Objects.Add(new LabelObject { Type = ObjectType.Text, Name = "Dễ vỡ", Text = "HÀNG DỄ VỠ", VisibilityCondition = "{Fragile}", XMm = 5, YMm = 5, WidthMm = 40, HeightMm = 8 });
        template.Objects.Add(new LabelObject { Type = ObjectType.Text, Name = "Loại A", Text = "LOẠI A", VisibilityCondition = "EQUALS(FIELD(\"Loai\"), \"A\")", XMm = 5, YMm = 15, WidthMm = 40, HeightMm = 8 });
        template.Objects.Add(new LabelObject { Type = ObjectType.Text, Name = "Gửi", Text = "GỬI ĐI", VisibilityCondition = "{?Ship}", XMm = 5, YMm = 25, WidthMm = 40, HeightMm = 8 });
        var renamed = new System.Data.DataTable();
        renamed.Columns.Add("Ma");
        renamed.Columns.Add("De vo");

        var issues = ANLAbel.App.Services.DataHeaderCheck.FindMissingConditionFields(template, renamed);
        Require(issues.Select(issue => issue.ObjectName).SequenceEqual(["Dễ vỡ", "Loại A"]) && issues.All(issue => issue.IsBlocking),
            "Both conditions on missing columns block; the prompt condition does not: " + string.Join(" | ", issues.Select(issue => issue.ObjectName + ": " + issue.Message)));
        var text = ANLAbel.App.Services.PreflightIssueText.Sentence(issues[0].Message);
        Require(text.Contains("điều kiện hiển thị", StringComparison.OrdinalIgnoreCase) && text.Contains("Fragile", StringComparison.Ordinal),
            "The operator reads which column, in Vietnamese: " + text);
        Require(!ANLAbel.App.Services.DataHeaderCheck.Append(new PrintPreflightResult(Array.Empty<PrintPreflightIssue>()), template, renamed).IsSuccess,
            "Print Preview and quick print stop.");

        var complete = new System.Data.DataTable();
        complete.Columns.Add("Ma");
        complete.Columns.Add("Fragile");
        complete.Columns.Add("Loai");
        Require(ANLAbel.App.Services.DataHeaderCheck.FindMissingConditionFields(template, complete).Count == 0,
            "With the columns back nothing is reported.");
        Require(ANLAbel.App.Services.DataHeaderCheck.FindMissingConditionFields(template, null).Count == 0,
            "With no data linked yet nothing is compared: the label prints without a file.");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Ten captions selected together and set to 9 pt changed only the one the
    /// selection started from: the ribbon and Properties edit the key object.
    /// As in NiceLabel 10, the font now goes to every selected text in one undo
    /// step, Grow Font steps each from its own size, a text box keeps its frame
    /// and a barcode in the selection keeps its own font.
    /// </summary>
    public static Task FontChangeReachesEverySelectedText()
    {
        RunOnSta(() =>
        {
            var window = new ANLAbel.App.MainWindow();
            var viewModel = (ANLAbel.App.ViewModels.MainViewModel)window.DataContext;
            window.Measure(new Size(1440, 900));
            window.Arrange(new Rect(0, 0, 1440, 900));
            window.UpdateLayout();
            void Pump() => System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);
            var canvas = (ANLAbel.App.Controls.LabelDesignerCanvas)window.FindName("DesignerCanvas");
            var selection = (HashSet<LabelObject>)typeof(ANLAbel.App.Controls.LabelDesignerCanvas)
                .GetField("_selectedObjects", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(canvas)!;
            LabelObject Named(string name) => viewModel.Template.Objects.Single(item => item.Name == name);
            void Select(params string[] names)
            {
                viewModel.SelectedObject = Named(names[0]);
                Pump();
                foreach (var name in names)
                {
                    selection.Add(Named(name));
                }
            }

            viewModel.Template.Objects.Add(new LabelObject { Type = ObjectType.Text, Name = "Vendor", Text = "Vendor", XMm = 3, YMm = 3, WidthMm = 20, HeightMm = 6, Style = { FontSizePt = 12, BorderThicknessMm = 0 } });
            viewModel.Template.Objects.Add(new LabelObject { Type = ObjectType.TextBox, Name = "QTY", Text = "QTY", XMm = 3, YMm = 12, WidthMm = 20, HeightMm = 6, Style = { FontSizePt = 10, BorderThicknessMm = 0 } });
            viewModel.Template.Objects.Add(new LabelObject { Type = ObjectType.BarcodeCode128, BarcodeSymbology = BarcodeSymbology.Code128, Name = "Code", Text = "SP-1", XMm = 30, YMm = 3, WidthMm = 40, HeightMm = 12 });
            Pump();
            typeof(ANLAbel.App.ViewModels.MainViewModel).GetMethod("CommitPendingHistory", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(viewModel, null);
            var barcodeFont = Named("Code").Style.FontSizePt;

            Select("Vendor", "QTY", "Code");
            Named("Vendor").Style.FontSizePt = 9;
            Named("Vendor").Style.Bold = true;
            Pump();
            Require(Named("QTY").Style.FontSizePt == 9 && Named("QTY").Style.Bold,
                $"The text box follows the key text: {Named("QTY").Style.FontSizePt} pt, bold {Named("QTY").Style.Bold}.");
            Require(Named("QTY").WidthMm == 20 && Named("QTY").HeightMm == 6, "The text box keeps its frame.");
            Require(Named("Code").Style.FontSizePt == barcodeFont && !Named("Code").Style.Bold, "The barcode keeps its own font.");

            viewModel.UndoCommand.Execute(null);
            Pump();
            Require(Named("Vendor").Style.FontSizePt == 12 && Named("QTY").Style.FontSizePt == 10 && !Named("QTY").Style.Bold,
                $"One undo puts both texts back: {Named("Vendor").Style.FontSizePt} and {Named("QTY").Style.FontSizePt} pt.");

            Select("Vendor", "QTY");
            typeof(ANLAbel.App.MainWindow).GetMethod("StepFontSize", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(window, new object[] { 1.0 });
            Require(Named("Vendor").Style.FontSizePt == 13 && Named("QTY").Style.FontSizePt == 11,
                $"Grow Font steps each text from its own size: {Named("Vendor").Style.FontSizePt} and {Named("QTY").Style.FontSizePt} pt.");

            // Khóa vị trí in the right-click menu locks every selected object too.
            typeof(ANLAbel.App.MainWindow).GetMethod("UpdateDesignerContextMenu", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(window, null);
            var lockItem = canvas.ContextMenu!.Items.OfType<MenuItem>().Single(item => (string)item.Header == "Khóa vị trí");
            lockItem.IsChecked = true;
            lockItem.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            Require(Named("Vendor").IsLocked && Named("QTY").IsLocked, "Khóa vị trí locks every selected object.");
            // Not closed: closing an edited label asks whether to save it.
        });

        return Task.CompletedTask;
    }

    /// <summary>
    /// Print Preview lists the answers given at print time ("Số lô: L1"), so a
    /// lot that only a QR code carries can be checked, and they can be entered
    /// again without leaving the preview: the labels are prepared again with
    /// the new answers. A reprint keeps the original job's answers.
    /// </summary>
    public static Task PreviewShowsAndEditsTheAnswers()
    {
        RunOnSta(() =>
        {
            var template = new LabelTemplate { Name = "Lô", WidthMm = 60, HeightMm = 30, Dpi = 203 };
            template.PrinterProfile.Dpi = 203;
            template.PromptVariables.Add(new PromptVariableDefinition { Name = "Lot", Prompt = "Số lô" });
            template.Objects.Add(new LabelObject { Type = ObjectType.QRCode, Name = "QR", BindingExpression = "{?Lot}", XMm = 3, YMm = 3, WidthMm = 20, HeightMm = 20 });
            var window = new ANLAbel.App.PrintPreviewWindow(
                template,
                null,
                null,
                new PrintService(),
                new ANLAbel.Data.PrintLogs.PrintLogService(Path.Combine(Path.GetTempPath(), $"anlabel-answers-{Guid.NewGuid():N}.csv")),
                string.Empty,
                promptValues: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Lot"] = "L1" });
            void Settle()
            {
                var deadline = DateTime.UtcNow.AddSeconds(30);
                do
                {
                    System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);
                    Thread.Sleep(20);
                }
                while ((window.IsPreviewBusy || window.Pages.Count == 0) && DateTime.UtcNow < deadline);
            }

            Settle();
            Require(window.CanEditPromptValues && window.JobValuesSummaryText == "Giá trị nhập khi in — Số lô: L1",
                "The preview lists the answer: " + window.JobValuesSummaryText);

            typeof(ANLAbel.App.PrintPreviewWindow).GetMethod("ApplyPromptValues", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(window, new object[] { new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Lot"] = "L2" } });
            Settle();
            Require(window.JobValuesSummaryText == "Giá trị nhập khi in — Số lô: L2" && window.PromptValues["Lot"] == "L2" && window.Pages.Count == 1,
                "New answers prepare the labels again: " + window.JobValuesSummaryText);
            window.Close();

            var reprint = new ANLAbel.App.PrintPreviewWindow(
                template,
                null,
                null,
                new PrintService(),
                new ANLAbel.Data.PrintLogs.PrintLogService(Path.Combine(Path.GetTempPath(), $"anlabel-answers-{Guid.NewGuid():N}.csv")),
                string.Empty,
                approvedReprintJobId: "job-1");
            Require(!reprint.CanEditPromptValues && reprint.HasPromptValues, "A reprint keeps the original job's answers.");
            reprint.Close();
        });

        return Task.CompletedTask;
    }

    /// <summary>
    /// Renaming a function in the Data Manager keeps the objects and the
    /// functions that read it: a text {MaLo} and a function Tag reading
    /// FIELD("MaLo") follow the new name MaHang, so the printed row still
    /// carries it; Ctrl+Z restores both.
    /// </summary>
    public static Task FunctionRenameCarriesItsObjects()
    {
        RunOnSta(() =>
        {
            var viewModel = new ANLAbel.App.ViewModels.MainViewModel();
            viewModel.Template.PromptVariables.Add(new PromptVariableDefinition { Name = "Lot", Prompt = "Số lô" });
            viewModel.Template.DataTransforms.Add(new ANLAbel.Core.Data.DataTransformDefinition("MaLo", "CONCAT(\"L-\", FIELD(\"?Lot\"))"));
            viewModel.Template.DataTransforms.Add(new ANLAbel.Core.Data.DataTransformDefinition("Tag", "CONCAT(\"[\", FIELD(\"MaLo\"), \"]\")"));
            viewModel.Template.Objects.Add(new LabelObject { Type = ObjectType.Text, Name = "Mã lô", BindingExpression = "{MaLo}", XMm = 3, YMm = 3, WidthMm = 30, HeightMm = 6 });
            viewModel.Template.Objects.Add(new LabelObject { Type = ObjectType.Text, Name = "Nhãn", BindingExpression = "{Tag}", XMm = 3, YMm = 12, WidthMm = 30, HeightMm = 6 });
            var window = new ANLAbel.App.DataManagerWindow(viewModel, "Functions");
            var functions = (System.Collections.IList)typeof(ANLAbel.App.DataManagerWindow).GetField("_functions", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(window)!;
            var draft = functions[0]!;
            draft.GetType().GetProperty("Name")!.SetValue(draft, "MaHang");
            typeof(ANLAbel.App.DataManagerWindow).GetMethod("Ok_Click", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(window, new object?[] { null, null });

            string[] Bindings() => viewModel.Template.Objects.Select(item => item.BindingExpression).ToArray();
            Require(Bindings().SequenceEqual(["{MaHang}", "{Tag}"]) && viewModel.Template.DataTransforms[1].Formula == "CONCAT(\"[\", FIELD(\"MaHang\"), \"]\")",
                "The object and the function reading it use the new name: " + string.Join(" | ", Bindings()) + " / " + viewModel.Template.DataTransforms[1].Formula);
            Require(ANLAbel.Core.Printing.JobVariableContract.TryApply(viewModel.Template, [null], DateTimeOffset.Now, new Dictionary<string, string> { ["Lot"] = "7" }, out var printed, out var error)
                    && printed[0]!["MaHang"] == "L-7" && printed[0]!["Tag"] == "[L-7]",
                "The printed row carries the renamed function and the one built on it: " + error);

            viewModel.UndoCommand.Execute(null);
            Require(Bindings().SequenceEqual(["{MaLo}", "{Tag}"]) && viewModel.Template.DataTransforms[0].Name == "MaLo",
                "Ctrl+Z restores the function name and its objects: " + string.Join(" | ", Bindings()));
        });

        return Task.CompletedTask;
    }

    /// <summary>
    /// A Data Manager function reads a print question, as the editor says it
    /// can and as NiceLabel functions read prompted variables: MaLo =
    /// CONCAT("L-", FIELD("?Lot")) on a text {MaLo}. The editor shows its
    /// result with the question's placeholder instead of an error, the print
    /// dialog asks "Lot" (it asked nothing), the printed row carries "L-123"
    /// (it printed nothing) and the designer reports no missing column "?Lot".
    /// </summary>
    public static Task FunctionsReadPrintQuestions()
    {
        RunOnSta(() =>
        {
            var viewModel = new ANLAbel.App.ViewModels.MainViewModel();
            viewModel.Template.PromptVariables.Add(new PromptVariableDefinition { Name = "Lot", Prompt = "Số lô", Required = true });
            viewModel.Template.DataTransforms.Add(new ANLAbel.Core.Data.DataTransformDefinition("MaLo", "CONCAT(\"L-\", FIELD(\"?Lot\"))"));
            viewModel.Template.Objects.Add(new LabelObject { Type = ObjectType.Text, Name = "Mã lô", BindingExpression = "{MaLo}", XMm = 3, YMm = 3, WidthMm = 30, HeightMm = 6 });

            var window = new ANLAbel.App.DataManagerWindow(viewModel, "Functions");
            var functions = (System.Collections.IList)typeof(ANLAbel.App.DataManagerWindow).GetField("_functions", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(window)!;
            typeof(ANLAbel.App.DataManagerWindow).GetMethod("RebuildTree", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(window, new[] { functions[0]! });
            System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);
            var result = ((TextBlock)window.FindName("FunctionResultText")).Text;
            Require(result == "L-‹Lot›", "The function editor shows the result with the question's placeholder: " + result);

            var asked = ANLAbel.Core.Printing.JobVariableContract.RequiredPrompts(viewModel.Template);
            Require(asked.Select(prompt => prompt.Name).SequenceEqual(["Lot"]), "The print dialog asks the question the function reads: " + string.Join(", ", asked.Select(prompt => prompt.Name)));
            Require(viewModel.TryBuildPrintPreviewRows(out var rows, out var rowError), "The print rows build: " + rowError);
            Require(ANLAbel.Core.Printing.JobVariableContract.TryApply(viewModel.Template, rows, DateTimeOffset.Now, new Dictionary<string, string> { ["Lot"] = "123" }, out var printed, out var error)
                    && printed[0]!["MaLo"] == "L-123",
                "The printed row carries the function's value: " + error);
            Require(!viewModel.HasBindingIssues, "No missing column is reported for the question: " + string.Join(" | ", viewModel.BindingIssues.Select(issue => issue.StatusText)));
            // Not closed: closing an edited draft asks to keep or discard it.
        });

        return Task.CompletedTask;
    }

    /// <summary>A presentation source for key events raised on windows that are never shown.</summary>
    private sealed class TestPresentationSource : PresentationSource
    {
        public override Visual? RootVisual { get; set; }

        public override bool IsDisposed => false;

        protected override CompositionTarget? GetCompositionTargetCore() => null;
    }

    private static IEnumerable<T> FindDescendants<T>(DependencyObject root) where T : DependencyObject
    {
        foreach (var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
        {
            if (child is T match)
            {
                yield return match;
            }

            foreach (var descendant in FindDescendants<T>(child))
            {
                yield return descendant;
            }
        }
    }

    /// <summary>
    /// An expiry computed from the Excel production date: the NSX column holds
    /// real Excel date cells; the GS1-128 carries DATEADD(NSX, 24 months) as
    /// yyMMdd in AI 17 and the text shows it as dd/MM/yyyy. Every row decodes
    /// with its own expiry and preflight blocks no row.
    /// </summary>
    public static async Task ExpiryComesFromTheExcelProductionDate()
    {
        var directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "anlabel-expiry-" + Guid.NewGuid().ToString("N")));
        try
        {
            var excelPath = Path.Combine(directory.FullName, "lots.xlsx");
            var produced = new[] { new DateTime(2026, 10, 31), new DateTime(2026, 2, 28), new DateTime(2025, 12, 1) };
            using (var workbook = new ClosedXML.Excel.XLWorkbook())
            {
                var sheet = workbook.AddWorksheet("Lots");
                sheet.Cell(1, 1).Value = "GTIN";
                sheet.Cell(1, 2).Value = "Lot";
                sheet.Cell(1, 3).Value = "NSX";
                for (var index = 0; index < produced.Length; index++)
                {
                    sheet.Cell(index + 2, 1).Value = "09501101530003";
                    sheet.Cell(index + 2, 2).Value = "L" + (index + 1);
                    sheet.Cell(index + 2, 3).Value = produced[index];
                }

                workbook.SaveAs(excelPath);
            }

            var viewModel = new ANLAbel.App.ViewModels.MainViewModel();
            await viewModel.ImportExcelAsync(excelPath, "Lots");
            var template = viewModel.Template;
            template.WidthMm = 100;
            template.HeightMm = 50;
            var code = new LabelObject
            {
                Type = ObjectType.BarcodeCode128,
                BarcodeSymbology = BarcodeSymbology.Code128,
                BarcodeApplicationProfile = BarcodeApplicationProfile.Gs1,
                Name = "Carton",
                BindingExpression = "CONCAT(\"(01)\", FIELD(\"GTIN\"), \"(17)\", DATEADD(FIELD(\"NSX\"), \"24\", \"mo\", \"yyMMdd\"), \"(10)\", FIELD(\"Lot\"))",
                BarcodeModuleWidthMm = 0.33,
                BarcodeWidthMode = BarcodeWidthMode.SizedFromX,
                QrQuietZoneModules = 10,
                BarcodeHriPlacement = BarcodeHriPlacement.None,
                XMm = 5,
                YMm = 5,
                WidthMm = 90,
                HeightMm = 22,
                ZIndex = 1
            };
            var expiry = new LabelObject
            {
                Type = ObjectType.Text,
                Name = "Expiry",
                BindingExpression = "DATEADD(FIELD(\"NSX\"), \"24\", \"mo\", \"dd/MM/yyyy\")",
                XMm = 5,
                YMm = 35,
                WidthMm = 40,
                HeightMm = 8,
                Style = { FontSizePt = 12, BorderThicknessMm = 0, TextSizing = TextSizingMode.AutoFit },
                ZIndex = 2
            };
            template.Objects.Add(code);
            template.Objects.Add(expiry);
            var rows = viewModel.ExcelDataView!.Cast<System.Data.DataRowView>()
                .Select(view => (IReadOnlyDictionary<string, string>)view.Row.Table.Columns.Cast<System.Data.DataColumn>()
                    .ToDictionary(column => column.ColumnName, column => view.Row[column]?.ToString() ?? string.Empty, StringComparer.OrdinalIgnoreCase))
                .ToArray();
            Require(rows.Length == produced.Length, $"The Excel sheet links {rows.Length} rows.");

            RunOnSta(() =>
            {
                var failures = new List<string>();
                var renderer = new ZxingBarcodeRenderer();
                var plan = new PrintRenderPlan { Dpi = Dpi, DpiX = Dpi, DpiY = Dpi, LabelWidthMm = template.WidthMm, LabelHeightMm = template.HeightMm };
                for (var index = 0; index < rows.Length; index++)
                {
                    var row = rows[index];
                    var due = produced[index].AddMonths(24);
                    var data = "(01)09501101530003(17)" + due.ToString("yyMMdd", System.Globalization.CultureInfo.InvariantCulture) + "(10)" + row["Lot"];
                    var raster = Rasterize(new LabelVisualRenderer().Render(template, row, plan), template.WidthMm, template.HeightMm);
                    var widthMm = LinearBarcodeProductionWidth.ResolveSymbolWidthMm(code, renderer, Dpi, data);
                    var decoded = Decode(raster, code.XMm, code.YMm, widthMm, code.HeightMm, BarcodeFormat.CODE_128).Replace("", string.Empty, StringComparison.Ordinal);
                    if (decoded != data.Replace("(", string.Empty, StringComparison.Ordinal).Replace(")", string.Empty, StringComparison.Ordinal))
                    {
                        failures.Add($"NSX {row["NSX"]}: the GS1-128 reads '{decoded}', expected expiry {due:yyyy-MM-dd}.");
                    }

                    var text = ANLAbel.Core.Expressions.Formulas.FormulaBindingEvaluator.Evaluate(expiry.BindingExpression, row).Value;
                    if (text != due.ToString("dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture))
                    {
                        failures.Add($"NSX {row["NSX"]}: the expiry text is '{text}'.");
                    }
                }

                var blocking = new PrintPreflightValidator(renderer).Validate(template, rows, Dpi).Issues.Where(issue => issue.IsBlocking).ToArray();
                failures.AddRange(blocking.Select(issue => $"Preflight blocks {issue.ObjectName}: {issue.Message}"));
                if (failures.Count > 0)
                {
                    throw new InvalidOperationException(string.Join(Environment.NewLine, failures));
                }
            });
        }
        finally
        {
            try
            {
                directory.Delete(recursive: true);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    /// <summary>Dark pixels (gray below 128) inside a rectangle (mm) of the page.</summary>
    private static int Ink(Raster raster, double xMm, double yMm, double widthMm, double heightMm)
    {
        int Px(double mm) => (int)Math.Round(mm / 25.4 * Dpi);
        var count = 0;
        for (var y = Math.Max(0, Px(yMm)); y < Math.Min(raster.Height, Px(yMm + heightMm)); y++)
        {
            for (var x = Math.Max(0, Px(xMm)); x < Math.Min(raster.Width, Px(xMm + widthMm)); x++)
            {
                if (raster.Gray[y * raster.Width + x] < 128)
                {
                    count++;
                }
            }
        }

        return count;
    }

    /// <summary>The Properties header ("name | type | W x H mm") follows renames and resizes of the selection.</summary>
    public static Task PropertiesHeaderFollowsTheSelectedObject()
    {
        RunOnSta(() =>
        {
            var viewModel = new ANLAbel.App.ViewModels.MainViewModel();
            var item = new LabelObject { Type = ObjectType.Rectangle, Name = "Khung", XMm = 2, YMm = 2, WidthMm = 20, HeightMm = 10 };
            viewModel.Template.Objects.Add(item);
            viewModel.SelectedObject = item;
            var raised = 0;
            viewModel.PropertyChanged += (_, e) => raised += e.PropertyName == nameof(viewModel.SelectedObjectSummaryText) ? 1 : 0;
            item.WidthMm = 35;
            item.HeightMm = 12.5;
            item.Name = "Khung ngoài";
            Require(raised >= 3, $"The header must refresh on resize and rename; it refreshed {raised} time(s).");
            Require(viewModel.SelectedObjectSummaryText.StartsWith("Khung ngoài |", StringComparison.Ordinal)
                    && viewModel.SelectedObjectSummaryText.Replace(',', '.').EndsWith("35 x 12.5 mm", StringComparison.Ordinal),
                $"Header reads '{viewModel.SelectedObjectSummaryText}'.");
        });

        return Task.CompletedTask;
    }

    private sealed record DataMatrixCase(string Name, DataMatrixSymbolSize Size, string Data, double X, double Y, double W, double H, double Aspect);

    /// <summary>
    /// NiceLabel 10 Data Matrix format. Automatic size prints the smallest
    /// square symbol (eight letters used to come out 8 x 32 in a square frame);
    /// a chosen square or rectangular size prints with that shape and decodes;
    /// data too long for the chosen size is refused, in Vietnamese, rather than
    /// grown; choosing a rectangular size gives the designer frame its shape.
    /// </summary>
    public static Task DataMatrixSizesPrintAsChosen()
    {
        RunOnSta(() =>
        {
            var cases = new[]
            {
                new DataMatrixCase("Auto eight letters", DataMatrixSymbolSize.Auto, "ABCDEFGH", 5, 5, 20, 20, 1),
                new DataMatrixCase("Fixed 52 x 52", DataMatrixSymbolSize.C52x52, "LOT-2026-10-30", 35, 5, 30, 30, 1),
                new DataMatrixCase("Rectangle 8 x 18", DataMatrixSymbolSize.C8x18, "12345", 5, 45, 36, 16, 18.0 / 8),
                new DataMatrixCase("Rectangle 16 x 48", DataMatrixSymbolSize.C16x48, "SN:A1B2C3D4E5F6", 45, 45, 60, 20, 48.0 / 16)
            };
            // Data comes through a field, as printed from Excel; each code has its own column.
            var row = cases.Select((code, index) => (Key: "C" + index, code.Data)).ToDictionary(pair => pair.Key, pair => pair.Data);
            var template = new LabelTemplate { Name = "Data Matrix sizes", WidthMm = 110, HeightMm = 75, Dpi = Dpi };
            foreach (var code in cases)
            {
                template.Objects.Add(new LabelObject
                {
                    Type = ObjectType.BarcodeCode128,
                    BarcodeSymbology = BarcodeSymbology.DataMatrix,
                    Name = code.Name,
                    BindingExpression = "{C" + Array.IndexOf(cases, code) + "}",
                    XMm = code.X,
                    YMm = code.Y,
                    WidthMm = code.W,
                    HeightMm = code.H,
                    QrQuietZoneModules = 0,
                    DataMatrixSymbolSize = code.Size,
                    Style = { BorderThicknessMm = 0, OutlineStyle = OutlineStyle.None }
                });
            }

            var failures = new List<string>();
            var plan = new PrintRenderPlan { Dpi = Dpi, DpiX = Dpi, DpiY = Dpi, LabelWidthMm = template.WidthMm, LabelHeightMm = template.HeightMm };
            var printed = Rasterize(new LabelVisualRenderer().Render(template, row, plan), template.WidthMm, template.HeightMm);
            foreach (var code in cases)
            {
                var frame = new PictureCase(code.Name, PictureSizing.Proportional, false, false, code.X, code.Y, code.W, code.H);
                var symbol = Box(printed, frame, 64);
                var aspect = symbol.H > 0 ? symbol.W / symbol.H : 0;
                var decoded = Decode(printed, code.X, code.Y, code.W, code.H, BarcodeFormat.DATA_MATRIX);
                if (decoded != code.Data || Math.Abs(aspect - code.Aspect) > 0.12 * code.Aspect)
                {
                    failures.Add($"{code.Name}: decoded '{decoded}', symbol {Show(symbol)} (width/height {aspect:0.00}, expected {code.Aspect:0.00}).");
                }
            }

            // A chosen size too small for the row's data blocks that row with a plain reason.
            var narrow = template.Objects.Single(item => item.Name == "Rectangle 8 x 18");
            var tooLong = new Dictionary<string, string>(row) { ["C2"] = "SERIAL-0123456789-ABCDEFGHIJ" };
            var rows = new IReadOnlyDictionary<string, string>?[] { row, tooLong };
            var preflight = new PrintPreflightValidator(new ZxingBarcodeRenderer()).Validate(template, rows, Dpi);
            var refused = preflight.Issues.Where(issue => issue.IsBlocking && issue.ObjectName == narrow.Name).ToArray();
            var reason = refused.Length == 1 ? ANLAbel.App.Services.PreflightIssueText.Sentence(refused[0].Message) : string.Empty;
            if (refused.Length != 1 || refused[0].RowNumber != 2 || !reason.Contains("không vừa khổ Data Matrix 8 × 18", StringComparison.Ordinal))
            {
                failures.Add("Data too long for 8 x 18 must block only row 2 with a Vietnamese reason; got "
                    + (refused.Length == 0 ? "nothing" : string.Join(" | ", refused.Select(issue => $"row {issue.RowNumber}: {issue.Message} -> {ANLAbel.App.Services.PreflightIssueText.Sentence(issue.Message)}"))));
            }

            // Choosing a rectangular size gives the designer frame the symbol's shape (width kept).
            var designer = new LabelTemplate { Name = "Designer", WidthMm = 60, HeightMm = 40, Dpi = Dpi };
            var square = new LabelObject { Type = ObjectType.BarcodeCode128, BarcodeSymbology = BarcodeSymbology.DataMatrix, Name = "Dm", BindingExpression = "{Dm}", QrQuietZoneModules = 1, XMm = 5, YMm = 5, WidthMm = 26, HeightMm = 26 };
            designer.Objects.Add(square);
            var canvas = new LabelDesignerCanvas { Template = designer, Zoom = 1 };
            canvas.Measure(new Size(400, 300));
            canvas.Arrange(new Rect(0, 0, 400, 300));
            canvas.UpdateLayout();
            square.DataMatrixSymbolSize = DataMatrixSymbolSize.C12x26;
            var expectedHeight = 26.0 * (12 + 2) / (26 + 2);
            if (Math.Abs(square.WidthMm - 26) > 0.01 || Math.Abs(square.HeightMm - expectedHeight) > 0.02)
            {
                failures.Add($"Choosing 12 x 26 should make the 26 mm frame {expectedHeight:0.00} mm high; it is {square.WidthMm:0.##} x {square.HeightMm:0.##} mm.");
            }

            square.DataMatrixSymbolSize = DataMatrixSymbolSize.Auto;
            if (Math.Abs(square.HeightMm - square.WidthMm) > 0.02)
            {
                failures.Add($"Back to Auto the frame should be square again; it is {square.WidthMm:0.##} x {square.HeightMm:0.##} mm.");
            }

            if (failures.Count > 0)
            {
                throw new InvalidOperationException(string.Join(Environment.NewLine, failures));
            }
        });

        return Task.CompletedTask;
    }

    private sealed record PictureCase(string Name, PictureSizing Sizing, bool FlipHorizontal, bool FlipVertical, double X, double Y, double W, double H);

    /// <summary>
    /// NiceLabel 10 picture Resize options and flips. A 400 x 200 px picture
    /// stating 508 dpi (20 x 10 mm) is grey with a black top-left quarter.
    /// Where the picture and its black quarter land is worked out here from
    /// the options' definitions, then measured on the printed label and on
    /// the designer canvas: proportional (centred, whole), stretched to the
    /// frame, kept at its own size from the frame's corner (and cut at a
    /// smaller frame), each also flipped.
    /// </summary>
    public static Task PicturesSizeAndFlipInDesignerAsInPrint()
    {
        RunOnSta(() =>
        {
            const int pixelWidth = 400;
            const int pixelHeight = 200;
            const double pictureDpi = 508;
            var payload = PicturePayload(pixelWidth, pixelHeight, pictureDpi);
            var cases = new[]
            {
                new PictureCase("Proportional", PictureSizing.Proportional, false, false, 5, 5, 30, 30),
                new PictureCase("Stretch", PictureSizing.Stretch, false, false, 40, 5, 25, 30),
                new PictureCase("Original", PictureSizing.Original, false, false, 70, 5, 25, 25),
                new PictureCase("Proportional flipped across", PictureSizing.Proportional, true, false, 5, 40, 30, 20),
                new PictureCase("Stretch flipped down", PictureSizing.Stretch, false, true, 40, 40, 24, 24),
                new PictureCase("Original flipped across in a smaller frame", PictureSizing.Original, true, false, 70, 40, 15, 8)
            };
            var template = new LabelTemplate { Name = "Pictures", WidthMm = 100, HeightMm = 70, Dpi = Dpi };
            foreach (var picture in cases)
            {
                template.Objects.Add(new LabelObject
                {
                    Type = ObjectType.Image,
                    Name = picture.Name,
                    XMm = picture.X,
                    YMm = picture.Y,
                    WidthMm = picture.W,
                    HeightMm = picture.H,
                    ImageDataBase64 = payload,
                    ImagePixelWidth = pixelWidth,
                    ImagePixelHeight = pixelHeight,
                    PictureSizing = picture.Sizing,
                    PictureFlipHorizontal = picture.FlipHorizontal,
                    PictureFlipVertical = picture.FlipVertical,
                    Style = { BorderThicknessMm = 0, OutlineStyle = OutlineStyle.None }
                });
            }

            var plan = new PrintRenderPlan { Dpi = Dpi, DpiX = Dpi, DpiY = Dpi, LabelWidthMm = template.WidthMm, LabelHeightMm = template.HeightMm };
            var printed = Rasterize(new LabelVisualRenderer().Render(template, null, plan), template.WidthMm, template.HeightMm);
            var canvas = new LabelDesignerCanvas { Template = template, Zoom = 1 };
            var size = new Size(MmConverter.MmToDip(template.WidthMm), MmConverter.MmToDip(template.HeightMm));
            canvas.Measure(size);
            canvas.Arrange(new Rect(size));
            canvas.UpdateLayout();
            var designed = Rasterize(canvas, template.WidthMm, template.HeightMm);

            var failures = new List<string>();
            foreach (var picture in cases)
            {
                var (drawn, black) = Expected(picture, pixelWidth, pixelHeight, pictureDpi);
                foreach (var (what, raster) in new[] { ("print", printed), ("designer", designed) })
                {
                    var measuredDrawn = Box(raster, picture, 180);
                    var measuredBlack = Box(raster, picture, 64);
                    if (!Near(measuredDrawn, drawn) || !Near(measuredBlack, black))
                    {
                        failures.Add($"{picture.Name} ({what}): picture {Show(measuredDrawn)}, black quarter {Show(measuredBlack)}; expected {Show(drawn)} and {Show(black)}.");
                    }
                }
            }

            if (failures.Count > 0)
            {
                throw new InvalidOperationException(string.Join(Environment.NewLine, failures));
            }
        });

        return Task.CompletedTask;
    }

    /// <summary>The drawn picture and its black quarter (mm) inside the frame, from the option definitions.</summary>
    private static ((double X, double Y, double W, double H) Drawn, (double X, double Y, double W, double H) Black) Expected(
        PictureCase picture,
        int pixelWidth,
        int pixelHeight,
        double pictureDpi)
    {
        double x = picture.X, y = picture.Y, w, h;
        switch (picture.Sizing)
        {
            case PictureSizing.Stretch:
                w = picture.W;
                h = picture.H;
                break;
            case PictureSizing.Original:
                w = pixelWidth / pictureDpi * 25.4;
                h = pixelHeight / pictureDpi * 25.4;
                break;
            default:
                var mmPerPixel = Math.Min(picture.W / pixelWidth, picture.H / pixelHeight);
                w = pixelWidth * mmPerPixel;
                h = pixelHeight * mmPerPixel;
                x += (picture.W - w) / 2;
                y += (picture.H - h) / 2;
                break;
        }

        var blackX = picture.FlipHorizontal ? x + w / 2 : x;
        var blackY = picture.FlipVertical ? y + h / 2 : y;
        return (Cut((x, y, w, h), picture), Cut((blackX, blackY, w / 2, h / 2), picture));
    }

    /// <summary>The part of a rectangle inside the picture's frame.</summary>
    private static (double X, double Y, double W, double H) Cut((double X, double Y, double W, double H) box, PictureCase frame)
    {
        var left = Math.Max(box.X, frame.X);
        var top = Math.Max(box.Y, frame.Y);
        var right = Math.Min(box.X + box.W, frame.X + frame.W);
        var bottom = Math.Min(box.Y + box.H, frame.Y + frame.H);
        return (left, top, Math.Max(0, right - left), Math.Max(0, bottom - top));
    }

    /// <summary>Bounds (mm) of the pixels darker than <paramref name="threshold"/> within 1 mm of the frame.</summary>
    private static (double X, double Y, double W, double H) Box(Raster raster, PictureCase frame, int threshold)
    {
        int Px(double mm) => (int)Math.Round(mm / 25.4 * Dpi);
        var x0 = Math.Max(0, Px(frame.X - 1));
        var y0 = Math.Max(0, Px(frame.Y - 1));
        var x1 = Math.Min(raster.Width, Px(frame.X + frame.W + 1));
        var y1 = Math.Min(raster.Height, Px(frame.Y + frame.H + 1));
        int minX = int.MaxValue, minY = int.MaxValue, maxX = -1, maxY = -1;
        for (var y = y0; y < y1; y++)
        {
            for (var x = x0; x < x1; x++)
            {
                if (raster.Gray[y * raster.Width + x] < threshold)
                {
                    minX = Math.Min(minX, x);
                    minY = Math.Min(minY, y);
                    maxX = Math.Max(maxX, x);
                    maxY = Math.Max(maxY, y);
                }
            }
        }

        static double Mm(int pixels) => pixels / (double)Dpi * 25.4;
        return maxX < 0 ? (0, 0, 0, 0) : (Mm(minX), Mm(minY), Mm(maxX - minX + 1), Mm(maxY - minY + 1));
    }

    private static bool Near((double X, double Y, double W, double H) measured, (double X, double Y, double W, double H) expected)
    {
        const double tolerance = 0.25;
        return Math.Abs(measured.X - expected.X) <= tolerance
            && Math.Abs(measured.Y - expected.Y) <= tolerance
            && Math.Abs(measured.W - expected.W) <= tolerance
            && Math.Abs(measured.H - expected.H) <= tolerance;
    }

    private static string Show((double X, double Y, double W, double H) box)
        => $"{box.W:0.00} x {box.H:0.00} mm at ({box.X:0.00}, {box.Y:0.00})";

    /// <summary>A grey PNG with a black top-left quarter that states its resolution.</summary>
    private static string PicturePayload(int width, int height, double dpi)
    {
        var stride = width * 4;
        var pixels = new byte[stride * height];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var value = (byte)(x < width / 2 && y < height / 2 ? 0 : 128);
                var index = y * stride + x * 4;
                pixels[index] = value;
                pixels[index + 1] = value;
                pixels[index + 2] = value;
                pixels[index + 3] = 255;
            }
        }

        var bitmap = BitmapSource.Create(width, height, dpi, dpi, PixelFormats.Bgra32, null, pixels, stride);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = new System.IO.MemoryStream();
        encoder.Save(stream);
        return Convert.ToBase64String(stream.ToArray());
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
        => DecodeWithSymbology(raster, xMm, yMm, widthMm, heightMm, format).Text;

    /// <summary>The decoded text and its symbology identifier (]d2 is a GS1 DataMatrix, ]Q3 a GS1 QR code).</summary>
    private static (string Text, string Symbology) DecodeWithSymbology(Raster raster, double xMm, double yMm, double widthMm, double heightMm, BarcodeFormat format)
    {
        int Px(double mm) => (int)Math.Round(mm / 25.4 * Dpi);
        var x0 = Math.Max(0, Px(xMm - 1.5));
        var y0 = Math.Max(0, Px(yMm - 1.5));
        var x1 = Math.Min(raster.Width, Px(xMm + widthMm + 1.5));
        var y1 = Math.Min(raster.Height, Px(yMm + heightMm + 1.5));
        if (x1 <= x0 || y1 <= y0)
        {
            return ("(outside the page)", string.Empty);
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
            var result = new MultiFormatReader().decode(new BinaryBitmap(new HybridBinarizer(new RGBLuminanceSource(crop, w, h, RGBLuminanceSource.BitmapFormat.Gray8))), hints);
            var symbology = result?.ResultMetadata is { } metadata && metadata.TryGetValue(ResultMetadataType.SYMBOLOGY_IDENTIFIER, out var identifier)
                ? identifier?.ToString() ?? string.Empty
                : string.Empty;
            var text = result?.Text ?? string.Empty;
            // ZXing returns the FNC1 that marks a GS1 DataMatrix as a leading GS;
            // a scanner does not transmit it.
            if (symbology == "]d2" && text.StartsWith('\u001D'))
            {
                text = text[1..];
            }

            return (text, symbology);
        }
        catch (ReaderException)
        {
            return (string.Empty, string.Empty);
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
