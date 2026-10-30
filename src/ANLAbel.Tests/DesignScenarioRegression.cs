using System.Reflection;
using System.Windows;
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
