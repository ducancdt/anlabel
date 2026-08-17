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
        Console.WriteLine($"Rendering pure industrial uniform icons to: {IconsDir}");

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

        Console.WriteLine("All 40 industrial icons rendered with 100% pure vector geometry & pixel-perfect centering!");
    }

    private static void Generate(string name, Action<DrawingContext, double> drawAction)
    {
        const int size = 48;
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
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

    // Standard Industrial Color Tokens
    private static readonly SolidColorBrush InkDark = Brush("#1E293B");      // Dark Slate Primary Ink
    private static readonly SolidColorBrush InkBorder = Brush("#0F172A");    // Deep Border Ink
    private static readonly SolidColorBrush CardBg = Brush("#FFFFFF");       // Pure Card Background
    private static readonly SolidColorBrush SurfaceTint = Brush("#F1F5F9");  // Neutral Light Surface
    private static readonly SolidColorBrush BluePrimary = Brush("#1D4ED8");  // Industrial Engineering Blue
    private static readonly SolidColorBrush BlueLight = Brush("#DBEAFE");    // Light Blue Accent Fill
    private static readonly SolidColorBrush BlueLine = Brush("#3B82F6");     // Blue Detail Line
    private static readonly SolidColorBrush AmberPrimary = Brush("#D97706"); // Industrial Amber/Gold
    private static readonly SolidColorBrush AmberLight = Brush("#FEF3C7");   // Light Amber Accent Fill
    private static readonly SolidColorBrush GreenPrimary = Brush("#059669"); // Industrial Emerald Green
    private static readonly SolidColorBrush GreenLight = Brush("#D1FAE5");   // Light Green Accent Fill
    private static readonly SolidColorBrush RedPrimary = Brush("#DC2626");   // Industrial Coral/Crimson Red
    private static readonly SolidColorBrush RedLight = Brush("#FEE2E2");     // Light Red Accent Fill
    private static readonly SolidColorBrush SlateMedium = Brush("#64748B");  // Medium Slate Metal
    private static readonly SolidColorBrush SlateLight = Brush("#CBD5E1");   // Light Slate Metal

    // 1. new: Industrial document with golden plus badge
    private static void DrawNew(DrawingContext dc, double s)
    {
        // Document page (Center X: 22, Y: 24)
        var page = new PathGeometry();
        var fig = new PathFigure { StartPoint = new Point(9, 6), IsClosed = true, IsFilled = true };
        fig.Segments.Add(new LineSegment(new Point(24, 6), true));
        fig.Segments.Add(new LineSegment(new Point(34, 16), true));
        fig.Segments.Add(new LineSegment(new Point(34, 40), true));
        fig.Segments.Add(new LineSegment(new Point(9, 40), true));
        page.Figures.Add(fig);

        dc.DrawGeometry(CardBg, new Pen(InkDark, 2.2), page);

        // Fold corner
        var fold = new PathGeometry();
        var foldFig = new PathFigure { StartPoint = new Point(24, 6), IsClosed = true, IsFilled = true };
        foldFig.Segments.Add(new LineSegment(new Point(34, 16), true));
        foldFig.Segments.Add(new LineSegment(new Point(24, 16), true));
        fold.Figures.Add(foldFig);
        dc.DrawGeometry(BlueLight, new Pen(InkDark, 1.8), fold);

        // Content lines
        var linePen = new Pen(BlueLine, 2.0) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        dc.DrawLine(linePen, new Point(15, 18), new Point(22, 18));
        dc.DrawLine(linePen, new Point(15, 24), new Point(28, 24));
        dc.DrawLine(linePen, new Point(15, 30), new Point(22, 30));

        // Plus badge (Amber circle with white cross)
        dc.DrawEllipse(AmberPrimary, new Pen(CardBg, 2.0), new Point(34, 34), 9.5, 9.5);
        var plusPen = new Pen(CardBg, 2.4) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        dc.DrawLine(plusPen, new Point(34, 28.5), new Point(34, 39.5));
        dc.DrawLine(plusPen, new Point(28.5, 34), new Point(39.5, 34));
    }

    // 2. open: Industrial amber folder with blue document
    private static void DrawOpen(DrawingContext dc, double s)
    {
        // Back folder tab
        var tab = new PathGeometry();
        var tabFig = new PathFigure { StartPoint = new Point(6, 12), IsClosed = true, IsFilled = true };
        tabFig.Segments.Add(new LineSegment(new Point(18, 12), true));
        tabFig.Segments.Add(new LineSegment(new Point(22, 16), true));
        tabFig.Segments.Add(new LineSegment(new Point(42, 16), true));
        tabFig.Segments.Add(new LineSegment(new Point(42, 40), true));
        tabFig.Segments.Add(new LineSegment(new Point(6, 40), true));
        tab.Figures.Add(tabFig);
        dc.DrawGeometry(AmberPrimary, new Pen(InkDark, 2.0), tab);

        // Emerging white/blue sheet
        dc.DrawRoundedRectangle(CardBg, new Pen(BluePrimary, 1.8), new Rect(14, 8, 20, 22), 2, 2);
        var bluePen = new Pen(BlueLine, 2.0) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        dc.DrawLine(bluePen, new Point(18, 14), new Point(30, 14));
        dc.DrawLine(bluePen, new Point(18, 19), new Point(26, 19));

        // Front open folder flap
        var front = new PathGeometry();
        var frontFig = new PathFigure { StartPoint = new Point(4, 21), IsClosed = true, IsFilled = true };
        frontFig.Segments.Add(new LineSegment(new Point(44, 21), true));
        frontFig.Segments.Add(new LineSegment(new Point(40, 41), true));
        frontFig.Segments.Add(new LineSegment(new Point(8, 41), true));
        front.Figures.Add(frontFig);
        dc.DrawGeometry(AmberLight, new Pen(InkDark, 2.0), front);

        // Upward blue arrow on folder
        var arrowPen = new Pen(BluePrimary, 2.4) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        dc.DrawLine(arrowPen, new Point(24, 36), new Point(24, 26));
        dc.DrawLine(arrowPen, new Point(19, 31), new Point(24, 26));
        dc.DrawLine(arrowPen, new Point(29, 31), new Point(24, 26));
    }

    // 3. save: Industrial Floppy Disk with silver shutter
    private static void DrawSave(DrawingContext dc, double s)
    {
        // Body (Dark Slate Blue)
        dc.DrawRoundedRectangle(Brush("#2563EB"), new Pen(InkBorder, 2.2), new Rect(7, 7, 34, 34), 4, 4);

        // Metal shutter at top (Silver)
        dc.DrawRoundedRectangle(SlateLight, new Pen(InkBorder, 1.6), new Rect(14, 7, 20, 14), 2, 2);
        // Shutter notch
        dc.DrawRectangle(InkDark, null, new Rect(18, 10, 4, 8));

        // Label sticker at bottom (White)
        dc.DrawRoundedRectangle(CardBg, new Pen(SlateLight, 1.4), new Rect(12, 25, 24, 14), 2, 2);
        var labelPen = new Pen(BlueLine, 1.8) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        dc.DrawLine(labelPen, new Point(16, 29), new Point(32, 29));
        dc.DrawLine(labelPen, new Point(16, 33), new Point(28, 33));
    }

    // 4. folder: Classic Industrial Folder
    private static void DrawFolder(DrawingContext dc, double s)
    {
        var tab = new PathGeometry();
        var tabFig = new PathFigure { StartPoint = new Point(6, 11), IsClosed = true, IsFilled = true };
        tabFig.Segments.Add(new LineSegment(new Point(18, 11), true));
        tabFig.Segments.Add(new LineSegment(new Point(22, 16), true));
        tabFig.Segments.Add(new LineSegment(new Point(42, 16), true));
        tabFig.Segments.Add(new LineSegment(new Point(42, 40), true));
        tabFig.Segments.Add(new LineSegment(new Point(6, 40), true));
        tab.Figures.Add(tabFig);
        dc.DrawGeometry(AmberPrimary, new Pen(InkBorder, 2.0), tab);

        // Front folder body
        dc.DrawRoundedRectangle(AmberLight, new Pen(InkBorder, 2.0), new Rect(6, 17, 36, 23), 3, 3);
        // Inner divider
        dc.DrawLine(new Pen(AmberPrimary, 1.8), new Point(10, 22), new Point(38, 22));
    }

    // 5. revisions: History Clock with emerald counter-clockwise arrow
    private static void DrawRevisions(DrawingContext dc, double s)
    {
        // Clock face centered at (24, 24)
        dc.DrawEllipse(CardBg, new Pen(InkDark, 2.2), new Point(24, 24), 16, 16);
        dc.DrawEllipse(InkDark, null, new Point(24, 24), 2.5, 2.5);

        // Clock hands (9:00)
        var handPen = new Pen(InkDark, 2.4) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        dc.DrawLine(handPen, new Point(24, 24), new Point(24, 14));
        dc.DrawLine(handPen, new Point(24, 24), new Point(14, 24));

        // Emerald counter-clockwise history arrow
        var arcPen = new Pen(GreenPrimary, 2.6) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        var arc = new PathGeometry();
        var arcFig = new PathFigure { StartPoint = new Point(36, 14), IsFilled = false };
        arcFig.Segments.Add(new ArcSegment(new Point(20, 6), new Size(18, 18), 0, false, SweepDirection.Counterclockwise, true));
        arc.Figures.Add(arcFig);
        dc.DrawGeometry(null, arcPen, arc);

        // Arrow head
        var head = new PathGeometry();
        var headFig = new PathFigure { StartPoint = new Point(14, 6), IsClosed = true, IsFilled = true };
        headFig.Segments.Add(new LineSegment(new Point(22, 1), true));
        headFig.Segments.Add(new LineSegment(new Point(22, 11), true));
        head.Figures.Add(headFig);
        dc.DrawGeometry(GreenPrimary, null, head);
    }

    // 6. undo: Industrial Coral Red curved left return arrow
    private static void DrawUndo(DrawingContext dc, double s)
    {
        var pen = new Pen(RedPrimary, 4.0) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        var path = new PathGeometry();
        var fig = new PathFigure { StartPoint = new Point(15, 22), IsFilled = false };
        fig.Segments.Add(new ArcSegment(new Point(37, 30), new Size(17, 17), 0, true, SweepDirection.Clockwise, true));
        path.Figures.Add(fig);
        dc.DrawGeometry(null, pen, path);

        // Arrowhead
        var head = new PathGeometry();
        var hFig = new PathFigure { StartPoint = new Point(7, 22), IsClosed = true, IsFilled = true };
        hFig.Segments.Add(new LineSegment(new Point(19, 12), true));
        hFig.Segments.Add(new LineSegment(new Point(19, 32), true));
        head.Figures.Add(hFig);
        dc.DrawGeometry(RedPrimary, null, head);
    }

    // 7. redo: Industrial Emerald Green curved right forward arrow
    private static void DrawRedo(DrawingContext dc, double s)
    {
        var pen = new Pen(GreenPrimary, 4.0) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        var path = new PathGeometry();
        var fig = new PathFigure { StartPoint = new Point(33, 22), IsFilled = false };
        fig.Segments.Add(new ArcSegment(new Point(11, 30), new Size(17, 17), 0, true, SweepDirection.Counterclockwise, true));
        path.Figures.Add(fig);
        dc.DrawGeometry(null, pen, path);

        // Arrowhead
        var head = new PathGeometry();
        var hFig = new PathFigure { StartPoint = new Point(41, 22), IsClosed = true, IsFilled = true };
        hFig.Segments.Add(new LineSegment(new Point(29, 12), true));
        hFig.Segments.Add(new LineSegment(new Point(29, 32), true));
        head.Figures.Add(hFig);
        dc.DrawGeometry(GreenPrimary, null, head);
    }

    // 8. delete_selection: Crimson Red trash can with lid
    private static void DrawDeleteSelection(DrawingContext dc, double s)
    {
        // Can body centered at X=24
        var body = new PathGeometry();
        var bFig = new PathFigure { StartPoint = new Point(13, 16), IsClosed = true, IsFilled = true };
        bFig.Segments.Add(new LineSegment(new Point(35, 16), true));
        bFig.Segments.Add(new LineSegment(new Point(32, 40), true));
        bFig.Segments.Add(new LineSegment(new Point(16, 40), true));
        body.Figures.Add(bFig);
        dc.DrawGeometry(RedLight, new Pen(RedPrimary, 2.0), body);

        // Ribs
        var ribPen = new Pen(RedPrimary, 2.0) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        dc.DrawLine(ribPen, new Point(20, 22), new Point(21, 34));
        dc.DrawLine(ribPen, new Point(24, 22), new Point(24, 34));
        dc.DrawLine(ribPen, new Point(28, 22), new Point(27, 34));

        // Lid
        dc.DrawRoundedRectangle(RedPrimary, new Pen(InkBorder, 1.8), new Rect(10, 11, 28, 5), 2, 2);
        // Handle
        dc.DrawRoundedRectangle(RedPrimary, new Pen(InkBorder, 1.6), new Rect(21, 7, 6, 4), 1, 1);
    }

    // 9. cursor_select: Dark Slate pointer with cyan selection frame
    private static void DrawCursorSelect(DrawingContext dc, double s)
    {
        // Selection bounding box centered
        var boxPen = new Pen(BluePrimary, 1.8) { DashStyle = DashStyles.Dash };
        dc.DrawRectangle(null, boxPen, new Rect(14, 14, 26, 26));

        // Corner handles
        dc.DrawRectangle(BluePrimary, null, new Rect(12, 12, 5, 5));
        dc.DrawRectangle(BluePrimary, null, new Rect(37, 12, 5, 5));
        dc.DrawRectangle(BluePrimary, null, new Rect(12, 37, 5, 5));
        dc.DrawRectangle(BluePrimary, null, new Rect(37, 37, 5, 5));

        // Pointer arrow
        var pointer = new PathGeometry();
        var pFig = new PathFigure { StartPoint = new Point(7, 5), IsClosed = true, IsFilled = true };
        pFig.Segments.Add(new LineSegment(new Point(7, 27), true));
        pFig.Segments.Add(new LineSegment(new Point(13, 21), true));
        pFig.Segments.Add(new LineSegment(new Point(19, 29), true));
        pFig.Segments.Add(new LineSegment(new Point(23, 26), true));
        pFig.Segments.Add(new LineSegment(new Point(17, 18), true));
        pFig.Segments.Add(new LineSegment(new Point(25, 18), true));
        pointer.Figures.Add(pFig);
        dc.DrawGeometry(InkDark, new Pen(CardBg, 2.0), pointer);
    }

    // 10. zoom_plus: Industrial magnifying glass with green plus
    private static void DrawZoomPlus(DrawingContext dc, double s)
    {
        // Glass rim centered at (20, 20)
        dc.DrawEllipse(SurfaceTint, new Pen(InkDark, 3.2), new Point(20, 20), 13, 13);

        // Handle
        var handlePen = new Pen(InkDark, 4.6) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        dc.DrawLine(handlePen, new Point(30, 30), new Point(41, 41));

        // Green Plus
        var plusPen = new Pen(GreenPrimary, 3.0) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        dc.DrawLine(plusPen, new Point(20, 13), new Point(20, 27));
        dc.DrawLine(plusPen, new Point(13, 20), new Point(27, 20));
    }

    // 11. zoom_minus: Industrial magnifying glass with coral minus
    private static void DrawZoomMinus(DrawingContext dc, double s)
    {
        // Glass rim centered at (20, 20)
        dc.DrawEllipse(SurfaceTint, new Pen(InkDark, 3.2), new Point(20, 20), 13, 13);

        // Handle
        var handlePen = new Pen(InkDark, 4.6) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        dc.DrawLine(handlePen, new Point(30, 30), new Point(41, 41));

        // Coral Minus
        var minusPen = new Pen(RedPrimary, 3.0) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        dc.DrawLine(minusPen, new Point(13, 20), new Point(27, 20));
    }

    // 12. snap_grid: Indigo grid with magnetic nodes
    private static void DrawSnapGrid(DrawingContext dc, double s)
    {
        dc.DrawRoundedRectangle(SurfaceTint, new Pen(InkDark, 2.0), new Rect(6, 6, 36, 36), 4, 4);

        // Grid lines
        var gridPen = new Pen(SlateMedium, 1.4);
        dc.DrawLine(gridPen, new Point(18, 6), new Point(18, 42));
        dc.DrawLine(gridPen, new Point(30, 6), new Point(30, 42));
        dc.DrawLine(gridPen, new Point(6, 18), new Point(42, 18));
        dc.DrawLine(gridPen, new Point(6, 30), new Point(42, 30));

        // Magnetic snap target points (Blue glowing dots)
        dc.DrawEllipse(BluePrimary, new Pen(CardBg, 1.2), new Point(18, 18), 3.5, 3.5);
        dc.DrawEllipse(BluePrimary, new Pen(CardBg, 1.2), new Point(30, 18), 3.5, 3.5);
        dc.DrawEllipse(BluePrimary, new Pen(CardBg, 1.2), new Point(18, 30), 3.5, 3.5);
        dc.DrawEllipse(BluePrimary, new Pen(CardBg, 1.2), new Point(30, 30), 3.5, 3.5);
    }

    // 13. snap_objects: Purple & Blue boxes with alignment guide
    private static void DrawSnapObjects(DrawingContext dc, double s)
    {
        // Box 1 (Left top)
        dc.DrawRoundedRectangle(Brush("#4F46E5"), new Pen(InkDark, 1.8), new Rect(6, 8, 18, 18), 3, 3);

        // Box 2 (Right bottom)
        dc.DrawRoundedRectangle(BluePrimary, new Pen(InkDark, 1.8), new Rect(24, 22, 18, 18), 3, 3);

        // Alignment guideline (Amber dashed)
        var guidePen = new Pen(AmberPrimary, 2.2) { DashStyle = DashStyles.Dash };
        dc.DrawLine(guidePen, new Point(24, 5), new Point(24, 43));

        // Snap indicator markers
        dc.DrawEllipse(AmberPrimary, new Pen(InkDark, 1.2), new Point(24, 8), 3, 3);
        dc.DrawEllipse(AmberPrimary, new Pen(InkDark, 1.2), new Point(24, 22), 3, 3);
    }

    // 14. panels: Multi-layered panel deck
    private static void DrawPanels(DrawingContext dc, double s)
    {
        // Bottom layer
        dc.DrawRoundedRectangle(SlateLight, new Pen(InkDark, 1.4), new Rect(6, 26, 36, 14), 3, 3);

        // Middle layer
        dc.DrawRoundedRectangle(SlateMedium, new Pen(InkDark, 1.6), new Rect(6, 17, 36, 14), 3, 3);

        // Top active layer (Vibrant Blue)
        dc.DrawRoundedRectangle(BluePrimary, new Pen(InkBorder, 2.0), new Rect(6, 8, 36, 14), 3, 3);
        // Header highlight
        dc.DrawRoundedRectangle(BlueLight, null, new Rect(10, 11, 14, 3.5), 1, 1);
    }

    // 15. table: Pure vector spreadsheet grid
    private static void DrawTable(DrawingContext dc, double s)
    {
        dc.DrawRoundedRectangle(CardBg, new Pen(InkDark, 2.0), new Rect(6, 7, 36, 34), 3, 3);

        // Header Row (Blue)
        var head = new PathGeometry();
        var hFig = new PathFigure { StartPoint = new Point(6, 10), IsClosed = true, IsFilled = true };
        hFig.Segments.Add(new ArcSegment(new Point(9, 7), new Size(3, 3), 0, false, SweepDirection.Clockwise, true));
        hFig.Segments.Add(new LineSegment(new Point(39, 7), true));
        hFig.Segments.Add(new ArcSegment(new Point(42, 10), new Size(3, 3), 0, false, SweepDirection.Clockwise, true));
        hFig.Segments.Add(new LineSegment(new Point(42, 18), true));
        hFig.Segments.Add(new LineSegment(new Point(6, 18), true));
        head.Figures.Add(hFig);
        dc.DrawGeometry(BluePrimary, null, head);

        // Grid lines
        var gridPen = new Pen(InkDark, 1.4);
        dc.DrawLine(gridPen, new Point(18, 7), new Point(18, 41));
        dc.DrawLine(gridPen, new Point(30, 7), new Point(30, 41));
        dc.DrawLine(gridPen, new Point(6, 26), new Point(42, 26));
        dc.DrawLine(gridPen, new Point(6, 33), new Point(42, 33));
    }

    // 16. static_text: Pure vector geometric serif "T" (100% font-independent)
    private static void DrawStaticText(DrawingContext dc, double s)
    {
        // Vector Serif "T" centered at X=24, Y=22
        var tPath = new PathGeometry();
        var fig = new PathFigure { StartPoint = new Point(10, 8), IsClosed = true, IsFilled = true };
        fig.Segments.Add(new LineSegment(new Point(38, 8), true));   // Top bar right
        fig.Segments.Add(new LineSegment(new Point(38, 14), true));  // Right serif down
        fig.Segments.Add(new LineSegment(new Point(34, 14), true));  // Right serif in
        fig.Segments.Add(new LineSegment(new Point(27, 14), true));  // To stem right
        fig.Segments.Add(new LineSegment(new Point(27, 32), true));  // Stem down
        fig.Segments.Add(new LineSegment(new Point(31, 32), true));  // Bottom foot right
        fig.Segments.Add(new LineSegment(new Point(31, 36), true));  // Foot right down
        fig.Segments.Add(new LineSegment(new Point(17, 36), true));  // Foot left
        fig.Segments.Add(new LineSegment(new Point(17, 32), true));  // Foot left up
        fig.Segments.Add(new LineSegment(new Point(21, 32), true));  // To stem left
        fig.Segments.Add(new LineSegment(new Point(21, 14), true));  // Stem up
        fig.Segments.Add(new LineSegment(new Point(14, 14), true));  // Left serif in
        fig.Segments.Add(new LineSegment(new Point(10, 14), true));  // Left serif out
        tPath.Figures.Add(fig);

        dc.DrawGeometry(Brush("#4F46E5"), new Pen(InkDark, 1.8), tPath);

        // Baseline stroke
        var basePen = new Pen(BluePrimary, 2.6) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        dc.DrawLine(basePen, new Point(7, 41), new Point(41, 41));
    }

    // 17. text_box: Pure vector bounded frame with "T" & text lines
    private static void DrawTextBox(DrawingContext dc, double s)
    {
        // Bounding frame
        var framePen = new Pen(BluePrimary, 2.0) { DashStyle = DashStyles.Dash };
        dc.DrawRectangle(BlueLight, framePen, new Rect(6, 6, 36, 36));

        // Corner handles
        dc.DrawRectangle(BluePrimary, null, new Rect(4, 4, 5, 5));
        dc.DrawRectangle(BluePrimary, null, new Rect(39, 4, 5, 5));
        dc.DrawRectangle(BluePrimary, null, new Rect(4, 39, 5, 5));
        dc.DrawRectangle(BluePrimary, null, new Rect(39, 39, 5, 5));

        // Inner vector "T"
        var tPath = new PathGeometry();
        var fig = new PathFigure { StartPoint = new Point(11, 11), IsClosed = true, IsFilled = true };
        fig.Segments.Add(new LineSegment(new Point(21, 11), true));
        fig.Segments.Add(new LineSegment(new Point(21, 14), true));
        fig.Segments.Add(new LineSegment(new Point(17.5, 14), true));
        fig.Segments.Add(new LineSegment(new Point(17.5, 23), true));
        fig.Segments.Add(new LineSegment(new Point(14.5, 23), true));
        fig.Segments.Add(new LineSegment(new Point(14.5, 14), true));
        fig.Segments.Add(new LineSegment(new Point(11, 14), true));
        tPath.Figures.Add(fig);
        dc.DrawGeometry(InkDark, null, tPath);

        // Text lines
        var linePen = new Pen(BlueLine, 2.2) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        dc.DrawLine(linePen, new Point(24, 16), new Point(36, 16));
        dc.DrawLine(linePen, new Point(12, 28), new Point(36, 28));
        dc.DrawLine(linePen, new Point(12, 34), new Point(28, 34));
    }

    // 18. barcode: Precision 1D Bars with red laser line
    private static void DrawBarcode(DrawingContext dc, double s)
    {
        dc.DrawRoundedRectangle(CardBg, new Pen(InkDark, 1.8), new Rect(4, 6, 40, 36), 3, 3);

        // Bars
        var barBrush = InkDark;
        dc.DrawRectangle(barBrush, null, new Rect(8, 10, 2.5, 20));
        dc.DrawRectangle(barBrush, null, new Rect(12, 10, 1.5, 20));
        dc.DrawRectangle(barBrush, null, new Rect(15, 10, 4.0, 20));
        dc.DrawRectangle(barBrush, null, new Rect(21, 10, 1.5, 20));
        dc.DrawRectangle(barBrush, null, new Rect(24, 10, 3.0, 20));
        dc.DrawRectangle(barBrush, null, new Rect(29, 10, 1.5, 20));
        dc.DrawRectangle(barBrush, null, new Rect(32, 10, 4.0, 20));
        dc.DrawRectangle(barBrush, null, new Rect(38, 10, 2.0, 20));

        // Red Laser Scan line
        var laserPen = new Pen(RedPrimary, 2.4);
        dc.DrawLine(laserPen, new Point(4, 20), new Point(44, 20));

        // Numbers below
        var numPen = new Pen(SlateMedium, 1.8);
        dc.DrawLine(numPen, new Point(10, 35), new Point(18, 35));
        dc.DrawLine(numPen, new Point(22, 35), new Point(30, 35));
        dc.DrawLine(numPen, new Point(34, 35), new Point(38, 35));
    }

    // 19. qr_code: Dark navy finder patterns + emerald green matrix
    private static void DrawQrCode(DrawingContext dc, double s)
    {
        dc.DrawRoundedRectangle(CardBg, new Pen(InkDark, 1.8), new Rect(5, 5, 38, 38), 4, 4);

        void DrawFinder(double x, double y)
        {
            dc.DrawRectangle(InkDark, null, new Rect(x, y, 11, 11));
            dc.DrawRectangle(CardBg, null, new Rect(x + 2.5, y + 2.5, 6, 6));
            dc.DrawRectangle(InkDark, null, new Rect(x + 4, y + 4, 3, 3));
        }

        DrawFinder(8, 8);
        DrawFinder(29, 8);
        DrawFinder(8, 29);

        // Emerald matrix modules
        var em = GreenPrimary;
        dc.DrawRectangle(em, null, new Rect(22, 10, 3.5, 3.5));
        dc.DrawRectangle(em, null, new Rect(25, 14, 3.5, 3.5));
        dc.DrawRectangle(em, null, new Rect(22, 18, 3.5, 3.5));
        dc.DrawRectangle(em, null, new Rect(25, 22, 3.5, 3.5));
        dc.DrawRectangle(em, null, new Rect(29, 23, 3.5, 3.5));
        dc.DrawRectangle(em, null, new Rect(33, 27, 3.5, 3.5));
        dc.DrawRectangle(em, null, new Rect(23, 29, 3.5, 3.5));
        dc.DrawRectangle(em, null, new Rect(29, 33, 3.5, 3.5));
        dc.DrawRectangle(em, null, new Rect(33, 33, 3.5, 3.5));
    }

    // 20. data_matrix: Solid L-finder + cyan data cells
    private static void DrawDataMatrix(DrawingContext dc, double s)
    {
        dc.DrawRoundedRectangle(CardBg, new Pen(InkDark, 1.8), new Rect(5, 5, 38, 38), 4, 4);

        var dark = InkDark;
        // Solid Left & Bottom "L" borders
        dc.DrawRectangle(dark, null, new Rect(9, 9, 3.5, 30));
        dc.DrawRectangle(dark, null, new Rect(9, 35.5, 30, 3.5));

        // Alternating Top & Right timing borders
        for (int i = 0; i < 5; i++)
        {
            dc.DrawRectangle(dark, null, new Rect(15 + i * 5, 9, 3, 3.5));
            dc.DrawRectangle(dark, null, new Rect(35.5, 15 + i * 5, 3.5, 3));
        }

        // Blue/Cyan matrix data cells
        var cyan = BluePrimary;
        dc.DrawRectangle(cyan, null, new Rect(16, 16, 4, 4));
        dc.DrawRectangle(cyan, null, new Rect(24, 16, 4, 4));
        dc.DrawRectangle(cyan, null, new Rect(20, 22, 4, 4));
        dc.DrawRectangle(cyan, null, new Rect(28, 22, 4, 4));
        dc.DrawRectangle(cyan, null, new Rect(16, 28, 4, 4));
        dc.DrawRectangle(cyan, null, new Rect(26, 28, 4, 4));
    }

    // 21. line: Electric Blue diagonal stroke with anchor nodes
    private static void DrawLine(DrawingContext dc, double s)
    {
        var linePen = new Pen(BluePrimary, 4.0) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        dc.DrawLine(linePen, new Point(9, 39), new Point(39, 9));

        // Start & End Anchor nodes (Blue with white inner)
        dc.DrawEllipse(BluePrimary, new Pen(InkBorder, 2.0), new Point(9, 39), 5.5, 5.5);
        dc.DrawEllipse(BluePrimary, new Pen(InkBorder, 2.0), new Point(39, 9), 5.5, 5.5);
        dc.DrawEllipse(CardBg, null, new Point(9, 39), 2.2, 2.2);
        dc.DrawEllipse(CardBg, null, new Point(39, 9), 2.2, 2.2);
    }

    // 22. rectangle: Warm Amber rounded rectangle
    private static void DrawRectangle(DrawingContext dc, double s)
    {
        dc.DrawRoundedRectangle(AmberLight, new Pen(InkDark, 2.4), new Rect(6, 10, 36, 28), 5, 5);
        dc.DrawRoundedRectangle(AmberPrimary, null, new Rect(10, 14, 28, 6), 2, 2);
    }

    // 23. ellipse: Vibrant Rose circle
    private static void DrawEllipse(DrawingContext dc, double s)
    {
        dc.DrawEllipse(Brush("#FFE4E6"), new Pen(InkDark, 2.4), new Point(24, 24), 16, 16);
        dc.DrawEllipse(Brush("#E11D48"), null, new Point(19, 19), 4.5, 4.5);
    }

    // 24. image: Photo card with mountains and sun
    private static void DrawImage(DrawingContext dc, double s)
    {
        dc.DrawRoundedRectangle(SurfaceTint, new Pen(InkDark, 2.0), new Rect(6, 7, 36, 34), 4, 4);

        // Golden Sun
        dc.DrawEllipse(AmberPrimary, null, new Point(16, 17), 4.5, 4.5);

        // Mountains (Green)
        var mtn = new PathGeometry();
        var mFig = new PathFigure { StartPoint = new Point(6, 40), IsClosed = true, IsFilled = true };
        mFig.Segments.Add(new LineSegment(new Point(18, 24), true));
        mFig.Segments.Add(new LineSegment(new Point(27, 33), true));
        mFig.Segments.Add(new LineSegment(new Point(33, 27), true));
        mFig.Segments.Add(new LineSegment(new Point(42, 40), true));
        mtn.Figures.Add(mFig);
        dc.DrawGeometry(GreenPrimary, null, mtn);
    }

    // 25. database: 3D Cylindrical SQL Database stack
    private static void DrawDatabase(DrawingContext dc, double s)
    {
        var borderPen = new Pen(InkDark, 2.0);
        var cylBrush = BluePrimary;

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
            dc.DrawEllipse(BlueLight, borderPen, new Point(24, y + 5), 14, 5);
        }

        DrawCylinder(22);
        DrawCylinder(13);
        DrawCylinder(4);

        // Emerald connection status LED
        dc.DrawEllipse(GreenPrimary, new Pen(CardBg, 1.4), new Point(36, 36), 4.5, 4.5);
    }

    // 26. import_excel: Vector Excel sheet + Blue Down arrow
    private static void DrawImportExcel(DrawingContext dc, double s)
    {
        // Excel Sheet (Green)
        dc.DrawRoundedRectangle(GreenPrimary, new Pen(InkDark, 2.0), new Rect(6, 6, 26, 34), 3, 3);

        // Vector "X" on Excel sheet
        var xPen = new Pen(CardBg, 3.0) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        dc.DrawLine(xPen, new Point(13, 16), new Point(25, 28));
        dc.DrawLine(xPen, new Point(25, 16), new Point(13, 28));

        // Downward Blue Import Arrow
        var arr = new PathGeometry();
        var aFig = new PathFigure { StartPoint = new Point(34, 18), IsClosed = true, IsFilled = true };
        aFig.Segments.Add(new LineSegment(new Point(34, 28), true));
        aFig.Segments.Add(new LineSegment(new Point(27, 28), true));
        aFig.Segments.Add(new LineSegment(new Point(37, 41), true));
        aFig.Segments.Add(new LineSegment(new Point(47, 28), true));
        aFig.Segments.Add(new LineSegment(new Point(40, 28), true));
        aFig.Segments.Add(new LineSegment(new Point(40, 18), true));
        arr.Figures.Add(aFig);
        dc.DrawGeometry(BluePrimary, new Pen(CardBg, 2.0), arr);
    }

    // 27. export_excel: Vector Excel sheet + Amber Up arrow
    private static void DrawExportExcel(DrawingContext dc, double s)
    {
        // Excel Sheet (Green)
        dc.DrawRoundedRectangle(GreenPrimary, new Pen(InkDark, 2.0), new Rect(6, 10, 26, 34), 3, 3);

        // Vector "X"
        var xPen = new Pen(CardBg, 3.0) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        dc.DrawLine(xPen, new Point(13, 20), new Point(25, 32));
        dc.DrawLine(xPen, new Point(25, 20), new Point(13, 32));

        // Upward Amber Export Arrow
        var arr = new PathGeometry();
        var aFig = new PathFigure { StartPoint = new Point(37, 5), IsClosed = true, IsFilled = true };
        aFig.Segments.Add(new LineSegment(new Point(27, 18), true));
        aFig.Segments.Add(new LineSegment(new Point(34, 18), true));
        aFig.Segments.Add(new LineSegment(new Point(34, 28), true));
        aFig.Segments.Add(new LineSegment(new Point(40, 28), true));
        aFig.Segments.Add(new LineSegment(new Point(40, 18), true));
        aFig.Segments.Add(new LineSegment(new Point(47, 18), true));
        arr.Figures.Add(aFig);
        dc.DrawGeometry(AmberPrimary, new Pen(CardBg, 2.0), arr);
    }

    // 28. update_excel: Vector Excel sheet + Sync arrows
    private static void DrawUpdateExcel(DrawingContext dc, double s)
    {
        // Excel Sheet (Green)
        dc.DrawRoundedRectangle(GreenPrimary, new Pen(InkDark, 2.0), new Rect(7, 7, 24, 28), 3, 3);

        // Vector "X"
        var xPen = new Pen(CardBg, 2.5) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        dc.DrawLine(xPen, new Point(14, 16), new Point(24, 26));
        dc.DrawLine(xPen, new Point(24, 16), new Point(14, 26));

        // Sync arrows (Blue)
        var syncPen = new Pen(BluePrimary, 3.2) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        var arc = new PathGeometry();
        var aFig = new PathFigure { StartPoint = new Point(34, 18), IsFilled = false };
        aFig.Segments.Add(new ArcSegment(new Point(28, 41), new Size(12, 12), 0, true, SweepDirection.Clockwise, true));
        arc.Figures.Add(aFig);
        dc.DrawGeometry(null, syncPen, arc);

        // Arrow tip
        var head = new PathGeometry();
        var hFig = new PathFigure { StartPoint = new Point(34, 11), IsClosed = true, IsFilled = true };
        hFig.Segments.Add(new LineSegment(new Point(41, 18), true));
        hFig.Segments.Add(new LineSegment(new Point(30, 23), true));
        head.Figures.Add(hFig);
        dc.DrawGeometry(BluePrimary, null, head);
    }

    // 29. print_current: Industrial Thermal Printer with blue label feed and green LED
    private static void DrawPrintCurrent(DrawingContext dc, double s)
    {
        // Printer body (Dark Slate)
        dc.DrawRoundedRectangle(InkDark, new Pen(InkBorder, 2.0), new Rect(6, 13, 36, 27), 4, 4);

        // Top cover lid
        dc.DrawRoundedRectangle(SlateMedium, null, new Rect(9, 15, 30, 8), 2, 2);

        // Label feed slot
        dc.DrawRectangle(InkBorder, null, new Rect(12, 25, 24, 3));

        // Printed label sheet emerging
        dc.DrawRoundedRectangle(CardBg, new Pen(BluePrimary, 1.8), new Rect(14, 27, 20, 17), 2, 2);
        // Barcode lines
        var bPen = new Pen(InkDark, 1.6);
        dc.DrawLine(bPen, new Point(17, 31), new Point(17, 38));
        dc.DrawLine(bPen, new Point(20, 31), new Point(20, 38));
        dc.DrawLine(bPen, new Point(22, 31), new Point(22, 38));
        dc.DrawLine(bPen, new Point(25, 31), new Point(25, 38));
        dc.DrawLine(bPen, new Point(28, 31), new Point(28, 38));
        dc.DrawLine(bPen, new Point(31, 31), new Point(31, 38));

        // Green power LED
        dc.DrawEllipse(GreenPrimary, new Pen(CardBg, 1.0), new Point(11, 19), 2.5, 2.5);
    }

    // 30. print_all_rows: Thermal Printer with batch labels
    private static void DrawPrintAllRows(DrawingContext dc, double s)
    {
        // Printer body
        dc.DrawRoundedRectangle(InkDark, new Pen(InkBorder, 2.0), new Rect(6, 9, 36, 23), 4, 4);
        dc.DrawRoundedRectangle(SlateMedium, null, new Rect(9, 11, 30, 6), 2, 2);
        dc.DrawRectangle(InkBorder, null, new Rect(12, 19, 24, 3));

        // Label 1 (behind)
        dc.DrawRoundedRectangle(BlueLight, new Pen(BlueLine, 1.4), new Rect(18, 21, 18, 17), 2, 2);

        // Label 2 (middle)
        dc.DrawRoundedRectangle(Brush("#BFDBFE"), new Pen(BluePrimary, 1.6), new Rect(14, 25, 18, 17), 2, 2);

        // Label 3 (front)
        dc.DrawRoundedRectangle(CardBg, new Pen(BluePrimary, 1.8), new Rect(10, 29, 18, 17), 2, 2);
        var bPen = new Pen(InkDark, 1.4);
        dc.DrawLine(bPen, new Point(13, 33), new Point(13, 40));
        dc.DrawLine(bPen, new Point(16, 33), new Point(16, 40));
        dc.DrawLine(bPen, new Point(19, 33), new Point(19, 40));
        dc.DrawLine(bPen, new Point(23, 33), new Point(23, 40));

        // Batch badge (Amber)
        dc.DrawEllipse(AmberPrimary, new Pen(CardBg, 1.4), new Point(36, 36), 7.5, 7.5);
        var pPen = new Pen(CardBg, 1.8) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        dc.DrawLine(pPen, new Point(33, 36), new Point(39, 36));
        dc.DrawLine(pPen, new Point(36, 33), new Point(36, 39));
    }

    // 31. preview: Document sheet under magnifying glass
    private static void DrawPreview(DrawingContext dc, double s)
    {
        // Document
        dc.DrawRoundedRectangle(CardBg, new Pen(InkDark, 2.0), new Rect(8, 5, 26, 36), 3, 3);
        var pPen = new Pen(BlueLine, 2.0) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        dc.DrawLine(pPen, new Point(12, 12), new Point(24, 12));
        dc.DrawLine(pPen, new Point(12, 18), new Point(28, 18));
        dc.DrawLine(pPen, new Point(12, 24), new Point(26, 24));
        dc.DrawLine(pPen, new Point(12, 30), new Point(20, 30));

        // Magnifying glass over sheet
        dc.DrawEllipse(SurfaceTint, new Pen(BluePrimary, 3.0), new Point(29, 25), 11, 11);
        var handlePen = new Pen(InkDark, 4.2) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        dc.DrawLine(handlePen, new Point(37, 33), new Point(44, 40));
    }

    // 32. printer_setup: Industrial printer with golden setup gear
    private static void DrawPrinterSetup(DrawingContext dc, double s)
    {
        // Printer
        dc.DrawRoundedRectangle(InkDark, new Pen(InkBorder, 2.0), new Rect(6, 10, 30, 26), 3, 3);
        dc.DrawRectangle(InkBorder, null, new Rect(10, 21, 20, 3));
        dc.DrawRoundedRectangle(CardBg, new Pen(SlateMedium, 1.4), new Rect(12, 22, 16, 11), 1, 1);

        // Golden Gear (Setup)
        dc.DrawEllipse(AmberPrimary, new Pen(InkBorder, 2.0), new Point(34, 30), 9.5, 9.5);
        dc.DrawEllipse(CardBg, new Pen(InkBorder, 1.4), new Point(34, 30), 4, 4);
    }

    // 33. printer_status: Industrial printer with emerald check badge
    private static void DrawPrinterStatus(DrawingContext dc, double s)
    {
        // Printer
        dc.DrawRoundedRectangle(InkDark, new Pen(InkBorder, 2.0), new Rect(6, 9, 30, 26), 3, 3);
        dc.DrawRectangle(InkBorder, null, new Rect(10, 20, 20, 3));
        dc.DrawRoundedRectangle(CardBg, new Pen(SlateMedium, 1.4), new Rect(12, 21, 16, 11), 1, 1);

        // Emerald Check Badge
        dc.DrawEllipse(GreenPrimary, new Pen(CardBg, 2.0), new Point(34, 30), 9.5, 9.5);
        var chkPen = new Pen(CardBg, 2.4) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        dc.DrawLine(chkPen, new Point(30, 30), new Point(33, 33));
        dc.DrawLine(chkPen, new Point(33, 33), new Point(38, 27));
    }

    // 34. print_history: Navy journal binder with golden clock
    private static void DrawPrintHistory(DrawingContext dc, double s)
    {
        // Log Book
        dc.DrawRoundedRectangle(Brush("#1E3A8A"), new Pen(InkBorder, 2.0), new Rect(6, 6, 26, 36), 3, 3);
        // Spine
        dc.DrawRoundedRectangle(BluePrimary, null, new Rect(6, 6, 6, 36), 2, 2);
        // Lines
        var lPen = new Pen(BlueLight, 1.8) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        dc.DrawLine(lPen, new Point(16, 14), new Point(27, 14));
        dc.DrawLine(lPen, new Point(16, 20), new Point(25, 20));

        // Golden History Clock
        dc.DrawEllipse(AmberLight, new Pen(AmberPrimary, 2.4), new Point(33, 28), 10.5, 10.5);
        var hPen = new Pen(AmberPrimary, 2.0) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        dc.DrawLine(hPen, new Point(33, 28), new Point(33, 22));
        dc.DrawLine(hPen, new Point(33, 28), new Point(38, 28));
    }

    // 35. test_print: Alignment crosshairs target sheet
    private static void DrawTestPrint(DrawingContext dc, double s)
    {
        dc.DrawRoundedRectangle(CardBg, new Pen(InkDark, 2.2), new Rect(5, 5, 38, 38), 4, 4);

        // Concentric target circles
        dc.DrawEllipse(null, new Pen(BlueLine, 1.6), new Point(24, 24), 14, 14);
        dc.DrawEllipse(null, new Pen(BluePrimary, 2.0), new Point(24, 24), 8.5, 8.5);
        dc.DrawEllipse(RedPrimary, null, new Point(24, 24), 3.5, 3.5);

        // Crosshairs
        var crossPen = new Pen(InkDark, 2.0);
        dc.DrawLine(crossPen, new Point(24, 6), new Point(24, 42));
        dc.DrawLine(crossPen, new Point(6, 24), new Point(42, 24));
    }

    // 36. settings: Interlocking Slate & Blue engineering gears
    private static void DrawSettings(DrawingContext dc, double s)
    {
        // Gear 1 (Slate, larger)
        dc.DrawEllipse(SlateMedium, new Pen(InkDark, 2.2), new Point(20, 20), 12.5, 12.5);
        dc.DrawEllipse(CardBg, new Pen(InkDark, 1.4), new Point(20, 20), 4.5, 4.5);

        // Gear 2 (Blue, smaller)
        dc.DrawEllipse(BluePrimary, new Pen(InkBorder, 2.0), new Point(33, 33), 8.5, 8.5);
        dc.DrawEllipse(CardBg, new Pen(InkBorder, 1.2), new Point(33, 33), 3, 3);
    }

    // 37. help: Pure vector "?" inside Oceanic Blue circular badge
    private static void DrawHelp(DrawingContext dc, double s)
    {
        dc.DrawEllipse(BluePrimary, new Pen(InkBorder, 2.2), new Point(24, 24), 17, 17);

        // Pure vector Question Mark "?"
        var qPen = new Pen(CardBg, 3.0) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        var qPath = new PathGeometry();
        var fig = new PathFigure { StartPoint = new Point(19, 18), IsFilled = false };
        fig.Segments.Add(new ArcSegment(new Point(28, 18), new Size(4.5, 4.5), 0, false, SweepDirection.Clockwise, true));
        fig.Segments.Add(new LineSegment(new Point(24, 23), true));
        fig.Segments.Add(new LineSegment(new Point(24, 27), true));
        qPath.Figures.Add(fig);
        dc.DrawGeometry(null, qPen, qPath);

        // Dot
        dc.DrawEllipse(CardBg, null, new Point(24, 33), 1.8, 1.8);
    }

    // 38. app_update: Blue cloud with download arrow
    private static void DrawAppUpdate(DrawingContext dc, double s)
    {
        dc.DrawRoundedRectangle(BluePrimary, new Pen(InkBorder, 2.0), new Rect(7, 8, 34, 24), 6, 6);

        // Emerald download arrow
        var arr = new PathGeometry();
        var aFig = new PathFigure { StartPoint = new Point(24, 15), IsClosed = true, IsFilled = true };
        aFig.Segments.Add(new LineSegment(new Point(24, 27), true));
        aFig.Segments.Add(new LineSegment(new Point(17, 27), true));
        aFig.Segments.Add(new LineSegment(new Point(24, 39), true));
        aFig.Segments.Add(new LineSegment(new Point(31, 27), true));
        aFig.Segments.Add(new LineSegment(new Point(24, 27), true));
        arr.Figures.Add(aFig);
        dc.DrawGeometry(GreenPrimary, new Pen(CardBg, 2.0), arr);
    }

    // 39. collapse_chevron: Crisp Slate Chevron pointing Left
    private static void DrawCollapseChevron(DrawingContext dc, double s)
    {
        var pen = new Pen(InkDark, 3.4) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        dc.DrawLine(pen, new Point(28, 13), new Point(19, 24));
        dc.DrawLine(pen, new Point(19, 24), new Point(28, 35));
    }

    // 40. expand_chevron: Crisp Slate Chevron pointing Right
    private static void DrawExpandChevron(DrawingContext dc, double s)
    {
        var pen = new Pen(InkDark, 3.4) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        dc.DrawLine(pen, new Point(20, 13), new Point(29, 24));
        dc.DrawLine(pen, new Point(29, 24), new Point(20, 35));
    }
}
