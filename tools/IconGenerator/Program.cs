using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace IconGenerator;

public static class Program
{
    private static string FindIconsDir()
    {
        var dir = AppDomain.CurrentDomain.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            var target = Path.Combine(dir, "src", "ANLAbel.App", "Icons");
            if (Directory.Exists(target)) return target;
            var parent = Directory.GetParent(dir);
            if (parent is null) break;
            dir = parent.FullName;
        }
        return Path.GetFullPath(@"H:\00_REPOS_PROJECTS\ANLABEL\src\ANLAbel.App\Icons");
    }

    private static readonly string IconsDir = FindIconsDir();

    [STAThread]
    public static void Main()
    {
        Directory.CreateDirectory(IconsDir);
        Console.WriteLine($"Generating modern, crisp, vibrant icons to: {IconsDir}");

        Generate("new", DrawNew);
        Generate("open", DrawOpen);
        Generate("save", DrawSave);
        Generate("folder", DrawFolder);
        Generate("revisions", DrawRevisions);
        Generate("undo", DrawUndo);
        Generate("redo", DrawRedo);
        Generate("delete_selection", DrawDeleteSelection);
        Generate("cursor_select", DrawCursorSelect);
        Generate("zoom_plus", DrawZoomPlus);
        Generate("zoom_minus", DrawZoomMinus);
        Generate("snap_grid", DrawSnapGrid);
        Generate("snap_objects", DrawSnapObjects);
        Generate("panels", DrawPanels);
        Generate("table", DrawTable);
        Generate("static_text", DrawStaticText);
        Generate("text_box", DrawTextBox);
        Generate("barcode", DrawBarcode);
        Generate("qr_code", DrawQrCode);
        Generate("data_matrix", DrawDataMatrix);
        Generate("line", DrawLine);
        Generate("rectangle", DrawRectangle);
        Generate("ellipse", DrawEllipse);
        Generate("image", DrawImage);
        Generate("database", DrawDatabase);
        Generate("import_excel", DrawImportExcel);
        Generate("export_excel", DrawExportExcel);
        Generate("update_excel", DrawUpdateExcel);
        Generate("print_current", DrawPrintCurrent);
        Generate("print_all_rows", DrawPrintAllRows);
        Generate("preview", DrawPreview);
        Generate("printer_setup", DrawPrinterSetup);
        Generate("printer_status", DrawPrinterStatus);
        Generate("print_history", DrawPrintHistory);
        Generate("test_print", DrawTestPrint);
        Generate("settings", DrawSettings);
        Generate("help", DrawHelp);
        Generate("app_update", DrawAppUpdate);
        Generate("collapse_chevron", DrawCollapseChevron);
        Generate("expand_chevron", DrawExpandChevron);

        Console.WriteLine("All 40 icons generated successfully!");
    }

    private static void Generate(string name, Action<DrawingContext, double> drawAction)
    {
        const int size = 48;
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            // Transparent background
            drawAction(dc, size);
        }

        var rtb = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(visual);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(rtb));

        var outPath = Path.Combine(IconsDir, $"{name}.png");
        using var stream = File.Create(outPath);
        encoder.Save(stream);
        Console.WriteLine($" -> {name}.png");
    }

    private static SolidColorBrush Brush(string hex) => (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;

    // 1. new: Bright Sapphire Blue document with amber plus badge
    private static void DrawNew(DrawingContext dc, double s)
    {
        // Document
        var page = new PathGeometry();
        var fig = new PathFigure { StartPoint = new Point(10, 6), IsClosed = true, IsFilled = true };
        fig.Segments.Add(new LineSegment(new Point(26, 6), true));
        fig.Segments.Add(new LineSegment(new Point(34, 14), true));
        fig.Segments.Add(new LineSegment(new Point(34, 38), true));
        fig.Segments.Add(new LineSegment(new Point(10, 38), true));
        page.Figures.Add(fig);

        dc.DrawGeometry(Brushes.White, new Pen(Brush("#2563EB"), 2.2), page);

        // Fold corner
        var fold = new PathGeometry();
        var foldFig = new PathFigure { StartPoint = new Point(26, 6), IsClosed = true, IsFilled = true };
        foldFig.Segments.Add(new LineSegment(new Point(34, 14), true));
        foldFig.Segments.Add(new LineSegment(new Point(26, 14), true));
        fold.Figures.Add(foldFig);
        dc.DrawGeometry(Brush("#DBEAFE"), new Pen(Brush("#2563EB"), 1.8), fold);

        // Content lines
        var linePen = new Pen(Brush("#93C5FD"), 2.0);
        dc.DrawLine(linePen, new Point(15, 18), new Point(24, 18));
        dc.DrawLine(linePen, new Point(15, 24), new Point(29, 24));
        dc.DrawLine(linePen, new Point(15, 30), new Point(22, 30));

        // Plus badge (Amber)
        dc.DrawEllipse(Brush("#F59E0B"), new Pen(Brushes.White, 1.8), new Point(34, 34), 10, 10);
        var plusPen = new Pen(Brushes.White, 2.4) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        dc.DrawLine(plusPen, new Point(34, 28), new Point(34, 40));
        dc.DrawLine(plusPen, new Point(28, 34), new Point(40, 34));
    }

    // 2. open: Warm Golden Amber folder with blue upward document
    private static void DrawOpen(DrawingContext dc, double s)
    {
        // Back folder tab
        var tab = new PathGeometry();
        var tabFig = new PathFigure { StartPoint = new Point(6, 14), IsClosed = true, IsFilled = true };
        tabFig.Segments.Add(new LineSegment(new Point(18, 14), true));
        tabFig.Segments.Add(new LineSegment(new Point(22, 18), true));
        tabFig.Segments.Add(new LineSegment(new Point(42, 18), true));
        tabFig.Segments.Add(new LineSegment(new Point(42, 40), true));
        tabFig.Segments.Add(new LineSegment(new Point(6, 40), true));
        tab.Figures.Add(tabFig);
        dc.DrawGeometry(Brush("#D97706"), null, tab);

        // Emerging white/blue sheet
        dc.DrawRoundedRectangle(Brushes.White, new Pen(Brush("#3B82F6"), 1.6), new Rect(14, 10, 20, 20), 2, 2);
        var bluePen = new Pen(Brush("#93C5FD"), 1.8);
        dc.DrawLine(bluePen, new Point(18, 16), new Point(30, 16));
        dc.DrawLine(bluePen, new Point(18, 21), new Point(26, 21));

        // Front open folder flap
        var front = new PathGeometry();
        var frontFig = new PathFigure { StartPoint = new Point(4, 22), IsClosed = true, IsFilled = true };
        frontFig.Segments.Add(new LineSegment(new Point(44, 22), true));
        frontFig.Segments.Add(new LineSegment(new Point(40, 42), true));
        frontFig.Segments.Add(new LineSegment(new Point(8, 42), true));
        front.Figures.Add(frontFig);
        dc.DrawGeometry(Brush("#F59E0B"), new Pen(Brush("#B45309"), 1.8), front);

        // Upward arrow on folder
        var arrowPen = new Pen(Brush("#2563EB"), 2.2) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        dc.DrawLine(arrowPen, new Point(24, 36), new Point(24, 27));
        dc.DrawLine(arrowPen, new Point(20, 31), new Point(24, 27));
        dc.DrawLine(arrowPen, new Point(28, 31), new Point(24, 27));
    }

    // 3. save: Royal Indigo Floppy Disk with silver shutter
    private static void DrawSave(DrawingContext dc, double s)
    {
        // Body
        dc.DrawRoundedRectangle(Brush("#4338CA"), new Pen(Brush("#312E81"), 1.8), new Rect(8, 8, 32, 32), 4, 4);

        // Metal shutter at top
        dc.DrawRoundedRectangle(Brush("#E2E8F0"), new Pen(Brush("#94A3B8"), 1.2), new Rect(14, 8, 20, 14), 2, 2);
        // Shutter black notch
        dc.DrawRectangle(Brush("#1E293B"), null, new Rect(18, 11, 4, 8));

        // Label sticker at bottom
        dc.DrawRoundedRectangle(Brushes.White, new Pen(Brush("#CBD5E1"), 1.2), new Rect(13, 24, 22, 14), 2, 2);
        var labelPen = new Pen(Brush("#38BDF8"), 1.6);
        dc.DrawLine(labelPen, new Point(16, 29), new Point(32, 29));
        dc.DrawLine(labelPen, new Point(16, 33), new Point(28, 33));
    }

    // 4. folder: Classic Manila Folder
    private static void DrawFolder(DrawingContext dc, double s)
    {
        var tab = new PathGeometry();
        var tabFig = new PathFigure { StartPoint = new Point(6, 12), IsClosed = true, IsFilled = true };
        tabFig.Segments.Add(new LineSegment(new Point(18, 12), true));
        tabFig.Segments.Add(new LineSegment(new Point(23, 17), true));
        tabFig.Segments.Add(new LineSegment(new Point(42, 17), true));
        tabFig.Segments.Add(new LineSegment(new Point(42, 40), true));
        tabFig.Segments.Add(new LineSegment(new Point(6, 40), true));
        tab.Figures.Add(tabFig);
        dc.DrawGeometry(Brush("#D97706"), new Pen(Brush("#B45309"), 1.5), tab);

        // Front folder body
        dc.DrawRoundedRectangle(Brush("#F59E0B"), new Pen(Brush("#D97706"), 1.8), new Rect(6, 18, 36, 22), 3, 3);
        // Gloss highlight
        dc.DrawRoundedRectangle(Brush("#FBBF24"), null, new Rect(8, 20, 32, 4), 2, 2);
    }

    // 5. revisions: History Clock with emerald counter-clockwise arrow
    private static void DrawRevisions(DrawingContext dc, double s)
    {
        // Clock face
        dc.DrawEllipse(Brush("#F8FAFC"), new Pen(Brush("#0284C7"), 2.4), new Point(24, 24), 16, 16);
        // Center dot
        dc.DrawEllipse(Brush("#0F172A"), null, new Point(24, 24), 2.5, 2.5);
        // Clock hands (9:00)
        var handPen = new Pen(Brush("#0F172A"), 2.2) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        dc.DrawLine(handPen, new Point(24, 24), new Point(24, 14));
        dc.DrawLine(handPen, new Point(24, 24), new Point(14, 24));

        // Emerald counter-clockwise history arrow around top-right
        var arcPen = new Pen(Brush("#10B981"), 2.5) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        var arc = new PathGeometry();
        var arcFig = new PathFigure { StartPoint = new Point(36, 14), IsFilled = false };
        arcFig.Segments.Add(new ArcSegment(new Point(20, 6), new Size(18, 18), 0, false, SweepDirection.Counterclockwise, true));
        arc.Figures.Add(arcFig);
        dc.DrawGeometry(null, arcPen, arc);

        // Arrow head
        var head = new PathGeometry();
        var headFig = new PathFigure { StartPoint = new Point(16, 6), IsClosed = true, IsFilled = true };
        headFig.Segments.Add(new LineSegment(new Point(22, 2), true));
        headFig.Segments.Add(new LineSegment(new Point(22, 10), true));
        head.Figures.Add(headFig);
        dc.DrawGeometry(Brush("#10B981"), null, head);
    }

    // 6. undo: Vibrant Coral Red curved left return arrow
    private static void DrawUndo(DrawingContext dc, double s)
    {
        var pen = new Pen(Brush("#EF4444"), 4.0) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        var path = new PathGeometry();
        var fig = new PathFigure { StartPoint = new Point(14, 22), IsFilled = false };
        fig.Segments.Add(new ArcSegment(new Point(38, 30), new Size(18, 18), 0, true, SweepDirection.Clockwise, true));
        path.Figures.Add(fig);
        dc.DrawGeometry(null, pen, path);

        // Arrowhead
        var head = new PathGeometry();
        var hFig = new PathFigure { StartPoint = new Point(8, 22), IsClosed = true, IsFilled = true };
        hFig.Segments.Add(new LineSegment(new Point(18, 12), true));
        hFig.Segments.Add(new LineSegment(new Point(18, 32), true));
        head.Figures.Add(hFig);
        dc.DrawGeometry(Brush("#EF4444"), null, head);
    }

    // 7. redo: Vibrant Emerald Green curved right forward arrow
    private static void DrawRedo(DrawingContext dc, double s)
    {
        var pen = new Pen(Brush("#10B981"), 4.0) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        var path = new PathGeometry();
        var fig = new PathFigure { StartPoint = new Point(34, 22), IsFilled = false };
        fig.Segments.Add(new ArcSegment(new Point(10, 30), new Size(18, 18), 0, true, SweepDirection.Counterclockwise, true));
        path.Figures.Add(fig);
        dc.DrawGeometry(null, pen, path);

        // Arrowhead
        var head = new PathGeometry();
        var hFig = new PathFigure { StartPoint = new Point(40, 22), IsClosed = true, IsFilled = true };
        hFig.Segments.Add(new LineSegment(new Point(30, 12), true));
        hFig.Segments.Add(new LineSegment(new Point(30, 32), true));
        head.Figures.Add(hFig);
        dc.DrawGeometry(Brush("#10B981"), null, head);
    }

    // 8. delete_selection: Crimson Red trash can with lid
    private static void DrawDeleteSelection(DrawingContext dc, double s)
    {
        // Can body
        var body = new PathGeometry();
        var bFig = new PathFigure { StartPoint = new Point(14, 16), IsClosed = true, IsFilled = true };
        bFig.Segments.Add(new LineSegment(new Point(34, 16), true));
        bFig.Segments.Add(new LineSegment(new Point(31, 40), true));
        bFig.Segments.Add(new LineSegment(new Point(17, 40), true));
        body.Figures.Add(bFig);
        dc.DrawGeometry(Brush("#DC2626"), new Pen(Brush("#991B1B"), 1.8), body);

        // Ribs
        var ribPen = new Pen(Brush("#F87171"), 1.8);
        dc.DrawLine(ribPen, new Point(20, 21), new Point(21, 35));
        dc.DrawLine(ribPen, new Point(24, 21), new Point(24, 35));
        dc.DrawLine(ribPen, new Point(28, 21), new Point(27, 35));

        // Lid
        dc.DrawRoundedRectangle(Brush("#B91C1C"), new Pen(Brush("#7F1D1D"), 1.6), new Rect(11, 12, 26, 5), 2, 2);
        // Handle
        dc.DrawRoundedRectangle(Brush("#DC2626"), new Pen(Brush("#7F1D1D"), 1.4), new Rect(21, 8, 6, 4), 1, 1);
    }

    // 9. cursor_select: Dark Slate pointer with cyan selection frame
    private static void DrawCursorSelect(DrawingContext dc, double s)
    {
        // Selection bounding box (Cyan dashed)
        var boxPen = new Pen(Brush("#06B6D4"), 1.6) { DashStyle = DashStyles.Dash };
        dc.DrawRectangle(null, boxPen, new Rect(14, 14, 26, 26));

        // Corner handles
        var hBrush = Brush("#0891B2");
        dc.DrawRectangle(hBrush, null, new Rect(12, 12, 5, 5));
        dc.DrawRectangle(hBrush, null, new Rect(37, 12, 5, 5));
        dc.DrawRectangle(hBrush, null, new Rect(12, 37, 5, 5));
        dc.DrawRectangle(hBrush, null, new Rect(37, 37, 5, 5));

        // Pointer arrow
        var pointer = new PathGeometry();
        var pFig = new PathFigure { StartPoint = new Point(8, 6), IsClosed = true, IsFilled = true };
        pFig.Segments.Add(new LineSegment(new Point(8, 26), true));
        pFig.Segments.Add(new LineSegment(new Point(14, 21), true));
        pFig.Segments.Add(new LineSegment(new Point(19, 28), true));
        pFig.Segments.Add(new LineSegment(new Point(23, 25), true));
        pFig.Segments.Add(new LineSegment(new Point(17, 18), true));
        pFig.Segments.Add(new LineSegment(new Point(24, 18), true));
        pointer.Figures.Add(pFig);
        dc.DrawGeometry(Brush("#0F172A"), new Pen(Brushes.White, 1.8), pointer);
    }

    // 10. zoom_plus: Oceanic magnifying glass with green plus
    private static void DrawZoomPlus(DrawingContext dc, double s)
    {
        // Glass rim
        dc.DrawEllipse(Brush("#F0F9FF"), new Pen(Brush("#0284C7"), 3.2), new Point(21, 21), 13, 13);

        // Handle
        var handlePen = new Pen(Brush("#334155"), 4.4) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        dc.DrawLine(handlePen, new Point(31, 31), new Point(41, 41));

        // Green Plus
        var plusPen = new Pen(Brush("#10B981"), 2.8) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        dc.DrawLine(plusPen, new Point(21, 14), new Point(21, 28));
        dc.DrawLine(plusPen, new Point(14, 21), new Point(28, 21));
    }

    // 11. zoom_minus: Oceanic magnifying glass with coral minus
    private static void DrawZoomMinus(DrawingContext dc, double s)
    {
        // Glass rim
        dc.DrawEllipse(Brush("#F0F9FF"), new Pen(Brush("#0284C7"), 3.2), new Point(21, 21), 13, 13);

        // Handle
        var handlePen = new Pen(Brush("#334155"), 4.4) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        dc.DrawLine(handlePen, new Point(31, 31), new Point(41, 41));

        // Coral Minus
        var minusPen = new Pen(Brush("#EF4444"), 2.8) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        dc.DrawLine(minusPen, new Point(14, 21), new Point(28, 21));
    }

    // 12. snap_grid: Indigo grid with cyan magnetic nodes
    private static void DrawSnapGrid(DrawingContext dc, double s)
    {
        dc.DrawRoundedRectangle(Brush("#EEF2FF"), new Pen(Brush("#6366F1"), 1.8), new Rect(8, 8, 32, 32), 4, 4);

        // Grid lines
        var gridPen = new Pen(Brush("#A5B4FC"), 1.4);
        dc.DrawLine(gridPen, new Point(18, 8), new Point(18, 40));
        dc.DrawLine(gridPen, new Point(29, 8), new Point(29, 40));
        dc.DrawLine(gridPen, new Point(8, 18), new Point(40, 18));
        dc.DrawLine(gridPen, new Point(8, 29), new Point(40, 29));

        // Magnetic snap target points (Cyan glowing dots)
        var dotBrush = Brush("#06B6D4");
        var dotBorder = new Pen(Brush("#0891B2"), 1.0);
        dc.DrawEllipse(dotBrush, dotBorder, new Point(18, 18), 3, 3);
        dc.DrawEllipse(dotBrush, dotBorder, new Point(29, 18), 3, 3);
        dc.DrawEllipse(dotBrush, dotBorder, new Point(18, 29), 3, 3);
        dc.DrawEllipse(dotBrush, dotBorder, new Point(29, 29), 3, 3);
    }

    // 13. snap_objects: Purple & Blue boxes with amber alignment guide
    private static void DrawSnapObjects(DrawingContext dc, double s)
    {
        // Box 1 (Purple)
        dc.DrawRoundedRectangle(Brush("#8B5CF6"), new Pen(Brush("#6D28D9"), 1.6), new Rect(6, 8, 18, 18), 3, 3);

        // Box 2 (Blue)
        dc.DrawRoundedRectangle(Brush("#3B82F6"), new Pen(Brush("#1D4ED8"), 1.6), new Rect(24, 22, 18, 18), 3, 3);

        // Alignment guideline (Amber dashed)
        var guidePen = new Pen(Brush("#F59E0B"), 2.0) { DashStyle = DashStyles.Dash };
        dc.DrawLine(guidePen, new Point(24, 6), new Point(24, 42));

        // Snap indicator markers
        dc.DrawEllipse(Brush("#F59E0B"), null, new Point(24, 8), 2.5, 2.5);
        dc.DrawEllipse(Brush("#F59E0B"), null, new Point(24, 22), 2.5, 2.5);
    }

    // 14. panels: Multi-layered panel deck with active royal blue layer
    private static void DrawPanels(DrawingContext dc, double s)
    {
        // Bottom layer (Slate 300)
        dc.DrawRoundedRectangle(Brush("#94A3B8"), new Pen(Brush("#64748B"), 1.2), new Rect(6, 26, 36, 14), 3, 3);

        // Middle layer (Slate 400)
        dc.DrawRoundedRectangle(Brush("#64748B"), new Pen(Brush("#475569"), 1.4), new Rect(6, 17, 36, 14), 3, 3);

        // Top active layer (Vibrant Royal Blue)
        dc.DrawRoundedRectangle(Brush("#2563EB"), new Pen(Brush("#1D4ED8"), 1.8), new Rect(6, 8, 36, 14), 3, 3);
        // Highlight notch
        dc.DrawRoundedRectangle(Brush("#60A5FA"), null, new Rect(10, 11, 14, 3), 1, 1);
    }

    // 15. table: Teal spreadsheet grid
    private static void DrawTable(DrawingContext dc, double s)
    {
        dc.DrawRoundedRectangle(Brushes.White, new Pen(Brush("#0D9488"), 2.0), new Rect(6, 8, 36, 32), 3, 3);

        // Teal Header Row
        var head = new PathGeometry();
        var hFig = new PathFigure { StartPoint = new Point(6, 11), IsClosed = true, IsFilled = true };
        hFig.Segments.Add(new ArcSegment(new Point(9, 8), new Size(3, 3), 0, false, SweepDirection.Clockwise, true));
        hFig.Segments.Add(new LineSegment(new Point(39, 8), true));
        hFig.Segments.Add(new ArcSegment(new Point(42, 11), new Size(3, 3), 0, false, SweepDirection.Clockwise, true));
        hFig.Segments.Add(new LineSegment(new Point(42, 18), true));
        hFig.Segments.Add(new LineSegment(new Point(6, 18), true));
        head.Figures.Add(hFig);
        dc.DrawGeometry(Brush("#0D9488"), null, head);

        // Grid lines
        var gridPen = new Pen(Brush("#CCFBF1"), 1.2);
        dc.DrawLine(gridPen, new Point(18, 8), new Point(18, 40));
        dc.DrawLine(gridPen, new Point(30, 8), new Point(30, 40));
        dc.DrawLine(gridPen, new Point(6, 25), new Point(42, 25));
        dc.DrawLine(gridPen, new Point(6, 32), new Point(42, 32));
    }

    // 16. static_text: Bold Royal Purple Serif "T"
    private static void DrawStaticText(DrawingContext dc, double s)
    {
        var text = new FormattedText(
            "T",
            System.Globalization.CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Georgia"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
            34,
            Brush("#7C3AED"),
            96);

        // Subtle shadow
        var shadowText = new FormattedText(
            "T",
            System.Globalization.CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Georgia"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
            34,
            Brush("#DDD6FE"),
            96);
        dc.DrawText(shadowText, new Point(13, 6));
        dc.DrawText(text, new Point(12, 5));

        // Clean underline baseline
        var basePen = new Pen(Brush("#A855F7"), 2.5) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        dc.DrawLine(basePen, new Point(8, 41), new Point(40, 41));
    }

    // 17. text_box: Blue frame + handles + "T" & text lines
    private static void DrawTextBox(DrawingContext dc, double s)
    {
        // Bounding frame
        var framePen = new Pen(Brush("#3B82F6"), 1.8) { DashStyle = DashStyles.Dash };
        dc.DrawRectangle(Brush("#F0F9FF"), framePen, new Rect(6, 6, 36, 36));

        // Corner handles
        var hBrush = Brush("#2563EB");
        dc.DrawRectangle(hBrush, null, new Rect(4, 4, 5, 5));
        dc.DrawRectangle(hBrush, null, new Rect(39, 4, 5, 5));
        dc.DrawRectangle(hBrush, null, new Rect(4, 39, 5, 5));
        dc.DrawRectangle(hBrush, null, new Rect(39, 39, 5, 5));

        // Inner "T"
        var t = new FormattedText(
            "T",
            System.Globalization.CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
            18,
            Brush("#1D4ED8"),
            96);
        dc.DrawText(t, new Point(10, 10));

        // Text lines
        var linePen = new Pen(Brush("#60A5FA"), 2.0) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        dc.DrawLine(linePen, new Point(24, 17), new Point(37, 17));
        dc.DrawLine(linePen, new Point(12, 28), new Point(37, 28));
        dc.DrawLine(linePen, new Point(12, 34), new Point(28, 34));
    }

    // 18. barcode: Precision 1D Bars with glowing red laser line
    private static void DrawBarcode(DrawingContext dc, double s)
    {
        dc.DrawRoundedRectangle(Brushes.White, new Pen(Brush("#E2E8F0"), 1.2), new Rect(4, 6, 40, 36), 3, 3);

        // Bars
        var barBrush = Brush("#18181B");
        dc.DrawRectangle(barBrush, null, new Rect(8, 10, 2.5, 20));
        dc.DrawRectangle(barBrush, null, new Rect(12, 10, 1.5, 20));
        dc.DrawRectangle(barBrush, null, new Rect(15, 10, 4.0, 20));
        dc.DrawRectangle(barBrush, null, new Rect(21, 10, 1.5, 20));
        dc.DrawRectangle(barBrush, null, new Rect(24, 10, 3.0, 20));
        dc.DrawRectangle(barBrush, null, new Rect(29, 10, 1.5, 20));
        dc.DrawRectangle(barBrush, null, new Rect(32, 10, 4.0, 20));
        dc.DrawRectangle(barBrush, null, new Rect(38, 10, 2.0, 20));

        // Red Laser Scan line
        var laserPen = new Pen(Brush("#EF4444"), 2.2);
        dc.DrawLine(laserPen, new Point(5, 20), new Point(43, 20));

        // Numbers below
        var numPen = new Pen(Brush("#71717A"), 1.5);
        dc.DrawLine(numPen, new Point(10, 35), new Point(18, 35));
        dc.DrawLine(numPen, new Point(22, 35), new Point(30, 35));
        dc.DrawLine(numPen, new Point(34, 35), new Point(38, 35));
    }

    // 19. qr_code: Dark navy finder patterns + emerald green matrix
    private static void DrawQrCode(DrawingContext dc, double s)
    {
        dc.DrawRoundedRectangle(Brushes.White, new Pen(Brush("#E2E8F0"), 1.2), new Rect(6, 6, 36, 36), 4, 4);

        void DrawFinder(double x, double y)
        {
            dc.DrawRectangle(Brush("#0F172A"), null, new Rect(x, y, 10, 10));
            dc.DrawRectangle(Brushes.White, null, new Rect(x + 2, y + 2, 6, 6));
            dc.DrawRectangle(Brush("#0F172A"), null, new Rect(x + 3.5, y + 3.5, 3, 3));
        }

        DrawFinder(9, 9);
        DrawFinder(29, 9);
        DrawFinder(9, 29);

        // Emerald matrix modules
        var em = Brush("#10B981");
        dc.DrawRectangle(em, null, new Rect(22, 10, 3, 3));
        dc.DrawRectangle(em, null, new Rect(25, 13, 3, 3));
        dc.DrawRectangle(em, null, new Rect(21, 17, 3, 3));
        dc.DrawRectangle(em, null, new Rect(25, 21, 3, 3));
        dc.DrawRectangle(em, null, new Rect(29, 23, 3, 3));
        dc.DrawRectangle(em, null, new Rect(33, 27, 3, 3));
        dc.DrawRectangle(em, null, new Rect(23, 29, 3, 3));
        dc.DrawRectangle(em, null, new Rect(29, 33, 3, 3));
        dc.DrawRectangle(em, null, new Rect(33, 33, 3, 3));
    }

    // 20. data_matrix: Solid L-finder + cyan data cells
    private static void DrawDataMatrix(DrawingContext dc, double s)
    {
        dc.DrawRoundedRectangle(Brushes.White, new Pen(Brush("#E2E8F0"), 1.2), new Rect(6, 6, 36, 36), 4, 4);

        var dark = Brush("#0F172A");
        // Solid Left & Bottom "L" borders
        dc.DrawRectangle(dark, null, new Rect(9, 9, 3.5, 30));
        dc.DrawRectangle(dark, null, new Rect(9, 35.5, 30, 3.5));

        // Alternating Top & Right timing borders
        for (int i = 0; i < 5; i++)
        {
            dc.DrawRectangle(dark, null, new Rect(15 + i * 5, 9, 3, 3.5));
            dc.DrawRectangle(dark, null, new Rect(35.5, 15 + i * 5, 3.5, 3));
        }

        // Cyan matrix data cells
        var cyan = Brush("#06B6D4");
        dc.DrawRectangle(cyan, null, new Rect(16, 16, 4, 4));
        dc.DrawRectangle(cyan, null, new Rect(24, 16, 4, 4));
        dc.DrawRectangle(cyan, null, new Rect(20, 22, 4, 4));
        dc.DrawRectangle(cyan, null, new Rect(28, 22, 4, 4));
        dc.DrawRectangle(cyan, null, new Rect(16, 28, 4, 4));
        dc.DrawRectangle(cyan, null, new Rect(26, 28, 4, 4));
    }

    // 21. line: Electric Blue diagonal stroke with cyan anchor nodes
    private static void DrawLine(DrawingContext dc, double s)
    {
        var linePen = new Pen(Brush("#2563EB"), 3.8) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        dc.DrawLine(linePen, new Point(10, 38), new Point(38, 10));

        // Start & End Anchor nodes (Cyan)
        var nBrush = Brush("#06B6D4");
        var nPen = new Pen(Brushes.White, 2.0);
        dc.DrawEllipse(nBrush, nPen, new Point(10, 38), 5, 5);
        dc.DrawEllipse(nBrush, nPen, new Point(38, 10), 5, 5);
    }

    // 22. rectangle: Warm Amber rounded rectangle
    private static void DrawRectangle(DrawingContext dc, double s)
    {
        dc.DrawRoundedRectangle(Brush("#FEF3C7"), new Pen(Brush("#D97706"), 2.6), new Rect(7, 10, 34, 28), 5, 5);
        // Highlight band
        dc.DrawRoundedRectangle(Brush("#FBBF24"), null, new Rect(11, 14, 26, 6), 2, 2);
    }

    // 23. ellipse: Vibrant Rose / Magenta circle
    private static void DrawEllipse(DrawingContext dc, double s)
    {
        dc.DrawEllipse(Brush("#FFE4E6"), new Pen(Brush("#E11D48"), 2.8), new Point(24, 24), 16, 16);
        // Highlight crescent/dot
        dc.DrawEllipse(Brush("#FB7185"), null, new Point(19, 19), 4, 4);
    }

    // 24. image: Emerald mountain card with golden sun
    private static void DrawImage(DrawingContext dc, double s)
    {
        // Frame
        dc.DrawRoundedRectangle(Brush("#F0FDF4"), new Pen(Brush("#16A34A"), 2.0), new Rect(6, 8, 36, 32), 4, 4);

        // Golden Sun
        dc.DrawEllipse(Brush("#F59E0B"), null, new Point(16, 18), 4.5, 4.5);

        // Mountains (Emerald Green)
        var mtn = new PathGeometry();
        var mFig = new PathFigure { StartPoint = new Point(6, 38), IsClosed = true, IsFilled = true };
        mFig.Segments.Add(new LineSegment(new Point(18, 24), true));
        mFig.Segments.Add(new LineSegment(new Point(27, 33), true));
        mFig.Segments.Add(new LineSegment(new Point(33, 27), true));
        mFig.Segments.Add(new LineSegment(new Point(42, 38), true));
        mtn.Figures.Add(mFig);
        dc.DrawGeometry(Brush("#059669"), null, mtn);
    }

    // 25. database: 3D Cylindrical Azure SQL Database stack
    private static void DrawDatabase(DrawingContext dc, double s)
    {
        var borderPen = new Pen(Brush("#0369A1"), 1.8);
        var cylBrush = Brush("#38BDF8");

        void DrawCylinder(double y)
        {
            var body = new PathGeometry();
            var bFig = new PathFigure { StartPoint = new Point(10, y + 5), IsClosed = true, IsFilled = true };
            bFig.Segments.Add(new LineSegment(new Point(10, y + 12), true));
            bFig.Segments.Add(new ArcSegment(new Point(38, y + 12), new Size(14, 5), 0, false, SweepDirection.Clockwise, true));
            bFig.Segments.Add(new LineSegment(new Point(38, y + 5), true));
            bFig.Segments.Add(new ArcSegment(new Point(10, y + 5), new Size(14, 5), 0, false, SweepDirection.Counterclockwise, true));
            body.Figures.Add(bFig);
            dc.DrawGeometry(cylBrush, borderPen, body);

            // Top lid
            dc.DrawEllipse(Brush("#BAE6FD"), borderPen, new Point(24, y + 5), 14, 5);
        }

        DrawCylinder(22);
        DrawCylinder(13);
        DrawCylinder(4);

        // Emerald connection status LED
        dc.DrawEllipse(Brush("#10B981"), new Pen(Brushes.White, 1.2), new Point(36, 36), 4, 4);
    }

    // 26. import_excel: Emerald Excel badge + Royal Blue Down arrow
    private static void DrawImportExcel(DrawingContext dc, double s)
    {
        // Excel Sheet (Emerald)
        dc.DrawRoundedRectangle(Brush("#16A34A"), new Pen(Brush("#15803D"), 1.8), new Rect(6, 6, 26, 32), 3, 3);
        var x = new FormattedText(
            "X",
            System.Globalization.CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
            18,
            Brushes.White,
            96);
        dc.DrawText(x, new Point(13, 11));

        // Downward Blue Import Arrow
        var arr = new PathGeometry();
        var aFig = new PathFigure { StartPoint = new Point(34, 18), IsClosed = true, IsFilled = true };
        aFig.Segments.Add(new LineSegment(new Point(34, 28), true));
        aFig.Segments.Add(new LineSegment(new Point(28, 28), true));
        aFig.Segments.Add(new LineSegment(new Point(37, 40), true));
        aFig.Segments.Add(new LineSegment(new Point(46, 28), true));
        aFig.Segments.Add(new LineSegment(new Point(40, 28), true));
        aFig.Segments.Add(new LineSegment(new Point(40, 18), true));
        arr.Figures.Add(aFig);
        dc.DrawGeometry(Brush("#2563EB"), new Pen(Brushes.White, 1.8), arr);
    }

    // 27. export_excel: Emerald Excel badge + Golden Amber Up arrow
    private static void DrawExportExcel(DrawingContext dc, double s)
    {
        // Excel Sheet (Emerald)
        dc.DrawRoundedRectangle(Brush("#16A34A"), new Pen(Brush("#15803D"), 1.8), new Rect(6, 10, 26, 32), 3, 3);
        var x = new FormattedText(
            "X",
            System.Globalization.CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
            18,
            Brushes.White,
            96);
        dc.DrawText(x, new Point(13, 15));

        // Upward Amber Export Arrow
        var arr = new PathGeometry();
        var aFig = new PathFigure { StartPoint = new Point(37, 6), IsClosed = true, IsFilled = true };
        aFig.Segments.Add(new LineSegment(new Point(28, 18), true));
        aFig.Segments.Add(new LineSegment(new Point(34, 18), true));
        aFig.Segments.Add(new LineSegment(new Point(34, 28), true));
        aFig.Segments.Add(new LineSegment(new Point(40, 28), true));
        aFig.Segments.Add(new LineSegment(new Point(40, 18), true));
        aFig.Segments.Add(new LineSegment(new Point(46, 18), true));
        arr.Figures.Add(aFig);
        dc.DrawGeometry(Brush("#F59E0B"), new Pen(Brushes.White, 1.8), arr);
    }

    // 28. update_excel: Emerald Excel badge + Cyan circular sync arrows
    private static void DrawUpdateExcel(DrawingContext dc, double s)
    {
        // Excel Sheet (Emerald)
        dc.DrawRoundedRectangle(Brush("#16A34A"), new Pen(Brush("#15803D"), 1.8), new Rect(8, 8, 22, 26), 3, 3);
        var x = new FormattedText(
            "X",
            System.Globalization.CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
            14,
            Brushes.White,
            96);
        dc.DrawText(x, new Point(13, 12));

        // Sync arrows (Cyan)
        var syncPen = new Pen(Brush("#0284C7"), 3.0) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        var arc = new PathGeometry();
        var aFig = new PathFigure { StartPoint = new Point(34, 18), IsFilled = false };
        aFig.Segments.Add(new ArcSegment(new Point(28, 40), new Size(11, 11), 0, true, SweepDirection.Clockwise, true));
        arc.Figures.Add(aFig);
        dc.DrawGeometry(null, syncPen, arc);

        // Arrow tip
        var head = new PathGeometry();
        var hFig = new PathFigure { StartPoint = new Point(34, 12), IsClosed = true, IsFilled = true };
        hFig.Segments.Add(new LineSegment(new Point(40, 18), true));
        hFig.Segments.Add(new LineSegment(new Point(30, 22), true));
        head.Figures.Add(hFig);
        dc.DrawGeometry(Brush("#0284C7"), null, head);
    }

    // 29. print_current: Industrial Thermal Printer with blue label feed and green LED
    private static void DrawPrintCurrent(DrawingContext dc, double s)
    {
        // Printer body (Obsidian slate)
        dc.DrawRoundedRectangle(Brush("#1E293B"), new Pen(Brush("#0F172A"), 2.0), new Rect(6, 14, 36, 26), 4, 4);

        // Top cover lid
        dc.DrawRoundedRectangle(Brush("#334155"), null, new Rect(9, 16, 30, 8), 2, 2);

        // Label feed slot
        dc.DrawRectangle(Brush("#0F172A"), null, new Rect(12, 26, 24, 3));

        // Printed label sheet emerging
        dc.DrawRoundedRectangle(Brushes.White, new Pen(Brush("#3B82F6"), 1.5), new Rect(14, 28, 20, 16), 2, 2);
        // Barcode on printed label
        var bPen = new Pen(Brush("#1E293B"), 1.4);
        dc.DrawLine(bPen, new Point(17, 32), new Point(17, 38));
        dc.DrawLine(bPen, new Point(20, 32), new Point(20, 38));
        dc.DrawLine(bPen, new Point(22, 32), new Point(22, 38));
        dc.DrawLine(bPen, new Point(25, 32), new Point(25, 38));
        dc.DrawLine(bPen, new Point(28, 32), new Point(28, 38));
        dc.DrawLine(bPen, new Point(31, 32), new Point(31, 38));

        // Green power LED
        dc.DrawEllipse(Brush("#10B981"), null, new Point(11, 20), 2, 2);
    }

    // 30. print_all_rows: Thermal Printer with cascading multi-label batch feed
    private static void DrawPrintAllRows(DrawingContext dc, double s)
    {
        // Printer body
        dc.DrawRoundedRectangle(Brush("#1E293B"), new Pen(Brush("#0F172A"), 2.0), new Rect(6, 10, 36, 22), 4, 4);
        dc.DrawRoundedRectangle(Brush("#334155"), null, new Rect(9, 12, 30, 6), 2, 2);
        dc.DrawRectangle(Brush("#0F172A"), null, new Rect(12, 20, 24, 3));

        // Label 1 (behind)
        dc.DrawRoundedRectangle(Brush("#DBEAFE"), new Pen(Brush("#3B82F6"), 1.2), new Rect(18, 22, 18, 16), 2, 2);

        // Label 2 (middle)
        dc.DrawRoundedRectangle(Brush("#BFDBFE"), new Pen(Brush("#2563EB"), 1.4), new Rect(14, 26, 18, 16), 2, 2);

        // Label 3 (front)
        dc.DrawRoundedRectangle(Brushes.White, new Pen(Brush("#1D4ED8"), 1.6), new Rect(10, 30, 18, 16), 2, 2);
        var bPen = new Pen(Brush("#1E293B"), 1.2);
        dc.DrawLine(bPen, new Point(13, 34), new Point(13, 40));
        dc.DrawLine(bPen, new Point(16, 34), new Point(16, 40));
        dc.DrawLine(bPen, new Point(19, 34), new Point(19, 40));
        dc.DrawLine(bPen, new Point(23, 34), new Point(23, 40));

        // Batch yellow count badge (3+)
        dc.DrawEllipse(Brush("#F59E0B"), new Pen(Brushes.White, 1.2), new Point(36, 36), 7, 7);
        var txt = new FormattedText("∞", System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal), 11, Brushes.White, 96);
        dc.DrawText(txt, new Point(32, 28));
    }

    // 31. preview: Document sheet under magnifying glass
    private static void DrawPreview(DrawingContext dc, double s)
    {
        // Document
        dc.DrawRoundedRectangle(Brush("#F0F9FF"), new Pen(Brush("#0284C7"), 2.0), new Rect(8, 6, 26, 34), 3, 3);
        var pPen = new Pen(Brush("#BAE6FD"), 1.8);
        dc.DrawLine(pPen, new Point(12, 12), new Point(24, 12));
        dc.DrawLine(pPen, new Point(12, 18), new Point(28, 18));
        dc.DrawLine(pPen, new Point(12, 24), new Point(26, 24));
        dc.DrawLine(pPen, new Point(12, 30), new Point(20, 30));

        // Magnifying glass over sheet
        dc.DrawEllipse(Brush("#E0F2FE"), new Pen(Brush("#0EA5E9"), 2.8), new Point(29, 25), 11, 11);
        var handlePen = new Pen(Brush("#334155"), 3.8) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        dc.DrawLine(handlePen, new Point(37, 33), new Point(44, 40));
    }

    // 32. printer_setup: Industrial printer with golden precision gear
    private static void DrawPrinterSetup(DrawingContext dc, double s)
    {
        // Printer
        dc.DrawRoundedRectangle(Brush("#334155"), new Pen(Brush("#1E293B"), 1.8), new Rect(6, 12, 30, 24), 3, 3);
        dc.DrawRectangle(Brush("#0F172A"), null, new Rect(10, 22, 20, 3));
        dc.DrawRoundedRectangle(Brushes.White, new Pen(Brush("#94A3B8"), 1.2), new Rect(12, 23, 16, 10), 1, 1);

        // Golden Gear (Setup)
        dc.DrawEllipse(Brush("#F59E0B"), new Pen(Brush("#D97706"), 2.2), new Point(34, 30), 9, 9);
        dc.DrawEllipse(Brush("#334155"), new Pen(Brush("#D97706"), 1.2), new Point(34, 30), 3.5, 3.5);
    }

    // 33. printer_status: Industrial printer with emerald check badge
    private static void DrawPrinterStatus(DrawingContext dc, double s)
    {
        // Printer
        dc.DrawRoundedRectangle(Brush("#334155"), new Pen(Brush("#1E293B"), 1.8), new Rect(6, 10, 30, 24), 3, 3);
        dc.DrawRectangle(Brush("#0F172A"), null, new Rect(10, 20, 20, 3));
        dc.DrawRoundedRectangle(Brushes.White, new Pen(Brush("#94A3B8"), 1.2), new Rect(12, 21, 16, 10), 1, 1);

        // Emerald Check Badge
        dc.DrawEllipse(Brush("#10B981"), new Pen(Brushes.White, 2.0), new Point(34, 30), 9, 9);
        var chkPen = new Pen(Brushes.White, 2.2) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        dc.DrawLine(chkPen, new Point(30, 30), new Point(33, 33));
        dc.DrawLine(chkPen, new Point(33, 33), new Point(38, 27));
    }

    // 34. print_history: Navy journal binder with golden clock
    private static void DrawPrintHistory(DrawingContext dc, double s)
    {
        // Log Book
        dc.DrawRoundedRectangle(Brush("#1E3A8A"), new Pen(Brush("#172554"), 1.8), new Rect(6, 6, 26, 36), 3, 3);
        // Spine
        dc.DrawRoundedRectangle(Brush("#3B82F6"), null, new Rect(6, 6, 6, 36), 2, 2);
        // Lines
        var lPen = new Pen(Brush("#93C5FD"), 1.5);
        dc.DrawLine(lPen, new Point(16, 14), new Point(27, 14));
        dc.DrawLine(lPen, new Point(16, 20), new Point(25, 20));

        // Golden History Clock
        dc.DrawEllipse(Brush("#FEF3C7"), new Pen(Brush("#F59E0B"), 2.2), new Point(33, 28), 10, 10);
        var hPen = new Pen(Brush("#B45309"), 1.8) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        dc.DrawLine(hPen, new Point(33, 28), new Point(33, 22));
        dc.DrawLine(hPen, new Point(33, 28), new Point(38, 28));
    }

    // 35. test_print: Alignment crosshairs target calibration sheet
    private static void DrawTestPrint(DrawingContext dc, double s)
    {
        dc.DrawRoundedRectangle(Brushes.White, new Pen(Brush("#6366F1"), 2.0), new Rect(6, 6, 36, 36), 4, 4);

        // Concentric target circles
        dc.DrawEllipse(null, new Pen(Brush("#A5B4FC"), 1.4), new Point(24, 24), 13, 13);
        dc.DrawEllipse(null, new Pen(Brush("#6366F1"), 1.8), new Point(24, 24), 8, 8);
        dc.DrawEllipse(Brush("#EC4899"), null, new Point(24, 24), 3, 3);

        // Crosshairs
        var crossPen = new Pen(Brush("#4F46E5"), 1.8);
        dc.DrawLine(crossPen, new Point(24, 7), new Point(24, 41));
        dc.DrawLine(crossPen, new Point(7, 24), new Point(41, 24));
    }

    // 36. settings: Interlocking Slate & Cyan engineering gears
    private static void DrawSettings(DrawingContext dc, double s)
    {
        // Gear 1 (Slate, larger)
        dc.DrawEllipse(Brush("#475569"), new Pen(Brush("#334155"), 2.0), new Point(20, 20), 12, 12);
        dc.DrawEllipse(Brushes.White, null, new Point(20, 20), 4.5, 4.5);

        // Gear 2 (Cyan, smaller)
        dc.DrawEllipse(Brush("#06B6D4"), new Pen(Brush("#0891B2"), 1.8), new Point(32, 32), 8, 8);
        dc.DrawEllipse(Brushes.White, null, new Point(32, 32), 3, 3);
    }

    // 37. help: Oceanic Blue support badge with bold "?"
    private static void DrawHelp(DrawingContext dc, double s)
    {
        dc.DrawEllipse(Brush("#0284C7"), new Pen(Brush("#0369A1"), 2.2), new Point(24, 24), 17, 17);

        var q = new FormattedText(
            "?",
            System.Globalization.CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
            24,
            Brushes.White,
            96);
        dc.DrawText(q, new Point(18, 8));
    }

    // 38. app_update: Cobalt Blue chip/cloud with glowing emerald download arrow
    private static void DrawAppUpdate(DrawingContext dc, double s)
    {
        // Cloud body
        dc.DrawRoundedRectangle(Brush("#2563EB"), new Pen(Brush("#1D4ED8"), 1.8), new Rect(8, 10, 32, 22), 6, 6);

        // Emerald download arrow
        var arr = new PathGeometry();
        var aFig = new PathFigure { StartPoint = new Point(24, 16), IsClosed = true, IsFilled = true };
        aFig.Segments.Add(new LineSegment(new Point(24, 28), true));
        aFig.Segments.Add(new LineSegment(new Point(18, 28), true));
        aFig.Segments.Add(new LineSegment(new Point(24, 38), true));
        aFig.Segments.Add(new LineSegment(new Point(30, 28), true));
        aFig.Segments.Add(new LineSegment(new Point(24, 28), true));
        arr.Figures.Add(aFig);
        dc.DrawGeometry(Brush("#10B981"), new Pen(Brushes.White, 1.6), arr);
    }

    // 39. collapse_chevron: Crisp Slate Chevron pointing Left
    private static void DrawCollapseChevron(DrawingContext dc, double s)
    {
        var pen = new Pen(Brush("#334155"), 3.2) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        dc.DrawLine(pen, new Point(28, 12), new Point(18, 24));
        dc.DrawLine(pen, new Point(18, 24), new Point(28, 36));
    }

    // 40. expand_chevron: Crisp Slate Chevron pointing Right
    private static void DrawExpandChevron(DrawingContext dc, double s)
    {
        var pen = new Pen(Brush("#334155"), 3.2) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        dc.DrawLine(pen, new Point(18, 12), new Point(28, 24));
        dc.DrawLine(pen, new Point(28, 24), new Point(18, 36));
    }
}
