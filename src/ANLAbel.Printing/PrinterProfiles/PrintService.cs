using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using ANLAbel.Core.Enums;
using ANLAbel.Core.Geometry;
using ANLAbel.Core.Models;
using ANLAbel.Printing.RenderPipeline;
using WpfPrintDialog = System.Windows.Controls.PrintDialog;

namespace ANLAbel.Printing.PrinterProfiles;

public sealed class PrintService
{
    private readonly LabelVisualRenderer _renderer = new();
    private readonly PrintPreflightValidator _preflightValidator = new();

    public IReadOnlyList<PrintPreviewPage> CreatePreviewPages(LabelTemplate template, IEnumerable<IReadOnlyDictionary<string, string>?> rows)
    {
        var plan = CreatePlan(template, null);
        var width = MmConverter.MmToDip(plan.LabelWidthMm);
        var height = MmConverter.MmToDip(plan.LabelHeightMm);
        return rows.Select((row, index) => new PrintPreviewPage
            {
                PageNumber = index + 1,
                Visual = _renderer.Render(template, row, plan),
                WidthDip = width,
                HeightDip = height
            })
            .ToArray();
    }

    public PrintPreflightResult ValidateRows(LabelTemplate template, IReadOnlyList<IReadOnlyDictionary<string, string>?> rows)
    {
        return _preflightValidator.Validate(template, rows);
    }

    public bool PrintCurrentRow(LabelTemplate template, IReadOnlyDictionary<string, string>? row)
    {
        return PrintRows(template, new[] { row }, $"{template.Name} label");
    }

    public bool PrintAllRows(LabelTemplate template, IEnumerable<IReadOnlyDictionary<string, string>> rows)
    {
        return PrintRows(template, rows.Cast<IReadOnlyDictionary<string, string>?>(), $"{template.Name} labels");
    }

    public bool PrintRows(LabelTemplate template, IEnumerable<IReadOnlyDictionary<string, string>?> rows, string description)
    {
        var dialog = CreatePrintDialog(template);
        if (dialog.ShowDialog() != true)
        {
            return false;
        }

        PrintRowsWithDialog(template, rows, dialog, description);
        return true;
    }

    public void PrintRows(LabelTemplate template, IEnumerable<IReadOnlyDictionary<string, string>?> rows, WpfPrintDialog dialog, string description)
    {
        PrintRowsWithDialog(template, rows, dialog, description);
    }

    public void PrintRows(LabelTemplate template, IEnumerable<IReadOnlyDictionary<string, string>?> rows, string printerName, string description)
    {
        var dialog = CreatePrintDialog(template, printerName);
        PrintRowsWithDialog(template, rows, dialog, description);
    }

    private void PrintRowsWithDialog(LabelTemplate template, IEnumerable<IReadOnlyDictionary<string, string>?> rows, WpfPrintDialog dialog, string description)
    {
        var rowList = rows.ToArray();
        var preflight = ValidateRows(template, rowList);
        if (!preflight.IsSuccess)
        {
            throw new InvalidOperationException(preflight.ToUserMessage());
        }

        if (rowList.Length == 0)
        {
            return;
        }

        EnsurePrintQueue(dialog, template.PrinterProfile.PrinterName);
        if (dialog.PrintQueue is null)
        {
            throw new InvalidOperationException("No printer is selected.");
        }

        ApplyTemplateTicket(dialog, template);
        var plan = CreatePlan(template, dialog.PrintTicket);
        var pageSize = CreatePageSize(plan);
        var paginator = new VisualDocumentPaginator(
            rowList.Length,
            pageSize,
            pageNumber => _renderer.Render(template, rowList[pageNumber], plan));

        dialog.PrintDocument(paginator, description);
    }

    public bool PrintCalibration(LabelTemplate template)
    {
        var dialog = CreatePrintDialog(template);
        if (dialog.ShowDialog() != true)
        {
            return false;
        }

        try
        {
            ApplyTemplateTicket(dialog, template);
            var plan = CreatePlan(template, dialog.PrintTicket);
            var paginator = new VisualDocumentPaginator(
                1,
                CreatePageSize(plan),
                _ => _renderer.RenderCalibration(plan));

            dialog.PrintDocument(paginator, $"{template.Name} calibration");
        }
        catch
        {
            return false;
        }

        return true;
    }

    public WpfPrintDialog CreatePrintDialog(LabelTemplate template)
    {
        return CreatePrintDialog(template, template.PrinterProfile.PrinterName);
    }

    public WpfPrintDialog CreatePrintDialog(LabelTemplate template, string? printerName)
    {
        var dialog = new WpfPrintDialog();
        EnsurePrintQueue(dialog, printerName);
        ApplyTemplateTicket(dialog, template);
        return dialog;
    }

    private static void EnsurePrintQueue(WpfPrintDialog dialog, string? printerName)
    {
        if (!string.IsNullOrWhiteSpace(printerName))
        {
            try
            {
                using var server = new LocalPrintServer();
                dialog.PrintQueue = server.GetPrintQueue(printerName);
                return;
            }
            catch
            {
                // Fall back to the Windows default printer if the saved printer is unavailable.
            }
        }

        if (dialog.PrintQueue is not null)
        {
            return;
        }

        try
        {
            using var server = new LocalPrintServer();
            dialog.PrintQueue = server.DefaultPrintQueue;
        }
        catch
        {
            // The caller will surface "No printer is selected" if Windows has no usable printer.
        }
    }

    private static PrintRenderPlan CreatePlan(LabelTemplate template, PrintTicket? ticket)
    {
        var dpi = template.PrinterProfile.Dpi > 0 ? template.PrinterProfile.Dpi : template.Dpi;
        if (ticket?.PageResolution?.X is not null && ticket.PageResolution.X.Value > 0)
        {
            dpi = ticket.PageResolution.X.Value;
        }

        return new PrintRenderPlan
        {
            Dpi = dpi,
            LabelWidthMm = template.WidthMm,
            LabelHeightMm = template.HeightMm,
            GapMm = template.PrinterProfile.GapMm > 0 ? template.PrinterProfile.GapMm : template.GapMm,
            MarginMm = template.MarginMm,
            OffsetXMm = template.PrinterProfile.OffsetXMm,
            OffsetYMm = template.PrinterProfile.OffsetYMm,
            Rotated180 = template.PrinterProfile.Rotated180,
            MediaType = template.PrinterProfile.MediaType,
            FeedDirection = template.PrinterProfile.FeedDirection,
            ScaleX = template.PrinterProfile.ScaleX == 0 ? 1 : template.PrinterProfile.ScaleX,
            ScaleY = template.PrinterProfile.ScaleY == 0 ? 1 : template.PrinterProfile.ScaleY
        };
    }

    private static void ApplyTemplateTicket(WpfPrintDialog dialog, LabelTemplate template)
    {
        if (dialog.PrintQueue is null)
        {
            return;
        }

        var ticket = dialog.PrintTicket ?? dialog.PrintQueue.DefaultPrintTicket ?? new PrintTicket();
        // Use physical paper dimensions (from PaperSizePrinter profile) for the PageMediaSize ticket.
        // When design orientation is Landscape, Template.WidthMm/HeightMm may be swapped for display.
        // The physical dimensions are always the original paper size selected by the user.
        var physicalWidthMm = template.PrinterProfile.PhysicalWidthMm > 0 ? template.PrinterProfile.PhysicalWidthMm : template.WidthMm;
        var physicalHeightMm = template.PrinterProfile.PhysicalHeightMm > 0 ? template.PrinterProfile.PhysicalHeightMm : template.HeightMm;
        var widthDip = MmConverter.MmToDip(physicalWidthMm);
        var heightDip = MmConverter.MmToDip(physicalHeightMm);

        // IMPORTANT: Do NOT set PageOrientation for thermal/label printers.
        // Thermal printer drivers (Zebra, TSC, Godex, etc.) interpret PageOrientation
        // as a rotation command on the physical media. Since PageMediaSize already carries
        // the exact label dimensions, setting orientation causes the driver to rotate
        // content, breaking the print alignment. By leaving PageOrientation unset,
        // the driver receives the exact physical dimensions and prints correctly.

        try
        {
            // PageMediaSizeName.Unknown signals a custom size not from the driver's paper list.
            // Thermal/label printer drivers rely on the exact width/height rather than named paper sizes.
            ticket.PageMediaSize = new PageMediaSize(PageMediaSizeName.Unknown, widthDip, heightDip);
        }
        catch
        {
            // Fall back to unnamed constructor if the driver rejects Unknown name.
            try
            {
                ticket.PageMediaSize = new PageMediaSize(widthDip, heightDip);
            }
            catch
            {
                // Last resort: keep whatever the driver default is and let the paginator handle sizing.
            }
        }

        var ticketDpi = template.PrinterProfile.Dpi > 0 ? template.PrinterProfile.Dpi : template.Dpi;
        if (ticketDpi > 0)
        {
            try
            {
                ticket.PageResolution = new PageResolution(ticketDpi, ticketDpi);
            }
            catch
            {
                // Keep the driver resolution if it rejects an explicit DPI.
            }
        }

        dialog.PrintTicket = ticket;
    }

    private static System.Windows.Size CreatePageSize(PrintRenderPlan plan)
    {
        return new System.Windows.Size(MmConverter.MmToDip(plan.LabelWidthMm), MmConverter.MmToDip(plan.LabelHeightMm));
    }

    private sealed class VisualDocumentPaginator : DocumentPaginator
    {
        private readonly int _pageCount;
        private readonly Func<int, Visual> _visualFactory;
        private System.Windows.Size _pageSize;

        public VisualDocumentPaginator(int pageCount, System.Windows.Size pageSize, Func<int, Visual> visualFactory)
        {
            _pageCount = pageCount;
            _pageSize = pageSize;
            _visualFactory = visualFactory;
        }

        public override bool IsPageCountValid => true;

        public override int PageCount => _pageCount;

        public override System.Windows.Size PageSize
        {
            get => _pageSize;
            set => _pageSize = value;
        }

        public override IDocumentPaginatorSource? Source => null;

        public override DocumentPage GetPage(int pageNumber)
        {
            if (pageNumber < 0 || pageNumber >= _pageCount)
            {
                return DocumentPage.Missing;
            }

            var pageRect = new Rect(_pageSize);
            var container = new ContainerVisual();
            var background = new DrawingVisual();
            using (var drawingContext = background.RenderOpen())
            {
                drawingContext.DrawRectangle(System.Windows.Media.Brushes.White, null, pageRect);
            }

            container.Children.Add(background);
            container.Children.Add(_visualFactory(pageNumber));
            return new DocumentPage(container, _pageSize, pageRect, pageRect);
        }
    }
}